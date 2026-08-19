namespace WinOpt;

/// <summary>各优化项的帮助说明（作用、好处、指引、生效方式）。</summary>
internal static class SettingCatalog
{
    static readonly SettingScope S2012 = new(minServer: "2012 R2+");
    static readonly SettingScope S2016 = new(minServer: "2016+");
    static readonly SettingScope S2019 = new(minServer: "2019+");
    static readonly SettingScope W10 = new(minWindows: "Win10+");
    static readonly SettingScope W10De = new(minWindows: "Win10+", requiresDesktopExperience: true);
    static readonly SettingScope S2016W10 = new(minServer: "2016+", minWindows: "Win10+");
    static readonly SettingScope Rdp2019 = new(minServer: "2019+", minWindows: "Win10 1809+");
    static readonly SettingScope Arc2019 = new(minServer: "2019+", note: "已安装 Azure Arc 代理时有效；亦见于 Win10/11。");
    static readonly SettingScope StorageW10 = new(minWindows: "Win10 1703+", note: "Server 上存储感知能力有限，以客户端系统为主。");
    static readonly SettingScope Activity1803 = new(minWindows: "Win10 1803+", minServer: "2019+");
    static readonly SettingScope FastStartup = new(minWindows: "Win8+", requiresDesktopExperience: true, note: "Server 桌面极少依赖快速启动。");
    static readonly SettingScope LongPath = new(minServer: "2016+", minWindows: "Win10 1607+");
    static readonly SettingScope PowerThrottle = new(minServer: "2019+", minWindows: "Win10 1709+");
    static readonly SettingScope ShutdownTracker = new(note: "Server 默认开启关机事件跟踪；客户端为可选组件。");

    public static readonly SettingHelpInfo CpuProgramPriority = H(
        "让前台程序获得更多 CPU 时间片，桌面操作更跟手。",
        "调整 Win32PrioritySeparation，使 CPU 调度偏向交互式程序而非后台服务。",
        "Server 当桌面用时减少卡顿；适合开发、办公、远程桌面日常操作。",
        "推荐开启。若机器纯跑后台服务且不需本地交互，可保持关闭。",
        "立即生效，无需重启。",
        SettingScope.DesktopExperience);

    public static readonly SettingHelpInfo Dep = H(
        "为旧版程序启用数据执行保护，降低特定内存攻击风险。",
        "开启 DEP 对未标记为可执行的内存页进行保护（Server 常见为 OptOut 策略）。",
        "提高兼容性环境下的基础安全防护，对多数桌面软件无感。",
        "一般建议开启；若极个别老软件崩溃，可关闭后排查。",
        "立即生效。");

    public static readonly SettingHelpInfo DisableUac = H(
        "关闭 UAC 弹窗，安装/改系统时不再反复确认。",
        "将 EnableLUA 设为 0，降低用户账户控制拦截级别。",
        "个人桌面环境操作更顺畅，减少「是否允许」打断。",
        "仅建议在可信的个人/内网环境开启；企业或公网暴露环境请保持 UAC。",
        "立即生效；部分程序需重启后完全生效。");

    public static readonly SettingHelpInfo DisableIeEsc = H(
        "关闭 Server 默认的 IE 增强安全模式。",
        "取消 IE/旧版 Web 控件的 Enhanced Security Configuration 限制。",
        "本地浏览器、内网管理页、旧 OA 系统可正常访问，不必逐站加白名单。",
        "个人桌面推荐开启；面向公网的生产 Server 请谨慎。",
        "新开 IE/Edge IE 模式窗口后生效。",
        SettingScope.ServerExclusive);

    public static readonly SettingHelpInfo HighPerfPower = H(
        "切换为「高性能」电源计划，避免 CPU 降频。",
        "激活 GUID 为高性能的 powercfg 计划，减少节能节流。",
        "提升响应速度与 sustained 性能，适合常开远程桌面或跑负载。",
        "笔记本/需省电时可关闭恢复「平衡」；台式 Server 桌面建议开启。",
        "立即生效。");

    public static readonly SettingHelpInfo DisableTelemetry = H(
        "关闭 Windows 遥测与 DiagTrack 诊断服务。",
        "将 AllowTelemetry 设为 0 并禁用 Connected User Experiences 相关采集。",
        "减少后台上传与磁盘/网络占用，提升隐私。",
        "个人/内网 Server 桌面推荐开启；需参与 Windows 诊断计划则关闭。",
        "立即生效；DiagTrack 服务停止后生效。",
        S2016);

