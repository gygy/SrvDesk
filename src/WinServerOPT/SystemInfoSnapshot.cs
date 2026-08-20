using System.Management;
using System.Net.NetworkInformation;
using System.Text;
using Microsoft.Win32;

namespace WinOpt;

internal sealed class SystemInfoRow
{
    public string Group { get; set; } = "";
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
}

internal static class SystemInfoSnapshot
{
    public static List<SystemInfoRow> Collect()
    {
        var rows = new List<SystemInfoRow>();
        CollectOs(rows);
        CollectComputer(rows);
        CollectCpu(rows);
        CollectMemory(rows);
        CollectDisks(rows);
        CollectNetwork(rows);
        CollectSession(rows);
        return rows;
    }

    public static string ToText(IReadOnlyList<SystemInfoRow> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Win一键优化 · 系统信息");
        sb.AppendLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        string? group = null;
        foreach (var r in rows)
        {
            if (r.Group != group)
            {
                group = r.Group;
                sb.AppendLine();
                sb.AppendLine("[" + group + "]");
            }
            sb.AppendLine(r.Name + "\t" + r.Value);
        }
        return sb.ToString();
    }

    private static void CollectOs(List<SystemInfoRow> rows)
    {
        const string g = "操作系统";
        using var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
            .OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
        var product = key?.GetValue("ProductName") as string ?? "";
        var display = key?.GetValue("DisplayVersion") as string ?? "";
        var build = key?.GetValue("CurrentBuildNumber") as string ?? "";
        var ubr = key?.GetValue("UBR");
        if (ubr is int u && u > 0) build += "." + u;
        var edition = key?.GetValue("EditionID") as string ?? "";
        var installType = key?.GetValue("InstallationType") as string ?? "";

        Add(rows, g, "产品名称", product);
        Add(rows, g, "显示版本", string.IsNullOrWhiteSpace(display) ? "—" : display);
        Add(rows, g, "内部版本", build);
        Add(rows, g, "版本号", Environment.OSVersion.Version.ToString());
        Add(rows, g, "版本类型", edition);
        Add(rows, g, "安装类型", installType);
        Add(rows, g, "系统架构", Environment.Is64BitOperatingSystem ? "64 位" : "32 位");
        Add(rows, g, "Server", Optimizer.IsWindowsServer() ? "是" : "否");
        Add(rows, g, "桌面体验", File.Exists(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe")) ? "有" : "无（Core）");

        using var os = First("SELECT Caption, InstallDate, LastBootUpTime, Locale, RegisteredUser FROM Win32_OperatingSystem");
        if (os is not null)
        {
            Add(rows, g, "完整名称", os["Caption"]?.ToString() ?? product);
            Add(rows, g, "安装日期", FormatWmiDate(os["InstallDate"]));
            Add(rows, g, "上次启动", FormatWmiDate(os["LastBootUpTime"]));
            Add(rows, g, "运行时长", FormatUptime(os["LastBootUpTime"]));
        }
    }

    private static void CollectComputer(List<SystemInfoRow> rows)
    {
        const string g = "计算机";
        try
        {
            var id = ComputerIdentityHelper.Read();
            Add(rows, g, "计算机名", id.ComputerName);
            Add(rows, g, "加入方式", id.PartOfDomain ? "域" : "工作组");
            Add(rows, g, id.PartOfDomain ? "域" : "工作组", id.PartOfDomain ? id.Domain : id.Workgroup);
        }
        catch
        {
            Add(rows, g, "计算机名", Environment.MachineName);
        }

        using var cs = First("SELECT Manufacturer, Model, SystemType FROM Win32_ComputerSystem");
        if (cs is not null)
        {
            Add(rows, g, "制造商", cs["Manufacturer"]?.ToString() ?? "—");
            Add(rows, g, "型号", cs["Model"]?.ToString() ?? "—");
            Add(rows, g, "系统类型", cs["SystemType"]?.ToString() ?? "—");
        }

        using var bios = First("SELECT Manufacturer, SMBIOSBIOSVersion, ReleaseDate FROM Win32_BIOS");
        if (bios is not null)
        {
            Add(rows, g, "BIOS 厂商", bios["Manufacturer"]?.ToString() ?? "—");
            Add(rows, g, "BIOS 版本", bios["SMBIOSBIOSVersion"]?.ToString() ?? "—");
            Add(rows, g, "BIOS 日期", FormatWmiDate(bios["ReleaseDate"]));
        }
    }

    private static void CollectCpu(List<SystemInfoRow> rows)
    {
        const string g = "处理器";
        using var cpu = First("SELECT Name, NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed FROM Win32_Processor");
        if (cpu is null)
        {
            Add(rows, g, "逻辑处理器", Environment.ProcessorCount.ToString());
            return;
        }

        Add(rows, g, "名称", (cpu["Name"]?.ToString() ?? "").Trim());
        Add(rows, g, "物理核心", cpu["NumberOfCores"]?.ToString() ?? "—");
        Add(rows, g, "逻辑处理器", cpu["NumberOfLogicalProcessors"]?.ToString() ?? Environment.ProcessorCount.ToString());
        if (cpu["MaxClockSpeed"] is uint mhz && mhz > 0)
            Add(rows, g, "最大频率", mhz + " MHz");
    }

    private static void CollectMemory(List<SystemInfoRow> rows)
    {
        const string g = "内存";
        using var os = First("SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem");
        if (os is null) return;
        var totalKb = ToULong(os["TotalVisibleMemorySize"]);
        var freeKb = ToULong(os["FreePhysicalMemory"]);
        Add(rows, g, "物理内存", FormatBytes(totalKb * 1024));
        Add(rows, g, "可用内存", FormatBytes(freeKb * 1024));
        if (totalKb > 0)
            Add(rows, g, "已用比例", ((totalKb - freeKb) * 100 / totalKb) + "%");
    }

    private static void CollectDisks(List<SystemInfoRow> rows)
    {
        const string g = "磁盘";
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT DeviceID, FileSystem, Size, FreeSpace FROM Win32_LogicalDisk WHERE DriveType=3");
            foreach (ManagementObject d in searcher.Get())
            {
                using (d)
                {
                    var id = d["DeviceID"]?.ToString() ?? "";
                    var fs = d["FileSystem"]?.ToString() ?? "";
                    var size = ToULong(d["Size"]);
                    var free = ToULong(d["FreeSpace"]);
                    Add(rows, g, id, $"{FormatBytes(size)}（可用 {FormatBytes(free)}）· {fs}");
                }
            }
        }
        catch
        {
            Add(rows, g, "读取失败", "无法查询逻辑磁盘");
        }
    }

    private static void CollectNetwork(List<SystemInfoRow> rows)
    {
        const string g = "网络";
        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up) continue;
                if (nic.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
                    continue;

                var ips = nic.GetIPProperties().UnicastAddresses
                    .Where(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    .Select(a => a.Address.ToString())
                    .ToList();
                if (ips.Count == 0) continue;
                Add(rows, g, nic.Name, string.Join(" / ", ips) + "（" + nic.NetworkInterfaceType + "）");
            }
        }
        catch
        {
            Add(rows, g, "读取失败", "无法枚举网卡");
        }
    }

