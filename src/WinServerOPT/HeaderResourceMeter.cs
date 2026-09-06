using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WinOpt;

/// <summary>顶栏右侧：CPU / 内存 / 系统盘占用（纯文字）。</summary>
internal sealed class HeaderResourceMeter : Panel
{
    private readonly Label _text = new()
    {
        AutoSize = false,
        Dock = DockStyle.Fill,
        ForeColor = AppTheme.TextOnPrimarySoft,
        Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
        TextAlign = ContentAlignment.MiddleRight,
        BackColor = Color.Transparent,
        Text = "…",
    };
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 1000 };
    private PerformanceCounter? _cpu;
    private bool _cpuPrimed;
    private readonly string _systemDrive;
    private ToolTip? _toolTip;
    private bool _disposed;

    public HeaderResourceMeter()
    {
        Width = 300;
        Height = 48;
        BackColor = Color.Transparent;
        DoubleBuffered = true;

        _systemDrive = Path.GetPathRoot(Environment.SystemDirectory)?.TrimEnd('\\') ?? "C:";
        Controls.Add(_text);

        _timer.Tick += (_, _) => RefreshValues();
        HandleCreated += (_, _) =>
        {
            TryCreateCpuCounter();
            RefreshValues();
            _timer.Start();
        };
        Disposed += (_, _) => Cleanup();
    }

    private void TryCreateCpuCounter()
    {
        try
        {
            _cpu = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
            _ = _cpu.NextValue();
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

        _text.Text = $"CPU {cpu:0}%    内存 {usedGb:0.0}G    {_systemDrive} 剩{freeGb:0.#}G";

        var tip =
            $"CPU：{cpu:0.0}%\r\n" +
            $"内存：已用 {usedGb:0.00} GB / 共 {totalGb:0.00} GB（{memLoad:0}%）\r\n" +
            $"系统盘 {_systemDrive}：剩余 {freeGb:0.00} GB / 共 {diskTotalGb:0.00} GB（已用 {diskLoad:0}%）";
        _toolTip ??= new ToolTip { ShowAlways = true, AutoPopDelay = 8000 };
        _toolTip.SetToolTip(this, tip);
        _toolTip.SetToolTip(_text, tip);
    }

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
}
