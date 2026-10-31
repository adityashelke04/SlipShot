using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SlipShot.Core;
using SlipShot.UI;
using NUnit.Framework;

namespace SlipShot.Tests;

[TestFixture, Apartment(ApartmentState.STA), NonParallelizable]
public sealed class EditorAuditTests
{
    [Test]
    public void Invalid_hex_color_with_internal_space_does_not_recolor_selected_annotation()
    {
        _ = Application.Current ?? new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        Theme.Configure("Light", true);
        var document = new EditDocument(CoreTests.Image());
        var annotation = new Annotation(Guid.NewGuid(), ToolKind.Rectangle, new Rect(10, 10, 20, 20), Colors.Red, 4);
        document.Commit(new DocumentState(document.State.Crop, new[] { annotation }));
        var editor = new EditorWindow(document, _ => Task.FromResult(false), _ => Task.CompletedTask, _ => { });
        try
        {
            ((DocumentViewModel)editor.DataContext).SelectedAnnotationId = annotation.Id;
            var hex = (TextBox)typeof(EditorWindow).GetField("_hex", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor)!;
            hex.Text = "# FFFFF";
            DocumentState before = document.State;

            typeof(EditorWindow).GetMethod("ApplyHex", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(editor, null);

            Assert.That(document.State, Is.SameAs(before));
            Assert.That(hex.Text, Is.EqualTo("# FFFFF"));
        }
        finally { document.MarkSaved(); editor.Close(); }
    }

    [Test]
    public void Horizontal_line_endpoint_selects_the_handle_that_can_extend_downward()
    {
        _ = Application.Current ?? new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        Theme.Configure("Light", true);
        var editor = new EditorWindow(new EditDocument(CoreTests.Image()), _ => Task.FromResult(false),
            _ => Task.CompletedTask, _ => { });
        try
        {
            int handle = (int)typeof(EditorWindow).GetMethod("HandleAt", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(editor, new object[] { new Rect(10, 20, 30, 0), new Point(40, 20) })!;
            Rect resized = EditorWindow.ResizeBounds(new Rect(10, 20, 30, 0), handle, new Point(70, 50));
            Assert.That(resized, Is.EqualTo(new Rect(10, 20, 60, 30)));
        }
        finally { editor.Close(); }
    }

    [TestCase(ToolKind.Line, false)]
    [TestCase(ToolKind.Arrow, false)]
    [TestCase(ToolKind.Line, true)]
    [TestCase(ToolKind.Arrow, true)]
    public void Flat_lines_resize_into_diagonals_with_endpoints_at_both_corners(ToolKind kind, bool vertical)
    {
        var original = new Annotation(Guid.NewGuid(), kind,
            vertical ? new Rect(10, 20, 0, 30) : new Rect(10, 20, 30, 0), Colors.Red, 4,
            Points: new[] { new Point(10, 20), vertical ? new Point(10, 50) : new Point(40, 20) });

        Annotation resized = GeometryUtil.Resize(original, new Rect(10, 20, 60, 30));

        Assert.That(resized.Points![0], Is.EqualTo(new Point(10, 20)));
        Assert.That(resized.Points[1], Is.EqualTo(new Point(70, 50)));
        var document = new EditDocument(CoreTests.Image());
        document.Commit(new DocumentState(document.State.Crop, new[] { resized }));
        var pixels = new byte[4];
        DocumentRenderer.Render(document).CopyPixels(new Int32Rect(40, 35, 1, 1), pixels, 4, 0);
        Assert.That(pixels[2], Is.EqualTo(255));
        Assert.That(pixels[1], Is.LessThan(50), "The exported line must pass through the diagonal midpoint.");
    }

    [Test]
    public void Arrowhead_can_be_selected_where_its_filled_wing_is_visible()
    {
        var arrow = new Annotation(Guid.NewGuid(), ToolKind.Arrow, new Rect(20, 40, 60, 0),
            Colors.Red, 20, Points: new[] { new Point(20, 40), new Point(80, 40) });

        Assert.That(GeometryUtil.HitTest(arrow, new Point(25, 20), 6), Is.True);
        Assert.That(GeometryUtil.HitTest(arrow, new Point(25, 5), 6), Is.False);
        Assert.That(GeometryUtil.HitTest(arrow with { Kind = ToolKind.Line }, new Point(25, 20), 6), Is.False);
    }

    [Test]
    public void Numbered_step_does_not_select_the_blank_corners_outside_its_circle()
    {
        var step = new Annotation(Guid.NewGuid(), ToolKind.Step, new Rect(10, 10, 36, 36), Colors.Red, 4);
        var document = new EditDocument(CoreTests.Image());
        document.Commit(new DocumentState(document.State.Crop, new[] { step }));
        var pixel = new byte[4];
        DocumentRenderer.Render(document).CopyPixels(new Int32Rect(10, 10, 1, 1), pixel, 4, 0);
        Assert.That(pixel, Is.EqualTo(new byte[] { 255, 255, 255, 255 }));

        Assert.That(GeometryUtil.HitTest(step, new Point(10, 10), 6), Is.False);
        Assert.That(GeometryUtil.HitTest(step, new Point(28, 28), 6), Is.True);
        Assert.That(GeometryUtil.HitTest(step, new Point(10, 28), 6), Is.True);
    }
}
