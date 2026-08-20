using System.Management;
using System.Net.NetworkInformation;

namespace WinOpt;

internal sealed class DnsAdapterInfo
{
    public int Index { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string[] DnsServers { get; set; } = [];
    public bool IsUp { get; set; }
    public bool LikelyVirtual { get; set; }

    public string DisplayText
    {
        get
        {
            var dns = DnsServers.Length == 0 ? "（自动/未设置）" : string.Join(", ", DnsServers.Take(3));
            var tag = LikelyVirtual ? " [虚拟]" : IsUp ? " [已连接]" : "";
            return Name + tag + "  ·  " + dns;
        }
    }
}

internal static class DnsAdapterHelper
{
    private static readonly string[] VirtualKeywords =
    [
        "virtual", "vmware", "virtualbox", "hyper-v", "vethernet", "vpn", "tap-", "tun",
        "loopback", "docker", "wsl", "bluetooth", "wi-fi direct", "microsoft kernel debug",
        "pseudo", "npcap", "openvpn", "wireguard", "zerotier", "hamachi",
    ];

    public static List<DnsAdapterInfo> ListIpEnabledAdapters()
    {
        var upNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus == OperationalStatus.Up &&
                    nic.NetworkInterfaceType is not NetworkInterfaceType.Loopback)
                    upNames.Add(nic.Name);
            }
        }
        catch { /* ignore */ }

        var list = new List<DnsAdapterInfo>();
        using var searcher = new ManagementObjectSearcher(
            "SELECT Index, Description, SettingID, DNSServerSearchOrder, IPEnabled FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = TRUE");
        foreach (ManagementObject mo in searcher.Get())
        {
            using (mo)
            {
                var index = Convert.ToInt32(mo["Index"]);
                var desc = Convert.ToString(mo["Description"]) ?? "";
                var name = ResolveFriendlyName(index, desc) ?? desc;
                var dns = mo["DNSServerSearchOrder"] as string[] ?? [];
                var virtualLikely = IsLikelyVirtual(name, desc);
                list.Add(new DnsAdapterInfo
                {
                    Index = index,
                    Name = name,
                    Description = desc,
                    DnsServers = dns,
                    IsUp = upNames.Contains(name) || upNames.Contains(desc),
                    LikelyVirtual = virtualLikely,
                });
            }
        }

        return list
            .OrderBy(a => a.LikelyVirtual)
            .ThenByDescending(a => a.IsUp)
            .ThenBy(a => a.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public static int ApplyDns(IEnumerable<int> indexes, string[]? servers)
    {
        var set = indexes.ToHashSet();
        if (set.Count == 0) throw new InvalidOperationException("请至少勾选一块网卡。");

        var ok = 0;
        using var searcher = new ManagementObjectSearcher(
            "SELECT Index, DNSServerSearchOrder FROM Win32_NetworkAdapterConfiguration WHERE IPEnabled = TRUE");
        foreach (ManagementObject mo in searcher.Get())
        {
            using (mo)
            {
                var index = Convert.ToInt32(mo["Index"]);
                if (!set.Contains(index)) continue;
                var result = (uint)mo.InvokeMethod("SetDNSServerSearchOrder", new object?[] { servers });
                if (result is not 0 and not 1)
                    throw new InvalidOperationException($"网卡 Index={index} 设置失败，WMI 返回 {result}。");
                ok++;
            }
        }

        if (ok == 0) throw new InvalidOperationException("未找到勾选的网卡（可能已断开）。请刷新后重试。");
        return ok;
    }

    private static string? ResolveFriendlyName(int configIndex, string description)
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                $"SELECT NetConnectionID, Index FROM Win32_NetworkAdapter WHERE Index = {configIndex}");
            foreach (ManagementObject mo in searcher.Get())
            {
                using (mo)
                {
                    var id = Convert.ToString(mo["NetConnectionID"]);
                    if (!string.IsNullOrWhiteSpace(id)) return id;
                }
            }
        }
        catch { /* ignore */ }

        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.Description.Equals(description, StringComparison.OrdinalIgnoreCase))
                    return nic.Name;
            }
        }
        catch { /* ignore */ }

        return null;
    }

    private static bool IsLikelyVirtual(string name, string description)
    {
        var text = (name + " " + description).ToLowerInvariant();
        return VirtualKeywords.Any(k => text.Contains(k));
    }
}
