using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace LeftShot.Services;

public sealed class DragTransferStore
{
    private const string Prefix = ".leftshot-drag-";
    private const int MaximumFiles = 32;
    private const long MaximumBytes = 256L * 1024 * 1024;
    private static readonly TimeSpan Retention = TimeSpan.FromHours(24);
    private readonly string directory;
    private readonly HashSet<string> active = new(StringComparer.OrdinalIgnoreCase);

    public DragTransferStore(string directory) => this.directory = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory));

    // Call only for an explicit drag, with the flattened DocumentRenderer output.
    public DragTransferLease Prepare(BitmapSource flattened)
    {
        ArgumentNullException.ThrowIfNull(flattened);
        EnsurePlainDirectory();
        Directory.CreateDirectory(directory);
        EnsurePlainDirectory();
        (int count, long bytes) = SweepExpired();
        if (count >= MaximumFiles || bytes >= MaximumBytes) throw CapacityError();

        string path = Path.Combine(directory, Prefix + Guid.NewGuid().ToString("N") + ".png");
        bool created = false;
        MemoryStream? png = null;
        try
        {
            ImageFiles.Save(flattened, path);
            created = true;
            if (new FileInfo(path).Length > MaximumBytes - bytes) throw CapacityError();
            png = new MemoryStream(File.ReadAllBytes(path), writable: false);
            BitmapFrame bitmap = BitmapDecoder.Create(png, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad).Frames[0];
            bitmap.Freeze();
            png.Position = 0;
            var data = new DataObject();
            data.SetData(DataFormats.FileDrop, new[] { path }, autoConvert: false);
            data.SetData(DataFormats.Bitmap, bitmap, autoConvert: false);
            data.SetData("PNG", png, autoConvert: false);
            active.Add(path);
            return new DragTransferLease(data, path, png, Complete);
        }
        catch
        {
            png?.Dispose();
            if (created) TryDeleteOwned(path);
            throw;
        }
    }

    private (int Count, long Bytes) SweepExpired()
    {
        int count = 0;
        long bytes = 0;
        DateTime cutoff = DateTime.UtcNow - Retention;
        foreach (string path in Directory.EnumerateFiles(directory, Prefix + "*.png", SearchOption.TopDirectoryOnly))
        {
            if (!IsOwnedName(path)) continue;
            var file = new FileInfo(path);
            if (!file.Exists || (file.Attributes & FileAttributes.ReparsePoint) != 0) continue;
            if (!active.Contains(path) && file.LastWriteTimeUtc < cutoff)
            {
                TryDeleteOwned(path);
                file.Refresh();
            }
            if (!file.Exists) continue;
            count++;
            bytes = checked(bytes + file.Length);
        }
        return (count, bytes);
    }

    private void Complete(string path, bool accepted)
    {
        active.Remove(path);
        if (!accepted)
        {
            TryDeleteOwned(path);
            return;
        }
        // A target may open the file after DoDragDrop returns, including after this app exits.
        try
        {
            EnsurePlainDirectory();
            File.SetLastWriteTimeUtc(path, DateTime.UtcNow);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private void TryDeleteOwned(string path)
    {
        if (!string.Equals(Path.GetDirectoryName(path), directory, StringComparison.OrdinalIgnoreCase) || !IsOwnedName(path)) return;
        try
        {
            EnsurePlainDirectory();
            if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0) File.Delete(path);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private static bool IsOwnedName(string path)
    {
        string name = Path.GetFileName(path);
        return name.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase) &&
            name.EndsWith(".png", StringComparison.OrdinalIgnoreCase) &&
            Guid.TryParseExact(name[Prefix.Length..^4], "N", out _);
    }

    private void EnsurePlainDirectory()
    {
        for (DirectoryInfo? folder = new(directory); folder is not null; folder = folder.Parent)
            if (folder.Exists && (folder.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Drag files cannot use a redirected folder. Move the portable app to a regular writable folder.");
    }

    private static IOException CapacityError() => new("Temporary drag files reached the 32-file or 256-MiB limit. Use Copy or Save, or try again after older drag files expire (24 hours).");
}

public sealed class DragTransferLease : IDisposable
{
    private readonly MemoryStream png;
    private readonly Action<string, bool> complete;
    private bool completed;

    public DataObject Data { get; }
    public string FilePath { get; }

    internal DragTransferLease(DataObject data, string filePath, MemoryStream png, Action<string, bool> complete)
    {
        Data = data;
        FilePath = filePath;
        this.png = png;
        this.complete = complete;
    }

    public void Complete(bool accepted)
    {
        if (completed) return;
        completed = true;
        complete(FilePath, accepted);
        // Successful data objects can also serve late PNG requests; this stream owns only managed memory.
        if (!accepted) png.Dispose();
    }

    public void Dispose() => Complete(false);
}
