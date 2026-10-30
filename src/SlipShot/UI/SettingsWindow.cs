using System;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using SlipShot.Services;

namespace SlipShot.UI;
internal sealed class SettingsWindow : Window
{
    internal SettingsWindow(AppSettings initial, Func<AppSettings,string?> apply)
    {
        Title="SlipShot settings";Width=460;Height=Math.Min(720,SystemParameters.WorkArea.Height-48);MinWidth=360;MinHeight=400;
        Theme.Apply(this);
        var body=new StackPanel { Margin=new Thickness(24,24,24,8) };
        var layout=new DockPanel();layout.SetResourceReference(Panel.BackgroundProperty,"Surface");
        var footer=new StackPanel {Margin=new Thickness(24,0,24,16)};DockPanel.SetDock(footer,Dock.Bottom);layout.Children.Add(footer);
        layout.Children.Add(new ScrollViewer { Content=body,VerticalScrollBarVisibility=ScrollBarVisibility.Auto });Content=layout;
        body.Children.Add(Theme.Text("Make it yours",24));
        body.Children.Add(Theme.Text("Local preferences. No accounts, uploads or automatic screenshot saving.",13,true));
        TextBox Field(string name,string value)
        { var label=Theme.Text(name);label.Margin=new Thickness(0,16,0,6);body.Children.Add(label);var box=new TextBox {Text=value};AutomationProperties.SetName(box,name);body.Children.Add(box);return box; }
        var hotkey=Field("Capture hotkey",initial.Hotkey);
        body.Children.Add(Theme.Text("Use Ctrl / Shift / Alt + a letter. Function keys also work. Existing bindings are never replaced.",12,true));
        var appearanceLabel=Theme.Text("Appearance");appearanceLabel.Margin=new Thickness(0,16,0,0);body.Children.Add(appearanceLabel);
        var appearance=new ComboBox {ItemsSource=new[]{"System","Light","Dark"},SelectedItem=initial.Appearance,MinHeight=34,Margin=new Thickness(0,6,0,8)};AutomationProperties.SetName(appearance,"Appearance");body.Children.Add(appearance);
        var timeout=Field("Preview duration (2–60 seconds)",initial.PreviewSeconds.ToString());
        body.Children.Add(Theme.Text("Default: 10 seconds. Hover adds up to twice this duration; dragging stays protected. Latest capture remains in the tray.",12,true));
        var motion=new CheckBox {Content="Reduce motion",IsChecked=initial.ReducedMotion};body.Children.Add(motion);
        var copy=new CheckBox {Content="Copy automatically after capture",IsChecked=initial.AutoCopy};body.Children.Add(copy);
        var folder=Field("Preferred save folder (optional)",initial.SaveDirectory??"");
        body.Children.Add(Theme.Text("Only used when you choose Save. No screenshot history is written.",12,true));
        var quality=Field("JPEG quality (1–100)",initial.JpegQuality.ToString());
        var background=Field("JPEG opaque background (#RRGGBB)",initial.JpegBackground);
        var error=Theme.Text("",13);error.Margin=new Thickness(0,8,0,8);AutomationProperties.SetLiveSetting(error,AutomationLiveSetting.Polite);footer.Children.Add(error);
        footer.Children.Add(Theme.Button("Save settings",()=>
        {
            if(!Hotkeys.TryParse(hotkey.Text,out _,out _)) {error.Text="Choose modifiers plus a letter, for example Ctrl+Alt+S.";return;}
            if(!int.TryParse(timeout.Text,out int seconds)||seconds<2||seconds>60) {error.Text="Preview duration must be 2–60 seconds.";return;}
            if(!int.TryParse(quality.Text,out int jpeg)||jpeg<1||jpeg>100) {error.Text="JPEG quality must be 1–100.";return;}
            if(background.Text.Length!=7 || background.Text[0]!='#' || !int.TryParse(background.Text.AsSpan(1),System.Globalization.NumberStyles.HexNumber,null,out _)) {error.Text="Choose an opaque background as #RRGGBB.";return;}
            if(!string.IsNullOrWhiteSpace(folder.Text)&&!Directory.Exists(folder.Text)) {error.Text="That folder is unavailable. Choose an existing folder or leave it blank.";return;}
            var changed=initial with {Hotkey=hotkey.Text,Appearance=(string)appearance.SelectedItem,PreviewSeconds=seconds,ReducedMotion=motion.IsChecked==true,AutoCopy=copy.IsChecked==true,SaveDirectory=string.IsNullOrWhiteSpace(folder.Text)?null:folder.Text,JpegQuality=jpeg,JpegBackground=background.Text};
            string? failure=apply(changed);if(failure is null) Close();else error.Text=failure;
        },true));
        SourceInitialized+=(_,_)=>Native.Protect(this,false);
    }
}
