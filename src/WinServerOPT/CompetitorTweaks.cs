using System.Diagnostics;
using Microsoft.Win32;

namespace WinOpt;

/// <summary>对齐 hellzerg Optimizer / SophiApp 的常用注册表与服务项（文档化策略为主）。</summary>
internal static class CompetitorTweaks
{
    public static void Apply(Optimizer.State s)
    {
        SetCortana(!s.DisableCortana);
        SetCopilotAi(!s.DisableCopilotAi);
        SetOfficeTelemetry(!s.DisableOfficeTelemetry);
        SetUtcTime(s.EnableUtcTime);
        SetHpet(!s.DisableHpet);
        SetLoginVerbose(s.EnableLoginVerbose);
        SetNetworkThrottling(!s.DisableNetworkThrottling);
        SetGameDvr(!s.DisableGameDvr);
        SetLocation(!s.DisableLocationTracking);
        SetConsumerFeatures(!s.DisableConsumerFeatures);
        SetEdgePreload(!s.DisableEdgePreload);
        SetTeredo(!s.DisableTeredo);
        SetClipboardCloud(!s.DisableClipboardCloud);
        SetNtfsLastAccess(!s.DisableNtfsLastAccess);
        SetXbox(!s.DisableXboxServices);
        SetFax(!s.DisableFaxService);
        SetF8Menu(s.EnableF8BootMenu);
        SetTakeOwnership(s.ContextMenuTakeOwnership);
        SetOpenCmdHere(s.ContextMenuOpenCmd);
        SetMediaSharing(!s.DisableMediaPlayerSharing);
        SetInsider(!s.DisableInsiderService);
        SetStoreAutoUpdate(!s.DisableStoreAutoUpdate);
        SetNewsInterests(!s.DisableNewsInterests);
    }

