using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using SlipShot.Core;
using SlipShot.Services;

namespace SlipShot.UI;

internal sealed class PreviewWindow : Window
{
    private readonly DispatcherTimer timer;
    private readonly CaptureMonitor monitor;
    private readonly TranslateTransform translation = new();
    private readonly PreviewGesture gesture = new();
    private readonly Stopwatch clock = new();
    private readonly TimeSpan previewDuration;
    private PreviewLifetime lifetime;
    private bool dragging;
    private bool closeRequested;

    internal bool IsDragging => dragging;

    internal PreviewWindow(BitmapSource image, CaptureMonitor monitor, int seconds, Action edit, Action copy,
        Action save, Action pin, Action<DependencyObject, BitmapSource>? drag = null)
    {
        this.monitor = monitor;
        previewDuration = TimeSpan.FromSeconds(seconds);
        lifetime = new PreviewLifetime(previewDuration);
        timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(200) };
        timer.Tick += Tick;
        Title = "SlipShot capture preview";
        ShowActivated = false;
        ShowInTaskbar = false;
        Topmost = true;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        SizeToContent = SizeToContent.Height;
        Width = Math.Max(180, Math.Min(328, monitor.Work.Width / monitor.Scale - 32));
        MaxHeight = Math.Max(120, monitor.Work.Height / monitor.Scale - 32);
        Theme.Apply(this);

        var stack = new StackPanel { Margin = new Thickness(14) };
        var heading = new DockPanel();
        Button close = Theme.IconButton("Dismiss preview", "M 3,3 L 13,13 M 13,3 L 3,13", Dismiss, size: 28);
        close.MinHeight = 28;
        close.Margin = new Thickness(0);
        close.Padding = new Thickness(7);
        DockPanel.SetDock(close, Dock.Right);
        heading.Children.Add(close);
        var titleRow = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        FrameworkElement mark = Theme.Glyph("M 1,5 L 1,1 L 5,1 M 11,1 L 15,1 L 15,5 M 15,11 L 15,15 L 11,15 M 5,15 L 1,15 L 1,11", 16);
        mark.SetResourceReference(System.Windows.Shapes.Shape.StrokeProperty,"Accent");
        mark.Margin = new Thickness(0, 0, 8, 0);
        titleRow.Children.Add(mark);
        TextBlock title = Theme.Text("Capture ready", 14);
        title.FontWeight = FontWeights.SemiBold;
        titleRow.Children.Add(title);
        heading.Children.Add(titleRow);
        stack.Children.Add(heading);

        var thumbnail = new Image
        {
            Source = image,
            Height = Math.Clamp((Width - 28) * image.PixelHeight / image.PixelWidth, 88, 190),
            Stretch = Stretch.Uniform
        };
        RenderOptions.SetBitmapScalingMode(thumbnail, BitmapScalingMode.HighQuality);
        var imageButton = new Button
        {
            Content = thumbnail,
            Padding = new Thickness(0),
            BorderThickness = new Thickness(0),
            Margin = new Thickness(0, 12, 0, 10),
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            ToolTip = "Click to edit. Dragging creates a temporary PNG beside SlipShot."
        };
        imageButton.SetResourceReference(BackgroundProperty, "Canvas");
        AutomationProperties.SetName(imageButton, "Edit captured image, or drag it to another app");
        imageButton.SizeChanged += (_, _) => imageButton.Clip = new RectangleGeometry(new Rect(imageButton.RenderSize), 8, 8);
        stack.Children.Add(imageButton);

        var metadata = new Grid();
        metadata.ColumnDefinitions.Add(new ColumnDefinition());
        metadata.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        TextBlock hint = Theme.Text("Drag image to another app", 11, true);
        hint.TextWrapping = TextWrapping.NoWrap;
        hint.TextTrimming = TextTrimming.CharacterEllipsis;
        hint.ToolTip = imageButton.ToolTip;
        metadata.Children.Add(hint);
        TextBlock dimensions = Theme.Text($"{image.PixelWidth} × {image.PixelHeight}", 11, true);
        dimensions.Margin = new Thickness(8, 0, 0, 0);
        dimensions.TextWrapping = TextWrapping.NoWrap;
        Grid.SetColumn(dimensions, 1);
        metadata.Children.Add(dimensions);
        stack.Children.Add(metadata);

        var actions = new UniformGrid { Columns = 4 };
        AddAction("Edit", "M 2,11 L 1,15 L 5,14 L 14,5 L 10,1 Z M 8,3 L 12,7", edit, true);
        AddAction("Copy", "M 5,5 L 14,5 L 14,15 L 5,15 Z M 2,11 L 1,11 L 1,1 L 10,1 L 10,2", copy);
        AddAction("Save", "M 2,1 L 11,1 L 15,5 L 15,15 L 1,15 L 1,1 Z M 5,1 L 5,6 L 11,6 L 11,1 M 5,15 L 5,10 L 11,10 L 11,15", save);
        AddAction("Pin", "M 5,1 L 11,1 M 6,1 L 6,6 L 3,10 L 13,10 L 10,6 L 10,1 M 8,10 L 8,15", pin);
        var actionGroup = new Border { Child = actions, CornerRadius = new CornerRadius(9), Padding = new Thickness(2), Margin = new Thickness(0, 12, 0, 0) };
        actionGroup.SetResourceReference(Border.BackgroundProperty, "Canvas");
        stack.Children.Add(actionGroup);
        TextBlock retention = Theme.Text("Drag creates a temporary PNG · Latest in tray", 11, true);
        retention.Margin = new Thickness(0, 10, 0, 0);
        retention.ToolTip = "Accepted drops keep a temporary PNG in data/drag-out beside SlipShot for at least 24 hours. Older files are cleaned on a later drag; cancelled drags are removed.";
        stack.Children.Add(retention);

