using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32.SafeHandles;

namespace LeftShot.Services;

internal interface IDesktopCapture
{
    Task<BitmapSource> CaptureAsync(CaptureMonitor monitor, CancellationToken cancellation);
}

internal sealed class DesktopCapture : IDesktopCapture
{
    // One still frame per invocation; no idle capture thread or polling.
    public Task<BitmapSource> CaptureAsync(CaptureMonitor monitor, CancellationToken cancellation) =>
        Task.Run(() => Capture(monitor, cancellation), cancellation);

    private static BitmapSource Capture(CaptureMonitor monitor, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        Native.DwmFlush();
        cancellation.ThrowIfCancellationRequested();
        int width = monitor.Bounds.Width, height = monitor.Bounds.Height;
        if (width < 1 || height < 1 || (long)width * height > 32_000_000)
            throw new InvalidOperationException("This display exceeds the 32-megapixel capture limit.");
        var info = new BitmapInfo { Size = 40, Width = width, Height = -height, Planes = 1, BitCount = 32 };
        using var screen = new ScreenDc(GetDC(IntPtr.Zero));
        if (screen.IsInvalid) throw new Win32Exception();
        using var memory = new MemoryDc(CreateCompatibleDC(screen.DangerousGetHandle()));
        if (memory.IsInvalid) throw new Win32Exception();
        using var bitmap = new GdiObject(CreateDIBSection(screen.DangerousGetHandle(), ref info, 0, out var bits, IntPtr.Zero, 0));
        if (bitmap.IsInvalid) throw new Win32Exception();
        IntPtr previous = SelectObject(memory.DangerousGetHandle(), bitmap.DangerousGetHandle());
        if (previous == IntPtr.Zero || previous == new IntPtr(-1)) throw new Win32Exception();
        try
        {
            if (!BitBlt(memory.DangerousGetHandle(),0,0,width,height,screen.DangerousGetHandle(),monitor.Bounds.Left,monitor.Bounds.Top,0x40CC0020))
                throw new Win32Exception();
            cancellation.ThrowIfCancellationRequested();
            var pixels = new byte[checked(width * height * 4)];
            Marshal.Copy(bits,pixels,0,pixels.Length);
            bool anyVisible = false;
            for (int i=0;i<pixels.Length;i+=4)
            {
                anyVisible |= pixels[i] != 0 || pixels[i+1] != 0 || pixels[i+2] != 0;
                pixels[i+3] = 255;
            }
            if (!anyVisible) throw new InvalidOperationException("The display returned a black frame. Try windowed or borderless mode. Protected content cannot be captured.");
            cancellation.ThrowIfCancellationRequested();
            var image = BitmapSource.Create(width,height,96,96,PixelFormats.Bgra32,null,pixels,width*4);
            image.Freeze();
            return image;
        }
        finally { SelectObject(memory.DangerousGetHandle(), previous); }
    }

    // DCs and HBITMAPs have different release functions. Restoring the previous selection precedes disposal.
    private sealed class ScreenDc : SafeHandleZeroOrMinusOneIsInvalid
    { public ScreenDc(IntPtr value) : base(true) => SetHandle(value); protected override bool ReleaseHandle() => ReleaseDC(IntPtr.Zero, handle) != 0; }
    private sealed class MemoryDc : SafeHandleZeroOrMinusOneIsInvalid
    { public MemoryDc(IntPtr value) : base(true) => SetHandle(value); protected override bool ReleaseHandle() => DeleteDC(handle); }
    private sealed class GdiObject : SafeHandleZeroOrMinusOneIsInvalid
    { public GdiObject(IntPtr value) : base(true) => SetHandle(value); protected override bool ReleaseHandle() => DeleteObject(handle); }
    [StructLayout(LayoutKind.Sequential)] private struct BitmapInfo
    { public uint Size; public int Width, Height; public ushort Planes, BitCount; public uint Compression, SizeImage; public int XPels, YPels; public uint ClrUsed, ClrImportant; public uint Colors; }
    [DllImport("user32.dll")] private static extern IntPtr GetDC(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern int ReleaseDC(IntPtr hwnd,IntPtr dc);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr dc);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr obj);
    [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr dc,IntPtr obj);
    [DllImport("gdi32.dll")] private static extern IntPtr CreateDIBSection(IntPtr dc,ref BitmapInfo info,uint usage,out IntPtr bits,IntPtr section,uint offset);
    [DllImport("gdi32.dll", SetLastError=true)] private static extern bool BitBlt(IntPtr dest,int x,int y,int width,int height,IntPtr src,int sx,int sy,uint rop);
}
