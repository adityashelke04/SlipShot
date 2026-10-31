using System;
using System.Diagnostics;
using System.IO;

namespace SlipShot.Services;
internal static class CaptureFolder
{
    internal static ProcessStartInfo CreateOpenRequest(string? configuredFolder)
    {
        string folder=string.IsNullOrWhiteSpace(configuredFolder)?ImageFiles.DefaultSaveDirectory:configuredFolder;
        if(!Path.IsPathFullyQualified(folder))throw new ArgumentException("Choose a full capture-folder path in Settings.");
        folder=Path.GetFullPath(folder);
        Directory.CreateDirectory(folder);
        var request=new ProcessStartInfo
        {
            FileName=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"explorer.exe"),
            UseShellExecute=false
        };
        request.ArgumentList.Add(folder);
        return request;
    }

    internal static void Open(string? configuredFolder)
    {
        using var process=Process.Start(CreateOpenRequest(configuredFolder));
        if(process is null)throw new InvalidOperationException("File Explorer could not be opened.");
    }
}
