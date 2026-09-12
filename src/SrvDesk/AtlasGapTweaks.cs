using Microsoft.Win32;

namespace SrvDesk;

/// <summary>对齐 Atlas OS Playbook 中适合本工具的可逆开关（共享安全 / 开关机 / QoL / 隐私补缺）。</summary>
internal static class AtlasGapTweaks
{
    private const string Desktop = @"Control Panel\Desktop";
    private const string ExplorerAdv = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
    private const string Serialize = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Serialize";
    private const string OpStatus = @"Software\Microsoft\Windows\CurrentVersion\Explorer\OperationStatusManager";
    private const string SettingSyncPol = @"SOFTWARE\Policies\Microsoft\Windows\SettingSync";
    private const string SettingSyncCu = @"Software\Microsoft\Windows\CurrentVersion\SettingSync";
    private const string ProfileEngage = @"Software\Microsoft\Windows\CurrentVersion\UserProfileEngagement";
    private const string LanmanServer = @"SYSTEM\CurrentControlSet\Services\LanManServer\Parameters";
    private const string LanmanWorkstation = @"SYSTEM\CurrentControlSet\Services\LanmanWorkstation\Parameters";
    private const string Lsa = @"SYSTEM\CurrentControlSet\Control\Lsa";
    private const string NetworkWizardOff = @"SYSTEM\CurrentControlSet\Control\Network\NewNetworkWindowOff";
    private const string Control = @"SYSTEM\CurrentControlSet\Control";
    private const string Ubpm = @"SYSTEM\CurrentControlSet\Control\Ubpm";
    private const string SrvDeskTweaks = @"Software\SrvDesk\Tweaks";

    /// <summary>Atlas debloat：常见遥测/体验计划任务（不存在则忽略）。</summary>
    internal static readonly string[] TelemetryScheduledTasks =
    [
        @"\Microsoft\Windows\Application Experience\PcaPatchDbTask",
        @"\Microsoft\Windows\AppxDeploymentClient\UCPD velocity",
        @"\Microsoft\Windows\DiskDiagnostic\Microsoft-Windows-DiskDiagnosticDataCollector",
        @"\Microsoft\Windows\Customer Experience Improvement Program\Consolidator",
        @"\Microsoft\Windows\Customer Experience Improvement Program\UsbCeip",
        @"\Microsoft\Windows\Flighting\FeatureConfig\UsageDataReporting",
    ];

    public static void Apply(Optimizer.State s, Optimizer.State? baseline = null)
    {
        bool D(Func<Optimizer.State, bool> f) => baseline is null || f(baseline) != f(s);
        bool Take(string field, Func<Optimizer.State, bool> f)
        {
            if (!D(f)) return false;
            ApplyLog.DebugField(field, baseline is null ? null : f(baseline), f(s));
            return true;
        }

        if (Take("RestrictNullSessionShares", x => x.RestrictNullSessionShares))
            SetDword(Hive.HkLm, LanmanServer, "RestrictNullSessAccess", s.RestrictNullSessionShares ? 1 : 0);
        if (Take("RestrictAnonymousEnum", x => x.RestrictAnonymousEnum))
            SetDword(Hive.HkLm, Lsa, "RestrictAnonymous", s.RestrictAnonymousEnum ? 1 : 0);
        if (Take("DisableSmbBandwidthThrottling", x => x.DisableSmbBandwidthThrottling))
            SetDword(Hive.HkLm, LanmanWorkstation, "DisableBandwidthThrottling",
                s.DisableSmbBandwidthThrottling ? 1 : 0);

        if (Take("FasterShutdown", x => x.FasterShutdown))
            SetFasterShutdown(s.FasterShutdown);
        if (Take("DisableStartupAppDelay", x => x.DisableStartupAppDelay))
            SetDword(Hive.HkCu, Serialize, "StartupDelayInMSec", s.DisableStartupAppDelay ? 0 : 200);

        if (Take("InstantMenuShow", x => x.InstantMenuShow))
            SetSz(Hive.HkCu, Desktop, "MenuShowDelay", s.InstantMenuShow ? "0" : "400");
        if (Take("DisableAeroShake", x => x.DisableAeroShake))
            SetDword(Hive.HkCu, ExplorerAdv, "DisallowShaking", s.DisableAeroShake ? 1 : 0);
        if (Take("DisableNetworkLocationWizard", x => x.DisableNetworkLocationWizard))
            SetNetworkLocationWizardOff(s.DisableNetworkLocationWizard);

        if (Take("DisableSettingSync", x => x.DisableSettingSync))
            SetSettingSyncOff(s.DisableSettingSync);
        if (Take("DisableFinishSetupSuggestions", x => x.DisableFinishSetupSuggestions))
            SetDword(Hive.HkCu, ProfileEngage, "ScoobeSystemSettingEnabled",
                s.DisableFinishSetupSuggestions ? 0 : 1);

        if (Take("ExplorerTransferDetails", x => x.ExplorerTransferDetails))
            SetDword(Hive.HkCu, OpStatus, "EnthusiastMode", s.ExplorerTransferDetails ? 1 : 0);

        if (Take("DisableTelemetryScheduledTasks", x => x.DisableTelemetryScheduledTasks))
            SetTelemetryScheduledTasks(!s.DisableTelemetryScheduledTasks);
    }

