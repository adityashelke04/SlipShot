using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SlipShot.Core;
using NUnit.Framework;

namespace SlipShot.Tests;

[TestFixture, Apartment(ApartmentState.STA), NonParallelizable]
public sealed class CoreTests
{
    internal static BitmapSource Image(int width = 100, int height = 80)
    {
        byte[] bytes = Enumerable.Repeat((byte)255, width * height * 4).ToArray();
        return BitmapSource.Create(width, height, 144, 144, PixelFormats.Bgra32, null, bytes, width * 4);
    }

    private static Color Pixel(BitmapSource bitmap, int x, int y)
    {
        var converted = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
        byte[] bytes = new byte[4];
        converted.CopyPixels(new Int32Rect(x, y, 1, 1), bytes, 4, 0);
        return Color.FromArgb(bytes[3], bytes[2], bytes[1], bytes[0]);
    }

    private static Annotation Shape(ToolKind kind) => new(Guid.NewGuid(), kind,
        new Rect(10, 10, 50, 40), Colors.Red, 6, true,
        Text: "A", FontSize: 28, Points: new[] { new Point(10, 50), new Point(60, 10) });

    [Test]
    public void GeometryPreservesNegativeMonitorOriginsAndIndependentZoom()
    {
        Point origin = new(-1920, -240);
        Assert.That(GeometryUtil.ToPixels(new Point(80, 40), 1.5, origin), Is.EqualTo(new Point(-1800, -180)));
        Assert.That(GeometryUtil.ToDips(new Point(-1800, -180), 1.5, origin), Is.EqualTo(new Point(80, 40)));
        Assert.That(GeometryUtil.ToDips(new Point(120, 60), 2, new Point(0, 0)), Is.EqualTo(new Point(60, 30)));
        Assert.That(GeometryUtil.Normalize(new Point(70, 60), new Point(-5, 5), new Rect(0, 0, 100, 80)), Is.EqualTo(new Rect(0, 5, 70, 55)));
        Assert.Throws<ArgumentOutOfRangeException>(() => GeometryUtil.ToPixels(new Point(), 0, origin));
        Assert.Throws<ArgumentException>(() => GeometryUtil.ToPixels(new Point(double.NaN, 5), 1, origin));
        Assert.That(GeometryUtil.Normalize(new Point(double.NaN, 5), new Point(10, 10), new Rect(0, 0, 100, 80)), Is.EqualTo(Rect.Empty));
    }

    [Test]
    public void HitTestingUsesLineDirectionAndMoveResizeTransformPoints()
    {
        Annotation line = Shape(ToolKind.Line);
        Assert.That(GeometryUtil.HitTest(line, new Point(35, 30), 2), Is.True);
        Assert.That(GeometryUtil.HitTest(line, new Point(15, 15), 2), Is.False);
        Annotation moved = GeometryUtil.Move(line, new Vector(20, 5));
        Assert.That(moved.Points![0], Is.EqualTo(new Point(30, 55)));
        Annotation resized = GeometryUtil.Resize(line, new Rect(20, 20, 100, 80));
        Assert.That(resized.Points![1], Is.EqualTo(new Point(120, 20)));
    }

    [Test]
    public void HistorySnapshotsDoNotRetainMutableListsOrCopyTheSource()
    {
        var doc = new EditDocument(Image());
        BitmapSource source = doc.Source;
        var points = new List<Point> { new(10, 50), new(60, 10) };
        var annotations = new List<Annotation> { Shape(ToolKind.Line) with { Points = points } };
        doc.Commit(new DocumentState(doc.State.Crop, annotations));
        points[0] = new Point(99, 99);
        annotations.Clear();
        Assert.That(doc.State.Annotations, Has.Count.EqualTo(1));
        Assert.That(doc.State.Annotations[0].Points![0], Is.EqualTo(new Point(10, 50)));
        Assert.That(doc.Source, Is.SameAs(source));
        Assert.That(source.IsFrozen, Is.True);
        Assert.That(doc.IsDirty, Is.True);
        doc.MarkSaved();
        doc.Undo();
        Assert.That(doc.IsDirty, Is.True);
        doc.Redo();
        Assert.That(doc.IsDirty, Is.False);
        doc.Undo();
        doc.Commit(new DocumentState(new Rect(0, 0, 10, 10), Array.Empty<Annotation>()));
        Assert.That(doc.CanRedo, Is.False);
    }