    public static readonly SettingHelpInfo NoUpdateReboot = H(
        "有用户登录时，更新完成后不强制自动重启。",
        "设置 NoAutoRebootWithLoggedOnUsers 策略。",
        "避免半夜或工作中被更新重启打断；适合长期在线的桌面 Server。",
        "仍建议在方便时手动重启完成更新；无人值守服务器可按需关闭。",
        "策略立即写入；下次更新周期生效。");

    public static readonly SettingHelpInfo DisableDeliveryOpt = H(
        "关闭更新 P2P 传递优化，不再对外/对内分发更新包。",
        "将 Delivery Optimization 下载模式设为仅本地/禁用 P2P。",
        "节省带宽与磁盘，避免成为他人更新的中继节点。",
        "单机或带宽有限环境推荐开启；多机内网共享更新可关闭。",
        "立即生效。",
        S2016W10);

    public static readonly SettingHelpInfo WuNotifyOnly = H(
        "Windows 更新仅通知下载，不自动安装。",
        "AUOptions 设为「通知下载」模式。",
        "由你决定何时安装更新，避免未经确认的重启与变更。",
        "需定期手动检查并安装安全更新；若希望全自动 patching 则关闭。",
        "策略立即写入；Windows Update 下次检查时生效。");

    public static readonly SettingHelpInfo DisableSysMain = H(
        "禁用 SysMain（原 Superfetch）超级预读服务。",
        "停止并禁用 SysMain 服务，减少 SSD 上不必要的预读。",
        "降低磁盘占用与后台 I/O，SSD/虚拟机环境更安静。",
        "机械硬盘且内存较小可保留开启；SSD 桌面 Server 推荐禁用。",
        "服务停止后立即生效。");

    public static readonly SettingHelpInfo VisualBestPerf = H(
        "关闭窗口动画、阴影等视觉效果，设为最佳性能。",
        "VisualFXSetting 设为性能优先，减少 DWM 合成开销。",
        "远程桌面与低配环境更流畅，降低 GPU/CPU 占用。",
        "若在意美观可关闭；远程办公或老硬件推荐开启。",
        "注销或重启资源管理器后完全生效。",
        SettingScope.DesktopExperience);

    public static readonly SettingHelpInfo PowerThrottlingOff = H(
        "关闭 Windows 对后台进程的 CPU 电源节流。",
        "PowerThrottlingOff 设为 1，减少后台任务被限速。",
        "编译、下载、同步等后台任务速度更稳定。",
        "笔记本省电场景可关闭；台式/常驻 Server 推荐开启。",
        "立即生效。",
        PowerThrottle);

    public static readonly SettingHelpInfo DisableHibernate = H(
        "关闭休眠并删除 hiberfil.sys，释放 C 盘空间。",
        "powercfg -h off 关闭休眠文件。",
        "通常可释放数 GB 磁盘；Server 桌面很少使用休眠。",
        "需要「休眠」快速恢复则不要开启；仅用睡眠/关机可开启。",
        "立即生效并删除休眠文件。",
        SettingScope.DesktopExperience);

    public static readonly SettingHelpInfo TcpOptimized = H(
        "调整 TCP 全局参数，对齐常见 Win10 桌面优化。",
        "设置 autotuninglevel、timestamps、ECN 等 netsh 参数。",
        "部分网络环境下降低延迟、提高吞吐稳定性。",
        "若遇特殊网络设备兼容问题可恢复默认；一般宽带/内网可开启。",
        "立即生效。");

    public static readonly SettingHelpInfo DisableErrorReport = H(
        "关闭 Windows 错误报告（WerSvc）上传。",
        "禁用 Windows Error Reporting 服务。",
        "崩溃时不再后台上传 dump，减少隐私与网络占用。",
        "需向微软提交崩溃诊断则保持关闭本项；个人桌面推荐开启。",
        "服务禁用后生效。");

    public static readonly SettingHelpInfo LongPathsEnabled = H(
        "允许路径与文件名超过 260 字符限制。",
        "启用 NTFS 长路径策略 LongPathsEnabled。",
        "开发工具、深层目录、npm/git 项目不再因路径过长失败。",
        "需配合应用程序支持长路径；现代开发环境强烈推荐。",
        "新启动的程序生效；部分旧程序需重启。",
        LongPath);

