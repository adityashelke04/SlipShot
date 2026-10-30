using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SlipShot.Services;

public interface IClipboardSink
{
    void SetImage(BitmapSource image);
}

public sealed class WindowsClipboardSink : IClipboardSink
{
    public void SetImage(BitmapSource image)
    {
        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            throw new InvalidOperationException("Clipboard access requires the application's UI thread.");
        Clipboard.SetDataObject(ClipboardPayload.Create(image), copy: true);
    }
}

public static class ClipboardPayload
{
    public static DataObject Create(BitmapSource flattened)
    {
        ArgumentNullException.ThrowIfNull(flattened);
        int stride = checked(flattened.PixelWidth * 4);
        var pixels = new byte[checked(stride * flattened.PixelHeight)];
        new FormatConvertedBitmap(flattened, PixelFormats.Bgra32, null, 0).CopyPixels(pixels, stride, 0);
        // Detach only flattened pixels so neither format retains source metadata or a thumbnail.
        BitmapSource bitmap = BitmapSource.Create(flattened.PixelWidth, flattened.PixelHeight,
            96, 96, PixelFormats.Bgra32, null, pixels, stride);
        bitmap.Freeze();
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var encoded = new MemoryStream();
        encoder.Save(encoded);
        var data = new DataObject();
        data.SetImage(bitmap);
        data.SetData("PNG", new MemoryStream(encoded.ToArray(), writable: false), autoConvert: false);
        return data;
    }
}

public static class ClipboardWriter
{
    public static async Task CopyAsync(BitmapSource image, IClipboardSink sink, CancellationToken cancellation = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(sink);
        if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
            throw new InvalidOperationException("Copy must start on the application's UI thread.");
        Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
        for (int attempt = 0; attempt < 4; attempt++)
        {
            cancellation.ThrowIfCancellationRequested();
            try
            {
                if (dispatcher.CheckAccess()) sink.SetImage(image);
                else await dispatcher.InvokeAsync(() => { cancellation.ThrowIfCancellationRequested(); sink.SetImage(image); }, DispatcherPriority.Normal, cancellation).Task.ConfigureAwait(false);
                return;
            }
            catch (ExternalException error) when (error.ErrorCode == unchecked((int)0x800401D0))
            {
                if (attempt == 3) throw new InvalidOperationException("The clipboard is busy. Close the app using it and try Copy again.", error);
                await Task.Delay(80 * (attempt + 1), cancellation).ConfigureAwait(false);
            }
        }
    }
}
