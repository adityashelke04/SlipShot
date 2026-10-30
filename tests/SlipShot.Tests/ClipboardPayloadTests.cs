using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NUnit.Framework;
using SlipShot.Core;
using SlipShot.Services;

namespace SlipShot.Tests;

[TestFixture, Apartment(ApartmentState.STA), NonParallelizable]
public sealed class ClipboardPayloadTests
{
    [Test]
    public void BitmapAndPngContainOnlyCroppedRedactedPixels()
    {
        var document = new EditDocument(CoreTests.Image(8, 7));
        document.Commit(new DocumentState(new Rect(1, 1, 6, 5), new[]
        {
            new Annotation(Guid.NewGuid(), ToolKind.Redaction, new Rect(2, 2, 3, 2), Colors.Transparent, 1)
        }));
        DataObject data = ClipboardPayload.Create(DocumentRenderer.Render(document));
        Assert.That(data.GetFormats(false), Is.EquivalentTo(new[] { DataFormats.Bitmap, "PNG" }));
        using var png = (MemoryStream)data.GetData("PNG", false);
        Assert.That(png.Position, Is.Zero);
        BitmapFrame encoded = BitmapDecoder.Create(png, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad).Frames[0];
        foreach (BitmapSource output in new[] { (BitmapSource)data.GetData(DataFormats.Bitmap, false), encoded })
        {
            Assert.That(output.PixelWidth, Is.EqualTo(6));
            Assert.That(output.PixelHeight, Is.EqualTo(5));
            byte[] pixels = Pixels(output);
            for (int y = 0; y < 5; y++)
            for (int x = 0; x < 6; x++)
            {
                int offset = (y * 6 + x) * 4;
                byte expected = x >= 1 && x < 4 && y >= 1 && y < 3 ? (byte)0 : (byte)255;
                Assert.That(pixels[offset], Is.EqualTo(expected));
                Assert.That(pixels[offset + 1], Is.EqualTo(expected));
                Assert.That(pixels[offset + 2], Is.EqualTo(expected));
                Assert.That(pixels[offset + 3], Is.EqualTo(255));
            }
        }
    }

    [Test]
    public void PayloadPreservesAlphaWithoutSourceMetadataThumbnailOrMutation()
    {
        const string tag = "/tEXt/{str=Description}";
        byte[] expected = { 0, 0, 0, 0, 20, 40, 80, 128, 10, 30, 90, 255 };
        BitmapSource pixels = BitmapSource.Create(3, 1, 144, 144, PixelFormats.Bgra32, null, expected, 12);
        var metadata = new BitmapMetadata("png");
        metadata.SetQuery(tag, "synthetic source description");
        BitmapFrame source = BitmapFrame.Create(pixels, CoreTests.Image(2, 2), metadata, null);
        source.Freeze();
        DataObject data = ClipboardPayload.Create(source);
        using var png = (MemoryStream)data.GetData("PNG", false);
        BitmapFrame encoded = BitmapDecoder.Create(png, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad).Frames[0];
        var bitmap = (BitmapSource)data.GetData(DataFormats.Bitmap, false);
        Assert.That(bitmap.IsFrozen, Is.True);
        foreach (BitmapSource output in new BitmapSource[] { bitmap, encoded })
        {
            Assert.That(output.PixelWidth, Is.EqualTo(3));
            Assert.That(output.PixelHeight, Is.EqualTo(1));
            Assert.That(Pixels(output), Is.EqualTo(expected));
            Assert.That(output.Metadata is BitmapMetadata tags && tags.ContainsQuery(tag), Is.False);
            Assert.That(output is BitmapFrame frame ? frame.Thumbnail : null, Is.Null);
        }
        Assert.That(Pixels(source), Is.EqualTo(expected));
        Assert.That(((BitmapMetadata)source.Metadata).GetQuery(tag), Is.EqualTo("synthetic source description"));
        Assert.That(source.Thumbnail, Is.Not.Null);
        Assert.That(source.DpiX, Is.EqualTo(144));
    }

    [Test]
    public void MissingPixelsAreRejected() => Assert.Throws<ArgumentNullException>(() => ClipboardPayload.Create(null!));

    [Test]
    public void ReplacingFirstCaptureCopiesSecondCapturesPixels()
    {
        var preview=new PreviewQueue<EditDocument>();
        preview.Replace(new EditDocument(CoreTests.Image(1,1)));
        var blue=BitmapSource.Create(1,1,96,96,PixelFormats.Bgra32,null,new byte[]{255,0,0,255},4);
        preview.Replace(new EditDocument(blue));
        DataObject data=ClipboardPayload.Create(DocumentRenderer.Render(preview.Current!));
        Assert.That(Pixels((BitmapSource)data.GetData(DataFormats.Bitmap)),Is.EqualTo(new byte[]{255,0,0,255}));
        using var png=(MemoryStream)data.GetData("PNG");
        var decoded=BitmapDecoder.Create(png,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad).Frames[0];
        Assert.That(Pixels(decoded),Is.EqualTo(new byte[]{255,0,0,255}));
        Assert.That(preview.PendingCount,Is.Zero);
    }

    private static byte[] Pixels(BitmapSource image)
    {
        var normalized = new FormatConvertedBitmap(image, PixelFormats.Bgra32, null, 0);
        var pixels = new byte[image.PixelWidth * image.PixelHeight * 4];
        normalized.CopyPixels(pixels, image.PixelWidth * 4, 0);
        return pixels;
    }
}
