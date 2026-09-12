namespace SrvDesk;

/// <summary>各优化项的帮助说明（作用、好处、指引、生效方式）。</summary>
internal static class SettingCatalog
{
    static string L(string zh, string en) => AppLang.L(zh, en);

    static readonly SettingScope S2012 = new(minServer: "2012 R2+");
    static readonly SettingScope S2016 = new(minServer: "2016+");
    static readonly SettingScope S2019 = new(minServer: "2019+");
    static readonly SettingScope W10 = new(minWindows: "Win10+");
    static readonly SettingScope W10De = new(minWindows: "Win10+", requiresDesktopExperience: true);
    static readonly SettingScope S2016W10 = new(minServer: "2016+", minWindows: "Win10+");
    static readonly SettingScope Rdp2019 = new(minServer: "2019+", minWindows: "Win10 1809+");
    static readonly SettingScope Arc2019 = new(minServer: "2019+", note: L("已安装 Azure Arc 代理时有效；亦见于 Win10/11。", "Effective when Azure Arc agent is installed; also on Win10/11."));
    static readonly SettingScope StorageW10 = new(minWindows: "Win10 1703+", note: L("Server 上存储感知能力有限，以客户端系统为主。", "Limited Storage Sense on Server; mainly for client OS."));
    static readonly SettingScope Activity1803 = new(minWindows: "Win10 1803+", minServer: "2019+");
    static readonly SettingScope FastStartup = new(minWindows: "Win8+", requiresDesktopExperience: true, note: L("Server 桌面极少依赖快速启动。", "Server desktops rarely need Fast Startup."));
    static readonly SettingScope LongPath = new(minServer: "2016+", minWindows: "Win10 1607+");
    static readonly SettingScope PowerThrottle = new(minServer: "2019+", minWindows: "Win10 1709+");
    static readonly SettingScope ShutdownTracker = new(note: L("Server 默认开启关机事件跟踪；客户端为可选组件。", "Server enables Shutdown Event Tracker by default; optional on clients."));

    public static readonly SettingHelpInfo CpuProgramPriority = H(
        L("让前台程序获得更多 CPU 时间片，桌面操作更跟手。", "Give foreground apps more CPU time so the desktop feels snappier."),
        L("调整 Win32PrioritySeparation，使 CPU 调度偏向交互式程序而非后台服务。", "Tune Win32PrioritySeparation so CPU scheduling favors interactive apps over background services."),
        L("Server 当桌面用时减少卡顿；适合开发、办公、远程桌面日常操作。", "Less stutter when Server is used as a desktop; good for coding, office, and daily RDP."),
        L("强烈推荐开启（程序优先）。若机器纯跑后台服务且不需本地交互，可保持关闭。", "Strongly recommended on (programs first). Keep off if the machine only runs background services with no local UI."),
        L("立即生效，不必重启。", "Takes effect immediately; no reboot needed."),
        SettingScope.DesktopExperience,
        recommend: RecommendLevel.Must);

    public static readonly SettingHelpInfo Dep = H(
        L("为旧版程序启用数据执行保护，降低特定内存攻击风险。", "Enable DEP for legacy apps to reduce certain memory-attack risks."),
        L("开启 DEP 对未标记为可执行的内存页进行保护（Server 常见为 OptOut 策略）。", "Turn on DEP for non-executable memory pages (common Server OptOut policy)."),
        L("提高兼容性环境下的基础安全防护，对多数桌面软件无感。", "Stronger baseline security in compatibility mode; invisible to most desktop apps."),
        L("一般可开；若极个别老软件崩溃，可关闭后排查。", "Usually safe on; turn off only if a rare legacy app crashes."),
        L("立即生效。", "Takes effect immediately."));

    public static readonly SettingHelpInfo DisableUac = H(
        L("将用户账户控制滑块设为「从不通知」，安装/改系统时不再弹确认框。", "Set UAC to Never notify so installs/system changes skip prompts."),
        L("ConsentPromptBehaviorAdmin=0、PromptOnSecureDesktop=0（与常见「从不通知」.reg 一致；不关闭 EnableLUA）。", "ConsentPromptBehaviorAdmin=0, PromptOnSecureDesktop=0 (same as common Never-notify .reg; EnableLUA stays on)."),
        L("个人桌面减少 UAC 打断；比直接 EnableLUA=0 更接近系统自带滑块行为。", "Fewer UAC interruptions on personal desktops; closer to the built-in slider than EnableLUA=0."),
        L("仅建议在可信的个人/内网环境开启；企业或公网暴露环境请保持默认通知。", "Only for trusted personal/LAN use; keep default prompts on enterprise or public-facing hosts."),
        L("立即生效；个别程序建议注销或重启后再试。", "Takes effect immediately; some apps may need logoff/reboot."),
        recommend: RecommendLevel.Optional,
        uiPlace: L("控制面板 → 用户账户 → 更改用户账户控制设置", "Control Panel → User Accounts → Change User Account Control settings"),
        whenHint: L("个人桌面可开；公网/域环境慎用", "OK for personal desktop; caution on public/domain hosts"));

    public static readonly SettingHelpInfo DisableIeEsc = H(
        L("关闭 Server 默认的 IE 增强安全模式。", "Turn off IE Enhanced Security Configuration on Server."),
        L("取消 IE/旧版 Web 控件的 Enhanced Security Configuration 限制。", "Remove Enhanced Security Configuration limits for IE/legacy Web controls."),
        L("本地浏览器、内网管理页、旧 OA 系统可正常访问，不必逐站加白名单。", "Local browsers, intranet admin pages, and legacy OA sites work without per-site allowlists."),
        L("个人桌面可开；公网生产 Server 请谨慎。", "OK for personal desktop; be careful on public production Servers."),
        L("新开 IE/Edge IE 模式窗口后生效。", "Takes effect in new IE / Edge IE-mode windows."),
        SettingScope.ServerExclusive,
        uiPlace: L("服务器管理器→本地服务器→IE 增强安全配置", "Server Manager → Local Server → IE Enhanced Security Configuration"),
        whenHint: L("当桌面用建议开", "Recommended when used as a desktop"),
        recommend: RecommendLevel.Must);

    public static readonly SettingHelpInfo HighPerfPower = H(
        L("切换电源计划：平衡 / 高性能 / 卓越性能。", "Switch power plan: Balanced / High performance / Ultimate Performance."),
        L("powercfg /setactive；卓越性能会先 duplicatescheme 内置 GUID。", "powercfg /setactive; Ultimate may duplicatescheme the built-in GUID first."),
        L("高性能减少节能节流；卓越性能更激进，适合台式常开负载。", "High perf reduces throttling; Ultimate is more aggressive for always-on desktops."),
        L("台式 Server 桌面建议「高性能」；笔记本/省电选「平衡」；「卓越性能」不推荐笔记本。", "Desktop Servers: High performance; laptops: Balanced; Ultimate not for notebooks."),
        L("立即生效。", "Takes effect immediately."),
        recommend: RecommendLevel.Strong);

    public static readonly SettingHelpInfo DisableTelemetry = H(
        L("关闭 Windows 遥测与 DiagTrack 诊断服务。", "Disable Windows telemetry and the DiagTrack diagnostic service."),
        L("将 AllowTelemetry 设为 0 并禁用 Connected User Experiences 相关采集。", "Set AllowTelemetry to 0 and disable Connected User Experiences collection."),
        L("减少后台上传与磁盘/网络占用。", "Less background upload and disk/network use."),
        L("个人/内网 Server 桌面推荐开启；需参与 Windows 诊断计划则关闭。", "Recommended for personal/LAN Server desktops; keep off if you join Windows diagnostics."),
        L("立即生效；DiagTrack 服务停止后生效。", "Takes effect immediately after DiagTrack stops."),
        S2016,
        recommend: RecommendLevel.Strong);

    public static readonly SettingHelpInfo NoUpdateReboot = H(
        L("有用户登录时，更新完成后不强制自动重启。", "Do not force reboot after updates while a user is logged on."),
        L("设置 NoAutoRebootWithLoggedOnUsers 策略。", "Set the NoAutoRebootWithLoggedOnUsers policy."),
        L("避免半夜或工作中被更新重启打断；适合长期在线的桌面 Server。", "Avoid mid-work or overnight update reboots; good for always-on desktop Servers."),
        L("仍建议在方便时手动重启完成更新；无人值守服务器可按需关闭。", "Still reboot manually when convenient; unattended servers may leave this off."),
        L("策略立即写入；下次更新周期生效。", "Policy is written immediately; applies on the next update cycle."),
        recommend: RecommendLevel.Strong);

    public static readonly SettingHelpInfo DisableDeliveryOpt = H(
        L("关闭更新 P2P 传递优化，不再对外/对内分发更新包。", "Disable Delivery Optimization P2P so this PC does not share updates."),
        L("将 Delivery Optimization 下载模式设为仅本地/禁用 P2P。", "Set Delivery Optimization download mode to local-only / no P2P."),
        L("节省带宽与磁盘，避免成为他人更新的中继节点。", "Saves bandwidth/disk and avoids acting as an update relay."),
        L("单机或带宽有限环境推荐开启；多机内网共享更新可关闭。", "Recommended on single PCs or limited bandwidth; leave off for LAN update sharing."),
        L("立即生效。", "Takes effect immediately."),
        S2016W10);

    public static readonly SettingHelpInfo WuNotifyOnly = H(
        L("Windows 更新仅通知下载，不自动安装。", "Windows Update notifies for download only; does not auto-install."),
        L("AUOptions 设为「通知下载」模式。", "Set AUOptions to notify-for-download mode."),
        L("由你决定何时安装更新，避免未经确认的重启与变更。", "You choose when to install; avoids unconfirmed reboots and changes."),
        L("需定期手动检查并安装安全更新；若希望全自动 patching 则关闭。", "Check and install security updates regularly; turn off for fully automatic patching."),
        L("策略立即写入；Windows Update 下次检查时生效。", "Policy is written immediately; applies on the next Windows Update check."));

    public static readonly SettingHelpInfo DisableSysMain = H(
        L("禁用 SysMain（原 Superfetch）超级预读服务。", "Disable the SysMain (formerly Superfetch) prefetch service."),
        L("停止并禁用 SysMain 服务，减少 SSD 上不必要的预读。", "Stop and disable SysMain to cut unnecessary SSD prefetch."),
        L("降低磁盘占用与后台 I/O，SSD/虚拟机环境更安静。", "Lower disk use and background I/O; quieter on SSD/VMs."),
        L("机械硬盘且内存较小可保留；虚拟机可关；物理 SSD 个人用途按需（高级）。",
            "Keep on small-RAM HDDs; OK off on VMs; optional on physical SSD desktops."),
        L("服务停止后立即生效。", "Takes effect as soon as the service stops."),
        recommend: RecommendLevel.Suggested);

    public static readonly SettingHelpInfo VisualBestPerf = H(
        L("关闭窗口动画、阴影等视觉效果，设为最佳性能。", "Turn off window animations/shadows and set visual effects for best performance."),
        L("VisualFXSetting 设为性能优先，减少 DWM 合成开销。", "Set VisualFXSetting to performance to reduce DWM composition cost."),
        L("远程桌面与低配环境更流畅，降低 GPU/CPU 占用。", "Smoother on RDP and low-end hardware; less GPU/CPU use."),
        L("若在意美观可关闭；远程办公或老硬件推荐开启。", "Turn off if you prefer looks; recommended for RDP or older hardware."),
        L("注销或重启资源管理器后完全生效。", "Fully applies after logoff or Explorer restart."),
        SettingScope.DesktopExperience);

    public static readonly SettingHelpInfo PowerThrottlingOff = H(
        L("关闭 Windows 对后台进程的 CPU 电源节流。", "Disable Windows CPU power throttling for background processes."),
        L("PowerThrottlingOff 设为 1，减少后台任务被限速。", "Set PowerThrottlingOff=1 so background tasks are less throttled."),
        L("编译、下载、同步等后台任务速度更稳定。", "More stable speed for compile/download/sync background work."),
        L("笔记本省电场景可关闭；台式/常驻 Server 推荐开启。", "Leave off on battery-saving laptops; recommended on desktops/always-on Servers."),
        L("立即生效。", "Takes effect immediately."),
        PowerThrottle);

    public static readonly SettingHelpInfo ShowProcessorBoostMode = H(
        L("在电源选项「高级设置」中显示「处理器性能提升模式」。", "Show Processor performance boost mode in Power Options advanced settings."),
        L("PERFBOOSTMODE 的 Attributes=2（取消隐藏）；关闭时改回 1。", "PERFBOOSTMODE Attributes=2 (unhide); set back to 1 when off."),
        L("可手动选择 Disabled / Enabled / Aggressive 等 Turbo Boost 策略。", "Lets you pick Disabled / Enabled / Aggressive Turbo Boost policies."),
        L("仅显示设置项，不改当前提升模式数值；改数值请到电源计划高级选项。", "Only unhides the setting; change the value in the power plan advanced options."),
        L("立即生效；重新打开「电源选项 → 更改高级电源设置」可见。", "Takes effect immediately; reopen Power Options → Change advanced power settings to see it."),
        uiPlace: L("控制面板 → 电源选项 → 更改计划设置 → 更改高级电源设置 → 处理器电源管理", "Control Panel → Power Options → Change plan settings → Change advanced power settings → Processor power management"));

    public static readonly SettingHelpInfo DisableHibernate = H(
        L("关闭休眠并删除 hiberfil.sys，释放 C 盘空间。", "Disable hibernation and delete hiberfil.sys to free C: space."),
        L("powercfg -h off 关闭休眠文件。", "powercfg -h off removes the hibernation file."),
        L("通常可释放数 GB 磁盘；Server 桌面很少使用休眠。", "Often frees several GB; Server desktops rarely use hibernate."),
        L("需要「休眠」快速恢复则不要开启；仅用睡眠/关机可开启。", "Do not enable if you need Hibernate resume; OK if you only sleep/shut down."),
        L("立即生效并删除休眠文件。", "Takes effect immediately and deletes the hibernation file."),
        SettingScope.DesktopExperience);

    public static readonly SettingHelpInfo NeverSleepOrScreenOff = H(
        L("关闭屏幕超时、睡眠与休眠超时（插电/电池均为永不）。", "Never turn off display / sleep / hibernate (AC and DC)."),
        L("powercfg -change：monitor / standby / hibernate timeout 的 AC、DC 均设为 0。", "powercfg -change: monitor/standby/hibernate timeouts AC+DC set to 0."),
        L("远程桌面与常开机器不会因闲置关屏或睡眠；任务不断线。", "RDP / always-on hosts won't blank or sleep on idle."),
        L("台式/Server 桌面推荐开启；笔记本需省电时请关闭。", "Recommended on desktop/Server; leave off on laptops that need battery life."),
        L("立即写入当前电源方案。", "Writes into the active power scheme immediately."),
        recommend: RecommendLevel.Strong);

    public static readonly SettingHelpInfo EnableDiskPerfCounters = H(
        L("开启磁盘性能计数器，任务管理器「性能」可显示硬盘。", "Enable disk performance counters so Task Manager shows Disk."),
        L("diskperf -y（PartMgr\\EnableCounterForIoctl=1）。", "diskperf -y (PartMgr\\EnableCounterForIoctl=1)."),
        L("任务管理器可看磁盘占用，便于排查 IO 卡顿。", "See disk usage in Task Manager to spot IO bottlenecks."),
        L("强烈推荐开启；几乎无副作用。", "Strongly recommended; virtually no downside."),
        L("立即生效；已打开的任务管理器需关闭后重开。", "Takes effect immediately; reopen Task Manager if already open."),
        recommend: RecommendLevel.Must);

    public static readonly SettingHelpInfo TcpOptimized = H(
        L("调整 TCP 全局参数，对齐常见 Win10 桌面优化。", "Tune global TCP settings to common Win10 desktop optimizations."),
        L("设置 autotuninglevel、timestamps、ECN 等 netsh 参数。", "Set netsh parameters such as autotuninglevel, timestamps, and ECN."),
        L("部分网络环境下降低延迟、提高吞吐稳定性。", "On some networks: lower latency and more stable throughput."),
        L("若遇特殊网络设备兼容问题可恢复默认；一般宽带/内网可开启。", "Revert if a special appliance has issues; OK for typical broadband/LAN."),
        L("立即生效。", "Takes effect immediately."));

    public static readonly SettingHelpInfo QosSpeedOptimize = H(
        L("QoS 零保留带宽 + 入站 TCP 最大吞吐量（级别 3）。", "QoS zero reserved bandwidth + inbound TCP max throughput (level 3)."),
        L("NonBestEffortLimit=0；基于策略的 QoS 高级设置 Tcp Autotuning Level=normal（接收窗口最大 16MB）。", "NonBestEffortLimit=0; Policy-based QoS Tcp Autotuning Level=normal (recv window up to 16MB)."),
        L("对齐 gpedit 中「限制可保留带宽 0%」与「入站 TCP 吞吐量级别 3」，利于大文件下载与远程传输。", "Matches gpedit Limit reservable bandwidth 0% and inbound TCP throughput level 3; helps large downloads/remote transfers."),
        L("企业域环境若已由 GPO 管控 QoS 请谨慎；个人/内网 Server 桌面可开启。", "Caution if domain GPO already manages QoS; OK for personal/LAN Server desktops."),
        L("策略写入后立即生效；部分场景建议重启网络栈或重启系统。", "Applies right after policy write; some cases need network stack or full reboot."));

    public static readonly SettingHelpInfo DisableErrorReport = H(
        L("关闭 Windows 错误报告（WerSvc）上传。", "Disable Windows Error Reporting (WerSvc) uploads."),
        L("禁用 Windows Error Reporting 服务。", "Disable the Windows Error Reporting service."),
        L("崩溃时不再后台上传 dump，减少隐私与网络占用。", "No background dump uploads on crash; less privacy/network use."),
        L("需向微软提交崩溃诊断则保持关闭本项；个人桌面推荐开启。", "Keep off if you send crash diagnostics to Microsoft; recommended on for personal desktops."),
        L("服务禁用后生效。", "Takes effect after the service is disabled."));

    public static readonly SettingHelpInfo LongPathsEnabled = H(
        L("允许路径与文件名超过 260 字符限制。", "Allow paths/filenames longer than the 260-character limit."),
        L("启用 NTFS 长路径策略 LongPathsEnabled。", "Enable the LongPathsEnabled NTFS long-path policy."),
        L("开发工具、深层目录、npm/git 项目不再因路径过长失败。", "Dev tools, deep folders, and npm/git projects stop failing on long paths."),
        L("需配合应用程序支持长路径；现代开发环境强烈推荐。", "Apps must also support long paths; strongly recommended for modern dev setups."),
        L("新启动的程序生效；部分旧程序需重启。", "Applies to newly started apps; some older apps need a restart."),
        LongPath);

    public static readonly SettingHelpInfo DisableFastStartup = H(
        L("关闭「快速启动」，关机改为完整关机。", "Disable Fast Startup so shutdown is a full power-off."),
        L("HiberbootEnabled 设为 0，避免混合关机。", "Set HiberbootEnabled=0 to avoid hybrid shutdown."),
        L("双系统、硬件变更、故障排查更可靠；部分驱动更新需完整关机。", "More reliable for dual-boot, hardware changes, and troubleshooting; some driver updates need full shutdown."),
        L("若追求最快开机且单系统可关闭；多系统/运维环境推荐开启。", "Leave off for fastest boot on single OS; recommended on for multi-boot/ops."),
        L("下次关机后生效。", "Takes effect after the next shutdown."),
        FastStartup);

