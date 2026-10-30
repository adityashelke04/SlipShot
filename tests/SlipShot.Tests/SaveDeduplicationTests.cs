using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using NUnit.Framework;
using SlipShot.Core;
using SlipShot.Services;

namespace SlipShot.Tests;
[TestFixture,Apartment(ApartmentState.STA),NonParallelizable]
public class SaveDeduplicationTests
{
    private string folder="";
    [SetUp] public void Setup(){folder=Path.Combine(AppContext.BaseDirectory,"test-temp",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);}
    [TearDown] public void Cleanup(){Directory.Delete(folder,true);}
    [Test] public void RepeatedSaveOfUnchangedDocumentCreatesOnlyOneFile()
    {
        // Do not Start the controller: no tray, hotkeys, capture or clipboard.
        using var controller=new AppController(Application.Current??new Application {ShutdownMode=ShutdownMode.OnExplicitShutdown});
        typeof(AppController).GetField("settings",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(controller,new AppSettings {SaveDirectory=folder});
        var doc=new EditDocument(CoreTests.Image(4,3));
        var save=typeof(AppController).GetMethod("SaveAsync",BindingFlags.Instance|BindingFlags.NonPublic)!;
        for(int i=0;i<3;i++)Assert.That(((Task<bool>)save.Invoke(controller,new object[]{doc})!).GetAwaiter().GetResult(),Is.True);
        Assert.That(Directory.GetFiles(folder,"*.png"),Has.Length.EqualTo(1));
    }
    [Test] public void PreviewAndReopenedEditorsShareSavedStatusButNotEdits()
    {
        var capture=new EditDocument(CoreTests.Image(4,3));
        var first=capture.Fork();string path=ImageFiles.SaveDocument(first,folder,DateTime.Now);
        Assert.That(capture.IsSaved,Is.True);
        var reopened=capture.Fork();Assert.That(reopened.SavedPath,Is.EqualTo(path));
        Assert.That(ImageFiles.SaveDocument(reopened,folder,DateTime.Now),Is.EqualTo(path));
        reopened.Commit(new DocumentState(new Rect(0,0,2,2),Array.Empty<Annotation>()));
        Assert.That(reopened.IsSaved,Is.False);Assert.That(capture.State.Crop.Width,Is.EqualTo(4));
        Assert.That(capture.IsSaved,Is.True);Assert.That(Directory.GetFiles(folder,"*.png"),Has.Length.EqualTo(1));
    }
    [Test] public void SavedUndoRedoStatesDoNotExportDuplicateVersions()
    {
        var doc=new EditDocument(CoreTests.Image(4,3));string original=ImageFiles.SaveDocument(doc,folder,DateTime.Now);
        doc.Commit(new DocumentState(new Rect(0,0,2,2),Array.Empty<Annotation>()));
        Assert.That(doc.IsDirty,Is.True);string edited=ImageFiles.SaveDocument(doc,folder,DateTime.Now);
        Assert.That(edited,Is.Not.EqualTo(original));doc.Undo();
        Assert.That(doc.IsDirty,Is.False);Assert.That(ImageFiles.SaveDocument(doc,folder,DateTime.Now),Is.EqualTo(original));
        doc.Redo();Assert.That(ImageFiles.SaveDocument(doc,folder,DateTime.Now),Is.EqualTo(edited));
        Assert.That(Directory.GetFiles(folder,"*.png"),Has.Length.EqualTo(2));
    }
    [Test] public void FailedSaveNeverMarksSnapshotSavedAndRetryWorks()
    {
        var doc=new EditDocument(CoreTests.Image(4,3));doc.Commit(new DocumentState(new Rect(0,0,2,2),Array.Empty<Annotation>()));
        string blocked=Path.Combine(folder,"blocked");File.WriteAllText(blocked,"preserve");
        Assert.Catch<IOException>(()=>ImageFiles.SaveDocument(doc,blocked,DateTime.Now));
        Assert.That(doc.IsSaved,Is.False);Assert.That(doc.IsDirty,Is.True);
        ImageFiles.SaveDocument(doc,folder,DateTime.Now);Assert.That(doc.IsSaved,Is.True);Assert.That(doc.IsDirty,Is.False);
    }
    [Test] public void DeletedExportCanBeRecreatedWithoutDuplicatingAnExistingFile()
    {
        var doc=new EditDocument(CoreTests.Image(4,3));string path=ImageFiles.SaveDocument(doc,folder,DateTime.Now);
        File.Delete(path);ImageFiles.RefreshSavedState(doc);Assert.That(doc.IsSaved,Is.False);Assert.That(doc.IsDirty,Is.True);
        string restored=ImageFiles.SaveDocument(doc,folder,DateTime.Now);
        Assert.That(File.Exists(restored),Is.True);Assert.That(Directory.GetFiles(folder,"*.png"),Has.Length.EqualTo(1));
    }
    [Test] public void OverwritingSavedPathInvalidatesTheOlderSnapshotReceipt()
    {
        var doc=new EditDocument(CoreTests.Image(4,3));string path=ImageFiles.SaveDocument(doc,folder,DateTime.Now);
        doc.Commit(new DocumentState(new Rect(0,0,2,2),Array.Empty<Annotation>()));
        ImageFiles.Save(DocumentRenderer.Render(doc),path,overwrite:true);doc.MarkSaved(path);doc.Undo();
        Assert.That(doc.IsSaved,Is.False);Assert.That(doc.IsDirty,Is.True);
        Assert.That(ImageFiles.SaveDocument(doc,folder,DateTime.Now),Is.Not.EqualTo(path));
    }
    [Test] public void SavingAnEditorRefreshesPreviewSaveStatus()
    {
        var capture=new EditDocument(CoreTests.Image());var editor=capture.Fork();int changes=0;
        capture.Changed+=()=>changes++;ImageFiles.SaveDocument(editor,folder,DateTime.Now);
        Assert.That(changes,Is.EqualTo(1));Assert.That(capture.IsSaved,Is.True);
    }
    [Test] public void ExplicitOverwriteByAnotherCaptureInvalidatesItsPreviousOwner()
    {
        var first=new EditDocument(CoreTests.Image(4,3));string path=ImageFiles.SaveDocument(first,folder,DateTime.Now);
        var second=new EditDocument(CoreTests.Image(2,2));ImageFiles.Save(DocumentRenderer.Render(second),path,overwrite:true);
        ImageFiles.RecordSavedFile(second,path,new[]{first,second});
        Assert.That(first.IsSaved,Is.False);Assert.That(first.IsDirty,Is.True);Assert.That(second.IsSaved,Is.True);
        Assert.That(ImageFiles.SaveDocument(first,folder,DateTime.Now),Is.Not.EqualTo(path));
    }
}
