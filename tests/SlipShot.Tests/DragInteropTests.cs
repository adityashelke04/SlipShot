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
public sealed class DragInteropTests
{
    [Test]
    public void RepeatedNativePngRequestsReturnTheWholeImageAfterManagedStreamReads()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "test-temp", Guid.NewGuid().ToString("N"));
        try
        {
            using DragTransferLease lease = new DragTransferStore(directory).Prepare(CoreTests.Image(17, 13));
            byte[] expected = File.ReadAllBytes(lease.FilePath);
            var stream = (MemoryStream)lease.Data.GetData("PNG", false);
            stream.Position = stream.Length;
            var native = (ComDataObject)lease.Data;
            for (int attempt = 0; attempt < 5; attempt++)
            {
                byte[] actual = ReadNativeBytes(native, "PNG");
                Assert.That(actual, Is.EqualTo(expected), $"Native PNG request {attempt + 1} returned corrupt or incomplete data.");
            }
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Test]
    public void NativeFileDropRetainsTheExactUnicodePathAfterAcceptedLeaseDisposal()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "test-temp", "drag-interop-\u6d4b\u8bd5-" + Guid.NewGuid().ToString("N"));
        try
        {
            DragTransferLease lease = new DragTransferStore(directory).Prepare(CoreTests.Image(17, 13));
            lease.Complete(true);
            lease.Dispose();
            var native = (ComDataObject)lease.Data;
            for (int attempt = 0; attempt < 3; attempt++)
            {
                FORMATETC format = Format(DataFormats.FileDrop);
                Assert.That(native.QueryGetData(ref format), Is.Zero);
                native.GetData(ref format, out STGMEDIUM medium);
                try
                {
                    Assert.That(medium.tymed, Is.EqualTo(TYMED.TYMED_HGLOBAL));
                    Assert.That(DragQueryFile(medium.unionmember, uint.MaxValue, null, 0), Is.EqualTo(1));
                    uint length = DragQueryFile(medium.unionmember, 0, null, 0);
                    var path = new System.Text.StringBuilder(checked((int)length + 1));
                    Assert.That(DragQueryFile(medium.unionmember, 0, path, checked(length + 1)), Is.EqualTo(length));
                    Assert.That(path.ToString(), Is.EqualTo(lease.FilePath));
                    Assert.That(File.Exists(path.ToString()), Is.True);
                }
                finally { ReleaseStgMedium(ref medium); }
            }
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Test]
    public void NativeFileReadPreservesLateFileAndPngReadsWhenTargetReportsNoCopy()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "test-temp", Guid.NewGuid().ToString("N"));
        try
        {
            using DragTransferLease lease = new DragTransferStore(directory).Prepare(CoreTests.Image(17, 13));
            byte[] expected = File.ReadAllBytes(lease.FilePath);
            var native = (ComDataObject)lease.Data;
            Assert.That(ReadNativeBytes(native, DataFormats.FileDrop), Is.Not.Empty);
            lease.Complete(false);
            lease.Dispose();
            Assert.That(File.ReadAllBytes(lease.FilePath), Is.EqualTo(expected));
            Assert.That(ReadNativeBytes(native, "PNG"), Is.EqualTo(expected));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Test]
    public void NativeFormatQueriesWithoutDataReadsDoNotRetainARejectedDrag()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "test-temp", Guid.NewGuid().ToString("N"));
        try
        {
            using DragTransferLease lease = new DragTransferStore(directory).Prepare(CoreTests.Image(17, 13));
            var native = (ComDataObject)lease.Data;
            FORMATETC format = Format(DataFormats.FileDrop);
            Assert.That(native.QueryGetData(ref format), Is.Zero);
            lease.Complete(false);
            Assert.That(File.Exists(lease.FilePath), Is.False);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    private static FORMATETC Format(string name) => new()
    {
        cfFormat = unchecked((short)DataFormats.GetDataFormat(name).Id),
        dwAspect = DVASPECT.DVASPECT_CONTENT,
        lindex = -1,
        tymed = TYMED.TYMED_HGLOBAL
    };

    private static byte[] ReadNativeBytes(ComDataObject data, string name)
    {
        FORMATETC format = Format(name);
        Assert.That(data.QueryGetData(ref format), Is.Zero);
        data.GetData(ref format, out STGMEDIUM medium);
        try
        {
            Assert.That(medium.tymed, Is.EqualTo(TYMED.TYMED_HGLOBAL));
            var bytes = new byte[checked((int)GlobalSize(medium.unionmember))];
            IntPtr pointer = GlobalLock(medium.unionmember);
            Assert.That(pointer, Is.Not.EqualTo(IntPtr.Zero));
            try { Marshal.Copy(pointer, bytes, 0, bytes.Length); }
            finally { GlobalUnlock(medium.unionmember); }
            return bytes;
        }
        finally { ReleaseStgMedium(ref medium); }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr memory);
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalUnlock(IntPtr memory);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern UIntPtr GlobalSize(IntPtr memory);
    [DllImport("ole32.dll")]
    private static extern void ReleaseStgMedium(ref STGMEDIUM medium);
    [DllImport("shell32.dll", EntryPoint = "DragQueryFileW", CharSet = CharSet.Unicode)]
    private static extern uint DragQueryFile(IntPtr drop, uint index, System.Text.StringBuilder? path, uint count);
}