    [Test]
    public void HistoryBoundsRetainedStatesAndRaisesChanges()
    {
        var doc = new EditDocument(Image());
        int changes = 0;
        doc.Changed += () => changes++;
        for (int i = 0; i < 100; i++)
            doc.Commit(new DocumentState(doc.State.Crop, new[] { Shape(ToolKind.Step) with { Number = i } }));
        int undoCount = 0;
        while (doc.CanUndo) { doc.Undo(); undoCount++; }
        Assert.That(undoCount, Is.EqualTo(79));
        Assert.That(changes, Is.EqualTo(179));
    }

    [Test]
    public void CropExportsPhysicalPixelsRegardlessOfSourceDpi()
    {
        var doc = new EditDocument(Image());
        doc.Commit(new DocumentState(new Rect(10.2, 9.1, 19.2, 10.3), Array.Empty<Annotation>()));
        BitmapSource result = DocumentRenderer.Render(doc);
        Assert.That(result.PixelWidth, Is.EqualTo(20));
        Assert.That(result.PixelHeight, Is.EqualTo(11));
        Assert.That(result.DpiX, Is.EqualTo(96));
        Assert.That(result.IsFrozen, Is.True);
        Assert.That(Pixel(result, 19, 10), Is.EqualTo(Colors.White));
    }

    [TestCase(ToolKind.Arrow)]
    [TestCase(ToolKind.Line)]
    [TestCase(ToolKind.Rectangle)]
    [TestCase(ToolKind.Ellipse)]
    [TestCase(ToolKind.Pen)]
    [TestCase(ToolKind.Highlighter)]
    [TestCase(ToolKind.Text)]
    [TestCase(ToolKind.Step)]
    [TestCase(ToolKind.Redaction)]
    public void EveryDrawingToolChangesPixels(ToolKind kind)
    {
        var doc = new EditDocument(Image());
        doc.Commit(new DocumentState(doc.State.Crop, new[] { Shape(kind) }));
        BitmapSource rendered = DocumentRenderer.Render(doc);
        byte[] bytes = new byte[100 * 80 * 4];
        rendered.CopyPixels(bytes, 100 * 4, 0);
        Assert.That(bytes.Where((_, index) => index % 4 != 3).Any(value => value < 240), Is.True);
        Assert.That(Pixel(rendered, 90, 70), Is.EqualTo(Colors.White));
    }

    [Test]
    public void RedactionIsOpaqueAndRemainsAboveLaterAnnotationsInPng()
    {
        var doc = new EditDocument(Image());
        var redaction = Shape(ToolKind.Redaction) with { Color = Colors.Transparent, Bounds = new Rect(10.2, 10.2, 20.2, 20.2) };
        doc.Commit(new DocumentState(new Rect(5, 5, 40, 40), new[] { redaction, Shape(ToolKind.Rectangle) }));
        var png = new PngBitmapEncoder();
        png.Frames.Add(BitmapFrame.Create(DocumentRenderer.Render(doc)));
        using var stream = new MemoryStream();
        png.Save(stream);
        stream.Position = 0;
        BitmapSource decoded = BitmapFrame.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        Assert.That(Pixel(decoded, 5, 5), Is.EqualTo(Colors.Black));
        Assert.That(Pixel(decoded, 25, 25), Is.EqualTo(Colors.Black));
        Assert.That(Pixel(decoded, 0, 0), Is.EqualTo(Colors.White));
    }

    [Test]
    public void DescendingLineUsesEndpointsAndHighlighterIsTranslucent()
    {
        var doc = new EditDocument(Image());
        doc.Commit(new DocumentState(doc.State.Crop, new[] { Shape(ToolKind.Line) }));
        BitmapSource line = DocumentRenderer.Render(doc);
        Assert.That(Pixel(line, 35, 30), Is.EqualTo(Colors.Red));
        Assert.That(Pixel(line, 15, 15), Is.EqualTo(Colors.White));
        doc.Commit(new DocumentState(doc.State.Crop, new[] { Shape(ToolKind.Highlighter) }));
        Color highlight = Pixel(DocumentRenderer.Render(doc), 35, 30);
        Assert.That(highlight.R, Is.EqualTo(255));
        Assert.That(highlight.G, Is.InRange(150, 180));
        Assert.That(highlight.A, Is.EqualTo(255));
    }

