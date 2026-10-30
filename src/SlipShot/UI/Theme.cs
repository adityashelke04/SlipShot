using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Win32;

namespace SlipShot.UI;

internal static class Theme
{
    public static bool MotionEnabled { get; private set; }
    public static Brush PanelBrush => Get("Panel");
    public static Brush BackgroundBrush => Get("Surface");
    public static Brush TextBrush => Get("Ink");
    public static Brush MutedBrush => Get("Muted");
    public static Brush BorderBrush => Get("Border");
    public static Brush AccentBrush => Get("Accent");
    public static Brush OnAccentBrush => Get("OnAccent");
    public static Brush CanvasBrush => Get("Canvas");
    private static Brush Get(string key) => (Brush)Application.Current.Resources[key];

    public static void Configure(string appearance, bool reducedMotion)
    {
        bool dark = appearance == "Dark";
        if (appearance == "System")
        {
            try { dark = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1) is int value && value == 0; }
            catch (System.Security.SecurityException) { dark = false; }
        }
        var r = Application.Current.Resources;
        void Set(string key,string value) { var color=(Color)ColorConverter.ConvertFromString(value); if(r[key] is SolidColorBrush existing && !existing.IsFrozen) existing.Color=color; else r[key]=new SolidColorBrush(color); }
        Set("Surface",dark?"#191C20":"#F7F8FA"); Set("Panel",dark?"#22262B":"#FFFFFF");
        Set("Canvas",dark?"#101316":"#E9EDF0"); Set("Selected",dark?"#2A403B":"#E2F2EB");
        Set("Ink",dark?"#F0F3F5":"#1E272C"); Set("Muted",dark?"#A3ADB5":"#5F6C75");
        Set("Border",dark?"#363D43":"#DDE3E7"); Set("Accent",dark?"#9DE4C7":"#197456");
        Set("OnAccent",dark?"#142C23":"#FFFFFF");
        Set("Hover",dark?"#30373D":"#EDF2F4"); Set("Pressed",dark?"#384941":"#DDEBE4");
        if (SystemParameters.HighContrast)
        {
            Set("Surface",SystemColors.WindowColor.ToString());Set("Panel",SystemColors.WindowColor.ToString());
            Set("Ink",SystemColors.WindowTextColor.ToString());Set("Muted",SystemColors.WindowTextColor.ToString());
            Set("Border",SystemColors.HighlightColor.ToString());Set("Accent",SystemColors.HighlightColor.ToString());
            Set("Hover",SystemColors.ControlColor.ToString());Set("Pressed",SystemColors.ControlColor.ToString());
            Set("Canvas",SystemColors.WindowColor.ToString());Set("Selected",SystemColors.ControlColor.ToString());Set("OnAccent",SystemColors.HighlightTextColor.ToString());
        }
        MotionEnabled = !reducedMotion && SystemParameters.ClientAreaAnimation && !SystemParameters.HighContrast;
        if (!r.Contains(typeof(Button)))
        {
            const string styles = """
            <ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
              <Style TargetType="Window"><Setter Property="FontFamily" Value="Segoe UI"/><Setter Property="FontSize" Value="13"/><Setter Property="Background" Value="{DynamicResource Surface}"/><Setter Property="Foreground" Value="{DynamicResource Ink}"/><Setter Property="UseLayoutRounding" Value="True"/><Setter Property="SnapsToDevicePixels" Value="True"/></Style>
              <Style TargetType="TextBlock"><Setter Property="Foreground" Value="{DynamicResource Ink}"/></Style>
              <Style TargetType="Button">
                <Setter Property="Foreground" Value="{DynamicResource Ink}"/><Setter Property="Background" Value="Transparent"/>
                <Setter Property="BorderBrush" Value="Transparent"/><Setter Property="BorderThickness" Value="1"/>
                <Setter Property="Padding" Value="12,7"/><Setter Property="Margin" Value="0,0,4,4"/><Setter Property="MinHeight" Value="34"/>
                <Setter Property="Template"><Setter.Value><ControlTemplate TargetType="Button">
                  <Border x:Name="Chrome" Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}" CornerRadius="8" Padding="{TemplateBinding Padding}">
                    <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center" RecognizesAccessKey="True"/>
                  </Border>
                  <ControlTemplate.Triggers>
                    <Trigger Property="IsMouseOver" Value="True"><Setter TargetName="Chrome" Property="Background" Value="{DynamicResource Hover}"/></Trigger>
                    <Trigger Property="IsPressed" Value="True"><Setter TargetName="Chrome" Property="Background" Value="{DynamicResource Pressed}"/></Trigger>
                    <Trigger Property="Tag" Value="Primary"><Setter TargetName="Chrome" Property="Background" Value="{DynamicResource Accent}"/><Setter Property="Foreground" Value="{DynamicResource OnAccent}"/></Trigger>
                    <Trigger Property="IsKeyboardFocused" Value="True"><Setter TargetName="Chrome" Property="BorderBrush" Value="{DynamicResource Accent}"/><Setter TargetName="Chrome" Property="BorderThickness" Value="2"/></Trigger>
                    <Trigger Property="IsEnabled" Value="False"><Setter Property="Opacity" Value="0.45"/></Trigger>
                  </ControlTemplate.Triggers>
                </ControlTemplate></Setter.Value></Setter>
              </Style>
              <Style TargetType="TextBox"><Setter Property="Background" Value="{DynamicResource Panel}"/><Setter Property="Foreground" Value="{DynamicResource Ink}"/><Setter Property="CaretBrush" Value="{DynamicResource Ink}"/><Setter Property="BorderBrush" Value="{DynamicResource Border}"/><Setter Property="Padding" Value="8,6"/><Setter Property="MinHeight" Value="32"/></Style>
              <Style TargetType="ComboBox">
                <Setter Property="Background" Value="{DynamicResource Panel}"/><Setter Property="Foreground" Value="{DynamicResource Ink}"/><Setter Property="BorderBrush" Value="{DynamicResource Border}"/><Setter Property="MinHeight" Value="32"/>
                <Setter Property="Template"><Setter.Value><ControlTemplate TargetType="ComboBox">
                  <Grid>
                    <ToggleButton Focusable="False" IsChecked="{Binding IsDropDownOpen, Mode=TwoWay, RelativeSource={RelativeSource TemplatedParent}}" ClickMode="Press">
                      <ToggleButton.Template><ControlTemplate TargetType="ToggleButton"><Border x:Name="Chrome" CornerRadius="6" Background="{DynamicResource Panel}" BorderBrush="{DynamicResource Border}" BorderThickness="1"><Grid><ContentPresenter Margin="8,4,22,4" Content="{Binding SelectionBoxItem, RelativeSource={RelativeSource AncestorType=ComboBox}}" ContentTemplate="{Binding SelectionBoxItemTemplate, RelativeSource={RelativeSource AncestorType=ComboBox}}" VerticalAlignment="Center"/><Path Data="M 0,0 L 4,4 L 8,0" Stroke="{DynamicResource Ink}" StrokeThickness="1" HorizontalAlignment="Right" VerticalAlignment="Center" Margin="0,0,8,0"/></Grid></Border><ControlTemplate.Triggers><Trigger Property="IsMouseOver" Value="True"><Setter TargetName="Chrome" Property="Background" Value="{DynamicResource Hover}"/></Trigger></ControlTemplate.Triggers></ControlTemplate></ToggleButton.Template>
                    </ToggleButton>
                    <Border x:Name="Focus" BorderBrush="{DynamicResource Accent}" BorderThickness="0" CornerRadius="6" IsHitTestVisible="False"/>
                    <Popup x:Name="PART_Popup" IsOpen="{TemplateBinding IsDropDownOpen}" Placement="Bottom" AllowsTransparency="True" Focusable="False"><Border Background="{DynamicResource Panel}" BorderBrush="{DynamicResource Border}" BorderThickness="1" Padding="4" MinWidth="{Binding ActualWidth, RelativeSource={RelativeSource TemplatedParent}}"><ScrollViewer MaxHeight="240"><ItemsPresenter KeyboardNavigation.DirectionalNavigation="Contained"/></ScrollViewer></Border></Popup>
                  </Grid>
                  <ControlTemplate.Triggers><Trigger Property="IsEnabled" Value="False"><Setter Property="Opacity" Value="0.45"/></Trigger><Trigger Property="IsKeyboardFocusWithin" Value="True"><Setter TargetName="Focus" Property="BorderThickness" Value="2"/></Trigger></ControlTemplate.Triggers>
                </ControlTemplate></Setter.Value></Setter>
              </Style>
              <Style TargetType="ComboBoxItem"><Setter Property="Foreground" Value="{DynamicResource Ink}"/><Setter Property="Background" Value="{DynamicResource Panel}"/><Setter Property="Padding" Value="8,6"/><Style.Triggers><Trigger Property="IsHighlighted" Value="True"><Setter Property="Background" Value="{DynamicResource Hover}"/></Trigger></Style.Triggers></Style>
              <Style TargetType="CheckBox"><Setter Property="Foreground" Value="{DynamicResource Ink}"/><Setter Property="Margin" Value="0,8,0,8"/></Style>
            </ResourceDictionary>
            """;
            var resources=(ResourceDictionary)XamlReader.Parse(styles);
            foreach(var key in resources.Keys) r[key]=resources[key];
        }
    }
    public static void Apply(Window window) { window.SetResourceReference(Control.BackgroundProperty,"Surface"); window.SetResourceReference(Control.ForegroundProperty,"Ink"); window.FontFamily=new FontFamily("Segoe UI"); window.FontSize=13; window.UseLayoutRounding=true; }
    public static TextBlock Text(string text,double size=13,bool muted=false)
    { var t=new TextBlock { Text=text,FontSize=size,TextWrapping=TextWrapping.Wrap }; t.SetResourceReference(TextBlock.ForegroundProperty,muted?"Muted":"Ink"); return t; }
    public static Button Button(string text,Action action,bool primary=false)
    {
        var b=new Button { Content=text,ToolTip=text };
        AutomationProperties.SetName(b,text.Replace("_",""));
        if(primary) {b.Tag="Primary";b.SetResourceReference(Control.BackgroundProperty,"Accent");b.SetResourceReference(Control.ForegroundProperty,"OnAccent");b.FontWeight=FontWeights.SemiBold;var label=new TextBlock {Text=text};label.SetResourceReference(TextBlock.ForegroundProperty,"OnAccent");b.Content=label;}
        b.MouseEnter+=(_,_)=> {if(MotionEnabled&&b.IsEnabled)b.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(.88,1,TimeSpan.FromMilliseconds(100)));};
        b.PreviewMouseLeftButtonDown+=(_,_)=> {if(MotionEnabled&&b.IsEnabled)b.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(.8,1,TimeSpan.FromMilliseconds(80)));};
        b.IsEnabledChanged+=(_,_)=>b.BeginAnimation(UIElement.OpacityProperty,null);
        b.Click+=(_,_)=>action();return b;
    }
    public static FrameworkElement Glyph(string path,double size=16)
    {
        var glyph=new System.Windows.Shapes.Path {Data=Geometry.Parse(path),StrokeThickness=1.65,StrokeStartLineCap=PenLineCap.Round,StrokeEndLineCap=PenLineCap.Round,StrokeLineJoin=PenLineJoin.Round,Stretch=Stretch.Uniform,Width=size,Height=size,IsHitTestVisible=false};
        glyph.SetBinding(System.Windows.Shapes.Shape.StrokeProperty,new System.Windows.Data.Binding("Foreground") {RelativeSource=new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.FindAncestor,typeof(Control),1)});
        return glyph;
    }
    public static Button IconButton(string tooltip,string path,Action action,bool primary=false,double size=36)
    {var button=Button(tooltip,action,primary);button.Content=Glyph(path);button.Width=size;button.Height=size;button.MinHeight=size;button.MinWidth=0;button.Padding=new Thickness(Math.Max(0,(size-18)/2));button.Margin=new Thickness(2);return button;}
    public static void AnimateEntrance(Window window)
    { if(MotionEnabled) window.BeginAnimation(UIElement.OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(180))); }
}
