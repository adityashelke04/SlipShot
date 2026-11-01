using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SlipShot.Services;
using ComDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;

// Synthetic-only OLE payload probe. --headless uses no windows or input.
// --run-input is an opt-in desktop probe; run only after explicit user authorization.
// Mouse input is sent only between the two windows created here; no desktop capture occurs.
internal static class DragDropProbe
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Contains("--headless", StringComparer.Ordinal)) return Headless();
        if (!args.Contains("--run-input", StringComparer.Ordinal))
        {
            Console.Error.WriteLine("Pass --headless to test native payload lifetime without desktop interaction.");
            return 2;
        }
        SetProcessDpiAwarenessContext(new IntPtr(-4));
        Marshal.ThrowExceptionForHR(OleInitialize(IntPtr.Zero));
        int failures = 0;
        int invalid = 0;
        GetCursorPos(out PointL originalCursor);
        PointL lastOwnCursor = originalCursor;
        bool movedCursor = false;
        bool safetyAbort = false;
        bool controllerMode = args.Contains("--controller", StringComparer.Ordinal);
        using var watchdog = new System.Threading.Timer(_ =>
        {
            MouseEvent(0x0004, 0, 0, 0, UIntPtr.Zero);
            Console.Error.WriteLine("SAFETY watchdog expired; releasing synthetic mouse and terminating probe.");
            Environment.Exit(124);
        }, null, TimeSpan.FromSeconds(45), System.Threading.Timeout.InfiniteTimeSpan);
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        var sourceContent = new Border { Background = Brushes.SteelBlue, Child = new TextBlock { Text = "Synthetic drag source", FontSize = 20, Foreground = Brushes.White } };
        var source = new Window { Title = "SlipShot native drag probe source", Left = 40, Top = 100, Width = 300, Height = 220, Topmost = true, Content = sourceContent };
        var destination = new Window { Title = "SlipShot native drag probe target", Left = 400, Top = 100, Width = 300, Height = 220, Topmost = true, Content = new TextBlock { Text = "Native IDropTarget\nLate file and PNG reads", FontSize = 20 } };
        var target = new ProbeTarget();
        string directory = Path.Combine(AppContext.BaseDirectory, "probe-files-" + Guid.NewGuid().ToString("N"));
        IDisposable? controller = null;
        MethodInfo? controllerDrag = null;
        app.Startup += async (_, _) =>
        {
            try
            {
                source.Show(); destination.Show();
                IntPtr sourceHandle = new WindowInteropHelper(source).Handle;
                IntPtr targetHandle = new WindowInteropHelper(destination).Handle;
                var monitor = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
                if (!GetMonitorInfo(MonitorFromPoint(originalCursor, 2), ref monitor)) throw new InvalidOperationException("Monitor work area unavailable.");
                int windowWidth = Math.Min(300, (monitor.Work.Right - monitor.Work.Left - 96) / 2);
                int windowHeight = Math.Min(220, monitor.Work.Bottom - monitor.Work.Top - 64);
                if (windowWidth < 160 || windowHeight < 120) throw new InvalidOperationException("Monitor work area is too small for isolated probe windows.");
                SetWindowPos(sourceHandle, new IntPtr(-1), monitor.Work.Left + 32, monitor.Work.Top + 32, windowWidth, windowHeight, 0x0010);
                SetWindowPos(targetHandle, new IntPtr(-1), monitor.Work.Left + 64 + windowWidth, monitor.Work.Top + 32, windowWidth, windowHeight, 0x0010);
                // WPF registers a window-level target even when AllowDrop is false.
                RevokeDragDrop(new WindowInteropHelper(destination).Handle);
                Marshal.ThrowExceptionForHR(RegisterDragDrop(new WindowInteropHelper(destination).Handle, target));
                await Task.Delay(150);
                Point start = sourceContent.PointToScreen(new Point(100, 70));
                Point end = ((FrameworkElement)destination.Content).PointToScreen(new Point(100, 70));
                sourceContent.QueryContinueDrag += (_, e) => { if (safetyAbort) { e.Action = DragAction.Cancel; e.Handled = true; } };
                BitmapSource image = BitmapSource.Create(64, 40, 96, 96, PixelFormats.Bgra32, null, Enumerable.Repeat((byte)255, 64 * 40 * 4).ToArray(), 64 * 4);
                image.Freeze();
                byte[]? controllerExpected = null;
                if (controllerMode)
                {
                    Type controllerType = typeof(DragTransferStore).Assembly.GetType("SlipShot.AppController", throwOnError: true)!;
                    controller = (IDisposable)Activator.CreateInstance(controllerType, BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { app }, null)!;
                    controllerDrag = controllerType.GetMethod("DragImage", BindingFlags.Instance | BindingFlags.NonPublic)!;
                    Directory.CreateDirectory(directory);
                    string expectedPath = Path.Combine(directory, "synthetic-expected.png");
                    ImageFiles.Save(image, expectedPath);
                    controllerExpected = File.ReadAllBytes(expectedPath);
                    // Deliberately never call Start: no tray, hotkeys, clipboard or capture are initialized.
                }
                var store = new DragTransferStore(directory);
                foreach (bool copyEffect in new[] { true, false })
                for (int attempt = 1; attempt <= 10; attempt++)
                {
                    using DragTransferLease? lease = controllerMode ? null : store.Prepare(image);
                    byte[] expected = controllerExpected ?? File.ReadAllBytes(lease!.FilePath);
                    target.Reset(copyEffect);
                    var down = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    using var inOle = new ManualResetEventSlim();
                    var input = new Thread(() =>
                    {
                        try
                        {
                            RequireOwnedPoint(start, sourceHandle);
                            RequireOwnedPoint(end, targetHandle);
                            SetCursorPos(checked((int)start.X), checked((int)start.Y));
                            lastOwnCursor = new PointL { X = checked((int)start.X), Y = checked((int)start.Y) }; movedCursor = true;
                            RequireOwnedPoint(start, sourceHandle);
                            MouseEvent(0x0002, 0, 0, 0, UIntPtr.Zero);
                            var pressClock = Stopwatch.StartNew();
                            while (GetAsyncKeyState(1) >= 0 && pressClock.ElapsedMilliseconds < 1000) Thread.Sleep(5);
                            if (GetAsyncKeyState(1) >= 0) throw new InvalidOperationException("Synthetic mouse press was not observed.");
                            down.TrySetResult(true);
                            if (!inOle.Wait(3000)) throw new TimeoutException("OLE start did not arrive.");
                            RequireOwnedPoint(end, targetHandle);
                            SetCursorPos(checked((int)end.X), checked((int)end.Y));
                            lastOwnCursor = new PointL { X = checked((int)end.X), Y = checked((int)end.Y) };
                            if (!target.Entered.Wait(1500)) throw new TimeoutException("Native target did not receive DragEnter.");
                            Thread.Sleep(50);
                            if (!GetCursorPos(out PointL current) || current.X != lastOwnCursor.X || current.Y != lastOwnCursor.Y)
                                throw new InvalidOperationException("Pointer moved independently; stopping probe input.");
                            RequireOwnedPoint(end, targetHandle);
                        }
                        catch (Exception error) { safetyAbort = true; down.TrySetException(error); Console.Error.WriteLine("SAFETY " + error.Message); }
                        finally { MouseEvent(0x0004, 0, 0, 0, UIntPtr.Zero); }
                    }) { IsBackground = true };
                    input.Start();
                    await down.Task.WaitAsync(TimeSpan.FromSeconds(3));
                    // Let WPF process the press before entering its native modal drag loop.
                    await Task.Delay(80);
                    inOle.Set();
                    DragDropEffects? effect = null;
                    if (controllerMode) controllerDrag!.Invoke(controller, new object[] { sourceContent, image });
                    else effect = DragDrop.DoDragDrop(sourceContent, lease!.Data, DragDropEffects.Copy);
                    input.Join(3000);
                    if (lease is not null) lease.Complete((effect & DragDropEffects.Copy) != 0);
                    await Task.Delay(50);
                    bool fileReadable = target.Path is string path && File.Exists(path) && File.ReadAllBytes(path).SequenceEqual(expected);
                    string pngResult;
                    try { pngResult = target.Data is not null && ReadPng(target.Data).SequenceEqual(expected) ? "valid" : "missing-or-corrupt"; }
                    catch (Exception error) { pngResult = error.GetType().Name + ":" + error.Message.Replace('\r', ' ').Replace('\n', ' '); }
                    bool passed = fileReadable && pngResult == "valid";
                    if (target.DropCount != 1) invalid++;
                    else if (!passed) failures++;
                    Console.WriteLine($"requestedEffect={(copyEffect ? "Copy" : "None")} attempt={attempt} returnedEffect={effect?.ToString() ?? "controller-trace"} enters={target.EnterCount} drops={target.DropCount} fileReadable={fileReadable} png={pngResult} result={(target.DropCount != 1 ? "INVALID" : passed ? "PASS" : "FAIL")}");
                    if (controllerMode && target.Path is string transferred && File.Exists(transferred))
                    {
                        string expectedFolder = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "data", "drag-out"));
                        if (!string.Equals(Path.GetDirectoryName(Path.GetFullPath(transferred)), expectedFolder, StringComparison.OrdinalIgnoreCase))
                            throw new InvalidOperationException("Controller transfer escaped the isolated probe output.");
                        File.Delete(transferred);
                    }
                    if (safetyAbort) throw new InvalidOperationException("Probe stopped after safety check.");
                    await Task.Delay(30);
                }
                Console.WriteLine($"SUMMARY mode={(controllerMode ? "native-controller" : "native")} total=20 invalid={invalid} failures={failures}");
            }
            catch (Exception error) { failures++; Console.Error.WriteLine(error); }
            finally
            {
                RevokeDragDrop(new WindowInteropHelper(destination).Handle);
                controller?.Dispose();
                source.Close(); destination.Close();
                if (Directory.Exists(directory)) Directory.Delete(directory, true);
                if (movedCursor && GetCursorPos(out PointL finalCursor) && finalCursor.X == lastOwnCursor.X && finalCursor.Y == lastOwnCursor.Y)
                    SetCursorPos(originalCursor.X, originalCursor.Y);
                app.Shutdown(failures == 0 && invalid == 0 ? 0 : 1);
            }
        };
        try { return app.Run(); }
        finally { OleUninitialize(); }
    }

    private static void RequireOwnedPoint(Point point, IntPtr expectedWindow)
    {
        IntPtr window = WindowFromPoint(new PointL { X = checked((int)point.X), Y = checked((int)point.Y) });
        if (GetAncestor(window, 2) != expectedWindow) throw new InvalidOperationException("Probe point is not over its own expected window.");
    }

    private static int Headless()
    {
        int failures = 0;
        string directory = Path.Combine(AppContext.BaseDirectory, "probe-files-" + Guid.NewGuid().ToString("N"));
        try
        {
            BitmapSource image = BitmapSource.Create(64, 40, 96, 96, PixelFormats.Bgra32, null, Enumerable.Repeat((byte)255, 64 * 40 * 4).ToArray(), 64 * 4);
            image.Freeze();
            var store = new DragTransferStore(directory);
            foreach (bool copyEffect in new[] { true, false })
            for (int attempt = 1; attempt <= 10; attempt++)
            {
                using DragTransferLease lease = store.Prepare(image);
                byte[] expected = File.ReadAllBytes(lease.FilePath);
                var data = (ComDataObject)lease.Data;
                string path = ReadFilePath(data);
                lease.Complete(copyEffect);
                bool fileReadable = File.Exists(path) && File.ReadAllBytes(path).SequenceEqual(expected);
                string pngResult;
                try { pngResult = ReadPng(data).SequenceEqual(expected) ? "valid" : "corrupt"; }
                catch (Exception error) { pngResult = error.GetType().Name; }
                bool passed = fileReadable && pngResult == "valid";
                if (!passed) failures++;
                Console.WriteLine($"effect={(copyEffect ? "Copy" : "None")} attempt={attempt} fileReadable={fileReadable} png={pngResult} result={(passed ? "PASS" : "FAIL")}");
            }
            Console.WriteLine($"SUMMARY mode=headless total=20 failures={failures}");
            return failures == 0 ? 0 : 1;
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    private static FORMATETC Format(string name) => new()
    {
        cfFormat = unchecked((short)DataFormats.GetDataFormat(name).Id),
        dwAspect = DVASPECT.DVASPECT_CONTENT, lindex = -1, tymed = TYMED.TYMED_HGLOBAL
    };

    private static byte[] ReadPng(ComDataObject data)
    {
        FORMATETC format = Format("PNG");
        data.GetData(ref format, out STGMEDIUM medium);
        try
        {
            byte[] result = new byte[checked((int)GlobalSize(medium.unionmember))];
            IntPtr pointer = GlobalLock(medium.unionmember);
            if (pointer == IntPtr.Zero) throw new InvalidOperationException("GlobalLock failed.");
            try { Marshal.Copy(pointer, result, 0, result.Length); }
            finally { GlobalUnlock(medium.unionmember); }
            return result;
        }
        finally { ReleaseStgMedium(ref medium); }
    }

    private static string ReadFilePath(ComDataObject data)
    {
        FORMATETC format = Format(DataFormats.FileDrop);
        data.GetData(ref format, out STGMEDIUM medium);
        try
        {
            uint length = DragQueryFile(medium.unionmember, 0, null, 0);
            var path = new System.Text.StringBuilder(checked((int)length + 1));
            DragQueryFile(medium.unionmember, 0, path, length + 1);
            return path.ToString();
        }
        finally { ReleaseStgMedium(ref medium); }
    }

    [ComVisible(true), ClassInterface(ClassInterfaceType.None)]
    private sealed class ProbeTarget : IDropTarget
    {
        private bool copyEffect;
        internal string? Path { get; private set; }
        internal ComDataObject? Data { get; private set; }
        internal int DropCount { get; private set; }
        internal int EnterCount { get; private set; }
        internal ManualResetEventSlim Entered { get; } = new();
        internal void Reset(bool copy) { copyEffect = copy; Path = null; Data = null; DropCount = 0; EnterCount = 0; Entered.Reset(); }
        public int DragEnter(ComDataObject data, uint keys, PointL point, ref uint effect) { EnterCount++; Entered.Set(); effect = 1; return 0; }
        public int DragOver(uint keys, PointL point, ref uint effect) { effect = 1; return 0; }
        public int DragLeave() => 0;
        public int Drop(ComDataObject data, uint keys, PointL point, ref uint effect)
        {
            try
            {
                DropCount++;
                Data = data;
                Path = ReadFilePath(data);
                // A target may start an asynchronous upload while reporting no shell operation.
                effect = copyEffect ? 1u : 0u;
                return 0;
            }
            catch (Exception error) { Console.Error.WriteLine(error); effect = 0; return error.HResult; }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PointL { public int X; public int Y; }
    [StructLayout(LayoutKind.Sequential)] private struct RectL { public int Left; public int Top; public int Right; public int Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo { public int Size; public RectL Monitor; public RectL Work; public uint Flags; }
    [ComImport, Guid("00000122-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IDropTarget
    {
        [PreserveSig] int DragEnter([MarshalAs(UnmanagedType.Interface)] ComDataObject data, uint keys, PointL point, ref uint effect);
        [PreserveSig] int DragOver(uint keys, PointL point, ref uint effect);
        [PreserveSig] int DragLeave();
        [PreserveSig] int Drop([MarshalAs(UnmanagedType.Interface)] ComDataObject data, uint keys, PointL point, ref uint effect);
    }

    [DllImport("ole32.dll")] private static extern int OleInitialize(IntPtr reserved);
    [DllImport("ole32.dll")] private static extern void OleUninitialize();
    [DllImport("ole32.dll")] private static extern int RegisterDragDrop(IntPtr window, [MarshalAs(UnmanagedType.Interface)] IDropTarget target);
    [DllImport("ole32.dll")] private static extern int RevokeDragDrop(IntPtr window);
    [DllImport("ole32.dll")] private static extern void ReleaseStgMedium(ref STGMEDIUM medium);
    [DllImport("kernel32.dll")] private static extern IntPtr GlobalLock(IntPtr memory);
    [DllImport("kernel32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GlobalUnlock(IntPtr memory);
    [DllImport("kernel32.dll")] private static extern UIntPtr GlobalSize(IntPtr memory);
    [DllImport("shell32.dll", EntryPoint = "DragQueryFileW", CharSet = CharSet.Unicode)] private static extern uint DragQueryFile(IntPtr drop, uint index, System.Text.StringBuilder? path, uint count);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetCursorPos(out PointL point);
    [DllImport("user32.dll")] private static extern IntPtr WindowFromPoint(PointL point);
    [DllImport("user32.dll")] private static extern IntPtr GetAncestor(IntPtr window, uint flags);
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromPoint(PointL point, uint flags);
    [DllImport("user32.dll", EntryPoint = "GetMonitorInfoW")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll", EntryPoint = "mouse_event")] private static extern void MouseEvent(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetProcessDpiAwarenessContext(IntPtr context);
}
