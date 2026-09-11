using Microsoft.Win32;

namespace SrvDesk;

/// <summary>识别到的 Windows 产品族（用于推荐矩阵）。</summary>
internal enum WindowsOsKind
{
    Unknown = 0,
    Win10 = 1,
    Win11 = 2,
    Server2016 = 3,
    Server2019 = 4,
    Server2022 = 5,
    Server2025 = 6,
    ServerOther = 7,
}

internal sealed class SystemFacts
{
    public bool IsServer { get; }
    public bool HasDesktopExperience { get; }
    public bool IsServerCore => IsServer && !HasDesktopExperience;
    public bool IsVirtualMachine { get; }
    public WindowsOsKind Kind { get; }
    public int BuildNumber { get; }
    public string ProductName { get; }
    public string DisplayVersion { get; }
    public string Build { get; }
    public string Summary { get; }

    public SystemFacts(
        bool isServer,
        bool hasDesktopExperience,
        bool isVirtualMachine,
        WindowsOsKind kind,
        int buildNumber,
        string productName,
        string displayVersion,
        string build,
        string summary)
    {
        IsServer = isServer;
        HasDesktopExperience = hasDesktopExperience;
        IsVirtualMachine = isVirtualMachine;
        Kind = kind;
        BuildNumber = buildNumber;
        ProductName = productName;
        DisplayVersion = displayVersion;
        Build = build;
        Summary = summary;
    }
}

internal static class SystemInfoHelper
{
    private static SystemFacts? _cached;
    private static int _cachedAt;

    public static SystemFacts Detect()
    {
        var now = Environment.TickCount;
        if (_cached is not null && unchecked(now - _cachedAt) < 60_000)
            return _cached;
        _cached = DetectCore();
        _cachedAt = now;
        return _cached;
    }

    public static void InvalidateCache() => _cached = null;

    private static SystemFacts DetectCore()
    {
        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
        var product = key?.GetValue("ProductName") as string ?? "";
        var display = key?.GetValue("DisplayVersion") as string ?? "";
        var buildStr = key?.GetValue("CurrentBuildNumber") as string ?? "";
        var ubr = key?.GetValue("UBR");
        var build = buildStr;
        if (ubr is int u && u > 0) build += "." + u;
        _ = int.TryParse(buildStr, out var buildNum);

        var isServer = Optimizer.IsWindowsServer();
        var hasDesktop = DetectDesktopExperience();
        var isVm = DetectVirtualMachine();
        var kind = ResolveKind(isServer, product, display, buildNum);

        var summary = isServer
            ? hasDesktop
                ? $"Windows Server（桌面体验）· {KindLabel(kind)} · {display} · Build {build}"
                : $"Windows Server Core · {KindLabel(kind)} · {display} · Build {build}"
            : $"{product} · {display} · Build {build}";
        if (isVm)
            summary += AppLang.L(" · 虚拟机", " · VM");

        return new SystemFacts(isServer, hasDesktop, isVm, kind, buildNum, product, display, build, summary);
    }

    public static string KindLabel(WindowsOsKind kind) => kind switch
    {
        WindowsOsKind.Win10 => "Windows 10",
        WindowsOsKind.Win11 => "Windows 11",
        WindowsOsKind.Server2016 => "Server 2016",
        WindowsOsKind.Server2019 => "Server 2019",
        WindowsOsKind.Server2022 => "Server 2022",
        WindowsOsKind.Server2025 => "Server 2025",
        WindowsOsKind.ServerOther => "Windows Server",
        _ => "Windows",
    };

    private static WindowsOsKind ResolveKind(bool isServer, string product, string display, int build)
    {
        if (isServer)
        {
            var p = product + " " + display;
            if (p.IndexOf("2025", StringComparison.OrdinalIgnoreCase) >= 0 || build >= 26100)
                return WindowsOsKind.Server2025;
            if (p.IndexOf("2022", StringComparison.OrdinalIgnoreCase) >= 0 || build is >= 20348 and < 26000)
                return WindowsOsKind.Server2022;
            if (p.IndexOf("2019", StringComparison.OrdinalIgnoreCase) >= 0 || build is >= 17763 and < 20348)
                return WindowsOsKind.Server2019;
            if (p.IndexOf("2016", StringComparison.OrdinalIgnoreCase) >= 0 || build is >= 14393 and < 17763)
                return WindowsOsKind.Server2016;
            return WindowsOsKind.ServerOther;
        }

        return build >= 22000 ? WindowsOsKind.Win11 : WindowsOsKind.Win10;
    }

    private static bool DetectDesktopExperience()
    {
        try
        {
            var themesStart = Registry.GetValue(
                @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\Themes", "Start", null);
            if (themesStart is int s && s == 4) return false;

            var shell = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            return File.Exists(Path.Combine(shell, "explorer.exe"));
        }
        catch
        {
            return true;
        }
    }

    private static bool DetectVirtualMachine()
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var key = baseKey.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\SystemInformation");
            var model = (key?.GetValue("SystemProductName") as string ?? "")
                        + " " + (key?.GetValue("SystemManufacturer") as string ?? "");
            if (ContainsAny(model, "Virtual", "VMware", "VirtualBox", "Hyper-V", "KVM", "QEMU", "Xen", "Parallels", "HVM"))
                return true;

            using var bios = baseKey.OpenSubKey(@"HARDWARE\DESCRIPTION\System\BIOS");
            var biosStr = (bios?.GetValue("SystemProductName") as string ?? "")
                          + " " + (bios?.GetValue("SystemManufacturer") as string ?? "");
            return ContainsAny(biosStr, "Virtual", "VMware", "VirtualBox", "Hyper-V", "KVM", "QEMU", "Xen");
        }
        catch
        {
            return false;
        }
    }

    private static bool ContainsAny(string text, params string[] keys)
    {
        foreach (var k in keys)
        {
            if (text.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }
        return false;
    }
}
