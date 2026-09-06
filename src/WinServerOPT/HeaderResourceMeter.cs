using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WinOpt;

/// <summary>顶栏右侧：CPU / 内存 / 系统盘占用动态显示。</summary>
internal sealed class HeaderResourceMeter : Panel
{
    private readonly Label _cpuText = MakeLabel();
    private readonly Label _memText = MakeLabel();
    private readonly Label _diskText = MakeLabel();
    private readonly MeterBar _cpuBar = new();
    private readonly MeterBar _memBar = new();
    private readonly MeterBar _diskBar = new();
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 1000 };
    private PerformanceCounter? _cpu;
    private bool _cpuPrimed;
    private readonly string _systemDrive;
    private bool _disposed;

    public HeaderResourceMeter()
    {
        Width = 340;
        Height = 48;
        BackColor = AppTheme.PrimaryDeep;
        DoubleBuffered = true;

        _systemDrive = Path.GetPathRoot(Environment.SystemDirectory)?.TrimEnd('\\') ?? "C:";

        // 三段：左 CPU / 中 内存 / 右 磁盘
        PlaceBlock(0, _cpuText, _cpuBar);
        PlaceBlock(114, _memText, _memBar);
        PlaceBlock(228, _diskText, _diskBar);

        Controls.AddRange([_cpuText, _memText, _diskText, _cpuBar, _memBar, _diskBar]);

        _timer.Tick += (_, _) => RefreshValues();
        HandleCreated += (_, _) =>
        {
            TryCreateCpuCounter();
            RefreshValues();
            _timer.Start();
        };
        Disposed += (_, _) => Cleanup();
    }

    private void PlaceBlock(int x, Label text, MeterBar bar)
    {
        text.SetBounds(x, 6, 106, 18);
        bar.SetBounds(x, 28, 100, 6);
    }

    private static Label MakeLabel() => new()
    {
        AutoSize = false,
        ForeColor = AppTheme.TextOnPrimarySoft,
        Font = new Font("Segoe UI", 8.25F, FontStyle.Bold),
        TextAlign = ContentAlignment.MiddleLeft,
        BackColor = Color.Transparent,
        Text = "…",
    };

    private void TryCreateCpuCounter()
    {
        try
        {
            _cpu = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
            _ = _cpu.NextValue(); // 预热
            _cpuPrimed = false;
        }
        catch
        {
            _cpu = null;
        }
    }

    private void RefreshValues()
    {
        if (_disposed || !IsHandleCreated) return;

        float cpu = 0;
        if (_cpu is not null)
        {
            try
            {
                var v = _cpu.NextValue();
                if (!_cpuPrimed)
                {
                    _cpuPrimed = true;
                    v = 0;
                }
                cpu = Math.Max(0, Math.Min(100, v));
            }
            catch
            {
                cpu = 0;
            }
        }

        GetMemory(out var memLoad, out var usedGb, out var totalGb);
        GetDisk(out var diskLoad, out var freeGb, out var diskTotalGb);

        _cpuText.Text = $"CPU  {cpu:0}%";
        _cpuBar.Value = cpu;
        _cpuBar.Accent = AccentFor(cpu);

        _memText.Text = $"内存  {usedGb:0.0}/{totalGb:0.#}G";
        _memBar.Value = memLoad;
        _memBar.Accent = AccentFor(memLoad);

        _diskText.Text = $"{_systemDrive}  剩{freeGb:0.#}G";
        _diskBar.Value = diskLoad;
        _diskBar.Accent = AccentFor(diskLoad);

        _cpuBar.Invalidate();
        _memBar.Invalidate();
        _diskBar.Invalidate();

        var tip =
            $"CPU：{cpu:0.0}%\r\n" +
            $"内存：已用 {usedGb:0.00} GB / 共 {totalGb:0.00} GB（{memLoad:0}%）\r\n" +
            $"系统盘 {_systemDrive}：剩余 {freeGb:0.00} GB / 共 {diskTotalGb:0.00} GB（已用 {diskLoad:0}%）";
        _toolTip ??= new ToolTip { ShowAlways = true, AutoPopDelay = 8000 };
        _toolTip.SetToolTip(this, tip);
        _toolTip.SetToolTip(_cpuText, tip);
        _toolTip.SetToolTip(_memText, tip);
        _toolTip.SetToolTip(_diskText, tip);
    }

    private ToolTip? _toolTip;

    private static Color AccentFor(float pct) =>
        pct >= 90 ? Color.FromArgb(255, 120, 100) :
        pct >= 75 ? Color.FromArgb(255, 190, 90) :
        Color.FromArgb(120, 210, 170);

    private static void GetMemory(out float loadPct, out double usedGb, out double totalGb)
    {
        loadPct = 0;
        usedGb = 0;
        totalGb = 0;
        try
        {
            var st = new MemoryStatusEx { Length = (uint)Marshal.SizeOf<MemoryStatusEx>() };
            if (!GlobalMemoryStatusEx(ref st) || st.TotalPhys == 0) return;
            totalGb = st.TotalPhys / (1024d * 1024d * 1024d);
            var availGb = st.AvailPhys / (1024d * 1024d * 1024d);
            usedGb = Math.Max(0, totalGb - availGb);
            loadPct = st.MemoryLoad;
        }
        catch { /* ignore */ }
    }

    private void GetDisk(out float usedPct, out double freeGb, out double totalGb)
    {
        usedPct = 0;
        freeGb = 0;
        totalGb = 0;
        try
        {
            var root = _systemDrive.EndsWith(":") ? _systemDrive + "\\" : _systemDrive;
            var di = new DriveInfo(root);
            if (!di.IsReady) return;
            totalGb = di.TotalSize / (1024d * 1024d * 1024d);
            freeGb = di.AvailableFreeSpace / (1024d * 1024d * 1024d);
            if (di.TotalSize > 0)
                usedPct = (float)((di.TotalSize - di.AvailableFreeSpace) * 100d / di.TotalSize);
        }
        catch { /* ignore */ }
    }

    private void Cleanup()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Stop();
        _timer.Dispose();
        _toolTip?.Dispose();
        try { _cpu?.Dispose(); } catch { /* ignore */ }
        _cpu = null;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhys;
        public ulong AvailPhys;
        public ulong TotalPageFile;
        public ulong AvailPageFile;
        public ulong TotalVirtual;
        public ulong AvailVirtual;
        public ulong AvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx lpBuffer);

    private sealed class MeterBar : Control
    {
        public float Value { get; set; }
        public Color Accent { get; set; } = Color.FromArgb(120, 210, 170);

        public MeterBar()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.UserPaint
                | ControlStyles.SupportsTransparentBackColor
                | ControlStyles.Opaque,
                true);
            Height = 6;
            BackColor = AppTheme.PrimaryDeep;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            var r = ClientRectangle;
            using (var bg = new SolidBrush(Color.FromArgb(70, 255, 255, 255)))
                g.FillRectangle(bg, r);
            var w = (int)Math.Round(r.Width * Math.Max(0, Math.Min(100, Value)) / 100f);
            if (w > 0)
            {
                using var fg = new SolidBrush(Accent);
                g.FillRectangle(fg, 0, 0, w, r.Height);
            }
        }
    }
}