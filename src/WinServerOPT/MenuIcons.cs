using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace WinOpt;

/// <summary>菜单项图标：优先取系统程序关联图标，失败时画简易字形。</summary>
internal static class MenuIcons
{
    private const int Size = 16;
    private static readonly Dictionary<string, Image> Cache = new(StringComparer.OrdinalIgnoreCase);

    // —— 文件 ——
    public static Image Import => Get("import",
        [FileCand(Sys("shell32.dll"), 4), FileCand(Sys("explorer.exe"))],
        DrawImport);

    public static Image Export => Get("export",
        [FileCand(Sys("shell32.dll"), 259), FileCand(Sys("notepad.exe"))],
        DrawExport);

    // —— 工具（已有项） ——
    public static Image Autologon => Get("autologon",
        [FileCand(Sys("netplwiz.exe")), FileCand(Sys("control.exe"))],
        DrawKey);

    public static Image Identity => Get("identity",
        [FileCand(Sys("SystemPropertiesComputerName.exe")), FileCand(Sys("sysdm.cpl")), FileCand(Sys("SystemPropertiesAdvanced.exe"))],
        DrawComputer);

    public static Image SystemInfo => Get("sysinfo",
        [FileCand(Sys("msinfo32.exe"))],
        DrawInfo);

    public static Image Hosts => Get("hosts",
        [FileCand(Sys("notepad.exe"))],
        DrawDoc);

    public static Image EventViewer => Get("eventvwr",
        [FileCand(Sys("eventvwr.exe")), FileCand(Sys("mmc.exe"))],
        DrawLog);

    public static Image GroupPolicy => Get("gpedit",
        [FileCand(Sys("gpedit.msc")), FileCand(Sys("mmc.exe"))],
        DrawShield);

    public static Image Cmd => Get("cmd",
        [FileCand(Sys("cmd.exe"))],
        g => DrawPrompt(g));

    public static Image PowerShell => Get("powershell",
        [FileCand(Sys("WindowsPowerShell\\v1.0\\powershell.exe")), FileCand(Sys("powershell.exe"))],
        g => DrawPrompt(g, Color.FromArgb(0, 120, 215)));

    public static Image TaskScheduler => Get("taskschd",
        [FileCand(Sys("taskschd.msc")), FileCand(Sys("mmc.exe"))],
        DrawClock);

    public static Image ComputerMgmt => Get("compmgmt",
        [FileCand(Sys("compmgmt.msc")), FileCand(Sys("mmc.exe"))],
        DrawComputer);

    public static Image FlushDns => Get("flushdns",
        [FileCand(Sys("ncpa.cpl")), FileCand(Sys("control.exe"))],
        DrawNetwork);

    public static Image CommonSoftware => Get("software",
        [FileCand(Sys("appwiz.cpl")), FileCand(Sys("msiexec.exe"))],
        DrawBox);

    public static Image Cleanup => Get("cleanup",
        [FileCand(Sys("cleanmgr.exe"))],
        DrawTrash);

    public static Image DesktopMaintenance => Get("desktop",
        [FileCand(Sys("explorer.exe")), FileCand(Sys("desk.cpl"))],
        DrawDesktop);

    public static Image Advanced => Get("advanced",
        [FileCand(Sys("SystemPropertiesAdvanced.exe")), FileCand(Sys("control.exe"))],
        DrawGear);

    public static Image WindowsFeatures => Get("optionalfeatures",
        [FileCand(Sys("OptionalFeatures.exe")), FileCand(Sys("optionalfeatures.exe"))],
        DrawWin);

    /// <summary>右键菜单：鼠标 + 弹出菜单（不用 shell32#0，避免与刷新撞图标）。</summary>
    public static Image ContextMenu => Get("contextmenu",
        Array.Empty<Cand>(),
        DrawContextMenu);

    public static Image Quick => Get("quick",
        [FileCand(Sys("control.exe"))],
        DrawWrench);

    /// <summary>刷新：圆形箭头（不用 shell32#0）。</summary>
    public static Image Refresh => Get("refresh",
        Array.Empty<Cand>(),
        DrawRefresh);

    public static Image Restore => Get("restore",
        [FileCand(Sys("rstrui.exe")), FileCand(Sys("SystemPropertiesProtection.exe"))],
        DrawUndo);

    // —— 视图 ——
    public static Image ViewAllOn => Get("view-allon",
        Array.Empty<Cand>(),
        DrawToggleOn);

    public static Image ViewAllOff => Get("view-alloff",
        Array.Empty<Cand>(),
        DrawToggleOff);

    public static Image ViewHide => Get("view-hide",
        [FileCand(Sys("shell32.dll"), 22)],
        DrawEyeOff);

