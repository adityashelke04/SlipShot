using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SlipShot.Core;
using SlipShot.Services;
using SlipShot.UI;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace SlipShot;

internal sealed class AppController : IDisposable
{
    private readonly Application app;
    private readonly SettingsStore store=new(Path.Combine(AppContext.BaseDirectory,"data","settings.json"));
    private readonly IDesktopCapture capture=new DesktopCapture();
    private readonly DragTransferStore dragTransfers=new(Path.Combine(AppContext.BaseDirectory,"data","drag-out"));
    private readonly PreviewQueue<Captured> queue=new();
    private readonly List<EditorWindow> editors=new();
    private readonly List<PinWindow> pins=new();
    private readonly Dictionary<EditorWindow,EditDocument> documents=new();
    private readonly CaptureGate captureGate=new();
    private readonly CancellationTokenSource lifetime=new();
    private AppSettings settings=new();
    private Forms.NotifyIcon? tray;
    private System.Drawing.Icon? icon;
    private Hotkeys? hotkeys;
    private PreviewWindow? preview;
    private SettingsWindow? preferences;
    private Captured? latest;
    private SelectionWindow? overlay;
    private CancellationTokenSource? activeCapture;
    private bool disposed;
    private bool suppressAdvance;
    private bool exitRequested;
    private CancellationTokenSource? copyRequest;
    private bool saving;
    internal AppController(Application app) => this.app=app;

    internal void Start()
    {
        settings=store.Load();Theme.Configure(settings.Appearance,settings.ReducedMotion);
        var menu=new Forms.ContextMenuStrip();
        menu.Items.Add("Capture region",null,(_,_)=>Dispatch(()=>_ = CaptureRegionAsync()));
        menu.Items.Add("Edit latest capture",null,(_,_)=>Dispatch(()=> {if(latest is not null) OpenEditor(latest);else Notify("No capture yet. Choose Capture region to begin.");}));
        menu.Items.Add("Show latest preview",null,(_,_)=>Dispatch(ReopenPreview));
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Settings",null,(_,_)=>Dispatch(OpenSettings));
        menu.Items.Add("Forget in-memory previews",null,(_,_)=>Dispatch(Forget));
        menu.Items.Add("Exit",null,(_,_)=>Dispatch(TryExit));
        icon=CreateIcon();
        tray=new Forms.NotifyIcon {Icon=icon,Text="SlipShot · "+settings.Hotkey,ContextMenuStrip=menu,Visible=true};
        tray.DoubleClick+=(_,_)=>Dispatch(()=>_ = CaptureRegionAsync());
        hotkeys=new Hotkeys();hotkeys.Invoked+=()=>_ = CaptureRegionAsync();
        if(!hotkeys.Register(settings.Hotkey)) Notify("Capture hotkey is unavailable. Use the tray menu and choose another binding in Settings.");
        else Notify("Ready · "+settings.Hotkey+" captures a region. Screenshots stay in memory until you save or deliberately drag them.");
        if(store.LoadWarning is not null) Notify(store.LoadWarning);
        SystemEvents.DisplaySettingsChanged+=DisplaysChanged;
        SystemEvents.UserPreferenceChanged+=PreferencesChanged;
    }

