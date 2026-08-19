using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using WinOpt;

static class Program
{
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
        var radius = size * 0.218f;

        using (var path = RoundedRect(rect, radius))
        {
            using var brush = new LinearGradientBrush(
                rect, BrandPalette.LogoTop, BrandPalette.LogoBottom, 90f, true);
            var blend = new ColorBlend(3)
            {
                Colors = new[] { BrandPalette.LogoTop, BrandPalette.LogoMid, BrandPalette.LogoBottom },
                Positions = new[] { 0f, 0.48f, 1f }
            };
            brush.InterpolationColors = blend;
            g.FillPath(brush, path);

            if (size >= 96)
            {
                using var shine = new LinearGradientBrush(
                    new RectangleF(size * 0.08f, size * 0.06f, size * 0.84f, size * 0.38f),
                    Color.FromArgb(36, 255, 255, 255),
                    Color.FromArgb(0, 255, 255, 255),
                    90f);
                g.FillPath(shine, path);
            }

            if (size >= 64)
            {
                using var edge = new Pen(Color.FromArgb(32, 255, 255, 255), Math.Max(1f, size / 160f));
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

        // Win11 风格四格：略小留白、均匀间距、圆角 pane
        var mark = size * 0.44f;
        var gap = mark * 0.105f;
        var pane = (mark - gap) / 2f;
        var paneRadius = size >= 128 ? pane * 0.16f : size >= 48 ? pane * 0.12f : 0f;
        var left = (size - mark) / 2f;
        var top = (size - mark) / 2f - size * 0.01f;

        if (size >= 128)
        {
            using var shadow = new SolidBrush(Color.FromArgb(28, 0, 48, 96));
            var off = size * 0.012f;
            FillPane(g, left + off, top + off, pane, paneRadius, shadow);
            FillPane(g, left + pane + gap + off, top + off, pane, paneRadius, shadow);
            FillPane(g, left + off, top + pane + gap + off, pane, paneRadius, shadow);
            FillPane(g, left + pane + gap + off, top + pane + gap + off, pane, paneRadius, shadow);
        }

        using var white = new SolidBrush(Color.FromArgb(255, 255, 255));
        FillPane(g, left, top, pane, paneRadius, white);
        FillPane(g, left + pane + gap, top, pane, paneRadius, white);
        FillPane(g, left, top + pane + gap, pane, paneRadius, white);
        FillPane(g, left + pane + gap, top + pane + gap, pane, paneRadius, white);
    }

    static void FillPane(Graphics g, float x, float y, float pane, float radius, Brush brush)
    {
        var r = new RectangleF(x, y, pane, pane);
        if (radius > 0.5f)
        {
            using var p = RoundedRect(r, radius);
            g.FillPath(brush, p);
        }
        else
        {
            g.FillRectangle(brush, x, y, pane, pane);
        }
    }

    static void DrawWindowsMarkTiny(Graphics g, int size)
    {
        var pad = Math.Max(2, (int)(size * 0.2f));
        var inner = size - pad * 2;
        var gap = Math.Max(1, (int)(inner * 0.1f));
        var pane = (inner - gap) / 2;
        using var white = new SolidBrush(Color.White);
        g.FillRectangle(white, pad, pad, pane, pane);
        g.FillRectangle(white, pad + pane + gap, pad, pane, pane);
        g.FillRectangle(white, pad, pad + pane + gap, pane, pane);
        g.FillRectangle(white, pad + pane + gap, pad + pane + gap, pane, pane);
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
