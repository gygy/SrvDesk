using Microsoft.Win32;

namespace WinOpt;

internal sealed class SystemFacts
{
    public bool IsServer { get; }
    public bool HasDesktopExperience { get; }
    public string ProductName { get; }
    public string DisplayVersion { get; }
    public string Build { get; }
    public string Summary { get; }

    public SystemFacts(
        bool isServer,
        bool hasDesktopExperience,
        string productName,
        string displayVersion,
        string build,
        string summary)
    {
        IsServer = isServer;
        HasDesktopExperience = hasDesktopExperience;
        ProductName = productName;
        DisplayVersion = displayVersion;
        Build = build;
        Summary = summary;
    }
}

internal static class SystemInfoHelper
{
    public static SystemFacts Detect()
    {
        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var key = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
        var product = key?.GetValue("ProductName") as string ?? "";
        var display = key?.GetValue("DisplayVersion") as string ?? "";
        var build = key?.GetValue("CurrentBuildNumber") as string ?? "";
        var ubr = key?.GetValue("UBR");
        if (ubr is int u && u > 0) build += "." + u;

        var isServer = Optimizer.IsWindowsServer();
        var hasDesktop = DetectDesktopExperience();

        var summary = isServer
            ? hasDesktop
                ? $"Windows Server（桌面体验）· {display} · Build {build}"
                : $"Windows Server Core · {display} · Build {build}"
            : $"{product} · {display} · Build {build}";

        return new SystemFacts(isServer, hasDesktop, product, display, build, summary);
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
}