    public static readonly SettingHelpInfo DisableAutoMaintenance = H(
        L("禁用系统自动维护计划任务。", "Disable scheduled Automatic Maintenance."),
        L("MaintenanceDisabled 设为 1，减少固定时段后台维护。", "Set MaintenanceDisabled=1 to reduce fixed-window background maintenance."),
        L("避免维护窗口内磁盘/CPU 突增，适合 7×24 在线桌面。", "Avoid disk/CPU spikes in the maintenance window; good for 24×7 desktops."),
        L("仍建议偶尔手动检查更新与磁盘；纯个人桌面可开启。", "Still check updates/disk occasionally; OK on for personal desktops."),
        L("下次维护周期起生效。", "Applies from the next maintenance cycle."));

    public static readonly SettingHelpInfo ExcludeDriverUpdates = H(
        L("Windows 更新不包含驱动程序。", "Exclude drivers from Windows Update."),
        L("ExcludeWUDriversInQualityUpdate 设为 1。", "Set ExcludeWUDriversInQualityUpdate=1."),
        L("避免驱动被自动更坏导致蓝屏/网卡失效；由厂商手动升级。", "Avoid auto driver updates that cause BSOD/NIC issues; update from vendors manually."),
        L("新硬件需手动装驱动；稳定为主的环境推荐开启。", "New hardware needs manual drivers; recommended where stability comes first."),
        L("下次 Windows Update 扫描起生效。", "Applies from the next Windows Update scan."),
        S2016W10);

    public static readonly SettingHelpInfo DisableSmb1 = H(
        L("禁用 SMB 1.0 文件共享协议。", "Disable the SMB 1.0 file-sharing protocol."),
        L("禁用 mrxsmb10 并关闭 SMB1 服务器参数。", "Disable mrxsmb10 and turn off SMB1 server parameters."),
        L("封堵 WannaCry 等旧协议攻击面，符合现代安全基线。", "Closes WannaCry-class SMB1 attack surface; modern security baseline."),
        L("仅当需访问极老 NAS/设备时才保留 SMB1；否则务必开启。", "Keep SMB1 only for very old NAS/devices; otherwise must enable this."),
        L("立即生效；访问 SMB1 设备将失败。", "Takes effect immediately; SMB1 devices will fail to connect."));

    public static readonly SettingHelpInfo DisableRemoteRegistry = H(
        L("禁用 Remote Registry 远程注册表服务。", "Disable the Remote Registry service."),
        L("停止 RemoteRegistry 服务并设为禁用。", "Stop RemoteRegistry and set it to Disabled."),
        L("减少远程篡改注册表的风险，符合安全加固惯例。", "Less risk of remote registry tampering; common hardening practice."),
        L("需用远程注册表管理工具（regedit 连远程）时勿开启。", "Do not enable if you need remote regedit management."),
        L("服务停止后生效。", "Takes effect after the service stops."));

    public static readonly SettingHelpInfo DisablePrintSpooler = H(
        L("禁用 Print Spooler 打印后台服务。", "Disable the Print Spooler service."),
        L("停止 Spooler 服务（无本地打印时可关）。", "Stop the Spooler service (OK when no local printing)."),
        L("减少攻击面与内存占用；PrintNightmare 类风险面更小。", "Smaller attack surface and memory use; less PrintNightmare exposure."),
        L("若需本地或网络打印必须关闭本项；无打印机强烈推荐开启。", "Must keep off if you print locally/over network; strongly recommended with no printers."),
        L("服务停止后无法打印。", "Printing stops once the service is stopped."));

    public static readonly SettingHelpInfo ShowThisPcIcon = H(
        L("在桌面显示「此电脑」图标。", "Show the This PC icon on the desktop."),
        L("修改桌面图标隐藏列表，显示计算机 CLSID。", "Unhide the Computer CLSID in the desktop icon hide list."),
        L("快速进入磁盘分区，符合传统 Windows 桌面习惯。", "Quick access to drives; classic Windows desktop habit."),
        L("运维/开发桌面强烈推荐；喜欢简洁桌面可关闭。", "Strongly recommended for ops/dev desktops; turn off for a minimal desktop."),
        L("立即生效或刷新桌面。", "Takes effect immediately or after desktop refresh."),
        SettingScope.DesktopExperience,
        recommend: RecommendLevel.Must);

    public static readonly SettingHelpInfo LaunchExplorerThisPc = H(
        L("打开资源管理器时默认进入「此电脑」。", "Open File Explorer to This PC by default."),
        L("LaunchTo 设为此电脑而非快速访问。", "Set LaunchTo to This PC instead of Quick access."),
        L("直接看到所有驱动器，减少一点击路径。", "See all drives immediately; one less click."),
        L("依赖快速访问/最近文件可关闭；传统习惯推荐开启。", "Turn off if you rely on Quick access/recent files; recommended for classic habits."),
        L("新开资源管理器窗口生效。", "Applies to newly opened Explorer windows."),
        SettingScope.DesktopExperience);

    public static readonly SettingHelpInfo SmallTaskbar = H(
        L("任务栏使用小图标按钮。", "Use small taskbar buttons."),
        L("TaskbarSmallIcons 设为 1。", "Set TaskbarSmallIcons=1."),
        L("节省垂直空间，同屏显示更多任务栏图标。", "Saves vertical space; more icons fit on the taskbar."),
        L("高 DPI 大屏若觉得太小可关闭；笔记本/1080p 推荐开启。", "Turn off if too small on high-DPI; recommended on laptop/1080p."),
        L("立即生效。", "Takes effect immediately."),
        SettingScope.DesktopExperience);

    public static readonly SettingHelpInfo ConfirmDelete = H(
        L("删除文件时弹出确认对话框。", "Show a confirmation dialog when deleting files."),
        L("ConfirmFileDelete 策略设为启用。", "Enable the ConfirmFileDelete policy."),
        L("防止误删；多一步确认更安全。", "Helps prevent accidental deletes."),
        L("熟练用户追求效率可关闭；公用或重要数据环境推荐开启。", "Power users may turn off; recommended on shared or important-data PCs."),
        L("立即生效。", "Takes effect immediately."),
        SettingScope.DesktopExperience);

    public static readonly SettingHelpInfo EnableAudio = H(
        L("启动 Windows Audio 音频服务。", "Start the Windows Audio services."),
        L("AudioSrv 与 AudioEndpointBuilder 设为自动并启动。", "Set AudioSrv and AudioEndpointBuilder to Automatic and start them."),
        L("Server 桌面可正常播放系统声音、提示音与媒体。", "Server desktops can play system sounds, beeps, and media."),
        L("Server 桌面强烈推荐；纯服务器无扬声器可关。", "Strongly recommended on Server desktop; OK off on headless servers."),
        L("服务启动后立即生效。", "Takes effect once the services start."),
        SettingScope.DesktopExperience,
        recommend: RecommendLevel.Strong);

    public static readonly SettingHelpInfo ShowFileExtensions = H(
        L("显示已知文件类型的扩展名。", "Show extensions for known file types."),
        L("HideFileExt 设为 0，显示 .txt .exe 等后缀。", "Set HideFileExt=0 to show .txt/.exe and other suffixes."),
        L("识别伪装恶意文件（如 virus.txt.exe），运维更安全。", "Spot disguised malware (e.g. virus.txt.exe); safer for ops."),
        L("建议显示扩展名；隐藏后难辨真实文件类型。", "Recommended on; hidden extensions make real types hard to see."),
        L("立即生效。", "Takes effect immediately."),
        SettingScope.DesktopExperience,
        recommend: RecommendLevel.Must);

    public static readonly SettingHelpInfo EnableThemes = H(
        L("启用 Themes 主题服务，完整 Aero/个性化外观。", "Enable the Themes service for full Aero/personalization look."),
        L("Themes 服务自动启动。", "Set the Themes service to start automatically."),
        L("窗口边框、壁纸、颜色正常；非「经典灰」界面。", "Normal borders, wallpaper, and colors—not the classic gray UI."),
        L("Server 当桌面几乎必选；极致省资源可关（界面变简陋）。", "Almost required for Server-as-desktop; turn off only to save resources (UI looks plain)."),
        L("服务启动后生效，必要时注销。", "Applies after the service starts; log off if needed."),
        SettingScope.DesktopExperience,
        recommend: RecommendLevel.Must);

    public static readonly SettingHelpInfo EnableSearch = H(
        L("启用 Windows Search 索引服务。", "Enable the Windows Search indexing service."),
        L("WSearch 服务自动启动。", "Set WSearch to start automatically."),
        L("开始菜单与资源管理器搜索更快，支持内容索引。", "Faster Start/Explorer search with content indexing."),
        L("极弱配置或几乎不用搜索可关；日常使用推荐开启。", "OK off on very weak hardware or if unused; recommended for daily use."),
        L("索引建立需时；服务启动后生效。", "Indexing takes time; applies after the service starts."),
        SettingScope.DesktopExperience);

    public static readonly SettingHelpInfo DisableWebSearch = H(
        L("开始菜单搜索仅本地，不查 Bing 网络。", "Start search stays local; no Bing web results."),
        L("DisableWebSearch 与 BingSearchEnabled 关闭网络搜索。", "DisableWebSearch and BingSearchEnabled turn off web search."),
        L("结果更干净、响应更快，无隐私外泄与广告。", "Cleaner/faster results; no privacy leak or ads."),
        L("个人/内网 Server 桌面推荐开启。", "Recommended for personal/LAN Server desktops."),
        L("立即生效。", "Takes effect immediately."),
        W10De);

    public static readonly SettingHelpInfo DisableFeedback = H(
        L("关闭「向我们反馈」等体验调查弹窗。", "Disable Feedback Hub / experience survey prompts."),
        L("限制 SIUF 体验反馈提示频率。", "Limit SIUF experience feedback prompts."),
        L("减少打断与后台联系 Microsoft 的提示。", "Fewer interruptions and Microsoft contact prompts."),
        L("个人桌面推荐开启。", "Recommended for personal desktops."),
        L("立即生效。", "Takes effect immediately."),
        W10De);

    public static readonly SettingHelpInfo NoLockScreen = H(
        L("跳过锁屏界面，唤醒直接进入登录或桌面。", "Skip the lock screen; wake goes straight to sign-in or desktop."),
        L("NoLockScreen 策略设为启用。", "Enable the NoLockScreen policy."),
        L("减少一次多余滑动/点击；个人物理安全可控时更顺手。", "One less swipe/click; handy when physical access is controlled."),
        L("笔记本公共场所或需锁屏广告/信息时勿开。", "Do not enable on public laptops or if you need lock-screen info/ads."),
        L("策略生效后下次唤醒可见。", "Visible on next wake after the policy applies."),
        SettingScope.DesktopExperience);

    public static readonly SettingHelpInfo ShowHiddenFiles = H(
        L("资源管理器默认显示隐藏文件与文件夹。", "Show hidden files and folders by default in Explorer."),
        L("Hidden 设为显示隐藏项。", "Set Hidden to show hidden items."),
        L("便于修改 AppData、系统配置；开发运维常见需求。", "Easier AppData/system config edits; common for dev/ops."),
        L("新手若怕误删系统文件可暂不开启；熟练用户推荐。", "Beginners may leave off; recommended for experienced users."),
        L("立即生效。", "Takes effect immediately."),
        SettingScope.DesktopExperience);

    public static readonly SettingHelpInfo NoShortcutArrow = H(
        L("桌面与资源管理器中快捷方式去掉小箭头 overlay。", "Remove the shortcut arrow overlay in desktop/Explorer."),
        L("Shell Icons 29 = %systemroot%\\system32\\imageres.dll,197（透明图标）。", "Shell Icons 29 = %systemroot%\\system32\\imageres.dll,197 (transparent icon)."),
        L("桌面更整洁；与常见「删除快捷方式箭头.reg」相同写法。", "Cleaner desktop; same as common remove-shortcut-arrow .reg."),
        L("桌面体验强烈推荐；需区分快捷方式与原件时可关闭。", "Strongly recommended for desktop UX; turn off if you need to tell shortcuts from originals."),
        L("写入后会重启资源管理器；关闭时删除整个 Shell Icons 键（与「恢复快捷方式箭头.reg」一致）。", "Restarts Explorer after write; turning off deletes the Shell Icons key (same as restore-arrow .reg)."),
        SettingScope.DesktopExperience,
        recommend: RecommendLevel.Must);

    public static readonly SettingHelpInfo ExplorerFullPath = H(
        L("资源管理器窗口标题栏显示完整文件夹路径。", "Show the full folder path in Explorer window titles."),
        L("FullPath 设为 1。", "Set FullPath=1."),
        L("复制路径、确认当前位置更方便，尤其适合深层目录。", "Easier path copy and location checks, especially in deep folders."),
        L("推荐开启；仅在意简洁标题可关闭。", "Recommended on; turn off only for short titles."),
        L("新开窗口生效。", "Applies to newly opened windows."),
        SettingScope.DesktopExperience);

    public static readonly SettingHelpInfo TaskbarAllIcons = H(
        L("通知区域始终显示全部托盘图标。", "Always show all notification-area tray icons."),
        L("EnableAutoTray 设为 0，不自动折叠到溢出区。", "Set EnableAutoTray=0 so icons are not auto-hidden."),
        L("网络、音量、后台工具一眼可见，减少「找不到图标」。", "Network/volume/background tools stay visible."),
        L("桌面运维强烈推荐；任务栏拥挤时可关闭恢复自动隐藏。", "Strongly recommended for desktop ops; turn off to restore auto-hide if crowded."),
        L("立即生效。", "Takes effect immediately."),
        SettingScope.DesktopExperience,
        recommend: RecommendLevel.Must);

    public static readonly SettingHelpInfo TaskbarClockWeekdaySeconds = H(
        L("任务栏右下角时钟显示星期，时间精确到秒。", "Taskbar clock shows weekday and seconds."),
        L("ShowSecondsInSystemClock=1，并将短日期格式设为 yyyy/MM/dd dddd。", "ShowSecondsInSystemClock=1 and short date yyyy/MM/dd dddd."),
        L("一眼看到星期几与秒级时间，适合排班、日志对照与远程桌面。", "See weekday and seconds at a glance; useful for shifts, logs, and RDP."),
        L("桌面/远程场景强烈推荐；不需要时可关闭恢复系统默认格式。", "Strongly recommended for desktop/RDP; turn off to restore defaults."),
        L("应用后自动重启资源管理器使托盘时钟立即刷新。", "Restarts Explorer so the tray clock refreshes immediately."),
        SettingScope.DesktopExperience,
        recommend: RecommendLevel.Must);

    public static readonly SettingHelpInfo DisableAnimations = H(
        L("关闭窗口最小化/任务栏等动画。", "Disable window minimize/taskbar animations."),
        L("MinAnimate、TaskbarAnimations 等设为关闭。", "Turn off MinAnimate, TaskbarAnimations, etc."),
        L("远程桌面更跟手，低配置 CPU 负担更小。", "Snappier RDP; less CPU on low-end hardware."),
        L("在意过渡效果可关闭；RDP 与性能优先推荐开启。", "Turn off if you like transitions; recommended for RDP/performance."),
        L("注销或重启资源管理器后生效。", "Applies after logoff or Explorer restart."),
        SettingScope.DesktopExperience);

    public static readonly SettingHelpInfo DisableTransparency = H(
        L("关闭「透明效果」与亚克力模糊。", "Disable transparency and acrylic blur."),
        L("EnableTransparency 设为 0。", "Set EnableTransparency=0."),
        L("减少 GPU 合成，界面更「实色」；远程桌面带宽略省。", "Less GPU composition; solid UI; slightly less RDP bandwidth."),
        L("喜欢 Win11 毛玻璃可关闭；低配/远程推荐开启。", "Turn off for Win11 glass look; recommended on low-end/RDP."),
        L("立即或注销后生效。", "Takes effect immediately or after logoff."),
        W10De);

    public static readonly SettingHelpInfo DisableTips = H(
        L("关闭开始菜单/设置的提示、建议与赞助内容。", "Disable Start/Settings tips, suggestions, and sponsored content."),
        L("关闭 ContentDeliveryManager 多项订阅提示。", "Turn off multiple ContentDeliveryManager suggestion flags."),
        L("减少「你应该试试」类打扰与隐私追踪。", "Fewer try-this prompts and tracking."),
        L("个人桌面推荐开启。", "Recommended for personal desktops."),
        L("立即生效。", "Takes effect immediately."),
        W10De);

    public static readonly SettingHelpInfo DisableAutoplay = H(
        L("插入 U 盘/光盘不自动运行或弹窗。", "Do not autoplay or prompt when inserting USB/optical media."),
        L("NoDriveTypeAutoRun 设为禁用所有驱动器自动播放。", "Set NoDriveTypeAutoRun to disable AutoPlay for all drives."),
        L("防止恶意 U 盘自动执行，安全基线项。", "Blocks malicious USB autorun; security baseline."),
        L("几乎总是推荐开启；需自动播放安装盘时临时关闭。", "Almost always recommended; temporarily off for installer AutoPlay."),
        L("立即生效。", "Takes effect immediately."));

    public static readonly SettingHelpInfo DisableActivityHistory = H(
        L("禁用时间线/活动历史与跨设备同步。", "Disable Timeline/activity history and cross-device sync."),
        L("AllowPublishUserActivities 与 PublishUserActivities 关闭。", "Turn off AllowPublishUserActivities and PublishUserActivities."),
        L("减少隐私上传与后台记录；任务视图内容更少。", "Less privacy upload/background logging; emptier Task View."),
        L("个人/内网推荐开启；依赖时间线恢复工作流则关闭。", "Recommended for personal/LAN; keep off if you rely on Timeline."),
        L("立即生效。", "Takes effect immediately."),
        Activity1803);

    public static readonly SettingHelpInfo DisableStorageSense = H(
        L("关闭存储感知自动清理与临时文件策略。", "Disable Storage Sense auto-cleanup and temp-file policy."),
        L("AllowStorageSenseGlobal 设为 0。", "Set AllowStorageSenseGlobal=0."),
        L("避免后台自动删文件；Server 桌面常需手动掌控磁盘。", "Avoid background auto-deletes; Server desktops often need manual disk control."),
        L("磁盘紧张且信任自动清理可关闭；控台环境推荐开启。", "Leave off if disk is tight and you trust auto-clean; recommended on consoles."),
        L("立即生效。", "Takes effect immediately."),
        StorageW10);

    public static readonly SettingHelpInfo EnableRdp = H(
        L("启用远程桌面（RDP）并接受连接。", "Enable Remote Desktop (RDP) and accept connections."),
        L("fDenyTSConnections 设为 0，并打开防火墙 RDP 规则。", "Set fDenyTSConnections=0 and enable firewall RDP rules."),
        L("可从其他 PC/macOS/Linux 图形远程本机，Server 当桌面核心能力。", "Graphical remote from PC/macOS/Linux; core for Server-as-desktop."),
        L("不远程访问且要减攻击面时可关闭；需 RDP 必须开启。", "Turn off if unused to shrink attack surface; must on if you need RDP."),
        L("立即生效；防火墙规则同步应用。", "Takes effect immediately; firewall rules applied together."),
        recommend: RecommendLevel.Must);

