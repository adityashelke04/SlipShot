using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Threading;
using NUnit.Framework;
using SlipShot.Services;
using SlipShot.UI;

namespace SlipShot.Tests;

[TestFixture, Apartment(ApartmentState.STA), NonParallelizable]
public class UiAuditTests
{
    [TestCase("# FFFFF")]
    [TestCase("#FFFFF ")]
    public void InvalidJpegColorIsRejectedBeforeApplyingSettings(string color)
    {
        _ = Application.Current ?? new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        Theme.Configure("Light", true);
        int applies = 0;
        var window = new SettingsWindow(new AppSettings(), _ => { applies++; return "Unexpected settings write"; });
        try
        {
            Descendants(window).OfType<TextBox>().Single(box => AutomationProperties.GetName(box) == "JPEG background").Text = color;
            Descendants(window).OfType<Button>().Single(button => AutomationProperties.GetName(button) == "Save settings")
                .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.That(applies, Is.Zero, "Whitespace is not a hexadecimal digit; invalid colors must stay in the form for correction.");
        }
        finally { window.Close(); }
    }

    [TestCase("Copy")]
    [TestCase("Save")]
    [TestCase("Open file location")]
    public void CompletingAnOperationDuringDismissalDoesNotCancelClosing(string action)
    {
        _ = Application.Current ?? new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        Theme.Configure("Light", true);
        PropertyInfo motion = typeof(Theme).GetProperty(nameof(Theme.MotionEnabled))!;
        bool previousMotion = Theme.MotionEnabled;
        motion.SetValue(null, true);
        var copy = new TaskCompletionSource();
        var save = new TaskCompletionSource<bool>();
        var monitor = new CaptureMonitor(IntPtr.Zero, new Native.RectI { Right = 800, Bottom = 600 }, new Native.RectI { Right = 800, Bottom = 600 }, 1);
        var window = new PreviewWindow(CoreTests.Image(), monitor, 10, () => { }, () => copy.Task, () => save.Task, () => { }, openLocation: () => { });
        var frame = new DispatcherFrame();
        bool closed = false;
        window.Closed += (_, _) => { closed = true; frame.Continue = false; };
        var watchdog = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        watchdog.Tick += (_, _) => frame.Continue = false;
        try
        {
            var button = Descendants(window).OfType<Button>().Single(button => AutomationProperties.GetName(button) == action);
            if (action != "Open file location") button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            window.Dismiss();
            if (action == "Open file location") button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            copy.TrySetResult();
            save.TrySetResult(true);
            watchdog.Start();
            Dispatcher.PushFrame(frame);
            Assert.That(closed, Is.True, "Completing an operation must preserve the active dismissal and close the faded preview.");
        }
        finally
        {
            watchdog.Stop();
            window.Close();
            motion.SetValue(null, previousMotion);
        }
    }

    private static IEnumerable<DependencyObject> Descendants(DependencyObject node)
    {
        yield return node;
        foreach (object child in LogicalTreeHelper.GetChildren(node))
            if (child is DependencyObject item)
                foreach (DependencyObject descendant in Descendants(item)) yield return descendant;
    }
}
