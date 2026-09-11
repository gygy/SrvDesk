namespace SrvDesk;

/// <summary>
/// 按本机 OS / 桌面体验 / 虚拟机等，解析「有效推荐强度」。
/// 原则：默认只抬高低风险、可逆、体验收益明确的项；安全缓解与激进性能项降为高级/不推荐。
/// </summary>
internal static class RecommendRules
{
    /// <summary>优化顾问默认只展示此强度及以上（强烈推荐 + 推荐）。</summary>
    public const RecommendLevel AdvisorMinLevel = RecommendLevel.Strong;

    public static RecommendLevel Resolve(SettingHelpInfo help, SystemFacts? facts)
    {
        if (facts is null)
            return help.Recommend;

        // Server Core：桌面类项不应出现在推荐链路
        if (facts.IsServerCore && help.Scope.RequiresDesktopExperience)
            return RecommendLevel.Optional;

        // —— 明确：不推荐作「一键优化」的安全/激进项 ——
        if (IsSecurityMitigationDisable(help))
            return RecommendLevel.Optional;
        if (IsAggressivePerf(help))
            return DemoteAggressive(help.Recommend, facts);

        // —— SysMain：物理机默认不推；虚拟机可推荐 ——
        if (ReferenceEquals(help, SettingCatalog.DisableSysMain))
        {
            if (facts.IsVirtualMachine)
                return RecommendLevel.Strong;
            return RecommendLevel.Suggested; // 高级：SSD 可选
        }

        // —— 产品指定：强烈推荐（列表 + 优化顾问同步） ——
        if (IsProductMust(help))
            return RecommendLevel.Must;

        // —— Server 桌面体验：产品差异化强推 ——
        if (facts.IsServer && facts.HasDesktopExperience)
        {
            if (ReferenceEquals(help, SettingCatalog.SkipServerManager))
                return RecommendLevel.Must;
            if (ReferenceEquals(help, SettingCatalog.HideServerManagerWacPrompt))
                return RecommendLevel.Must;
            if (ReferenceEquals(help, SettingCatalog.DisableAzureArc))
                return RecommendLevel.Strong;
            if (ReferenceEquals(help, SettingCatalog.DisableIeEsc))
                return RecommendLevel.Must;
            if (ReferenceEquals(help, SettingCatalog.EnableAudio))
                return RecommendLevel.Must;
            // 账户策略：个人/桌面 Server 体验项 → 强烈推荐（顾问同步）
            if (ReferenceEquals(help, SettingCatalog.DisablePasswordComplexity)
                || ReferenceEquals(help, SettingCatalog.DisableCad)
                || ReferenceEquals(help, SettingCatalog.DisableShutdownReason))
                return RecommendLevel.Must;
        }

        // —— 客户端隐私 / 体验：Win10/11 强推 ——
        if (!facts.IsServer && IsClientPrivacyBoost(help))
            return Max(help.Recommend, RecommendLevel.Strong);

        // —— 通用安全推荐 ——
        if (ReferenceEquals(help, SettingCatalog.DisableSmb1))
            return RecommendLevel.Must;
        if (ReferenceEquals(help, SettingCatalog.DisableRemoteRegistry))
            return RecommendLevel.Must;
        if (ReferenceEquals(help, SettingCatalog.ShowFileExtensions))
            return RecommendLevel.Must;
        if (ReferenceEquals(help, SettingCatalog.DisableSmartScreenWarning))
            return RecommendLevel.Optional; // 不要默认关 SmartScreen

        // —— Server 2025：更保守，压低「老优化」 ——
        if (facts.Kind == WindowsOsKind.Server2025 && IsLegacyPerfTweak(help))
            return Min(help.Recommend, RecommendLevel.Suggested);

        // —— 隐藏受保护系统文件：推荐保持隐藏 = 开启本项 ——
        if (ReferenceEquals(help, SettingCatalog.HideProtectedOsFiles))
            return RecommendLevel.Strong;

        return help.Recommend;
    }

