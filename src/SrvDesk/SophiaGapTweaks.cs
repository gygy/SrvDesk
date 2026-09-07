using System.Diagnostics;
using System.Security.Principal;
using Microsoft.Win32;

namespace SrvDesk;

/// <summary>对齐 Sophia / WinUtil / Win11Debloat 的 11 项开关（还原点走应用流程，不在此）。</summary>
internal static class SophiaGapTweaks
{
    private const string DataCollection = @"SOFTWARE\Policies\Microsoft\Windows\DataCollection";
    private const string DataCollectionUx = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\DataCollection";
    private const string DiagTrackCu = @"Software\Microsoft\Windows\CurrentVersion\Diagnostics\DiagTrack";
    private const string ContentDelivery = @"Software\Microsoft\Windows\CurrentVersion\ContentDeliveryManager";
    private const string ExplorerAdv = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
    private const string Personalize = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string HomeClsid = @"Software\Classes\CLSID\{f874310e-b6b7-47dc-bc84-b9e6b38f5903}";
    private const string GalleryClsid = @"Software\Classes\CLSID\{e88865ea-0e1c-4e20-9aa6-edcd0212c87c}";
    private const string WuUx = @"SOFTWARE\Microsoft\WindowsUpdate\UX\Settings";
    private const string WindowsAiLm = @"SOFTWARE\Policies\Microsoft\Windows\WindowsAI";
    private const string WindowsAiCu = @"Software\Policies\Microsoft\Windows\WindowsAI";
    private const string SettingsPolCu = @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer";
    private const string CloudContent = @"SOFTWARE\Policies\Microsoft\Windows\CloudContent";
    private const string PaintPol = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Paint";
    private const string NotepadPol = @"SOFTWARE\Policies\WindowsNotepad";

    public static void Apply(Optimizer.State s, Optimizer.State? baseline = null)
    {
        bool D(Func<Optimizer.State, bool> f) => baseline is null || f(baseline) != f(s);
        bool Take(string field, Func<Optimizer.State, bool> f)
        {
            if (!D(f)) return false;
            ApplyLog.DebugField(field, baseline is null ? null : f(baseline), f(s));
            return true;
        }

        if (Take("DiagnosticDataMinimal", x => x.DiagnosticDataMinimal))
            SetDiagnosticMinimal(s.DiagnosticDataMinimal, s.DisableTelemetry);
        if (Take("DisableSigninReopen", x => x.DisableSigninReopen))
            SetSigninReopen(!s.DisableSigninReopen);
        if (Take("DisableSilentAppInstall", x => x.DisableSilentAppInstall))
            SetDword(Hive.HkCu, ContentDelivery, "SilentInstalledAppsEnabled", s.DisableSilentAppInstall ? 0 : 1);
        if (Take("HideExplorerHomeGallery", x => x.HideExplorerHomeGallery))
            SetHomeGalleryHidden(s.HideExplorerHomeGallery);
        if (Take("DisableSnapAssist", x => x.DisableSnapAssist))
            SetSnapAssist(!s.DisableSnapAssist);
        if (Take("EnableDarkMode", x => x.EnableDarkMode))
            SetDarkMode(s.EnableDarkMode);
        if (Take("DisableBitLockerAutoEncrypt", x => x.DisableBitLockerAutoEncrypt))
            SetDword(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\BitLocker", "PreventDeviceEncryption",
                s.DisableBitLockerAutoEncrypt ? 1 : 0);
        if (Take("PreventDeviceCompanionApps", x => x.PreventDeviceCompanionApps))
            SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\Device Metadata",
                "PreventDeviceMetadataFromNetwork", s.PreventDeviceCompanionApps ? 1 : 0);
        if (Take("DisableUpdateAsap", x => x.DisableUpdateAsap))
            SetUpdateAsap(!s.DisableUpdateAsap);
        if (Take("HideSettingsHomeAds", x => x.HideSettingsHomeAds))
            SetSettingsHomeAds(s.HideSettingsHomeAds);
        if (Take("DisableWin11ExtraAi", x => x.DisableWin11ExtraAi))
            SetWin11ExtraAi(s.DisableWin11ExtraAi);

