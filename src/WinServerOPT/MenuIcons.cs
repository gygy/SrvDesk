using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace WinOpt;

/// <summary>菜单项图标：优先取系统程序关联图标，失败时画简易字形。</summary>
internal static class MenuIcons
{
    private const int Size = 16;
    private static readonly Dictionary<string, Image> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static Image Autologon => Get("autologon",
        [Sys("netplwiz.exe"), Sys("control.exe")],
        g => DrawKey(g));

    public static Image Identity => Get("identity",
        [Sys("SystemPropertiesComputerName.exe"), Sys("sysdm.cpl"), Sys("SystemPropertiesAdvanced.exe")],
        g => DrawComputer(g));

    public static Image SystemInfo => Get("sysinfo",
        [Sys("msinfo32.exe")],
        g => DrawInfo(g));

    public static Image Hosts => Get("hosts",
        [Sys("notepad.exe")],
        g => DrawDoc(g));

    public static Image EventViewer => Get("eventvwr",
        [Sys("eventvwr.exe"), Sys("mmc.exe")],
        g => DrawLog(g));

    public static Image GroupPolicy => Get("gpedit",
        [Sys("gpedit.msc"), Sys("mmc.exe")],
        g => DrawShield(g));

    public static Image Cmd => Get("cmd",
        [Sys("cmd.exe")],
        g => DrawPrompt(g));

    public static Image PowerShell => Get("powershell",
        [Sys("WindowsPowerShell\\v1.0\\powershell.exe"), Sys("powershell.exe")],
        g => DrawPrompt(g, Color.FromArgb(0, 120, 215)));

    public static Image TaskScheduler => Get("taskschd",
        [Sys("taskschd.msc"), Sys("mmc.exe")],
        g => DrawClock(g));

    public static Image ComputerMgmt => Get("compmgmt",
        [Sys("compmgmt.msc"), Sys("mmc.exe")],
        g => DrawComputer(g));

    public static Image FlushDns => Get("flushdns",
        [Sys("ncpa.cpl"), Sys("control.exe")],
        g => DrawNetwork(g));

    public static Image CommonSoftware => Get("software",
        [Sys("appwiz.cpl"), Sys("msiexec.exe")],
        g => DrawBox(g));

    public static Image Cleanup => Get("cleanup",
        [Sys("cleanmgr.exe")],
        g => DrawTrash(g));

    public static Image DesktopMaintenance => Get("desktop",
        [Sys("explorer.exe"), Sys("desk.cpl")],
        g => DrawDesktop(g));

    public static Image Advanced => Get("advanced",
        [Sys("SystemPropertiesAdvanced.exe"), Sys("control.exe")],
        g => DrawGear(g));

    public static Image WindowsFeatures => Get("optionalfeatures",
        [Sys("OptionalFeatures.exe"), Sys("optionalfeatures.exe")],
        g => DrawWin(g));

    public static Image ContextMenu => Get("contextmenu",
        [Sys("shell32.dll"), Sys("explorer.exe")],
        g => DrawMenu(g));

    public static Image Quick => Get("quick",
        [Sys("control.exe")],
        g => DrawWrench(g));

    public static Image Refresh => Get("refresh",
        [Sys("shell32.dll")],
        g => DrawRefresh(g));

    public static Image Restore => Get("restore",
        [Sys("rstrui.exe"), Sys("SystemPropertiesProtection.exe")],
        g => DrawUndo(g));

    private static string Sys(string relative) =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), relative);

    private static Image Get(string key, string[] candidates, Action<Graphics> fallback)
    {
        if (Cache.TryGetValue(key, out var cached))
            return cached;

        Image? img = null;
        foreach (var path in candidates)
        {
            img = FromFile(path);
            if (img is not null) break;
        }

        img ??= Draw(fallback);
        Cache[key] = img;
        return img;
    }

    private static Image? FromFile(string path)
    {
        try
        {
            if (!File.Exists(path)) return null;
            if (path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                var h = ExtractIcon(IntPtr.Zero, path, 0);
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

    private static void DrawMenu(Graphics g)
    {
        using var b = new SolidBrush(Color.FromArgb(240, 240, 240));
        using var border = new Pen(Color.FromArgb(100, 100, 100));
        g.FillRectangle(b, 2, 2, 12, 12);
        g.DrawRectangle(border, 2, 2, 12, 12);
        using var line = new Pen(Color.FromArgb(60, 60, 60));
        g.DrawLine(line, 4, 5, 12, 5);
        g.DrawLine(line, 4, 8, 12, 8);
        g.DrawLine(line, 4, 11, 10, 11);
    }

    private static void DrawWrench(Graphics g)
    {
        using var p = new Pen(Color.FromArgb(120, 120, 120), 2.2f);
        g.DrawLine(p, 3, 13, 10, 6);
        g.DrawEllipse(p, 9, 2, 5, 5);
    }

    private static void DrawRefresh(Graphics g)
    {
        using var p = new Pen(Color.FromArgb(0, 120, 215), 1.8f);
        g.DrawArc(p, 2, 2, 12, 12, 40, 260);
        g.FillPolygon(Brushes.DodgerBlue, new[] { new Point(12, 2), new Point(15, 6), new Point(10, 6) });
    }

    private static void DrawUndo(Graphics g)
    {
        using var p = new Pen(Color.FromArgb(180, 80, 40), 1.8f);
        g.DrawArc(p, 3, 3, 10, 10, 200, 220);
        g.FillPolygon(new SolidBrush(Color.FromArgb(180, 80, 40)),
            new[] { new Point(3, 3), new Point(8, 3), new Point(5, 8) });
    }

    [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

    [DllImport("User32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