    public static void ReadInto(Optimizer.State s)
    {
        s.RestrictNullSessionShares = DwordEquals(Hive.HkLm, LanmanServer, "RestrictNullSessAccess", 1);
        s.RestrictAnonymousEnum = DwordEquals(Hive.HkLm, Lsa, "RestrictAnonymous", 1);
        s.DisableSmbBandwidthThrottling = DwordEquals(Hive.HkLm, LanmanWorkstation, "DisableBandwidthThrottling", 1);
        s.FasterShutdown = IsFasterShutdownOn();
        s.DisableStartupAppDelay = DwordEquals(Hive.HkCu, Serialize, "StartupDelayInMSec", 0);
        s.InstantMenuShow = SzEquals(Hive.HkCu, Desktop, "MenuShowDelay", "0");
        s.DisableAeroShake = DwordEquals(Hive.HkCu, ExplorerAdv, "DisallowShaking", 1);
        s.DisableNetworkLocationWizard = KeyExists(Hive.HkLm, NetworkWizardOff);
        s.DisableSettingSync = DwordEquals(Hive.HkLm, SettingSyncPol, "DisableSettingSync", 2);
        s.DisableFinishSetupSuggestions = DwordEquals(Hive.HkCu, ProfileEngage, "ScoobeSystemSettingEnabled", 0);
        s.ExplorerTransferDetails = DwordEquals(Hive.HkCu, OpStatus, "EnthusiastMode", 1);
        s.DisableTelemetryScheduledTasks = IsTelemetryScheduledTasksDisabled();
    }

    public static bool AnyChanged(Optimizer.State? b, Optimizer.State s)
    {
        if (b is null) return true;
        return b.RestrictNullSessionShares != s.RestrictNullSessionShares
            || b.RestrictAnonymousEnum != s.RestrictAnonymousEnum
            || b.DisableSmbBandwidthThrottling != s.DisableSmbBandwidthThrottling
            || b.FasterShutdown != s.FasterShutdown
            || b.DisableStartupAppDelay != s.DisableStartupAppDelay
            || b.InstantMenuShow != s.InstantMenuShow
            || b.DisableAeroShake != s.DisableAeroShake
            || b.DisableNetworkLocationWizard != s.DisableNetworkLocationWizard
            || b.DisableSettingSync != s.DisableSettingSync
            || b.DisableFinishSetupSuggestions != s.DisableFinishSetupSuggestions
            || b.ExplorerTransferDetails != s.ExplorerTransferDetails
            || b.DisableTelemetryScheduledTasks != s.DisableTelemetryScheduledTasks;
    }

    private static void SetFasterShutdown(bool on)
    {
        var app = on ? "2000" : "5000";
        var svc = on ? "2000" : "5000";
        SetSz(Hive.HkCu, Desktop, "HungAppTimeout", app);
        SetSz(Hive.HkCu, Desktop, "WaitToKillAppTimeOut", app);
        SetSz(Hive.HkLm, Control, "WaitToKillServiceTimeout", svc);
    }

    private static bool IsFasterShutdownOn() =>
        SzEquals(Hive.HkCu, Desktop, "HungAppTimeout", "2000")
        && SzEquals(Hive.HkCu, Desktop, "WaitToKillAppTimeOut", "2000")
        && SzEquals(Hive.HkLm, Control, "WaitToKillServiceTimeout", "2000");

    private static void SetNetworkLocationWizardOff(bool off)
    {
        if (off)
            EnsureKey(Hive.HkLm, NetworkWizardOff);
        else
            DeleteKey(Hive.HkLm, NetworkWizardOff);
    }

