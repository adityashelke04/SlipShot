namespace SlipShot.Core;

// Owned by the UI dispatcher. Cancellation rejects delivery but cannot release an in-flight native worker.
public sealed class CaptureGate
{
    private long generation;
    private bool active;
    private bool canceled;

    public bool TryBegin(out long token)
    {
        token = 0;
        if (active) return false;
        active = true;
        canceled = false;
        token = ++generation;
        return true;
    }

    public void Cancel()
    {
        if (active) canceled = true;
    }

    public bool IsCurrent(long token) => active && !canceled && token == generation;

    public bool TryComplete(long token)
    {
        if (!active || token != generation) return false;
        active = false;
        return !canceled;
    }
}
