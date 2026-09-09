using System.Runtime.Serialization;
using System.ServiceProcess;

namespace SrvDesk;

[Flags]
internal enum ServerRoleFlags
{
    None = 0,
    RdpDesktop = 1 << 0,
    FileShare = 1 << 1,
    Docker = 1 << 2,
    BrowserDownload = 1 << 3,
    WebIis = 1 << 4,
    HyperV = 1 << 5,
    Database = 1 << 6,
    Print = 1 << 7,
    Vpn = 1 << 8,
    Game = 1 << 9,
    Wsus = 1 << 10,
    HomeLab = 1 << 11,
    SmallBusiness = 1 << 12,
}

[DataContract]
internal sealed class ServerProfileData
{
    [DataMember] public int Roles { get; set; }
    [DataMember] public int OptimizationLevel { get; set; } = (int)SrvDesk.OptimizationLevel.Standard;
    [DataMember] public bool HealthInspectionEnabled { get; set; }
    [DataMember] public string LastInspectionUtc { get; set; } = "";
    [DataMember] public bool ProfileConfigured { get; set; }
}

/// <summary>服务器用途画像：用户勾选 + 自动探测。</summary>
internal static class ServerProfile
{
    private static ServerProfileData? _cache;

    public static ServerProfileData Load()
    {
        if (_cache is not null) return _cache;
        var prefs = UiPrefs.Load();
        _cache = new ServerProfileData
        {
            Roles = prefs.ServerRoles,
            OptimizationLevel = prefs.OptimizationLevel,
            HealthInspectionEnabled = prefs.HealthInspectionEnabled,
            LastInspectionUtc = prefs.LastInspectionUtc ?? "",
            ProfileConfigured = prefs.ServerProfileConfigured,
        };
        return _cache;
    }

    public static void Save(ServerProfileData data)
    {
        _cache = data;
        var prefs = UiPrefs.Load();
        prefs.ServerRoles = data.Roles;
        prefs.OptimizationLevel = data.OptimizationLevel;
        prefs.HealthInspectionEnabled = data.HealthInspectionEnabled;
        prefs.LastInspectionUtc = data.LastInspectionUtc ?? "";
        prefs.ServerProfileConfigured = data.ProfileConfigured;
        UiPrefs.Save(prefs);
    }

    public static ServerRoleFlags Roles => (ServerRoleFlags)Load().Roles;

    public static OptimizationLevel Level =>
        (OptimizationLevel)Math.Max(0, Math.Min(3, Load().OptimizationLevel));

    public static bool Has(ServerRoleFlags flag) => (Roles & flag) != 0;

    public static string Summary()
    {
        var flags = Roles;
        if (flags == ServerRoleFlags.None)
            return AppLang.L("未配置用途（按通用 Server 桌面建议）", "No roles set (generic Server desktop advice)");
        var parts = new List<string>();
        foreach (ServerRoleFlags f in Enum.GetValues(typeof(ServerRoleFlags)))
        {
            if (f == ServerRoleFlags.None || (flags & f) == 0) continue;
            parts.Add(RoleTitle(f));
        }
        return string.Join(" · ", parts);
    }

    public static string RoleTitle(ServerRoleFlags role) => role switch
    {
        ServerRoleFlags.RdpDesktop => AppLang.L("RDP 桌面", "RDP desktop"),
        ServerRoleFlags.FileShare => AppLang.L("文件共享", "File share"),
        ServerRoleFlags.Docker => "Docker",
        ServerRoleFlags.BrowserDownload => AppLang.L("浏览/下载", "Browser/download"),
        ServerRoleFlags.WebIis => "IIS / Web",
        ServerRoleFlags.HyperV => "Hyper-V",
        ServerRoleFlags.Database => AppLang.L("数据库", "Database"),
        ServerRoleFlags.Print => AppLang.L("打印", "Print"),
        ServerRoleFlags.Vpn => "VPN",
        ServerRoleFlags.Game => AppLang.L("游戏服", "Game server"),
        ServerRoleFlags.Wsus => "WSUS",
        ServerRoleFlags.HomeLab => AppLang.L("家庭/实验室", "Home lab"),
        ServerRoleFlags.SmallBusiness => AppLang.L("小型业务", "Small business"),
        _ => role.ToString(),
    };

