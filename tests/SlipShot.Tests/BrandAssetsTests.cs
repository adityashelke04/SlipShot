using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NUnit.Framework;
using SlipShot.UI;

namespace SlipShot.Tests;

[TestFixture, Apartment(ApartmentState.STA), NonParallelizable]
public class BrandAssetsTests
{
    [Test]
    public void WindowAndTrayIconsRemainUsableAfterTheirResourceStreamsClose()
    {
        BitmapSource window = Branding.WindowIcon;
        Assert.That(window.IsFrozen, Is.True);
        Assert.That(window.PixelWidth, Is.EqualTo(256));
        Assert.That(Branding.WindowIcon, Is.SameAs(window));
        var pixels = new byte[window.PixelWidth * window.PixelHeight * 4];
        window.CopyPixels(pixels, window.PixelWidth * 4, 0);
        using var first = Branding.CreateTrayIcon();
        using var second = Branding.CreateTrayIcon();
        Assert.That(second, Is.Not.SameAs(first));
        first.Dispose();
        using var bitmap = second.ToBitmap();
        Assert.That(bitmap.Width, Is.EqualTo(32));
        Assert.That(bitmap.Height, Is.EqualTo(32));
    }

    [Test]
    public void EmbeddedIconSuppliesEveryWindowsSizeWithTransparentCornersAndBrandColors()
    {
        using var stream = typeof(EditorWindow).Assembly.GetManifestResourceStream("SlipShot.Assets.slipshot.ico");
        Assert.That(stream, Is.Not.Null, "The app must carry its icon without a loose asset file.");
        using var reader = new BinaryReader(stream!);
        Assert.That(reader.ReadUInt16(), Is.Zero);
        Assert.That(reader.ReadUInt16(), Is.EqualTo(1));
        int count = reader.ReadUInt16();
        Assert.That(count, Is.EqualTo(9));
        var frames = new List<(int Size, int Length, int Offset)>();
        for (int i = 0; i < count; i++)
        {
            int width = reader.ReadByte(); if (width == 0) width = 256;
            int height = reader.ReadByte(); if (height == 0) height = 256;
            Assert.That(height, Is.EqualTo(width));
            Assert.That(reader.ReadByte(), Is.Zero);
            Assert.That(reader.ReadByte(), Is.Zero);
            Assert.That(reader.ReadUInt16(), Is.EqualTo(1));
            Assert.That(reader.ReadUInt16(), Is.EqualTo(32));
            frames.Add((width, reader.ReadInt32(), reader.ReadInt32()));
        }
        Assert.That(frames.ConvertAll(frame => frame.Size), Is.EquivalentTo(new[] {16,20,24,32,40,48,64,128,256}));
        Assert.That(frames[0].Size, Is.EqualTo(256), "Keep the canonical ICO frame order; the window loader explicitly selects the largest frame.");
        int nextOffset = 6 + count * 16;
        foreach (var frame in frames)
        {
            Assert.That(frame.Offset, Is.EqualTo(nextOffset));
            Assert.That(frame.Length, Is.GreaterThan(8));
            Assert.That((long)frame.Offset + frame.Length, Is.LessThanOrEqualTo(stream!.Length));
            stream.Position = frame.Offset;
            byte[] png = reader.ReadBytes(frame.Length);
            Assert.That(png[..8], Is.EqualTo(new byte[] {137,80,78,71,13,10,26,10}));
            using var payload = new MemoryStream(png);
            var decoder = new PngBitmapDecoder(payload, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            var bitmap = new FormatConvertedBitmap(decoder.Frames[0], PixelFormats.Bgra32, null, 0);
            Assert.That(bitmap.PixelWidth, Is.EqualTo(frame.Size));
            Assert.That(bitmap.PixelHeight, Is.EqualTo(frame.Size));
            var pixels = new byte[frame.Size * frame.Size * 4];
            bitmap.CopyPixels(pixels, frame.Size * 4, 0);
            Assert.That(pixels[3], Is.Zero, "The tile's outer corner must be transparent.");
            int orange = 0, dark = 0;
            for (int p = 0; p < pixels.Length; p += 4)
            {
                if (pixels[p + 3] != 255) continue;
                if (pixels[p] == 0x57 && pixels[p + 1] == 0x77 && pixels[p + 2] == 0xD9) orange++;
                if (pixels[p] == 0x13 && pixels[p + 1] == 0x14 && pixels[p + 2] == 0x14) dark++;
            }
            Assert.That(orange, Is.GreaterThan(frame.Size * frame.Size / 20), $"{frame.Size}px orange mark disappeared.");
            Assert.That(dark, Is.GreaterThan(frame.Size * frame.Size / 4), $"{frame.Size}px dark tile disappeared.");
            nextOffset += frame.Length;
        }
        Assert.That(nextOffset, Is.EqualTo(stream!.Length));
    }
}
