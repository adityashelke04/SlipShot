using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using LeftShot.Core;
using LeftShot.Services;
using NUnit.Framework;

namespace LeftShot.Tests;

[TestFixture, Apartment(ApartmentState.STA), NonParallelizable]
public sealed class ServiceTests
{
    private string directory = "";

    [SetUp]
    public void CreateTestDirectory()
    {
        string parent = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "test-temp"));
        directory = Path.GetFullPath(Path.Combine(parent, Guid.NewGuid().ToString("N")));
        Assert.That(directory.StartsWith(parent + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase), Is.True,
            "Generated test files must remain inside the application-local test-temp directory.");
        Directory.CreateDirectory(directory);
    }

    [TearDown]
    public void RemoveOnlyTestDirectory()
    {
        string parent = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "test-temp")) + Path.DirectorySeparatorChar;
        if (!string.IsNullOrEmpty(directory) && Path.GetFullPath(directory).StartsWith(parent, StringComparison.OrdinalIgnoreCase) && Directory.Exists(directory))
            Directory.Delete(directory, true);
    }

    [Test]
    public void NamesAvoidFilesAndDirectoriesAndSaveRefusesAnUnconfirmedOverwrite()
    {
        DateTime now = new(2026, 10, 30, 12, 0, 0);
        string first = ImageFiles.GenerateName(directory, now);
        Assert.That(Path.GetFileName(first), Is.EqualTo("LeftShot_2026-10-30_12-00-00_001.png"));
        byte[] sentinel = { 1, 2, 3, 4 };
        File.WriteAllBytes(first, sentinel);
        string second = ImageFiles.GenerateName(directory, now);
        Directory.CreateDirectory(second);
        Assert.That(ImageFiles.GenerateName(directory, now), Does.EndWith("_003.png"));
        Assert.Throws<IOException>(() => ImageFiles.Save(CoreTests.Image(), first));
        Assert.That(File.ReadAllBytes(first), Is.EqualTo(sentinel));
        ImageFiles.Save(CoreTests.Image(17, 13), first, overwrite: true);
        AssertDimensions(first, 17, 13);
        Assert.That(Directory.GetFiles(directory, ".leftshot-*.tmp"), Is.Empty);
    }

    [Test]
    public void InvalidPathsAndControlledWriteFailuresLeaveNoPartialImage()
    {
        BitmapSource image = CoreTests.Image();
        Assert.Throws<ArgumentException>(() => ImageFiles.Save(image, Path.Combine(directory, "bad.gif")));
        Assert.Throws<ArgumentException>(() => ImageFiles.Save(image, "\0.png"));
        Assert.Throws<DirectoryNotFoundException>(() => ImageFiles.Save(image, Path.Combine(directory, "missing", "capture.png")));
        string blocked = Path.Combine(directory, "blocked.png");
        File.WriteAllText(blocked, "keep me");
        using (var locked = new FileStream(blocked, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            Exception? error = Assert.Catch(() => ImageFiles.Save(image, blocked, overwrite: true));
            Assert.That(error, Is.InstanceOf<IOException>().Or.InstanceOf<UnauthorizedAccessException>());
        }
        Assert.That(File.ReadAllText(blocked), Is.EqualTo("keep me"));
        Assert.That(Directory.GetFiles(directory, ".leftshot-*.tmp"), Is.Empty);
    }

    [Test]
    public void PngKeepsAlphaAndJpegRequiresAndUsesOpaqueBackground()
    {
        BitmapSource transparent = BitmapSource.Create(12, 10, 144, 144, PixelFormats.Bgra32, null, new byte[12 * 10 * 4], 12 * 4);
        string png = Path.Combine(directory, "transparent.png");
        string jpeg = Path.Combine(directory, "opaque.jpg");
        ImageFiles.Save(transparent, png);
        Assert.That(Pixel(Read(png)).A, Is.Zero);
        Assert.Throws<ArgumentException>(() => ImageFiles.Save(transparent, jpeg));
        Assert.Throws<ArgumentException>(() => ImageFiles.Save(transparent, jpeg, jpegBackground: Colors.Transparent));
        Assert.Throws<ArgumentOutOfRangeException>(() => ImageFiles.Save(transparent, jpeg, jpegQuality: 101, jpegBackground: Colors.White));
        ImageFiles.Save(transparent, jpeg, jpegQuality: 100, jpegBackground: Colors.White);
        Color color = Pixel(Read(jpeg));
        Assert.That(color.A, Is.EqualTo(255));
        Assert.That(color.R, Is.GreaterThanOrEqualTo(250));
        Assert.That(color.G, Is.GreaterThanOrEqualTo(250));
        Assert.That(color.B, Is.GreaterThanOrEqualTo(250));
        AssertDimensions(jpeg, 12, 10);
    }

    [Test]
    public void CroppedRedactionStaysOpaqueThroughFileAndInjectedClipboardWithoutSourceMetadata()
    {
        const string sourceTag = "/tEXt/{str=Description}";
        byte[] sourcePixels = new byte[8 * 7 * 4];
        for (int i = 0; i < sourcePixels.Length; i += 4)
        {
            sourcePixels[i] = sourcePixels[i + 2] = sourcePixels[i + 3] = 255;
        }
        BitmapSource pixels = BitmapSource.Create(8, 7, 96, 96, PixelFormats.Bgra32, null, sourcePixels, 8 * 4);
        var metadata = new BitmapMetadata("png");
        metadata.SetQuery(sourceTag, "private source description");
        BitmapFrame source = BitmapFrame.Create(pixels, CoreTests.Image(2, 2), metadata, null);
        Assert.That(((BitmapMetadata)source.Metadata).ContainsQuery(sourceTag), Is.True);
        Assert.That(source.Thumbnail, Is.Not.Null);
        var document = new EditDocument(source);
        document.Commit(new DocumentState(new Rect(1, 1, 6, 5), new[]
        {
            new Annotation(Guid.NewGuid(), ToolKind.Redaction, new Rect(2, 2, 3, 2), Colors.Transparent, 1)
        }));
        BitmapSource flattened = DocumentRenderer.Render(document);
        string path = Path.Combine(directory, "redacted.png");
        ImageFiles.Save(flattened, path);
        var saved = (BitmapFrame)Read(path);
        Assert.That(saved.PixelWidth, Is.EqualTo(6));
        Assert.That(saved.PixelHeight, Is.EqualTo(5));
        Assert.That(saved.Thumbnail, Is.Null);
        Assert.That(saved.Metadata is BitmapMetadata savedMetadata && savedMetadata.ContainsQuery(sourceTag), Is.False);
        var sink = new FakeClipboard(0);
        Pump(ClipboardWriter.CopyAsync(flattened, sink));
        Assert.That(sink.LastImage, Is.SameAs(flattened).And.TypeOf<RenderTargetBitmap>());
        Assert.That(sink.LastImage!.Metadata, Is.Null);
        byte[] expected = new byte[6 * 5 * 4];
        for (int y = 0; y < 5; y++)
        for (int x = 0; x < 6; x++)
        {
            int offset = (y * 6 + x) * 4;
            expected[offset + 3] = 255;
            if (x >= 1 && x < 4 && y >= 1 && y < 3) continue;
            expected[offset] = expected[offset + 2] = 255;
        }
        foreach (BitmapSource output in new[] { saved, sink.LastImage! })
        {
            var converted = new FormatConvertedBitmap(output, PixelFormats.Bgra32, null, 0);
            byte[] actual = new byte[expected.Length];
            converted.CopyPixels(actual, 6 * 4, 0);
            Assert.That(actual, Is.EqualTo(expected));
        }
    }

    [Test]
    public void SettingsRoundTripAndRecoverFromMissingCorruptOrUnsupportedData()
    {
        string path = Path.Combine(directory, "settings.json");
        var store = new SettingsStore(path);
        Assert.That(store.Load(), Is.EqualTo(new AppSettings()));
        Assert.That(store.LoadWarning, Is.Null);
        var settings = new AppSettings { Appearance = "Dark", PreviewSeconds = 12, ReducedMotion = true, AutoCopy = true, SaveDirectory = directory };
        store.Save(settings);
        Assert.That(store.Load(), Is.EqualTo(settings));
        foreach (string invalid in new[] { "{broken", "null", "{\"Version\":2}", "{\"JpegQuality\":0}", "{\"JpegBackground\":\"#00FFFFFF\"}" })
        {
            File.WriteAllText(path, invalid);
            Assert.That(store.Load(), Is.EqualTo(new AppSettings()));
            Assert.That(store.LoadWarning, Is.Not.Null.And.Not.Empty);
        }
        Assert.Throws<ArgumentException>(() => store.Save(settings with { Version = 2 }));
        Assert.That(Directory.GetFiles(directory, ".leftshot-*.tmp"), Is.Empty);
    }

    [Test]
    public void SettingsFailurePreservesExistingFileAndCleansTemporaryFile()
    {
        string path = Path.Combine(directory, "settings.json");
        var store = new SettingsStore(path);
        store.Save(new AppSettings());
        string original = File.ReadAllText(path);
        using (var locked = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            Exception? error = Assert.Catch(() => store.Save(new AppSettings { Appearance = "Dark" }));
            Assert.That(error, Is.InstanceOf<IOException>().Or.InstanceOf<UnauthorizedAccessException>());
        }
        Assert.That(File.ReadAllText(path), Is.EqualTo(original));
        Assert.That(Directory.GetFiles(directory, ".leftshot-*.tmp"), Is.Empty);
    }

    [Test]
    public void ClipboardRetriesOnlyBusyErrorsOnOriginalStaThread()
    {
        int thread = Environment.CurrentManagedThreadId;
        var sink = new FakeClipboard(2);
        Pump(ClipboardWriter.CopyAsync(CoreTests.Image(), sink));
        Assert.That(sink.Attempts, Is.EqualTo(3));
        Assert.That(sink.Threads, Is.All.EqualTo(thread));
        Assert.That(sink.Apartments, Is.All.EqualTo(ApartmentState.STA));
        var otherError = new FakeClipboard(10, unchecked((int)0x80004005));
        Assert.Throws<ExternalException>(() => Pump(ClipboardWriter.CopyAsync(CoreTests.Image(), otherError)));
        Assert.That(otherError.Attempts, Is.EqualTo(1));
    }

    [Test]
    public void ClipboardStopsAfterBoundedRetriesAndHonorsCancellation()
    {
        var busy = new FakeClipboard(10);
        var error = Assert.Throws<InvalidOperationException>(() => Pump(ClipboardWriter.CopyAsync(CoreTests.Image(), busy)));
        Assert.That(error!.Message, Does.Contain("clipboard is busy"));
        Assert.That(busy.Attempts, Is.EqualTo(4));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var cancelled = new FakeClipboard(0);
        Assert.Catch<OperationCanceledException>(() => Pump(ClipboardWriter.CopyAsync(CoreTests.Image(), cancelled, cancellation.Token)));
        Assert.That(cancelled.Attempts, Is.Zero);
        using var duringRetry = new CancellationTokenSource();
        var pending = new FakeClipboard(10);
        Task copy = ClipboardWriter.CopyAsync(CoreTests.Image(), pending, duringRetry.Token);
        duringRetry.Cancel();
        Assert.Catch<OperationCanceledException>(() => Pump(copy));
        Assert.That(pending.Attempts, Is.EqualTo(1));
    }

    private static BitmapSource Read(string path)
    {
        using var stream = File.OpenRead(path);
        return BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad).Frames[0];
    }

    private static void AssertDimensions(string path, int width, int height)
    {
        BitmapSource image = Read(path);
        Assert.That(image.PixelWidth, Is.EqualTo(width));
        Assert.That(image.PixelHeight, Is.EqualTo(height));
    }

    private static Color Pixel(BitmapSource image)
    {
        var converted = new FormatConvertedBitmap(image, PixelFormats.Bgra32, null, 0);
        byte[] bytes = new byte[4];
        converted.CopyPixels(new Int32Rect(0, 0, 1, 1), bytes, 4, 0);
        return Color.FromArgb(bytes[3], bytes[2], bytes[1], bytes[0]);
    }

    private static void Pump(Task task)
    {
        if (!task.IsCompleted)
        {
            Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
            var frame = new DispatcherFrame();
            var timeout = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            timeout.Tick += (_, _) => frame.Continue = false;
            _ = task.ContinueWith(_ => dispatcher.BeginInvoke(new Action(() => frame.Continue = false)), TaskScheduler.Default);
            timeout.Start();
            try { Dispatcher.PushFrame(frame); }
            finally { timeout.Stop(); }
            Assert.That(task.IsCompleted, Is.True, "Clipboard retry did not complete within its bounded interval.");
        }
        task.GetAwaiter().GetResult();
    }

    private sealed class FakeClipboard(int failures, int code = unchecked((int)0x800401D0)) : IClipboardSink
    {
        public int Attempts { get; private set; }
        public BitmapSource? LastImage { get; private set; }
        public List<int> Threads { get; } = new();
        public List<ApartmentState> Apartments { get; } = new();
        public void SetImage(BitmapSource image)
        {
            Attempts++;
            Threads.Add(Environment.CurrentManagedThreadId);
            Apartments.Add(Thread.CurrentThread.GetApartmentState());
            if (Attempts <= failures) throw new ExternalException("Injected clipboard error", code);
            LastImage = image;
        }
    }
}
