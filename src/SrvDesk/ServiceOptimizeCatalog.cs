namespace SrvDesk;

[Flags]
internal enum ServiceOsTarget
{
    None = 0,
    Win10 = 1,
    Win11 = 2,
    Server = 4,
    Client = Win10 | Win11,
    All = Win10 | Win11 | Server,
}

/// <summary>对本机场景的建议启动类型。</summary>
internal enum ServiceRecommend
{
    /// <summary>不主动改；关键或依赖多。</summary>
    Keep = 0,
    /// <summary>建议禁用（优化项）。</summary>
    Disable = 1,
    /// <summary>建议手动（按需）。</summary>
    Manual = 2,
    /// <summary>建议自动启动（如桌面体验音频/主题）。</summary>
    Auto = 3,
}

internal sealed class ServiceOptimizeEntry
{
    public string ServiceName { get; }
    public string TitleZh { get; }
    public string TitleEn { get; }
    public string CategoryZh { get; }
    public string CategoryEn { get; }
    public string NoteZh { get; }
    public string NoteEn { get; }
    public ServiceOsTarget Os { get; }
    public ServiceRecommend RecommendClient { get; }
    public ServiceRecommend RecommendServer { get; }
    /// <summary>该服务在各 OS 出厂/系统默认启动类型（未知则为 Unknown）。</summary>
    public ServiceStartTypeKind DefaultWin10 { get; }
    public ServiceStartTypeKind DefaultWin11 { get; }
    public ServiceStartTypeKind DefaultServer { get; }
    /// <summary>名称带随机后缀的用户服务前缀（如 OneSyncSvc_）。</summary>
    public bool IsPrefix { get; }

    public ServiceOptimizeEntry(
        string serviceName,
        string titleZh,
        string titleEn,
        string categoryZh,
        string categoryEn,
        ServiceOsTarget os,
        ServiceRecommend recommendClient,
        ServiceRecommend recommendServer,
        string noteZh,
        string noteEn,
        bool isPrefix = false,
        ServiceStartTypeKind defaultWin10 = ServiceStartTypeKind.Unknown,
        ServiceStartTypeKind defaultWin11 = ServiceStartTypeKind.Unknown,
        ServiceStartTypeKind defaultServer = ServiceStartTypeKind.Unknown)
    {
        ServiceName = serviceName;
        TitleZh = titleZh;
        TitleEn = titleEn;
        CategoryZh = categoryZh;
        CategoryEn = categoryEn;
        Os = os;
        RecommendClient = recommendClient;
        RecommendServer = recommendServer;
        NoteZh = noteZh;
        NoteEn = noteEn;
        IsPrefix = isPrefix;
        DefaultWin10 = defaultWin10;
        DefaultWin11 = defaultWin11;
        DefaultServer = defaultServer;
    }

    public string Title => AppLang.L(TitleZh, TitleEn);
    public string Category => AppLang.L(CategoryZh, CategoryEn);
    public string Note => AppLang.L(NoteZh, NoteEn);

    public ServiceRecommend Recommend(bool isServer) =>
        isServer ? RecommendServer : RecommendClient;

    public bool AppliesTo(ServiceOsTarget current) =>
        (Os & current) != 0;

    public ServiceStartTypeKind DefaultFor(ServiceOsTarget os) => os switch
    {
        ServiceOsTarget.Server => DefaultServer,
        ServiceOsTarget.Win11 => DefaultWin11,
        _ => DefaultWin10,
    };
}

