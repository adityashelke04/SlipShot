using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace LeftShot.Core;

public enum ToolKind { Select, Crop, Arrow, Line, Rectangle, Ellipse, Pen, Highlighter, Text, Step, Redaction, Pan }

public sealed record Annotation(Guid Id, ToolKind Kind, Rect Bounds, Color Color,
    double StrokeWidth, bool Filled = false, string Text = "", double FontSize = 24,
    int Number = 1, IReadOnlyList<Point>? Points = null);

public sealed record DocumentState(Rect Crop, IReadOnlyList<Annotation> Annotations);

public sealed class EditDocument
{
    private readonly List<DocumentState> history = new();
    private readonly List<long> historyCosts = new();
    private long retainedCost;
    private int position;
    private readonly WeakReference<DocumentState> saved;

    public EditDocument(BitmapSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.PixelWidth < 1 || source.PixelHeight < 1)
            throw new ArgumentException("The image must contain pixels.", nameof(source));
        Source = source.IsFrozen ? source : source.Clone();
        Source.Freeze();
        var initial = new DocumentState(new Rect(0, 0, source.PixelWidth, source.PixelHeight), Array.Empty<Annotation>());
        saved = new WeakReference<DocumentState>(initial);
        history.Add(initial);
        historyCosts.Add(0);
    }

    public BitmapSource Source { get; }
    public DocumentState State => history[position];
    public bool CanUndo => position > 0;
    public bool CanRedo => position < history.Count - 1;
    public bool IsDirty => !saved.TryGetTarget(out DocumentState? state) || !ReferenceEquals(State, state);
    public event Action? Changed;

    public void Commit(DocumentState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        Rect crop = GeometryUtil.PixelBounds(state.Crop, new Rect(0, 0, Source.PixelWidth, Source.PixelHeight));
        if (crop.IsEmpty || crop.Width < 1 || crop.Height < 1)
            throw new ArgumentException("The crop must contain at least one image pixel.", nameof(state));
        long cost = Validate(state.Annotations);
        Annotation[] annotations = state.Annotations.Select(annotation => annotation with
        {
            Points = annotation.Points is null ? null : Array.AsReadOnly(annotation.Points.ToArray())
        }).ToArray();
        var snapshot = new DocumentState(crop, Array.AsReadOnly(annotations));
        if (CanRedo)
        {
            retainedCost -= historyCosts.Skip(position + 1).Sum();
            historyCosts.RemoveRange(position + 1, history.Count - position - 1);
            history.RemoveRange(position + 1, history.Count - position - 1);
        }
        history.Add(snapshot);
        historyCosts.Add(cost);
        retainedCost += cost;
        // A long drawing cannot multiply into eighty full point arrays.
        while (history.Count > 1 && (history.Count > 80 || retainedCost > 1_000_000))
        {
            retainedCost -= historyCosts[0];
            historyCosts.RemoveAt(0);
            history.RemoveAt(0);
        }
        position = history.Count - 1;
        Changed?.Invoke();
    }

    public void Undo()
    {
        if (!CanUndo) return;
        position--;
        Changed?.Invoke();
    }

    public void Redo()
    {
        if (!CanRedo) return;
        position++;
        Changed?.Invoke();
    }

    public void MarkSaved()
    {
        saved.SetTarget(State);
        Changed?.Invoke();
    }

    private static long Validate(IReadOnlyList<Annotation> annotations)
    {
        if (annotations is null || annotations.Count > 1000)
            throw new ArgumentException("A capture can contain up to 1,000 annotations.");
        long points = 0, characters = 0;
        foreach (Annotation annotation in annotations)
        {
            if (annotation is null || !Enum.IsDefined(annotation.Kind) || annotation.Bounds.IsEmpty
                || !Finite(annotation.Bounds.X) || !Finite(annotation.Bounds.Y)
                || !Finite(annotation.Bounds.Width) || !Finite(annotation.Bounds.Height)
                || !double.IsFinite(annotation.StrokeWidth) || annotation.StrokeWidth <= 0 || annotation.StrokeWidth > 1000
                || !double.IsFinite(annotation.FontSize) || annotation.FontSize <= 0 || annotation.FontSize > 1000
                || annotation.Text is null)
                throw new ArgumentException("Annotation coordinates and sizes must be finite and within supported limits.");
            points += annotation.Points?.Count ?? 0;
            characters += annotation.Text.Length;
            if (points > 100000 || characters > 100000)
                throw new ArgumentException("This capture reached its drawing or text memory limit. Save it and start a new capture.");
            if (annotation.Points is not null)
                foreach (Point point in annotation.Points)
                    if (!Finite(point.X) || !Finite(point.Y)) throw new ArgumentException("Drawing points must be finite.");
        }
        return points * 2 + characters + annotations.Count * 16L;
    }

    private static bool Finite(double value) => double.IsFinite(value) && Math.Abs(value) <= 1_000_000;
}