    public static readonly SettingHelpInfo DisableFastStartup = H(
        "关闭「快速启动」，关机改为完整关机。",
        "HiberbootEnabled 设为 0，避免混合关机。",
        "双系统、硬件变更、故障排查更可靠；部分驱动更新需完整关机。",
        "若追求最快开机且单系统可关闭；多系统/运维环境推荐开启。",
        "下次关机后生效。",
        FastStartup);

    public static readonly SettingHelpInfo DisableAutoMaintenance = H(
        "禁用系统自动维护计划任务。",
        "MaintenanceDisabled 设为 1，减少固定时段后台维护。",
        "避免维护窗口内磁盘/CPU 突增，适合 7×24 在线桌面。",
        "仍建议偶尔手动检查更新与磁盘；纯个人桌面可开启。",
        "下次维护周期起生效。");

    public static readonly SettingHelpInfo ExcludeDriverUpdates = H(
        "Windows 更新不包含驱动程序。",
        "ExcludeWUDriversInQualityUpdate 设为 1。",
        "避免驱动被自动更坏导致蓝屏/网卡失效；由厂商手动升级。",
        "新硬件需手动装驱动；稳定为主的环境推荐开启。",
        "下次 Windows Update 扫描起生效。",
        S2016W10);

    public static readonly SettingHelpInfo DisableSmb1 = H(
        "禁用 SMB 1.0 文件共享协议。",
        "禁用 mrxsmb10 并关闭 SMB1 服务器参数。",
        "封堵 WannaCry 等旧协议攻击面，符合现代安全基线。",
        "仅当需访问极老 NAS/设备时才保留 SMB1；否则务必开启。",
        "立即生效；访问 SMB1 设备将失败。");

    public static readonly SettingHelpInfo DisableRemoteRegistry = H(
        "禁用 Remote Registry 远程注册表服务。",
        "停止 RemoteRegistry 服务并设为禁用。",
        "减少远程篡改注册表的风险，符合安全加固惯例。",
        "需用远程注册表管理工具（regedit 连远程）时勿开启。",
        "服务停止后生效。");

    public static readonly SettingHelpInfo DisablePrintSpooler = H(
        "禁用 Print Spooler 打印后台服务。",
        "停止 Spooler 服务（无本地打印时可关）。",
        "减少攻击面与内存占用；PrintNightmare 类风险面更小。",
        "若需本地或网络打印必须关闭本项；无打印机强烈推荐开启。",
        "服务停止后无法打印。");

    public static readonly SettingHelpInfo ShowThisPcIcon = H(
        "在桌面显示「此电脑」图标。",
        "修改桌面图标隐藏列表，显示计算机 CLSID。",
        "快速进入磁盘分区，符合传统 Windows 桌面习惯。",
        "喜欢简洁桌面可关闭；运维/开发桌面推荐开启。",
        "立即生效或刷新桌面。",
        SettingScope.DesktopExperience);

    public static readonly SettingHelpInfo LaunchExplorerThisPc = H(
        "打开资源管理器时默认进入「此电脑」。",
        "LaunchTo 设为此电脑而非快速访问。",
        "直接看到所有驱动器，减少一点击路径。",
        "依赖快速访问/最近文件可关闭；传统习惯推荐开启。",
        "新开资源管理器窗口生效。",
        SettingScope.DesktopExperience);

    public static readonly SettingHelpInfo SmallTaskbar = H(
        "任务栏使用小图标按钮。",
        "TaskbarSmallIcons 设为 1。",
        "节省垂直空间，同屏显示更多任务栏图标。",
        "高 DPI 大屏若觉得太小可关闭；笔记本/1080p 推荐开启。",
        "立即生效。",
        SettingScope.DesktopExperience);

    public static readonly SettingHelpInfo ConfirmDelete = H(
        "删除文件时弹出确认对话框。",
        "ConfirmFileDelete 策略设为启用。",
        "防止误删；多一步确认更安全。",
        "熟练用户追求效率可关闭；公用或重要数据环境推荐开启。",
        "立即生效。",
        SettingScope.DesktopExperience);

    public static readonly SettingHelpInfo EnableAudio = H(
        "启动 Windows Audio 音频服务。",
        "AudioSrv 与 AudioEndpointBuilder 设为自动并启动。",
        "Server 桌面可正常播放系统声音、提示音与媒体。",
        "纯服务器无扬声器可关闭；当桌面用必须开启。",
        "服务启动后立即生效。",
        SettingScope.DesktopExperience);