    [Test]
    public void CropCannotEscapeSourceOrBecomeEmpty()
    {
        var doc = new EditDocument(Image());
        doc.Commit(new DocumentState(new Rect(-20, -20, 200, 200), Array.Empty<Annotation>()));
        Assert.That(doc.State.Crop, Is.EqualTo(new Rect(0, 0, 100, 80)));
        Assert.Throws<ArgumentException>(() => doc.Commit(new DocumentState(new Rect(200, 200, 20, 20), Array.Empty<Annotation>())));
        Assert.Throws<ArgumentException>(() => doc.Commit(new DocumentState(new Rect(10.2, 10.2, 0, 0), Array.Empty<Annotation>())));
        Assert.That(doc.State.Crop, Is.EqualTo(new Rect(0, 0, 100, 80)));
    }

    [Test]
    public void PreviewQueueDropsOldestPendingAndClearsReferences()
    {
        var queue = new PreviewQueue<string>();
        foreach (string item in new[] { "one", "two", "three", "four", "five" }) queue.Push(item);
        Assert.That(queue.Current, Is.EqualTo("one"));
        Assert.That(queue.PendingCount, Is.EqualTo(3));
        Assert.That(queue.Items, Is.EqualTo(new[] { "one", "three", "four", "five" }));
        queue.Dismiss();
        Assert.That(queue.Current, Is.EqualTo("three"));
        queue.Dismiss(); queue.Dismiss(); queue.Dismiss();
        Assert.That(queue.Current, Is.Null);
        queue.Push("six"); queue.Push("seven"); queue.Clear();
        Assert.That(queue.Current, Is.Null);
        Assert.That(queue.PendingCount, Is.Zero);
        Assert.That(queue.Items, Is.Empty);
    }

    [Test]
    public void InvalidAnnotationsAndMemoryBudgetsLeaveDocumentUntouched()
    {
        var doc = new EditDocument(Image());
        DocumentState before = doc.State;
        foreach (Annotation invalid in new[]
        {
            Shape(ToolKind.Line) with { StrokeWidth = double.NaN },
            Shape(ToolKind.Text) with { FontSize = double.PositiveInfinity },
            Shape(ToolKind.Pen) with { Points = new[] { new Point(double.NaN, 5) } },
            Shape(ToolKind.Rectangle) with { Bounds = new Rect(double.PositiveInfinity, 0, 5, 5) },
            Shape(ToolKind.Text) with { Text = new string('a', 100001) },
            Shape(ToolKind.Pen) with { Points = new Point[100001] }
        })
            Assert.Throws<ArgumentException>(() => doc.Commit(new DocumentState(before.Crop, new[] { invalid })));
        Assert.Throws<ArgumentException>(() => doc.Commit(new DocumentState(before.Crop, Enumerable.Repeat(Shape(ToolKind.Step), 1001).ToArray())));
        Assert.That(doc.State, Is.SameAs(before));
        Assert.That(doc.CanUndo, Is.False);
    }

    [Test]
    public void LargeHistoriesAreBoundedByPointMemoryAsWellAsStateCount()
    {
        var doc = new EditDocument(Image());
        var annotation = Shape(ToolKind.Pen) with { Points = new Point[100000] };
        for (int i = 0; i < 12; i++) doc.Commit(new DocumentState(doc.State.Crop, new[] { annotation }));
        int retained = 1;
        while (doc.CanUndo) { retained++; doc.Undo(); }
        Assert.That(retained, Is.LessThanOrEqualTo(5));
    }

    [Test]
    public void CaptureGateRejectsReentryAndCanceledOrStaleDelivery()
    {
        var gate = new CaptureGate();
        Assert.That(gate.TryBegin(out long first), Is.True);
        Assert.That(gate.IsCurrent(first), Is.True);
        Assert.That(gate.TryBegin(out _), Is.False);
        gate.Cancel();
        Assert.That(gate.IsCurrent(first), Is.False);
        Assert.That(gate.TryBegin(out _), Is.False);
        Assert.That(gate.TryComplete(first), Is.False);
        Assert.That(gate.TryBegin(out long second), Is.True);
        Assert.That(gate.IsCurrent(first), Is.False);
        Assert.That(gate.IsCurrent(second), Is.True);
        Assert.That(gate.TryComplete(first), Is.False);
        Assert.That(gate.TryBegin(out _), Is.False);
        Assert.That(gate.TryComplete(second), Is.True);
        Assert.That(gate.IsCurrent(second), Is.False);
        Assert.That(gate.TryComplete(second), Is.False);
        Assert.That(gate.TryBegin(out _), Is.True);
    }

