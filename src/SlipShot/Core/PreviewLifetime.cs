using System;

namespace SlipShot.Core;

public sealed class PreviewLifetime
{
    private TimeSpan passiveGrace;
    private TimeSpan dismissalRemaining;
    public PreviewLifetime(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(duration));
        Remaining = duration;
        passiveGrace = duration * 2;
    }

    public TimeSpan Remaining { get; private set; }
    public bool IsDismissing { get; private set; }

    // Elapsed comes from a monotonic Stopwatch, never the adjustable wall clock.
    public bool Advance(TimeSpan elapsed, bool interacting, bool protectedInteraction = false)
    {
        if (elapsed < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(elapsed));
        if (protectedInteraction || IsDismissing) return false;
        // Hover/focus get a finite shared allowance. A parked pointer must not pin the card forever.
        if (interacting)
        {
            TimeSpan paused = elapsed < passiveGrace ? elapsed : passiveGrace;
            passiveGrace -= paused;
            elapsed -= paused;
        }
        Remaining = elapsed >= Remaining ? TimeSpan.Zero : Remaining - elapsed;
        return Remaining == TimeSpan.Zero;
    }

    public TimeSpan BeginDismiss(bool motionEnabled)
    {
        if (IsDismissing) return TimeSpan.Zero;
        IsDismissing = true;
        dismissalRemaining = motionEnabled ? TimeSpan.FromMilliseconds(120) : TimeSpan.Zero;
        return dismissalRemaining;
    }

    public bool AdvanceDismissal(TimeSpan elapsed)
    {
        if (elapsed < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(elapsed));
        if (!IsDismissing) return false;
        dismissalRemaining = elapsed >= dismissalRemaining ? TimeSpan.Zero : dismissalRemaining - elapsed;
        return dismissalRemaining == TimeSpan.Zero;
    }
}