    private async Task CaptureRegionAsync()
    {
        if(disposed||exitRequested||preview?.IsDragging==true||!captureGate.TryBegin(out long generation)) return;
        var hidden=new List<Window>();
        Task<BitmapSource>? pending=null;
        using var cancel=CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        activeCapture=cancel;
        try
        {
            var monitor=Native.MonitorAtPointer();
            if(!ResourcePolicy.CanAllocate(RetainedBytes(),(long)monitor.Bounds.Width*monitor.Bounds.Height)) {Notify("Close an editor or pinned reference, or forget previews, before another capture to keep image memory bounded.");return;}
            foreach(Window window in app.Windows.Cast<Window>().ToArray())
            {
                if(window.IsVisible) {hidden.Add(window);window.Hide();}
            }
            // Allow the tray menu to close, then wait for DWM to present hidden own windows.
            await app.Dispatcher.InvokeAsync(()=> { },System.Windows.Threading.DispatcherPriority.ContextIdle);
            pending=capture.CaptureAsync(monitor,cancel.Token);
            BitmapSource frame=await pending.WaitAsync(TimeSpan.FromSeconds(3),cancel.Token);
            if(disposed||!captureGate.IsCurrent(generation))return;
            overlay=new SelectionWindow(frame,monitor);
            var selected=new TaskCompletionSource<Int32Rect?>(TaskCreationOptions.RunContinuationsAsynchronously);
            overlay.Closed+=(_,_)=>selected.TrySetResult(overlay?.Selection);
            overlay.Show();
            Int32Rect? rect=await selected.Task;
            overlay=null;
            if(rect is null||cancel.IsCancellationRequested||!captureGate.IsCurrent(generation))return;
            // Copy pixels into a detached bitmap: CroppedBitmap alone would retain the whole monitor frame.
            var crop=new CroppedBitmap(frame,rect.Value);
            int stride=checked(rect.Value.Width*4);var pixels=new byte[checked(stride*rect.Value.Height)];
            crop.CopyPixels(pixels,stride,0);
            var image=BitmapSource.Create(rect.Value.Width,rect.Value.Height,96,96,PixelFormats.Bgra32,null,pixels,stride);image.Freeze();
            var captured=new Captured(image,monitor);latest=captured;
            RetirePreview();queue.Replace(captured);
            if(settings.AutoCopy) await PreviewCopyAsync(image);
        }
        catch(TimeoutException) {cancel.Cancel();captureGate.Cancel();Notify("Capture timed out. Try again after the display is responsive, or use borderless/windowed mode.");}
        catch(OperationCanceledException) { }
        catch(Exception ex) when(IsExpected(ex)) {Notify("Capture unavailable. "+Friendly(ex));}
        finally
        {
            activeCapture=null;
            foreach(var window in hidden)
            {
                if(disposed||!app.Windows.Cast<Window>().Contains(window))continue;
                window.ShowActivated=false;window.Show();
            }
            // A timed-out native call cannot be killed safely. Keep the capture gate occupied until it returns.
            if(pending is not null&&!pending.IsCompleted)
            {
                _=pending.ContinueWith(task=> { _=task.Exception; Dispatch(()=> {captureGate.TryComplete(generation);ShowPreview();}); },TaskScheduler.Default);
            }
            else {captureGate.TryComplete(generation);if(!disposed)ShowPreview();}
        }
    }

    private void ShowPreview()
    {
        if(disposed||exitRequested||captureGate.IsBusy||preview is not null||queue.Current is not Captured shot)return;
        var card=new PreviewWindow(shot.Image,shot.Monitor,settings.PreviewSeconds,
            ()=>OpenEditor(shot),()=>CopyAsync(new EditDocument(shot.Image)),
            ()=>_ = SaveAsync(new EditDocument(shot.Image)),()=> {try {Pin(shot.Image);}catch(InvalidOperationException ex){Notify(ex.Message);} },DragImage);
        preview=card;
        card.Closed+=(_,_)=> {if(!ReferenceEquals(preview,card))return;preview=null;if(!suppressAdvance)queue.Clear();};
        card.Show();
    }
    private void RetirePreview()
    {var retired=preview;preview=null;retired?.Close();}
    private void ReopenPreview()
    {
        if(captureGate.IsBusy||preview?.IsDragging==true)return;
        if(latest is null){Notify("No capture retained. Choose Capture region to begin.");return;}
        try {latest=latest with {Monitor=Native.RefreshMonitor(latest.Monitor)};}
        catch(Exception ex) when(IsExpected(ex)){Notify("No display is available for the preview. The image remains available through Edit latest capture.");return;}
        RetirePreview();queue.Replace(latest);ShowPreview();
    }
    private void Forget()
    {if(preview?.IsDragging==true)return;suppressAdvance=true;preview?.Close();suppressAdvance=false;queue.Clear();latest=null;Notify("Preview memory released. Open editors and pinned references remain available.");}

    private void DragImage(DependencyObject source,BitmapSource image)
    {
        if(!ResourcePolicy.CanAllocate(RetainedBytes(),(long)image.PixelWidth*image.PixelHeight,4))throw new InvalidOperationException("Close another editor or reference before dragging this image.");
        using var transfer=dragTransfers.Prepare(DocumentRenderer.Render(new EditDocument(image)));
        var effect=DragDrop.DoDragDrop(source,transfer.Data,DragDropEffects.Copy);
        transfer.Complete((effect&DragDropEffects.Copy)!=0);
    }

