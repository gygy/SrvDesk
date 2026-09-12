using Microsoft.Win32;

namespace SrvDesk;

/// <summary>
/// 对齐 r/sysadmin「Pushing RemoteFX to its limits」与 TurboRemoteFXHost 可逆开关。
/// 不含已废弃的 Hyper-V RemoteFX vGPU 硬件分配；聚焦 RDP 主机端 AVC/流控/延迟。
/// </summary>
internal static class RemoteFxTweaks
{
    private const string TermServices = @"SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services";
    private const string TermDd = @"SYSTEM\CurrentControlSet\Services\TermDD";
    private const string RdpTcp = @"SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp";
    private const string LanmanWorkstation = @"SYSTEM\CurrentControlSet\Services\LanmanWorkstation\Parameters";

    public static void Apply(Optimizer.State s, Optimizer.State? baseline = null)
    {
        bool D(Func<Optimizer.State, bool> f) => baseline is null || f(baseline) != f(s);
        bool Take(string field, Func<Optimizer.State, bool> f)
        {
            if (!D(f)) return false;
            ApplyLog.DebugField(field, baseline is null ? null : f(baseline), f(s));
            return true;
        }

        if (Take("RdpAvc444", x => x.RdpAvc444))
            SetOrDeleteDword(Hive.HkLm, TermServices, "AVC444ModePreferred", s.RdpAvc444, 1);

        if (Take("RdpAvcHwEncode", x => x.RdpAvcHwEncode))
            SetOrDeleteDword(Hive.HkLm, TermServices, "AVCHardwareEncodePreferred", s.RdpAvcHwEncode, 1);

        if (Take("RdpHwGraphicsFirst", x => x.RdpHwGraphicsFirst))
            SetOrDeleteDword(Hive.HkLm, TermServices, "bEnumerateHWBeforeSW", s.RdpHwGraphicsFirst, 1);

        if (Take("RdpRemoteFxGraphics", x => x.RdpRemoteFxGraphics))
            SetRemoteFxGraphics(s.RdpRemoteFxGraphics);

        if (Take("RdpLowLatency", x => x.RdpLowLatency))
            SetLowLatency(s.RdpLowLatency);

        if (Take("RdpDisableWddm", x => x.RdpDisableWddm))
            SetOrDeleteDword(Hive.HkLm, TermServices, "fEnableWddmDriver", s.RdpDisableWddm, 0);
    }

    public static void ReadInto(Optimizer.State s)
    {
        s.RdpAvc444 = DwordEquals(Hive.HkLm, TermServices, "AVC444ModePreferred", 1);
        s.RdpAvcHwEncode = DwordEquals(Hive.HkLm, TermServices, "AVCHardwareEncodePreferred", 1);
        s.RdpHwGraphicsFirst = DwordEquals(Hive.HkLm, TermServices, "bEnumerateHWBeforeSW", 1);
        s.RdpRemoteFxGraphics = IsRemoteFxGraphicsOn();
        s.RdpLowLatency = IsLowLatencyOn();
        s.RdpDisableWddm = DwordEquals(Hive.HkLm, TermServices, "fEnableWddmDriver", 0);
    }

    public static bool AnyChanged(Optimizer.State? b, Optimizer.State s)
    {
        if (b is null) return true;
        return b.RdpAvc444 != s.RdpAvc444
            || b.RdpAvcHwEncode != s.RdpAvcHwEncode
            || b.RdpHwGraphicsFirst != s.RdpHwGraphicsFirst
            || b.RdpRemoteFxGraphics != s.RdpRemoteFxGraphics
            || b.RdpLowLatency != s.RdpLowLatency
            || b.RdpDisableWddm != s.RdpDisableWddm;
    }

