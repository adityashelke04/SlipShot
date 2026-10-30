using System;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using SlipShot.Services;
using SlipShot.UI;
using NUnit.Framework;

namespace SlipShot.Tests;

[TestFixture,Apartment(ApartmentState.STA),NonParallelizable]
public class PreviewWindowTests
{
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
        var window=new PreviewWindow(CoreTests.Image(),monitor,10,()=>{},()=>System.Threading.Tasks.Task.CompletedTask,()=>{},()=>{});
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
