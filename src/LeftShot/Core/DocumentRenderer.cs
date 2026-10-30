using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LeftShot.Core;

public static class DocumentRenderer
{
    public static BitmapSource Render(EditDocument document, Color? background = null)
    {
        Rect crop = document.State.Crop;
        var visual = new DrawingVisual();
        using (DrawingContext drawing = visual.RenderOpen())
        {
            if (background is Color color)
                drawing.DrawRectangle(new SolidColorBrush(color), null, new Rect(0, 0, crop.Width, crop.Height));
            drawing.PushTransform(new TranslateTransform(-crop.X, -crop.Y));
            Draw(drawing, document.Source, document.State);
            drawing.Pop();
        }
        var result = new RenderTargetBitmap((int)crop.Width, (int)crop.Height, 96, 96, PixelFormats.Pbgra32);
        result.Render(visual);
        result.Freeze();
        return result;
    }

    // All coordinates are source pixels; the editor supplies its pan/zoom transform.
    public static void Draw(DrawingContext drawing, BitmapSource source, DocumentState state)
    {
        drawing.PushClip(new RectangleGeometry(state.Crop));
        drawing.DrawImage(source, new Rect(0, 0, source.PixelWidth, source.PixelHeight));
        foreach (Annotation annotation in state.Annotations)
            if (annotation.Kind != ToolKind.Redaction) DrawAnnotation(drawing, annotation);
        // Redactions must cover even annotations added later and never inherit opacity.
        foreach (Annotation annotation in state.Annotations)
        {
            if (annotation.Kind != ToolKind.Redaction) continue;
            Rect pixels = GeometryUtil.PixelBounds(annotation.Bounds, state.Crop);
            if (!pixels.IsEmpty) drawing.DrawRectangle(Brushes.Black, null, pixels);
        }
        drawing.Pop();
    }

    private static void DrawAnnotation(DrawingContext drawing, Annotation annotation)
    {
        Rect bounds = annotation.Bounds;
        var brush = new SolidColorBrush(annotation.Color);
        var pen = new Pen(brush, Math.Max(0.5, annotation.StrokeWidth))
        {
            StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round
        };
        Brush? fill = annotation.Filled ? brush : null;
        switch (annotation.Kind)
        {
            case ToolKind.Rectangle:
                drawing.DrawRectangle(fill, pen, bounds);
                break;
            case ToolKind.Ellipse:
                drawing.DrawEllipse(fill, pen, new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2), bounds.Width / 2, bounds.Height / 2);
                break;
            case ToolKind.Line:
            case ToolKind.Arrow:
            {
                IReadOnlyList<Point>? points = annotation.Points;
                Point start = points is { Count: > 0 } ? points[0] : bounds.TopLeft;
                Point end = points is { Count: > 1 } ? points[points.Count - 1] : bounds.BottomRight;
                drawing.DrawLine(pen, start, end);
                Vector direction = end - start;
                if (annotation.Kind == ToolKind.Arrow && direction.Length > 0)
                {
                    direction.Normalize();
                    double size = Math.Max(12, annotation.StrokeWidth * 3);
                    Vector perpendicular = new(-direction.Y, direction.X);
                    var head = new StreamGeometry();
                    using (StreamGeometryContext path = head.Open())
                    {
                        path.BeginFigure(end, true, true);
                        path.LineTo(end - direction * size + perpendicular * size * 0.45, true, false);
                        path.LineTo(end - direction * size - perpendicular * size * 0.45, true, false);
                    }
                    drawing.DrawGeometry(brush, null, head);
                }
                break;
            }
            case ToolKind.Pen:
            case ToolKind.Highlighter:
            {
                if (annotation.Points is not { Count: > 0 } points) break;
                bool highlight = annotation.Kind == ToolKind.Highlighter;
                if (highlight) drawing.PushOpacity(0.35);
                if (points.Count == 1)
                    drawing.DrawEllipse(brush, null, points[0], pen.Thickness / 2, pen.Thickness / 2);
                else
                {
                    var geometry = new StreamGeometry();
                    using (StreamGeometryContext path = geometry.Open())
                    {
                        path.BeginFigure(points[0], false, false);
                        for (int i = 1; i < points.Count; i++) path.LineTo(points[i], true, false);
                    }
                    drawing.DrawGeometry(null, pen, geometry);
                }
                if (highlight) drawing.Pop();
                break;
            }
            case ToolKind.Text:
                drawing.DrawText(Text(annotation.Text, annotation.FontSize, brush), bounds.TopLeft);
                break;
            case ToolKind.Step:
            {
                Point center = new(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
                double radius = Math.Max(10, Math.Min(bounds.Width, bounds.Height) / 2);
                drawing.DrawEllipse(brush, null, center, radius, radius);
                FormattedText text = Text(annotation.Number.ToString(CultureInfo.InvariantCulture), Math.Max(10, radius * 1.15), Brushes.White);
                drawing.DrawText(text, new Point(center.X - text.Width / 2, center.Y - text.Height / 2));
                break;
            }
        }
    }

    private static FormattedText Text(string value, double size, Brush brush) => new(value,
        CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), Math.Max(1, size), brush, 1);
}