    public static RecommendLevel ResolveService(
        string serviceName,
        ServiceRecommend want,
        SystemFacts? facts)
    {
        if (want == ServiceRecommend.Keep)
            return RecommendLevel.Optional;

        var name = serviceName ?? "";
        // Windows Search：Server 桌面 / 客户端个人用途不作为强推关闭
        if (name.Equals("WSearch", StringComparison.OrdinalIgnoreCase))
        {
            if (facts is { IsServer: true, HasDesktopExperience: true })
                return RecommendLevel.Optional; // 应保留 → 顾问不列
            if (facts is { IsServer: false })
                return RecommendLevel.Suggested; // 高级
            return RecommendLevel.Strong;
        }

        // SysMain：与开关策略一致
        if (name.Equals("SysMain", StringComparison.OrdinalIgnoreCase))
        {
            if (facts?.IsVirtualMachine == true)
                return RecommendLevel.Strong;
            return RecommendLevel.Suggested;
        }

        // 安全/垃圾类：强烈推荐关闭
        if (IsMustDisableService(name))
            return want == ServiceRecommend.Disable ? RecommendLevel.Must : RecommendLevel.Strong;

        return want switch
        {
            ServiceRecommend.Disable => RecommendLevel.Strong,
            ServiceRecommend.Manual => RecommendLevel.Suggested,
            ServiceRecommend.Auto => RecommendLevel.Suggested,
            _ => RecommendLevel.Optional,
        };
    }

    private static bool IsMustDisableService(string name) =>
        name.Equals("RemoteRegistry", StringComparison.OrdinalIgnoreCase)
        || name.Equals("Fax", StringComparison.OrdinalIgnoreCase)
        || name.Equals("WMPNetworkSvc", StringComparison.OrdinalIgnoreCase)
        || name.Equals("RemoteAccess", StringComparison.OrdinalIgnoreCase)
        || name.Equals("RetailDemo", StringComparison.OrdinalIgnoreCase)
        || name.Equals("MapsBroker", StringComparison.OrdinalIgnoreCase)
        || name.Equals("PhoneSvc", StringComparison.OrdinalIgnoreCase)
        || name.Equals("wisvc", StringComparison.OrdinalIgnoreCase)
        || name.Equals("XblAuthManager", StringComparison.OrdinalIgnoreCase)
        || name.Equals("XblGameSave", StringComparison.OrdinalIgnoreCase)
        || name.Equals("XboxNetApiSvc", StringComparison.OrdinalIgnoreCase)
        || name.Equals("XboxGipSvc", StringComparison.OrdinalIgnoreCase)
        || name.Equals("DiagTrack", StringComparison.OrdinalIgnoreCase)
        || name.Equals("WerSvc", StringComparison.OrdinalIgnoreCase);

    private static bool IsSecurityMitigationDisable(SettingHelpInfo help) =>
        ReferenceEquals(help, SettingCatalog.DisableMeltdownSpectre)
        || ReferenceEquals(help, SettingCatalog.DisableMemoryIntegrity)
        || ReferenceEquals(help, SettingCatalog.DisableWdac)
        || ReferenceEquals(help, SettingCatalog.DisableVbs)
        || ReferenceEquals(help, SettingCatalog.DisableSmartScreenWarning);

