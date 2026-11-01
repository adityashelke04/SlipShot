using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using NUnit.Framework;
using SlipShot.Services;

namespace SlipShot.Tests;

[TestFixture, Apartment(ApartmentState.STA), NonParallelizable]
public sealed class DragDiagnosticsTests
{
    [Test]
    public void TraceExplainsHandoffAndRetentionWithoutImageContentsOrPaths()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "test-temp", Guid.NewGuid().ToString("N"));
        string log = Path.Combine(directory, "diagnostics", "drag.jsonl");
        try
        {
            var store = new DragTransferStore(Path.Combine(directory, "private-path"), new DragDiagnostics(log));
            using DragTransferLease lease = store.Prepare(CoreTests.Image(17, 13));
            _ = lease.Data.GetData(DataFormats.FileDrop, false);
            lease.Complete(false);
            string trace = File.ReadAllText(log);
            Assert.That(trace, Does.Contain("prepared").And.Contain("data_read").And.Contain("completed"));
            Assert.That(trace, Does.Contain("\"retained\":true"));
            Assert.That(trace, Does.Not.Contain(directory).And.Not.Contain(Path.GetFileName(lease.FilePath)));
            Assert.That(trace.Split('\n', StringSplitOptions.RemoveEmptyEntries).All(line => line.StartsWith("{")), Is.True);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Test]
    public void UnwritableDiagnosticsCannotBreakAnOtherwiseValidTransfer()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "test-temp", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string blocked = Path.Combine(directory, "blocked");
        File.WriteAllText(blocked, "preserve");
        try
        {
            var store = new DragTransferStore(Path.Combine(directory, "drag-out"), new DragDiagnostics(Path.Combine(blocked, "drag.jsonl")));
            using DragTransferLease lease = store.Prepare(CoreTests.Image());
            Assert.That(File.Exists(lease.FilePath), Is.True);
            Assert.That(File.ReadAllText(blocked), Is.EqualTo("preserve"));
        }
        finally { Directory.Delete(directory, true); }
    }

    [Test]
    public void RepeatedTargetProbesAreBoundedAndKeepTheCompletionRecord()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "test-temp", Guid.NewGuid().ToString("N"));
        string log = Path.Combine(directory, "drag.jsonl");
        try
        {
            DragTrace trace = new DragDiagnostics(log).Begin();
            for (int i = 0; i < 1000; i++) trace.Record("format_query", "private-format-name");
            trace.Record("native_failed", error: new IOException("private-error-message"));
            trace.Record("completed", retained: false);
            string[] lines = File.ReadAllLines(log);
            Assert.That(lines.Length, Is.LessThan(150));
            Assert.That(lines[^1], Does.Contain("completed"));
            Assert.That(string.Join("", lines), Does.Contain("IOException").And.Not.Contain("private-"));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Test]
    public void FullDiagnosticLogRotatesToOneBoundedPreviousFile()
    {
        string directory = Path.Combine(AppContext.BaseDirectory, "test-temp", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string log = Path.Combine(directory, "drag.jsonl");
        try
        {
            var diagnostics = new DragDiagnostics(log);
            for (int i = 0; i < 3; i++)
            {
                File.WriteAllText(log, new string('x', 1024 * 1024));
                diagnostics.Begin().Record("prepared");
                Assert.That(new FileInfo(log + ".1").Length, Is.EqualTo(1024 * 1024));
                Assert.That(new FileInfo(log).Length, Is.LessThan(1024));
            }
            Assert.That(Directory.GetFiles(directory), Has.Length.EqualTo(2));
        }
        finally { Directory.Delete(directory, true); }
    }
}