    private static void SetSettingSyncOff(bool off)
    {
        if (off)
        {
            SetDword(Hive.HkLm, SettingSyncPol, "DisableSettingSync", 2);
            SetDword(Hive.HkLm, SettingSyncPol, "DisableSettingSyncUserOverride", 1);
            SetDword(Hive.HkLm, SettingSyncPol, "DisableSyncOnPaidNetwork", 1);
            SetDword(Hive.HkLm, SettingSyncPol, "DisableWindowsSettingSync", 2);
            SetDword(Hive.HkCu, SettingSyncCu, "SyncPolicy", 5);
            foreach (var group in new[] { "Personalization", "BrowserSettings", "Credentials", "Accessibility", "Windows" })
                SetDword(Hive.HkCu, SettingSyncCu + @"\Groups\" + group, "Enabled", 0);
        }
        else
        {
            DeleteValue(Hive.HkLm, SettingSyncPol, "DisableSettingSync");
            DeleteValue(Hive.HkLm, SettingSyncPol, "DisableSettingSyncUserOverride");
            DeleteValue(Hive.HkLm, SettingSyncPol, "DisableSyncOnPaidNetwork");
            DeleteValue(Hive.HkLm, SettingSyncPol, "DisableWindowsSettingSync");
            DeleteValue(Hive.HkCu, SettingSyncCu, "SyncPolicy");
            foreach (var group in new[] { "Personalization", "BrowserSettings", "Credentials", "Accessibility", "Windows" })
                SetDword(Hive.HkCu, SettingSyncCu + @"\Groups\" + group, "Enabled", 1);
        }
    }

    private static void SetTelemetryScheduledTasks(bool enable)
    {
        foreach (var path in TelemetryScheduledTasks)
        {
            try { ScheduledTaskHelper.SetEnabled(path, enable); }
            catch (Exception ex) { ApplyLog.Debug("计划任务 " + path + "：" + ex.Message); }
        }

        if (!enable)
        {
            try { DeleteValue(Hive.HkLm, Ubpm, "CriticalMaintenance_UsageDataReporting"); }
            catch { /* ignore */ }
            SetDword(Hive.HkCu, SrvDeskTweaks, "DisableTelemetryScheduledTasks", 1);
        }
        else
        {
            DeleteValue(Hive.HkCu, SrvDeskTweaks, "DisableTelemetryScheduledTasks");
        }
    }

    private static bool IsTelemetryScheduledTasksDisabled()
    {
        if (DwordEquals(Hive.HkCu, SrvDeskTweaks, "DisableTelemetryScheduledTasks", 1))
            return true;
        // 探针：CEIP Consolidator 已禁用则视为开启本项
        try
        {
            return !ScheduledTaskHelper.IsEnabled(
                @"\Microsoft\Windows\Customer Experience Improvement Program\Consolidator");
        }
        catch
        {
            return false;
        }
    }

    private static void SetDword(Hive hive, string subKey, string name, int value)
    {
        using var key = OpenOrCreate(hive, subKey);
        key?.SetValue(name, value, RegistryValueKind.DWord);
    }

    private static void SetSz(Hive hive, string subKey, string name, string value)
    {
        using var key = OpenOrCreate(hive, subKey);
        key?.SetValue(name, value, RegistryValueKind.String);
    }

    private static void DeleteValue(Hive hive, string subKey, string name)
    {
        try
        {
            using var key = OpenKey(hive, subKey, writable: true);
            key?.DeleteValue(name, throwOnMissingValue: false);
        }
        catch { /* ignore */ }
    }

    private static void EnsureKey(Hive hive, string subKey)
    {
        using var _ = OpenOrCreate(hive, subKey);
    }

    private static void DeleteKey(Hive hive, string subKey)
    {
        try
        {
            var parent = Path.GetDirectoryName(subKey.Replace('/', '\\')) ?? "";
            var leaf = Path.GetFileName(subKey.Replace('/', '\\'));
            if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(leaf)) return;
            using var key = OpenKey(hive, parent, writable: true);
            key?.DeleteSubKeyTree(leaf, throwOnMissingSubKey: false);
        }
        catch { /* ignore */ }
    }

    private static bool DwordEquals(Hive hive, string subKey, string name, int expected)
    {
        using var key = OpenKey(hive, subKey);
        if (key?.GetValue(name) is int i) return i == expected;
        if (key?.GetValue(name) is long l) return unchecked((int)l) == expected;
        return false;
    }

    private static bool SzEquals(Hive hive, string subKey, string name, string expected)
    {
        using var key = OpenKey(hive, subKey);
        var v = key?.GetValue(name) as string;
        return string.Equals(v, expected, StringComparison.OrdinalIgnoreCase);
    }

    private static bool KeyExists(Hive hive, string subKey)
    {
        using var key = OpenKey(hive, subKey);
        return key is not null;
    }

    private static RegistryKey? OpenKey(Hive hive, string subKey, bool writable = false)
    {
        var root = hive == Hive.HkLm ? RegistryHive.LocalMachine : RegistryHive.CurrentUser;
        using var baseKey = RegistryKey.OpenBaseKey(root, RegistryView.Registry64);
        return baseKey.OpenSubKey(subKey, writable);
    }

    private static RegistryKey? OpenOrCreate(Hive hive, string subKey)
    {
        var root = hive == Hive.HkLm ? RegistryHive.LocalMachine : RegistryHive.CurrentUser;
        using var baseKey = RegistryKey.OpenBaseKey(root, RegistryView.Registry64);
        return baseKey.CreateSubKey(subKey, true);
    }

    private enum Hive { HkLm, HkCu }
}