    /// <summary>产品明确要求「强烈推荐」的开关（覆盖目录基准与 OS 微调）。</summary>
    private static bool IsProductMust(SettingHelpInfo help) =>
        ReferenceEquals(help, SettingCatalog.DisableIeEsc)
        || ReferenceEquals(help, SettingCatalog.ShowThisPcIcon)
        || ReferenceEquals(help, SettingCatalog.EnableRdp)
        || ReferenceEquals(help, SettingCatalog.RdpGpuAccel)
        || ReferenceEquals(help, SettingCatalog.RdpHighRefresh)
        || ReferenceEquals(help, SettingCatalog.TaskbarClockWeekdaySeconds)
        || ReferenceEquals(help, SettingCatalog.RemoveAdminShield)
        || ReferenceEquals(help, SettingCatalog.NoShortcutSuffix)
        || ReferenceEquals(help, SettingCatalog.NoShortcutArrow)
        || ReferenceEquals(help, SettingCatalog.ContextMenuCopyMoveTo)
        || ReferenceEquals(help, SettingCatalog.ContextMenuQuickOps)
        || ReferenceEquals(help, SettingCatalog.TaskbarAllIcons)
        || ReferenceEquals(help, SettingCatalog.DisableSystemRestore)
        || ReferenceEquals(help, SettingCatalog.DisablePasswordComplexity)
        || ReferenceEquals(help, SettingCatalog.DisableCad)
        || ReferenceEquals(help, SettingCatalog.DisableShutdownReason)
        || ReferenceEquals(help, SettingCatalog.EnableDiskPerfCounters);

    private static bool IsAggressivePerf(SettingHelpInfo help) =>
        ReferenceEquals(help, SettingCatalog.EnableTcpBbr2)
        || ReferenceEquals(help, SettingCatalog.EnableTcpCtcp)
        || ReferenceEquals(help, SettingCatalog.DisableHpet)
        || ReferenceEquals(help, SettingCatalog.MergeSvchostProcesses)
        || ReferenceEquals(help, SettingCatalog.DisableMemoryCompression)
        || ReferenceEquals(help, SettingCatalog.DisablePageCombining)
        || ReferenceEquals(help, SettingCatalog.LargeSystemCacheOptimize)
        || ReferenceEquals(help, SettingCatalog.QosSpeedOptimize)
        || ReferenceEquals(help, SettingCatalog.TcpOptimized)
        || ReferenceEquals(help, SettingCatalog.OptimizeMultimediaScheduler);

    private static bool IsLegacyPerfTweak(SettingHelpInfo help) =>
        IsAggressivePerf(help)
        || ReferenceEquals(help, SettingCatalog.DisableAutoMaintenance)
        || ReferenceEquals(help, SettingCatalog.DisableReservedStorage)
        || ReferenceEquals(help, SettingCatalog.DisableSrvSplit);

    private static bool IsClientPrivacyBoost(SettingHelpInfo help) =>
        ReferenceEquals(help, SettingCatalog.DisableTelemetry)
        || ReferenceEquals(help, SettingCatalog.DisableCeip)
        || ReferenceEquals(help, SettingCatalog.DisableConsumerFeatures)
        || ReferenceEquals(help, SettingCatalog.DisableSilentAppInstall)
        || ReferenceEquals(help, SettingCatalog.DisableAdTracking)
        || ReferenceEquals(help, SettingCatalog.DisableSearchHighlights)
        || ReferenceEquals(help, SettingCatalog.DisableTips)
        || ReferenceEquals(help, SettingCatalog.DisableWidgets)
        || ReferenceEquals(help, SettingCatalog.HideTaskbarChat)
        || ReferenceEquals(help, SettingCatalog.DisableGameDvr)
        || ReferenceEquals(help, SettingCatalog.DisableAutoplay)
        || ReferenceEquals(help, SettingCatalog.ShowFileExtensions);

    private static RecommendLevel DemoteAggressive(RecommendLevel baseLevel, SystemFacts facts)
    {
        // 高级档：最多 Suggested；Server 2025 更严
        if (facts.Kind == WindowsOsKind.Server2025)
            return RecommendLevel.Optional;
        return Min(baseLevel, RecommendLevel.Suggested);
    }

    private static RecommendLevel Max(RecommendLevel a, RecommendLevel b) => a >= b ? a : b;
    private static RecommendLevel Min(RecommendLevel a, RecommendLevel b) => a <= b ? a : b;
}
