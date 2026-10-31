using System;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using NUnit.Framework;
using SlipShot.UI;
namespace SlipShot.Tests;
[TestFixture,Apartment(ApartmentState.STA),NonParallelizable]
public class BrandThemeTests
{
    [TestCase("Light")] [TestCase("Dark")]
    public void RequestedAccentRemainsExactWithReadableTextAndFocus(string appearance)
    {
        if(SystemParameters.HighContrast)Assert.Ignore("System high contrast intentionally overrides the brand palette.");
        var application=Application.Current??new Application {ShutdownMode=ShutdownMode.OnExplicitShutdown};Theme.Configure(appearance,true);
        var accent=((SolidColorBrush)Theme.AccentBrush).Color;
        Assert.That(accent,Is.EqualTo(Color.FromRgb(0xD9,0x77,0x57)));
        Assert.That(((SolidColorBrush)Theme.OnAccentBrush).Color,Is.EqualTo(Color.FromRgb(0x14,0x14,0x13)));
        Assert.That(Contrast(Theme.AccentBrush,Theme.OnAccentBrush),Is.GreaterThanOrEqualTo(4.5));
        Assert.That(Contrast(Theme.PanelBrush,Theme.TextBrush),Is.GreaterThanOrEqualTo(4.5));
        Assert.That(Contrast(Theme.PanelBrush,Theme.MutedBrush),Is.GreaterThanOrEqualTo(4.5));
        Assert.That(Contrast(Theme.PanelBrush,Theme.FocusBrush),Is.GreaterThanOrEqualTo(3));
        Assert.That(Contrast((Brush)application.Resources["Selected"],Theme.FocusBrush),Is.GreaterThanOrEqualTo(3));
    }
    private static double Contrast(Brush a,Brush b)
    {double x=Luminance(((SolidColorBrush)a).Color),y=Luminance(((SolidColorBrush)b).Color);return (Math.Max(x,y)+.05)/(Math.Min(x,y)+.05);}
    private static double Luminance(Color c)
    {double Linear(byte channel){double v=channel/255d;return v<=.04045?v/12.92:Math.Pow((v+.055)/1.055,2.4);}return .2126*Linear(c.R)+.7152*Linear(c.G)+.0722*Linear(c.B);}
}