        if (NeedsExplorerRestart(baseline, s))
            DesktopQuickActions.RestartExplorer();
    }

    public static void ReadInto(Optimizer.State s)
    {
        s.DiagnosticDataMinimal = DwordEquals(Hive.HkLm, DataCollectionUx, "MaxTelemetryAllowed", 1);
        s.DisableSigninReopen = DwordEquals(Hive.HkLm,
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "DisableAutomaticRestartSignOn", 1)
            || IsUserArsoOptOut();
        s.DisableSilentAppInstall = DwordEquals(Hive.HkCu, ContentDelivery, "SilentInstalledAppsEnabled", 0);
        s.HideExplorerHomeGallery =
            DwordEquals(Hive.HkCu, HomeClsid, "System.IsPinnedToNameSpaceTree", 0)
            && DwordEquals(Hive.HkCu, GalleryClsid, "System.IsPinnedToNameSpaceTree", 0);
        s.DisableSnapAssist = DwordEquals(Hive.HkCu, ExplorerAdv, "SnapAssist", 0);
        s.EnableDarkMode = DwordEquals(Hive.HkCu, Personalize, "AppsUseLightTheme", 0)
            && DwordEquals(Hive.HkCu, Personalize, "SystemUsesLightTheme", 0);
        s.DisableBitLockerAutoEncrypt = DwordEquals(Hive.HkLm,
            @"SYSTEM\CurrentControlSet\Control\BitLocker", "PreventDeviceEncryption", 1);
        s.PreventDeviceCompanionApps = DwordEquals(Hive.HkLm,
            @"SOFTWARE\Policies\Microsoft\Windows\Device Metadata", "PreventDeviceMetadataFromNetwork", 1);
        s.DisableUpdateAsap = IsUpdateAsapOff();
        s.HideSettingsHomeAds = SettingsHomeHidden()
            && DwordEquals(Hive.HkLm, CloudContent, "DisableConsumerAccountStateContent", 1);
        s.DisableWin11ExtraAi = DwordEquals(Hive.HkLm, WindowsAiLm, "DisableAIDataAnalysis", 1)
            && DwordEquals(Hive.HkLm, WindowsAiLm, "DisableClickToDo", 1);
    }

    public static bool AnyChanged(Optimizer.State? b, Optimizer.State s)
    {
        if (b is null) return true;
        return b.DiagnosticDataMinimal != s.DiagnosticDataMinimal
            || b.DisableSigninReopen != s.DisableSigninReopen
            || b.DisableSilentAppInstall != s.DisableSilentAppInstall
            || b.HideExplorerHomeGallery != s.HideExplorerHomeGallery
            || b.DisableSnapAssist != s.DisableSnapAssist
            || b.EnableDarkMode != s.EnableDarkMode
            || b.DisableBitLockerAutoEncrypt != s.DisableBitLockerAutoEncrypt
            || b.PreventDeviceCompanionApps != s.PreventDeviceCompanionApps
            || b.DisableUpdateAsap != s.DisableUpdateAsap
            || b.HideSettingsHomeAds != s.HideSettingsHomeAds
            || b.DisableWin11ExtraAi != s.DisableWin11ExtraAi;
    }

    public static bool NeedsExplorerRestart(Optimizer.State? b, Optimizer.State s)
    {
        if (b is null) return true;
        return b.HideExplorerHomeGallery != s.HideExplorerHomeGallery
            || b.DisableSnapAssist != s.DisableSnapAssist;
    }

    private static void SetDiagnosticMinimal(bool minimal, bool telemetryFullyOff)
    {
        if (minimal)
        {
            var allow = telemetryFullyOff || Optimizer.IsWindowsServer() ? 0 : 1;
            SetDword(Hive.HkLm, DataCollection, "AllowTelemetry", allow);
            SetDword(Hive.HkLm, DataCollectionUx, "MaxTelemetryAllowed", 1);
            SetDword(Hive.HkCu, DiagTrackCu, "ShowedToastAtLevel", 1);
        }
        else
        {
            if (!telemetryFullyOff)
                DeleteValue(Hive.HkLm, DataCollection, "AllowTelemetry");
            SetDword(Hive.HkLm, DataCollectionUx, "MaxTelemetryAllowed", 3);
            SetDword(Hive.HkCu, DiagTrackCu, "ShowedToastAtLevel", 3);
        }
    }

    private static void SetSigninReopen(bool allow)
    {
        SetDword(Hive.HkLm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System",
            "DisableAutomaticRestartSignOn", allow ? 0 : 1);
        var sid = WindowsIdentity.GetCurrent().User?.Value;
        if (string.IsNullOrEmpty(sid)) return;
        var key = $@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon\UserARSO\{sid}";
        if (allow)
            DeleteValue(Hive.HkLm, key, "OptOut");
        else
            SetDword(Hive.HkLm, key, "OptOut", 1);
    }

    private static bool IsUserArsoOptOut()
    {
        var sid = WindowsIdentity.GetCurrent().User?.Value;
        if (string.IsNullOrEmpty(sid)) return false;
        return DwordEquals(Hive.HkLm,
            $@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon\UserARSO\{sid}", "OptOut", 1);
    }

    private static void SetHomeGalleryHidden(bool hide)
    {
        SetDword(Hive.HkCu, HomeClsid, "System.IsPinnedToNameSpaceTree", hide ? 0 : 1);
        SetDword(Hive.HkCu, GalleryClsid, "System.IsPinnedToNameSpaceTree", hide ? 0 : 1);
    }

    private static void SetSnapAssist(bool enable)
    {
        SetDword(Hive.HkCu, ExplorerAdv, "SnapAssist", enable ? 1 : 0);
        SetDword(Hive.HkCu, ExplorerAdv, "EnableSnapAssistFlyout", enable ? 1 : 0);
    }

    private static void SetDarkMode(bool dark)
    {
        SetDword(Hive.HkCu, Personalize, "AppsUseLightTheme", dark ? 0 : 1);
        SetDword(Hive.HkCu, Personalize, "SystemUsesLightTheme", dark ? 0 : 1);
        DesktopQuickActions.NotifyShellChanged();
    }

    private static bool IsUpdateAsapOff()
    {
        using var baseKey = OpenBase(Hive.HkLm);
        using var k = baseKey.OpenSubKey(WuUx);
        if (k?.GetValue("IsContinuousInnovationOptedIn") is int ci)
            return ci == 0;
        return k?.GetValue("IsExpedited") is int exp && exp == 0;
    }

    private static void SetUpdateAsap(bool enable)
    {
        SetDword(Hive.HkLm, WuUx, "IsContinuousInnovationOptedIn", enable ? 1 : 0);
        SetDword(Hive.HkLm, WuUx, "IsExpedited", enable ? 1 : 0);
    }

    private static void SetSettingsHomeAds(bool hide)
    {
        SetString(Hive.HkCu, SettingsPolCu, "SettingsPageVisibility", hide ? "hide:home" : "show:home");
        SetDword(Hive.HkLm, CloudContent, "DisableConsumerAccountStateContent", hide ? 1 : 0);
    }

    private static bool SettingsHomeHidden()
    {
        using var baseKey = OpenBase(Hive.HkCu);
        using var k = baseKey.OpenSubKey(SettingsPolCu);
        var v = Convert.ToString(k?.GetValue("SettingsPageVisibility")) ?? "";
        return v.IndexOf("hide:home", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static void SetWin11ExtraAi(bool disable)
    {
        foreach (var hive in new[] { Hive.HkLm, Hive.HkCu })
        {
            var key = hive == Hive.HkLm ? WindowsAiLm : WindowsAiCu;
            SetDword(hive, key, "DisableAIDataAnalysis", disable ? 1 : 0);
            SetDword(hive, key, "DisableClickToDo", disable ? 1 : 0);
            SetDword(hive, key, "TurnOffSavingSnapshots", disable ? 1 : 0);
        }
        SetDword(Hive.HkLm, WindowsAiLm, "AllowRecallEnablement", disable ? 0 : 1);
        SetDword(Hive.HkLm, NotepadPol, "DisableAIFeatures", disable ? 1 : 0);
        SetDword(Hive.HkLm, PaintPol, "DisableCocreator", disable ? 1 : 0);
        SetDword(Hive.HkLm, PaintPol, "DisableGenerativeFill", disable ? 1 : 0);
        SetDword(Hive.HkLm, PaintPol, "DisableImageCreator", disable ? 1 : 0);
        SetDword(Hive.HkLm, PaintPol, "DisableGenerativeErase", disable ? 1 : 0);
        SetDword(Hive.HkLm, PaintPol, "DisableRemoveBackground", disable ? 1 : 0);
        SetServiceOptional("WSAIFabricSvc", !disable);
    }

    private static void SetServiceOptional(string name, bool enable)
    {
        if (!KeyExists(Hive.HkLm, @"SYSTEM\CurrentControlSet\Services\" + name)) return;
        Run("sc.exe", enable ? $"config {name} start= demand" : $"config {name} start= disabled");
        if (!enable) Run("sc.exe", "stop " + name);
    }

    private enum Hive { HkLm, HkCu }

    private static RegistryKey OpenBase(Hive hive) =>
        RegistryKey.OpenBaseKey(
            hive == Hive.HkLm ? RegistryHive.LocalMachine : RegistryHive.CurrentUser,
            hive == Hive.HkLm ? RegistryView.Registry64 : RegistryView.Default);

    private static bool DwordEquals(Hive hive, string key, string name, int expected)
    {
        using var baseKey = OpenBase(hive);
        using var k = baseKey.OpenSubKey(key);
        return k?.GetValue(name) is int i && i == expected;
    }

    private static bool KeyExists(Hive hive, string key)
    {
        using var baseKey = OpenBase(hive);
        using var k = baseKey.OpenSubKey(key);
        return k is not null;
    }

    private static object? GetValue(Hive hive, string key, string name)
    {
        using var baseKey = OpenBase(hive);
        using var k = baseKey.OpenSubKey(key);
        return k?.GetValue(name);
    }

    private static void SetDword(Hive hive, string key, string name, int value)
    {
        var old = GetValue(hive, key, name);
        ApplyLog.RegistryDword(hive == Hive.HkLm ? "HKLM" : "HKCU", key, name, old, value);
        using var baseKey = OpenBase(hive);
        using var k = baseKey.CreateSubKey(key, writable: true);
        k?.SetValue(name, value, RegistryValueKind.DWord);
    }

    private static void SetString(Hive hive, string key, string name, string value)
    {
        var old = GetValue(hive, key, name);
        ApplyLog.RegistryString(hive == Hive.HkLm ? "HKLM" : "HKCU", key, name, old, value);
        using var baseKey = OpenBase(hive);
        using var k = baseKey.CreateSubKey(key, writable: true);
        k?.SetValue(name, value, RegistryValueKind.String);
    }

    private static void DeleteValue(Hive hive, string key, string name)
    {
        var old = GetValue(hive, key, name);
        ApplyLog.RegistryDelete(hive == Hive.HkLm ? "HKLM" : "HKCU", key, name, old);
        using var baseKey = OpenBase(hive);
        using var k = baseKey.OpenSubKey(key, writable: true);
        k?.DeleteValue(name, throwOnMissingValue: false);
    }

    private static void Run(string file, string args)
    {
        try
        {
            using var p = Process.Start(new ProcessStartInfo
            {
                FileName = file,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });
            p?.WaitForExit(20_000);
        }
        catch (Exception ex)
        {
            ApplyLog.Debug($"{file} {args} 失败：{ex.Message}");
        }
    }
}
