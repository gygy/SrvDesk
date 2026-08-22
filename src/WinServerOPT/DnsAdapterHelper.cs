using System.Management;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;

namespace WinOpt;

internal sealed class DnsAdapterInfo
{
    /// <summary>Win32_NetworkAdapterConfiguration.Index，用于 SetDNSServerSearchOrder。</summary>
    public int ConfigIndex { get; set; }
    public string SettingId { get; set; } = "";
    /// <summary>网络连接中的名称（如「以太网」）。</summary>
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string[] DnsServers { get; set; } = [];
    public bool IsUp { get; set; }
    public bool IpEnabled { get; set; }
    public bool LikelyVirtual { get; set; }

    public string DisplayText
    {
        get
        {
            var dns = !IpEnabled
                ? "（未启用 IP，无法设 DNS）"
                : DnsServers.Length == 0
                    ? "（自动/未设置）"
                    : string.Join(", ", DnsServers.Take(3));
            var tag = LikelyVirtual ? " [虚拟]" : IsUp ? " [已连接]" : " [未连接]";
            var desc = string.IsNullOrWhiteSpace(Description) ||
                       Description.Equals(Name, StringComparison.OrdinalIgnoreCase)
                ? ""
                : "  ·  " + Description;
            return Name + tag + desc + "  ·  " + dns;
        }
    }

    public override string ToString() => DisplayText;
}

internal static class DnsAdapterHelper
{
    private static readonly string[] VirtualKeywords =
    [
        "vmware", "virtualbox", "hyper-v", "vethernet", "vpn", "tap-", "tun",
        "loopback", "docker", "wsl", "bluetooth", "wi-fi direct", "microsoft kernel debug",
        "pseudo", "npcap", "openvpn", "wireguard", "zerotier", "hamachi", "tailscale",
        "virtual port", "virtual adapter", "virtual network",
    ];

