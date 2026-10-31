using System;
using System.IO;
using NUnit.Framework;
using SlipShot.Services;

namespace SlipShot.Tests;
public class CaptureFolderTests
{
    private string root="";
    [SetUp] public void Setup(){root=Path.Combine(AppContext.BaseDirectory,"test-temp",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);}
    [TearDown] public void Cleanup(){Directory.Delete(root,true);}
    [Test] public void OpensOneAbsoluteFolderArgumentAndCreatesNoImages()
    {
        string folder=Path.Combine(root,"SlipShot Captures, example");
        var request=CaptureFolder.CreateOpenRequest(folder);
        Assert.That(Directory.Exists(folder),Is.True);Assert.That(Directory.GetFiles(folder),Is.Empty);
        Assert.That(request.FileName,Is.EqualTo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),"explorer.exe")));
        Assert.That(request.UseShellExecute,Is.False);Assert.That(request.ArgumentList,Is.EqualTo(new[]{folder}));
        Assert.That(request.Arguments,Is.Empty);
    }
    [Test] public void ExistingImagesArePreserved()
    {
        string sentinel=Path.Combine(root,"existing.png");File.WriteAllText(sentinel,"synthetic sentinel");
        CaptureFolder.CreateOpenRequest(root);Assert.That(File.ReadAllText(sentinel),Is.EqualTo("synthetic sentinel"));
    }
    [Test] public void AFileOrRelativePathCannotBecomeALaunchTarget()
    {
        string file=Path.Combine(root,"not-a-folder.exe");File.WriteAllText(file,"synthetic sentinel");
        Assert.Catch<IOException>(()=>CaptureFolder.CreateOpenRequest(file));
        Assert.Throws<ArgumentException>(()=>CaptureFolder.CreateOpenRequest("relative"));
        Assert.That(File.ReadAllText(file),Is.EqualTo("synthetic sentinel"));
    }
}