/// <summary>常见可优化服务的建议目录；实际列表由本机动态枚举，目录仅用于匹配建议与系统默认。</summary>
internal static class ServiceOptimizeCatalog
{
    // Start: 2=Automatic 3=Manual 4=Disabled（与 ServiceStartTypeKind 一致）
    // 各 OS 出厂常见默认；未列出的显示为「-」
    private static readonly Dictionary<string, (byte W10, byte W11, byte Srv)> SystemDefaults =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["DiagTrack"] = (2, 2, 2),
            ["dmwappushservice"] = (2, 2, 4),
            ["DPS"] = (2, 2, 2),
            ["WdiServiceHost"] = (3, 3, 3),
            ["WdiSystemHost"] = (3, 3, 3),
            ["diagnosticshub.standardcollector.service"] = (3, 3, 3),
            ["WerSvc"] = (3, 3, 3),
            ["SysMain"] = (2, 2, 2),
            ["PcaSvc"] = (2, 2, 2),
            ["TrkWks"] = (2, 2, 2),
            ["DoSvc"] = (2, 2, 4),
            ["WSearch"] = (2, 2, 4),
            ["RemoteRegistry"] = (3, 3, 3),
            ["WinRM"] = (3, 3, 2),
            ["RemoteAccess"] = (4, 4, 3),
            ["Spooler"] = (2, 2, 2),
            ["PrintNotify"] = (3, 3, 3),
            ["Fax"] = (3, 3, 3),
            ["WMPNetworkSvc"] = (3, 3, 4),
            ["FrameServer"] = (3, 3, 4),
            ["XblAuthManager"] = (3, 3, 4),
            ["XblGameSave"] = (3, 3, 4),
            ["XboxNetApiSvc"] = (3, 3, 4),
            ["XboxGipSvc"] = (3, 3, 4),
            ["BcastDVRUserService"] = (3, 3, 4),
            ["GameInputSvc"] = (3, 2, 4),
            ["Themes"] = (2, 2, 2),
            ["AudioSrv"] = (2, 2, 2),
            ["AudioEndpointBuilder"] = (2, 2, 2),
            ["TabletInputService"] = (3, 3, 4),
            ["FontCache"] = (2, 2, 2),
            ["MapsBroker"] = (2, 2, 4),
            ["lfsvc"] = (3, 3, 4),
            ["WbioSrvc"] = (3, 3, 4),
            ["PhoneSvc"] = (3, 3, 4),
            ["MessagingService"] = (3, 3, 4),
            ["OneSyncSvc"] = (2, 2, 4),
            ["WpnService"] = (2, 2, 4),
            ["WpnUserService"] = (2, 2, 4),
            ["wisvc"] = (3, 3, 4),
            ["RetailDemo"] = (3, 3, 4),
            ["WalletService"] = (3, 3, 4),
            ["WpcMonSvc"] = (3, 3, 4),
            ["AJRouter"] = (3, 3, 4),
            ["SharedAccess"] = (4, 4, 4),
            ["icssvc"] = (3, 3, 4),
            ["SEMgrSvc"] = (3, 3, 4),
            ["SensorService"] = (3, 3, 4),
            ["SensrSvc"] = (3, 3, 4),
            ["StiSvc"] = (3, 3, 4),
            ["fdPHost"] = (3, 3, 3),
            ["FDResPub"] = (3, 3, 3),
            ["SSDPSRV"] = (3, 3, 4),
            ["upnphost"] = (3, 3, 4),
            ["lltdsvc"] = (3, 3, 4),
            ["TermService"] = (3, 3, 3),
            ["SessionEnv"] = (3, 3, 3),
            ["UmRdpService"] = (3, 3, 3),
            ["UALSVC"] = (2, 2, 2),
            ["SrmSvc"] = (2, 2, 2),
            ["W3SVC"] = (2, 2, 2),
            ["SMTPSVC"] = (2, 2, 2),
            ["FTPSVC"] = (2, 2, 2),
            ["Was"] = (3, 3, 3),
            ["Netlogon"] = (2, 2, 2),
            ["NTDS"] = (2, 2, 2),
            ["DNS"] = (2, 2, 2),
            ["DHCPServer"] = (2, 2, 2),
            ["LanmanServer"] = (2, 2, 2),
            ["LanmanWorkstation"] = (2, 2, 2),
            ["webthreatdefsvc"] = (3, 2, 4),
            ["webthreatdefusersvc"] = (3, 2, 4),
            ["ClipboardUserService"] = (3, 3, 4),
            ["wlidsvc"] = (3, 3, 4),
            ["ClipSVC"] = (3, 3, 4),
            ["InstallService"] = (3, 3, 4),
            ["PushToInstall"] = (3, 3, 4),
            ["CDPSvc"] = (2, 2, 4),
            ["CDPUserSvc"] = (2, 2, 4),
        };

    public static ServiceStartTypeKind SystemDefaultFor(string serviceName, ServiceOsTarget os)
    {
        if (string.IsNullOrWhiteSpace(serviceName)) return ServiceStartTypeKind.Unknown;
        if (TryLookupDefault(serviceName, os, out var d)) return d;
        var entry = Find(serviceName);
        if (entry is null) return ServiceStartTypeKind.Unknown;
        var fromEntry = entry.DefaultFor(os);
        if (fromEntry != ServiceStartTypeKind.Unknown) return fromEntry;
        return TryLookupDefault(entry.ServiceName, os, out d) ? d : ServiceStartTypeKind.Unknown;
    }

    private static bool TryLookupDefault(string name, ServiceOsTarget os, out ServiceStartTypeKind kind)
    {
        kind = ServiceStartTypeKind.Unknown;
        if (!SystemDefaults.TryGetValue(name, out var t)) return false;
        var code = os switch
        {
            ServiceOsTarget.Server => t.Srv,
            ServiceOsTarget.Win11 => t.W11,
            _ => t.W10,
        };
        if (code is < 0 or > 4) return false;
        kind = (ServiceStartTypeKind)code;
        return true;
    }

    public static IReadOnlyList<ServiceOptimizeEntry> All { get; } =
    [
        // —— 遥测 / 诊断 ——
        E("DiagTrack", "连接的用户体验和遥测", "Connected User Experiences and Telemetry",
            "遥测诊断", "Telemetry", ServiceOsTarget.All,
            ServiceRecommend.Disable, ServiceRecommend.Disable,
            "向微软上报诊断数据；服务器与精简桌面通常可禁。",
            "Sends diagnostic data to Microsoft; safe to disable on servers."),
        E("dmwappushservice", "设备管理 WAP 推送", "Device Management WAP Push",
            "遥测诊断", "Telemetry", ServiceOsTarget.Client,
            ServiceRecommend.Disable, ServiceRecommend.Disable,
            "推送路由相关；多数环境用不到。",
            "WAP push routing; rarely needed."),
        E("DPS", "诊断策略服务", "Diagnostic Policy Service",
            "遥测诊断", "Telemetry", ServiceOsTarget.All,
            ServiceRecommend.Disable, ServiceRecommend.Disable,
            "【应禁用】诊断策略，可禁",
            "Diagnostic policy; safe to disable when unused."),
        E("WdiServiceHost", "诊断服务主机", "Diagnostic Service Host",
            "遥测诊断", "Telemetry", ServiceOsTarget.All,
            ServiceRecommend.Disable, ServiceRecommend.Disable,
            "【应禁用】诊断主机，可禁", "Diagnostic host; disable with DPS."),
        E("WdiSystemHost", "诊断系统主机", "Diagnostic System Host",
            "遥测诊断", "Telemetry", ServiceOsTarget.All,
            ServiceRecommend.Disable, ServiceRecommend.Disable,
            "【应禁用】诊断系统主机，可禁", "Diagnostic system host; disable with DPS."),
        E("diagnosticshub.standardcollector.service", "诊断中心标准收集器", "Diagnostics Hub Standard Collector",
            "遥测诊断", "Telemetry", ServiceOsTarget.All,
            ServiceRecommend.Disable, ServiceRecommend.Disable,
            "【应禁用】诊断收集，可禁",
            "Diagnostics Hub collector; usually safe to disable."),
        E("WerSvc", "Windows 错误报告", "Windows Error Reporting",
            "遥测诊断", "Telemetry", ServiceOsTarget.All,
            ServiceRecommend.Disable, ServiceRecommend.Disable,
            "【应禁用】错误报告，可禁", "Error reporting; safe to disable."),

        // —— 性能 ——
        E("SysMain", "SysMain（超级抓取）", "SysMain (Superfetch)",
            "性能", "Performance", ServiceOsTarget.All,
            ServiceRecommend.Disable, ServiceRecommend.Disable,
            "【应禁用】超级抓取，建议禁",
            "Disable SysMain/Superfetch."),
        E("PcaSvc", "程序兼容性助手", "Program Compatibility Assistant",
            "性能", "Performance", ServiceOsTarget.All,
            ServiceRecommend.Disable, ServiceRecommend.Disable,
            "【应禁用】兼容性助手，可禁",
            "PCA; usually safe to disable."),
        E("TrkWks", "分布式链接跟踪客户端", "Distributed Link Tracking Client",
            "性能", "Performance", ServiceOsTarget.All,
            ServiceRecommend.Disable, ServiceRecommend.Disable,
            "【应禁用】NTFS 快捷方式跟踪，可禁",
            "Tracks NTFS shortcuts; usually safe to disable."),
        E("DoSvc", "传递优化", "Delivery Optimization",
            "性能", "Performance", ServiceOsTarget.Client,
            ServiceRecommend.Manual, ServiceRecommend.Disable,
            "【按需】P2P 更新分发，不用可手动",
            "P2P update delivery; set to manual if unused."),
        E("WSearch", "Windows Search", "Windows Search",
            "性能", "Performance", ServiceOsTarget.All,
            ServiceRecommend.Disable, ServiceRecommend.Disable,
            "【应禁用】占资源，可用 Everything",
            "Heavy; Everything can replace it."),

        // —— 安全相关（谨慎） ——
        E("RemoteRegistry", "远程注册表", "Remote Registry",
            "安全", "Security", ServiceOsTarget.All,
            ServiceRecommend.Disable, ServiceRecommend.Disable,
            "允许远程改注册表；默认应禁。",
            "Remote registry access; should be disabled."),
        E("WinRM", "Windows 远程管理", "Windows Remote Management",
            "安全", "Security", ServiceOsTarget.All,
            ServiceRecommend.Manual, ServiceRecommend.Keep,
            "PowerShell remoting；Server 管理常用，勿盲目禁。",
            "PowerShell remoting; keep on servers that manage remotely."),
        E("RemoteAccess", "路由和远程访问", "Routing and Remote Access",
            "安全", "Security", ServiceOsTarget.All,
            ServiceRecommend.Disable, ServiceRecommend.Manual,
            "RRAS；非路由角色可禁。", "RRAS; disable if not a router."),

        // —— 打印 / 传真 ——
        E("Spooler", "Print Spooler", "Print Spooler",
            "打印传真", "Print & fax", ServiceOsTarget.All,
            ServiceRecommend.Keep, ServiceRecommend.Manual,
            "打印必需；无打印机的服务器可手动/禁用。",
            "Required for printing; manual/disable if no printers."),
        E("PrintNotify", "打印通知", "Printer Extensions and Notifications",
            "打印传真", "Print & fax", ServiceOsTarget.All,
            ServiceRecommend.Manual, ServiceRecommend.Disable,
            "打印扩展通知。", "Printer toast notifications."),
        E("Fax", "传真", "Fax",
            "打印传真", "Print & fax", ServiceOsTarget.All,
            ServiceRecommend.Disable, ServiceRecommend.Disable,
            "传真调制解调器；几乎都可禁。", "Fax modem; almost always disable."),

        // —— 媒体 / 共享 ——
        E("WMPNetworkSvc", "Windows Media Player 网络共享", "Windows Media Player Network Sharing",
            "媒体", "Media", ServiceOsTarget.Client,
            ServiceRecommend.Disable, ServiceRecommend.Disable,
            "媒体库网络共享；可禁。", "WMP library sharing; disable."),
        E("FrameServer", "Windows Camera Frame Server", "Windows Camera Frame Server",
            "媒体", "Media", ServiceOsTarget.Client,
            ServiceRecommend.Manual, ServiceRecommend.Disable,
            "摄像头帧服务；无摄像头可手动。", "Camera frames; manual if unused."),

        // —— Xbox / 游戏（客户端） ——
        E("XblAuthManager", "Xbox Live 身份验证", "Xbox Live Auth Manager",
            "游戏 Xbox", "Gaming", ServiceOsTarget.Client,
            ServiceRecommend.Disable, ServiceRecommend.Disable,
            "Xbox 登录；不用 Xbox 可禁。", "Xbox sign-in; disable if unused."),
        E("XblGameSave", "Xbox Live 游戏保存", "Xbox Live Game Save",
            "游戏 Xbox", "Gaming", ServiceOsTarget.Client,
            ServiceRecommend.Disable, ServiceRecommend.Disable,
            "Xbox 云存档。", "Xbox cloud saves."),
        E("XboxNetApiSvc", "Xbox Live 网络服务", "Xbox Live Networking",
            "游戏 Xbox", "Gaming", ServiceOsTarget.Client,
            ServiceRecommend.Disable, ServiceRecommend.Disable,
            "Xbox 网络。", "Xbox networking."),
        E("XboxGipSvc", "Xbox Accessory Management", "Xbox Accessory Management",
            "游戏 Xbox", "Gaming", ServiceOsTarget.Client,
            ServiceRecommend.Disable, ServiceRecommend.Disable,
            "Xbox 配件。", "Xbox accessories."),
        E("BcastDVRUserService", "游戏栏录制（用户）", "Game DVR (user)",
            "游戏 Xbox", "Gaming", ServiceOsTarget.Client,
            ServiceRecommend.Disable, ServiceRecommend.Disable,
            "Xbox Game Bar 录制用户服务。", "Game Bar DVR user service.",
            isPrefix: true),
        E("GameInputSvc", "GameInput", "GameInput",
            "游戏 Xbox", "Gaming", ServiceOsTarget.Win11,
            ServiceRecommend.Manual, ServiceRecommend.Disable,
            "Win11 游戏输入栈；不用可手动。", "Win11 GameInput; manual if unused."),

        // —— 桌面体验 ——
        E("Themes", "Themes", "Themes",
            "桌面体验", "Desktop", ServiceOsTarget.All,
            ServiceRecommend.Auto, ServiceRecommend.Auto,
            "主题与视觉样式；有桌面体验时应自动。",
            "Themes; Auto when Desktop Experience is installed."),
        E("AudioSrv", "Windows Audio", "Windows Audio",
            "桌面体验", "Desktop", ServiceOsTarget.All,
            ServiceRecommend.Auto, ServiceRecommend.Auto,
            "音频服务；桌面/有声卡应自动。", "Audio; Auto on desktop."),
        E("AudioEndpointBuilder", "Windows Audio Endpoint Builder", "Windows Audio Endpoint Builder",
            "桌面体验", "Desktop", ServiceOsTarget.All,
            ServiceRecommend.Auto, ServiceRecommend.Auto,
            "音频终结点；随 AudioSrv。", "Audio endpoints; with AudioSrv."),
        E("TabletInputService", "触摸键盘和手写面板", "Touch Keyboard and Handwriting",
            "桌面体验", "Desktop", ServiceOsTarget.Client,
            ServiceRecommend.Manual, ServiceRecommend.Disable,
            "触摸/手写；台式机可手动。", "Touch/pen; manual on desktop PC."),
        E("FontCache", "Windows Font Cache", "Windows Font Cache Service",
            "桌面体验", "Desktop", ServiceOsTarget.All,
            ServiceRecommend.Auto, ServiceRecommend.Manual,
            "字体缓存；一般保持自动。", "Font cache; usually Auto."),

        // —— 消费端隐私 / 体验 ——
        E("MapsBroker", "下载的地图管理器", "Downloaded Maps Manager",
            "消费端", "Consumer", ServiceOsTarget.Client,
            ServiceRecommend.Disable, ServiceRecommend.Disable,
            "离线地图；可禁。", "Offline maps; disable."),
        E("lfsvc", "地理位置服务", "Geolocation Service",
            "消费端", "Consumer", ServiceOsTarget.Client,
            ServiceRecommend.Manual, ServiceRecommend.Disable,
            "定位；不需要可禁。", "Location; disable if unused."),
        E("WbioSrvc", "Windows Biometric Service", "Windows Biometric Service",
            "消费端", "Consumer", ServiceOsTarget.Client,
            ServiceRecommend.Manual, ServiceRecommend.Disable,
            "指纹/虹膜；无生物识别可手动。", "Biometrics; manual if unused."),
        E("PhoneSvc", "Phone Service", "Phone Service",
            "消费端", "Consumer", ServiceOsTarget.Client,
            ServiceRecommend.Disable, ServiceRecommend.Disable,
            "手机连接/通话相关。", "Phone linkage features."),
        E("MessagingService", "消息服务（用户）", "Messaging Service (user)",
            "消费端", "Consumer", ServiceOsTarget.Client,
            ServiceRecommend.Disable, ServiceRecommend.Disable,
            "短信/消息桥接用户服务。", "Messaging bridge user service.",
            isPrefix: true),
        E("OneSyncSvc", "同步主机（用户）", "Sync Host (user)",
            "消费端", "Consumer", ServiceOsTarget.Client,
            ServiceRecommend.Manual, ServiceRecommend.Disable,
            "邮件/账户同步用户服务。", "Mail/account sync user service.",
            isPrefix: true),
        E("WpnService", "Windows 推送通知系统服务", "Windows Push Notifications System Service",
            "消费端", "Consumer", ServiceOsTarget.Client,
            ServiceRecommend.Manual, ServiceRecommend.Disable,
            "系统推送；可手动。", "System push; manual if unused."),
        E("WpnUserService", "Windows 推送通知用户服务", "Windows Push Notifications User Service",
            "消费端", "Consumer", ServiceOsTarget.Client,
            ServiceRecommend.Manual, ServiceRecommend.Disable,
            "用户推送通知。", "User push notifications.",
            isPrefix: true),
        E("wisvc", "Windows Insider 服务", "Windows Insider Service",
            "消费端", "Consumer", ServiceOsTarget.Client,
            ServiceRecommend.Disable, ServiceRecommend.Disable,
            "预览体验成员；正式版可禁。", "Insider channel; disable on stable."),
        E("RetailDemo", "零售演示服务", "Retail Demo Service",
            "消费端", "Consumer", ServiceOsTarget.Client,
            ServiceRecommend.Disable, ServiceRecommend.Disable,
            "商店展机演示；务必禁。", "Store demo mode; disable."),
        E("WalletService", "WalletService", "WalletService",
            "消费端", "Consumer", ServiceOsTarget.Client,
            ServiceRecommend.Disable, ServiceRecommend.Disable,
            "钱包相关；多数可禁。", "Wallet features; usually disable."),
        E("WpcMonSvc", "家长控制", "Parental Controls",
            "消费端", "Consumer", ServiceOsTarget.Client,
            ServiceRecommend.Disable, ServiceRecommend.Disable,
            "家庭安全监控。", "Family Safety monitor."),
        E("AJRouter", "AllJoyn Router", "AllJoyn Router Service",
            "消费端", "Consumer", ServiceOsTarget.Client,
            ServiceRecommend.Disable, ServiceRecommend.Disable,
            "IoT AllJoyn；可禁。", "IoT AllJoyn; disable."),
        E("SharedAccess", "Internet Connection Sharing", "Internet Connection Sharing (ICS)",
            "消费端", "Consumer", ServiceOsTarget.All,
            ServiceRecommend.Disable, ServiceRecommend.Manual,
            "连接共享/热点；不用可禁。", "ICS/hotspot; disable if unused."),
        E("icssvc", "Windows Mobile 热点", "Windows Mobile Hotspot Service",
            "消费端", "Consumer", ServiceOsTarget.Client,
            ServiceRecommend.Disable, ServiceRecommend.Disable,
            "移动热点。", "Mobile hotspot."),
        E("SEMgrSvc", "付款和 NFC/SE 管理器", "Payments and NFC/SE Manager",
            "消费端", "Consumer", ServiceOsTarget.Client,
            ServiceRecommend.Disable, ServiceRecommend.Disable,
            "NFC 支付；无 NFC 可禁。", "NFC payments; disable without NFC."),
        E("SensorService", "传感器服务", "Sensor Service",
            "消费端", "Consumer", ServiceOsTarget.Client,
            ServiceRecommend.Manual, ServiceRecommend.Disable,
            "加速度计等；台式机可手动。", "Sensors; manual on desktop PC."),
        E("SensrSvc", "传感器监视服务", "Sensor Monitoring Service",
            "消费端", "Consumer", ServiceOsTarget.Client,
            ServiceRecommend.Disable, ServiceRecommend.Disable,
            "传感器监视。", "Sensor monitoring."),
        E("StiSvc", "Still Image Acquisition", "Windows Image Acquisition (WIA)",
            "消费端", "Consumer", ServiceOsTarget.Client,
            ServiceRecommend.Manual, ServiceRecommend.Disable,
            "扫描仪/相机导入。", "Scanner/camera import."),

        // —— 网络发现 ——
        E("fdPHost", "Function Discovery Provider Host", "Function Discovery Provider Host",
            "网络发现", "Discovery", ServiceOsTarget.All,
            ServiceRecommend.Manual, ServiceRecommend.Manual,
            "网络发现依赖之一。", "Needed for network discovery."),
        E("FDResPub", "Function Discovery Resource Publication", "Function Discovery Resource Publication",
            "网络发现", "Discovery", ServiceOsTarget.All,
            ServiceRecommend.Manual, ServiceRecommend.Manual,
            "发布本机资源到网络。", "Publishes this PC on the LAN."),
        E("SSDPSRV", "SSDP Discovery", "SSDP Discovery",
            "网络发现", "Discovery", ServiceOsTarget.All,
            ServiceRecommend.Manual, ServiceRecommend.Disable,
            "UPnP 发现；服务器常禁。", "UPnP discovery; often disable on servers."),
        E("upnphost", "UPnP Device Host", "UPnP Device Host",
            "网络发现", "Discovery", ServiceOsTarget.All,
            ServiceRecommend.Manual, ServiceRecommend.Disable,
            "UPnP 设备主机。", "UPnP device host."),
        E("lltdsvc", "Link-Layer Topology Discovery Mapper", "Link-Layer Topology Discovery Mapper",
            "网络发现", "Discovery", ServiceOsTarget.All,
            ServiceRecommend.Manual, ServiceRecommend.Disable,
            "网络映射图。", "Network map."),

        // —— 远程桌面 ——
        E("TermService", "Remote Desktop Services", "Remote Desktop Services",
            "远程桌面", "Remote Desktop", ServiceOsTarget.All,
            ServiceRecommend.Keep, ServiceRecommend.Keep,
            "RDP 核心；需要远程桌面时勿禁。", "RDP core; do not disable if you use RDP."),
        E("SessionEnv", "Remote Desktop Configuration", "Remote Desktop Configuration",
            "远程桌面", "Remote Desktop", ServiceOsTarget.All,
            ServiceRecommend.Keep, ServiceRecommend.Keep,
            "RDP 配置。", "RDP configuration."),
        E("UmRdpService", "Remote Desktop Services UserMode Port Redirector", "RDP UserMode Port Redirector",
            "远程桌面", "Remote Desktop", ServiceOsTarget.All,
            ServiceRecommend.Keep, ServiceRecommend.Keep,
            "RDP 端口重定向。", "RDP port redirector."),

        // —— Server 特有 / 常用 ——
        E("UALSVC", "User Access Logging Service", "User Access Logging Service",
            "Server", "Server", ServiceOsTarget.Server,
            ServiceRecommend.Keep, ServiceRecommend.Manual,
            "Server 用户访问日志；精简环境可手动。",
            "Server access logging; manual on lean hosts."),
        E("SrmSvc", "File Server Resource Manager", "File Server Resource Manager",
            "Server", "Server", ServiceOsTarget.Server,
            ServiceRecommend.Keep, ServiceRecommend.Manual,
            "文件服务器资源管理；未装角色可无。",
            "FSRM; only if the role is installed."),
        E("W3SVC", "World Wide Web Publishing Service", "World Wide Web Publishing Service",
            "Server", "Server", ServiceOsTarget.Server,
            ServiceRecommend.Keep, ServiceRecommend.Keep,
            "IIS；未托管网站勿启。未安装则不显示。",
            "IIS; keep off unless hosting sites."),
        E("SMTPSVC", "Simple Mail Transfer Protocol", "Simple Mail Transfer Protocol (SMTP)",
            "Server", "Server", ServiceOsTarget.Server,
            ServiceRecommend.Keep, ServiceRecommend.Disable,
            "旧版 SMTP；多数应禁。", "Legacy SMTP; usually disable."),
        E("FTPSVC", "Microsoft FTP Service", "Microsoft FTP Service",
            "Server", "Server", ServiceOsTarget.Server,
            ServiceRecommend.Keep, ServiceRecommend.Manual,
            "IIS FTP；未用可禁。", "IIS FTP; disable if unused."),
        E("Was", "Windows Process Activation Service", "Windows Process Activation Service",
            "Server", "Server", ServiceOsTarget.Server,
            ServiceRecommend.Keep, ServiceRecommend.Keep,
            "IIS/WAS 依赖。", "IIS/WAS dependency."),
        E("Netlogon", "Netlogon", "Netlogon",
            "Server", "Server", ServiceOsTarget.Server,
            ServiceRecommend.Keep, ServiceRecommend.Keep,
            "域成员/DC 关键；勿在域环境禁。",
            "Critical for domain; do not disable on domain-joined hosts."),
        E("NTDS", "Active Directory Domain Services", "Active Directory Domain Services",
            "Server", "Server", ServiceOsTarget.Server,
            ServiceRecommend.Keep, ServiceRecommend.Keep,
            "仅域控制器；DC 上必须保持。",
            "Domain Controllers only; must stay running on DCs."),
        E("DNS", "DNS Server", "DNS Server",
            "Server", "Server", ServiceOsTarget.Server,
            ServiceRecommend.Keep, ServiceRecommend.Keep,
            "DNS 服务器角色。", "DNS Server role."),
        E("DHCPServer", "DHCP Server", "DHCP Server",
            "Server", "Server", ServiceOsTarget.Server,
            ServiceRecommend.Keep, ServiceRecommend.Keep,
            "DHCP 服务器角色。", "DHCP Server role."),
        E("LanmanServer", "Server（文件共享）", "Server (file sharing)",
            "Server", "Server", ServiceOsTarget.All,
            ServiceRecommend.Keep, ServiceRecommend.Keep,
            "SMB 文件共享；关共享前勿禁。",
            "SMB file sharing; keep if you share folders."),
        E("LanmanWorkstation", "Workstation", "Workstation",
            "Server", "Server", ServiceOsTarget.All,
            ServiceRecommend.Keep, ServiceRecommend.Keep,
            "访问网络共享必需。", "Required to access network shares."),

        // —— Win11 相关 ——
        E("webthreatdefsvc", "Web Threat Defense Service", "Web Threat Defense Service",
            "Win11", "Win11", ServiceOsTarget.Win11,
            ServiceRecommend.Keep, ServiceRecommend.Keep,
            "Smart App Control / Web 威胁防护相关。",
            "Related to web threat defense / SAC."),
        E("webthreatdefusersvc", "Web Threat Defense User Service", "Web Threat Defense User Service",
            "Win11", "Win11", ServiceOsTarget.Win11,
            ServiceRecommend.Keep, ServiceRecommend.Keep,
            "Web 威胁防护用户服务。", "Web threat defense user service.",
            isPrefix: true),
        E("ClipboardUserService", "剪贴板用户服务", "Clipboard User Service",
            "Win11", "Win11", ServiceOsTarget.Win11,
            ServiceRecommend.Manual, ServiceRecommend.Disable,
            "跨设备剪贴板；可手动。", "Cloud clipboard; manual if unused.",
            isPrefix: true),

        // —— Microsoft 账户 / Store（谨慎） ——
        E("wlidsvc", "Microsoft Account Sign-in Assistant", "Microsoft Account Sign-in Assistant",
            "账户商店", "Account & Store", ServiceOsTarget.Client,
            ServiceRecommend.Keep, ServiceRecommend.Manual,
            "微软账户登录；用 Store/OneDrive 勿禁。",
            "MSA sign-in; keep if using Store/OneDrive."),
        E("ClipSVC", "Client License Service (ClipSVC)", "Client License Service (ClipSVC)",
            "账户商店", "Account & Store", ServiceOsTarget.Client,
            ServiceRecommend.Keep, ServiceRecommend.Manual,
            "商店许可；用 Store 应用勿禁。",
            "Store licensing; keep for Store apps."),
        E("InstallService", "Microsoft Store Install Service", "Microsoft Store Install Service",
            "账户商店", "Account & Store", ServiceOsTarget.Client,
            ServiceRecommend.Manual, ServiceRecommend.Disable,
            "商店安装服务。", "Store install service."),
        E("PushToInstall", "Windows PushToInstall", "Windows PushToInstall Service",
            "账户商店", "Account & Store", ServiceOsTarget.Client,
            ServiceRecommend.Disable, ServiceRecommend.Disable,
            "远程推送安装商店应用。", "Remote Store app push-install."),
        E("CDPSvc", "Connected Devices Platform Service", "Connected Devices Platform Service",
            "账户商店", "Account & Store", ServiceOsTarget.Client,
            ServiceRecommend.Manual, ServiceRecommend.Disable,
            "近距离共享/连接设备。", "Nearby sharing / connected devices."),
        E("CDPUserSvc", "Connected Devices Platform User Service", "Connected Devices Platform User Service",
            "账户商店", "Account & Store", ServiceOsTarget.Client,
            ServiceRecommend.Manual, ServiceRecommend.Disable,
            "连接设备用户服务。", "CDP user service.",
            isPrefix: true),
    ];

    /// <summary>按服务名匹配目录项（含前缀型用户服务）。</summary>
    public static ServiceOptimizeEntry? Find(string serviceName)
    {
        if (string.IsNullOrWhiteSpace(serviceName)) return null;
        ServiceOptimizeEntry? prefixHit = null;
        foreach (var e in All)
        {
            if (e.IsPrefix)
            {
                if (serviceName.Equals(e.ServiceName, StringComparison.OrdinalIgnoreCase)
                    || serviceName.StartsWith(e.ServiceName + "_", StringComparison.OrdinalIgnoreCase))
                    prefixHit ??= e;
                continue;
            }

            if (serviceName.Equals(e.ServiceName, StringComparison.OrdinalIgnoreCase))
                return e;
        }
        return prefixHit;
    }

    private static ServiceOptimizeEntry E(
        string name, string titleZh, string titleEn,
        string catZh, string catEn, ServiceOsTarget os,
        ServiceRecommend client, ServiceRecommend server,
        string noteZh, string noteEn, bool isPrefix = false) =>
        new(name, titleZh, titleEn, catZh, catEn, os, client, server, noteZh, noteEn, isPrefix);
}
