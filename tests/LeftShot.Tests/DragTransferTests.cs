using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using LeftShot.Core;
using LeftShot.Services;
using NUnit.Framework;

namespace LeftShot.Tests;

[TestFixture, Apartment(ApartmentState.STA), NonParallelizable]
public sealed class DragTransferTests
{
    private string directory = "";

    [SetUp]
    public void CreateTestDirectory()
    {
        string parent = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "test-temp"));
        directory = Path.GetFullPath(Path.Combine(parent, Guid.NewGuid().ToString("N")));
        Assert.That(directory.StartsWith(parent + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase), Is.True);
        Directory.CreateDirectory(directory);
    }

    [TearDown]
    public void RemoveOnlyTestDirectory()
    {
        string parent = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "test-temp")) + Path.DirectorySeparatorChar;
        if (Path.GetFullPath(directory).StartsWith(parent, StringComparison.OrdinalIgnoreCase) && Directory.Exists(directory))
            Directory.Delete(directory, true);
    }

    [Test]
    public void StoreConstructionDoesNotCreateAnyImageOrDirectory()
    {
        string destination = Path.Combine(directory, "drag-out");
        _ = new DragTransferStore(destination);
        Assert.That(Directory.Exists(destination), Is.False);
        Assert.That(Directory.GetFileSystemEntries(directory), Is.Empty);
    }

    [Test]
    public void PayloadContainsOnlyExportPixelsInFileBitmapAndPngFormats()
    {
        const string sourceTag = "/tEXt/{str=Description}";
        var metadata = new BitmapMetadata("png");
        metadata.SetQuery(sourceTag, "private source description");
        BitmapFrame source = BitmapFrame.Create(CoreTests.Image(8, 7), CoreTests.Image(2, 2), metadata, null);
        var document = new EditDocument(source);
        document.Commit(new DocumentState(new Rect(1, 1, 6, 5), new[]
        {
            new Annotation(Guid.NewGuid(), ToolKind.Redaction, new Rect(2, 2, 3, 2), Colors.Transparent, 1)
        }));
        using DragTransferLease lease = new DragTransferStore(directory).Prepare(DocumentRenderer.Render(document));
        Assert.That(lease.Data.GetFormats(false), Is.EquivalentTo(new[] { DataFormats.FileDrop, DataFormats.Bitmap, "PNG" }));
        Assert.That(lease.Data.GetData(DataFormats.FileDrop, false), Is.EqualTo(new[] { lease.FilePath }));
        Assert.That(Path.GetDirectoryName(lease.FilePath), Is.EqualTo(directory));
        Assert.That(Path.GetExtension(lease.FilePath), Is.EqualTo(".png"));
        var png = (MemoryStream)lease.Data.GetData("PNG", false);
        Assert.That(png.ToArray(), Is.EqualTo(File.ReadAllBytes(lease.FilePath)));
        png.Position = 0;
        BitmapFrame encoded = BitmapDecoder.Create(png, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad).Frames[0];
        using var file = File.OpenRead(lease.FilePath);
        BitmapFrame saved = BitmapDecoder.Create(file, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad).Frames[0];
        var bitmap = (BitmapSource)lease.Data.GetData(DataFormats.Bitmap, false);
        foreach (BitmapSource output in new BitmapSource[] { encoded, saved, bitmap })
        {
            Assert.That(output.PixelWidth, Is.EqualTo(6));
            Assert.That(output.PixelHeight, Is.EqualTo(5));
            Assert.That(output is BitmapFrame frame ? frame.Thumbnail : null, Is.Null);
            Assert.That(output.Metadata is BitmapMetadata tags && tags.ContainsQuery(sourceTag), Is.False);
            byte[] actual = new byte[6 * 5 * 4];
            new FormatConvertedBitmap(output, PixelFormats.Bgra32, null, 0).CopyPixels(actual, 6 * 4, 0);
            for (int y = 0; y < 5; y++)
            for (int x = 0; x < 6; x++)
            {
                int offset = (y * 6 + x) * 4;
                byte expected = x >= 1 && x < 4 && y >= 1 && y < 3 ? (byte)0 : (byte)255;
                Assert.That(actual.Skip(offset).Take(4), Is.EqualTo(new[] { expected, expected, expected, (byte)255 }));
            }
        }
    }

    [Test]
    public void AcceptedDropCanReadFileAndPngAfterDragScopeEnds()
    {
        var store = new DragTransferStore(directory);
        DragTransferLease lease = store.Prepare(CoreTests.Image(17, 13));
        byte[] expected = File.ReadAllBytes(lease.FilePath);
        lease.Complete(true);
        lease.Dispose();
        lease.Dispose();
        Assert.That(File.ReadAllBytes(lease.FilePath), Is.EqualTo(expected));
        Assert.That(((MemoryStream)lease.Data.GetData("PNG", false)).CanRead, Is.True);
        using DragTransferLease later = new DragTransferStore(directory).Prepare(CoreTests.Image());
        Assert.That(File.ReadAllBytes(lease.FilePath), Is.EqualTo(expected));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void CancelledOrInterruptedDragDeletesOnlyItsTemporaryFile(bool complete)
    {
        string unrelated = Path.Combine(directory, "saved-by-user.png");
        File.WriteAllText(unrelated, "keep me");
        DragTransferLease lease = new DragTransferStore(directory + Path.DirectorySeparatorChar).Prepare(CoreTests.Image());
        if (complete) lease.Complete(false);
        lease.Dispose();
        Assert.That(File.Exists(lease.FilePath), Is.False);
        Assert.That(File.ReadAllText(unrelated), Is.EqualTo("keep me"));
    }

    [Test]
    public void ExpirySweepsOnlyOwnedFilesAndNeverAnActiveLease()
    {
        var store = new DragTransferStore(directory);
        using DragTransferLease active = store.Prepare(CoreTests.Image());
        string expired;
        using (DragTransferLease accepted = store.Prepare(CoreTests.Image()))
        {
            expired = accepted.FilePath;
            accepted.Complete(true);
        }
        string unrelated = Path.Combine(directory, ".leftshot-drag-user-image.png");
        File.WriteAllText(unrelated, "keep me");
        string child = Path.Combine(directory, "child");
        Directory.CreateDirectory(child);
        string nested = Path.Combine(child, Path.GetFileName(expired));
        File.WriteAllText(nested, "keep nested");
        DateTime old = DateTime.UtcNow.AddDays(-2);
        foreach (string path in new[] { active.FilePath, expired, unrelated, nested }) File.SetLastWriteTimeUtc(path, old);
        using DragTransferLease next = store.Prepare(CoreTests.Image());
        Assert.That(File.Exists(expired), Is.False);
        Assert.That(File.Exists(active.FilePath), Is.True);
        Assert.That(File.ReadAllText(unrelated), Is.EqualTo("keep me"));
        Assert.That(File.ReadAllText(nested), Is.EqualTo("keep nested"));
        active.Complete(true);
        Assert.That(File.GetLastWriteTimeUtc(active.FilePath), Is.GreaterThan(old.AddDays(1)));
    }

    [Test]
    public void FileCountBudgetRefusesAnotherDragWithoutDeletingRecentTransfers()
    {
        var store = new DragTransferStore(directory);
        for (int i = 0; i < 32; i++)
        {
            using DragTransferLease lease = store.Prepare(CoreTests.Image(1, 1));
            lease.Complete(true);
        }
        string[] original = Directory.GetFiles(directory);
        Assert.Throws<IOException>(() => store.Prepare(CoreTests.Image(1, 1)));
        Assert.That(Directory.GetFiles(directory), Is.EquivalentTo(original));
        File.SetLastWriteTimeUtc(original[0], DateTime.UtcNow.AddDays(-2));
        using DragTransferLease next = store.Prepare(CoreTests.Image(1, 1));
        Assert.That(Directory.GetFiles(directory), Has.Length.EqualTo(32));
        Assert.That(File.Exists(original[0]), Is.False);
    }

    [Test]
    public void ByteBudgetRefusesAnotherDragAndRemovesItsPartialTransfer()
    {
        string retained = Path.Combine(directory, ".leftshot-drag-" + Guid.NewGuid().ToString("N") + ".png");
        using (var file = new FileStream(retained, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            file.SetLength(256L * 1024 * 1024 - 1);
        Assert.Throws<IOException>(() => new DragTransferStore(directory).Prepare(CoreTests.Image(1, 1)));
        Assert.That(Directory.GetFiles(directory), Is.EqualTo(new[] { retained }));
    }

    [Test]
    public void UnwritableDestinationDoesNotReturnAPayloadOrDamageExistingFile()
    {
        string blocked = Path.Combine(directory, "drag-out");
        File.WriteAllText(blocked, "keep me");
        var store = new DragTransferStore(blocked);
        Exception? error = Assert.Catch(() => store.Prepare(CoreTests.Image()));
        Assert.That(error, Is.InstanceOf<IOException>().Or.InstanceOf<UnauthorizedAccessException>());
        Assert.That(File.ReadAllText(blocked), Is.EqualTo("keep me"));
        Assert.That(Directory.GetFiles(directory), Is.EqualTo(new[] { blocked }));
    }
}
