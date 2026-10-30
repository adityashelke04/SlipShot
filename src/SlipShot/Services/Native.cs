using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace SlipShot.Services;

internal static class Native
{
    internal const int GwlExStyle = -20;
    internal const long NoActivate = 0x08000000, ToolWindow = 0x80;
    internal const uint SwpNoActivate = 0x10;
    [StructLayout(LayoutKind.Sequential)] internal struct PointI { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] internal struct RectI { public int Left, Top, Right, Bottom; public int Width => Right - Left; public int Height => Bottom - Top; }
    [StructLayout(LayoutKind.Sequential)] internal struct MonitorInfo { public int Size; public RectI Monitor, Work; public uint Flags; }
    [DllImport("user32.dll")] internal static extern bool SetProcessDpiAwarenessContext(IntPtr value);
    [DllImport("user32.dll")] internal static extern bool GetCursorPos(out PointI point);
    [DllImport("user32.dll")] internal static extern IntPtr MonitorFromPoint(PointI point, uint flags);
    [DllImport("user32.dll")] internal static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
    [DllImport("shcore.dll")] internal static extern int GetDpiForMonitor(IntPtr monitor, int type, out uint x, out uint y);
    [DllImport("user32.dll", EntryPoint="GetWindowLongPtrW")] internal static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);
    [DllImport("user32.dll", EntryPoint="SetWindowLongPtrW")] internal static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value);
    [DllImport("user32.dll")] internal static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")] internal static extern bool SetWindowDisplayAffinity(IntPtr hwnd, uint affinity);
    [DllImport("user32.dll", SetLastError=true)] internal static extern bool RegisterHotKey(IntPtr hwnd,int id,uint modifiers,uint key);
    [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(IntPtr hwnd,int id);
    [DllImport("dwmapi.dll")] internal static extern int DwmFlush();
    [DllImport("user32.dll")] internal static extern bool IsWindowVisible(IntPtr hwnd);
    [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] internal static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")] internal static extern bool GetWindowRect(IntPtr hwnd,out RectI rect);
    internal static bool PointerInside(IntPtr hwnd) => GetCursorPos(out var p) && GetWindowRect(hwnd,out var r) && p.X>=r.Left && p.X<r.Right && p.Y>=r.Top && p.Y<r.Bottom;

    internal static CaptureMonitor MonitorAtPointer()
    {
        if (!GetCursorPos(out var point)) throw new Win32Exception();
        var handle = MonitorFromPoint(point, 2);
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(handle, ref info)) throw new Win32Exception();
        double scale = GetDpiForMonitor(handle, 0, out var dx, out _) == 0 ? dx / 96d : 1;
        return new CaptureMonitor(handle, info.Monitor, info.Work, scale);
    }

    internal static CaptureMonitor RefreshMonitor(CaptureMonitor previous)
    {
        var info=new MonitorInfo {Size=Marshal.SizeOf<MonitorInfo>()};
        if(!GetMonitorInfo(previous.Handle,ref info))return MonitorAtPointer();
        double scale=GetDpiForMonitor(previous.Handle,0,out var dpi,out _)==0?dpi/96d:1;
        return new CaptureMonitor(previous.Handle,info.Monitor,info.Work,scale);
    }

    internal static void Protect(Window window, bool nonactivating)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (nonactivating)
        {
            var style = GetWindowLongPtr(handle, GwlExStyle).ToInt64();
            SetWindowLongPtr(handle, GwlExStyle, new IntPtr(style | NoActivate | ToolWindow));
        }
        // Best effort only. Own windows are also hidden and the compositor flushed before capture.
        SetWindowDisplayAffinity(handle, 0x11);
    }

    internal static void Place(Window window, RectI rect, bool topmost = true)
    {
        SetWindowPos(new WindowInteropHelper(window).Handle, new IntPtr(topmost ? -1 : 0),
            rect.Left, rect.Top, rect.Width, rect.Height, SwpNoActivate);
    }
}

internal sealed record CaptureMonitor(IntPtr Handle, Native.RectI Bounds, Native.RectI Work, double Scale);
