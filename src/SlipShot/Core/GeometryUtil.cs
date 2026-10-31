using System;
using System.Linq;
using System.Windows;

namespace SlipShot.Core;

public static class GeometryUtil
{
    public static Rect Normalize(Point a, Point b, Rect bounds)
    {
        if (!Finite(a) || !Finite(b) || bounds.IsEmpty) return Rect.Empty;
        var rectangle = new Rect(a, b);
        rectangle.Intersect(bounds);
        return rectangle;
    }

    public static Point ToPixels(Point local, double scale, Point physicalOrigin)
    {
        ValidateScale(scale);
        Point result = new(physicalOrigin.X + local.X * scale, physicalOrigin.Y + local.Y * scale);
        if (!Finite(local) || !Finite(physicalOrigin) || !Finite(result)) throw new ArgumentException("Coordinates must be finite.");
        return result;
    }

    public static Point ToDips(Point physical, double scale, Point physicalOrigin)
    {
        ValidateScale(scale);
        Point result = new((physical.X - physicalOrigin.X) / scale, (physical.Y - physicalOrigin.Y) / scale);
        if (!Finite(physical) || !Finite(physicalOrigin) || !Finite(result)) throw new ArgumentException("Coordinates must be finite.");
        return result;
    }

    private static void ValidateScale(double scale)
    {
        if (!double.IsFinite(scale) || scale <= 0) throw new ArgumentOutOfRangeException(nameof(scale));
    }

    public static Rect PixelBounds(Rect rectangle, Rect limits)
    {
        if (rectangle.IsEmpty || rectangle.Width <= 0 || rectangle.Height <= 0 || !double.IsFinite(rectangle.X) || !double.IsFinite(rectangle.Y)
            || !double.IsFinite(rectangle.Width) || !double.IsFinite(rectangle.Height)) return Rect.Empty;
        var result = new Rect(new Point(Math.Floor(rectangle.Left), Math.Floor(rectangle.Top)),
            new Point(Math.Ceiling(rectangle.Right), Math.Ceiling(rectangle.Bottom)));
        result.Intersect(limits);
        return result;
    }

    private static bool Finite(Point point) => double.IsFinite(point.X) && double.IsFinite(point.Y);

    public static bool HitTest(Annotation annotation, Point point, double tolerance)
    {
        double radius = Math.Max(0, tolerance) + Math.Max(0, annotation.StrokeWidth) / 2;
        if (annotation.Kind is ToolKind.Line or ToolKind.Arrow or ToolKind.Pen or ToolKind.Highlighter)
        {
            var points = annotation.Points;
            if (annotation.Kind == ToolKind.Arrow)
            {
                Point start = points is { Count: > 0 } ? points[0] : annotation.Bounds.TopLeft;
                Point end = points is { Count: > 1 } ? points[^1] : annotation.Bounds.BottomRight;
                Vector direction = end - start;
                if (direction.Length > 0)
                {
                    direction.Normalize();
                    Vector offset = point - end;
                    double behind = -Vector.Multiply(offset, direction);
                    double across = Math.Abs(Vector.CrossProduct(offset, direction));
                    if (behind >= 0 && behind <= Math.Max(12, annotation.StrokeWidth * 3)
                        && across <= behind * 0.45) return true;
                }
            }
            if (points is null || points.Count == 0)
                return SegmentDistance(point, annotation.Bounds.TopLeft, annotation.Bounds.BottomRight) <= radius;
            if (points.Count == 1) return (point - points[0]).Length <= radius;
            for (int i = 1; i < points.Count; i++)
                if (SegmentDistance(point, points[i - 1], points[i]) <= radius) return true;
            return false;
        }
        if (annotation.Kind == ToolKind.Step)
        {
            Point center = new(annotation.Bounds.X + annotation.Bounds.Width / 2, annotation.Bounds.Y + annotation.Bounds.Height / 2);
            return (point - center).Length <= Math.Max(10, Math.Min(annotation.Bounds.Width, annotation.Bounds.Height) / 2) + Math.Max(0, tolerance);
        }
        Rect bounds = annotation.Bounds;
        bounds.Inflate(radius, radius);
        if (!bounds.Contains(point)) return false;
        if (annotation.Kind == ToolKind.Ellipse && annotation.Bounds.Width > 0 && annotation.Bounds.Height > 0)
        {
            double rx = annotation.Bounds.Width / 2, ry = annotation.Bounds.Height / 2;
            double dx = point.X - annotation.Bounds.X - rx, dy = point.Y - annotation.Bounds.Y - ry;
            double outer = dx * dx / ((rx + radius) * (rx + radius)) + dy * dy / ((ry + radius) * (ry + radius));
            if (outer > 1) return false;
            if (annotation.Filled || rx <= radius || ry <= radius) return true;
            return dx * dx / ((rx - radius) * (rx - radius)) + dy * dy / ((ry - radius) * (ry - radius)) >= 1;
        }
        if (annotation.Kind == ToolKind.Rectangle && !annotation.Filled)
        {
            Rect interior = annotation.Bounds;
            if (interior.Width <= 2 * radius || interior.Height <= 2 * radius) return true;
            interior.Inflate(-radius, -radius);
            return !interior.Contains(point);
        }
        return true;
    }

    private static double SegmentDistance(Point point, Point start, Point end)
    {
        Vector segment = end - start;
        if (segment.LengthSquared == 0) return (point - start).Length;
        double fraction = Math.Clamp(Vector.Multiply(point - start, segment) / segment.LengthSquared, 0, 1);
        return (point - (start + segment * fraction)).Length;
    }

    public static Annotation Move(Annotation annotation, Vector offset)
    {
        Rect bounds = annotation.Bounds;
        bounds.Offset(offset);
        return annotation with { Bounds = bounds, Points = annotation.Points?.Select(point => point + offset).ToArray() };
    }

    public static Annotation Resize(Annotation annotation, Rect bounds)
    {
        Rect old = annotation.Bounds;
        return annotation with
        {
            Bounds = bounds,
            Points = annotation.Points?.Select(point => new Point(
                bounds.X + (old.Width == 0
                    ? old.Height == 0 ? 0 : (point.Y - old.Y) * bounds.Width / old.Height
                    : (point.X - old.X) * bounds.Width / old.Width),
                bounds.Y + (old.Height == 0
                    ? old.Width == 0 ? 0 : (point.X - old.X) * bounds.Height / old.Width
                    : (point.Y - old.Y) * bounds.Height / old.Height))).ToArray()
        };
    }
}