    public static readonly SettingHelpInfo RdpMultiUserLogin = H(
        L("允许多用户同时登录远程桌面（同账号多会话）。", "Allow multiple concurrent RDP logons (multi-session per user)."),
        L("开启 RDP；DISM 启用 RDS-RD-Server；fSingleSessionPerUser=0；MaxSessions=999999；fAllowConsoleLogout=0。",
            "Enable RDP; DISM enable RDS-RD-Server; fSingleSessionPerUser=0; MaxSessions=999999; fAllowConsoleLogout=0."),
        L("多人同时远程同一 Server；不强制踢掉已登录的控制台管理员。",
            "Multiple people can remote the same Server; do not force-logoff the console admin."),
        L("Server 多会话实验室可用；正式环境请确认 RDS CAL/授权。关闭时仅恢复单会话限制，不卸载 RDS、不关 RDP。",
            "OK for Server multi-session labs; confirm RDS CALs in production. Off only restores single-session limits; does not remove RDS or disable RDP."),
        L("注册表立即写入；RDS 角色可能需重启。", "Registry applies immediately; RDS role may need a reboot."),
        SettingScope.ServerExclusive,
        recommend: RecommendLevel.Suggested);

    public static readonly SettingHelpInfo RdpGpuAccel = H(
        L("RDP 会话启用 GPU 硬件加速与更好的图形管线。", "Enable GPU acceleration and a better graphics pipeline for RDP sessions."),
        L("Terminal Services UseAdvancedGraphics 策略。", "Terminal Services UseAdvancedGraphics policy."),
        L("远程看网页、视频、UI 动画更流畅。", "Smoother remote web/video/UI animation."),
        L("有显卡远程桌面强烈推荐；无 GPU 或极老驱动可关。", "Strongly recommended for RDP with a GPU; turn off with no GPU/very old drivers."),
        L("新 RDP 连接生效。", "Applies to new RDP connections."),
        Rdp2019,
        recommend: RecommendLevel.Must);

    public static readonly SettingHelpInfo RdpHighRefresh = H(
        L("提高远程桌面帧率上限（缩短 DWMFRAMEINTERVAL）。", "Raise RDP frame-rate cap (shorten DWMFRAMEINTERVAL)."),
        L("将帧间隔设为 15（约 60Hz 档）。", "Set frame interval to 15 (~60 Hz class)."),
        L("鼠标移动、滚动、视频观感更顺滑。", "Smoother mouse, scroll, and video feel."),
        L("内网/高带宽远程强烈推荐；低带宽网络可能增带宽。", "Strongly recommended on LAN/high-bandwidth RDP; may use more bandwidth on slow links."),
        L("新 RDP 连接生效。", "Applies to new RDP connections."),
        Rdp2019,
        recommend: RecommendLevel.Must);

    public static readonly SettingHelpInfo RdpDisableNla = H(
        L("RDP 不要求网络级身份验证（NLA）。", "Do not require Network Level Authentication (NLA) for RDP."),
        L("UserAuthentication 设为 0，兼容旧客户端。", "Set UserAuthentication=0 for older clients."),
        L("部分 Linux 旧版 rdesktop、特殊跳板可连上。", "Lets some old Linux rdesktop / special jump hosts connect."),
        L("安全性降低，仅内网可信环境短期使用；能开 NLA 则勿开。", "Weaker security—short-term trusted LAN only; keep NLA if possible."),
        L("新 RDP 连接生效。", "Applies to new RDP connections."));

    public static readonly SettingHelpInfo RdpAvc444 = H(
        L("优先 H.264/AVC 444 图形模式（RemoteFX 画质）。", "Prefer H.264/AVC 444 graphics mode (RemoteFX quality)."),
        L("Policies\\Terminal Services AVC444ModePreferred=1。", "Policies\\Terminal Services AVC444ModePreferred=1."),
        L("文字更清晰、帧率更高；对齐 r/sysadmin TurboRemoteFX。", "Sharper text and higher FPS; aligns with r/sysadmin TurboRemoteFX."),
        L("内网/有 GPU 的 RDP 强烈推荐；极慢链路可能更吃带宽。", "Strongly recommended on LAN/GPU RDP; may use more bandwidth on slow links."),
        L("新 RDP 连接生效；客户端需支持 AVC444。", "Applies to new RDP connections; client must support AVC444."),
        Rdp2019,
        recommend: RecommendLevel.Must);

    public static readonly SettingHelpInfo RdpAvcHwEncode = H(
        L("RDP 优先使用 H.264/AVC 硬件编码器。", "Prefer H.264/AVC hardware encoding for RDP."),
        L("AVCHardwareEncodePreferred=1。", "AVCHardwareEncodePreferred=1."),
        L("编码负载转到 GPU，减轻 CPU。", "Moves encode load to GPU; less CPU."),
        L("有硬件编码器时推荐；个别卡/驱动反而更差可关。", "Recommended with HW encoders; turn off if a GPU/driver regresses."),
        L("新 RDP 连接生效。", "Applies to new RDP connections."),
        Rdp2019,
        recommend: RecommendLevel.Strong);

    public static readonly SettingHelpInfo RdpHwGraphicsFirst = H(
        L("RDP 会话优先枚举硬件图形适配器。", "Enumerate hardware graphics adapters first for RDP."),
        L("bEnumerateHWBeforeSW=1。", "bEnumerateHWBeforeSW=1."),
        L("优先走 GPU 图形路径，少用软件光栅。", "Prefer GPU graphics path over software raster."),
        L("有独立/核显远程桌面推荐开启。", "Recommended when the host has a GPU."),
        L("新 RDP 连接生效。", "Applies to new RDP connections."),
        Rdp2019,
        recommend: RecommendLevel.Must);

    public static readonly SettingHelpInfo RdpRemoteFxGraphics = H(
        L("RemoteFX 自适应图形高质量包（压缩/画质/传输）。", "RemoteFX adaptive graphics high-quality pack."),
        L("VirtualizedGraphics、VisualExperience、ImageQuality=高、MaxCompression=最低、SelectTransport=双向。",
            "VirtualizedGraphics, VisualExperience, ImageQuality=high, MaxCompression=lowest, SelectTransport=both."),
        L("远程 UI/视频更清晰；带宽占用上升。", "Clearer remote UI/video; higher bandwidth."),
        L("内网高带宽 RDP 推荐；窄带环境请关闭。", "Recommended on LAN/high-bandwidth RDP; keep off on narrow links."),
        L("新 RDP 连接生效。", "Applies to new RDP connections."),
        Rdp2019,
        recommend: RecommendLevel.Strong);

    public static readonly SettingHelpInfo RdpLowLatency = H(
        L("降低 RDP 交互延迟（InteractiveDelay + TermDD 流控）。", "Lower RDP interaction latency (InteractiveDelay + TermDD flow)."),
        L("InteractiveDelay=0；TermDD 优先显示带宽；允许大 MTU。", "InteractiveDelay=0; TermDD prefers display bandwidth; allow large MTU."),
        L("鼠标/键盘跟手感更好，少「粘滞」。", "Snappier mouse/keyboard; less sticky feel."),
        L("内网桌面强烈推荐；极端窄带可关。", "Strongly recommended on LAN desktops; keep off on very narrow links."),
        L("立即写入；新连接体感更明显。", "Written immediately; clearest on new connections."),
        recommend: RecommendLevel.Must);

    public static readonly SettingHelpInfo RdpDisableWddm = H(
        L("RDP 禁用 WDDM、改用 XDDM 显示驱动（旧路径）。", "Disable WDDM for RDP; use legacy XDDM path."),
        L("fEnableWddmDriver=0。", "fEnableWddmDriver=0."),
        L("部分 NVIDIA 老方案远程 3D 更稳；现代 Win11/AMD 可能无益或更差。", "Helps some older NVIDIA remote-3D setups; may hurt modern Win11/AMD."),
        L("高级项，默认勿开；出问题再试。", "Advanced—leave off by default; try only if troubleshooting."),
        L("新 RDP 连接生效；可能需重启会话主机。", "Applies to new connections; session host restart may be needed."),
        Rdp2019,
        recommend: RecommendLevel.Optional);

    public static readonly SettingHelpInfo EnableNetworkDiscovery = H(
        L("启用网络发现与文件和打印机共享防火墙规则。", "Enable Network Discovery and File and Printer Sharing firewall rules."),
        L("启动 fdPHost/FDResPub 并放行相关防火墙组。", "Start fdPHost/FDResPub and allow related firewall groups."),
        L("局域网可见其他电脑、访问共享文件夹。", "See other PCs on the LAN and access shares."),
        L("不需要 SMB 共享或要最小暴露时关闭；家庭/ lab 局域网推荐开启。", "Turn off if no SMB shares / minimal exposure; recommended on home/lab LAN."),
        L("立即生效。", "Takes effect immediately."));

    public static readonly SettingHelpInfo DisableSmRemoting = H(
        L("关闭 Server Manager 远程管理（WinRM/SMRemoting）。", "Disable Server Manager remoting (WinRM/SMRemoting)."),
        L("禁用 Configure-SMRemoting 或 WinRM 服务。", "Disable Configure-SMRemoting or the WinRM service."),
        L("减少远程 PowerShell 管理入口，个人桌面通常用不到。", "Fewer remote PowerShell management endpoints; rarely needed on personal desktops."),
        L("需远程 Server Manager 管理多台 Server 时勿开。", "Do not enable if you manage multiple Servers via remote Server Manager."),
        L("立即生效。", "Takes effect immediately."),
        SettingScope.ServerExclusive);

    public static readonly SettingHelpInfo SkipServerManager = H(
        L("登录时不自动打开服务器管理器。", "Do not open Server Manager automatically at logon."),
        L("HKLM/HKCU\\…\\ServerManager\\DoNotOpenServerManagerAtLogon=1；策略 DoNotOpenAtLogon。", "HKLM/HKCU\\…\\ServerManager\\DoNotOpenServerManagerAtLogon=1; policy DoNotOpenAtLogon."),
        L("进桌面不再自动弹出服务器管理器，需要时仍可手动打开。", "No auto Server Manager on desktop; still openable manually."),
        L("Server 当桌面建议开。", "Recommended when Server is used as a desktop."),
        L("下次登录生效。", "Takes effect on next logon."),
        new SettingScope(serverOnly: true, minServer: "2012 R2+"),
        uiPlace: L("服务器管理器→管理→服务器管理器属性", "Server Manager → Manage → Server Manager Properties"),
        whenHint: L("当桌面用建议开", "Recommended when used as a desktop"),
        recommend: RecommendLevel.Must);

    public static readonly SettingHelpInfo HideServerManagerWacPrompt = H(
        L("不再弹出「立即尝试 Windows Admin Center 并 Azure Arc」推广窗。", "Stop the Try Windows Admin Center and Azure Arc promo popup."),
        L("HKLM\\SOFTWARE\\Microsoft\\ServerManager\\DoNotPopWACConsoleAtSMLaunch=1。", "HKLM\\SOFTWARE\\Microsoft\\ServerManager\\DoNotPopWACConsoleAtSMLaunch=1."),
        L("去掉开机/打开服务器管理器时的 WAC·Azure Arc 推广，管理器本身仍可用。", "Removes WAC/Azure Arc promos at boot/Server Manager; Server Manager still works."),
        L("Server 2019/2022 桌面建议开。", "Recommended on Server 2019/2022 desktops."),
        L("立即生效（下次打开服务器管理器）。", "Takes effect immediately (next Server Manager open)."),
        new SettingScope(serverOnly: true, minServer: "2019+"),
        uiPlace: L("开机弹窗「立即尝试 WAC 并 Azure Arc」", "Boot popup: Try WAC and Azure Arc"),
        whenHint: L("烦人就开", "Enable if it annoys you"),
        recommend: RecommendLevel.Must);

    public static readonly SettingHelpInfo DisableAzureArc = H(
        L("禁止 Azure Arc 托盘程序开机自启。", "Prevent Azure Arc tray app from starting at logon."),
        L("删除 Run 键中 AzureArcSetup 启动项。", "Remove the AzureArcSetup Run key startup entry."),
        L("无 Azure 混合管理需求时不占托盘、不后台连接云。", "No tray icon or cloud connect when Azure hybrid management is unused."),
        L("若已接入 Azure Arc 管理需关闭本项保留启动。", "Keep this off if the machine is already Arc-managed."),
        L("下次登录生效。", "Takes effect on next logon."),
        Arc2019,
        uiPlace: L("托盘/开机启动里的 Azure Arc", "Azure Arc in tray / startup"),
        whenHint: L("不用 Azure 建议开", "Recommended if you do not use Azure"),
        recommend: RecommendLevel.Strong);

    public static readonly SettingHelpInfo EnableInstaller = H(
        L("Windows Installer 服务设为自动。", "Set the Windows Installer service to Automatic."),
        L("msiserver 自动启动，安装 .msi 软件无需手动启服务。", "msiserver auto-starts so .msi installs need no manual service start."),
        L("正常安装 Office、工具软件不会报「Windows Installer 未启动」。", "Office/tool installs won't fail with Windows Installer not started."),
        L("极简 hardened 环境可关；桌面用途推荐开启。", "OK off on hardened minimal hosts; recommended for desktop use."),
        L("服务配置立即写入。", "Service config is written immediately."));

    public static readonly SettingHelpInfo EnableWia = H(
        L("启用 Windows Image Acquisition（扫描仪/部分摄像头）。", "Enable Windows Image Acquisition (scanners / some cameras)."),
        L("stisvc 服务自动启动。", "Set stisvc to start automatically."),
        L("扫描仪、部分旧摄像头可即插即用。", "Scanners and some legacy cameras work plug-and-play."),
        L("无 imaging 设备可关；需扫描/摄像头则开启。", "OK off without imaging devices; on if you need scan/camera."),
        L("服务启动后生效。", "Takes effect after the service starts."),
        SettingScope.DesktopExperience);

    public static readonly SettingHelpInfo DisablePasswordComplexity = H(
        L("本地账户密码不要求大小写+数字+符号组合。", "Local account passwords need not mix case/digits/symbols."),
        L("通过 secedit 将 PasswordComplexity 设为 0。", "Set PasswordComplexity=0 via secedit."),
        L("可设简单 PIN 式密码，个人 VM/内网更方便。", "Allows simple PIN-like passwords; handy for personal VMs/LAN."),
        L("个人桌面 / 内网 lab 强烈推荐；公网或合规环境请保持复杂性。", "Strongly recommended for personal desktop / private labs; keep complexity on public/compliance hosts."),
        L("策略立即写入。", "Policy is written immediately."),
        recommend: RecommendLevel.Must);

    public static readonly SettingHelpInfo PasswordNeverExpire = H(
        L("本地账户密码永不过期。", "Local account passwords never expire."),
        L("MaximumPasswordAge 设为 0。", "Set MaximumPasswordAge=0."),
        L("不会 42 天强制改密码打断工作。", "No forced password change every ~42 days."),
        L("有安全合规要求时勿开；个人单机推荐。", "Do not enable under compliance rules; OK on personal single PCs."),
        L("策略立即写入。", "Policy is written immediately."));

    public static readonly SettingHelpInfo ShutdownWithoutLogon = H(
        L("登录界面允许直接关机（无需先登录）。", "Allow shutdown from the sign-in screen without logging on."),
        L("ShutdownWithoutLogon 策略启用。", "Enable the ShutdownWithoutLogon policy."),
        L("物理机前可快速关机；虚拟机管理略方便。", "Faster physical shutdown; slightly easier VM power-off."),
        L("防他人恶意关机/物理安全场景可关闭。", "Turn off if you fear malicious shutdowns / need physical security."),
        L("立即生效。", "Takes effect immediately."));

    public static readonly SettingHelpInfo DisableShutdownReason = H(
        L("关闭「关机原因」与 Shutdown Event Tracker 弹窗。", "Disable shutdown reason / Shutdown Event Tracker prompts."),
        L("ShutdownReasonOn/UI 设为关闭。", "Turn off ShutdownReasonOn/UI."),
        L("关机/重启不再填原因问卷，个人桌面更省事。", "No reason questionnaire on shutdown/reboot; easier personal desktop."),
        L("个人桌面强烈推荐；企业审计需要关机原因时请保持开启。", "Strongly recommended for personal desktops; keep on if enterprise audit needs shutdown reasons."),
        L("立即生效。", "Takes effect immediately."),
        ShutdownTracker,
        recommend: RecommendLevel.Must);

    public static readonly SettingHelpInfo DisableCad = H(
        L("登录时不要求按 Ctrl+Alt+Del 安全 attention。", "Do not require Ctrl+Alt+Del secure attention at sign-in."),
        L("DisableCAD 设为 1，直接进入密码框。", "Set DisableCAD=1 to go straight to the password box."),
        L("减少一步按键；远程桌面登录略快。", "One less key chord; slightly faster RDP sign-in."),
        L("个人桌面 / 可控物理环境强烈推荐；高安全场景可保持需按键。", "Strongly recommended for personal / controlled physical access; keep CAD required in high-security setups."),
        L("立即生效。", "Takes effect immediately."),
        recommend: RecommendLevel.Must);
    public static readonly SettingHelpInfo EnableAutologon = H(
        L("开机后自动登录指定本地/域账户，无需输入密码。", "Auto-logon a chosen local/domain account without typing a password."),
        L("写入 Winlogon（AutoAdminLogon、DefaultUserName、DefaultDomainName），密码经 LsaStorePrivateData 存入 LSA，与 Sysinternals Autologon 相同。", "Writes Winlogon (AutoAdminLogon, DefaultUserName, DefaultDomainName); password stored via LsaStorePrivateData like Sysinternals Autologon."),
        L("个人物理机、开发用 Server 桌面免输密码；重启/断电恢复后直达桌面。", "Passwordless personal/dev Server desktops; reboot/power-loss goes straight to desktop."),
        L("开启后点击工具栏「Autologon 配置」填写账户；应用到系统时写入。关闭开关并应用可禁用。", "After enabling, use toolbar Autologon to set the account; written on Apply. Turn off and Apply to disable."),
        L("下次重启后生效；启动时按住 Shift 可临时跳过自动登录。", "Takes effect on next reboot; hold Shift at boot to skip once."),
        SettingScope.DesktopExperience);

    public static readonly SettingHelpInfo DisableSmartScreenWarning = H(
        L("关闭 SmartScreen 应用筛选与「打开文件安全警告」。", "Disable SmartScreen app filtering and Open File - Security Warning."),
        L("SmartScreenEnabled=0；Attachments SaveZoneInformation=1 跳过区域标记提示。", "SmartScreenEnabled=0; Attachments SaveZoneInformation=1 skips zone prompts."),
        L("本地/内网软件安装与脚本运行不再反复弹窗确认。", "Local/LAN installs and scripts stop prompting repeatedly."),
        L("仅建议在可信环境开启；公网下载文件请保持系统默认防护。", "Only for trusted environments; keep defaults for internet downloads."),
        L("立即生效；部分程序需重启后完全生效。", "Takes effect immediately; some apps need a restart."),
        SettingScope.DesktopExperience);

