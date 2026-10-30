using System;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SlipShot.Core;

namespace SlipShot.Services;

public static class ImageFiles
{
    public static void RecordSavedFile(EditDocument document,string path,IEnumerable<EditDocument> openDocuments)
    {
        // Explicit Save As may replace a file exported by another still-open capture.
        foreach(var other in openDocuments)other.InvalidateSavedFile(path);
        document.MarkSaved(path);
    }
    public static void RefreshSavedState(EditDocument document)
    {if(document.SavedPath is string path && !File.Exists(path))document.InvalidateSavedFile(path);}

    public static string SaveDocument(EditDocument document,string directory,DateTime now)
    {
        ArgumentNullException.ThrowIfNull(document);
        RefreshSavedState(document);
        if(document.SavedPath is string existing && File.Exists(existing))return existing;
        string path=QuickSave(DocumentRenderer.Render(document),directory,now);
        document.MarkSaved(path);return path;
    }
    public static string DefaultSaveDirectory
    {
        get
        {
            string root=Path.GetPathRoot(AppContext.BaseDirectory)!;
            // A portable app launched from a network share must not silently save back to it.
            return root.StartsWith(@"\\",StringComparison.Ordinal)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"SlipShot","Captures")
                : Path.Combine(root,"SlipShot Captures");
        }
    }

    public static string QuickSave(BitmapSource image,string directory,DateTime now)
    {
        ArgumentNullException.ThrowIfNull(image);
        if(!Path.IsPathFullyQualified(directory))throw new ArgumentException("Choose an absolute save folder.",nameof(directory));
        string folder=Path.GetFullPath(directory);
        Directory.CreateDirectory(folder);
        for(int attempt=0;attempt<16;attempt++)
        {
            string path=GenerateName(folder,now);
            try {Save(image,path);return path;}
            catch(IOException) when(File.Exists(path)||Directory.Exists(path)) { }
        }
        throw new IOException("Could not reserve a unique filename. Try saving again.");
    }

    public static void Save(BitmapSource image, string path, bool overwrite = false, int jpegQuality = 90, Color? jpegBackground = null)
    {
        ArgumentNullException.ThrowIfNull(image);
        string fullPath = Path.GetFullPath(path);
        string extension = Path.GetExtension(fullPath).ToLowerInvariant();
        bool jpeg = extension is ".jpg" or ".jpeg";
        if (!jpeg && extension != ".png") throw new ArgumentException("Choose a PNG or JPEG filename.", nameof(path));
        if (jpeg && jpegBackground is not Color { A: 255 })
            throw new ArgumentException("JPEG export requires an explicit opaque background color.", nameof(jpegBackground));
        if (jpeg && jpegQuality is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(jpegQuality));
        if (!overwrite && File.Exists(fullPath)) throw new IOException("A file already exists at this location. Confirm overwrite or choose another name.");

        // Rerender only the supplied flattened pixels; never carry source metadata or thumbnails.
        var visual = new DrawingVisual();
        var bounds = new Rect(0, 0, image.PixelWidth, image.PixelHeight);
        using (DrawingContext drawing = visual.RenderOpen())
        {
            if (jpeg) drawing.DrawRectangle(new SolidColorBrush(jpegBackground!.Value), null, bounds);
            drawing.DrawImage(image, bounds);
        }
        var pixels = new RenderTargetBitmap(image.PixelWidth, image.PixelHeight, 96, 96, PixelFormats.Pbgra32);
        pixels.Render(visual);
        pixels.Freeze();
        BitmapEncoder encoder = jpeg ? new JpegBitmapEncoder { QualityLevel = jpegQuality } : new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(pixels));
        string temporary = Path.Combine(Path.GetDirectoryName(fullPath)!, ".slipshot-image-" + Guid.NewGuid().ToString("N") + ".tmp");
        bool created = false;
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                created = true;
                encoder.Save(stream);
                stream.Flush(true);
            }
            using (var stream = File.OpenRead(temporary))
            {
                BitmapDecoder decoded = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                if (decoded.Frames.Count != 1 || decoded.Frames[0].PixelWidth != image.PixelWidth || decoded.Frames[0].PixelHeight != image.PixelHeight)
                    throw new InvalidDataException("The saved image did not pass verification.");
            }
            File.Move(temporary, fullPath, overwrite);
        }
        finally { if (created) SettingsStore.TryDelete(temporary); }
    }

    public static string GenerateName(string directory, DateTime now, string extension = ".png")
    {
        string folder = Path.GetFullPath(directory);
        extension = extension.ToLowerInvariant();
        if (extension is not (".png" or ".jpg" or ".jpeg")) throw new ArgumentException("Choose PNG or JPEG.", nameof(extension));
        string stem = "SlipShot_" + now.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);
        for (int index = 1; index < int.MaxValue; index++)
        {
            string candidate = Path.Combine(folder, stem + "_" + index.ToString("D3", CultureInfo.InvariantCulture) + extension);
            if (!File.Exists(candidate) && !Directory.Exists(candidate)) return candidate;
        }
        throw new IOException("No unused image filename is available.");
    }
}
