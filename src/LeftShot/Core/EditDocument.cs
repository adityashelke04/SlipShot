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
        Annotation[] annotations = state.Annotations.Select(annotation => annotation with
        {
            Points = annotation.Points is null ? null : Array.AsReadOnly(annotation.Points.ToArray())
        }).ToArray();
        var snapshot = new DocumentState(crop, Array.AsReadOnly(annotations));
        if (CanRedo) history.RemoveRange(position + 1, history.Count - position - 1);
        history.Add(snapshot);
        if (history.Count > 80) history.RemoveAt(0);
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
}
