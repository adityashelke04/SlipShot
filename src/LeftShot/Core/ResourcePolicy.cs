using System;
using System.Windows;

namespace LeftShot.Core;
public static class ResourcePolicy
{
    public const long ImageBudget = 512L * 1024 * 1024;
    // A conservative pixel-buffer budget, not a measured process working-set guarantee.
    public static bool CanAllocate(long retainedBytes,long pixels,int transientBuffers=3) =>
        retainedBytes>=0 && pixels>0 && pixels<=32_000_000 && transientBuffers>0 &&
        retainedBytes <= ImageBudget - pixels*4*transientBuffers;

    public static Rect PreviewBounds(Rect workArea,double scale,Size dipSize)
    {
        if(workArea.IsEmpty||workArea.Width<1||workArea.Height<1||!double.IsFinite(scale)||scale<=0)
            throw new ArgumentException("A valid monitor work area and scale are required.");
        double inset=Math.Min(16*scale,Math.Min(workArea.Width,workArea.Height)/8);
        double width=Math.Max(1,Math.Min(Math.Ceiling(dipSize.Width*scale),workArea.Width-inset*2));
        double height=Math.Max(1,Math.Min(Math.Ceiling(dipSize.Height*scale),workArea.Height-inset*2));
        return new Rect(Math.Ceiling(workArea.Left+inset),Math.Floor(workArea.Bottom-inset-height),Math.Floor(width),Math.Floor(height));
    }
}
