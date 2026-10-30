using System.Windows;
using SlipShot.Core;
using NUnit.Framework;
namespace SlipShot.Tests;
public class PreviewGestureTests
{
    [Test] public void SmallMotionClicksButThresholdStartsOnlyOneDrag()
    {var gesture=new PreviewGesture();gesture.Press(new Point(5,5));Assert.That(gesture.Move(new Point(7,7),4,4),Is.False);Assert.That(gesture.Release(),Is.True);gesture.Press(new Point(5,5));Assert.That(gesture.Move(new Point(9,5),4,4),Is.True);Assert.That(gesture.Move(new Point(20,20),4,4),Is.False);Assert.That(gesture.Release(),Is.False);}
    [Test] public void CancelNeverTurnsIntoClickOrDrag()
    {var gesture=new PreviewGesture();gesture.Press(new Point());gesture.Cancel();Assert.That(gesture.Release(),Is.False);Assert.That(gesture.Move(new Point(50,50),4,4),Is.False);}
    [Test] public void HoldingAndDraggingBothKeepPreviewAliveOutsideItsBounds()
    {var g=new PreviewGesture();var timer=new PreviewLifetime(System.TimeSpan.FromSeconds(8));g.Press(new Point());timer.Advance(System.TimeSpan.FromSeconds(30),g.Holding||g.Dragging);Assert.That(timer.Remaining.TotalSeconds,Is.EqualTo(8));g.Move(new Point(-200,200),4,4);timer.Advance(System.TimeSpan.FromMinutes(1),g.Holding||g.Dragging);Assert.That(timer.Remaining.TotalSeconds,Is.EqualTo(8));g.Release();Assert.That(timer.Advance(System.TimeSpan.FromSeconds(8),false),Is.True);}
}
