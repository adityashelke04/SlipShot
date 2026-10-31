using System;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using SlipShot.Services;

namespace SlipShot.UI;
internal sealed class SettingsWindow : Window
{
    internal SettingsWindow(AppSettings initial, Func<AppSettings,string?> apply)
    {
        Title="SlipShot · Settings";Width=460;Height=Math.Min(720,SystemParameters.WorkArea.Height-48);MinWidth=360;MinHeight=400;
        Theme.Apply(this);
        var body=new StackPanel { Margin=new Thickness(24,24,24,24) };
        var layout=new DockPanel();layout.SetResourceReference(Panel.BackgroundProperty,"Surface");
        var footer=new StackPanel();
        var footerBorder=new Border {Child=footer,Padding=new Thickness(24,12,24,16),BorderThickness=new Thickness(0,1,0,0)};
        footerBorder.SetResourceReference(Border.BackgroundProperty,"Panel");footerBorder.SetResourceReference(Border.BorderBrushProperty,"Border");
        DockPanel.SetDock(footerBorder,Dock.Bottom);layout.Children.Add(footerBorder);
        var scroll=new ScrollViewer { Content=body,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled };
        KeyboardNavigation.SetTabIndex(scroll,0);KeyboardNavigation.SetTabNavigation(scroll,KeyboardNavigationMode.Local);
        KeyboardNavigation.SetTabIndex(footerBorder,1);KeyboardNavigation.SetTabNavigation(footerBorder,KeyboardNavigationMode.Local);
        layout.Children.Add(scroll);Content=layout;
        var heading=new StackPanel {Orientation=Orientation.Horizontal,Margin=new Thickness(0,0,0,8)};
        var mark=Branding.Mark(24);mark.Margin=new Thickness(0,0,12,0);mark.VerticalAlignment=VerticalAlignment.Center;heading.Children.Add(mark);
        var title=Theme.Text("Settings",24);title.FontWeight=FontWeights.SemiBold;heading.Children.Add(title);body.Children.Add(heading);
        body.Children.Add(Theme.Text("Your preferences stay on this PC.",13,true));
        void Section(string name)
        {var label=Theme.Text(name,15);label.FontWeight=FontWeights.SemiBold;label.Margin=new Thickness(0,24,0,8);body.Children.Add(label);}
        void Hint(string text)
        {var hint=Theme.Text(text,12,true);hint.Margin=new Thickness(0,6,0,0);body.Children.Add(hint);}
        TextBox Field(string name,string value,StackPanel? parent=null)
        {
            parent??=body;var label=Theme.Text(name);label.Margin=new Thickness(0,8,0,6);parent.Children.Add(label);
            var box=new TextBox {Text=value};AutomationProperties.SetName(box,name);AutomationProperties.SetLabeledBy(box,label);parent.Children.Add(box);return box;
        }
        CheckBox Toggle(string name,bool value)
        {
            var box=new CheckBox {Content=Theme.Text(name),IsChecked=value};AutomationProperties.SetName(box,name);body.Children.Add(box);return box;
        }
        Section("Appearance");
        var appearanceLabel=Theme.Text("Color theme");appearanceLabel.Margin=new Thickness(0,0,0,6);body.Children.Add(appearanceLabel);
        var appearance=new ComboBox {ItemsSource=new[]{"System","Light","Dark"},SelectedItem=initial.Appearance,MinHeight=34};
        AutomationProperties.SetName(appearance,"Appearance");AutomationProperties.SetLabeledBy(appearance,appearanceLabel);body.Children.Add(appearance);
        Hint("System follows your Windows app theme.");
        var motion=Toggle("Reduce motion",initial.ReducedMotion);
        Section("Capture");
        var hotkey=Field("Capture hotkey",initial.Hotkey);
        Hint("Use Ctrl / Shift / Alt + a letter, such as Ctrl+Alt+S. Existing bindings are never replaced.");
        var timeout=Field("Preview duration (2–60 seconds)",initial.PreviewSeconds.ToString());
        Hint("Default: 10 seconds. Hover adds up to twice this duration; dragging stays protected. Latest capture remains in the tray.");
        var copy=Toggle("Copy automatically after capture",initial.AutoCopy);
        Section("Saving & export");
        var folder=Field("Quick-save folder",initial.SaveDirectory??ImageFiles.DefaultSaveDirectory);
        Hint("Save writes a PNG here once per edit. Nothing is saved automatically. Save a copy as… in the editor lets you choose PNG or JPEG.");
        var jpegFields=new Grid();jpegFields.ColumnDefinitions.Add(new ColumnDefinition());jpegFields.ColumnDefinitions.Add(new ColumnDefinition());body.Children.Add(jpegFields);
        var qualityColumn=new StackPanel {Margin=new Thickness(0,8,8,0)};jpegFields.Children.Add(qualityColumn);
        var backgroundColumn=new StackPanel {Margin=new Thickness(8,8,0,0)};Grid.SetColumn(backgroundColumn,1);jpegFields.Children.Add(backgroundColumn);
        var quality=Field("JPEG quality (1–100)",initial.JpegQuality.ToString(),qualityColumn);
        var background=Field("JPEG background",initial.JpegBackground,backgroundColumn);
        Hint("JPEG uses an opaque background. Enter a color as #RRGGBB.");
        var error=Theme.Text("",13);error.Margin=new Thickness(0,0,0,8);error.Visibility=Visibility.Collapsed;error.SetResourceReference(TextBlock.ForegroundProperty,"Error");
        AutomationProperties.SetLiveSetting(error,AutomationLiveSetting.Polite);footer.Children.Add(error);
        void Failure(string message,Control? field=null)
        {error.Text=message;error.Visibility=Visibility.Visible;if(field is not null){field.BringIntoView();field.Focus();}}
        var actions=new StackPanel {Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right};footer.Children.Add(actions);
        var cancel=Theme.Button("Cancel",Close);cancel.IsCancel=true;actions.Children.Add(cancel);
        var save=Theme.Button("Save settings",()=>
        {
            if(!Hotkeys.TryParse(hotkey.Text,out _,out _)) {Failure("Choose modifiers plus a letter, for example Ctrl+Alt+S.",hotkey);return;}
            if(!int.TryParse(timeout.Text,out int seconds)||seconds<2||seconds>60) {Failure("Preview duration must be 2–60 seconds.",timeout);return;}
            if(!int.TryParse(quality.Text,out int jpeg)||jpeg<1||jpeg>100) {Failure("JPEG quality must be 1–100.",quality);return;}
            if(background.Text.Length!=7 || background.Text[0]!='#' || !int.TryParse(background.Text.AsSpan(1),System.Globalization.NumberStyles.AllowHexSpecifier,null,out _)) {Failure("Choose an opaque background as #RRGGBB.",background);return;}
            if(!string.IsNullOrWhiteSpace(folder.Text))
            {
                try {if(!Path.IsPathFullyQualified(folder.Text)||folder.Text.IndexOfAny(Path.GetInvalidPathChars())>=0)throw new ArgumentException();Path.GetFullPath(folder.Text);}
                catch(Exception ex) when(ex is ArgumentException or NotSupportedException or PathTooLongException){Failure("Enter a full folder path, such as "+ImageFiles.DefaultSaveDirectory,folder);return;}
            }
            var changed=initial with {Hotkey=hotkey.Text,Appearance=(string)appearance.SelectedItem,PreviewSeconds=seconds,ReducedMotion=motion.IsChecked==true,AutoCopy=copy.IsChecked==true,SaveDirectory=string.IsNullOrWhiteSpace(folder.Text)?null:folder.Text,JpegQuality=jpeg,JpegBackground=background.Text};
            string? failure=apply(changed);if(failure is null) Close();else Failure(failure);
        },true);save.Margin=new Thickness(4,0,0,0);actions.Children.Add(save);
        SourceInitialized+=(_,_)=>Native.Protect(this,false);
    }
}