    public static readonly SettingHelpInfo ShowFileExtensions = H(
        "显示已知文件类型的扩展名。",
        "HideFileExt 设为 0，显示 .txt .exe 等后缀。",
        "识别伪装恶意文件（如 virus.txt.exe），运维更安全。",
        "强烈建议开启；无特殊理由不应隐藏扩展名。",
        "立即生效。",
        SettingScope.DesktopExperience);

    public static readonly SettingHelpInfo EnableThemes = H(
        "启用 Themes 主题服务，完整 Aero/个性化外观。",
        "Themes 服务自动启动。",
        "窗口边框、壁纸、颜色正常；非「经典灰」界面。",
        "Server 当桌面几乎必选；极致省资源可关（界面变简陋）。",
        "服务启动后生效，必要时注销。",
        SettingScope.DesktopExperience);

    public static readonly SettingHelpInfo EnableSearch = H(
        "启用 Windows Search 索引服务。",
        "WSearch 服务自动启动。",
        "开始菜单与资源管理器搜索更快，支持内容索引。",
        "极弱配置或几乎不用搜索可关；日常使用推荐开启。",
        "索引建立需时；服务启动后生效。",
        SettingScope.DesktopExperience);

    public static readonly SettingHelpInfo DisableWebSearch = H(
        "开始菜单搜索仅本地，不查 Bing 网络。",
        "DisableWebSearch 与 BingSearchEnabled 关闭网络搜索。",
        "结果更干净、响应更快，无隐私外泄与广告。",
        "个人/内网 Server 桌面推荐开启。",
        "立即生效。",
        W10De);

    public static readonly SettingHelpInfo DisableFeedback = H(
        "关闭「向我们反馈」等体验调查弹窗。",
        "限制 SIUF 体验反馈提示频率。",
        "减少打断与后台联系 Microsoft 的提示。",
        "个人桌面推荐开启。",
        "立即生效。",
        W10De);

    public static readonly SettingHelpInfo NoLockScreen = H(
        "跳过锁屏界面，唤醒直接进入登录或桌面。",
        "NoLockScreen 策略设为启用。",
        "减少一次多余滑动/点击；个人物理安全可控时更顺手。",
        "笔记本公共场所或需锁屏广告/信息时勿开。",
        "策略生效后下次唤醒可见。",
        SettingScope.DesktopExperience);

    public static readonly SettingHelpInfo ShowHiddenFiles = H(
        "资源管理器默认显示隐藏文件与文件夹。",
        "Hidden 设为显示隐藏项。",
        "便于修改 AppData、系统配置；开发运维常见需求。",
        "新手若怕误删系统文件可暂不开启；熟练用户推荐。",
        "立即生效。",
        SettingScope.DesktopExperience);

    public static readonly SettingHelpInfo NoShortcutArrow = H(
        "桌面与资源管理器中快捷方式去掉小箭头 overlay。",
        "Shell Icons 29 置空，隐藏快捷方式箭头。",
        "桌面更整洁，与 macOS/部分美化习惯一致。",
        "需区分快捷方式与原件时可关闭；纯美观需求可开启。",
        "注销或重启资源管理器后生效。",
        SettingScope.DesktopExperience);

    public static readonly SettingHelpInfo ExplorerFullPath = H(
        "资源管理器窗口标题栏显示完整文件夹路径。",
        "FullPath 设为 1。",
        "复制路径、确认当前位置更方便，尤其适合深层目录。",
        "推荐开启；仅在意简洁标题可关闭。",
        "新开窗口生效。",
        SettingScope.DesktopExperience);

    public static readonly SettingHelpInfo TaskbarAllIcons = H(
        "通知区域始终显示全部托盘图标。",
        "EnableAutoTray 设为 0，不自动折叠到溢出区。",
        "网络、音量、后台工具一眼可见，减少「找不到图标」。",
        "任务栏拥挤时可关闭恢复自动隐藏。",
        "立即生效。",
        SettingScope.DesktopExperience);

    public static readonly SettingHelpInfo TaskbarClockWeekdaySeconds = H(
        "任务栏右下角时钟显示星期，时间精确到秒。",
        "ShowSecondsInSystemClock=1，并将短日期格式设为 yyyy/MM/dd dddd。",
        "一眼看到星期几与秒级时间，适合排班、日志对照与远程桌面。",
        "任务栏略宽；不需要时可关闭恢复系统默认格式。",
        "应用后自动重启资源管理器使托盘时钟立即刷新。",
        SettingScope.DesktopExperience);

