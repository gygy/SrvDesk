using System.Reflection;

namespace WinOpt;

/// <summary>对标 WinUtil / m2nlight 的预设方案。</summary>
internal static class OptPresets
{
    internal sealed class PresetInfo
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public Func<Optimizer.State> Build { get; set; } = () => new Optimizer.State();

        public override string ToString() => Title;
    }

    public static IReadOnlyList<PresetInfo> All { get; } = new List<PresetInfo>
    {
        new()
        {
            Id = "server-desktop",
            Title = "Server 桌面（推荐）",
            Description = "Server 当日常桌面：个性化、RDP、隐私与性能项全开，对齐 m2nlight / 社区 Server 桌面帖。",
            Build = ServerDesktop,
        },
        new()
        {
            Id = "security",
            Title = "安全加固",
            Description = "保留 UAC/NLA/密码复杂性，强化 SMB1、Remote Registry、遥测与远程管理关闭。",
            Build = SecurityHardened,
        },
        new()
        {
            Id = "remote-work",
            Title = "远程办公",
            Description = "RDP 高帧率+GPU、高性能电源、动画关闭，适合长期远程桌面办公。",
            Build = RemoteWork,
        },
        new()
        {
            Id = "minimal",
            Title = "最小改动",
            Description = "仅 Server 专属与账户便利项，尽量少动系统默认。",
            Build = Minimal,
        },
    };

    public static PresetInfo? Find(string id) =>
        All.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    public static Optimizer.State ServerDesktop()
    {
        var s = new Optimizer.State();
        foreach (var field in typeof(Optimizer.State).GetFields(BindingFlags.Instance | BindingFlags.Public))
        {
            if (field.FieldType == typeof(bool))
                field.SetValue(s, true);
        }
        // 与「全开」语义冲突的项：Server 桌面帖推荐关闭搜索/Defender，改用 DISM 项
        s.EnableSearch = false;
        s.DisableSearchEngineFeature = true;
        s.DisableDefenderAntivirus = true;
        s.EnableUtcTime = false;
        s.DisableHpet = false;
        s.EnableF8BootMenu = false;
        s.EnableLoginVerbose = false;
        return s;
    }

    public static Optimizer.State SecurityHardened()
    {
        var s = ServerDesktop();
        s.DisableUac = false;
        s.DisableCad = false;
        s.DisablePasswordComplexity = false;
        s.RdpDisableNla = false;
        s.DisableSmb1 = true;
        s.DisableRemoteRegistry = true;
        s.DisableTelemetry = true;
        s.DisableSmRemoting = true;
        s.DisablePrintSpooler = true;
        s.DisableAutoplay = true;
        s.ExcludeDriverUpdates = true;
        s.DisableMeltdownSpectre = false;
        s.DisableMemoryIntegrity = false;
        s.DisableWdac = false;
        s.DisableVbs = false;
        s.EnableUtcTime = false;
        s.DisableHpet = false;
        s.EnableF8BootMenu = false;
        s.EnableLoginVerbose = false;
        return s;
    }

    public static Optimizer.State RemoteWork()
    {
        var s = ServerDesktop();
        s.EnableRdp = true;
        s.RdpGpuAccel = true;
        s.RdpHighRefresh = true;
        s.RdpDisableNla = false;
        s.HighPerfPower = true;
        s.DisableAnimations = true;
        s.VisualBestPerf = true;
        s.DisableTransparency = true;
        s.PowerThrottlingOff = true;
        s.DisableSysMain = true;
        s.EnableNetworkDiscovery = true;
        return s;
    }

    public static Optimizer.State Minimal()
    {
        var s = new Optimizer.State();
        s.DisableIeEsc = true;
        s.SkipServerManager = true;
        s.DisableSmRemoting = true;
        s.DisableAzureArc = true;
        s.DisableShutdownReason = true;
        s.DisableCad = true;
        s.ShutdownWithoutLogon = true;
        s.DisablePasswordComplexity = true;
        s.PasswordNeverExpire = true;
        s.EnableThemes = true;
        s.EnableAudio = true;
        s.EnableInstaller = true;
        return s;
    }
}