    private static void SetRemoteFxGraphics(bool on)
    {
        if (on)
        {
            SetDword(Hive.HkLm, TermServices, "SelectTransport", 0);
            SetDword(Hive.HkLm, TermServices, "fEnableVirtualizedGraphics", 1);
            SetDword(Hive.HkLm, TermServices, "VGOptimization_CaptureFrameRate", 1);
            SetDword(Hive.HkLm, TermServices, "VGOptimization_CompressionRatio", 1);
            SetDword(Hive.HkLm, TermServices, "VisualExperiencePolicy", 1);
            SetDword(Hive.HkLm, TermServices, "ImageQuality", 2);
            SetDword(Hive.HkLm, TermServices, "MaxCompressionLevel", 0);
        }
        else
        {
            DeleteValue(Hive.HkLm, TermServices, "SelectTransport");
            DeleteValue(Hive.HkLm, TermServices, "fEnableVirtualizedGraphics");
            DeleteValue(Hive.HkLm, TermServices, "VGOptimization_CaptureFrameRate");
            DeleteValue(Hive.HkLm, TermServices, "VGOptimization_CompressionRatio");
            DeleteValue(Hive.HkLm, TermServices, "VisualExperiencePolicy");
            DeleteValue(Hive.HkLm, TermServices, "ImageQuality");
            DeleteValue(Hive.HkLm, TermServices, "MaxCompressionLevel");
        }
    }

    private static bool IsRemoteFxGraphicsOn() =>
        DwordEquals(Hive.HkLm, TermServices, "fEnableVirtualizedGraphics", 1)
        && DwordEquals(Hive.HkLm, TermServices, "VisualExperiencePolicy", 1)
        && DwordEquals(Hive.HkLm, TermServices, "ImageQuality", 2)
        && DwordEquals(Hive.HkLm, TermServices, "MaxCompressionLevel", 0);

    private static void SetLowLatency(bool on)
    {
        if (on)
        {
            SetDword(Hive.HkLm, RdpTcp, "InteractiveDelay", 0);
            SetDword(Hive.HkLm, TermDd, "FlowControlDisable", 1);
            SetDword(Hive.HkLm, TermDd, "FlowControlDisplayBandwidth", 0x10);
            SetDword(Hive.HkLm, TermDd, "FlowControlChannelBandwidth", 0x90);
            SetDword(Hive.HkLm, TermDd, "FlowControlChargePostCompression", 0);
            SetDword(Hive.HkLm, LanmanWorkstation, "DisableLargeMtu", 0);
        }
        else
        {
            SetDword(Hive.HkLm, RdpTcp, "InteractiveDelay", 0x32); // 系统常见默认 50
            DeleteValue(Hive.HkLm, TermDd, "FlowControlDisable");
            DeleteValue(Hive.HkLm, TermDd, "FlowControlDisplayBandwidth");
            DeleteValue(Hive.HkLm, TermDd, "FlowControlChannelBandwidth");
            DeleteValue(Hive.HkLm, TermDd, "FlowControlChargePostCompression");
            DeleteValue(Hive.HkLm, LanmanWorkstation, "DisableLargeMtu");
        }
    }

    private static bool IsLowLatencyOn() =>
        DwordEquals(Hive.HkLm, RdpTcp, "InteractiveDelay", 0)
        && DwordEquals(Hive.HkLm, TermDd, "FlowControlDisable", 1)
        && DwordEquals(Hive.HkLm, TermDd, "FlowControlDisplayBandwidth", 0x10);

    private static void SetOrDeleteDword(Hive hive, string subKey, string name, bool on, int onValue)
    {
        if (on) SetDword(hive, subKey, name, onValue);
        else DeleteValue(hive, subKey, name);
    }

    private static void SetDword(Hive hive, string subKey, string name, int value)
    {
        using var key = OpenOrCreate(hive, subKey);
        key?.SetValue(name, value, RegistryValueKind.DWord);
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

    private static bool DwordEquals(Hive hive, string subKey, string name, int expected)
    {
        using var key = OpenKey(hive, subKey);
        if (key?.GetValue(name) is int i) return i == expected;
        if (key?.GetValue(name) is long l) return unchecked((int)l) == expected;
        return false;
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