    public static void ReadInto(Optimizer.State s)
    {
        s.DisableCortana = DwordEquals(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", 0);
        s.DisableCopilotAi = DwordEquals(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot", 1);
        s.DisableOfficeTelemetry = DwordEquals(Hive.HkCu, @"Software\Microsoft\Office\Common\ClientTelemetry", "DisableTelemetry", 1);
        s.EnableUtcTime = DwordEquals(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\TimeZoneInformation", "RealTimeIsUniversal", 1);
        s.DisableHpet = IsHpetDisabled();
        s.EnableLoginVerbose = DwordEquals(Hive.HkLm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "VerboseStatus", 1);
        s.DisableNetworkThrottling = IsDword(Hive.HkLm, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex", unchecked((int)0xFFFFFFFF));
        s.DisableGameDvr = DwordEquals(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\GameDVR", "AllowGameDVR", 0);
        s.DisableLocationTracking = DwordEquals(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors", "DisableLocation", 1);
        s.DisableConsumerFeatures = DwordEquals(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableWindowsConsumerFeatures", 1);
        s.DisableEdgePreload = DwordEquals(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Edge", "StartupBoostEnabled", 0);
        s.DisableTeredo = IsTeredoDisabled();
        s.DisableClipboardCloud = DwordEquals(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\System", "AllowCrossDeviceClipboard", 0);
        s.DisableNtfsLastAccess = IsNtfsLastAccessDisabled();
        s.DisableXboxServices = ServiceDisabled("XblAuthManager") && ServiceDisabled("XboxNetApiSvc");
        s.DisableFaxService = ServiceDisabled("Fax");
        s.EnableF8BootMenu = IsF8Legacy();
        s.ContextMenuTakeOwnership = IsTakeOwnershipOn();
        s.ContextMenuOpenCmd = IsOpenCmdHereOn();
        s.DisableMediaPlayerSharing = ServiceDisabled("WMPNetworkSvc");
        s.DisableInsiderService = ServiceDisabled("wisvc");
        s.DisableStoreAutoUpdate = DwordEquals(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\WindowsStore", "AutoDownload", 2);
        s.DisableNewsInterests = DwordEquals(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Dsh", "AllowNewsAndInterests", 0);
    }

    public static void RepairLockedComponents()
    {
        SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Policies\System", "DisableTaskMgr", 0);
        SetDword(Hive.HkCu, @"Software\Policies\Microsoft\Windows\System", "DisableCMD", 0);
        SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Policies\System", "DisableRegistryTools", 0);
        SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoControlPanel", 0);
        SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoFolderOptions", 0);
        SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoRun", 0);
        SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Policies\Explorer", "NoViewContextMenu", 0);
        DeleteValue(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\Windows Error Reporting", "Disabled");
        ApplyLog.Write("已修复任务管理器/CMD/注册表等可能被策略锁死的组件");
    }

    private static void SetCortana(bool enable)
    {
        SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\Windows Search", "AllowCortana", enable ? 1 : 0);
        SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Search", "CortanaConsent", enable ? 1 : 0);
        SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Search", "AllowSearchToUseLocation", enable ? 1 : 0);
    }

    private static void SetCopilotAi(bool enable)
    {
        SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot", enable ? 0 : 1);
        SetDword(Hive.HkCu, @"Software\Policies\Microsoft\Windows\WindowsCopilot", "TurnOffWindowsCopilot", enable ? 0 : 1);
        SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Edge", "HubsSidebarEnabled", enable ? 1 : 0);
        SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", "ShowCopilotButton", enable ? 1 : 0);
    }

    private static void SetOfficeTelemetry(bool enable)
    {
        SetDword(Hive.HkCu, @"Software\Microsoft\Office\Common\ClientTelemetry", "DisableTelemetry", enable ? 0 : 1);
        SetDword(Hive.HkCu, @"Software\Policies\Microsoft\office\16.0\osm", "enablelogging", enable ? 1 : 0);
        SetDword(Hive.HkCu, @"Software\Policies\Microsoft\office\16.0\osm", "enableupload", enable ? 1 : 0);
        SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Office\16.0\osm", "enablelogging", enable ? 1 : 0);
        SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Office\16.0\osm", "enableupload", enable ? 1 : 0);
    }

    private static void SetUtcTime(bool utc) =>
        SetDword(Hive.HkLm, @"SYSTEM\CurrentControlSet\Control\TimeZoneInformation", "RealTimeIsUniversal", utc ? 1 : 0);

    private static void SetHpet(bool enable)
    {
        Run("bcdedit.exe", enable ? "/set useplatformclock true" : "/deletevalue useplatformclock");
        Run("bcdedit.exe", enable ? "/set disabledynamictick no" : "/set disabledynamictick yes");
    }

    private static bool IsHpetDisabled()
    {
        try
        {
            var output = RunCapture("bcdedit.exe", "/enum {current}");
            return output.IndexOf("disabledynamictick", StringComparison.OrdinalIgnoreCase) >= 0
                && output.IndexOf("Yes", StringComparison.OrdinalIgnoreCase) >= 0;
        }
        catch { return false; }
    }

    private static void SetLoginVerbose(bool enable) =>
        SetDword(Hive.HkLm, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "VerboseStatus", enable ? 1 : 0);

    private static void SetNetworkThrottling(bool throttle)
    {
        if (throttle)
            DeleteValue(Hive.HkLm, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex");
        else
            SetDword(Hive.HkLm, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex", unchecked((int)0xFFFFFFFF));
    }

    private static void SetGameDvr(bool enable)
    {
        SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\GameDVR", "AllowGameDVR", enable ? 1 : 0);
        SetDword(Hive.HkCu, @"System\GameConfigStore", "GameDVR_Enabled", enable ? 1 : 0);
        SetDword(Hive.HkCu, @"Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", enable ? 1 : 0);
    }

    private static void SetLocation(bool enable) =>
        SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\LocationAndSensors", "DisableLocation", enable ? 0 : 1);

    private static void SetConsumerFeatures(bool enable) =>
        SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\CloudContent", "DisableWindowsConsumerFeatures", enable ? 0 : 1);

    private static void SetEdgePreload(bool enable)
    {
        SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Edge", "StartupBoostEnabled", enable ? 1 : 0);
        SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Edge", "BackgroundModeEnabled", enable ? 1 : 0);
        SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\MicrosoftEdge\Main", "AllowPrelaunch", enable ? 1 : 0);
    }

    private static void SetTeredo(bool enable) =>
        Run("netsh.exe", enable ? "interface teredo set state default" : "interface teredo set state disabled");

    private static bool IsTeredoDisabled()
    {
        try
        {
            var output = RunCapture("netsh.exe", "interface teredo show state");
            return output.IndexOf("offline", StringComparison.OrdinalIgnoreCase) >= 0
                || output.IndexOf("disabled", StringComparison.OrdinalIgnoreCase) >= 0;
        }
        catch { return false; }
    }

    private static void SetClipboardCloud(bool enable)
    {
        SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\System", "AllowCrossDeviceClipboard", enable ? 1 : 0);
        SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Windows\System", "AllowClipboardHistory", enable ? 1 : 0);
    }

    private static void SetNtfsLastAccess(bool enable) =>
        Run("fsutil.exe", enable ? "behavior set disablelastaccess 0" : "behavior set disablelastaccess 1");

    private static bool IsNtfsLastAccessDisabled()
    {
        try
        {
            var output = RunCapture("fsutil.exe", "behavior query disablelastaccess");
            return output.IndexOf("1", StringComparison.Ordinal) >= 0
                || output.IndexOf("2", StringComparison.Ordinal) >= 0
                || output.IndexOf("3", StringComparison.Ordinal) >= 0;
        }
        catch { return false; }
    }

    private static void SetXbox(bool enable)
    {
        foreach (var svc in new[] { "XblAuthManager", "XblGameSave", "XboxNetApiSvc", "XboxGipSvc" })
            SetService(svc, enable);
    }

    private static void SetFax(bool enable) => SetService("Fax", enable);

    private static void SetF8Menu(bool legacy) =>
        Run("bcdedit.exe", legacy ? "/set bootmenupolicy legacy" : "/set bootmenupolicy standard");

    private static bool IsF8Legacy()
    {
        try
        {
            var output = RunCapture("bcdedit.exe", "/enum {current}");
            return output.IndexOf("legacy", StringComparison.OrdinalIgnoreCase) >= 0;
        }
        catch { return false; }
    }

    private static void SetMediaSharing(bool enable) => SetService("WMPNetworkSvc", enable);

    private static void SetInsider(bool enable) => SetService("wisvc", enable);

    private static void SetStoreAutoUpdate(bool enable) =>
        SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\WindowsStore", "AutoDownload", enable ? 4 : 2);

    private static void SetNewsInterests(bool enable) =>
        SetDword(Hive.HkLm, @"SOFTWARE\Policies\Microsoft\Dsh", "AllowNewsAndInterests", enable ? 1 : 0);

    private const string TakeOwnClsid = @"*\shell\WinOptTakeOwnership";

    private static bool IsTakeOwnershipOn()
    {
        using var k = Registry.ClassesRoot.OpenSubKey(TakeOwnClsid);
        return k is not null;
    }

    private static void SetTakeOwnership(bool enable)
    {
        if (!enable)
        {
            try { Registry.ClassesRoot.DeleteSubKeyTree(TakeOwnClsid, throwOnMissingSubKey: false); }
            catch { /* ignore */ }
            return;
        }

        using var k = Registry.ClassesRoot.CreateSubKey(TakeOwnClsid);
        k?.SetValue("", "取得所有权");
        using var cmd = Registry.ClassesRoot.CreateSubKey(TakeOwnClsid + @"\command");
        cmd?.SetValue("", "cmd.exe /c takeown /f \"%1\" /r /d y & icacls \"%1\" /grant administrators:F /t");
    }

    private const string OpenCmdClsid = @"Directory\shell\WinOptOpenCmd";

    private static bool IsOpenCmdHereOn()
    {
        using var k = Registry.ClassesRoot.OpenSubKey(OpenCmdClsid);
        return k is not null;
    }

    private static void SetOpenCmdHere(bool enable)
    {
        if (!enable)
        {
            try { Registry.ClassesRoot.DeleteSubKeyTree(OpenCmdClsid, throwOnMissingSubKey: false); }
            catch { /* ignore */ }
            return;
        }

        using var k = Registry.ClassesRoot.CreateSubKey(OpenCmdClsid);
        k?.SetValue("", "在此处打开命令提示符");
        using var cmd = Registry.ClassesRoot.CreateSubKey(OpenCmdClsid + @"\command");
        cmd?.SetValue("", "cmd.exe /s /k pushd \"%V\"");
    }

    private static bool ServiceDisabled(string name) =>
        DwordEquals(Hive.HkLm, $@"SYSTEM\CurrentControlSet\Services\{name}", "Start", 4);

    private static void SetService(string name, bool enable)
    {
        if (!RegistryKeyExists(@"SYSTEM\CurrentControlSet\Services\" + name)) return;
        Run("sc.exe", enable ? $"config {name} start= demand" : $"config {name} start= disabled");
        if (!enable) Run("sc.exe", "stop " + name);
    }

    private static bool RegistryKeyExists(string path)
    {
        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var k = baseKey.OpenSubKey(path);
        return k is not null;
    }

    private enum Hive { HkLm, HkCu }

    private static bool IsDword(Hive hive, string key, string name, int expected)
    {
        using var baseKey = RegistryKey.OpenBaseKey(
            hive == Hive.HkLm ? RegistryHive.LocalMachine : RegistryHive.CurrentUser,
            hive == Hive.HkLm ? RegistryView.Registry64 : RegistryView.Default);
        using var k = baseKey.OpenSubKey(key);
        return k?.GetValue(name) is int i && i == expected;
    }

    private static bool DwordEquals(Hive hive, string key, string name, int expected) =>
        GetDword(hive, key, name) == expected;

    private static int GetDword(Hive hive, string key, string name)
    {
        using var baseKey = RegistryKey.OpenBaseKey(
            hive == Hive.HkLm ? RegistryHive.LocalMachine : RegistryHive.CurrentUser,
            hive == Hive.HkLm ? RegistryView.Registry64 : RegistryView.Default);
        using var k = baseKey.OpenSubKey(key);
        return k?.GetValue(name) switch { int i => i, _ => -1 };
    }

    private static void SetDword(Hive hive, string key, string name, int value)
    {
        using var baseKey = RegistryKey.OpenBaseKey(
            hive == Hive.HkLm ? RegistryHive.LocalMachine : RegistryHive.CurrentUser,
            hive == Hive.HkLm ? RegistryView.Registry64 : RegistryView.Default);
        using var k = baseKey.CreateSubKey(key, true);
        k?.SetValue(name, value, RegistryValueKind.DWord);
    }

    private static void DeleteValue(Hive hive, string key, string name)
    {
        using var baseKey = RegistryKey.OpenBaseKey(
            hive == Hive.HkLm ? RegistryHive.LocalMachine : RegistryHive.CurrentUser,
            hive == Hive.HkLm ? RegistryView.Registry64 : RegistryView.Default);
        using var k = baseKey.OpenSubKey(key, true);
        k?.DeleteValue(name, throwOnMissingValue: false);
    }

    private static void Run(string file, string args)
    {
        using var p = Process.Start(new ProcessStartInfo
        {
            FileName = file, Arguments = args, UseShellExecute = false,
            CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true,
        });
        p?.WaitForExit(20_000);
    }

    private static string RunCapture(string file, string args)
    {
        using var p = Process.Start(new ProcessStartInfo
        {
            FileName = file, Arguments = args, UseShellExecute = false,
            CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true,
        }) ?? throw new InvalidOperationException("无法启动 " + file);
        var o = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
        p.WaitForExit(15_000);
        return o;
    }
}
