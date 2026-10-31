using System;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using NUnit.Framework;
using SlipShot.UI;
namespace SlipShot.Tests;
[TestFixture,Apartment(ApartmentState.STA),NonParallelizable]
public class BrandThemeTests
{
    [TestCase("Light")] [TestCase("Dark")]
    public void CheckedBoxUsesThemeAndUpdatesWithoutRecreatingControl(string appearance)
    {
        var application=Application.Current??new Application {ShutdownMode=ShutdownMode.OnExplicitShutdown};
        Theme.Configure(appearance,true);
        var check=new CheckBox {Content="Reduce motion",IsChecked=true,Resources=application.Resources};
        check.Measure(new Size(240,40));check.Arrange(new Rect(0,0,240,40));check.ApplyTemplate();
        var box=check.Template.FindName("Box",check) as Border;
        Assert.That(box,Is.Not.Null,"Checkbox should use the shared theme, not OS light chrome.");
        Assert.That(((SolidColorBrush)box!.Background).Color,Is.EqualTo(((SolidColorBrush)Theme.AccentBrush).Color));
        Theme.Configure(appearance=="Dark"?"Light":"Dark",true);
        var frame=new System.Windows.Threading.DispatcherFrame();
        System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle,new Action(()=>frame.Continue=false));
        System.Windows.Threading.Dispatcher.PushFrame(frame);
        Assert.That(((SolidColorBrush)check.Foreground).Color,Is.EqualTo(((SolidColorBrush)Theme.TextBrush).Color));
        // Remove this offscreen STA owner before NUnit proceeds to fixtures on another STA.
        check.Resources=new ResourceDictionary();
    }
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
        foreach(string token in new[]{"Accent","AccentHover","AccentPressed"})
            Assert.That(Contrast((Brush)application.Resources[token],Theme.OnAccentBrush),Is.GreaterThanOrEqualTo(4.5),token);
        Assert.That(Contrast(Theme.PanelBrush,(Brush)application.Resources["Error"]),Is.GreaterThanOrEqualTo(4.5));
        Assert.That(Contrast(Theme.PanelBrush,(Brush)application.Resources["ControlBorder"]),Is.GreaterThanOrEqualTo(3));
        Assert.That(Contrast(Theme.PanelBrush,(Brush)application.Resources["BrandMark"]),Is.GreaterThanOrEqualTo(3));
    }
    [TestCase("Light")] [TestCase("Dark")]
    public void PrimaryFocusUsesContrastingInnerBorderEvenWithReducedMotion(string appearance)
    {
        var application=Application.Current??new Application {ShutdownMode=ShutdownMode.OnExplicitShutdown};Theme.Configure(appearance,true);
        var button=Theme.Button("Save",()=>{},true);
        button.Measure(new Size(120,40));button.Arrange(new Rect(0,0,120,40));button.ApplyTemplate();
        var trigger=System.Linq.Enumerable.Single(System.Linq.Enumerable.OfType<MultiTrigger>(button.Template.Triggers),t=>
            System.Linq.Enumerable.Any(System.Linq.Enumerable.Cast<Condition>(t.Conditions),c=>c.Property==UIElement.IsKeyboardFocusedProperty));
        var setter=System.Linq.Enumerable.Single(System.Linq.Enumerable.OfType<Setter>(trigger.Setters),s=>s.Property==Border.BorderBrushProperty);
        var key=((DynamicResourceExtension)setter.Value).ResourceKey;
        Assert.That(Contrast(Theme.AccentBrush,(Brush)application.Resources[key]),Is.GreaterThanOrEqualTo(3));
        Assert.That(Theme.MotionEnabled,Is.False);
    }
    [Test]
    public void TrayMenuRefreshesColorsWithoutCreatingVisibleWindow()
    {
        _=Application.Current??new Application {ShutdownMode=ShutdownMode.OnExplicitShutdown};
        using var menu=new System.Windows.Forms.ContextMenuStrip();menu.Items.Add("Capture region");
        foreach(string mode in new[]{"Dark","Light"})
        {
            Theme.Configure(mode,true);Theme.Apply(menu);
            var color=((SolidColorBrush)Theme.PanelBrush).Color;
            Assert.That(menu.BackColor.R,Is.EqualTo(color.R));
            Assert.That(menu.Items[0].ForeColor,Is.EqualTo(menu.ForeColor));
            Assert.That(menu.Visible,Is.False);
        }
    }
    [TestCase(Orientation.Horizontal)] [TestCase(Orientation.Vertical)]
    public void ScrollbarRetainsRangeAndCorrectPageCommands(Orientation orientation)
    {
        var app=Application.Current??new Application {ShutdownMode=ShutdownMode.OnExplicitShutdown};Theme.Configure("Dark",true);
        var bar=new ScrollBar {Style=(Style)app.Resources[typeof(ScrollBar)],Orientation=orientation,Minimum=0,Maximum=100,Value=30,ViewportSize=25};
        var size=orientation==Orientation.Horizontal?new Size(300,14):new Size(14,300);
        bar.Measure(size);bar.Arrange(new Rect(size));bar.UpdateLayout();
        var track=(Track)bar.Template.FindName("PART_Track",bar);
        Assert.That(track.Value,Is.EqualTo(30));Assert.That(track.Maximum,Is.EqualTo(100));Assert.That(track.ViewportSize,Is.EqualTo(25));
        Assert.That(track.DecreaseRepeatButton.Command,Is.EqualTo(orientation==Orientation.Horizontal?ScrollBar.PageLeftCommand:ScrollBar.PageUpCommand));
        Assert.That(track.IncreaseRepeatButton.Command,Is.EqualTo(orientation==Orientation.Horizontal?ScrollBar.PageRightCommand:ScrollBar.PageDownCommand));
        Assert.That(orientation==Orientation.Horizontal?track.Thumb.ActualHeight:track.Thumb.ActualWidth,Is.LessThanOrEqualTo(14));
    }
    [TestCase("Light")] [TestCase("Dark")]
    public void AuxiliaryTemplatesLoadWithoutOpeningPopups(string mode)
    {
        var app=Application.Current??new Application {ShutdownMode=ShutdownMode.OnExplicitShutdown};Theme.Configure(mode,true);
        var tooltip=new ToolTip {Content="Open captures folder"};
        var menu=new ContextMenu();menu.Items.Add(new MenuItem {Header="Save a copy as…"});
        foreach(Control control in new Control[]{tooltip,menu,new TextBox {Text="Synthetic text"},new ComboBoxItem {Content="Dark",IsSelected=true}})
        {control.Style=(Style)app.Resources[control.GetType()];Assert.That(control.ApplyTemplate(),Is.True,control.GetType().Name);control.Measure(new Size(240,80));control.Arrange(new Rect(0,0,240,80));control.UpdateLayout();}
        Assert.That(menu.IsOpen,Is.False);Assert.That(tooltip.IsOpen,Is.False);
    }
    private static double Contrast(Brush a,Brush b)
    {double x=Luminance(((SolidColorBrush)a).Color),y=Luminance(((SolidColorBrush)b).Color);return (Math.Max(x,y)+.05)/(Math.Min(x,y)+.05);}
    private static double Luminance(Color c)
    {double Linear(byte channel){double v=channel/255d;return v<=.04045?v/12.92:Math.Pow((v+.055)/1.055,2.4);}return .2126*Linear(c.R)+.7152*Linear(c.G)+.0722*Linear(c.B);}
}
