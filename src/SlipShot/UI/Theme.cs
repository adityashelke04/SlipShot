using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Interop;
using SlipShot.Services;
using Microsoft.Win32;

namespace SlipShot.UI;

internal static partial class Theme
{
    public static bool MotionEnabled { get; private set; }
    internal static bool IsDark { get; private set; }
    public static Brush PanelBrush => Get("Panel");
    public static Brush BackgroundBrush => Get("Surface");
    public static Brush TextBrush => Get("Ink");
    public static Brush MutedBrush => Get("Muted");
    public static Brush BorderBrush => Get("Border");
    public static Brush AccentBrush => Get("Accent");
    public static Brush FocusBrush => Get("Focus");
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
        Set("Surface",dark?"#141413":"#FAF9F5"); Set("Panel",dark?"#20201E":"#FFFFFF");
        Set("Canvas",dark?"#10100F":"#E8E6DC"); Set("Selected",dark?"#35241F":"#F4E4DC");
        Set("Ink",dark?"#FAF9F5":"#141413"); Set("Muted",dark?"#B0AEA5":"#625F57");
        Set("Border",dark?"#3A3A36":"#D8D5CC"); Set("Accent","#D97757");
        Set("OnAccent","#141413");Set("Focus",dark?"#D97757":"#A04F35");
        Set("Hover",dark?"#30302C":"#EFEDE6"); Set("Pressed",dark?"#453027":"#EAD7CC");
        Set("BrandMark",dark?"#D97757":"#141413");
        Set("AccentHover","#E38B6D");Set("AccentPressed","#CB7053");
        Set("Error",dark?"#FFB4A5":"#A33225");
        Set("ControlBorder",dark?"#79776F":"#868279");
        if (SystemParameters.HighContrast)
        {
            Set("Surface",SystemColors.WindowColor.ToString());Set("Panel",SystemColors.WindowColor.ToString());
            Set("Ink",SystemColors.WindowTextColor.ToString());Set("Muted",SystemColors.WindowTextColor.ToString());
            Set("Border",SystemColors.HighlightColor.ToString());Set("Accent",SystemColors.HighlightColor.ToString());
            Set("Hover",SystemColors.ControlColor.ToString());Set("Pressed",SystemColors.ControlColor.ToString());
            Set("Canvas",SystemColors.WindowColor.ToString());Set("Selected",SystemColors.ControlColor.ToString());Set("OnAccent",SystemColors.HighlightTextColor.ToString());
            Set("Focus",SystemColors.HighlightColor.ToString());
            Set("BrandMark",SystemColors.WindowTextColor.ToString());Set("Error",SystemColors.WindowTextColor.ToString());
            Set("ControlBorder",SystemColors.WindowTextColor.ToString());
            Set("AccentHover",SystemColors.HighlightColor.ToString());Set("AccentPressed",SystemColors.HighlightColor.ToString());
        }
        IsDark=dark&&!SystemParameters.HighContrast;
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
                    <MultiTrigger><MultiTrigger.Conditions><Condition Property="Tag" Value="Primary"/><Condition Property="IsMouseOver" Value="True"/></MultiTrigger.Conditions><Setter TargetName="Chrome" Property="Background" Value="{DynamicResource AccentHover}"/></MultiTrigger>
                    <MultiTrigger><MultiTrigger.Conditions><Condition Property="Tag" Value="Primary"/><Condition Property="IsPressed" Value="True"/></MultiTrigger.Conditions><Setter TargetName="Chrome" Property="Background" Value="{DynamicResource AccentPressed}"/></MultiTrigger>
                    <Trigger Property="IsKeyboardFocused" Value="True"><Setter TargetName="Chrome" Property="BorderBrush" Value="{DynamicResource Focus}"/><Setter TargetName="Chrome" Property="BorderThickness" Value="2"/></Trigger>
                    <MultiTrigger><MultiTrigger.Conditions><Condition Property="Tag" Value="Primary"/><Condition Property="IsKeyboardFocused" Value="True"/></MultiTrigger.Conditions><Setter TargetName="Chrome" Property="BorderBrush" Value="{DynamicResource OnAccent}"/></MultiTrigger>
                    <Trigger Property="IsEnabled" Value="False"><Setter Property="Opacity" Value="0.45"/></Trigger>
                  </ControlTemplate.Triggers>
                </ControlTemplate></Setter.Value></Setter>
              </Style>
              <Style TargetType="TextBox">
                <Setter Property="Background" Value="{DynamicResource Panel}"/><Setter Property="Foreground" Value="{DynamicResource Ink}"/><Setter Property="CaretBrush" Value="{DynamicResource Ink}"/><Setter Property="BorderBrush" Value="{DynamicResource ControlBorder}"/><Setter Property="BorderThickness" Value="1"/><Setter Property="Padding" Value="8,6"/><Setter Property="MinHeight" Value="32"/>
                <Setter Property="SelectionBrush" Value="{DynamicResource Accent}"/><Setter Property="SelectionOpacity" Value="0.35"/>
                <Setter Property="Template"><Setter.Value><ControlTemplate TargetType="TextBox"><Grid><Border Background="{TemplateBinding Background}" BorderBrush="{TemplateBinding BorderBrush}" BorderThickness="{TemplateBinding BorderThickness}" CornerRadius="8"><ScrollViewer x:Name="PART_ContentHost" Margin="{TemplateBinding Padding}"/></Border><Border x:Name="Focus" BorderBrush="{DynamicResource Focus}" BorderThickness="0" CornerRadius="8" IsHitTestVisible="False"/></Grid><ControlTemplate.Triggers><Trigger Property="IsKeyboardFocusWithin" Value="True"><Setter TargetName="Focus" Property="BorderThickness" Value="2"/></Trigger><Trigger Property="IsEnabled" Value="False"><Setter Property="Opacity" Value="0.45"/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter>
              </Style>
              <Style TargetType="ComboBox">
                <Setter Property="Background" Value="{DynamicResource Panel}"/><Setter Property="Foreground" Value="{DynamicResource Ink}"/><Setter Property="BorderBrush" Value="{DynamicResource Border}"/><Setter Property="MinHeight" Value="32"/>
                <Setter Property="Template"><Setter.Value><ControlTemplate TargetType="ComboBox">
                  <Grid>
                    <ToggleButton Focusable="False" IsChecked="{Binding IsDropDownOpen, Mode=TwoWay, RelativeSource={RelativeSource TemplatedParent}}" ClickMode="Press">
                      <ToggleButton.Template><ControlTemplate TargetType="ToggleButton"><Border x:Name="Chrome" CornerRadius="8" Background="{DynamicResource Panel}" BorderBrush="{DynamicResource ControlBorder}" BorderThickness="1"><Grid><ContentPresenter Margin="8,4,22,4" Content="{Binding SelectionBoxItem, RelativeSource={RelativeSource AncestorType=ComboBox}}" ContentTemplate="{Binding SelectionBoxItemTemplate, RelativeSource={RelativeSource AncestorType=ComboBox}}" VerticalAlignment="Center"/><Path Data="M 0,0 L 4,4 L 8,0" Stroke="{DynamicResource Ink}" StrokeThickness="1" HorizontalAlignment="Right" VerticalAlignment="Center" Margin="0,0,8,0"/></Grid></Border><ControlTemplate.Triggers><Trigger Property="IsMouseOver" Value="True"><Setter TargetName="Chrome" Property="Background" Value="{DynamicResource Hover}"/></Trigger></ControlTemplate.Triggers></ControlTemplate></ToggleButton.Template>
                    </ToggleButton>
                    <Border x:Name="Focus" BorderBrush="{DynamicResource Focus}" BorderThickness="0" CornerRadius="6" IsHitTestVisible="False"/>
                    <Popup x:Name="PART_Popup" IsOpen="{TemplateBinding IsDropDownOpen}" Placement="Bottom" AllowsTransparency="True" Focusable="False"><Border Background="{DynamicResource Panel}" BorderBrush="{DynamicResource Border}" BorderThickness="1" Padding="4" MinWidth="{Binding ActualWidth, RelativeSource={RelativeSource TemplatedParent}}"><ScrollViewer MaxHeight="240"><ItemsPresenter KeyboardNavigation.DirectionalNavigation="Contained"/></ScrollViewer></Border></Popup>
                  </Grid>
                  <ControlTemplate.Triggers><Trigger Property="IsEnabled" Value="False"><Setter Property="Opacity" Value="0.45"/></Trigger><Trigger Property="IsKeyboardFocusWithin" Value="True"><Setter TargetName="Focus" Property="BorderThickness" Value="2"/></Trigger></ControlTemplate.Triggers>
                </ControlTemplate></Setter.Value></Setter>
              </Style>
              <Style TargetType="ComboBoxItem"><Setter Property="Foreground" Value="{DynamicResource Ink}"/><Setter Property="Padding" Value="8,6"/><Setter Property="Template"><Setter.Value><ControlTemplate TargetType="ComboBoxItem"><Border x:Name="Chrome" Background="Transparent" Padding="{TemplateBinding Padding}" CornerRadius="6"><ContentPresenter/></Border><ControlTemplate.Triggers><Trigger Property="IsSelected" Value="True"><Setter TargetName="Chrome" Property="Background" Value="{DynamicResource Selected}"/></Trigger><Trigger Property="IsHighlighted" Value="True"><Setter TargetName="Chrome" Property="Background" Value="{DynamicResource Hover}"/><Setter TargetName="Chrome" Property="BorderBrush" Value="{DynamicResource Focus}"/><Setter TargetName="Chrome" Property="BorderThickness" Value="1"/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter></Style>
              <Style TargetType="CheckBox">
                <Setter Property="Foreground" Value="{DynamicResource Ink}"/><Setter Property="Margin" Value="0,4,0,4"/><Setter Property="MinHeight" Value="32"/>
                <Setter Property="Template"><Setter.Value><ControlTemplate TargetType="CheckBox"><Grid Background="Transparent"><Grid.ColumnDefinitions><ColumnDefinition Width="24"/><ColumnDefinition Width="*"/></Grid.ColumnDefinitions><Border x:Name="Box" Width="18" Height="18" HorizontalAlignment="Left" VerticalAlignment="Center" CornerRadius="4" Background="{DynamicResource Panel}" BorderBrush="{DynamicResource ControlBorder}" BorderThickness="1"><Path x:Name="Tick" Data="M 3,8 L 6,11 L 13,4" Stroke="{DynamicResource OnAccent}" StrokeThickness="2" Visibility="Collapsed"/></Border><ContentPresenter Grid.Column="1" Margin="4,4,0,4" VerticalAlignment="Center" RecognizesAccessKey="True"/></Grid><ControlTemplate.Triggers><Trigger Property="IsChecked" Value="True"><Setter TargetName="Box" Property="Background" Value="{DynamicResource Accent}"/><Setter TargetName="Box" Property="BorderBrush" Value="{DynamicResource Focus}"/><Setter TargetName="Tick" Property="Visibility" Value="Visible"/></Trigger><Trigger Property="IsMouseOver" Value="True"><Setter TargetName="Box" Property="BorderBrush" Value="{DynamicResource Focus}"/><Setter TargetName="Box" Property="BorderThickness" Value="2"/></Trigger><Trigger Property="IsKeyboardFocused" Value="True"><Setter TargetName="Box" Property="BorderBrush" Value="{DynamicResource Ink}"/><Setter TargetName="Box" Property="BorderThickness" Value="2"/></Trigger><Trigger Property="IsEnabled" Value="False"><Setter Property="Opacity" Value="0.45"/></Trigger></ControlTemplate.Triggers></ControlTemplate></Setter.Value></Setter>
              </Style>
            </ResourceDictionary>
            """;
            foreach(var markup in new[]{styles,ControlStyles})
            {
                var resources=(ResourceDictionary)XamlReader.Parse(markup);
                foreach(var key in resources.Keys) r[key]=resources[key];
            }
        }
        // Resources can also be configured by an offscreen STA renderer; HWNDs belong to the app dispatcher.
        if(Application.Current.Dispatcher.CheckAccess())
            foreach(Window window in Application.Current.Windows) ApplyTitleBar(window);
    }
    public static void Apply(Window window)
    {
        window.SetResourceReference(Control.BackgroundProperty,"Surface"); window.SetResourceReference(Control.ForegroundProperty,"Ink");
        window.FontFamily=new FontFamily("Segoe UI"); window.FontSize=13; window.UseLayoutRounding=true;window.Icon=Branding.WindowIcon;
        window.SourceInitialized+=(_,_)=>ApplyTitleBar(window);
    }
    private static void ApplyTitleBar(Window window)
    {
        // Unsupported attributes leave native system chrome intact; never create an HWND just to style it.
        if(!OperatingSystem.IsWindowsVersionAtLeast(10,0,22000))return;
        var handle=new WindowInteropHelper(window).Handle;
        if(handle==IntPtr.Zero||window.WindowStyle==WindowStyle.None)return;
        int dark=IsDark?1:0;
        Native.DwmSetWindowAttribute(handle,20,ref dark,sizeof(int));
    }
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