    private void OpenEditor(Captured shot)
    {
        if(editors.Count>=3){Notify("Three editors are already open. Close one before editing another capture.");return;}
        var document=new EditDocument(shot.Image);
        var editor=new EditorWindow(document,SaveAsync,CopyAsync,Pin);
        editors.Add(editor);documents.Add(editor,document);
        editor.SourceInitialized+=(_,_)=>Native.Protect(editor,false);
        editor.Closed+=(_,_)=> {editors.Remove(editor);documents.Remove(editor);if(exitRequested)app.Dispatcher.BeginInvoke(new Action(TryExit));};
        editor.Show();editor.Activate(); // Explicit Edit interaction only.
        preview?.Dismiss();
    }
    private void Pin(BitmapSource image)
    {
        if(pins.Count>=3||!ResourcePolicy.CanAllocate(RetainedBytes(),(long)image.PixelWidth*image.PixelHeight,1))throw new InvalidOperationException("Close a pinned reference or editor before pinning another image.");
        var pin=new PinWindow(image);pins.Add(pin);pin.Closed+=(_,_)=>pins.Remove(pin);pin.Show();
    }
    private async Task CopyAsync(EditDocument document)
    {
        if(!ResourcePolicy.CanAllocate(RetainedBytes(),(long)document.State.Crop.Width*(long)document.State.Crop.Height,5))throw new InvalidOperationException("There is not enough image memory to copy this capture. Close another editor/reference or save it instead.");
        copyRequest?.Cancel();
        using var request=CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
        copyRequest=request;
        try {await ClipboardWriter.CopyAsync(DocumentRenderer.Render(document),new WindowsClipboardSink(),request.Token);Notify("Image copied.");}
        finally {if(ReferenceEquals(copyRequest,request))copyRequest=null;}
    }
    private async Task PreviewCopyAsync(BitmapSource image)
    {try {await CopyAsync(new EditDocument(image));}catch(OperationCanceledException){}catch(Exception ex) when(IsExpected(ex)){Notify("Capture is ready, but automatic copy failed. Use Copy on the preview to retry.");}}
    private Task<bool> SaveAsync(EditDocument document)
    {
        if(saving)return Task.FromResult(false);saving=true;
        try
        {
            if(!ResourcePolicy.CanAllocate(RetainedBytes(),(long)document.State.Crop.Width*(long)document.State.Crop.Height))throw new InvalidOperationException("Close another editor or reference before exporting this image.");
            string directory=settings.SaveDirectory??AppContext.BaseDirectory;
            if(!Directory.Exists(directory))directory=AppContext.BaseDirectory;
            var dialog=new SaveFileDialog {Title="Save flattened screenshot",Filter="PNG image|*.png|JPEG image|*.jpg",DefaultExt=".png",AddExtension=true,OverwritePrompt=false,InitialDirectory=directory,FileName=Path.GetFileName(ImageFiles.GenerateName(directory,DateTime.Now))};
            if(dialog.ShowDialog()!=true)return Task.FromResult(false);
            bool exists=File.Exists(dialog.FileName);
            if(exists&&MessageBox.Show("Replace the existing image? This cannot be undone.","SlipShot",MessageBoxButton.YesNo,MessageBoxImage.Warning,MessageBoxResult.No)!=MessageBoxResult.Yes)return Task.FromResult(false);
            // New-file writes remain non-overwriting even if another process creates a file after this check.
            var background=(Color)ColorConverter.ConvertFromString(settings.JpegBackground);
            ImageFiles.Save(DocumentRenderer.Render(document),dialog.FileName,exists,settings.JpegQuality,background);
            document.MarkSaved();Notify("Image saved.");return Task.FromResult(true);
        }
        catch(Exception ex) when(IsExpected(ex)) {MessageBox.Show("Could not save the image. "+Friendly(ex),"SlipShot",MessageBoxButton.OK,MessageBoxImage.Warning);return Task.FromResult(false);}
        finally {saving=false;}
    }