    /// <summary>
    /// 列出与「网络连接」(ncpa.cpl) 一致的网卡：有 NetConnectionID 的适配器，
    /// 通过 GUID/SettingID 关联配置，避免 Index 错位与仅显示硬件描述名。
    /// </summary>
    public static List<DnsAdapterInfo> ListIpEnabledAdapters()
    {
        var configsBySettingId = new Dictionary<string, ConfigRow>(StringComparer.OrdinalIgnoreCase);
        var configsByIndex = new Dictionary<int, ConfigRow>();
        try
        {
            using var cfgSearcher = new ManagementObjectSearcher(
                "SELECT Index, Description, SettingID, DNSServerSearchOrder, IPEnabled FROM Win32_NetworkAdapterConfiguration");
            foreach (ManagementObject mo in cfgSearcher.Get())
            {
                using (mo)
                {
                    var settingId = NormalizeGuid(Convert.ToString(mo["SettingID"]));
                    var row = new ConfigRow
                    {
                        Index = Convert.ToInt32(mo["Index"] ?? -1),
                        Description = Convert.ToString(mo["Description"]) ?? "",
                        SettingId = settingId,
                        DnsServers = mo["DNSServerSearchOrder"] as string[] ?? [],
                        IpEnabled = mo["IPEnabled"] is true,
                    };
                    if (settingId.Length > 0) configsBySettingId[settingId] = row;
                    if (row.Index >= 0) configsByIndex[row.Index] = row;
                }
            }
        }
        catch { /* fall through */ }

        var nicById = new Dictionary<string, NetworkInterface>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                var id = NormalizeGuid(nic.Id);
                if (id.Length > 0) nicById[id] = nic;
            }
        }
        catch { /* ignore */ }

        var list = new List<DnsAdapterInfo>();
        var seenSetting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            // 与「网络连接」一致：仅有连接名称的适配器
            using var adpSearcher = new ManagementObjectSearcher(
                "SELECT Index, Name, NetConnectionID, Description, GUID, NetConnectionStatus, NetEnabled " +
                "FROM Win32_NetworkAdapter WHERE NetConnectionID IS NOT NULL");
            foreach (ManagementObject mo in adpSearcher.Get())
            {
                using (mo)
                {
                    var name = (Convert.ToString(mo["NetConnectionID"]) ?? "").Trim();
                    if (name.Length == 0) continue;

                    var guid = NormalizeGuid(Convert.ToString(mo["GUID"]));
                    var adapterIndex = Convert.ToInt32(mo["Index"] ?? -1);
                    var desc = (Convert.ToString(mo["Description"]) ?? Convert.ToString(mo["Name"]) ?? "").Trim();

                    ConfigRow? cfg = null;
                    if (guid.Length > 0) configsBySettingId.TryGetValue(guid, out cfg);
                    if (cfg is null && adapterIndex >= 0) configsByIndex.TryGetValue(adapterIndex, out cfg);

                    NetworkInterface? nic = null;
                    if (guid.Length > 0) nicById.TryGetValue(guid, out nic);

                    var dns = cfg?.DnsServers ?? [];
                    if (dns.Length == 0 && nic is not null)
                        dns = ReadDnsFromNic(nic);

                    var ipEnabled = cfg?.IpEnabled == true;
                    var status = Convert.ToInt32(mo["NetConnectionStatus"] ?? 0);
                    var isUp = status == 2 ||
                               (nic is not null && nic.OperationalStatus == OperationalStatus.Up);

                    var settingId = cfg?.SettingId ?? guid;
                    if (settingId.Length > 0 && !seenSetting.Add(settingId))
                        continue;

                    list.Add(new DnsAdapterInfo
                    {
                        ConfigIndex = cfg?.Index ?? adapterIndex,
                        SettingId = settingId,
                        Name = name,
                        Description = string.IsNullOrWhiteSpace(cfg?.Description) ? desc : cfg!.Description,
                        DnsServers = dns,
                        IsUp = isUp,
                        IpEnabled = ipEnabled,
                        LikelyVirtual = IsLikelyVirtual(name, desc),
                    });
                }
            }
        }
        catch
        {
            // WMI 失败时回退
        }

        // 若 WMI 未列出任何带连接名的网卡，回退到 .NET NetworkInterface + SettingID
        if (list.Count == 0)
            list.AddRange(ListFromNetworkInterfaceFallback(configsBySettingId));

        return list
            .OrderBy(a => a.LikelyVirtual)
            .ThenByDescending(a => a.IsUp)
            .ThenByDescending(a => a.IpEnabled)
            .ThenBy(a => a.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public static int ApplyDns(IEnumerable<int> indexes, string[]? servers) =>
        ApplyDns(indexes, settingIds: null, servers);

    public static int ApplyDns(IEnumerable<int> indexes, IEnumerable<string>? settingIds, string[]? servers)
    {
        var indexSet = indexes.ToHashSet();
        var idSet = new HashSet<string>(
            (settingIds ?? []).Select(NormalizeGuid).Where(s => s.Length > 0),
            StringComparer.OrdinalIgnoreCase);
        if (indexSet.Count == 0 && idSet.Count == 0)
            throw new InvalidOperationException("请至少勾选一块网卡。");

        var ok = 0;
        using var searcher = new ManagementObjectSearcher(
            "SELECT Index, SettingID, IPEnabled FROM Win32_NetworkAdapterConfiguration");
        foreach (ManagementObject mo in searcher.Get())
        {
            using (mo)
            {
                var index = Convert.ToInt32(mo["Index"] ?? -1);
                var settingId = NormalizeGuid(Convert.ToString(mo["SettingID"]));
                var match = (settingId.Length > 0 && idSet.Contains(settingId)) || indexSet.Contains(index);
                if (!match) continue;
                if (mo["IPEnabled"] is not true)
                    throw new InvalidOperationException($"网卡配置 Index={index} 未启用 IP，无法设置 DNS。");

                var result = (uint)mo.InvokeMethod("SetDNSServerSearchOrder", new object?[] { servers });
                if (result is not 0 and not 1)
                    throw new InvalidOperationException($"网卡 Index={index} 设置失败，WMI 返回 {result}。");
                ok++;
            }
        }

        if (ok == 0) throw new InvalidOperationException("未找到勾选的网卡（可能已断开）。请刷新后重试。");
        return ok;
    }

    private static List<DnsAdapterInfo> ListFromNetworkInterfaceFallback(
        Dictionary<string, ConfigRow> configsBySettingId)
    {
        var list = new List<DnsAdapterInfo>();
        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                var guid = NormalizeGuid(nic.Id);
                configsBySettingId.TryGetValue(guid, out var cfg);
                var dns = cfg?.DnsServers ?? ReadDnsFromNic(nic);
                list.Add(new DnsAdapterInfo
                {
                    ConfigIndex = cfg?.Index ?? -1,
                    SettingId = guid,
                    Name = nic.Name,
                    Description = nic.Description,
                    DnsServers = dns,
                    IsUp = nic.OperationalStatus == OperationalStatus.Up,
                    IpEnabled = cfg?.IpEnabled == true || nic.OperationalStatus == OperationalStatus.Up,
                    LikelyVirtual = IsLikelyVirtual(nic.Name, nic.Description),
                });
            }
        }
        catch { /* ignore */ }
        return list;
    }

    private static string[] ReadDnsFromNic(NetworkInterface nic)
    {
        try
        {
            return nic.GetIPProperties().DnsAddresses
                .Select(a => a.ToString())
                .Where(s => s.Length > 0 && s != "::")
                .ToArray();
        }
        catch { return []; }
    }

    private static string NormalizeGuid(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        var s = raw!.Trim();
        var m = Regex.Match(s, @"[0-9A-Fa-f]{8}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{4}-[0-9A-Fa-f]{12}");
        return m.Success ? "{" + m.Value.ToUpperInvariant() + "}" : s.ToUpperInvariant();
    }

    private static bool IsLikelyVirtual(string name, string description)
    {
        var text = (name + " " + description).ToLowerInvariant();
        // 避免把 VirtIO 物理/半虚拟网卡误判为「virtual」
        if (text.Contains("virtio") && !text.Contains("vmware") && !text.Contains("hyper-v"))
            return false;
        return VirtualKeywords.Any(k => text.Contains(k));
    }

    private sealed class ConfigRow
    {
        public int Index;
        public string Description = "";
        public string SettingId = "";
        public string[] DnsServers = [];
        public bool IpEnabled;
    }
}
