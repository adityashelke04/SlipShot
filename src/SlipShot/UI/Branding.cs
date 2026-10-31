using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SlipShot.UI;

internal static class Branding
{
    private const string IconResource = "SlipShot.Assets.slipshot.ico";
    private static readonly Lazy<BitmapSource> CachedWindowIcon = new(LoadWindowIcon);

    internal static BitmapSource WindowIcon => CachedWindowIcon.Value;

    // Same two paths as the original Frame-S master; vector UI marks stay crisp at any DPI.
    internal static FrameworkElement Mark(double size)
    {
        var mark=new System.Windows.Shapes.Path
        {
            Data=Geometry.Parse("M14 4 H52 Q54 4 54 6 V15 Q54 17 52 17 H20 Q18 17 18 19 V28 L27 35 Q29 38 26 38 H12 Q4 38 4 30 V14 Q4 4 14 4 Z M50 60 H12 Q10 60 10 58 V49 Q10 47 12 47 H44 Q46 47 46 45 V36 L37 29 Q35 26 38 26 H52 Q60 26 60 34 V50 Q60 60 50 60 Z"),
            Width=size,Height=size,Stretch=Stretch.Uniform,IsHitTestVisible=false,
            VerticalAlignment=VerticalAlignment.Center
        };
        mark.SetResourceReference(System.Windows.Shapes.Shape.FillProperty,"BrandMark");
        return mark;
    }

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
