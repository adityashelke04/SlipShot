using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using LeftShot.Services;
using LeftShot.Core;

namespace LeftShot.UI;

internal sealed class PreviewWindow : Window
{
    private readonly DispatcherTimer timer;
    private readonly CaptureMonitor monitor;
    private readonly TranslateTransform translation=new();
    private readonly PreviewLifetime lifetime;
    private readonly Stopwatch clock=new();
    internal PreviewWindow(BitmapSource image,CaptureMonitor monitor,int seconds,Action edit,Action copy,Action save,Action pin)
    {
        this.monitor=monitor; lifetime=new PreviewLifetime(TimeSpan.FromSeconds(seconds));
        timer=new DispatcherTimer { Interval=TimeSpan.FromMilliseconds(200) };timer.Tick+=Tick;
        Title="LeftShot capture preview"; ShowActivated=false; ShowInTaskbar=false; Topmost=true;
        WindowStyle=WindowStyle.None; ResizeMode=ResizeMode.NoResize; SizeToContent=SizeToContent.Height;
        Width=Math.Max(180,Math.Min(312,monitor.Work.Width/monitor.Scale-32));
        MaxHeight=Math.Max(120,monitor.Work.Height/monitor.Scale-32);
        Theme.Apply(this);
        var stack=new StackPanel { Margin=new Thickness(12) };
        var heading=new DockPanel(); var close=Theme.Button("Close",Dismiss); close.HorizontalAlignment=HorizontalAlignment.Right; DockPanel.SetDock(close,Dock.Right); heading.Children.Add(close); heading.Children.Add(Theme.Text("Captured",16)); stack.Children.Add(heading);
        var thumbnail=new Image { Source=image,Height=140,Stretch=Stretch.Uniform,Margin=new Thickness(0,8,0,12) };
        var imageButton=new Button { Content=thumbnail,Padding=new Thickness(0),ToolTip="Open captured image in editor" }; System.Windows.Automation.AutomationProperties.SetName(imageButton,"Edit captured image"); imageButton.Click+=(_,_)=>edit(); stack.Children.Add(imageButton);
        var actions=new WrapPanel(); actions.Children.Add(Theme.Button("Edit",edit,true));actions.Children.Add(Theme.Button("Copy",copy));actions.Children.Add(Theme.Button("Save",save));actions.Children.Add(Theme.Button("Pin",pin));stack.Children.Add(actions);
        stack.Children.Add(Theme.Text("Memory only · Latest stays in the tray. Older unsaved captures are replaced.",11,true));
        var card=new Border { Child=stack,CornerRadius=new CornerRadius(12),BorderThickness=new Thickness(1),RenderTransform=translation };
        card.SetResourceReference(Border.BackgroundProperty,"Panel");card.SetResourceReference(Border.BorderBrushProperty,"Border");Content=new ScrollViewer {Content=card,VerticalScrollBarVisibility=ScrollBarVisibility.Auto};
        SourceInitialized+=(_,_)=>Native.Protect(this,true);
        Loaded+=(_,_)=> { Place(); if(Theme.MotionEnabled) { translation.BeginAnimation(TranslateTransform.XProperty,new DoubleAnimation(-24,0,TimeSpan.FromMilliseconds(180)) { EasingFunction=new CubicEase { EasingMode=EasingMode.EaseOut } }); Theme.AnimateEntrance(this); } clock.Start();timer.Start(); };
        Closed+=(_,_)=> { timer.Stop(); thumbnail.Source=null; };
    }
    internal void Place()
    {
        var rect=ResourcePolicy.PreviewBounds(new Rect(monitor.Work.Left,monitor.Work.Top,monitor.Work.Width,monitor.Work.Height),monitor.Scale,new Size(ActualWidth,ActualHeight));
        Native.Place(this,new Native.RectI {Left=(int)rect.Left,Top=(int)rect.Top,Right=(int)rect.Right,Bottom=(int)rect.Bottom});
    }
    private void Tick(object? sender,EventArgs e)
    {
        var elapsed=clock.Elapsed;clock.Restart();
        if(lifetime.Advance(elapsed,!IsVisible||IsMouseOver||IsKeyboardFocusWithin)) Dismiss();
    }
    internal void Dismiss()
    {
        if(lifetime.IsDismissing)return;timer.Stop();
        var duration=lifetime.BeginDismiss(Theme.MotionEnabled);
        if(duration==TimeSpan.Zero) {Close();return;}
        var fade=new DoubleAnimation(0,duration);fade.Completed+=(_,_)=>Close();BeginAnimation(OpacityProperty,fade);
        translation.BeginAnimation(TranslateTransform.XProperty,new DoubleAnimation(-16,TimeSpan.FromMilliseconds(120)));
    }
}
