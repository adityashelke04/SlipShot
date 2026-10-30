using System.Reflection;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using LeftShot.Core;
using LeftShot.UI;
using NUnit.Framework;

namespace LeftShot.Tests;

[TestFixture]
public sealed class EditorTests
{
    // Exercise coordinate logic without constructing or showing an editor window.
    private static Rect Resize(Rect bounds, int handle, Point point) => (Rect)typeof(EditorWindow)
        .GetMethod("ResizeBounds", BindingFlags.NonPublic | BindingFlags.Static)!
        .Invoke(null, new object[] { bounds, handle, point })!;

    [TestCase(0, 5, 8, 35, 42)]
    [TestCase(1, 10, 8, 30, 42)]
    [TestCase(2, 10, 8, 1, 42)]
    [TestCase(3, 10, 20, 1, 30)]
    [TestCase(4, 10, 20, 1, 1)]
    [TestCase(5, 10, 20, 30, 1)]
    [TestCase(6, 5, 20, 35, 1)]
    [TestCase(7, 5, 20, 35, 30)]
    public void Each_handle_changes_only_its_adjacent_edges(int handle, double x, double y, double width, double height)
    {
        Rect result = Resize(new Rect(10, 20, 30, 30), handle, new Point(5, 8));
        Assert.That(result, Is.EqualTo(new Rect(x, y, width, height)));
    }

    [Test]
    public void Dragging_past_opposite_corner_keeps_one_pixel_and_does_not_flip()
    {
        Rect result = Resize(new Rect(10, 20, 30, 40), 0, new Point(100, 100));
        Assert.That(result, Is.EqualTo(new Rect(39, 59, 1, 1)));
    }

    [Test]
    public void Horizontal_line_can_be_resized_into_diagonal_line()
    {
        Rect result = Resize(new Rect(10, 20, 30, 0), 4, new Point(70, 50));
        Assert.That(result, Is.EqualTo(new Rect(10, 20, 60, 30)));
    }

    [TestCase(1, 1, 1)]
    [TestCase(1, 1.25, 0.8)]
    [TestCase(1, 1.5, 2.0 / 3)]
    [TestCase(1, 2, 0.5)]
    [TestCase(0.001, 2, 0.01)]
    [TestCase(16, 1.25, 6.4)]
    public void Physical_pixel_zoom_converts_monitor_scale_and_clamps_in_physical_pixels(double physicalZoom, double dpiScale, double expectedDipZoom)
    {
        double result = (double)typeof(EditorWindow)
            .GetMethod("DipZoom", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { physicalZoom, dpiScale })!;
        Assert.That(result, Is.EqualTo(expectedDipZoom).Within(0.000001));
    }

    [Test, Apartment(ApartmentState.STA)]
    public void View_model_reports_history_changes_and_disables_undo_while_busy()
    {
        var document = new EditDocument(CoreTests.Image());
        using var viewModel = new DocumentViewModel(document);
        var changed = new List<string?>();
        viewModel.PropertyChanged += (_, args) => changed.Add(args.PropertyName);
        document.Commit(new DocumentState(new Rect(0, 0, 50, 40), document.State.Annotations));
        Assert.That(viewModel.IsDirty, Is.True);
        Assert.That(viewModel.CanUndo, Is.True);
        Assert.That(changed, Does.Contain(nameof(DocumentViewModel.IsDirty)));
        viewModel.IsBusy = true;
        Assert.That(viewModel.CanUndo, Is.False);
        viewModel.IsBusy = false;
        document.Undo();
        Assert.That(viewModel.CanRedo, Is.True);
        Assert.That(viewModel.IsDirty, Is.False);
    }
}