    private static void CollectSession(List<SystemInfoRow> rows)
    {
        const string g = "当前会话";
        Add(rows, g, "登录用户", Environment.UserDomainName + "\\" + Environment.UserName);
        Add(rows, g, "管理员运行", AdminHelper.IsRunningAsAdministrator() ? "是" : "否");
        Add(rows, g, ".NET", Environment.Version.ToString());
        Add(rows, g, "系统目录", Environment.GetFolderPath(Environment.SpecialFolder.System));
        Add(rows, g, "时区", TimeZoneInfo.Local.DisplayName);
    }

    private static void Add(List<SystemInfoRow> rows, string group, string name, string value) =>
        rows.Add(new SystemInfoRow { Group = group, Name = name, Value = string.IsNullOrWhiteSpace(value) ? "—" : value });

    private static ManagementObject? First(string query)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(query);
            foreach (ManagementObject obj in searcher.Get())
                return obj;
        }
        catch { /* WMI 不可用时跳过 */ }
        return null;
    }

    private static string FormatWmiDate(object? value)
    {
        var s = value?.ToString();
        if (string.IsNullOrWhiteSpace(s) || s!.Length < 14) return "—";
        try
        {
            return ManagementDateTimeConverter.ToDateTime(s).ToString("yyyy-MM-dd HH:mm:ss");
        }
        catch
        {
            return s;
        }
    }

    private static string FormatUptime(object? lastBoot)
    {
        var s = lastBoot?.ToString();
        if (string.IsNullOrWhiteSpace(s)) return "—";
        try
        {
            var boot = ManagementDateTimeConverter.ToDateTime(s);
            var span = DateTime.Now - boot;
            if (span.TotalDays >= 1)
                return $"{(int)span.TotalDays} 天 {span.Hours} 小时 {span.Minutes} 分";
            return $"{span.Hours} 小时 {span.Minutes} 分";
        }
        catch
        {
            return "—";
        }
    }

    private static ulong ToULong(object? v) =>
        v switch
        {
            ulong u => u,
            uint i => i,
            int i when i >= 0 => (ulong)i,
            long l when l >= 0 => (ulong)l,
            _ => 0,
        };

    private static string FormatBytes(ulong bytes)
    {
        if (bytes >= 1024UL * 1024 * 1024 * 1024)
            return (bytes / (1024d * 1024 * 1024 * 1024)).ToString("0.00") + " TB";
        if (bytes >= 1024UL * 1024 * 1024)
            return (bytes / (1024d * 1024 * 1024)).ToString("0.00") + " GB";
        if (bytes >= 1024UL * 1024)
            return (bytes / (1024d * 1024)).ToString("0.0") + " MB";
        return bytes + " B";
    }
}
