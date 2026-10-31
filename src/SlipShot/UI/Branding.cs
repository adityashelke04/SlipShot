using System;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;

namespace SlipShot.UI;

internal static class Branding
{
    private const string IconResource = "SlipShot.Assets.slipshot.ico";
    private static readonly Lazy<BitmapSource> CachedWindowIcon = new(LoadWindowIcon);

    internal static BitmapSource WindowIcon => CachedWindowIcon.Value;

    internal static System.Drawing.Icon CreateTrayIcon()
    {
        using var stream = OpenIcon();
        using var icon = new System.Drawing.Icon(stream, 32, 32);
        return (System.Drawing.Icon)icon.Clone();
    }

    private static BitmapSource LoadWindowIcon()
    {
        using var stream = OpenIcon();
        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        var image = decoder.Frames.OrderByDescending(frame => frame.PixelWidth * frame.PixelHeight).First();
        image.Freeze();
        return image;
    }

    private static Stream OpenIcon() => typeof(Branding).Assembly.GetManifestResourceStream(IconResource)
        ?? throw new InvalidOperationException("The embedded SlipShot icon is missing.");
}