        var card = new Border { Child = stack, CornerRadius = new CornerRadius(12), BorderThickness = new Thickness(1), RenderTransform = translation };
        card.SetResourceReference(Border.BackgroundProperty, "Panel");
        card.SetResourceReference(Border.BorderBrushProperty, "Border");
        Content = new ScrollViewer { Content = card, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };

        imageButton.PreviewMouseLeftButtonDown += (_, e) =>
        {
            e.Handled = true;
            if (dragging || lifetime.IsDismissing) return;
            gesture.Press(e.GetPosition(imageButton));
            if (!imageButton.CaptureMouse()) gesture.Cancel();
        };
        imageButton.PreviewMouseMove += (_, e) =>
        {
            if (dragging || !gesture.Holding) return;
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                gesture.Cancel();
                imageButton.ReleaseMouseCapture();
                return;
            }
            if (!gesture.Move(e.GetPosition(imageButton), SystemParameters.MinimumHorizontalDragDistance, SystemParameters.MinimumVerticalDragDistance)) return;
            e.Handled = true;
            dragging = true;
            imageButton.ReleaseMouseCapture();
            hint.Text = "Drop into an app that accepts files";
            try
            {
                drag?.Invoke(imageButton, image);
                hint.Text = "Ready to drag again";
                hint.ToolTip = imageButton.ToolTip;
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or
                ArgumentException or NotSupportedException or OutOfMemoryException or System.Runtime.InteropServices.ExternalException)
            {
                hint.Text = "Drag unavailable · use Copy or Save";
                hint.ToolTip = error.Message;
            }
            finally
            {
                gesture.Cancel();
                dragging = false;
                lifetime = new PreviewLifetime(previewDuration);
                clock.Restart();
                if (closeRequested) Close();
                else if (IsVisible) timer.Start();
            }
        };
        imageButton.PreviewMouseLeftButtonUp += (_, e) =>
        {
            e.Handled = true;
            if (dragging) return;
            bool clicked = gesture.Release();
            imageButton.ReleaseMouseCapture();
            if (clicked && new Rect(imageButton.RenderSize).Contains(e.GetPosition(imageButton)) && !lifetime.IsDismissing) edit();
        };
        imageButton.LostMouseCapture += (_, _) => { if (!dragging) gesture.Cancel(); };
        imageButton.Click += (_, _) => { if (!gesture.Holding && !dragging && !lifetime.IsDismissing) edit(); };
        imageButton.QueryContinueDrag += (_, e) =>
        {
            if (!closeRequested) return;
            e.Action = DragAction.Cancel;
            e.Handled = true;
        };

        SourceInitialized += (_, _) => Native.Protect(this, true);
        Loaded += (_, _) =>
        {
            Place();
            if (Theme.MotionEnabled)
            {
                translation.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(-24, 0, TimeSpan.FromMilliseconds(180)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
                Theme.AnimateEntrance(this);
            }
            clock.Start();
            timer.Start();
        };
        Closed += (_, _) => { timer.Stop(); clock.Stop(); gesture.Cancel(); thumbnail.Source = null; };

        void AddAction(string label, string path, Action action, bool primary = false)
        {
            Button button = Theme.Button(label, action, primary);
            button.Margin = new Thickness(1, 0, 1, 0);
            button.Padding = new Thickness(5, 6, 5, 6);
            var content = new StackPanel { Orientation = Orientation.Horizontal };
            content.Children.Add(Theme.Glyph(path, 13));
            var text = new TextBlock { Text = label, FontSize = 12, Margin = new Thickness(5, 0, 0, 0) };
            text.SetResourceReference(TextBlock.ForegroundProperty, primary ? "OnAccent" : "Ink");
            content.Children.Add(text);
            button.Content = content;
            actions.Children.Add(button);
        }
    }

    internal void Place()
    {
        Rect rect = ResourcePolicy.PreviewBounds(new Rect(monitor.Work.Left, monitor.Work.Top, monitor.Work.Width, monitor.Work.Height), monitor.Scale, new Size(ActualWidth, ActualHeight));
        Native.Place(this, new Native.RectI { Left = (int)rect.Left, Top = (int)rect.Top, Right = (int)rect.Right, Bottom = (int)rect.Bottom });
    }

    private void Tick(object? sender, EventArgs e)
    {
        TimeSpan elapsed = clock.Elapsed;
        clock.Restart();
        if (lifetime.Advance(elapsed, !IsVisible || IsMouseOver || IsKeyboardFocusWithin || gesture.Holding || dragging)) Dismiss();
    }

    internal void Dismiss()
    {
        if (dragging) { closeRequested = true; return; }
        if (lifetime.IsDismissing) return;
        timer.Stop();
        gesture.Cancel();
        TimeSpan duration = lifetime.BeginDismiss(Theme.MotionEnabled);
        if (duration == TimeSpan.Zero) { Close(); return; }
        var fade = new DoubleAnimation(0, duration);
        fade.Completed += (_, _) => Close();
        BeginAnimation(OpacityProperty, fade);
        translation.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(-16, TimeSpan.FromMilliseconds(120)));
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        if (!dragging) return;
        closeRequested = true;
        e.Cancel = true;
    }
}