    public static readonly SettingHelpInfo DisableAnimations = H(
        "关闭窗口最小化/任务栏等动画。",
        "MinAnimate、TaskbarAnimations 等设为关闭。",
        "远程桌面更跟手，低配置 CPU 负担更小。",
        "在意过渡效果可关闭；RDP 与性能优先推荐开启。",
        "注销或重启资源管理器后生效。",
        SettingScope.DesktopExperience);

    public static readonly SettingHelpInfo DisableTransparency = H(
        "关闭「透明效果」与亚克力模糊。",
        "EnableTransparency 设为 0。",
        "减少 GPU 合成，界面更「实色」；远程桌面带宽略省。",
        "喜欢 Win11 毛玻璃可关闭；低配/远程推荐开启。",
        "立即或注销后生效。",
        W10De);

    public static readonly SettingHelpInfo DisableTips = H(
        "关闭开始菜单/设置的提示、建议与赞助内容。",
        "关闭 ContentDeliveryManager 多项订阅提示。",
        "减少「你应该试试」类打扰与隐私追踪。",
        "个人桌面推荐开启。",
        "立即生效。",
        W10De);

    public static readonly SettingHelpInfo DisableAutoplay = H(
        "插入 U 盘/光盘不自动运行或弹窗。",
        "NoDriveTypeAutoRun 设为禁用所有驱动器自动播放。",
        "防止恶意 U 盘自动执行，安全基线项。",
        "几乎总是推荐开启；需自动播放安装盘时临时关闭。",
        "立即生效。");

    public static readonly SettingHelpInfo DisableActivityHistory = H(
        "禁用时间线/活动历史与跨设备同步。",
        "AllowPublishUserActivities 与 PublishUserActivities 关闭。",
        "减少隐私上传与后台记录；任务视图内容更少。",
        "个人/内网推荐开启；依赖时间线恢复工作流则关闭。",
        "立即生效。",
        Activity1803);

    public static readonly SettingHelpInfo DisableStorageSense = H(
        "关闭存储感知自动清理与临时文件策略。",
        "AllowStorageSenseGlobal 设为 0。",
        "避免后台自动删文件；Server 桌面常需手动掌控磁盘。",
        "磁盘紧张且信任自动清理可关闭；控台环境推荐开启。",
        "立即生效。",
        StorageW10);

    public static readonly SettingHelpInfo EnableRdp = H(
        "启用远程桌面（RDP）并接受连接。",
        "fDenyTSConnections 设为 0，并打开防火墙 RDP 规则。",
        "可从其他 PC/macOS/Linux 图形远程本机，Server 当桌面核心能力。",
        "不远程访问且要减攻击面时可关闭；需 RDP 必须开启。",
        "立即生效；防火墙规则同步应用。");

    public static readonly SettingHelpInfo RdpGpuAccel = H(
        "RDP 会话启用 GPU 硬件加速与更好的图形管线。",
        "Terminal Services UseAdvancedGraphics 策略。",
        "远程看网页、视频、UI 动画更流畅。",
        "无 GPU 或极老驱动可关；有显卡远程桌面推荐开启。",
        "新 RDP 连接生效。",
        Rdp2019);

    public static readonly SettingHelpInfo RdpHighRefresh = H(
        "提高远程桌面帧率上限（缩短 DWMFRAMEINTERVAL）。",
        "将帧间隔设为 15（约 60Hz 档）。",
        "鼠标移动、滚动、视频观感更顺滑。",
        "低带宽网络可能增带宽；内网/高带宽推荐开启。",
        "新 RDP 连接生效。",
        Rdp2019);

    public static readonly SettingHelpInfo RdpDisableNla = H(
        "RDP 不要求网络级身份验证（NLA）。",
        "UserAuthentication 设为 0，兼容旧客户端。",
        "部分 Linux 旧版 rdesktop、特殊跳板可连上。",
        "安全性降低，仅内网可信环境短期使用；能开 NLA 则勿开。",
        "新 RDP 连接生效。");

    public static readonly SettingHelpInfo EnableNetworkDiscovery = H(
        "启用网络发现与文件和打印机共享防火墙规则。",
        "启动 fdPHost/FDResPub 并放行相关防火墙组。",
        "局域网可见其他电脑、访问共享文件夹。",
        "不需要 SMB 共享或要最小暴露时关闭；家庭/ lab 局域网推荐开启。",
        "立即生效。");

