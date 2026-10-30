using System;

namespace LeftShot.Core;

public sealed class PreviewLifetime
{
    public PreviewLifetime(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(duration));
        Remaining = duration;
    }

    public TimeSpan Remaining { get; private set; }
    public bool IsDismissing { get; private set; }

    // Elapsed comes from a monotonic Stopwatch, never the adjustable wall clock.
    public bool Advance(TimeSpan elapsed, bool interacting)
    {
        if (elapsed < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(elapsed));
        if (!interacting && !IsDismissing)
            Remaining = elapsed >= Remaining ? TimeSpan.Zero : Remaining - elapsed;
        return Remaining == TimeSpan.Zero;
    }

    public TimeSpan BeginDismiss(bool motionEnabled)
    {
        if (IsDismissing) return TimeSpan.Zero;
        IsDismissing = true;
        return motionEnabled ? TimeSpan.FromMilliseconds(120) : TimeSpan.Zero;
    }
}
