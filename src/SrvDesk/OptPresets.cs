using System.Reflection;

namespace SrvDesk;

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

    public static IReadOnlyList<PresetInfo> All => BuildList();

    static List<PresetInfo> BuildList() =>
    [
        new()
        {
            Id = "server-desktop",
            Title = AppLang.L("Server 桌面（推荐）", "Server desktop (recommended)"),
            Description = AppLang.L(
                "Server 当日常桌面：个性化、RDP、隐私与性能项全开，对齐 m2nlight / 社区 Server 桌面帖。",
                "Server as daily desktop: personalization, RDP, privacy and performance on; aligned with common Server-desktop guides."),
            Build = ServerDesktop,
        },
        new()
        {
            Id = "security",
            Title = AppLang.L("安全加固", "Security hardened"),
            Description = AppLang.L(
                "保留 UAC、NLA 与密码复杂性；关闭 SMB1、Remote Registry、遥测与远程管理。",
                "Keep UAC, NLA and password complexity; disable SMB1, Remote Registry, telemetry and remote mgmt."),
            Build = SecurityHardened,
        },
        new()
        {
            Id = "remote-work",
            Title = AppLang.L("远程办公", "Remote work"),
            Description = AppLang.L(
                "RDP 高帧率与 GPU、高性能电源、关闭动画，适合长期远程桌面。",
                "High-FPS RDP with GPU, high-performance power, animations off — for long remote sessions."),
            Build = RemoteWork,
        },
        new()
        {
            Id = "minimal",
            Title = AppLang.L("最小改动", "Minimal changes"),
            Description = AppLang.L(
                "只改 Server 专属与账户便利项，少动系统默认。",
                "Only Server-specific and account convenience items; leave most defaults alone."),
            Build = Minimal,
        },
        new()
        {
            Id = "profile-home",
            Title = AppLang.L("家庭服务器（用途模板）", "Home server (profile)"),
            Description = AppLang.L(
                "写入用途：RDP + 文件共享 + 家庭场景；开关接近 Server 桌面，优化等级=标准。",
                "Sets profile: RDP + file share + home lab; toggles near Server desktop; level=Standard."),
            Build = () =>
            {
                ApplyProfilePreset(ServerRoleFlags.RdpDesktop | ServerRoleFlags.FileShare | ServerRoleFlags.HomeLab | ServerRoleFlags.BrowserDownload,
                    OptimizationLevel.Standard);
                return ServerDesktop();
            },
        },
        new()
        {
            Id = "profile-docker",
            Title = AppLang.L("Docker 主机（用途模板）", "Docker host (profile)"),
            Description = AppLang.L(
                "写入用途：Docker + RDP；保留容器相关；优化等级=安全。",
                "Sets profile: Docker + RDP; keep container-related; level=Safe."),
            Build = () =>
            {
                ApplyProfilePreset(ServerRoleFlags.Docker | ServerRoleFlags.RdpDesktop | ServerRoleFlags.BrowserDownload,
                    OptimizationLevel.Safe);
                return RemoteWork();
            },
        },
        new()
        {
            Id = "profile-nas",
            Title = AppLang.L("NAS / 文件服（用途模板）", "NAS / file server (profile)"),
            Description = AppLang.L(
                "写入用途：文件共享；强调 SMB 安全（关 SMB1）；优化等级=标准。",
                "Sets profile: file share; SMB safety (no SMB1); level=Standard."),
            Build = () =>
            {
                ApplyProfilePreset(ServerRoleFlags.FileShare | ServerRoleFlags.HomeLab, OptimizationLevel.Standard);
                var s = SecurityHardened();
                return s;
            },
        },
    ];

    private static void ApplyProfilePreset(ServerRoleFlags roles, OptimizationLevel level)
    {
        var data = ServerProfile.Load();
        data.Roles = (int)roles;
        data.OptimizationLevel = (int)level;
        data.ProfileConfigured = true;
        ServerProfile.Save(data);
    }

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
        s.EnableSearch = false;
        s.DisableSearchEngineFeature = true;
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
        s.RdpMultiUserLogin = true;
        s.RdpGpuAccel = true;
        s.RdpHighRefresh = true;
        s.RdpDisableNla = false;
        s.RdpAvc444 = true;
        s.RdpAvcHwEncode = true;
        s.RdpHwGraphicsFirst = true;
        s.RdpRemoteFxGraphics = true;
        s.RdpLowLatency = true;
        s.RdpDisableWddm = false;
        s.HighPerfPower = true;
        s.DisableAnimations = true;
        s.VisualBestPerf = true;
        s.DisableTransparency = true;
        s.PowerThrottlingOff = true;
        s.DisableSysMain = true;
        s.EnableNetworkDiscovery = true;
        s.DisableSmbBandwidthThrottling = true;
        s.DisableNetworkThrottling = true;
        return s;
    }

    public static Optimizer.State Minimal()
    {
        var s = new Optimizer.State();
        s.DisableIeEsc = true;
        s.SkipServerManager = true;
        s.HideServerManagerWacPrompt = true;
        s.DisableSmRemoting = true;
        s.DisableAzureArc = true;
        s.DisableShutdownReason = true;
        s.DisableCad = true;
        s.ShutdownWithoutLogon = true;
        s.DisablePasswordComplexity = true;
        s.PasswordNeverExpire = true;
        s.DisablePasswordHistory = true;
        s.EnableThemes = true;
        s.EnableAudio = true;
        s.EnableInstaller = true;
        return s;
    }
}