    public static Image ViewHelpPanel => Get("view-helppanel",
        [FileCand(Sys("hh.exe")), FileCand(Sys("shell32.dll"), 23)],
        DrawPanel);

    // —— 帮助 ——
    public static Image HelpUsage => Get("help-usage",
        [FileCand(Sys("hh.exe")), FileCand(Sys("shell32.dll"), 23)],
        DrawHelp);

    public static Image HelpLegend => Get("help-legend",
        Array.Empty<Cand>(),
        DrawLegend);

    public static Image HelpChangeLog => Get("help-changelog",
        [FileCand(Sys("notepad.exe")), FileCand(Sys("write.exe"))],
        DrawDoc);

    public static Image HelpLog => Get("help-log",
        [FileCand(Sys("eventvwr.exe"))],
        DrawLog);

    public static Image HelpAbout => Get("help-about",
        [FileCand(Sys("winver.exe")), FileCand(Sys("SystemPropertiesAbout.exe"))],
        DrawInfo);

    private struct Cand
    {
        public string Path;
        public int Index;
        public Cand(string path, int index = 0)
        {
            Path = path;
            Index = index;
        }
    }

    private static string Sys(string relative) =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), relative);

    private static Cand FileCand(string path, int index = 0) => new(path, index);

    private static Image Get(string key, Cand[] candidates, Action<Graphics> fallback)
    {
        if (Cache.TryGetValue(key, out var cached))
            return cached;

        Image? img = null;
        foreach (var c in candidates)
        {
            img = FromFile(c.Path, c.Index);
            if (img is not null) break;
        }

        img ??= Draw(fallback);
        Cache[key] = img;
        return img;
    }

    private static Image? FromFile(string path, int index = 0)
    {
        try
        {
            if (!File.Exists(path)) return null;

            var ext = Path.GetExtension(path);
            if (ext.Equals(".dll", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".cpl", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".msc", StringComparison.OrdinalIgnoreCase)
                || index != 0)
            {
                var h = ExtractIcon(IntPtr.Zero, path, index);
                if (h == IntPtr.Zero) return null;
                using var ico = Icon.FromHandle(h);
                var bmp = new Bitmap(ico.ToBitmap(), Size, Size);
                DestroyIcon(h);
                return bmp;
            }

            using var associated = Icon.ExtractAssociatedIcon(path);
            if (associated is null) return null;
            return new Bitmap(associated.ToBitmap(), Size, Size);
        }
        catch
        {
            return null;
        }
    }

    private static Image Draw(Action<Graphics> paint)
    {
        var bmp = new Bitmap(Size, Size);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);
        paint(g);
        return bmp;
    }

    private static void DrawImport(Graphics g)
    {
        using var p = new Pen(Color.FromArgb(0, 120, 215), 1.8f);
        g.DrawRectangle(p, 3, 2, 10, 12);
        g.DrawLine(p, 8, 5, 8, 11);
        g.FillPolygon(Brushes.DodgerBlue, new[] { new Point(8, 12), new Point(5, 8), new Point(11, 8) });
    }

    private static void DrawExport(Graphics g)
    {
        using var p = new Pen(Color.FromArgb(0, 120, 215), 1.8f);
        g.DrawRectangle(p, 3, 2, 10, 12);
        g.DrawLine(p, 8, 11, 8, 5);
        g.FillPolygon(Brushes.DodgerBlue, new[] { new Point(8, 3), new Point(5, 7), new Point(11, 7) });
    }

    private static void DrawKey(Graphics g)
    {
        using var b = new SolidBrush(Color.FromArgb(180, 140, 40));
        g.FillEllipse(b, 2, 3, 7, 7);
        using var p = new Pen(Color.FromArgb(180, 140, 40), 2f);
        g.DrawLine(p, 8, 7, 14, 7);
        g.DrawLine(p, 12, 7, 12, 11);
    }

    private static void DrawComputer(Graphics g)
    {
        using var b = new SolidBrush(Color.FromArgb(70, 110, 160));
        g.FillRectangle(b, 2, 3, 12, 8);
        g.FillRectangle(b, 5, 12, 6, 2);
        using var screen = new SolidBrush(Color.FromArgb(200, 220, 240));
        g.FillRectangle(screen, 3, 4, 10, 6);
    }

    private static void DrawInfo(Graphics g)
    {
        using var b = new SolidBrush(Color.FromArgb(0, 120, 215));
        g.FillEllipse(b, 2, 2, 12, 12);
        using var f = new Font("Segoe UI", 8f, FontStyle.Bold);
        using var w = new SolidBrush(Color.White);
        g.DrawString("i", f, w, 5, 1);
    }

    private static void DrawDoc(Graphics g)
    {
        using var b = new SolidBrush(Color.FromArgb(240, 240, 240));
        using var border = new Pen(Color.FromArgb(100, 100, 100));
        g.FillRectangle(b, 3, 1, 10, 14);
        g.DrawRectangle(border, 3, 1, 10, 14);
        using var line = new Pen(Color.FromArgb(140, 140, 140));
        g.DrawLine(line, 5, 5, 11, 5);
        g.DrawLine(line, 5, 8, 11, 8);
        g.DrawLine(line, 5, 11, 9, 11);
    }

    private static void DrawLog(Graphics g)
    {
        using var b = new SolidBrush(Color.FromArgb(80, 80, 80));
        g.FillRectangle(b, 2, 2, 12, 12);
        using var y = new SolidBrush(Color.FromArgb(240, 190, 40));
        g.FillRectangle(y, 4, 4, 8, 2);
        using var r = new SolidBrush(Color.FromArgb(220, 60, 60));
        g.FillRectangle(r, 4, 8, 8, 2);
        using var gr = new SolidBrush(Color.FromArgb(60, 170, 80));
        g.FillRectangle(gr, 4, 12, 8, 1);
    }

    private static void DrawShield(Graphics g)
    {
        using var b = new SolidBrush(Color.FromArgb(0, 120, 215));
        var pts = new[]
        {
            new Point(8, 1), new Point(14, 4), new Point(14, 9),
            new Point(8, 15), new Point(2, 9), new Point(2, 4),
        };
        g.FillPolygon(b, pts);
    }

    private static void DrawPrompt(Graphics g, Color? accent = null)
    {
        using var b = new SolidBrush(Color.FromArgb(30, 30, 30));
        g.FillRectangle(b, 1, 2, 14, 12);
        using var p = new Pen(accent ?? Color.FromArgb(180, 180, 180), 1.5f);
        g.DrawLines(p, new[] { new Point(4, 5), new Point(7, 8), new Point(4, 11) });
        g.DrawLine(p, 8, 11, 12, 11);
    }

    private static void DrawClock(Graphics g)
    {
        using var b = new SolidBrush(Color.FromArgb(0, 120, 215));
        g.FillEllipse(b, 1, 1, 14, 14);
        using var p = new Pen(Color.White, 1.5f);
        g.DrawLine(p, 8, 8, 8, 4);
        g.DrawLine(p, 8, 8, 11, 10);
    }

    private static void DrawNetwork(Graphics g)
    {
        using var p = new Pen(Color.FromArgb(0, 120, 215), 1.5f);
        g.DrawEllipse(p, 5, 5, 6, 6);
        g.DrawArc(p, 2, 2, 12, 12, 210, 120);
        g.DrawArc(p, 0, 0, 16, 16, 210, 120);
    }

    private static void DrawBox(Graphics g)
    {
        using var b = new SolidBrush(Color.FromArgb(0, 120, 215));
        g.FillRectangle(b, 2, 4, 12, 10);
        using var top = new SolidBrush(Color.FromArgb(100, 170, 240));
        g.FillPolygon(top, new[] { new Point(2, 4), new Point(8, 1), new Point(14, 4) });
    }

    private static void DrawTrash(Graphics g)
    {
        using var b = new SolidBrush(Color.FromArgb(120, 120, 120));
        g.FillRectangle(b, 4, 5, 8, 10);
        g.FillRectangle(b, 3, 3, 10, 2);
        using var p = new Pen(Color.White, 1f);
        g.DrawLine(p, 6, 7, 6, 13);
        g.DrawLine(p, 8, 7, 8, 13);
        g.DrawLine(p, 10, 7, 10, 13);
    }

    private static void DrawDesktop(Graphics g)
    {
        using var b = new SolidBrush(Color.FromArgb(0, 120, 215));
        g.FillRectangle(b, 1, 2, 14, 10);
        using var t = new SolidBrush(Color.FromArgb(70, 70, 70));
        g.FillRectangle(t, 5, 12, 6, 2);
    }

    private static void DrawGear(Graphics g)
    {
        using var b = new SolidBrush(Color.FromArgb(90, 90, 90));
        g.FillEllipse(b, 3, 3, 10, 10);
        using var hole = new SolidBrush(Color.White);
        g.FillEllipse(hole, 6, 6, 4, 4);
    }

    private static void DrawWin(Graphics g)
    {
        using var b = new SolidBrush(Color.FromArgb(0, 120, 215));
        g.FillRectangle(b, 2, 2, 5, 5);
        g.FillRectangle(b, 9, 2, 5, 5);
        g.FillRectangle(b, 2, 9, 5, 5);
        g.FillRectangle(b, 9, 9, 5, 5);
    }

    private static void DrawContextMenu(Graphics g)
    {
        // 弹出菜单
        using var panel = new SolidBrush(Color.FromArgb(250, 250, 250));
        using var border = new Pen(Color.FromArgb(80, 80, 80));
        g.FillRectangle(panel, 1, 1, 9, 11);
        g.DrawRectangle(border, 1, 1, 9, 11);
        using var line = new Pen(Color.FromArgb(60, 60, 60));
        g.DrawLine(line, 3, 4, 8, 4);
        g.DrawLine(line, 3, 7, 8, 7);
        using var hi = new SolidBrush(Color.FromArgb(0, 120, 215));
        g.FillRectangle(hi, 2, 9, 7, 2);

        // 鼠标指针（右下）
        using var mouse = new SolidBrush(Color.FromArgb(40, 40, 40));
        g.FillPolygon(mouse, new[]
        {
            new Point(9, 7), new Point(9, 14), new Point(11, 12),
            new Point(13, 15), new Point(14, 14), new Point(12, 11), new Point(15, 11),
        });
    }

    private static void DrawWrench(Graphics g)
    {
        using var p = new Pen(Color.FromArgb(120, 120, 120), 2.2f);
        g.DrawLine(p, 3, 13, 10, 6);
        g.DrawEllipse(p, 9, 2, 5, 5);
    }

    private static void DrawRefresh(Graphics g)
    {
        using var brush = new SolidBrush(Color.FromArgb(0, 150, 80));
        using var p = new Pen(Color.FromArgb(0, 150, 80), 2f);
        g.DrawArc(p, 2, 2, 12, 12, 35, 250);
        g.FillPolygon(brush, new[] { new Point(13, 1), new Point(16, 6), new Point(10, 6) });
    }

    private static void DrawUndo(Graphics g)
    {
        using var p = new Pen(Color.FromArgb(180, 80, 40), 1.8f);
        g.DrawArc(p, 3, 3, 10, 10, 200, 220);
        g.FillPolygon(new SolidBrush(Color.FromArgb(180, 80, 40)),
            new[] { new Point(3, 3), new Point(8, 3), new Point(5, 8) });
    }

    private static void DrawToggleOn(Graphics g)
    {
        using var track = new SolidBrush(Color.FromArgb(0, 150, 80));
        g.FillRectangle(track, 1, 5, 14, 6);
        using var knob = new SolidBrush(Color.White);
        g.FillEllipse(knob, 8, 4, 8, 8);
    }

    private static void DrawToggleOff(Graphics g)
    {
        using var track = new SolidBrush(Color.FromArgb(160, 160, 160));
        g.FillRectangle(track, 1, 5, 14, 6);
        using var knob = new SolidBrush(Color.White);
        g.FillEllipse(knob, 0, 4, 8, 8);
    }

    private static void DrawEyeOff(Graphics g)
    {
        using var p = new Pen(Color.FromArgb(90, 90, 90), 1.5f);
        g.DrawArc(p, 1, 4, 14, 10, 200, 140);
        g.DrawArc(p, 1, 2, 14, 10, 20, 140);
        g.DrawLine(p, 3, 13, 13, 3);
    }

    private static void DrawPanel(Graphics g)
    {
        using var b = new SolidBrush(Color.FromArgb(230, 240, 250));
        using var border = new Pen(Color.FromArgb(0, 120, 215));
        g.FillRectangle(b, 2, 2, 12, 12);
        g.DrawRectangle(border, 2, 2, 12, 12);
        using var side = new SolidBrush(Color.FromArgb(0, 120, 215));
        g.FillRectangle(side, 10, 3, 3, 10);
    }

    private static void DrawHelp(Graphics g)
    {
        using var b = new SolidBrush(Color.FromArgb(0, 120, 215));
        g.FillEllipse(b, 2, 2, 12, 12);
        using var f = new Font("Segoe UI", 8f, FontStyle.Bold);
        using var w = new SolidBrush(Color.White);
        g.DrawString("?", f, w, 4, 1);
    }

    private static void DrawLegend(Graphics g)
    {
        using var r = new SolidBrush(Color.FromArgb(220, 80, 80));
        g.FillRectangle(r, 2, 2, 4, 4);
        using var y = new SolidBrush(Color.FromArgb(230, 170, 40));
        g.FillRectangle(y, 2, 7, 4, 4);
        using var gr = new SolidBrush(Color.FromArgb(60, 170, 80));
        g.FillRectangle(gr, 2, 12, 4, 3);
        using var line = new Pen(Color.FromArgb(120, 120, 120));
        g.DrawLine(line, 8, 4, 14, 4);
        g.DrawLine(line, 8, 9, 14, 9);
        g.DrawLine(line, 8, 13, 12, 13);
    }

    [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

    [DllImport("User32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
