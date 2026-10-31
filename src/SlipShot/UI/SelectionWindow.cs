using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using SlipShot.Core;
using SlipShot.Services;

namespace SlipShot.UI;

internal sealed class SelectionWindow : Window
{
    private readonly CaptureMonitor monitor;
    private readonly SelectionSurface surface;
    private Point? anchor;
    private Point endpoint;
    public Int32Rect? Selection { get; private set; }
    internal SelectionWindow(BitmapSource frame,CaptureMonitor monitor)
    {
        this.monitor=monitor;
        Title="SlipShot — select within this monitor";
        WindowStyle=WindowStyle.None; ResizeMode=ResizeMode.NoResize; ShowInTaskbar=false; Topmost=true;
        Width=monitor.Bounds.Width/monitor.Scale; Height=monitor.Bounds.Height/monitor.Scale;
        Background=Brushes.Black; Cursor=Cursors.Cross;
        surface=new SelectionSurface(frame); Content=surface;
        SourceInitialized+=(_,_)=> { Native.Place(this,monitor.Bounds); HwndSource.FromHwnd(new WindowInteropHelper(this).Handle)?.AddHook(Messages); };
        Loaded+=(_,_)=> { Native.Place(this,monitor.Bounds); Focus(); };
        MouseLeftButtonDown+=(_,e)=> { anchor=PixelPoint(e.GetPosition(surface)); endpoint=anchor.Value; CaptureMouse(); Update(); };
        MouseMove+=(_,e)=> { if(anchor.HasValue && IsMouseCaptured) { endpoint=PixelPoint(e.GetPosition(surface)); Update(); } };
        MouseLeftButtonUp+=(_,e)=> { if(anchor.HasValue) { endpoint=PixelPoint(e.GetPosition(surface)); Finish(); } };
        LostMouseCapture+=(_,_)=> { if(IsVisible) { anchor=null; surface.Selection=Rect.Empty; surface.InvalidateVisual(); } };
        KeyDown+=Keys;
        Deactivated+=(_,_)=> { if(IsVisible) Close(); };
    }
    private IntPtr Messages(IntPtr hwnd,int msg,IntPtr w,IntPtr l,ref bool handled)
    { if(msg==0x7E || msg==0x2E0) Dispatcher.BeginInvoke(new Action(Close)); return IntPtr.Zero; }
    private Point PixelPoint(Point point) => new(Math.Clamp(point.X/Math.Max(1,surface.ActualWidth)*surface.Frame.PixelWidth,0,surface.Frame.PixelWidth),Math.Clamp(point.Y/Math.Max(1,surface.ActualHeight)*surface.Frame.PixelHeight,0,surface.Frame.PixelHeight));
    private void Update() { surface.Selection=GeometryUtil.Normalize(anchor!.Value,endpoint,new Rect(0,0,surface.Frame.PixelWidth,surface.Frame.PixelHeight)); surface.InvalidateVisual(); }
    private void Finish()
    {
        Update(); var r=surface.Selection;
        if(r.IsEmpty || r.Width<1 || r.Height<1) { anchor=null; ReleaseMouseCapture(); return; }
        int left=(int)Math.Floor(r.Left),top=(int)Math.Floor(r.Top),right=(int)Math.Ceiling(r.Right),bottom=(int)Math.Ceiling(r.Bottom);
        Selection=new Int32Rect(left,top,right-left,bottom-top); Close();
    }
    private void Keys(object sender,KeyEventArgs e)
    {
        if(e.Key==Key.Escape) { Close(); e.Handled=true; return; }
        if(e.Key==Key.Space && !anchor.HasValue) { anchor=new Point(surface.Frame.PixelWidth/2d,surface.Frame.PixelHeight/2d); endpoint=anchor.Value; Update(); e.Handled=true; return; }
        if(!anchor.HasValue) return;
        int step=(Keyboard.Modifiers&ModifierKeys.Shift)!=0?10:1;
        switch(e.Key) { case Key.Left: endpoint.X-=step; break; case Key.Right:endpoint.X+=step;break;case Key.Up:endpoint.Y-=step;break;case Key.Down:endpoint.Y+=step;break;case Key.Enter:Finish();e.Handled=true;return;default:return; }
        endpoint.X=Math.Clamp(endpoint.X,0,surface.Frame.PixelWidth); endpoint.Y=Math.Clamp(endpoint.Y,0,surface.Frame.PixelHeight); Update(); e.Handled=true;
    }
    private sealed class SelectionSurface(BitmapSource frame) : FrameworkElement
    {
        internal BitmapSource Frame=>frame;
        internal Rect Selection=Rect.Empty;
        protected override void OnRender(DrawingContext dc)
        {
            var viewport=new Rect(0,0,ActualWidth,ActualHeight);
            dc.DrawImage(frame,viewport);
            var selected=Selection.IsEmpty?Rect.Empty:new Rect(Selection.X/frame.PixelWidth*ActualWidth,Selection.Y/frame.PixelHeight*ActualHeight,Selection.Width/frame.PixelWidth*ActualWidth,Selection.Height/frame.PixelHeight*ActualHeight);
            var shade=new SolidColorBrush(Color.FromArgb(145,0,0,0));
            if(selected.IsEmpty) dc.DrawRectangle(shade,null,viewport);
            else
            {
                dc.DrawGeometry(shade,null,new CombinedGeometry(GeometryCombineMode.Exclude,new RectangleGeometry(viewport),new RectangleGeometry(selected)));
                dc.DrawRectangle(null,new Pen(Brushes.White,1),selected);
            }
            string label=Selection.IsEmpty?"Drag a region · Esc cancels · This monitor only\nKeyboard: Space starts, arrows adjust, Enter captures":$"{Math.Ceiling(Selection.Width):0} × {Math.Ceiling(Selection.Height):0} px · Release to capture · Esc cancels";
            var text=new FormattedText(label,CultureInfo.CurrentCulture,FlowDirection.LeftToRight,new Typeface("Segoe UI"),14,Brushes.White,VisualTreeHelper.GetDpi(this).PixelsPerDip);
            double x=Math.Max(12,(ActualWidth-text.Width)/2),y=24;
            dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(240,20,20,19)),null,new Rect(x-12,y-8,text.Width+24,text.Height+16),8,8);
            dc.DrawText(text,new Point(x,y));
        }
    }
}