    [Test]
    public void PreviewTimePausesWithoutResetAndReducedMotionClosesImmediately()
    {
        var lifetime = new PreviewLifetime(TimeSpan.FromSeconds(8));
        Assert.That(lifetime.Advance(TimeSpan.FromSeconds(3), false), Is.False);
        Assert.That(lifetime.Advance(TimeSpan.FromSeconds(3), true), Is.False);
        Assert.That(lifetime.Remaining, Is.EqualTo(TimeSpan.FromSeconds(5)));
        Assert.That(lifetime.Advance(TimeSpan.FromSeconds(5), false), Is.True);
        Assert.That(lifetime.Remaining, Is.EqualTo(TimeSpan.Zero));
        Assert.That(lifetime.BeginDismiss(false), Is.EqualTo(TimeSpan.Zero));
        Assert.That(lifetime.IsDismissing, Is.True);
        Assert.That(lifetime.BeginDismiss(true), Is.EqualTo(TimeSpan.Zero));
        var animated = new PreviewLifetime(TimeSpan.FromSeconds(8));
        Assert.That(animated.BeginDismiss(true), Is.EqualTo(TimeSpan.FromMilliseconds(120)));
        Assert.Throws<ArgumentOutOfRangeException>(() => animated.Advance(TimeSpan.FromSeconds(-1), false));
    }

    [TestCase(ToolKind.Rectangle, 20, 20, 255, 0, 0)]
    [TestCase(ToolKind.Ellipse, 35, 30, 255, 0, 0)]
    [TestCase(ToolKind.Pen, 35, 30, 255, 0, 0)]
    [TestCase(ToolKind.Arrow, 50, 15, 255, 0, 0)]
    [TestCase(ToolKind.Step, 22, 30, 255, 0, 0)]
    [TestCase(ToolKind.Redaction, 20, 20, 0, 0, 0)]
    public void DrawingToolsPaintTheirExpectedInterior(ToolKind kind, int x, int y, byte r, byte g, byte b)
    {
        var doc = new EditDocument(Image());
        doc.Commit(new DocumentState(doc.State.Crop, new[] { Shape(kind) }));
        Assert.That(Pixel(DocumentRenderer.Render(doc), x, y), Is.EqualTo(Color.FromRgb(r, g, b)));
    }

    [Test]
    public void TextPaintsGlyphsNearItsAnchorAndResizedRedactionRemainsOpaque()
    {
        var doc = new EditDocument(Image());
        doc.Commit(new DocumentState(doc.State.Crop, new[] { Shape(ToolKind.Text) }));
        BitmapSource text = DocumentRenderer.Render(doc);
        int redPixels = 0;
        for (int y = 10; y < 45; y++) for (int x = 10; x < 40; x++)
        {
            Color pixel = Pixel(text, x, y);
            if (pixel.R > 240 && pixel.G < 100 && pixel.B < 100) redPixels++;
        }
        Assert.That(redPixels, Is.GreaterThan(20));
        Annotation resized = GeometryUtil.Resize(Shape(ToolKind.Redaction), new Rect(20.2, 30.2, 5.1, 4.1));
        doc.Commit(new DocumentState(doc.State.Crop, new[] { resized }));
        BitmapSource redaction = DocumentRenderer.Render(doc);
        Assert.That(Pixel(redaction, 20, 30), Is.EqualTo(Colors.Black));
        Assert.That(Pixel(redaction, 25, 34), Is.EqualTo(Colors.Black));
        Assert.That(Pixel(redaction, 19, 30), Is.EqualTo(Colors.White));
    }

    [TestCase(255, 255, 255)]
    [TestCase(255, 255, 0)]
    public void StepNumberRemainsReadableOnLightFill(byte r, byte g, byte b)
    {
        var doc = new EditDocument(Image());
        doc.Commit(new DocumentState(doc.State.Crop, new[] { Shape(ToolKind.Step) with { Color = Color.FromRgb(r, g, b) } }));
        BitmapSource rendered = DocumentRenderer.Render(doc);
        int darkPixels = 0;
        for (int y = 15; y < 45; y++) for (int x = 25; x < 45; x++)
        {
            Color pixel = Pixel(rendered, x, y);
            if (pixel.R < 60 && pixel.G < 60 && pixel.B < 60) darkPixels++;
        }
        Assert.That(darkPixels, Is.GreaterThan(10));
    }
}