    public static readonly SettingHelpInfo ShowControlPanelRecycleBin = H(
        L("在桌面显示「控制面板」与「回收站」图标。", "Show Control Panel and Recycle Bin icons on the desktop."),
        L("修改 HideDesktopIcons\\NewStartPanel 下对应 CLSID 为显示。", "Unhide the matching CLSIDs under HideDesktopIcons\\NewStartPanel."),
        L("与「此电脑」图标一起，恢复经典 Server 桌面布局。", "Together with This PC, restores a classic Server desktop layout."),
        L("推荐 Server 当桌面用时开启。", "Recommended when Server is used as a desktop."),
        L("立即生效；若未刷新可重启 Explorer。", "Takes effect immediately; restart Explorer if needed."),
        SettingScope.DesktopExperience);

    public static readonly SettingHelpInfo LargeSystemCacheOptimize = H(
        L("启用大系统缓存、禁止内核分页、增大 NTFS 内存缓冲。", "Enable large system cache, keep kernel non-paged, enlarge NTFS memory buffer."),
        L("LargeSystemCache=1、DisablePagingExecutive=1、NtfsMemoryUsage=2。", "LargeSystemCache=1, DisablePagingExecutive=1, NtfsMemoryUsage=2."),
        L("文件服务器/大文件读写场景提升缓存命中率；部分桌面 Server 帖推荐。", "Better cache hit rate for file servers/large I/O; some desktop-Server tips recommend it."),
        L("纯交互桌面、内存紧张时可关闭恢复默认。", "Turn off on interactive-only / low-RAM desktops to restore defaults."),
        L("重启后完全生效。", "Fully applies after reboot."),
        S2016);

    public static readonly SettingHelpInfo DisableReservedStorage = H(
        L("关闭系统分区「保留存储」占用。", "Disable Reserved Storage on the system volume."),
        L("ReserveManager ShippedWithReserves=0。", "ReserveManager ShippedWithReserves=0."),
        L("释放数 GB 磁盘给数据盘使用。", "Frees several GB for your data."),
        L("更新失败回滚空间略减；磁盘紧张时推荐开启。", "Slightly less update-rollback space; recommended when disk is tight."),
        L("立即写入；部分版本需重启或磁盘清理后可见。", "Written immediately; some builds need reboot or Disk Cleanup to show space."),
        W10);

    public static readonly SettingHelpInfo DisableSrvSplit = H(
        L("关闭 LanmanServer 服务拆分（SrvSplitThreshold 设为最大）。", "Disable LanmanServer service split (SrvSplitThreshold maximized)."),
        L("避免 SMB 服务在高负载下拆分子进程导致连接异常。", "Avoid SMB subprocess splits under load that can break connections."),
        L("部分社区 Server 桌面优化脚本推荐；适合 SMB 文件共享场景。", "Recommended by some community Server-desktop scripts; for SMB file sharing."),
        L("极高并发 SMB 场景可按微软文档评估是否保持默认。", "For very high SMB concurrency, follow Microsoft docs on keeping defaults."),
        L("重启后生效。", "Takes effect after reboot."),
        SettingScope.ServerExclusive);

    public static readonly SettingHelpInfo EnableGpuHwScheduling = H(
        L("启用系统级 GPU 硬件加速计划（HwSchMode=2）。", "Enable hardware-accelerated GPU scheduling (HwSchMode=2)."),
        L("与 RDP 图形加速不同，作用于本机 GPU 调度。", "Unlike RDP graphics accel; affects local GPU scheduling."),
        L("Win10/Server 2019+ 桌面游戏、视频、GPU 计算更流畅。", "Smoother games/video/GPU compute on Win10/Server 2019+."),
        L("极老显卡或驱动问题可关闭排查。", "Turn off to troubleshoot very old GPUs or driver issues."),
        L("重启后生效。", "Takes effect after reboot."),
        S2019);

    public static readonly SettingHelpInfo DisableLoginKeyboardFilters = H(
        L("取消登录界面粘滞键/筛选键等辅助功能快捷键提示。", "Disable Sticky Keys / Filter Keys prompts on the sign-in screen."),
        L("写入当前用户与 .DEFAULT 下 Accessibility Flags。", "Writes Accessibility Flags for the current user and .DEFAULT."),
        L("连续按 Shift 登录时不再弹出「启用粘滞键？」打断。", "No Enable Sticky Keys popup when Shift is pressed repeatedly at logon."),
        L("需要辅助功能的用户请勿开启。", "Do not enable if you need accessibility shortcuts."),
        L("注销或重启登录界面后生效。", "Applies after logoff or restarting the sign-in UI."),
        SettingScope.DesktopExperience);

    public static readonly SettingHelpInfo DisableBackgroundApps = H(
        L("禁止 UWP/商店应用在后台运行。", "Block UWP/Store apps from running in the background."),
        L("LetAppsRunInBackground=2 + GlobalUserDisabled=1 + BackgroundAppGlobalToggle=0。", "LetAppsRunInBackground=2 + GlobalUserDisabled=1 + BackgroundAppGlobalToggle=0."),
        L("减少 idle CPU/网络占用，对齐 optimizerDuck DisableBackgroundApps。", "Less idle CPU/network; aligns with optimizerDuck DisableBackgroundApps."),
        L("依赖后台同步的应用（邮件、OneDrive 等）可能受影响。", "Apps that need background sync (Mail, OneDrive, etc.) may be affected."),
        L("立即生效。", "Takes effect immediately."),
        W10De);

    public static readonly SettingHelpInfo ClassicFileSearch = H(
        L("搜索退回传统模式：关闭 Cortana、任务栏搜索框改为图标。", "Classic search: disable Cortana; taskbar search box becomes an icon."),
        L("AllowCortana=0；SearchboxTaskbarMode=0。", "AllowCortana=0; SearchboxTaskbarMode=0."),
        L("减少索引与 Web 搜索干扰，贴近 Win7 搜索体验。", "Less index/web-search noise; closer to Win7 search feel."),
        L("需 Windows 搜索索引时请配合「启用 Windows 搜索」使用。", "If you need indexing, also enable Windows Search."),
        L("注销或重启 Explorer 后生效。", "Applies after logoff or Explorer restart."),
        W10De);

    public static readonly SettingHelpInfo DisableSearchEngineFeature = H(
        L("卸载/禁用 SearchEngine 可选功能并停止 WSearch 服务。", "Disable SearchEngine optional feature and stop WSearch."),
        L("DISM Disable-Feature SearchEngine + WSearch 禁用。", "DISM Disable-Feature SearchEngine + disable WSearch."),
        L("比仅停服务更彻底，减少索引磁盘占用。", "More thorough than stopping the service alone; less index disk use."),
        L("与「启用 Windows 搜索」冲突：开启本项时搜索服务不会启动。", "Conflicts with Enable Windows Search: WSearch will not start while this is on."),
        L("DISM 完成后建议重启。", "Reboot recommended after DISM finishes."),
        SettingScope.ServerExclusive);

    public static readonly SettingHelpInfo EnableDesktopMediaFeatures = H(
        L("开启 Server 桌面媒体组件：MediaFoundation、DirectPlay、WLAN 等。", "Enable Server desktop media features: MediaFoundation, DirectPlay, WLAN, etc."),
        L("DISM Enable-Feature：Server-Media-Foundation、DirectPlay、Wireless-Networking 等（按版本跳过不存在项）。", "DISM Enable-Feature: Server-Media-Foundation, DirectPlay, Wireless-Networking, etc. (skips missing per SKU)."),
        L("支持音视频播放、旧游戏 DirectPlay、无线网络。", "Supports A/V playback, legacy DirectPlay games, and Wi-Fi."),
        L("Server Core 或无桌面体验无效；2016/2019/2022 桌面推荐。", "No effect on Server Core / no Desktop Experience; recommended on 2016/2019/2022 desktop."),
        L("DISM 完成后需重启。", "Reboot required after DISM finishes."),
        SettingScope.ServerExclusive);

    public static readonly SettingHelpInfo DisableServerBloatFeatures = H(
        L("关闭 Server 冗余可选功能：RSAT、SystemDataArchiver、WAC 安装包等。", "Disable Server bloat optional features: RSAT, SystemDataArchiver, WAC package, etc."),
        L("DISM 禁用已知功能名并扫描 RSAT-* 已启用项。", "DISM disables known feature names and scans enabled RSAT-* features."),
        L("减少组件占用与误触管理工具；纯桌面用途更干净。", "Less component footprint and accidental admin tools; cleaner desktop-only use."),
        L("仍需远程管理 Server 时请勿禁用 RSAT/WAC。", "Do not disable RSAT/WAC if you still remotely manage Servers."),
        L("DISM 完成后建议重启。", "Reboot recommended after DISM finishes."),
        SettingScope.ServerExclusive);

    public static readonly SettingHelpInfo ShowItemCheckboxes = H(
        L("资源管理器使用复选框选择文件。", "Use checkboxes to select files in Explorer."), L("AutoCheckSelect=1。", "AutoCheckSelect=1."), L("批量选择更方便。", "Easier multi-select."), L("桌面/File Explorer 推荐。", "Recommended for desktop / File Explorer."), L("重启资源管理器后生效。", "Takes effect after Explorer restart."), W10De);
    public static readonly SettingHelpInfo ShowCommonFolders = H(
        L("导航窗格显示所有文件夹。", "Show all folders in the navigation pane."), L("NavPaneShowAllFolders=1。", "NavPaneShowAllFolders=1."), L("快速访问常用目录。", "Faster access to common folders."), L("个人桌面推荐。", "Recommended for personal desktops."), L("重启资源管理器后生效。", "Takes effect after Explorer restart."), W10De);
    public static readonly SettingHelpInfo RemoveAdminShield = H(
        L("快捷方式不显示管理员盾牌图标。", "Hide the admin shield overlay on shortcuts."),
        L("Shell Icons 77 = %systemroot%\\system32\\imageres.dll,197。", "Shell Icons 77 = %systemroot%\\system32\\imageres.dll,197."),
        L("界面更简洁。", "Cleaner UI."),
        L("桌面体验强烈推荐；仅影响图标显示，不降低权限。", "Strongly recommended for desktop UX; icon-only, does not lower privileges."),
        L("重启资源管理器后生效。", "Takes effect after Explorer restart."),
        W10De,
        recommend: RecommendLevel.Must);
    public static readonly SettingHelpInfo NoShortcutSuffix = H(
        L("新建快捷方式时不自动加「快捷方式」后缀。", "Do not append Shortcut suffix when creating shortcuts."),
        L("NamingTemplates\\ShortcutNameTemplate（已弃用会弄丢桌面图标的 Link 写法）。", "NamingTemplates\\ShortcutNameTemplate (avoids the old Link method that could drop desktop icons)."),
        L("文件名更干净。", "Cleaner file names."),
        L("强烈推荐开启。", "Strongly recommended on."),
        L("写入后会重启资源管理器。", "Restarts Explorer after write."),
        W10De,
        recommend: RecommendLevel.Must);
    public static readonly SettingHelpInfo Win11ExplorerStyle = H(
        L("使用 Win11 默认间距的资源管理器布局。", "Use Win11 default Explorer spacing (non-compact)."), L("UseCompactMode=0。", "UseCompactMode=0."), L("非紧凑模式。", "Non-compact mode."), L("关闭则使用紧凑模式。", "Turning off uses compact mode."), L("重启资源管理器后生效。", "Takes effect after Explorer restart."), W10De);
    public static readonly SettingHelpInfo Win10ClassicContextMenu = H(
        L("右键菜单恢复 Win10 经典完整菜单。", "Restore the classic full Win10 context menu."), L("CLSID 86ca1aa0 InprocServer32。", "CLSID 86ca1aa0 InprocServer32."), L("更多项一步可见。", "More items visible in one step."), L("习惯经典菜单时开启。", "Enable if you prefer the classic menu."), L("重启资源管理器后生效。", "Takes effect after Explorer restart."), W10De);
    public static readonly SettingHelpInfo HideTaskbarSearch = H(
        L("完全隐藏任务栏搜索栏（不显示搜索框和图标）。", "Fully hide taskbar search (no box or icon)."),
        L("SearchboxTaskbarMode=0，并写 SearchboxTaskbarModeCache=1（Advanced + Search）。", "SearchboxTaskbarMode=0 and SearchboxTaskbarModeCache=1 (Advanced + Search)."),
        L("任务栏更干净，少占空间。", "Cleaner taskbar; less space used."),
        L("Server 桌面或追求极简任务栏建议开启；仍要搜索可按 Win 键。", "Recommended for Server/minimal taskbar; press Win to search."),
        L("应用到系统后会重启资源管理器生效。", "Restarts Explorer after Apply to take effect."),
        W10De,
        uiPlace: L("任务栏右键→任务栏设置→搜索→隐藏", "Taskbar right-click → Taskbar settings → Search → Hidden"),
        whenHint: L("想干净任务栏就开", "Enable for a cleaner taskbar"));

    public static readonly SettingHelpInfo TaskbarSearchBox = H(
        L("任务栏显示搜索框。", "Show the search box on the taskbar."), L("SearchboxTaskbarMode=2。", "SearchboxTaskbarMode=2."), L("快速搜索入口。", "Quick search entry."), L("关闭则仅显示图标。", "Turning off shows icon only."), L("重启资源管理器后生效。", "Takes effect after Explorer restart."), W10De);
    public static readonly SettingHelpInfo TaskbarAlignLeft = H(
        L("任务栏图标靠左对齐。", "Align taskbar icons to the left."), L("TaskbarAl=0。", "TaskbarAl=0."), L("类似 Win10 布局。", "Win10-like layout."), L("Win11 22H2+ 有效。", "Effective on Win11 22H2+."), L("重启资源管理器后生效。", "Takes effect after Explorer restart."), W10De);
    public static readonly SettingHelpInfo TaskbarCombineAlways = H(
        L("任务栏按钮始终合并。", "Always combine taskbar buttons."), L("TaskbarGlomLevel=0。", "TaskbarGlomLevel=0."), L("节省任务栏空间。", "Saves taskbar space."), L("关闭则从不合并。", "Turning off never combines."), L("重启资源管理器后生效。", "Takes effect after Explorer restart."), W10De);
    public static readonly SettingHelpInfo TaskbarAutoHide = H(
        L("任务栏显示方式：一直显示或自动隐藏。", "Taskbar display: always show or auto-hide."), L("StuckRects3 Settings。", "StuckRects3 Settings."), L("自动隐藏可最大化屏幕空间。", "Auto-hide maximizes screen space."), L("鼠标移至边缘临时显示。", "Move mouse to the edge to show temporarily."), L("重启资源管理器后生效。", "Takes effect after Explorer restart."), W10De);
    public static readonly SettingHelpInfo ShowTaskViewButton = H(
        L("任务栏显示任务视图按钮。", "Show the Task View button on the taskbar."), L("ShowTaskViewButton=1。", "ShowTaskViewButton=1."), L("多桌面/任务概览。", "Virtual desktops / task overview."), L("不用时可关闭。", "Turn off if unused."), L("重启资源管理器后生效。", "Takes effect after Explorer restart."), W10De);
    public static readonly SettingHelpInfo TaskbarEndTask = H(
        L("任务栏右键可结束任务。", "End task from the taskbar context menu."), L("EndTask=1。", "EndTask=1."), L("快速结束无响应用。", "Quickly kill hung apps."), L("内网桌面实用。", "Handy on LAN desktops."), L("重启资源管理器后生效。", "Takes effect after Explorer restart."), W10De);
    public static readonly SettingHelpInfo DisableWidgets = H(
        L("关闭任务栏小组件/资讯。", "Disable taskbar Widgets / news."), L("TaskbarDa=0。", "TaskbarDa=0."), L("减少干扰与占用。", "Less distraction and resource use."), L("Win11 有效。", "Effective on Win11."), L("重启资源管理器后生效。", "Takes effect after Explorer restart."), W10De);
    public static readonly SettingHelpInfo DisableSearchHighlights = H(
        L("关闭搜索框动态亮点/推荐。", "Disable search-box highlights / recommendations."), L("IsDynamicSearchBoxEnabled=0。", "IsDynamicSearchBoxEnabled=0."), L("搜索更纯净。", "Cleaner search UI."), L("推荐开启。", "Recommended on."), L("立即或重启资源管理器。", "Immediate or after Explorer restart."), W10De);
    public static readonly SettingHelpInfo DisableSearchBoxSuggestions = H(
        L("去除搜索界面信息流与搜索框建议。", "Remove search UI feed and search-box suggestions."),
        L("HKCU\\SOFTWARE\\Policies\\Microsoft\\Windows\\Explorer DisableSearchBoxSuggestions=1。",
            "HKCU\\SOFTWARE\\Policies\\Microsoft\\Windows\\Explorer DisableSearchBoxSuggestions=1."),
        L("任务栏/开始搜索不再推新闻与建议内容。", "Taskbar/Start search no longer pushes news or suggestions."),
        L("桌面强烈推荐开启。", "Strongly recommended on desktops."),
        L("注销或重启资源管理器后完全生效。", "Fully applies after sign-out or Explorer restart."),
        W10De,
        recommend: RecommendLevel.Must);
    public static readonly SettingHelpInfo DisableRecommendedItems = H(
        L("开始菜单不显示推荐项目。", "Hide recommended items on Start."), L("Start_ShowRecentRecommendations=0。", "Start_ShowRecentRecommendations=0."), L("减少开始菜单干扰。", "Less Start menu clutter."), L("个人桌面推荐。", "Recommended for personal desktops."), L("重启资源管理器后生效。", "Takes effect after Explorer restart."), W10De);
    public static readonly SettingHelpInfo DisableAdTracking = H(
        L("关闭广告标识符跟踪。", "Disable advertising ID tracking."), L("AdvertisingInfo\\Enabled=0。", "AdvertisingInfo\\Enabled=0."), L("本机不再向广告标识符上报。", "This PC no longer reports an advertising ID."), L("推荐开启。", "Recommended on."), L("立即生效。", "Takes effect immediately."), W10De);
    public static readonly SettingHelpInfo DisableSearchHistory = H(
        L("关闭 Windows 搜索历史记录。", "Disable Windows Search history."), L("HistoryViewEnabled=0。", "HistoryViewEnabled=0."), L("减少本地搜索痕迹。", "Fewer local search traces."), L("推荐开启。", "Recommended on."), L("立即生效。", "Takes effect immediately."), W10De);
    public static readonly SettingHelpInfo DisableStickyKeys = H(
        L("禁用粘滞键 / 筛选键 / 切换键热键提示。", "Disable Sticky Keys / Filter Keys / Toggle Keys hotkey prompts."),
        L("StickyKeys Flags=506；Keyboard Response=2；ToggleKeys=34。", "StickyKeys Flags=506; Keyboard Response=2; ToggleKeys=34."),
        L("避免误触 Shift 五次弹窗，并对齐 Duck 辅助功能键盘热键关闭。", "Stops Shift×5 popups; aligns with Duck accessibility keyboard hotkey off."),
        L("需要辅助功能快捷键的用户请勿开启。", "Do not enable if you need accessibility hotkeys."),
        L("立即或注销后生效。", "Takes effect immediately or after logoff."),
        W10De);
    public static readonly SettingHelpInfo DisablePca = H(
        L("禁用程序兼容性助手服务。", "Disable the Program Compatibility Assistant service."), L("停止 PcaSvc。", "Stop PcaSvc."), L("减少兼容性弹窗。", "Fewer compatibility popups."), L("极老软件排查时可关闭本项。", "Turn this off when troubleshooting very old software."), L("服务停止后生效。", "Takes effect after the service stops."));
    public static readonly SettingHelpInfo PauseFeatureUpdatesUntil2035 = H(
        L("暂停功能更新至 2035 年。", "Pause feature updates until 2035."), L("PauseFeatureUpdates 策略。", "PauseFeatureUpdates policy."), L("长期跳过功能版升级。", "Skip feature upgrades long-term."), L("安全更新仍可能推送；请自行评估风险。", "Security updates may still arrive; assess the risk yourself."), L("策略写入后生效。", "Takes effect after the policy is written."), W10De,
        recommend: RecommendLevel.Optional);
    public static readonly SettingHelpInfo PauseWindowsUpdatesUx = H(
        L("通过更新界面设置长期暂停功能更新与质量更新（至约 2099）。", "Long-pause feature and quality updates via Update UX settings (~2099)."),
        L("HKLM\\…\\WindowsUpdate\\UX\\Settings 的 Pause*Start/End/ExpiryTime。", "HKLM\\…\\WindowsUpdate\\UX\\Settings Pause*Start/End/ExpiryTime."),
        L("对应「设置 → Windows 更新 → 暂停更新」的底层键值，比仅停功能更新更彻底。", "Same keys as Settings → Windows Update → Pause updates; more thorough than feature-pause alone."),
        L("长期不装安全补丁有风险；仅建议隔离/内网机。与「暂停功能更新至 2035」策略项可并存。", "Risky without security patches long-term; isolated/LAN only. Can coexist with Pause feature updates until 2035."),
        L("写入后打开 Windows 更新页可见暂停状态。", "Pause state visible on Windows Update page after write."),
        W10De,
        uiPlace: L("设置 → Windows 更新 → 暂停更新", "Settings → Windows Update → Pause updates"),
        recommend: RecommendLevel.Optional);

