using System;
using System.IO;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NUnit.Framework;
using SlipShot.Services;

namespace SlipShot.Tests;
[TestFixture,Apartment(ApartmentState.STA),NonParallelizable]
public class QuickSaveTests
{
    private string root="";
    [SetUp] public void Setup(){root=Path.Combine(AppContext.BaseDirectory,"test-temp",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);}
    [TearDown] public void Cleanup(){Directory.Delete(root,true);}
    [Test] public void SaveCreatesFolderAndRepeatedSaveNeverOverwrites()
    {
        string folder=Path.Combine(root,"SlipShot Captures");var now=new DateTime(2026,10,30,18,0,0);
        string first=ImageFiles.QuickSave(CoreTests.Image(11,7),folder,now);byte[] original=File.ReadAllBytes(first);
        string second=ImageFiles.QuickSave(CoreTests.Image(3,2),folder,now);
        Assert.That(Path.GetDirectoryName(first),Is.EqualTo(folder));Assert.That(first,Does.EndWith("_001.png"));Assert.That(second,Does.EndWith("_002.png"));
        Assert.That(File.ReadAllBytes(first),Is.EqualTo(original));
        using var file=File.OpenRead(second);var image=BitmapDecoder.Create(file,BitmapCreateOptions.None,BitmapCacheOption.OnLoad).Frames[0];
        Assert.That(image.PixelWidth,Is.EqualTo(3));Assert.That(image.PixelHeight,Is.EqualTo(2));
    }
    [Test] public void InvalidOrBlockedFolderFailsWithoutFallbackOrPartialImage()
    {
        string blocked=Path.Combine(root,"blocked");File.WriteAllText(blocked,"preserve");
        Assert.Catch<IOException>(()=>ImageFiles.QuickSave(CoreTests.Image(),blocked,DateTime.Now));
        Assert.That(File.ReadAllText(blocked),Is.EqualTo("preserve"));Assert.That(Directory.GetFiles(root),Has.Length.EqualTo(1));
        Assert.Throws<ArgumentException>(()=>ImageFiles.QuickSave(CoreTests.Image(),"relative-folder",DateTime.Now));
    }
}
