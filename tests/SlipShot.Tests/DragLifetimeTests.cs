using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Windows;
using NUnit.Framework;
using SlipShot.Services;
using ComDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;

namespace SlipShot.Tests;

[TestFixture, Apartment(ApartmentState.STA), NonParallelizable]
public sealed class DragLifetimeTests
{
    private string directory = "";

    [SetUp]
    public void CreateTestDirectory()
    {
        directory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "test-temp", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(directory);
    }

    [TearDown]
    public void RemoveOnlyTestDirectory()
    {
        string parent = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "test-temp")) + Path.DirectorySeparatorChar;
        if (directory.StartsWith(parent, StringComparison.OrdinalIgnoreCase) && Directory.Exists(directory))
            Directory.Delete(directory, true);
    }

    [Test]
    public void TargetCanOpenConsumedFilePathAfterReportingNoDropEffect()
    {
        var store = new DragTransferStore(directory);
        DragTransferLease lease = store.Prepare(CoreTests.Image(17, 13));
        string targetPath = ((string[])lease.Data.GetData(DataFormats.FileDrop, false))[0];
        byte[] expected = File.ReadAllBytes(targetPath);

        // A target can queue its file read, then return None before the upload worker opens it.
        lease.Complete(false);
        lease.Dispose();

        Assert.That(File.Exists(targetPath), Is.True, "A returned drag effect cannot prove that a target discarded its copy of the path.");
        Assert.That(File.ReadAllBytes(targetPath), Is.EqualTo(expected));
        using DragTransferLease next = new DragTransferStore(directory).Prepare(CoreTests.Image(1, 1));
        Assert.That(File.ReadAllBytes(targetPath), Is.EqualTo(expected));
    }

    [Test]
    public void TargetCanReadConsumedPngStreamAfterReportingNoDropEffect()
    {
        DragTransferLease lease = new DragTransferStore(directory).Prepare(CoreTests.Image(17, 13));
        var targetStream = (MemoryStream)lease.Data.GetData("PNG", false);
        byte[] expected = File.ReadAllBytes(lease.FilePath);

        lease.Complete(false);
        lease.Dispose();

        Assert.That(targetStream.CanRead, Is.True, "The native drag result is independent of queued data requests.");
        targetStream.Position = 0;
        using var received = new MemoryStream();
        targetStream.CopyTo(received);
        Assert.That(received.ToArray(), Is.EqualTo(expected));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void UnconsumedCancellationOrPreparationFailureDeletesItsFile(bool explicitlyComplete)
    {
        DragTransferLease lease = new DragTransferStore(directory).Prepare(CoreTests.Image(1, 1));
        if (explicitlyComplete) lease.Complete(false);
        lease.Dispose();
        Assert.That(File.Exists(lease.FilePath), Is.False);
    }

    [Test]
    public void FormatQueriesWithoutPayloadRetrievalDoNotRetainCancelledFile()
    {
        DragTransferLease lease = new DragTransferStore(directory).Prepare(CoreTests.Image(1, 1));
        Assert.That(lease.Data.GetFormats(false), Does.Contain(DataFormats.FileDrop));
        Assert.That(lease.Data.GetDataPresent(DataFormats.FileDrop, false), Is.True);
        Assert.That(lease.Data.GetDataPresent("PNG", false), Is.True);
        var native = (ComDataObject)lease.Data;
        FORMATETC format = FileDropFormat();
        Assert.That(native.QueryGetData(ref format), Is.Zero);
        IEnumFORMATETC formats = native.EnumFormatEtc(DATADIR.DATADIR_GET);
        var item = new FORMATETC[1];
        var fetched = new int[1];
        int count = 0;
        while (formats.Next(1, item, fetched) == 0) count++;
        Assert.That(count, Is.GreaterThanOrEqualTo(3));

        lease.Complete(false);
        lease.Dispose();

        Assert.That(File.Exists(lease.FilePath), Is.False);
    }

    [Test]
    public void NativeFileDropConsumerCanOpenPathAfterReportingNoDropEffect()
    {
        DragTransferLease lease = new DragTransferStore(directory).Prepare(CoreTests.Image(17, 13));
        byte[] expected = File.ReadAllBytes(lease.FilePath);
        var native = (ComDataObject)lease.Data;
        FORMATETC format = FileDropFormat();
        native.GetData(ref format, out STGMEDIUM medium);
        string targetPath;
        try
        {
            Assert.That(medium.tymed, Is.EqualTo(TYMED.TYMED_HGLOBAL));
            Assert.That(DragQueryFile(medium.unionmember, uint.MaxValue, null, 0), Is.EqualTo(1));
            uint length = DragQueryFile(medium.unionmember, 0, null, 0);
            var path = new System.Text.StringBuilder(checked((int)length + 1));
            Assert.That(DragQueryFile(medium.unionmember, 0, path, checked(length + 1)), Is.EqualTo(length));
            targetPath = path.ToString();
        }
        finally { ReleaseStgMedium(ref medium); }

        lease.Complete(false);
        lease.Dispose();

        Assert.That(File.Exists(targetPath), Is.True, "Native CF_HDROP handoff needs the same late-read lifetime as managed payload access.");
        Assert.That(File.ReadAllBytes(targetPath), Is.EqualTo(expected));
    }

    [Test]
    public void ConsumedTransfersWithNoDropEffectStillRespectRetentionBudget()
    {
        var store = new DragTransferStore(directory);
        using (DragTransferLease lease = store.Prepare(CoreTests.Image(1, 1)))
        {
            _ = lease.Data.GetData(DataFormats.FileDrop, false);
            lease.Complete(false);
            Assert.That(File.Exists(lease.FilePath), Is.True);
            for (int index = 1; index < 1024; index++)
                File.Copy(lease.FilePath, Path.Combine(directory, ".leftshot-drag-" + Guid.NewGuid().ToString("N") + ".png"));
        }
        string[] original = Directory.GetFiles(directory);
        Assert.That(original, Has.Length.EqualTo(1024));
        Assert.Throws<IOException>(() => store.Prepare(CoreTests.Image(1, 1)));
        Assert.That(Directory.GetFiles(directory), Is.EquivalentTo(original));
        File.SetLastWriteTimeUtc(original[0], DateTime.UtcNow.AddDays(-2));
        using DragTransferLease next = store.Prepare(CoreTests.Image(1, 1));
        Assert.That(File.Exists(original[0]), Is.False);
    }

    private static FORMATETC FileDropFormat() => new()
    {
        cfFormat = unchecked((short)DataFormats.GetDataFormat(DataFormats.FileDrop).Id),
        dwAspect = DVASPECT.DVASPECT_CONTENT,
        lindex = -1,
        tymed = TYMED.TYMED_HGLOBAL
    };

    [DllImport("ole32.dll")]
    private static extern void ReleaseStgMedium(ref STGMEDIUM medium);
    [DllImport("shell32.dll", EntryPoint = "DragQueryFileW", CharSet = CharSet.Unicode)]
    private static extern uint DragQueryFile(IntPtr drop, uint index, System.Text.StringBuilder? path, uint count);
}
