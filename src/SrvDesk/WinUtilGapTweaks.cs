using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace SrvDesk;

/// <summary>对齐 WinUtil 的可逆 QoL 开关（电池%、滚动条、NumLock、鼠标加速、WPBT、开始菜单旧布局）。</summary>
internal static class WinUtilGapTweaks
{
    private const string ExplorerAdv = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
    private const string Accessibility = @"Control Panel\Accessibility";
    private const string Keyboard = @"Control Panel\Keyboard";
    private const string Mouse = @"Control Panel\Mouse";
    private const string SessionManager = @"SYSTEM\CurrentControlSet\Control\Session Manager";
    public static void Apply(Optimizer.State s, Optimizer.State? baseline = null)
    {
        bool D(Func<Optimizer.State, bool> f) => baseline is null || f(baseline) != f(s);
        bool Take(string field, Func<Optimizer.State, bool> f)
        {
            if (!D(f)) return false;
            ApplyLog.DebugField(field, baseline is null ? null : f(baseline), f(s));
            return true;
        }

        if (Take("ShowTrayBatteryPercent", x => x.ShowTrayBatteryPercent))
            SetDword(Hive.HkCu, ExplorerAdv, "TaskbarShowBatteryPercentage", s.ShowTrayBatteryPercent ? 1 : 0);

        if (Take("AlwaysShowScrollbars", x => x.AlwaysShowScrollbars))
            SetDword(Hive.HkCu, Accessibility, "DynamicScrollbars", s.AlwaysShowScrollbars ? 0 : 1);

        if (Take("NumLockOnBoot", x => x.NumLockOnBoot))
            SetNumLockOnBoot(s.NumLockOnBoot);

        if (Take("DisableMouseAcceleration", x => x.DisableMouseAcceleration))
            SetMouseAcceleration(!s.DisableMouseAcceleration);

        if (Take("DisableWpbt", x => x.DisableWpbt))
            SetDword(Hive.HkLm, SessionManager, "DisableWpbtExecution", s.DisableWpbt ? 1 : 0);

        if (Take("Win11StartMenuPreviousLayout", x => x.Win11StartMenuPreviousLayout))
            SetDword(Hive.HkCu, ExplorerAdv, "Start_ShowClassicMode", s.Win11StartMenuPreviousLayout ? 1 : 0);
    }

    public static void ReadInto(Optimizer.State s)
    {
        s.ShowTrayBatteryPercent = DwordEquals(Hive.HkCu, ExplorerAdv, "TaskbarShowBatteryPercentage", 1);
        s.AlwaysShowScrollbars = DwordEquals(Hive.HkCu, Accessibility, "DynamicScrollbars", 0);
        s.NumLockOnBoot = IsNumLockOnBoot();
        s.DisableMouseAcceleration = IsMouseAccelerationOff();
        s.DisableWpbt = DwordEquals(Hive.HkLm, SessionManager, "DisableWpbtExecution", 1);
        s.Win11StartMenuPreviousLayout = DwordEquals(Hive.HkCu, ExplorerAdv, "Start_ShowClassicMode", 1);
    }

    public static bool AnyChanged(Optimizer.State? b, Optimizer.State s)
    {
        if (b is null) return true;
        return b.ShowTrayBatteryPercent != s.ShowTrayBatteryPercent
            || b.AlwaysShowScrollbars != s.AlwaysShowScrollbars
            || b.NumLockOnBoot != s.NumLockOnBoot
            || b.DisableMouseAcceleration != s.DisableMouseAcceleration
            || b.DisableWpbt != s.DisableWpbt
            || b.Win11StartMenuPreviousLayout != s.Win11StartMenuPreviousLayout;
    }

    private static void SetNumLockOnBoot(bool on)
    {
        var value = on ? "2" : "0";
        SetSz(Hive.HkCu, Keyboard, "InitialKeyboardIndicators", value);
        try
        {
            using var users = RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Registry64);
            using var def = users.CreateSubKey(@".DEFAULT\Control Panel\Keyboard", true);
            def?.SetValue("InitialKeyboardIndicators", value, RegistryValueKind.String);
        }
        catch (Exception ex)
        {
            ApplyLog.Debug("NumLock .DEFAULT：" + ex.Message);
        }
    }

    private static bool IsNumLockOnBoot() =>
        SzEquals(Hive.HkCu, Keyboard, "InitialKeyboardIndicators", "2")
        || SzEquals(Hive.HkCu, Keyboard, "InitialKeyboardIndicators", "2147483650");

    private static void SetMouseAcceleration(bool enabled)
    {
        // Enhance pointer precision：开=1,1,6 / 关=0,0,0 + MouseSpeed
        SetSz(Hive.HkCu, Mouse, "MouseSpeed", enabled ? "1" : "0");
        SetSz(Hive.HkCu, Mouse, "MouseThreshold1", enabled ? "6" : "0");
        SetSz(Hive.HkCu, Mouse, "MouseThreshold2", enabled ? "10" : "0");
        try
        {
            var mouseParams = new[] { 0, enabled ? 6 : 0, enabled ? 10 : 0 };
            SystemParametersInfo(0x0004 /* SPI_SETMOUSE */, 0, mouseParams, 0x01 | 0x02);
        }
        catch (Exception ex)
        {
            ApplyLog.Debug("SPI_SETMOUSE：" + ex.Message);
        }
    }

    private static bool IsMouseAccelerationOff() =>
        SzEquals(Hive.HkCu, Mouse, "MouseSpeed", "0")
        && SzEquals(Hive.HkCu, Mouse, "MouseThreshold1", "0")
        && SzEquals(Hive.HkCu, Mouse, "MouseThreshold2", "0");

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, int[] pvParam, uint fWinIni);

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