    public static readonly SettingHelpInfo HideProtectedOsFiles = H(
        L("隐藏受保护的操作系统文件。", "Hide protected operating system files."), L("ShowSuperHidden=0。", "ShowSuperHidden=0."), L("避免误删系统文件。", "Helps avoid deleting system files by mistake."), L("一般建议开启。", "Generally recommended on."), L("重启资源管理器后生效。", "Takes effect after Explorer restart."), W10De);
    public static readonly SettingHelpInfo AlwaysShowIconsNeverThumbnails = H(
        L("始终显示图标，不生成缩略图。", "Always show icons; do not generate thumbnails."), L("IconsOnly=1。", "IconsOnly=1."), L("减少磁盘缓存、列表更快。", "Less thumbnail cache; faster lists."), L("看图场景可关闭。", "Turn off when browsing pictures."), L("重启资源管理器后生效。", "Takes effect after Explorer restart."), W10De);
    public static readonly SettingHelpInfo ShowEmptyDrives = H(
        L("显示没有介质的空驱动器。", "Show empty drives with no media."), L("HideDrivesWithNoMedia=0。", "HideDrivesWithNoMedia=0."), L("读卡器/空光驱可见。", "Card readers / empty optical drives stay visible."), L("桌面按需。", "Optional for desktops."), L("重启资源管理器后生效。", "Takes effect after Explorer restart."), W10De);
    public static readonly SettingHelpInfo ShowRecentFiles = H(
        L("开始屏幕 / 快速访问显示最近使用的文件。", "Show recently used files on Start / Quick access."),
        L("ShowRecent 与 Start_TrackDocs 同步（对齐「开始屏幕不显示/恢复最近使用的文件」.reg）。", "Syncs ShowRecent and Start_TrackDocs (same as common recent-files .reg)."),
        L("方便找回文件；关闭后开始菜单与快速访问不再跟踪最近文档。", "Easier to find files; when off, Start/Quick access stop tracking recent docs."),
        L("隐私场景可关闭本项（开关关闭=不显示）。", "Turn off for privacy (switch off = hide)."),
        L("立即或重启资源管理器后生效。", "Takes effect immediately or after Explorer restart."),
        W10De,
        uiPlace: L("设置 → 个性化 → 开始 → 显示最近打开的项目", "Settings → Personalization → Start → Show recently opened items"),
        whenHint: L("注重隐私时可关闭", "Turn off for privacy"));
    public static readonly SettingHelpInfo NotepadWordWrap = H(
        L("经典记事本默认开启自动换行。", "Classic Notepad wraps lines by default."),
        L("HKCU\\Software\\Microsoft\\Notepad\\fWrap=1（对齐「设置记事本默认为自动换行方式」.reg）。", "HKCU\\Software\\Microsoft\\Notepad\\fWrap=1 (same as common wrap .reg)."),
        L("打开长文本不必横向滚动。", "Long text without horizontal scrolling."),
        L("仅影响系统自带 notepad.exe；微软商店新版记事本用应用内设置。", "Only classic notepad.exe; Store Notepad uses in-app settings."),
        L("新打开的记事本窗口生效。", "Applies to newly opened Notepad windows."),
        W10De,
        uiPlace: L("记事本 → 格式 → 自动换行", "Notepad → Format → Word Wrap"),
        whenHint: L("常用记事本看日志/说明时可开", "Enable if you often read logs/notes in Notepad"),
        recommend: RecommendLevel.Suggested);
    public static readonly SettingHelpInfo NotepadStatusBar = H(
        L("经典记事本默认显示状态栏（行号/列号等）。", "Classic Notepad shows the status bar by default (line/col)."),
        L("HKCU\\Software\\Microsoft\\Notepad\\StatusBar=1（对齐「设置记事本显示状态栏」.reg）。", "HKCU\\Software\\Microsoft\\Notepad\\StatusBar=1 (same as common status-bar .reg)."),
        L("打开文本时底部可见光标位置，便于对照行号。", "Cursor position at the bottom for line checks."),
        L("仅影响系统自带 notepad.exe；商店版记事本用应用内设置。开启自动换行时经典记事本可能隐藏状态栏。", "Only classic notepad.exe; Store Notepad uses in-app settings. Classic may hide status bar when wrap is on."),
        L("新打开的记事本窗口生效。", "Applies to newly opened Notepad windows."),
        W10De,
        uiPlace: L("记事本 → 查看 → 状态栏", "Notepad → View → Status Bar"),
        whenHint: L("常用记事本对照行号时可开", "Enable if you check line numbers in Notepad"),
        recommend: RecommendLevel.Suggested);
    public static readonly SettingHelpInfo ShowFrequentPlaces = H(
        L("快速访问显示常用文件夹。", "Show frequent folders in Quick access."), L("Explorer\\ShowFrequent。", "Explorer\\ShowFrequent."), L("常用目录更快。", "Faster access to frequent folders."), L("隐私场景可关闭。", "Turn off for privacy."), L("立即生效。", "Takes effect immediately."), W10De);
    public static readonly SettingHelpInfo HideOfficeCloudFiles = H(
        L("快速访问不显示 office.com 云文件。", "Hide office.com cloud files from Quick access."), L("ShowCloudFilesInQuickAccess=0。", "ShowCloudFilesInQuickAccess=0."), L("减少云内容混入。", "Less cloud content mixed into Quick access."), L("不用 Microsoft 365 可开启。", "Enable if you do not use Microsoft 365."), L("立即生效。", "Takes effect immediately."), W10De);
    public static readonly SettingHelpInfo DisableOneDrive = H(
        L("策略禁止 OneDrive 文件同步。", "Policy-block OneDrive file sync."), L("DisableFileSyncNGSC=1。", "DisableFileSyncNGSC=1."), L("去掉网盘占用。", "Removes OneDrive overhead."), L("仍需 OneDrive 时勿开。", "Do not enable if you still need OneDrive."), L("注销后完全生效。", "Fully applies after logoff."), W10De);
    public static readonly SettingHelpInfo HideTaskbarChat = H(
        L("隐藏任务栏聊天按钮。", "Hide the taskbar Chat button."), L("TaskbarMn=0。", "TaskbarMn=0."), L("任务栏更干净。", "Cleaner taskbar."), L("Win11 有效。", "Effective on Win11."), L("重启资源管理器后生效。", "Takes effect after Explorer restart."), W10De);
    public static readonly SettingHelpInfo HideTaskbarCopilot = H(
        L("隐藏任务栏 Copilot。", "Hide Copilot on the taskbar."), L("TaskbarCo=0。", "TaskbarCo=0."), L("减少入口干扰。", "Fewer distracting entry points."), L("Win11 有效。", "Effective on Win11."), L("重启资源管理器后生效。", "Takes effect after Explorer restart."), W10De);
    public static readonly SettingHelpInfo DisableCloudSearch = H(
        L("禁止搜索界面云内容搜索。", "Disable cloud content search in Search."), L("AllowCloudSearch=0。", "AllowCloudSearch=0."), L("搜索不查 OneDrive/SharePoint 等。", "Search does not query OneDrive/SharePoint, etc."), L("推荐隐私场景。", "Recommended for privacy."), L("立即生效。", "Takes effect immediately."), W10De);
    public static readonly SettingHelpInfo DisableWebsiteLangList = H(
        L("禁止网站读取语言列表做本地化推荐。", "Block websites from reading your language list for localization."), L("HttpAcceptLanguageOptOut=1。", "HttpAcceptLanguageOptOut=1."), L("减少指纹。", "Less fingerprinting."), L("网页语言可能不自动匹配。", "Web pages may not auto-match language."), L("立即生效。", "Takes effect immediately."));
    public static readonly SettingHelpInfo DisableAppLaunchTracking = H(
        L("不跟踪应用启动以改进开始菜单搜索。", "Do not track app launches to improve Start search."), L("Start_TrackProgs=0。", "Start_TrackProgs=0."), L("减少本地画像。", "Less local profiling."), L("开始菜单推荐会变弱。", "Start recommendations get weaker."), L("立即生效。", "Takes effect immediately."), W10De);
    public static readonly SettingHelpInfo DisableSettingsSuggestions = H(
        L("设置应用不显示建议内容。", "Hide suggestions in the Settings app."), L("SystemPaneSuggestionsEnabled=0。", "SystemPaneSuggestionsEnabled=0."), L("设置页更干净。", "Cleaner Settings pages."), L("推荐关闭建议。", "Recommended to hide suggestions."), L("立即生效。", "Takes effect immediately."), W10De);
    public static readonly SettingHelpInfo DisableInkingPersonalization = H(
        L("关闭墨迹与键入个性化词典，并禁止语言数据收集。", "Disable inking/typing personalization and linguistic data collection."),
        L("HKLM/HKCU RestrictImplicitInk/TextCollection；AllowLinguisticDataCollection=0。", "HKLM/HKCU RestrictImplicitInk/TextCollection; AllowLinguisticDataCollection=0."),
        L("减少输入/手写上传与本地画像。", "Less typing/handwriting upload and local profiling."),
        L("手写识别与云词库可能变弱。", "Handwriting recognition / cloud lexicon may weaken."),
        L("立即生效。", "Takes effect immediately."),
        uiPlace: L("设置 → 隐私和安全性 → 墨迹书写和键入个性化", "Settings → Privacy & security → Inking & typing personalization"));
    public static readonly SettingHelpInfo MsPinyinDefaultEnglish = H(
        L("微软拼音新建文档/窗口默认英文键盘，需手动 Shift 切中文。", "Microsoft Pinyin defaults to English; press Shift for Chinese."),
        L("HKCU …\\InputMethod\\Settings\\CHS「Default Mode」=1。", "HKCU …\\InputMethod\\Settings\\CHS Default Mode=1."),
        L("减少误输中文，适合开发与英文为主场景。", "Fewer accidental Chinese inputs; good for coding/English-first work."),
        L("仅影响微软拼音；第三方输入法不受影响。", "Affects Microsoft Pinyin only; third-party IMEs unchanged."),
        L("立即生效；新开输入窗口更明显。", "Takes effect immediately; clearer in new input windows."),
        W10De,
        uiPlace: L("设置 → 时间和语言 → 输入 → 微软拼音 → 常规 → 默认输入模式", "Settings → Time & language → Typing → Microsoft Pinyin → General → Default mode"));
    public static readonly SettingHelpInfo DisableMsPinyinCloudAndInsights = H(
        L("关闭微软拼音云候选、输入见解、多语言与硬件键盘文本预测。", "Disable Microsoft Pinyin cloud candidates, typing insights, multilingual, and hardware keyboard prediction."),
        L("Enable Cloud Candidate / InsightsEnabled / EnableTypingInsights / MultilingualEnabled 等。", "Enable Cloud Candidate / InsightsEnabled / EnableTypingInsights / MultilingualEnabled, etc."),
        L("停止热词、网络流行语、表情预测与输入习惯上传。", "Stops hot words, slang, emoji prediction, and typing-habit upload."),
        L("候选词仅用本地词典，可能少一些流行语。", "Candidates use local lexicon only; fewer trendy words."),
        L("立即生效；部分项需切换输入法或重开窗口。", "Takes effect immediately; some need IME switch or new windows."),
        W10De,
        uiPlace: L("设置 → 时间和语言 → 输入 / 键入见解", "Settings → Time & language → Typing / Typing insights"));
    public static readonly SettingHelpInfo DisableMsPinyinToolbar = H(
        L("关闭微软拼音候选窗工具条，并隐藏语言栏帮助按钮。", "Hide Microsoft Pinyin candidate toolbar and language-bar Help."),
        L("ToolBarEnabled=0；LangBar DemoteLevel=3。", "ToolBarEnabled=0; LangBar DemoteLevel=3."),
        L("去掉桌面左上角浮动条与多余帮助入口。", "Removes the floating toolbar and extra Help entry."),
        L("需要工具条快捷按钮时请关闭本项。", "Turn this off if you need toolbar shortcut buttons."),
        L("立即生效。", "Takes effect immediately."),
        W10De);
    public static readonly SettingHelpInfo ExcludeMsrtFromWu = H(
        L("Windows 更新不含恶意软件删除工具。", "Exclude the Malicious Software Removal Tool from Windows Update."), L("MRT DontOfferThroughWUAU=1。", "MRT DontOfferThroughWUAU=1."), L("减少每月 MSRT 包。", "Fewer monthly MSRT packages."), L("需自行维护杀软。", "Maintain your own antivirus."), L("下次更新扫描生效。", "Applies on the next update scan."));
    public static readonly SettingHelpInfo DisableMeltdownSpectre = H(
        L("关闭 Meltdown/Spectre 微码缓解。", "Disable Meltdown/Spectre mitigations."), L("FeatureSettingsOverride=3。", "FeatureSettingsOverride=3."), L("部分旧 CPU 可提升性能。", "May improve performance on some older CPUs."),         L("降低侧信道防护，仅可信内网且知情同意；不作为常规优化。", "Weaker side-channel protection; trusted LAN only — not routine optimize."), L("需重启。", "Requires reboot."),
        recommend: RecommendLevel.Optional);
    public static readonly SettingHelpInfo DisableMemoryIntegrity = H(
        L("关闭内存完整性（HVCI）。", "Disable Memory Integrity (HVCI)."), L("HypervisorEnforcedCodeIntegrity Enabled=0。", "HypervisorEnforcedCodeIntegrity Enabled=0."), L("减少虚拟化开销、兼容部分驱动。", "Less virtualization overhead; better for some drivers."), L("降低内核防护；属高级安全兼容项，非性能一键优化。", "Weaker kernel protection; advanced compatibility — not one-click perf."), L("需重启。", "Requires reboot."), W10,
        recommend: RecommendLevel.Optional);
    public static readonly SettingHelpInfo DisableWdac = H(
        L("关闭 WDAC 应用控制策略部署。", "Disable WDAC application control policy deployment."), L("ConfigCIPolicyEnable=0。", "ConfigCIPolicyEnable=0."), L("避免企业策略误拦程序。", "Avoid enterprise policies blocking apps by mistake."), L("有合规 WDAC 时勿开；不作为常规优化。", "Do not enable if you need compliance WDAC; not routine optimize."), L("需重启。", "Requires reboot."), W10,
        recommend: RecommendLevel.Optional);
    public static readonly SettingHelpInfo DisableVbs = H(
        L("强制关闭基于虚拟化的安全性。", "Force-disable Virtualization-based Security."), L("EnableVirtualizationBasedSecurity=0。", "EnableVirtualizationBasedSecurity=0."), L("减少 VBS 性能损耗。", "Less VBS performance cost."), L("Credential Guard/HVCI 将不可用；高级项，勿一键全开。", "Credential Guard / HVCI unavailable; advanced — not one-click."), L("需重启。", "Requires reboot."), W10,
        recommend: RecommendLevel.Optional);
    public static readonly SettingHelpInfo EnableTcpBbr2 = H(
        L("TCP 拥塞控制改用 BBR2。", "Use BBR2 for TCP congestion control."), L("netsh int tcp set supplemental CongestionProvider=bbr2。", "netsh int tcp set supplemental CongestionProvider=bbr2."), L("部分广域网吞吐更好。", "Better throughput on some WAN links."), L("旧系统或不支持时会失败并保持 CUBIC；与 CTCP 互斥，属高级网络项。", "Fails on unsupported OS and keeps CUBIC; mutually exclusive with CTCP; advanced."), L("立即生效。", "Takes effect immediately."), W10,
        recommend: RecommendLevel.Suggested);
    public static readonly SettingHelpInfo EnableTcpCtcp = H(
        L("TCP 拥塞控制改用 CTCP（Compound TCP）。", "Use CTCP (Compound TCP) for congestion control."),
        L("netsh int tcp set supplemental template=internet congestionprovider=ctcp。", "netsh int tcp set supplemental template=internet congestionprovider=ctcp."),
        L("高带宽高延迟链路下吞吐往往优于默认 CUBIC；局域网/NAS 媒体访问体感可能更跟手。", "Often better throughput on high-BDP links than default CUBIC; LAN/NAS media may feel snappier."),
        L("Wi‑Fi/不稳定链路请自行对比；与 BBR2 互斥（开启本项会覆盖 BBR2）。不支持时保持原算法。", "Benchmark on Wi‑Fi/unstable links; mutually exclusive with BBR2 (this overrides BBR2). Unsupported OS keeps previous provider."),
        L("立即生效（当前 Internet 模板）。", "Takes effect immediately (Internet template)."),
        W10,
        recommend: RecommendLevel.Suggested);
    public static readonly SettingHelpInfo DisableSystemRestore = H(
        L("禁用系统还原。", "Disable System Restore."),
        L("DisableSR=1。", "DisableSR=1."),
        L("节省还原点磁盘。", "Saves restore-point disk space."),
        L("个人桌面/磁盘紧张时强烈推荐；需要系统还原点回滚时请保持开启。", "Strongly recommended for personal desktops / low disk; keep on if you need restore-point rollback."),
        L("立即生效。", "Takes effect immediately."),
        recommend: RecommendLevel.Must);
    public static readonly SettingHelpInfo DisableCeip = H(
        L("关闭微软客户体验改善计划。", "Disable Microsoft Customer Experience Improvement Program."), L("CEIPEnable=0。", "CEIPEnable=0."), L("减少遥测。", "Less telemetry."), L("与关闭 DiagTrack 互补。", "Complements disabling DiagTrack."), L("立即生效。", "Takes effect immediately."));
    public static readonly SettingHelpInfo DisableDiagnosticPolicy = H(
        L("禁用诊断策略服务 DPS。", "Disable the Diagnostic Policy Service (DPS)."), L("DPS Start=disabled。", "DPS Start=disabled."), L("减少后台诊断。", "Less background diagnostics."), L("故障排查向导可能不可用。", "Troubleshooting wizards may be unavailable."), L("服务停止后生效。", "Takes effect after the service stops."));
    public static readonly SettingHelpInfo DisableRemoteAssistance = H(
        L("禁止远程协助。", "Disable Remote Assistance."), L("fAllowToGetHelp=0。", "fAllowToGetHelp=0."), L("缩小远程协助攻击面。", "Smaller Remote Assistance attack surface."), L("需要远程协助时勿开。", "Do not enable if you need Remote Assistance."), L("立即生效。", "Takes effect immediately."));
    public static readonly SettingHelpInfo DisableMemoryCompression = H(
        L("关闭内存压缩。", "Disable memory compression."), L("Disable-MMAgent MemoryCompression。", "Disable-MMAgent MemoryCompression."), L("减少压缩 CPU 占用。", "Less CPU used for compression."), L("内存紧张时可能更易用页文件。", "Low-RAM systems may use the page file more."), L("立即生效。", "Takes effect immediately."), W10);
    public static readonly SettingHelpInfo DisableAppPrelaunch = H(
        L("关闭应用预启动。", "Disable application prelaunch."), L("Disable-MMAgent ApplicationPreLaunch。", "Disable-MMAgent ApplicationPreLaunch."), L("减少预热占用。", "Less warm-up resource use."), L("UWP 首次打开可能稍慢。", "First UWP launches may be slightly slower."), L("立即生效。", "Takes effect immediately."), W10De);
    public static readonly SettingHelpInfo DisablePageCombining = H(
        L("关闭内存页面合并。", "Disable memory page combining."), L("Disable-MMAgent PageCombining。", "Disable-MMAgent PageCombining."), L("减少合并扫描。", "Less combining scan work."), L("内存占用可能略增。", "Memory use may rise slightly."), L("立即生效。", "Takes effect immediately."), W10);
    public static readonly SettingHelpInfo DisableUcpdDriver = H(
        L("禁用微软用户选择保护驱动 UCPD。", "Disable the Microsoft User Choice Protection Driver (UCPD)."), L("UCPD 服务禁用。", "Disable the UCPD service."), L("便于修改默认浏览器/关联。", "Easier to change default browser/associations."), L("仅在需要改默认应用时开启。", "Enable only when changing default apps."), L("服务停止后生效。", "Takes effect after the service stops."), W10);

