using Microsoft.Win32;

namespace SrvDesk;

/// <summary>Winhance / 常见桌面优化缺口项（驱动协同安装、Toast、开发者模式、PowerShell 策略）。</summary>
internal static class WinhanceGapTweaks
{
    private const string DeviceInstaller = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Device Installer";
    private const string PushNotifications = @"Software\Microsoft\Windows\CurrentVersion\PushNotifications";
    private const string AppModelUnlock = @"SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock";
    private const string PsExecutionPolicy = @"SOFTWARE\Microsoft\PowerShell\1\ShellIds\Microsoft.PowerShell";
    private const string Ps7ExecutionPolicy = @"SOFTWARE\Microsoft\PowerShell\7\ShellIds\Microsoft.PowerShell";

    public static void Apply(Optimizer.State s, Optimizer.State? baseline = null)
    {
        bool D(Func<Optimizer.State, bool> f) => baseline is null || f(baseline) != f(s);
        bool Take(string field, Func<Optimizer.State, bool> f)
        {
            if (!D(f)) return false;
            ApplyLog.DebugField(field, baseline is null ? null : f(baseline), f(s));
            return true;
        }

        if (Take("DisableDriverCoInstallers", x => x.DisableDriverCoInstallers))
            SetDword(Hive.HkLm, DeviceInstaller, "DisableCoInstallers", s.DisableDriverCoInstallers ? 1 : 0);

        if (Take("DisableToastNotifications", x => x.DisableToastNotifications))
            SetDword(Hive.HkCu, PushNotifications, "ToastEnabled", s.DisableToastNotifications ? 0 : 1);

        if (Take("EnableDeveloperMode", x => x.EnableDeveloperMode))
            SetDeveloperMode(s.EnableDeveloperMode);

        if (Take("PowerShellRemoteSigned", x => x.PowerShellRemoteSigned))
            SetPowerShellRemoteSigned(s.PowerShellRemoteSigned);
    }

    public static void ReadInto(Optimizer.State s)
    {
        s.DisableDriverCoInstallers = DwordEquals(Hive.HkLm, DeviceInstaller, "DisableCoInstallers", 1);
        s.DisableToastNotifications = DwordEquals(Hive.HkCu, PushNotifications, "ToastEnabled", 0);
        s.EnableDeveloperMode = DwordEquals(Hive.HkLm, AppModelUnlock, "AllowDevelopmentWithoutDevLicense", 1)
            && DwordEquals(Hive.HkLm, AppModelUnlock, "AllowAllTrustedApps", 1);
        s.PowerShellRemoteSigned = IsPowerShellRemoteSigned();
    }

    public static bool AnyChanged(Optimizer.State? b, Optimizer.State s)
    {
        if (b is null) return true;
        return b.DisableDriverCoInstallers != s.DisableDriverCoInstallers
            || b.DisableToastNotifications != s.DisableToastNotifications
            || b.EnableDeveloperMode != s.EnableDeveloperMode
            || b.PowerShellRemoteSigned != s.PowerShellRemoteSigned;
    }

    private static void SetDeveloperMode(bool on)
    {
        SetDword(Hive.HkLm, AppModelUnlock, "AllowDevelopmentWithoutDevLicense", on ? 1 : 0);
        SetDword(Hive.HkLm, AppModelUnlock, "AllowAllTrustedApps", on ? 1 : 0);
    }

    private static void SetPowerShellRemoteSigned(bool on)
    {
        var policy = on ? "RemoteSigned" : "Restricted";
        SetSz(Hive.HkLm, PsExecutionPolicy, "ExecutionPolicy", policy);
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var existing = baseKey.OpenSubKey(Ps7ExecutionPolicy);
            if (existing is not null)
                SetSz(Hive.HkLm, Ps7ExecutionPolicy, "ExecutionPolicy", policy);
            else
            {
                using var created = baseKey.CreateSubKey(Ps7ExecutionPolicy, true);
                if (created is not null)
                    created.SetValue("ExecutionPolicy", policy, RegistryValueKind.String);
            }
        }
        catch (Exception ex)
        {
            ApplyLog.Debug("PowerShell 7 ExecutionPolicy：" + ex.Message);
        }
    }

    private static bool IsPowerShellRemoteSigned()
    {
        using var key = OpenKey(Hive.HkLm, PsExecutionPolicy);
        var v = key?.GetValue("ExecutionPolicy") as string;
        return string.Equals(v, "RemoteSigned", StringComparison.OrdinalIgnoreCase);
    }

    private static void SetDword(Hive hive, string subKey, string name, int value)
    {
        using var key = OpenOrCreate(hive, subKey);
        var old = key?.GetValue(name);
        key?.SetValue(name, value, RegistryValueKind.DWord);
        ApplyLog.RegistryDword(hive == Hive.HkLm ? "HKLM" : "HKCU", subKey, name, old, value);
    }

    private static void SetSz(Hive hive, string subKey, string name, string value)
    {
        using var key = OpenOrCreate(hive, subKey);
        var old = key?.GetValue(name);
        key?.SetValue(name, value, RegistryValueKind.String);
        ApplyLog.RegistryString(hive == Hive.HkLm ? "HKLM" : "HKCU", subKey, name, old, value);
    }

    private static bool DwordEquals(Hive hive, string subKey, string name, int expected)
    {
        using var key = OpenKey(hive, subKey);
        if (key?.GetValue(name) is int i) return i == expected;
        if (key?.GetValue(name) is long l) return unchecked((int)l) == expected;
        return false;
    }

    private static RegistryKey? OpenKey(Hive hive, string subKey)
    {
        var root = hive == Hive.HkLm ? RegistryHive.LocalMachine : RegistryHive.CurrentUser;
        using var baseKey = RegistryKey.OpenBaseKey(root, RegistryView.Registry64);
        return baseKey.OpenSubKey(subKey);
    }

    private static RegistryKey? OpenOrCreate(Hive hive, string subKey)
    {
        var root = hive == Hive.HkLm ? RegistryHive.LocalMachine : RegistryHive.CurrentUser;
        using var baseKey = RegistryKey.OpenBaseKey(root, RegistryView.Registry64);
        return baseKey.CreateSubKey(subKey, true);
    }

    private enum Hive { HkLm, HkCu }
}
