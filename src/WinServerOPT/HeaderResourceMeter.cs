using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace WinOpt;

/// <summary>顶栏右侧：本机 IP · CPU / 内存 / 系统盘占用（纯文字）。</summary>
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
    private string _ipText = "…";
    private string _ipTip = "";
    private int _ipRefreshCountdown;

    public HeaderResourceMeter()
    {
        Width = 520;
        Height = 48;
        BackColor = Color.Transparent;
        DoubleBuffered = true;

        _systemDrive = Path.GetPathRoot(Environment.SystemDirectory)?.TrimEnd('\\') ?? "C:";
        Controls.Add(_text);

        _timer.Tick += (_, _) => RefreshValues();
        HandleCreated += (_, _) =>
        {
            TryCreateCpuCounter();
            RefreshIp(force: true);
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

        if (--_ipRefreshCountdown <= 0)
            RefreshIp(force: false);

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

        GetMemory(out var memLoad, out var usedGb, out var totalGb, out var freeGbMem);
        GetDisk(out var diskLoad, out var freeGb, out var diskTotalGb);

        var cpuFree = Math.Max(0, Math.Min(100, 100 - cpu));
        var memFreePct = totalGb > 0
            ? Math.Max(0, Math.Min(100, (int)Math.Round(freeGbMem * 100 / totalGb)))
            : Math.Max(0, 100 - (int)memLoad);

        // IP 在 CPU/内存左侧
        _text.Text =
            $"IP {_ipText}    CPU 剩{cpuFree:0}%    内存 剩{freeGbMem:0.0}G({memFreePct}%)    {_systemDrive} 剩{freeGb:0.#}G";

        var tip =
            (_ipTip.Length > 0 ? _ipTip + "\r\n" : "") +
            $"CPU：占用 {cpu:0.0}% · 空闲 {cpuFree:0.0}%\r\n" +
            $"内存：剩余 {freeGbMem:0.00} GB（{memFreePct}%）· 已用 {usedGb:0.00} GB / 共 {totalGb:0.00} GB\r\n" +
            $"系统盘 {_systemDrive}：剩余 {freeGb:0.00} GB / 共 {diskTotalGb:0.00} GB（已用 {diskLoad:0}%）";
        _toolTip ??= new ToolTip { ShowAlways = true, AutoPopDelay = 8000 };
        _toolTip.SetToolTip(this, tip);
        _toolTip.SetToolTip(_text, tip);
    }

    private void RefreshIp(bool force)
    {
        _ipRefreshCountdown = 5; // 约每 5 秒刷新一次
        try
        {
            var primary = "";
            var lines = new List<string>();
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up) continue;
                if (nic.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
                    continue;

                var ips = nic.GetIPProperties().UnicastAddresses
                    .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
                    .Select(a => a.Address.ToString())
                    .Where(ip => !ip.StartsWith("169.254.", StringComparison.Ordinal))
                    .ToList();
                if (ips.Count == 0) continue;

                lines.Add(nic.Name + "：" + string.Join(" / ", ips));
                if (primary.Length == 0)
                {
                    // 优先以太网/无线；否则取第一张已连接网卡
                    var prefer = nic.NetworkInterfaceType is NetworkInterfaceType.Ethernet
                        or NetworkInterfaceType.Wireless80211
                        or NetworkInterfaceType.GigabitEthernet;
                    if (prefer || primary.Length == 0)
                        primary = ips[0];
                }
            }

            if (primary.Length == 0)
            {
                _ipText = "—";
                _ipTip = "本机 IP：未检测到可用 IPv4";
            }
            else
            {
                _ipText = primary;
                _ipTip = "本机 IP（已连接）：\r\n" + string.Join("\r\n", lines);
            }
        }
        catch
        {
            if (force || _ipText == "…")
            {
                _ipText = "—";
                _ipTip = "本机 IP：读取失败";
            }
        }
    }

    private static void GetMemory(out float loadPct, out double usedGb, out double totalGb, out double freeGb)
    {
        loadPct = 0;
        usedGb = 0;
        totalGb = 0;
        freeGb = 0;
        try
        {
            var st = new MemoryStatusEx { Length = (uint)Marshal.SizeOf<MemoryStatusEx>() };
            if (!GlobalMemoryStatusEx(ref st) || st.TotalPhys == 0) return;
            totalGb = st.TotalPhys / (1024d * 1024d * 1024d);
            freeGb = st.AvailPhys / (1024d * 1024d * 1024d);
            usedGb = Math.Max(0, totalGb - freeGb);
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