    public static readonly SettingHelpInfo DisableCortana = H(
        L("关闭 Cortana。", "Disable Cortana."), L("AllowCortana=0。", "AllowCortana=0."), L("减少语音助手后台。", "Less voice-assistant background work."), L("对齐 Optimizer DisableCortana。", "Aligns with Optimizer DisableCortana."), L("注销后完全生效。", "Fully applies after logoff."), W10De);
    public static readonly SettingHelpInfo DisableCopilotAi = H(
        L("关闭 Windows / Edge Copilot。", "Disable Windows / Edge Copilot."), L("TurnOffWindowsCopilot + Edge HubsSidebar。", "TurnOffWindowsCopilot + Edge HubsSidebar."), L("去掉 AI 侧栏占用。", "Removes AI sidebar overhead."), L("对齐 Optimizer DisableCoPilotAI。", "Aligns with Optimizer DisableCoPilotAI."), L("重启资源管理器或 Edge 后生效。", "Takes effect after Explorer or Edge restart."), W10De);
    public static readonly SettingHelpInfo DisableOfficeTelemetry = H(
        L("关闭 Office 遥测上传。", "Disable Office telemetry uploads."), L("Office ClientTelemetry / OSM 策略。", "Office ClientTelemetry / OSM policies."), L("减少 Office 后台上报。", "Less Office background reporting."), L("需已安装 Office 2016+。", "Requires Office 2016+ installed."), L("重新打开 Office 后生效。", "Takes effect after reopening Office."));
    public static readonly SettingHelpInfo EnableUtcTime = H(
        L("硬件时钟使用 UTC（双系统）。", "Use UTC for the hardware clock (dual-boot)."), L("RealTimeIsUniversal=1。", "RealTimeIsUniversal=1."), L("与 Linux 双系统时间一致。", "Keeps time consistent with Linux dual-boot."), L("仅 Windows+Linux 双系统需要；纯 Windows 请保持关闭。", "Only for Windows+Linux dual-boot; keep off on Windows-only."), L("立即生效。", "Takes effect immediately."));
    public static readonly SettingHelpInfo DisableHpet = H(
        L("关闭 HPET 高精度事件计时器。", "Disable the HPET high-precision event timer."), L("bcdedit useplatformclock / disabledynamictick。", "bcdedit useplatformclock / disabledynamictick."), L("部分游戏/延迟场景可能改善。", "May help some games/latency cases."), L("可能影响多媒体时钟，不确定勿开。", "May affect multimedia clocks; skip if unsure."), L("需重启。", "Requires reboot."));
    public static readonly SettingHelpInfo EnableLoginVerbose = H(
        L("登录时显示详细状态信息。", "Show verbose status messages at sign-in."), L("VerboseStatus=1。", "VerboseStatus=1."), L("便于排查启动卡住的服务。", "Easier to diagnose hung startup services."), L("运维机推荐。", "Recommended on ops machines."), L("下次登录生效。", "Takes effect on next logon."));
    public static readonly SettingHelpInfo DisableNetworkThrottling = H(
        L("关闭多媒体网络节流。", "Disable multimedia network throttling."), L("NetworkThrottlingIndex=0xFFFFFFFF。", "NetworkThrottlingIndex=0xFFFFFFFF."), L("提高后台网络吞吐。", "Higher background network throughput."),
        L("对齐 Optimizer / Duck；完整 MMCSS 请另开「优化多媒体调度」。", "Aligns with Optimizer/Duck; for full MMCSS also enable Optimize multimedia scheduler."), L("立即生效。", "Takes effect immediately."));
    public static readonly SettingHelpInfo OptimizeMultimediaScheduler = H(
        L("优化多媒体类调度服务 (MMCSS)。", "Optimize the Multimedia Class Scheduler Service (MMCSS)."),
        L("SystemProfile：NoLazyMode/AlwaysOn/SystemResponsiveness=10；Tasks\\Games 高优先级。", "SystemProfile: NoLazyMode/AlwaysOn/SystemResponsiveness=10; Tasks\\Games high priority."),
        L("降低多媒体与前台交互延迟，对齐 optimizerDuck OptimizeMultimediaScheduler。", "Lower multimedia/UI latency; aligns with optimizerDuck OptimizeMultimediaScheduler."),
        L("与「关闭多媒体网络节流」互补；游戏/低延迟场景推荐。", "Complements Disable multimedia network throttling; recommended for games/low latency."),
        L("立即生效，部分场景建议注销或重启。", "Takes effect immediately; some cases benefit from logoff/reboot."));
    public static readonly SettingHelpInfo OptimizeKeyboardLatency = H(
        L("优化键盘重复延迟与速度。", "Optimize keyboard repeat delay and rate."),
        L("KeyboardDelay=0；KeyboardSpeed=31。", "KeyboardDelay=0; KeyboardSpeed=31."),
        L("缩短按键重复等待，对齐 Duck KeyboardLatencyOptimization。", "Shorter key-repeat wait; aligns with Duck KeyboardLatencyOptimization."),
        L("全局生效。", "Applies globally."),
        L("立即生效。", "Takes effect immediately."));
    public static readonly SettingHelpInfo LiftWebDavFileSizeLimit = H(
        L("放开 WebDAV 单文件大小上限。", "Lift the WebDAV single-file size limit."),
        L("WebClient\\Parameters FileSizeLimitInBytes=0xFFFFFFFF（约 4GB）。", "WebClient\\Parameters FileSizeLimitInBytes=0xFFFFFFFF (~4GB)."),
        L("映射网络驱动器/WebDAV 大文件传输不再被默认 50MB 卡住。", "Mapped drive/WebDAV large transfers are no longer stuck at the default ~50MB."),
        L("仅在使用 WebDAV/WebClient 时有意义。", "Only meaningful when using WebDAV/WebClient."),
        L("立即生效，可能需重启 WebClient 服务。", "Takes effect immediately; may need WebClient service restart."));
    public static readonly SettingHelpInfo DisableGameDvr = H(
        L("关闭游戏栏 / Game DVR。", "Disable Game Bar / Game DVR."), L("AllowGameDVR=0。", "AllowGameDVR=0."), L("减少录制与 Xbox 叠加层。", "Less recording and Xbox overlay."), L("不玩游戏可开。", "Enable if you do not play games."), L("立即生效。", "Takes effect immediately."), W10De);
    public static readonly SettingHelpInfo DisableLocationTracking = H(
        L("禁止定位服务。", "Disable location services."), L("DisableLocation=1。", "DisableLocation=1."), L("减少位置上传。", "Less location upload."), L("地图/天气定位会失效。", "Maps/weather location will stop working."), L("立即生效。", "Takes effect immediately."));
    public static readonly SettingHelpInfo DisableConsumerFeatures = H(
        L("关闭 Windows 消费者体验推送。", "Disable Windows Consumer Features pushes."), L("DisableWindowsConsumerFeatures=1。", "DisableWindowsConsumerFeatures=1."), L("减少预装建议应用。", "Fewer suggested/preinstalled apps."), L("对齐 SophiApp / Optimizer。", "Aligns with SophiApp / Optimizer."), L("注销后生效。", "Takes effect after logoff."), W10De);
    public static readonly SettingHelpInfo DisableEdgePreload = H(
        L("禁止 Edge 预启动与后台模式。", "Disable Edge prelaunch and background mode."), L("StartupBoostEnabled=0。", "StartupBoostEnabled=0."), L("减少闲时内存。", "Less idle memory use."), L("首次打开 Edge 会稍慢。", "First Edge launch may be slightly slower."), L("立即生效。", "Takes effect immediately."));
    public static readonly SettingHelpInfo DisableTeredo = H(
        L("禁用 Teredo IPv6 隧道。", "Disable the Teredo IPv6 tunnel."), L("netsh teredo disabled。", "netsh teredo disabled."), L("减少无用隧道与扫描面。", "Less unused tunneling and scan surface."), L("需 IPv6 穿越 NAT 时勿开。", "Do not enable if you need IPv6 NAT traversal."), L("立即生效。", "Takes effect immediately."));
    public static readonly SettingHelpInfo DisableClipboardCloud = H(
        L("关闭剪贴板云同步与跨设备。", "Disable clipboard cloud sync / cross-device."), L("AllowCrossDeviceClipboard=0。", "AllowCrossDeviceClipboard=0."), L("剪贴板内容不上传。", "Clipboard contents are not uploaded."), L("对齐 Optimizer DisableCloudClipboard。", "Aligns with Optimizer DisableCloudClipboard."), L("立即生效。", "Takes effect immediately."), W10);
    public static readonly SettingHelpInfo DisableNtfsLastAccess = H(
        L("关闭 NTFS 最后访问时间戳。", "Disable NTFS last-access timestamps."), L("fsutil disablelastaccess。", "fsutil disablelastaccess."), L("降低磁盘元数据写入。", "Less disk metadata write."), L("对齐 Optimizer DisableNTFSTimeStamp。", "Aligns with Optimizer DisableNTFSTimeStamp."), L("立即生效。", "Takes effect immediately."));
    public static readonly SettingHelpInfo DisableXboxServices = H(
        L("禁用 Xbox Live 相关服务。", "Disable Xbox Live related services."), L("XblAuthManager 等。", "XblAuthManager, etc."), L("无 Xbox 时减少后台。", "Less background work without Xbox."), L("商店游戏/Xbox 应用会受影响。", "Store games / Xbox apps will be affected."), L("服务停止后生效。", "Takes effect after the service stops."), W10De);
    public static readonly SettingHelpInfo DisableFaxService = H(
        L("禁用传真服务。", "Disable the Fax service."), L("Fax 服务禁用。", "Disable the Fax service."), L("几乎无人用传真时可关。", "OK off when fax is unused."), L("需要传真时勿开。", "Do not enable if you need fax."), L("服务停止后生效。", "Takes effect after the service stops."));
    public static readonly SettingHelpInfo EnableF8BootMenu = H(
        L("启用传统 F8 高级启动菜单。", "Enable the legacy F8 advanced boot menu."), L("bcdedit bootmenupolicy legacy。", "bcdedit bootmenupolicy legacy."), L("开机可进安全模式菜单。", "Access Safe Mode menu at boot."), L("UEFI 机器仍可用 Shift+重启。", "UEFI PCs can still use Shift+Restart."), L("下次开机生效。", "Takes effect on next boot."));
    public static readonly SettingHelpInfo ContextMenuTakeOwnership = H(
        L("右键菜单增加「取得所有权」。", "Add Take Ownership to the context menu."), L("HKCR *\\shell。", "HKCR *\\shell."), L("快速 takeown/icacls。", "Quick takeown/icacls."), L("对齐 Optimizer Integrator。", "Aligns with Optimizer Integrator."), L("立即生效。", "Takes effect immediately."), W10De);
    public static readonly SettingHelpInfo ContextMenuOpenCmd = H(
        L("文件夹右键「在此处打开命令提示符」。", "Folder context menu: Open Command Prompt here."),
        L("写入 Directory\\shell 与 Background（空白处）；关闭时一并清除常见优化包的 Folder\\shell\\OpenDOSBox。", "Writes Directory\\shell and Background; turning off also clears common Folder\\shell\\OpenDOSBox."),
        L("在当前目录快速打开 CMD，运维常用。", "Open CMD in the current folder; common for ops."),
        L("建议开启；已用其它 .reg 加过同类项时，用本开关开关即可统一管理。", "Recommended on; if other .reg added similar items, manage them with this switch."),
        L("立即生效。", "Takes effect immediately."),
        W10De,
        uiPlace: L("资源管理器 → 文件夹 / 空白处右键", "Explorer → folder / empty-area right-click"),
        whenHint: L("需要在目录下开 CMD 时开启", "Enable when you need CMD in a folder"),
        recommend: RecommendLevel.Suggested);
    public static readonly SettingHelpInfo ContextMenuCopyMoveTo = H(
        L("右键菜单增加「复制到文件夹」「移动到文件夹」。", "Add Copy To Folder / Move To Folder to the context menu."),
        L("HKCR AllFilesystemObjects\\shellex\\ContextMenuHandlers\\Copy To / Move To（系统自带 CLSID）。", "HKCR AllFilesystemObjects\\shellex\\ContextMenuHandlers\\Copy To / Move To (built-in CLSIDs)."),
        L("选中文件/文件夹后可一键复制或移动到指定目录，少开资源管理器窗口。", "Copy/move selected items to a chosen folder with fewer Explorer windows."),
        L("桌面整理强烈推荐；不需要可关。", "Strongly recommended for desktop cleanup; turn off if unused."),
        L("立即生效；若菜单未出现可刷新资源管理器。", "Takes effect immediately; refresh Explorer if the menu is missing."),
        W10De,
        uiPlace: L("资源管理器 → 文件/文件夹右键", "Explorer → file/folder right-click"),
        whenHint: L("需要快速复制/移动到其它目录时开启", "Enable for quick copy/move to another folder"),
        recommend: RecommendLevel.Must);
    public static readonly SettingHelpInfo ContextMenuQuickOps = H(
        L("桌面与文件夹空白处右键增加「快捷操作组」级联菜单。", "Add a Quick Ops cascade menu on desktop/folder background right-click."),
        L("HKCR Directory/LibraryFolder\\Background\\shell\\QwhMenu + Explorer CommandStore（对齐添加/删除快捷操作组 .reg）。", "HKCR Directory/LibraryFolder\\Background\\shell\\QwhMenu + Explorer CommandStore (same as quick-ops .reg)."),
        L("一键打开此电脑、控制面板、CMD、记事本、画图、注册表、重启资源管理器等。", "One-click This PC, Control Panel, CMD, Notepad, Paint, Registry, restart Explorer, etc."),
        L("桌面运维强烈推荐；菜单偏多时可关。关闭仅删级联键（与删除 .reg 一致）。", "Strongly recommended for desktop ops; turn off if menus feel crowded. Off only deletes the cascade keys."),
        L("立即生效；若未出现可刷新资源管理器。", "Takes effect immediately; refresh Explorer if it does not appear."),
        W10De,
        uiPlace: L("桌面 / 文件夹空白处右键", "Desktop / folder empty-area right-click"),
        whenHint: L("需要空白处快速入口时开启", "Enable for quick actions on empty area"),
        recommend: RecommendLevel.Must);
    public static readonly SettingHelpInfo DisableMediaPlayerSharing = H(
        L("禁用 Windows Media Player 网络共享。", "Disable Windows Media Player network sharing."), L("WMPNetworkSvc。", "WMPNetworkSvc."), L("减少共享端口。", "Fewer sharing ports."), L("不共享媒体库可开。", "Enable if you do not share media libraries."), L("服务停止后生效。", "Takes effect after the service stops."));
    public static readonly SettingHelpInfo DisableInsiderService = H(
        L("禁用 Windows Insider 服务。", "Disable the Windows Insider service."), L("wisvc。", "wisvc."), L("不参加预览计划时可关。", "OK off if you are not on Insider."), L("Insider 通道将不可用。", "Insider channels will be unavailable."), L("服务停止后生效。", "Takes effect after the service stops."), W10);
    public static readonly SettingHelpInfo DisableStoreAutoUpdate = H(
        L("禁止微软商店自动更新应用。", "Block Microsoft Store auto app updates."), L("WindowsStore AutoDownload=2。", "WindowsStore AutoDownload=2."), L("避免商店应用悄悄更新。", "Stops Store apps updating silently."), L("需手动检查商店更新。", "Check Store updates manually."), L("立即生效。", "Takes effect immediately."), W10De);
    public static readonly SettingHelpInfo DisableNewsInterests = H(
        L("关闭资讯与兴趣/天气动态。", "Disable News and interests / weather feed."), L("AllowNewsAndInterests=0。", "AllowNewsAndInterests=0."), L("任务栏更干净。", "Cleaner taskbar."), L("对齐 Optimizer DisableNewsInterests。", "Aligns with Optimizer DisableNewsInterests."), L("重启资源管理器后生效。", "Takes effect after Explorer restart."), W10De);

