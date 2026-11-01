using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace SlipShot.Services;

// Explicit local opt-in; never record pixels, paths, app/window titles or exception messages.
internal sealed class DragDiagnostics(string? path)
{
    private static readonly object Gate = new();
    private const long MaximumLogBytes = 1024 * 1024;

    internal static DragDiagnostics FromEnvironment() => new(
        Environment.GetEnvironmentVariable("SLIPSHOT_DRAG_DIAGNOSTICS") == "1"
            ? Path.Combine(AppContext.BaseDirectory, "data", "diagnostics", "drag.jsonl") : null);

    internal DragTrace Begin() => new(this);

    internal void Write(object entry)
    {
        if (path is null) return;
        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                if (File.Exists(path) && new FileInfo(path).Length >= MaximumLogBytes)
                    File.Move(path, path + ".1", overwrite: true);
                File.AppendAllText(path, JsonSerializer.Serialize(entry) + Environment.NewLine);
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException) { }
    }
}

internal sealed class DragTrace(DragDiagnostics diagnostics)
{
    private readonly string id = Guid.NewGuid().ToString("N");
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private int requests;

    internal void Record(string stage, string? format = null, bool? available = null, long? bytes = null,
        int? width = null, int? height = null, bool? accepted = null, bool? retained = null,
        Exception? error = null, int? effect = null, bool? leftButtonDown = null)
    {
        // Bound chatty target probes while always keeping terminal events.
        if (format is not null && ++requests > 128) return;
        string? safeFormat = format is null ? null : format switch
        {
            "FileDrop" or "FileName" or "FileNameW" => "FileDrop",
            "Bitmap" => "Bitmap",
            "PNG" => "PNG",
            _ => "other"
        };
        diagnostics.Write(new { utc = DateTime.UtcNow, id, stage, elapsedMs = clock.ElapsedMilliseconds,
            processId = Environment.ProcessId, version = typeof(DragTrace).Assembly.GetName().Version?.ToString(),
            format = safeFormat, available, bytes, width, height, accepted, retained, effect, leftButtonDown,
            errorType = error?.GetType().Name, hresult = error?.HResult });
    }
}
