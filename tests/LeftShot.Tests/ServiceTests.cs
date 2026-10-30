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
        Assert.That(Path.GetPathRoot(parent), Is.EqualTo("F:\\").IgnoreCase, "Test files must remain on the authorized F drive.");
        directory = Path.Combine(parent, Guid.NewGuid().ToString("N"));
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
        public List<int> Threads { get; } = new();
        public List<ApartmentState> Apartments { get; } = new();
        public void SetImage(BitmapSource image)
        {
            Attempts++;
            Threads.Add(Environment.CurrentManagedThreadId);
            Apartments.Add(Thread.CurrentThread.GetApartmentState());
            if (Attempts <= failures) throw new ExternalException("Injected clipboard error", code);
        }
    }
}
