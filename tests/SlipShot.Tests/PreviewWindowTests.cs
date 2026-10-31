using System;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Automation;
using System.Linq;
using SlipShot.Core;
using SlipShot.Services;
using SlipShot.UI;
using NUnit.Framework;

namespace SlipShot.Tests;

[TestFixture,Apartment(ApartmentState.STA),NonParallelizable]
public class PreviewWindowTests
{
    [TestCase(false)] [TestCase(true)]
    public void OpenFileLocationIsAvailableBeforeAndAfterSaveWithoutSaving(bool saved)
    {
        _=Application.Current??new Application {ShutdownMode=ShutdownMode.OnExplicitShutdown};Theme.Configure("Light",true);
        var monitor=new CaptureMonitor(IntPtr.Zero,new Native.RectI {Right=800,Bottom=600},new Native.RectI {Right=800,Bottom=600},1);
        var doc=new EditDocument(CoreTests.Image());if(saved)doc.MarkSaved("synthetic-status.png");int opens=0,saves=0,edits=0;
        var window=new PreviewWindow(doc.Source,monitor,10,()=>edits++,()=>System.Threading.Tasks.Task.CompletedTask,()=>{saves++;return System.Threading.Tasks.Task.FromResult(true);},()=>{},document:doc,openLocation:()=>opens++);
        try
        {
            var button=Descendants(window).OfType<Button>().Single(b=>AutomationProperties.GetName(b)=="Open file location");
            var footer=(Grid)LogicalTreeHelper.GetParent(button);
            Assert.That(footer.Children.OfType<TextBlock>().Single().Text,Is.EqualTo("Auto-hide: 10s · Latest in tray"));
            Assert.That(Grid.GetColumn(button),Is.EqualTo(1));
            Assert.That(button.Width,Is.EqualTo(28));Assert.That(button.Height,Is.EqualTo(28));
            Assert.That(button.ToolTip,Is.EqualTo("Open captures folder"));
            var actions=Descendants(window).OfType<UniformGrid>().Single();
            Assert.That(actions.Columns,Is.EqualTo(4));
            Assert.That(actions.Children.OfType<Button>().Select(AutomationProperties.GetName),Is.EqualTo(new[]{"Edit","Copy",saved?"Image saved":"Save","Pin"}));
            Assert.That(button.IsEnabled,Is.True);button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.That(opens,Is.EqualTo(1));Assert.That(saves,Is.Zero);Assert.That(edits,Is.Zero);
            Assert.That(new WindowInteropHelper(window).Handle,Is.EqualTo(IntPtr.Zero));
        }
        finally{window.Close();}
    }
    [Test] public void FooterLocationRemainsAvailableWhenSaveStateChanges()
    {
        _=Application.Current??new Application {ShutdownMode=ShutdownMode.OnExplicitShutdown};Theme.Configure("Light",true);
        var monitor=new CaptureMonitor(IntPtr.Zero,new Native.RectI {Right=800,Bottom=600},new Native.RectI {Right=800,Bottom=600},1);
        var doc=new EditDocument(CoreTests.Image());int opens=0;
        var window=new PreviewWindow(doc.Source,monitor,10,()=>{},()=>System.Threading.Tasks.Task.CompletedTask,()=>System.Threading.Tasks.Task.FromResult(false),()=>{},document:doc,openLocation:()=>opens++);
        try
        {
            Button folder=Descendants(window).OfType<Button>().Single(b=>AutomationProperties.GetName(b)=="Open file location");
            folder.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            doc.MarkSaved("synthetic-status.png");
            Assert.That(Descendants(window).OfType<Button>().Single(b=>AutomationProperties.GetName(b)=="Image saved").IsEnabled,Is.False);
            Assert.That(folder.IsEnabled,Is.True);folder.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.That(opens,Is.EqualTo(2));
            Assert.That(new WindowInteropHelper(window).Handle,Is.EqualTo(IntPtr.Zero));
        }
        finally{window.Close();}
    }
    [Test] public void FolderOpenFailureStaysInlineAndCanBeRetried()
    {
        _=Application.Current??new Application {ShutdownMode=ShutdownMode.OnExplicitShutdown};Theme.Configure("Light",true);
        var monitor=new CaptureMonitor(IntPtr.Zero,new Native.RectI {Right=800,Bottom=600},new Native.RectI {Right=800,Bottom=600},1);
        var window=new PreviewWindow(CoreTests.Image(),monitor,10,()=>{},()=>System.Threading.Tasks.Task.CompletedTask,()=>System.Threading.Tasks.Task.FromResult(false),()=>{},openLocation:()=>throw new System.IO.IOException("Synthetic inaccessible folder"));
        try
        {
            var button=Descendants(window).OfType<Button>().Single(b=>AutomationProperties.GetName(b)=="Open file location");
            Assert.DoesNotThrow(()=>button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)));
            Assert.That(button.IsEnabled,Is.True);Assert.That(Descendants(window).OfType<TextBlock>().Any(t=>t.Text.Contains("Folder unavailable")),Is.True);
        }
        finally{window.Close();}
    }
    [Test] public void SuccessfulPreviewSaveDisablesSaveAndCannotRunTwice()
    {
        _=Application.Current??new Application {ShutdownMode=ShutdownMode.OnExplicitShutdown};Theme.Configure("Light",true);
        var monitor=new CaptureMonitor(IntPtr.Zero,new Native.RectI {Right=800,Bottom=600},new Native.RectI {Right=800,Bottom=600},1);
        int saves=0;var doc=new EditDocument(CoreTests.Image());
        var window=new PreviewWindow(doc.Source,monitor,10,()=>{},()=>System.Threading.Tasks.Task.CompletedTask,()=>{saves++;doc.MarkSaved("synthetic-file.png");return System.Threading.Tasks.Task.FromResult(true);},()=>{},document:doc);
        try
        {
            Button button=Descendants(window).OfType<Button>().Single(b=>AutomationProperties.GetName(b)=="Save");
            button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.That(saves,Is.EqualTo(1));Assert.That(button.IsEnabled,Is.False);Assert.That(AutomationProperties.GetName(button),Is.EqualTo("Image saved"));
            Assert.That(new WindowInteropHelper(window).Handle,Is.EqualTo(IntPtr.Zero));
        }
        finally{window.Close();}
    }
    [Test] public void FailedPreviewSaveRemainsAvailableForRetry()
    {
        _=Application.Current??new Application {ShutdownMode=ShutdownMode.OnExplicitShutdown};Theme.Configure("Light",true);
        var monitor=new CaptureMonitor(IntPtr.Zero,new Native.RectI {Right=800,Bottom=600},new Native.RectI {Right=800,Bottom=600},1);
        int saves=0;var window=new PreviewWindow(CoreTests.Image(),monitor,10,()=>{},()=>System.Threading.Tasks.Task.CompletedTask,()=>{saves++;return System.Threading.Tasks.Task.FromResult(false);},()=>{});
        try{Button button=Descendants(window).OfType<Button>().Single(b=>AutomationProperties.GetName(b)=="Save");button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Assert.That(button.IsEnabled,Is.True);button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Assert.That(saves,Is.EqualTo(2));}
        finally{window.Close();}
    }
    private static System.Collections.Generic.IEnumerable<DependencyObject> Descendants(DependencyObject node)
    {yield return node;foreach(object child in LogicalTreeHelper.GetChildren(node))if(child is DependencyObject item)foreach(var descendant in Descendants(item))yield return descendant;}
    [Test]
    public void UnrenderedWindowStillClosesWhenFadeCannotComplete()
    {
        // No Show, handle creation, capture, real input or clipboard. An unrendered fade
        // cannot drive the old Completed-only close path; the real timer must close it.
        _=Application.Current ?? new Application {ShutdownMode=ShutdownMode.OnExplicitShutdown};
        Theme.Configure("Light",false);
        PropertyInfo motion=typeof(Theme).GetProperty(nameof(Theme.MotionEnabled))!;
        bool previous=Theme.MotionEnabled;motion.SetValue(null,true);
        var frame=new DispatcherFrame();
        var monitor=new CaptureMonitor(IntPtr.Zero,new Native.RectI {Right=800,Bottom=600},new Native.RectI {Right=800,Bottom=600},1);
        var window=new PreviewWindow(CoreTests.Image(),monitor,10,()=>{},()=>System.Threading.Tasks.Task.CompletedTask,()=>System.Threading.Tasks.Task.FromResult(false),()=>{});
        bool closed=false;
        window.Closed+=(_,_)=>{closed=true;frame.Continue=false;};
        var watchdog=new DispatcherTimer {Interval=TimeSpan.FromSeconds(2)};
        watchdog.Tick+=(_,_)=>frame.Continue=false;
        try
        {
            Assert.That(new WindowInteropHelper(window).Handle,Is.EqualTo(IntPtr.Zero));
            window.Dismiss();watchdog.Start();Dispatcher.PushFrame(frame);
            Assert.That(closed,Is.True,"Preview must close even when no rendered animation sends Completed.");
        }
        finally {watchdog.Stop();window.Close();motion.SetValue(null,previous);}
    }
}
