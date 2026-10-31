using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

internal static class BuildBrandAssets
{
    private static readonly int[] Sizes = {256,16,20,24,32,40,48,64,128};
    private static readonly SolidColorBrush Tile = new(Color.FromRgb(0x14,0x14,0x13));

    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Length != 1) throw new ArgumentException("Usage: BuildBrandAssets <repository-root>");
        string root = Path.GetFullPath(args[0]);
        string assets = Path.Combine(root, "src", "SlipShot", "Assets");
        string audit = Path.Combine(root, "artifacts", "brand-audit");
        Directory.CreateDirectory(audit);
        RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
        var mark = ReadMark(Path.Combine(assets, "frame-s.svg"));
        var frames = Sizes.Select(size => RenderIcon(mark, size)).ToArray();
        var payloads = frames.Select(EncodePng).ToArray();
        using (var writer = new BinaryWriter(File.Create(Path.Combine(assets, "slipshot.ico"))))
        {
            writer.Write((ushort)0); writer.Write((ushort)1); writer.Write((ushort)Sizes.Length);
            int offset = 6 + 16 * Sizes.Length;
            for (int i = 0; i < Sizes.Length; i++)
            {
                writer.Write((byte)(Sizes[i] == 256 ? 0 : Sizes[i]));
                writer.Write((byte)(Sizes[i] == 256 ? 0 : Sizes[i]));
                writer.Write((byte)0); writer.Write((byte)0);
                writer.Write((ushort)1); writer.Write((ushort)32);
                writer.Write(payloads[i].Length); writer.Write(offset);
                offset += payloads[i].Length;
            }
            foreach (byte[] payload in payloads) writer.Write(payload);
        }
        foreach (var frame in frames) File.WriteAllBytes(Path.Combine(audit, $"frame-s-{frame.PixelWidth}.png"), EncodePng(frame));
        RenderContactSheet(frames, Path.Combine(audit, "icon-sizes.png"));
        RenderMasters(assets, Path.Combine(audit, "vector-masters.png"));
        RenderSmallMasters(assets,Path.Combine(audit,"concept-small-sizes.png"));
        Console.WriteLine("Generated Assets/slipshot.ico and synthetic artifacts/brand-audit previews.");
    }

    private static DrawingGroup ReadMark(string path)
    {
        var document = XDocument.Load(path);
        var root = document.Root ?? throw new InvalidDataException("SVG root is missing.");
        XNamespace svg = "http://www.w3.org/2000/svg";
        if (root.Name != svg + "svg" || (string?)root.Attribute("viewBox") != "0 0 64 64")
            throw new InvalidDataException("Brand masters must use the 64px SVG view box.");
        var mark = new DrawingGroup();
        foreach (var pathElement in root.Elements())
        {
            if (pathElement.Name != svg + "path" || pathElement.Attributes().Any(a => a.Name != "fill" && a.Name != "d"))
                throw new InvalidDataException("Brand masters support filled paths only, without external references.");
            string fill = (string?)pathElement.Attribute("fill") ?? "";
            if (fill != "#D97757" && fill != "#141413") throw new InvalidDataException("Unexpected brand color.");
            var geometry = Geometry.Parse((string?)pathElement.Attribute("d") ?? throw new InvalidDataException("Missing path data."));
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(fill));
            mark.Children.Add(new GeometryDrawing(brush, null, geometry));
        }
        if (mark.Children.Count == 0 || mark.Bounds.IsEmpty || !new Rect(0,0,64,64).Contains(mark.Bounds))
            throw new InvalidDataException("Brand geometry is empty or outside its view box.");
        mark.Freeze();
        return mark;
    }

    private static void RenderSmallMasters(string assets,string path)
    {
        var visual=new DrawingVisual();
        using(var dc=visual.RenderOpen())
        {
            dc.DrawRectangle(Brushes.White,null,new Rect(0,0,640,240));
            var names=new[]{"frame-s","lifted-slip","open-shutter"};
            for(int row=0;row<names.Length;row++)
            {
                dc.DrawText(new FormattedText(names[row],System.Globalization.CultureInfo.InvariantCulture,FlowDirection.LeftToRight,new Typeface("Segoe UI"),14,Brushes.Black,1),new Point(12,30+row*76));
                var mark=ReadMark(Path.Combine(assets,names[row]+".svg"));
                var sizes=new[]{16,24,32,64};
                for(int column=0;column<sizes.Length;column++)DrawMark(dc,mark,new Rect(160+column*110,10+row*76,sizes[column],sizes[column]));
            }
        }
        File.WriteAllBytes(path,EncodePng(Render(visual,640,240)));
    }

    private static BitmapSource RenderIcon(DrawingGroup mark, int size)
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawRoundedRectangle(Tile, null, new Rect(0.5,0.5,size-1,size-1), size * 0.2, size * 0.2);
            DrawMark(dc, mark, new Rect(size * 0.15,size * 0.15,size * 0.7,size * 0.7));
        }
        return Render(visual, size, size);
    }

    private static void DrawMark(DrawingContext dc, DrawingGroup mark, Rect box)
    {
        Rect bounds = mark.Bounds;
        double scale = Math.Min(box.Width / bounds.Width, box.Height / bounds.Height);
        dc.PushTransform(new MatrixTransform(scale,0,0,scale,
            box.X + (box.Width - bounds.Width * scale) / 2 - bounds.X * scale,
            box.Y + (box.Height - bounds.Height * scale) / 2 - bounds.Y * scale));
        dc.DrawDrawing(mark);
        dc.Pop();
    }

    private static void RenderContactSheet(BitmapSource[] frames, string path)
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawRectangle(Brushes.White, null, new Rect(0,0,880,320));
            double left = 20;
            foreach (var frame in frames.OrderBy(f => f.PixelWidth))
            {
                dc.DrawImage(frame, new Rect(left,(320-frame.PixelHeight)/2,frame.PixelWidth,frame.PixelHeight));
                left += frame.PixelWidth + 20;
            }
        }
        File.WriteAllBytes(path, EncodePng(Render(visual,880,320)));
    }

    private static void RenderMasters(string assets, string path)
    {
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawRectangle(Brushes.White, null, new Rect(0,0,720,480));
            string[] names = {"frame-s","lifted-slip","open-shutter"};
            for (int col = 0; col < names.Length; col++)
            for (int row = 0; row < 2; row++)
            {
                var mark = ReadMark(Path.Combine(assets, names[col] + (row == 0 ? "" : "-dark") + ".svg"));
                DrawMark(dc, mark, new Rect(col * 240 + 40,row * 240 + 40,160,160));
            }
        }
        File.WriteAllBytes(path, EncodePng(Render(visual,720,480)));
    }

    private static BitmapSource Render(DrawingVisual visual, int width, int height)
    {
        var image = new RenderTargetBitmap(width,height,96,96,PixelFormats.Pbgra32);
        image.Render(visual); image.Freeze();
        return image;
    }

    private static byte[] EncodePng(BitmapSource image)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }
}