    public static readonly SettingHelpInfo DisableBrokenShortcutTracking = H(
        L("禁止资源管理器跟踪或搜索损坏的快捷方式。", "Stop Explorer from tracking/searching broken shortcuts."),
        L("Policies\\Explorer：NoResolveTrack / NoResolveSearch / LinkResolveIgnoreLinkInfo=1。", "Policies\\Explorer: NoResolveTrack / NoResolveSearch / LinkResolveIgnoreLinkInfo=1."),
        L("打开死链时不再联网解析或全盘搜索，少卡顿、少多余网络请求。", "Opening dead links no longer does network resolve or full-disk search."),
        L("桌面快捷方式多、网络一般的环境推荐开启。", "Recommended with many desktop shortcuts or average networks."),
        L("重启资源管理器后完全生效。", "Fully applies after Explorer restart."),
        SettingScope.DesktopExperience);

    public static readonly SettingHelpInfo ExplorerSeparateProcess = H(
        L("每个文件夹窗口使用独立的 explorer 进程。", "Use a separate explorer process per folder window."),
        L("Explorer\\Advanced SeparateProcess=1。", "Explorer\\Advanced SeparateProcess=1."),
        L("单个窗口崩溃不会拖垮整个桌面与任务栏。", "One window crash won't take down desktop/taskbar."),
        L("窗口很多时会多占内存；日常桌面推荐开启。", "More windows use more RAM; recommended for daily desktops."),
        L("新开的资源管理器窗口生效。", "Applies to newly opened Explorer windows."),
        SettingScope.DesktopExperience);

    public static readonly SettingHelpInfo AutoRestartExplorer = H(
        L("资源管理器崩溃后由 Winlogon 自动拉起。", "Winlogon auto-restarts Explorer after a crash."),
        L("Winlogon AutoRestartShell=1（多数系统默认已是 1）。", "Winlogon AutoRestartShell=1 (already 1 on most systems)."),
        L("explorer 异常退出后桌面/任务栏会自己回来，少一次手工重启。", "Desktop/taskbar come back after explorer exits; less manual restart."),
        L("推荐保持开启。", "Recommended to keep on."),
        L("立即写入，下次崩溃时生效。", "Written immediately; applies on next crash."));

    public static readonly SettingHelpInfo HideDesktopSpotlight = H(
        L("隐藏桌面「了解此图片」/ Windows 聚焦图标。", "Hide desktop Learn about this picture / Windows Spotlight icon."),
        L("HideDesktopIcons CLSID {2cc5ca98-6485-489a-920e-b3e88a6ccce3}=1。", "HideDesktopIcons CLSID {2cc5ca98-6485-489a-920e-b3e88a6ccce3}=1."),
        L("桌面少一个推广入口，图标列表更干净。", "One fewer promo icon; cleaner desktop."),
        L("Win11 桌面常见；没有该图标时开着也无害。", "Common on Win11; harmless if the icon is absent."),
        L("重启资源管理器后生效。", "Takes effect after Explorer restart."),
        W10De);

    public static readonly SettingHelpInfo HideDuplicateRemovableDrives = H(
        L("去掉可移动磁盘在「此电脑」里重复出现的一项。", "Remove duplicate removable-drive entries under This PC."),
        L("删除 DelegateFolders\\{F5FB2C77-0E2F-4A16-A381-3E560C68BC83}（键不存在=已优化）。", "Delete DelegateFolders\\{F5FB2C77-0E2F-4A16-A381-3E560C68BC83} (missing key = already optimized)."),
        L("插 U 盘时不再看到两个相同盘符。", "USB sticks no longer show two identical drive letters."),
        L("出现重复盘符时推荐开启。", "Recommended when duplicate letters appear."),
        L("重启资源管理器后生效。", "Takes effect after Explorer restart."),
        SettingScope.DesktopExperience);

    public static readonly SettingHelpInfo DisableRunDialogHistory = H(
        L("「运行」对话框（Win+R）不再记录与展示历史命令。", "Run dialog (Win+R) no longer records/shows command history."),
        L("写入 HKCU\\Software\\SrvDesk\\Tweaks\\DisableRunDialogHistory=1，并清空 Explorer\\RunMRU。不改 Start_TrackProgs（那是「关闭应用启动跟踪」）。", "Writes HKCU\\Software\\SrvDesk\\Tweaks\\DisableRunDialogHistory=1 and clears Explorer\\RunMRU. Does not change Start_TrackProgs."),
        L("共用机器上别人看不到你跑过的命令。", "Others on a shared PC cannot see your Run history."),
        L("经常复用运行历史的可关闭。", "Turn off if you reuse Run history often."),
        L("立即清空；本工具再次应用时会保持清空。关闭只恢复记录，不还原旧命令。", "Clears now; re-Apply keeps it empty. Turning off only resumes recording, not old commands."),
        SettingScope.DesktopExperience);

    public static readonly SettingHelpInfo MergeSvchostProcesses = H(
        L("提高 svchost 拆分阈值，减少服务宿主进程数量。", "Raise svchost split threshold to reduce service-host process count."),
        L("HKLM\\SYSTEM\\CurrentControlSet\\Control SvcHostSplitThresholdInKB=0xFFFFFFFF（不是 LanmanServer 的 SrvSplit）。", "HKLM\\SYSTEM\\CurrentControlSet\\Control SvcHostSplitThresholdInKB=0xFFFFFFFF (not LanmanServer SrvSplit)."),
        L("任务管理器里 svchost 更少，内存碎片略降。", "Fewer svchost entries in Task Manager; slightly less memory fragmentation."),
        L("推荐开启；个别环境需重启后观察服务分组。", "Recommended on; some environments need reboot then check service grouping."),
        L("重启后完全生效。", "Fully applies after reboot."));

    public static readonly SettingHelpInfo DisableDistributedLinkTracking = H(
        L("禁用 NTFS 分布式链接跟踪客户端（TrkWks）。", "Disable NTFS Distributed Link Tracking Client (TrkWks)."),
        L("服务 TrkWks 启动类型=disabled。与「关闭最后访问时间戳」不是同一项。", "Set TrkWks startup=disabled. Different from Disable NTFS last-access timestamps."),
        L("少一个常驻服务；快捷方式跨卷移动后不再自动重定向。", "One fewer resident service; shortcuts no longer auto-redirect across volumes."),
        L("不依赖「移动后快捷方式仍可用」时推荐开启。", "Recommended if you do not need move-aware shortcut redirect."),
        L("服务停止后生效。", "Takes effect after the service stops."));

    public static readonly SettingHelpInfo DisableLowDiskSpaceChecks = H(
        L("不再弹出磁盘空间不足气泡警告。", "Stop low-disk-space balloon warnings."),
        L("Policies\\Explorer NoLowDiskSpaceChecks=1。", "Policies\\Explorer NoLowDiskSpaceChecks=1."),
        L("系统盘偏满的桌面少被托盘打断。", "Fewer tray interruptions when the system disk is nearly full."),
        L("磁盘紧张又希望被提醒时保持关闭。", "Keep off if the disk is tight and you want reminders."),
        L("重启资源管理器后生效。", "Takes effect after Explorer restart."),
        SettingScope.DesktopExperience);

    public static readonly SettingHelpInfo UsbFullPowerOff = H(
        L("安全弹出 USB 后尽量让端口掉电（指示灯灭）。", "After Safe Remove, power down the USB port when possible (LED off)."),
        L("Services\\USB DisableSelectiveSuspend=1，并关闭当前电源方案的 USB 选择性挂起。", "Services\\USB DisableSelectiveSuspend=1 and disable USB selective suspend in the current power plan."),
        L("U 盘/移动硬盘弹出后少处于假掉线、灯还亮的状态。", "Fewer fake-ejected sticks/drives with LEDs still on."),
        L("个别 Hub 仍由硬件供电；推荐在常用 U 盘环境开启。", "Some hubs stay powered by hardware; recommended if you use USB sticks often."),
        L("立即写入；已插入设备可能需重新插拔。", "Written immediately; already-inserted devices may need re-plug."));

    public static readonly SettingHelpInfo AutoRebootOnCrash = H(
        L("发生蓝屏后自动重启，而不是停在蓝屏画面。", "Auto-reboot after BSOD instead of staying on the blue screen."),
        L("CrashControl AutoReboot=1（多数系统默认已是 1）。", "CrashControl AutoReboot=1 (already 1 on most systems)."),
        L("无人值守或远程桌面机器蓝屏后能自己回来。", "Unattended/RDP machines recover themselves after BSOD."),
        L("正在抓蓝屏现场、需要看 stop 码时保持关闭。", "Keep off when capturing BSOD / reading stop codes."),
        L("立即写入，下次崩溃时生效。", "Written immediately; applies on next crash."));

    public static readonly SettingHelpInfo DisableDotNetPowerShellTelemetry = H(
        L("关闭 .NET CLI 与 PowerShell 遥测。", "Disable .NET CLI and PowerShell telemetry."),
        L("机器环境变量 DOTNET_CLI_TELEMETRY_OPTOUT=1、POWERSHELL_TELEMETRY_OPTOUT=1。", "Machine env DOTNET_CLI_TELEMETRY_OPTOUT=1, POWERSHELL_TELEMETRY_OPTOUT=1."),
        L("本机跑 dotnet / pwsh 时不再向微软回传使用数据。", "Local dotnet/pwsh no longer send usage data to Microsoft."),
        L("开发机与 Server 桌面都推荐开启。", "Recommended on both developer machines and Server desktops."),
        L("新开的终端立即生效；已打开的会话需重开。", "New terminals apply immediately; reopen existing sessions."));

    public static readonly SettingHelpInfo DiagnosticDataMinimal = H(
        L("把诊断数据调到系统允许的最低官方级别（不是只停 DiagTrack）。", "Set diagnostic data to the lowest official level (not only stopping DiagTrack)."),
        L("MaxTelemetryAllowed=1；Server 上 AllowTelemetry=0，客户端 Pro 为 1。与「关闭遥测」并存时保持 0。", "MaxTelemetryAllowed=1; AllowTelemetry=0 on Server, 1 on client Pro. Stays 0 if Disable telemetry is also on."),
        L("设置里显示「必需/基本」，少传可选诊断。", "Settings shows Required/Basic; less optional diagnostic data."),
        L("推荐开启；要比官方级别更狠请同时开「关闭遥测与 DiagTrack」。", "Recommended on; for stricter than official, also enable Disable telemetry & DiagTrack."),
        L("立即写入。", "Written immediately."));

    public static readonly SettingHelpInfo DisableSigninReopen = H(
        L("更新或重启后不使用登录信息自动完成设置、也不自动重开应用。", "After update/reboot, do not use sign-in info to finish setup or reopen apps."),
        L("DisableAutomaticRestartSignOn=1，当前用户 UserARSO OptOut=1。", "DisableAutomaticRestartSignOn=1; current user UserARSO OptOut=1."),
        L("少一次「正在完成更新」卡住，会话也不会被自动拉起的应用占住。", "Fewer Finishing updates hangs; session not occupied by auto-reopened apps."),
        L("推荐开启。", "Recommended on."),
        L("下次更新/重启后生效。", "Takes effect after the next update/reboot."));

    public static readonly SettingHelpInfo DisableSilentAppInstall = H(
        L("禁止 Windows 在后台静默安装建议应用。", "Block Windows from silently installing suggested apps in the background."),
        L("ContentDeliveryManager SilentInstalledAppsEnabled=0。", "ContentDeliveryManager SilentInstalledAppsEnabled=0."),
        L("不会自己多出 Candy Crush、试用商店应用。", "No surprise Candy Crush / trial Store apps."),
        L("推荐开启。", "Recommended on."),
        L("立即生效。", "Takes effect immediately."),
        W10De);

    public static readonly SettingHelpInfo HideExplorerHomeGallery = H(
        L("从资源管理器导航窗格去掉「主页」和「图库」。", "Remove Home and Gallery from Explorer navigation pane."),
        L("CLSID Home / Gallery 的 System.IsPinnedToNameSpaceTree=0。", "CLSID Home/Gallery System.IsPinnedToNameSpaceTree=0."),
        L("导航栏更短，直接看此电脑与磁盘。", "Shorter nav pane; go straight to This PC and drives."),
        L("Win11 桌面推荐开启；没有这两项时开着也无害。", "Recommended on Win11 desktops; harmless if those entries are absent."),
        L("重启资源管理器后生效。", "Takes effect after Explorer restart."),
        W10De);

    public static readonly SettingHelpInfo DisableSnapAssist = H(
        L("关闭窗口贴靠时的应用建议与最大化贴靠飞出。", "Disable Snap Assist app suggestions and maximize snap flyouts."),
        L("Explorer\\Advanced SnapAssist=0、EnableSnapAssistFlyout=0。", "Explorer\\Advanced SnapAssist=0, EnableSnapAssistFlyout=0."),
        L("拖窗口贴边时不再弹一排推荐应用。", "No row of suggested apps when snapping windows."),
        L("仍可用 Win+方向键贴靠；推荐开启。", "Win+Arrow snap still works; recommended on."),
        L("重启资源管理器后完全生效。", "Fully applies after Explorer restart."),
        SettingScope.DesktopExperience);

    public static readonly SettingHelpInfo EnableDarkMode = H(
        L("系统外壳和应用使用深色主题。", "Use dark theme for system shell and apps."),
        L("Personalize AppsUseLightTheme=0、SystemUsesLightTheme=0。", "Personalize AppsUseLightTheme=0, SystemUsesLightTheme=0."),
        L("夜间或 OLED 更省眼；未适配的 Win32 窗口仍可能是浅色。", "Easier on eyes at night/OLED; some Win32 windows may stay light."),
        L("喜欢浅色桌面时关闭。", "Turn off if you prefer a light desktop."),
        L("立即刷新主题。", "Refreshes the theme immediately."),
        SettingScope.DesktopExperience);

    public static readonly SettingHelpInfo DisableBitLockerAutoEncrypt = H(
        L("禁止系统在后台自动做设备加密（BitLocker）。", "Prevent background automatic device encryption (BitLocker)."),
        L("BitLocker PreventDeviceEncryption=1。不关闭你已经手动打开的加密。", "BitLocker PreventDeviceEncryption=1. Does not turn off encryption you already enabled."),
        L("新装机不会悄悄加密系统盘，少一次恢复密钥麻烦。", "New installs won't silently encrypt the system disk; fewer recovery-key surprises."),
        L("需要自动设备加密的笔记本请关闭本项。", "Turn this off on laptops that need automatic device encryption."),
        L("立即写入；已加密的盘不会自动解密。", "Written immediately; already-encrypted volumes are not decrypted."));

    public static readonly SettingHelpInfo PreventDeviceCompanionApps = H(
        L("插入显示器等设备时，不从网络拉配套应用。", "Do not download companion apps from the network when plugging in displays, etc."),
        L("Device Metadata PreventDeviceMetadataFromNetwork=1。", "Device Metadata PreventDeviceMetadataFromNetwork=1."),
        L("插 HDMI/扩展坞时少被塞厂商商店应用。", "Fewer vendor Store apps when connecting HDMI/docks."),
        L("推荐开启。", "Recommended on."),
        L("立即生效。", "Takes effect immediately."));

    public static readonly SettingHelpInfo DisableUpdateAsap = H(
        L("关闭「尽快获取最新更新」（连续创新/预览功能更新）。", "Disable Get the latest updates as soon as they are available (continuous innovation)."),
        L("WindowsUpdate UX IsContinuousInnovationOptedIn=0、IsExpedited=0。", "WindowsUpdate UX IsContinuousInnovationOptedIn=0, IsExpedited=0."),
        L("功能更新按正常通道来，不被拉进实验开关。", "Feature updates stay on the normal channel; not pulled into experiment switches."),
        L("安全补丁不受影响；推荐开启。", "Security patches unaffected; recommended on."),
        L("立即写入。", "Written immediately."),
        W10);

    public static readonly SettingHelpInfo HideSettingsHomeAds = H(
        L("隐藏设置应用首页，并关掉首页上的 Microsoft 365 广告。", "Hide Settings Home and turn off Microsoft 365 ads on it."),
        L("SettingsPageVisibility=hide:home；CloudContent DisableConsumerAccountStateContent=1。", "SettingsPageVisibility=hide:home; CloudContent DisableConsumerAccountStateContent=1."),
        L("打开设置直接进系统页，少一块推广。", "Settings opens to system pages; less promo."),
        L("Win11 推荐开启。", "Recommended on Win11."),
        L("重新打开设置后生效。", "Takes effect after reopening Settings."),
        W10De);

    public static readonly SettingHelpInfo DisableWin11ExtraAi = H(
        L("关闭 Recall、Click to Do，以及记事本/画图的生成式 AI。", "Disable Recall, Click to Do, and generative AI in Notepad/Paint."),
        L("WindowsAI DisableAIDataAnalysis / DisableClickToDo / AllowRecallEnablement；Notepad DisableAIFeatures；Paint 生成式策略。", "WindowsAI DisableAIDataAnalysis / DisableClickToDo / AllowRecallEnablement; Notepad DisableAIFeatures; Paint generative policies."),
        L("本工具「关闭 Copilot」管不到的 Win11 24H2+ AI 入口一并关掉。", "Also turns off Win11 24H2+ AI entries that Disable Copilot does not cover."),
        L("没有这些功能的系统开着也无害；与 Atlas「关 Recall」同类，推荐开启。", "Harmless without these features; same idea as Atlas Disable Recall — recommended on."),
        L("立即写入；Recall 可选功能需重启后完全消失。", "Written immediately; Recall optional feature fully gone after reboot."),
        W10);

    public static readonly SettingHelpInfo RestrictNullSessionShares = H(
        L("限制匿名访问命名管道与共享（RestrictNullSessAccess）。", "Restrict anonymous access to named pipes and shares."),
        L("LanManServer\\Parameters RestrictNullSessAccess=1。", "LanManServer\\Parameters RestrictNullSessAccess=1."),
        L("降低空会话扫共享/管道的攻击面（STIG 常见项）。", "Smaller null-session attack surface (common STIG item)."),
        L("家庭 NAS / 文件服推荐开启；依赖匿名共享的老应用勿开。", "Recommended for home NAS/file servers; skip if apps need anonymous shares."),
        L("立即写入。", "Written immediately."),
        recommend: RecommendLevel.Strong);

    public static readonly SettingHelpInfo RestrictAnonymousEnum = H(
        L("限制匿名枚举共享（RestrictAnonymous）。", "Restrict anonymous enumeration of shares."),
        L("Lsa RestrictAnonymous=1。", "Lsa RestrictAnonymous=1."),
        L("匿名用户更难列出本机共享名。", "Harder for anonymous users to list share names."),
        L("与上一项一起开更完整；域控/特殊兼容场景自行评估。", "Pair with null-session restrict; assess on DCs / legacy apps."),
        L("立即写入。", "Written immediately."),
        recommend: RecommendLevel.Strong);

    public static readonly SettingHelpInfo DisableSmbBandwidthThrottling = H(
        L("关闭 SMB 客户端带宽节流。", "Disable SMB client bandwidth throttling."),
        L("LanmanWorkstation\\Parameters DisableBandwidthThrottling=1。", "LanmanWorkstation\\Parameters DisableBandwidthThrottling=1."),
        L("高延迟链路上文件拷贝/媒体库吞吐可能更好（微软文件服调优建议）。", "May improve copy/media throughput on high-latency links (MS file-server tuning)."),
        L("文件服/NAS 访问推荐开启；一般宽带可开可关。", "Recommended when talking to file servers/NAS."),
        L("立即写入。", "Written immediately."),
        recommend: RecommendLevel.Strong);

