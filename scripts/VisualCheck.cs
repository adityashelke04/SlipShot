using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SlipShot.Core;
using SlipShot.UI;
using SlipShot.Services;

internal static class VisualCheck
{
    [STAThread]
    private static int Main(string[] args)
    {
        string output = args.Length == 1 ? Path.GetFullPath(args[0]) : Path.GetFullPath("artifacts/visual");
        Directory.CreateDirectory(output);
        RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
        _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        Type theme = typeof(EditorWindow).Assembly.GetType("SlipShot.UI.Theme", true)!;
        MethodInfo configure = theme.GetMethod("Configure", BindingFlags.Public | BindingFlags.Static)!;
        foreach (string appearance in new[] { "Light", "Dark" })
        {
            configure.Invoke(null, new object[] { appearance, true });
            foreach (var size in new[] { new Size(1240, 820), new Size(850, 650), new Size(640, 480) })
            {
                var editor = new EditorWindow(SyntheticDocument(), _ => Task.FromResult(false), _ => Task.CompletedTask, _ => { });
                var content = (FrameworkElement)editor.Content;
                content.DataContext = editor.DataContext;
                TextElement.SetFontFamily(content, new FontFamily("Segoe UI"));
                TextElement.SetFontSize(content, 13);
                editor.Content = null;
                content.Measure(size);
                content.Arrange(new Rect(size));
                content.UpdateLayout();
                var image = new RenderTargetBitmap((int)size.Width, (int)size.Height, 96, 96, PixelFormats.Pbgra32);
                image.Render(content);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                string path = Path.Combine(output, $"editor-{appearance.ToLowerInvariant()}-{size.Width:0}x{size.Height:0}.png");
                using (var stream = File.Create(path)) encoder.Save(stream);
                ((DocumentViewModel)editor.DataContext).Dispose();
                Console.WriteLine(path);
            }
            var sample=SyntheticDocument();
            var monitor=new CaptureMonitor(IntPtr.Zero,new Native.RectI {Right=1920,Bottom=1080},new Native.RectI {Right=1920,Bottom=1040},1);
            var preview=new PreviewWindow(sample.Source,monitor,10,()=>{},()=>Task.CompletedTask,()=>{},()=>{});
            RenderContent(preview,new Size(328,360),Path.Combine(output,$"preview-{appearance.ToLowerInvariant()}.png"));
            var settings=new SettingsWindow(new AppSettings(),_=>null);
            RenderContent(settings,new Size(460,660),Path.Combine(output,$"settings-{appearance.ToLowerInvariant()}.png"));
            foreach(var tool in new[]{ToolKind.Text,ToolKind.Rectangle,ToolKind.Redaction})
            {
                var contextual=new EditorWindow(SyntheticDocument(),_=>Task.FromResult(false),_=>Task.CompletedTask,_=>{});
                typeof(EditorWindow).GetMethod("ChooseTool",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(contextual,new object[]{tool});
                RenderContent(contextual,new Size(640,480),Path.Combine(output,$"editor-{appearance.ToLowerInvariant()}-{tool.ToString().ToLowerInvariant()}-640.png"));
                ((DocumentViewModel)contextual.DataContext).Dispose();
            }
        }
        // Do not close the unshown editor objects: no native lifetime or close-dialog path is exercised.
        return 0;
    }

    private static void RenderContent(Window window,Size size,string path)
    {
        var content=(FrameworkElement)window.Content;window.Content=null;
        TextElement.SetFontFamily(content,new FontFamily("Segoe UI"));TextElement.SetFontSize(content,13);
        content.Measure(size);content.Arrange(new Rect(size));content.UpdateLayout();
        var image=new RenderTargetBitmap((int)size.Width,(int)size.Height,96,96,PixelFormats.Pbgra32);image.Render(content);
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(image));using(var stream=File.Create(path))encoder.Save(stream);Console.WriteLine(path);
    }

    private static EditDocument SyntheticDocument()
    {
        var visual = new DrawingVisual();
        using (DrawingContext drawing = visual.RenderOpen())
        {
            drawing.DrawRectangle(Brushes.White, null, new Rect(0, 0, 960, 540));
            drawing.DrawRectangle(new SolidColorBrush(Color.FromRgb(233, 237, 241)), null, new Rect(0, 0, 960, 70));
            Text(drawing, "Synthetic sample — no desktop content", 28, 26, 18, Brushes.Black);
            Text(drawing, "Release checklist", 30, 46, 105, Brushes.Black);
            Text(drawing, "Review the capture flow", 24, 64, 183, Brushes.DimGray);
            Text(drawing, "Keep private details covered", 24, 64, 248, Brushes.DimGray);
            Text(drawing, "Save a flattened image", 24, 64, 313, Brushes.DimGray);
            Text(drawing, "sample@example.test", 23, 64, 424, Brushes.DimGray);
            drawing.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(224, 235, 230)), null, new Rect(630, 120, 265, 260), 10, 10);
            Text(drawing, "Ready for review", 24, 666, 224, Brushes.DarkSlateGray);
        }
        var source = new RenderTargetBitmap(960, 540, 96, 96, PixelFormats.Pbgra32);
        source.Render(visual);
        source.Freeze();
        var document = new EditDocument(source);
        document.Commit(new DocumentState(new Rect(0, 0, 960, 540), new[]
        {
            new Annotation(Guid.NewGuid(), ToolKind.Rectangle, new Rect(50, 173, 335, 48), Colors.IndianRed, 4),
            new Annotation(Guid.NewGuid(), ToolKind.Arrow, new Rect(410, 154, 190, 50), Colors.IndianRed, 5, Points: new[] { new Point(600,154), new Point(410,204) }),
            new Annotation(Guid.NewGuid(), ToolKind.Highlighter, new Rect(62, 337, 300, 0), Colors.Gold, 20, Points: new[] { new Point(62,337), new Point(362,337) }),
            new Annotation(Guid.NewGuid(), ToolKind.Step, new Rect(18, 180, 34, 34), Colors.SeaGreen, 3, Number: 1),
            new Annotation(Guid.NewGuid(), ToolKind.Text, new Rect(620, 402, 240, 45), Colors.SeaGreen, 3, Text: "Looks good", FontSize: 30),
            new Annotation(Guid.NewGuid(), ToolKind.Redaction, new Rect(60, 423, 263, 34), Colors.Black, 1)
        }));
        document.MarkSaved();
        return document;
    }

    private static void Text(DrawingContext drawing, string value, double size, double x, double y, Brush brush)
        => drawing.DrawText(new FormattedText(value, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface("Segoe UI"), size, brush, 1), new Point(x, y));
}
