using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

static class Program
{
    static readonly Color Top = Color.FromArgb(72, 158, 255);
    static readonly Color Mid = Color.FromArgb(38, 132, 245);
    static readonly Color Bot = Color.FromArgb(16, 98, 210);

    static int Main(string[] args)
    {
        var outDir = args.Length > 0 ? args[0] : ".";
        Directory.CreateDirectory(outDir);

        var pngPath = Path.Combine(outDir, "app.png");
        var icoPath = Path.Combine(outDir, "app.ico");

        using (var master = Render(512))
            master.Save(pngPath, ImageFormat.Png);

        var sizes = new[] { 16, 24, 32, 48, 64, 128, 256 };
        var icons = sizes.Select(Render).ToArray();
        SaveIco(icoPath, icons);
        foreach (var bmp in icons) bmp.Dispose();

        Console.WriteLine($"Generated: {pngPath}");
        Console.WriteLine($"Generated: {icoPath}");
        return 0;
    }

    static Bitmap Render(int size)
    {
        if (size <= 24) return RenderCore(size);

        var scale = size >= 256 ? 4 : size >= 128 ? 3 : 2;
        using var hi = RenderCore(size * scale);
        var lo = new Bitmap(size, size);
        using (var g = Graphics.FromImage(lo))
        {
            SetQuality(g);
            g.Clear(Color.Transparent);
            g.DrawImage(hi, new Rectangle(0, 0, size, size));
        }
        return lo;
    }

    static Bitmap RenderCore(int size)
    {
        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        SetQuality(g);
        g.Clear(Color.Transparent);

        var rect = new RectangleF(0, 0, size, size);
        var radius = size * 0.225f;
        using (var path = RoundedRect(rect, radius))
        {
            using var brush = new LinearGradientBrush(rect, Top, Bot, 135f, true);
            var blend = new ColorBlend(3)
            {
                Colors = new[] { Top, Mid, Bot },
                Positions = new[] { 0f, 0.42f, 1f }
            };
            brush.InterpolationColors = blend;
            g.FillPath(brush, path);

            if (size >= 48)
            {
                using var shine = new LinearGradientBrush(
                    new RectangleF(0, 0, size, size * 0.55f),
                    Color.FromArgb(80, 255, 255, 255),
                    Color.FromArgb(0, 255, 255, 255),
                    90f);
                g.FillPath(shine, path);
            }

            if (size >= 64)
            {
                using var edge = new Pen(Color.FromArgb(55, 255, 255, 255), Math.Max(1f, size / 128f));
                g.DrawPath(edge, path);
            }
        }

        DrawWindowsMark(g, size);
        return bmp;
    }

    static void DrawWindowsMark(Graphics g, int size)
    {
        if (size <= 20)
        {
            DrawWindowsMarkTiny(g, size);
            return;
        }

        var mark = size * 0.50f;
        var gap = Math.Max(1f, mark * 0.12f);
        var pane = (mark - gap) / 2f;
        var paneRadius = size >= 128 ? pane * 0.18f : size >= 48 ? pane * 0.14f : 0f;
        var left = (size - mark) / 2f;
        var top = (size - mark) / 2f;

        FillPane(g, left, top, pane, paneRadius);
        FillPane(g, left + pane + gap, top, pane, paneRadius);
        FillPane(g, left, top + pane + gap, pane, paneRadius);
        FillPane(g, left + pane + gap, top + pane + gap, pane, paneRadius);
    }

    static void FillPane(Graphics g, float x, float y, float pane, float radius)
    {
        var r = new RectangleF(x, y, pane, pane);
        if (radius > 0.5f)
        {
            using var p = RoundedRect(r, radius);
            g.FillPath(Brushes.White, p);
        }
        else
        {
            g.FillRectangle(Brushes.White, x, y, pane, pane);
        }
    }

    static void DrawWindowsMarkTiny(Graphics g, int size)
    {
        var pad = Math.Max(2, (int)(size * 0.22f));
        var inner = size - pad * 2;
        var gap = Math.Max(1, inner / 9);
        var pane = (inner - gap) / 2;
        g.FillRectangle(Brushes.White, pad, pad, pane, pane);
        g.FillRectangle(Brushes.White, pad + pane + gap, pad, pane, pane);
        g.FillRectangle(Brushes.White, pad, pad + pane + gap, pane, pane);
        g.FillRectangle(Brushes.White, pad + pane + gap, pad + pane + gap, pane, pane);
    }

    static GraphicsPath RoundedRect(RectangleF rect, float radius)
    {
        var path = new GraphicsPath();
        var r = Math.Min(radius, Math.Min(rect.Width, rect.Height) / 2f);
        var d = r * 2f;
        if (d <= 0)
        {
            path.AddRectangle(rect);
            return path;
        }
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    static void SetQuality(Graphics g)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        g.CompositingQuality = CompositingQuality.HighQuality;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
    }

    static void SaveIco(string path, Bitmap[] images)
    {
        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);
        bw.Write((ushort)0);
        bw.Write((ushort)1);
        bw.Write((ushort)images.Length);

        var offset = 6 + 16 * images.Length;
        var pngData = new List<byte[]>();
        foreach (var img in images)
        {
            using var s = new MemoryStream();
            img.Save(s, ImageFormat.Png);
            pngData.Add(s.ToArray());
        }

        for (var i = 0; i < images.Length; i++)
        {
            var img = images[i];
            bw.Write((byte)(img.Width >= 256 ? 0 : img.Width));
            bw.Write((byte)(img.Height >= 256 ? 0 : img.Height));
            bw.Write((byte)0);
            bw.Write((byte)0);
            bw.Write((ushort)1);
            bw.Write((ushort)32);
            bw.Write((uint)pngData[i].Length);
            bw.Write((uint)offset);
            offset += pngData[i].Length;
        }

        foreach (var data in pngData) bw.Write(data);
        File.WriteAllBytes(path, ms.ToArray());
    }
}