    public static readonly SettingHelpInfo DisableSmRemoting = H(
        "关闭 Server Manager 远程管理（WinRM/SMRemoting）。",
        "禁用 Configure-SMRemoting 或 WinRM 服务。",
        "减少远程 PowerShell 管理入口，个人桌面通常用不到。",
        "需远程 Server Manager 管理多台 Server 时勿开。",
        "立即生效。",
        SettingScope.ServerExclusive);

    public static readonly SettingHelpInfo SkipServerManager = H(
        "登录后不自动弹出「服务器管理器」。",
        "DoNotOpenServerManagerAtLogon 等 Server Manager 策略。",
        "进桌面即干净，不必每次关管理器窗口。",
        "Server 当桌面强烈建议开启。",
        "下次登录生效。",
        new SettingScope(serverOnly: true, minServer: "2012 R2+"));

    public static readonly SettingHelpInfo DisableAzureArc = H(
        "禁止 Azure Arc 托盘程序开机自启。",
        "删除 Run 键中 AzureArcSetup 启动项。",
        "无 Azure 混合管理需求时不占托盘、不后台连接云。",
        "若已接入 Azure Arc 管理需关闭本项保留启动。",
        "下次登录生效。",
        Arc2019);

    public static readonly SettingHelpInfo EnableInstaller = H(
        "Windows Installer 服务设为自动。",
        "msiserver 自动启动，安装 .msi 软件无需手动启服务。",
        "正常安装 Office、工具软件不会报「Windows Installer 未启动」。",
        "极简 hardened 环境可关；桌面用途推荐开启。",
        "服务配置立即写入。");

    public static readonly SettingHelpInfo EnableWia = H(
        "启用 Windows Image Acquisition（扫描仪/部分摄像头）。",
        "stisvc 服务自动启动。",
        "扫描仪、部分旧摄像头可即插即用。",
        "无 imaging 设备可关；需扫描/摄像头则开启。",
        "服务启动后生效。",
        SettingScope.DesktopExperience);

    public static readonly SettingHelpInfo DisablePasswordComplexity = H(
        "本地账户密码不要求大小写+数字+符号组合。",
        "通过 secedit 将 PasswordComplexity 设为 0。",
        "可设简单 PIN 式密码，个人 VM/内网更方便。",
        "公网或合规环境必须保持复杂性；仅私人 lab 推荐。",
        "策略立即写入。");

    public static readonly SettingHelpInfo PasswordNeverExpire = H(
        "本地账户密码永不过期。",
        "MaximumPasswordAge 设为 0。",
        "不会 42 天强制改密码打断工作。",
        "有安全合规要求时勿开；个人单机推荐。",
        "策略立即写入。");

    public static readonly SettingHelpInfo ShutdownWithoutLogon = H(
        "登录界面允许直接关机（无需先登录）。",
        "ShutdownWithoutLogon 策略启用。",
        "物理机前可快速关机；虚拟机管理略方便。",
        "防他人恶意关机/物理安全场景可关闭。",
        "立即生效。");

    public static readonly SettingHelpInfo DisableShutdownReason = H(
        "关闭「关机原因」与 Shutdown Event Tracker 弹窗。",
        "ShutdownReasonOn/UI 设为关闭。",
        "关机/重启不再填原因问卷，个人桌面更省事。",
        "企业审计需要关机原因时勿开。",
        "立即生效。",
        ShutdownTracker);

    public static readonly SettingHelpInfo DisableCad = H(
        "登录时不要求按 Ctrl+Alt+Del 安全 attention。",
        "DisableCAD 设为 1，直接进入密码框。",
        "减少一步按键；远程桌面登录略快。",
        "降低防伪造登录界面能力；物理安全可控时可开。",
        "立即生效。");

    public static readonly SettingHelpInfo EnableAutologon = H(
        "开机后自动登录指定本地/域账户，无需输入密码。",
        "写入 Winlogon（AutoAdminLogon、DefaultUserName、DefaultDomainName），密码经 LsaStorePrivateData 存入 LSA，与 Sysinternals Autologon 相同。",
        "个人物理机、开发用 Server 桌面免输密码；重启/断电恢复后直达桌面。",
        "开启后点击工具栏「Autologon 配置」填写账户；应用推荐时写入。关闭开关并应用可禁用。",
        "下次重启后生效；启动时按住 Shift 可临时跳过自动登录。",
        SettingScope.DesktopExperience);

    static SettingHelpInfo H(
        string summary,
        string purpose,
        string benefit,
        string guide,
        string effect,
        SettingScope? scope = null) =>
        new(summary, purpose, benefit, guide, effect, scope);
}