    private void OpenSettings()
    {
        if(preferences is not null){preferences.Activate();return;}
        preferences=new SettingsWindow(settings,changed=>
        {
            bool changedBinding=changed.Hotkey!=settings.Hotkey;
            if(changedBinding&&hotkeys?.Register(changed.Hotkey)!=true)return "That hotkey is already in use. Try Ctrl+Alt+Q; tray capture remains available.";
            try {store.Save(changed);}
            catch(Exception ex) when(IsExpected(ex)) {if(changedBinding)hotkeys?.Register(settings.Hotkey);return "Preferences could not be written beside the app. Choose a writable portable folder.";}
            settings=changed;Theme.Configure(settings.Appearance,settings.ReducedMotion);if(tray is not null)tray.Text="SlipShot · "+settings.Hotkey;return null;
        });
        preferences.Closed+=(_,_)=>preferences=null;preferences.Show();
    }
    private void TryExit()
    {
        if(disposed)return;
        if(preview?.IsDragging==true){Notify("Finish or cancel the drag before exiting SlipShot.");return;}
        exitRequested=true;
        if(editors.Count>0)
        {
            var editor=editors[0];editor.Close();
            if(editors.Contains(editor)) {if(!editor.ClosePending){exitRequested=false;ShowPreview();}return;}
            if(editors.Count>0){app.Dispatcher.BeginInvoke(new Action(TryExit));return;}
        }
        suppressAdvance=true;queue.Clear();latest=null;preview?.Close();activeCapture?.Cancel();
        app.Shutdown();
    }
    private long RetainedBytes() => documents.Values.Select(x=>x.Source).Concat(queue.Items.Select(x=>x.Image)).Concat(latest is null?Array.Empty<BitmapSource>():new[]{latest.Image}).Distinct().Sum(x=>(long)x.PixelWidth*x.PixelHeight*4)+pins.Sum(x=>x.ImageBytes);
    private void DisplaysChanged(object? sender,EventArgs e)=>Dispatch(()=> {captureGate.Cancel();activeCapture?.Cancel();overlay?.Close();suppressAdvance=true;preview?.Close();suppressAdvance=false;queue.Clear();Notify("Display changed. Pending previews were dismissed; the latest image is still available in the tray.");});
    private void PreferencesChanged(object sender,UserPreferenceChangedEventArgs e)=>Dispatch(()=>Theme.Configure(settings.Appearance,settings.ReducedMotion));
    private void Dispatch(Action action) {if(!disposed&&!app.Dispatcher.HasShutdownStarted)app.Dispatcher.BeginInvoke(action);}
    private void Notify(string message) {if(!disposed)tray?.ShowBalloonTip(3500,"SlipShot",message,Forms.ToolTipIcon.Info);}
    private static bool IsExpected(Exception ex)=>ex is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException or System.ComponentModel.Win32Exception or System.Runtime.InteropServices.ExternalException or NotSupportedException or OutOfMemoryException;
    private static string Friendly(Exception ex)=>ex switch
    {UnauthorizedAccessException=>"The destination is not writable. Choose another folder.",IOException=>"The file or device is unavailable. Choose another location or try again.",OutOfMemoryException=>"Image memory is full. Close editors or pinned references and try again.",_=>"Try again, or check Settings. Protected and exclusive-fullscreen content may be unavailable."};

    private static System.Drawing.Icon CreateIcon()
    {
        // Original vector crop mark, rendered once; no borrowed artwork or asset dependency.
        using var bitmap=new System.Drawing.Bitmap(32,32);
        using(var g=System.Drawing.Graphics.FromImage(bitmap))
        {g.Clear(System.Drawing.Color.FromArgb(34,37,43));using var pen=new System.Drawing.Pen(System.Drawing.Color.FromArgb(130,216,190),3);g.DrawLines(pen,new[]{new System.Drawing.Point(10,5),new System.Drawing.Point(10,22),new System.Drawing.Point(27,22)});g.DrawLines(pen,new[]{new System.Drawing.Point(5,10),new System.Drawing.Point(22,10),new System.Drawing.Point(22,27)});}
        IntPtr handle=bitmap.GetHicon();try {using var borrowed=System.Drawing.Icon.FromHandle(handle);return (System.Drawing.Icon)borrowed.Clone();}finally {DestroyIcon(handle);}
    }
    [System.Runtime.InteropServices.DllImport("user32.dll")]private static extern bool DestroyIcon(IntPtr handle);
    public void Dispose()
    {
        if(disposed)return;disposed=true;lifetime.Cancel();
        SystemEvents.DisplaySettingsChanged-=DisplaysChanged;SystemEvents.UserPreferenceChanged-=PreferencesChanged;
        hotkeys?.Dispose();if(tray is not null){tray.Visible=false;tray.ContextMenuStrip?.Dispose();tray.Dispose();}icon?.Dispose();
        suppressAdvance=true;preview?.Close();overlay?.Close();foreach(var pin in pins.ToArray())pin.Close();queue.Clear();latest=null;lifetime.Dispose();
    }
    private sealed record Captured(BitmapSource Image,CaptureMonitor Monitor);
}
