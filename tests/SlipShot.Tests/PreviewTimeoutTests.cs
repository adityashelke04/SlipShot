using System;
using NUnit.Framework;
using SlipShot.Core;
namespace SlipShot.Tests;
public class PreviewTimeoutTests
{
    [Test] public void UnattendedPreviewExpiresAtItsConfiguredDuration()
    {var t=new PreviewLifetime(TimeSpan.FromSeconds(10));Assert.That(t.Advance(TimeSpan.FromSeconds(9.9),false),Is.False);Assert.That(t.Advance(TimeSpan.FromSeconds(.1),false),Is.True);}
    [Test] public void ParkedPointerOrStaleFocusCannotKeepPreviewForever()
    {var t=new PreviewLifetime(TimeSpan.FromSeconds(10));Assert.That(t.Advance(TimeSpan.FromSeconds(29.9),true),Is.False);Assert.That(t.Advance(TimeSpan.FromSeconds(.1),true),Is.True);}
    [Test] public void LeavingAndReenteringDoesNotRefillHoverAllowance()
    {var t=new PreviewLifetime(TimeSpan.FromSeconds(10));t.Advance(TimeSpan.FromSeconds(15),true);t.Advance(TimeSpan.FromSeconds(5),false);Assert.That(t.Advance(TimeSpan.FromSeconds(10),true),Is.True);}
    [Test] public void ActiveDragAndHiddenCaptureDoNotConsumeEitherBudget()
    {var t=new PreviewLifetime(TimeSpan.FromSeconds(10));t.Advance(TimeSpan.FromHours(2),true,true);Assert.That(t.Remaining.TotalSeconds,Is.EqualTo(10));Assert.That(t.Advance(TimeSpan.FromSeconds(30),true),Is.True);}
    [Test] public void ClosingDoesNotDependOnAnimationCompletedOrHoverFlags()
    {var t=new PreviewLifetime(TimeSpan.FromSeconds(10));t.BeginDismiss(true);Assert.That(t.AdvanceDismissal(TimeSpan.FromMilliseconds(100)),Is.False);Assert.That(t.AdvanceDismissal(TimeSpan.FromMilliseconds(100)),Is.True);}
    [Test] public void ReducedMotionAndRepeatedDismissRemainBounded()
    {var t=new PreviewLifetime(TimeSpan.FromSeconds(10));Assert.That(t.AdvanceDismissal(TimeSpan.FromHours(1)),Is.False);t.BeginDismiss(false);Assert.That(t.AdvanceDismissal(TimeSpan.Zero),Is.True);Assert.That(t.BeginDismiss(true),Is.EqualTo(TimeSpan.Zero));}
    [Test] public void NewestCaptureReplacesCardAndOldPendingCapturesCannotResurface()
    {var queue=new PreviewQueue<string>();queue.Push("A");queue.Push("old pending");queue.Replace("B");Assert.That(queue.Current,Is.EqualTo("B"));Assert.That(queue.PendingCount,Is.Zero);Assert.That(queue.Items,Is.EqualTo(new[]{"B"}));queue.Dismiss();Assert.That(queue.Current,Is.Null);}
    [Test] public void CapturePublicationStaysBlockedUntilEvenCancelledWorkFinishes()
    {var gate=new CaptureGate();gate.TryBegin(out long generation);Assert.That(gate.IsBusy,Is.True);gate.Cancel();Assert.That(gate.IsBusy,Is.True);gate.TryComplete(generation);Assert.That(gate.IsBusy,Is.False);}
}