    /// <summary>服务是否因当前用途必须保留。</summary>
    public static bool MustKeepService(string serviceName)
    {
        var n = serviceName ?? "";
        if (Has(ServerRoleFlags.Print) && n.Equals("Spooler", StringComparison.OrdinalIgnoreCase))
            return true;
        if (Has(ServerRoleFlags.WebIis) &&
            (n.Equals("W3SVC", StringComparison.OrdinalIgnoreCase) ||
             n.Equals("WAS", StringComparison.OrdinalIgnoreCase) ||
             n.Equals("IISADMIN", StringComparison.OrdinalIgnoreCase)))
            return true;
        if (Has(ServerRoleFlags.Docker) &&
            (n.IndexOf("docker", StringComparison.OrdinalIgnoreCase) >= 0 ||
             n.Equals("com.docker.service", StringComparison.OrdinalIgnoreCase) ||
             n.Equals("LxssManager", StringComparison.OrdinalIgnoreCase)))
            return true;
        if (Has(ServerRoleFlags.HyperV) &&
            (n.Equals("vmms", StringComparison.OrdinalIgnoreCase) ||
             n.StartsWith("vmic", StringComparison.OrdinalIgnoreCase) ||
             n.Equals("hvhost", StringComparison.OrdinalIgnoreCase)))
            return true;
        if (Has(ServerRoleFlags.Database) &&
            (n.StartsWith("MSSQL", StringComparison.OrdinalIgnoreCase) ||
             n.StartsWith("SQL", StringComparison.OrdinalIgnoreCase) ||
             n.IndexOf("postgres", StringComparison.OrdinalIgnoreCase) >= 0 ||
             n.IndexOf("mysql", StringComparison.OrdinalIgnoreCase) >= 0 ||
             n.Equals("Redis", StringComparison.OrdinalIgnoreCase)))
            return true;
        if (Has(ServerRoleFlags.FileShare) &&
            (n.Equals("LanmanServer", StringComparison.OrdinalIgnoreCase) ||
             n.Equals("LanmanWorkstation", StringComparison.OrdinalIgnoreCase)))
            return true;
        if (Has(ServerRoleFlags.RdpDesktop) &&
            (n.Equals("TermService", StringComparison.OrdinalIgnoreCase) ||
             n.Equals("SessionEnv", StringComparison.OrdinalIgnoreCase) ||
             n.Equals("UmRdpService", StringComparison.OrdinalIgnoreCase)))
            return true;
        if (Has(ServerRoleFlags.Vpn) &&
            (n.IndexOf("WireGuard", StringComparison.OrdinalIgnoreCase) >= 0 ||
             n.IndexOf("Tailscale", StringComparison.OrdinalIgnoreCase) >= 0 ||
             n.Equals("RemoteAccess", StringComparison.OrdinalIgnoreCase)))
            return true;
        return false;
    }
}

internal static class ServerRoleDetector
{
    public static ServerRoleFlags Detect()
    {
        ServerRoleFlags flags = ServerRoleFlags.None;
        try
        {
            if (ServiceExists("TermService")) flags |= ServerRoleFlags.RdpDesktop;
            if (ServiceExists("LanmanServer")) flags |= ServerRoleFlags.FileShare;
            if (ServiceExists("Spooler") && IsRunning("Spooler")) flags |= ServerRoleFlags.Print;
            if (ServiceExists("W3SVC") || FeatureInstalled("IIS")) flags |= ServerRoleFlags.WebIis;
            if (ServiceExists("vmms") || FeatureInstalled("Microsoft-Hyper-V")) flags |= ServerRoleFlags.HyperV;
            if (ServiceExists("com.docker.service") || ServiceExists("docker") ||
                Directory.Exists(@"C:\Program Files\Docker") ||
                Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Docker")))
                flags |= ServerRoleFlags.Docker;
            if (HasDbService()) flags |= ServerRoleFlags.Database;
            if (ProcessOrPathExists("wireguard") || ProcessOrPathExists("tailscale") || ServiceExists("RemoteAccess"))
                flags |= ServerRoleFlags.Vpn;
        }
        catch { /* ignore */ }
        return flags;
    }

    public static void MergeDetectedIntoProfile(bool overwriteUser = false)
    {
        var data = ServerProfile.Load();
        var detected = Detect();
        if (overwriteUser || !data.ProfileConfigured)
            data.Roles = (int)((ServerRoleFlags)data.Roles | detected);
        else
            data.Roles = (int)((ServerRoleFlags)data.Roles | detected);
        data.ProfileConfigured = true;
        ServerProfile.Save(data);
    }

    private static bool HasDbService()
    {
        try
        {
            foreach (var sc in ServiceController.GetServices())
            {
                using (sc)
                {
                    var n = sc.ServiceName;
                    if (n.StartsWith("MSSQL", StringComparison.OrdinalIgnoreCase) ||
                        n.StartsWith("SQLServer", StringComparison.OrdinalIgnoreCase) ||
                        n.IndexOf("postgresql", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        n.IndexOf("MySQL", StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
            }
        }
        catch { /* ignore */ }
        return false;
    }

    private static bool ServiceExists(string name)
    {
        try
        {
            using var sc = new ServiceController(name);
            _ = sc.Status;
            return true;
        }
        catch { return false; }
    }

    private static bool IsRunning(string name)
    {
        try
        {
            using var sc = new ServiceController(name);
            return sc.Status == ServiceControllerStatus.Running;
        }
        catch { return false; }
    }

    private static bool FeatureInstalled(string keyword)
    {
        try
        {
            // 轻量：看常见目录 / 注册表，避免每次 DISM
            if (keyword.IndexOf("Hyper-V", StringComparison.OrdinalIgnoreCase) >= 0)
                return Directory.Exists(Path.Combine(Environment.SystemDirectory, "vmms.exe")) ||
                       File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "vmms.exe"));
            if (keyword.IndexOf("IIS", StringComparison.OrdinalIgnoreCase) >= 0)
                return Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "inetsrv"));
        }
        catch { /* ignore */ }
        return false;
    }

    private static bool ProcessOrPathExists(string keyword)
    {
        try
        {
            foreach (var p in System.Diagnostics.Process.GetProcesses())
            {
                try
                {
                    if (p.ProcessName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;
                }
                catch { /* ignore */ }
                finally { p.Dispose(); }
            }
        }
        catch { /* ignore */ }
        return false;
    }
}