    public static readonly SettingHelpInfo FasterShutdown = H(
        L("缩短关机时等待无响应程序/服务的时间。", "Shorten wait for hung apps/services on shutdown."),
        L("HungAppTimeout / WaitToKillAppTimeOut / WaitToKillServiceTimeout = 2000。", "HungAppTimeout / WaitToKillAppTimeOut / WaitToKillServiceTimeout = 2000."),
        L("关机更快；卡住的程序更快被结束。", "Faster shutdown; hung apps end sooner."),
        L("台式/Server 桌面推荐；需要慢关以保存工作的场景保持关闭。", "Recommended on desktops/Servers; keep off if you need slow shutdown to save work."),
        L("下次关机起生效。", "Applies on next shutdown."),
        recommend: RecommendLevel.Strong);

    public static readonly SettingHelpInfo DisableStartupAppDelay = H(
        L("取消开机后启动项的人为延迟。", "Remove artificial startup-app delay after logon."),
        L("Explorer\\Serialize StartupDelayInMSec=0。", "Explorer\\Serialize StartupDelayInMSec=0."),
        L("登录后托盘/启动软件更快起来。", "Tray/startup apps appear sooner after logon."),
        L("桌面推荐开启。", "Recommended on desktops."),
        L("注销或重启后生效。", "Applies after sign-out or reboot."),
        SettingScope.DesktopExperience,
        recommend: RecommendLevel.Strong);

    public static readonly SettingHelpInfo InstantMenuShow = H(
        L("菜单悬停立即展开（MenuShowDelay=0）。", "Open menus instantly on hover (MenuShowDelay=0)."),
        L("HKCU\\Control Panel\\Desktop MenuShowDelay=0。", "HKCU\\Control Panel\\Desktop MenuShowDelay=0."),
        L("右键/菜单栏子菜单更跟手。", "Context/menu bar submenus feel snappier."),
        L("桌面推荐开启。", "Recommended on desktops."),
        L("重新登录后完全生效。", "Fully applies after re-login."),
        SettingScope.DesktopExperience,
        recommend: RecommendLevel.Strong);

    public static readonly SettingHelpInfo DisableAeroShake = H(
        L("禁用「摇一摇窗口最小化其它窗口」（Aero Shake）。", "Disable Aero Shake (shake a window to minimize others)."),
        L("Explorer\\Advanced DisallowShaking=1。", "Explorer\\Advanced DisallowShaking=1."),
        L("少误触最小化整桌窗口。", "Fewer accidental minimize-all."),
        L("桌面强烈推荐。", "Strongly recommended on desktops."),
        L("立即写入。", "Written immediately."),
        SettingScope.DesktopExperience,
        recommend: RecommendLevel.Must);

    public static readonly SettingHelpInfo DisableNetworkLocationWizard = H(
        L("关闭「网络位置/是否可发现」弹窗向导。", "Disable the network location / discoverable wizard popup."),
        L("创建 HKLM\\…\\Network\\NewNetworkWindowOff 键。", "Create HKLM\\…\\Network\\NewNetworkWindowOff key."),
        L("插网线/新网络时少一个打扰弹窗。", "Fewer prompts on new networks."),
        L("台式/Server 桌面推荐；需要每次确认网络配置文件时保持关闭。", "Recommended on desktop/Server; keep off if you want profile prompts."),
        L("立即写入。", "Written immediately."),
        recommend: RecommendLevel.Strong);

    public static readonly SettingHelpInfo DisableSettingSync = H(
        L("关闭设置同步（Windows 备份/漫游设置）。", "Disable Settings Sync (Windows Backup / roaming settings)."),
        L("Policies\\SettingSync DisableSettingSync=2 等，并关掉各组同步。", "Policies\\SettingSync DisableSettingSync=2 etc., disable sync groups."),
        L("设置与凭据不经微软账户漫游，隐私更好。", "Settings/credentials do not roam via Microsoft account."),
        L("个人/内网桌面推荐；多机靠微软账号同步设置时勿开。", "Recommended for personal/LAN desktops; skip if you sync via MSA."),
        L("立即写入。", "Written immediately."),
        W10,
        recommend: RecommendLevel.Strong);

    public static readonly SettingHelpInfo DisableFinishSetupSuggestions = H(
        L("关闭「完成设备设置的建议方式」（SCOOBE）。", "Disable “suggested ways to finish setting up your device” (SCOOBE)."),
        L("UserProfileEngagement ScoobeSystemSettingEnabled=0。", "UserProfileEngagement ScoobeSystemSettingEnabled=0."),
        L("少被推微软账户/云功能。", "Fewer prompts to use MSA / cloud features."),
        L("桌面推荐开启。", "Recommended on desktops."),
        L("立即写入。", "Written immediately."),
        SettingScope.DesktopExperience,
        recommend: RecommendLevel.Strong);

    public static readonly SettingHelpInfo ExplorerTransferDetails = H(
        L("文件复制/移动默认展开「更多详细信息」。", "Show More details by default on file transfers."),
        L("OperationStatusManager EnthusiastMode=1。", "OperationStatusManager EnthusiastMode=1."),
        L("拷贝对话框直接显示速度与剩余时间。", "Copy dialog shows speed and ETA without an extra click."),
        L("桌面推荐开启。", "Recommended on desktops."),
        L("立即写入。", "Written immediately."),
        SettingScope.DesktopExperience,
        recommend: RecommendLevel.Strong);

    public static readonly SettingHelpInfo DisableTelemetryScheduledTasks = H(
        L("禁用常见遥测/CEIP 计划任务（可选清单同源）。", "Disable common telemetry/CEIP scheduled tasks."),
        L("禁用 Consolidator、UsbCeip、DiskDiagnosticDataCollector、PcaPatchDbTask、UsageDataReporting 等。", "Disable Consolidator, UsbCeip, DiskDiagnosticDataCollector, PcaPatchDbTask, UsageDataReporting, etc."),
        L("少后台采集任务；细项可在「工具 → 计划任务优化」勾选。", "Fewer background collectors; fine-tune in Tools → Scheduled tasks."),
        L("个人桌面推荐开启；企业需 CEIP 报表时保持关闭。", "Recommended on personal desktops; keep off if you need CEIP reports."),
        L("立即对存在的任务生效；不存在的任务跳过。", "Applies immediately to tasks that exist; missing tasks skipped."),
        recommend: RecommendLevel.Strong);

    public static readonly SettingHelpInfo ShowTrayBatteryPercent = H(
        L("托盘电量图标显示百分比。", "Show battery percentage on the tray icon."),
        L("Explorer\\Advanced TaskbarShowBatteryPercentage=1（Win11）。", "Explorer\\Advanced TaskbarShowBatteryPercentage=1 (Win11)."),
        L("一眼看到剩余电量，无需悬停。", "See remaining charge without hovering."),
        L("笔记本推荐；台式机无电池时无效。", "Recommended on laptops; no effect on desktops without a battery."),
        L("可能需重启资源管理器。", "May need Explorer restart."),
        SettingScope.DesktopExperience,
        recommend: RecommendLevel.Strong);

    public static readonly SettingHelpInfo AlwaysShowScrollbars = H(
        L("始终显示滚动条（关闭动态隐藏）。", "Always show scrollbars (disable dynamic hide)."),
        L("Control Panel\\Accessibility DynamicScrollbars=0。", "Control Panel\\Accessibility DynamicScrollbars=0."),
        L("滚动条常显，远程/触控板场景更好找。", "Scrollbars stay visible; easier on RDP/trackpads."),
        L("桌面可选开启。", "Optional on desktops."),
        L("新开窗口后生效。", "Applies to newly opened windows."),
        SettingScope.DesktopExperience,
        recommend: RecommendLevel.Suggested);

    public static readonly SettingHelpInfo NumLockOnBoot = H(
        L("开机/登录时默认开启 NumLock。", "NumLock on by default at boot/logon."),
        L("InitialKeyboardIndicators=2（当前用户与 .DEFAULT）。", "InitialKeyboardIndicators=2 (current user and .DEFAULT)."),
        L("数字小键盘一登录即可用。", "Numpad ready right after logon."),
        L("台式键盘推荐；笔记本视布局可选。", "Recommended with desktop keyboards; optional on laptops."),
        L("下次登录生效。", "Applies on next logon."),
        recommend: RecommendLevel.Strong);

    public static readonly SettingHelpInfo DisableMouseAcceleration = H(
        L("关闭鼠标加速（Enhance pointer precision）。", "Disable mouse acceleration (Enhance pointer precision)."),
        L("MouseSpeed / Threshold 置 0，并 SPI_SETMOUSE。", "MouseSpeed/Thresholds to 0 and SPI_SETMOUSE."),
        L("指针移动更线性，适合精细操作。", "More linear pointer travel for precise work."),
        L("游戏/CAD 常用；触控板用户可保持关闭本项。", "Common for gaming/CAD; trackpad users may leave off."),
        L("立即生效。", "Takes effect immediately."),
        SettingScope.DesktopExperience,
        recommend: RecommendLevel.Suggested);

    public static readonly SettingHelpInfo DisableWpbt = H(
        L("禁用 WPBT（厂商预启动驱动注入）。", "Disable WPBT (vendor pre-boot driver injection)."),
        L("Session Manager DisableWpbtExecution=1。", "Session Manager DisableWpbtExecution=1."),
        L("减少 OEM 预装驱动在启动阶段的注入。", "Fewer OEM drivers injected at boot."),
        L("高级项；不确定厂商依赖时保持关闭。", "Advanced; keep off if unsure about vendor deps."),
        L("重启后生效。", "Applies after reboot."),
        recommend: RecommendLevel.Optional);

    public static readonly SettingHelpInfo Win11StartMenuPreviousLayout = H(
        L("Win11 开始菜单使用更接近旧版的布局。", "Use a more classic Start menu layout on Win11."),
        L("Explorer\\Advanced Start_ShowClassicMode=1。", "Explorer\\Advanced Start_ShowClassicMode=1."),
        L("开始菜单少推荐/少广告感。", "Start feels less recommendation/ad heavy."),
        L("仅 Win11；桌面可选。", "Win11 only; optional on desktops."),
        L("可能需重启资源管理器。", "May need Explorer restart."),
        W10De,
        recommend: RecommendLevel.Suggested);

    public static readonly SettingHelpInfo AlwaysShowMenus = H(
        L("资源管理器始终显示菜单栏（文件/编辑/查看）。", "Always show the Explorer menu bar (File/Edit/View)."),
        L("Explorer\\Advanced AlwaysShowMenus=1。", "Explorer\\Advanced AlwaysShowMenus=1."),
        L("不必按 Alt 才出现菜单，文件夹选项里「始终显示菜单」同一项。", "No need to press Alt; same as Folder Options Always show menus."),
        L("桌面运维推荐开启。", "Recommended for desktop ops."),
        L("重启资源管理器后生效。", "Takes effect after Explorer restart."),
        SettingScope.DesktopExperience);
    public static readonly SettingHelpInfo HideMergeConflicts = H(
        L("复制文件夹时不弹出「合并冲突」确认。", "Do not prompt for merge conflicts when copying folders."),
        L("HideMergeConflicts=1，对应文件夹选项「隐藏文件夹合并冲突」。", "HideMergeConflicts=1; same as Folder Options Hide folder merge conflicts."),
        L("同名文件夹直接合并，少一次对话框。", "Same-name folders merge directly; one fewer dialog."),
        L("需要每次确认合并时关闭。", "Turn off if you want a confirm every merge."),
        L("新开的资源管理器窗口生效。", "Applies to newly opened Explorer windows."),
        SettingScope.DesktopExperience);
    public static readonly SettingHelpInfo ShowCompColor = H(
        L("加密或压缩的 NTFS 文件用蓝色/绿色显示。", "Show encrypted/compressed NTFS files in blue/green."),
        L("ShowCompColor=1。", "ShowCompColor=1."),
        L("一眼看出哪些文件被压缩或 EFS 加密。", "See at a glance which files are compressed or EFS-encrypted."),
        L("推荐开启。", "Recommended on."),
        L("刷新文件夹后生效。", "Takes effect after refreshing the folder."),
        SettingScope.DesktopExperience);
    public static readonly SettingHelpInfo ShowInfoTip = H(
        L("鼠标悬停文件夹/桌面图标时显示弹出说明。", "Show info tips when hovering folder/desktop icons."),
        L("ShowInfoTip=1（系统默认多为开）。", "ShowInfoTip=1 (usually on by default)."),
        L("提示尺寸、修改时间等，对应文件夹选项「显示弹出说明」。", "Shows size/modified time, etc.; Folder Options Show pop-up descriptions."),
        L("嫌气泡烦可关。", "Turn off if tips annoy you."),
        L("立即生效。", "Takes effect immediately."),
        SettingScope.DesktopExperience);
    public static readonly SettingHelpInfo ShowStatusBar = H(
        L("资源管理器底部显示状态栏（选中数量/大小）。", "Show Explorer status bar (selection count/size)."),
        L("ShowStatusBar=1。", "ShowStatusBar=1."),
        L("对齐文件夹选项「显示状态栏」。", "Matches Folder Options Show status bar."),
        L("推荐开启。", "Recommended on."),
        L("重启资源管理器后生效。", "Takes effect after Explorer restart."),
        SettingScope.DesktopExperience);
    public static readonly SettingHelpInfo DisablePersistBrowsers = H(
        L("登录后不自动还原上次未关的文件夹窗口。", "Do not restore previous folder windows after sign-in."),
        L("PersistBrowsers=0，对应「登录时还原上一个文件夹窗口」。", "PersistBrowsers=0; same as Restore previous folder windows at logon."),
        L("开机桌面更干净，少一堆上次留下的窗口。", "Cleaner desktop at boot; fewer leftover windows."),
        L("希望接着上次浏览位置时关闭本项。", "Turn this off if you want to resume last folder locations."),
        L("下次登录生效。", "Takes effect on next logon."),
        SettingScope.DesktopExperience);
    public static readonly SettingHelpInfo NavPaneExpandCurrent = H(
        L("导航窗格自动展开到当前打开的文件夹。", "Auto-expand the navigation pane to the current folder."),
        L("NavPaneExpandToCurrentFolder=1。", "NavPaneExpandToCurrentFolder=1."),
        L("左边树跟着当前路径走，少自己点开层级。", "Left tree follows the current path; less manual expanding."),
        L("推荐开启。", "Recommended on."),
        L("重启资源管理器后生效。", "Takes effect after Explorer restart."),
        SettingScope.DesktopExperience);
    public static readonly SettingHelpInfo DisableSharingWizard = H(
        L("不用「共享向导」，改走经典共享对话框。", "Use the classic sharing dialog instead of the Sharing Wizard."),
        L("SharingWizardOn=0。", "SharingWizardOn=0."),
        L("右键共享少几步向导。", "Fewer wizard steps for right-click share."),
        L("不习惯经典对话框时关闭。", "Turn off if you prefer the wizard."),
        L("立即生效。", "Takes effect immediately."),
        SettingScope.DesktopExperience);
    public static readonly SettingHelpInfo ShowDriveLettersMode = H(
        L("此电脑里盘符相对卷标的位置。", "Drive-letter position relative to volume labels under This PC."),
        L("ShowDriveLettersFirst：0=卷标后，1=卷标前，2=隐藏。", "ShowDriveLettersFirst: 0=after label, 1=before label, 2=hidden."),
        L("运维看盘符更方便时可把盘符放到前面。", "Put letters first when ops need to spot drive letters faster."),
        L("推荐「卷标后面」（系统默认）或「卷标前面」。", "Recommended: after label (default) or before label."),
        L("重启资源管理器后生效。", "Takes effect after Explorer restart."),
        SettingScope.DesktopExperience);
    public static readonly SettingHelpInfo FolderGroupByMode = H(
        L("所有常见文件夹类型的默认「分组依据」。", "Default Group by for common folder types."),
        L("写入 FolderTypes TopViews 的 GroupBy，并同步 Bags\\AllFolders GroupView。", "Writes FolderTypes TopViews GroupBy and syncs Bags\\AllFolders GroupView."),
        L("下载目录不再按日期一大组；可统一改成不分组或按名称/类型。", "Downloads no longer one big date group; unify to none / name / type."),
        L("桌面推荐「不分组」。已打开的窗口需重启资源管理器。", "Desktop: prefer No grouping. Open windows need Explorer restart."),
        L("重启资源管理器后生效。", "Takes effect after Explorer restart."),
        SettingScope.DesktopExperience);
    public static readonly SettingHelpInfo FolderSortByMode = H(
        L("所有常见文件夹类型的默认「排序方式」。", "Default Sort by for common folder types."),
        L("FolderTypes TopViews SortByList（名称/日期/类型/大小，升序或降序）。", "FolderTypes TopViews SortByList (name/date/type/size, asc/desc)."),
        L("新开窗口按你选的列排序，不必每个文件夹再点一次。", "New windows sort by your chosen column without per-folder clicks."),
        L("推荐名称升序；下载目录若习惯新文件在上可选「日期新到旧」。", "Recommended: name ascending; Downloads may use date newest-first."),
        L("重启资源管理器后生效。", "Takes effect after Explorer restart."),
        SettingScope.DesktopExperience);

    static SettingHelpInfo H(
        string summary,
        string purpose,
        string benefit,
        string guide,
        string effect,
        SettingScope? scope = null,
        string? uiPlace = null,
        string? whenHint = null,
        RecommendLevel? recommend = null) =>
        new(summary, purpose, benefit, guide, effect, scope, uiPlace, whenHint,
            recommend ?? InferRecommend(guide, whenHint, scope));

    /// <summary>未显式标注时，根据指引文案推断推荐强度。</summary>
    static RecommendLevel InferRecommend(string guide, string? whenHint, SettingScope? scope)
    {
        var t = (guide ?? "") + "\n" + (whenHint ?? "");
        // 风险/按需 → 可选 (zh + en；静态初始化时 guide 可能已是英文)
        if (ContainsAny(t,
                "谨慎", "勿开", "不要开", "按需", "可选", "可关闭", "请自行评估", "有兼容", "会失效", "受影响",
                "caution", "be careful", "Do not enable", "do not enable", "assess the risk", "may be affected", "will be affected"))
            return RecommendLevel.Optional;
        // 必做语境
        if (ContainsAny(t, "务必", "必须", "必开", "几乎必", "must enable", "Must keep", "Almost required", "must on") ||
            (scope?.ServerOnly == true && ContainsAny(t, "当桌面用建议", "个人桌面推荐开启", "Recommended when used as a desktop", "Recommended for personal")))
            return RecommendLevel.Must;
        // 强烈
        if (ContainsAny(t,
                "强烈", "强烈建议", "强烈推荐", "当桌面用建议", "个人桌面推荐", "推荐开启", "建议开启",
                "strongly recommended", "Strongly recommended", "Recommended when used as a desktop",
                "Recommended for personal", "Recommended on", "Suggested for"))
            return RecommendLevel.Strong;
        return RecommendLevel.Suggested;
    }

    static bool ContainsAny(string text, params string[] keys)
    {
        foreach (var k in keys)
        {
            if (text.IndexOf(k, StringComparison.Ordinal) >= 0)
                return true;
        }
        return false;
    }
}
