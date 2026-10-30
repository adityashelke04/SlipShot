using System;
using System.IO;
using System.Text.Json;
using System.Windows.Media;

namespace SlipShot.Services;

public sealed record AppSettings
{
    public int Version { get; init; } = 1;
    public string Hotkey { get; init; } = "Ctrl+Alt+S";
    public string Appearance { get; init; } = "System";
    public int PreviewSeconds { get; init; } = 8;
    public bool ReducedMotion { get; init; }
    public bool AutoCopy { get; init; }
    public string? SaveDirectory { get; init; }
    public int JpegQuality { get; init; } = 90;
    public string JpegBackground { get; init; } = "#FFFFFF";
}

public sealed class SettingsStore
{
    private readonly string path;
    public string? LoadWarning { get; private set; }

    public SettingsStore(string path) => this.path = Path.GetFullPath(path);

    public AppSettings Load()
    {
        LoadWarning = null;
        try
        {
            using var stream = File.OpenRead(path);
            if (stream.Length > 65536) throw new InvalidDataException("Settings file is too large.");
            AppSettings? value = JsonSerializer.Deserialize<AppSettings>(stream);
            if (value is null || !Valid(value)) throw new InvalidDataException("Settings version or values are unsupported.");
            return value;
        }
        catch (FileNotFoundException) { return new AppSettings(); }
        catch (DirectoryNotFoundException) { return new AppSettings(); }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or JsonException)
        {
            LoadWarning = "Settings could not be loaded. Defaults are being used.";
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!Valid(settings)) throw new ArgumentException("Settings version or values are unsupported.", nameof(settings));
        string directory = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(directory);
        string temporary = Path.Combine(directory, ".slipshot-settings-" + Guid.NewGuid().ToString("N") + ".tmp");
        bool created = false;
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                created = true;
                JsonSerializer.Serialize(stream, settings, new JsonSerializerOptions { WriteIndented = true });
                stream.Flush(true);
            }
            File.Move(temporary, path, true);
        }
        finally { if (created) TryDelete(temporary); }
    }

    private static bool Valid(AppSettings value)
    {
        if (value.Version != 1 || string.IsNullOrWhiteSpace(value.Hotkey) || value.Hotkey.Length > 100 ||
            value.Appearance is not ("System" or "Light" or "Dark") || value.PreviewSeconds is < 2 or > 60 ||
            value.JpegQuality is < 1 or > 100 || string.IsNullOrWhiteSpace(value.JpegBackground)) return false;
        if (!Hotkeys.TryParse(value.Hotkey, out _, out _)) return false;
        try { return ColorConverter.ConvertFromString(value.JpegBackground) is Color { A: 255 }; }
        catch (Exception error) when (error is FormatException or ArgumentException or NotSupportedException) { return false; }
    }

    internal static void TryDelete(string temporary)
    {
        try { File.Delete(temporary); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
