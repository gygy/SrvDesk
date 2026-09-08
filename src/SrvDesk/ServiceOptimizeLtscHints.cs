namespace SrvDesk;

/// <summary>
/// 来自 docs/Win10 LTSC 2021 系统服务.xlsx 备注的精简处置说明。
/// 标签：应禁用 / 应保留 / 按需 / 更新时开 —— 便于一眼判断。
/// </summary>
internal static class ServiceOptimizeLtscHints
{
    public readonly struct Hint
    {
        public ServiceRecommend Recommend { get; }
        public string Tag { get; }
        public string Note { get; }

        public Hint(ServiceRecommend recommend, string tag, string note)
        {
            Recommend = recommend;
            Tag = tag;
            Note = note;
        }
    }

    private static readonly Dictionary<string, Hint> ByService =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["AarSvc"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】可禁，不影响使用"),
            ["AJRouter"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】AllJoyn IoT，可禁"),
            ["ALG"] = new(ServiceRecommend.Manual, "按需", "【按需】用系统防火墙则保留"),
            ["AppXSvc"] = new(ServiceRecommend.Manual, "按需", "【按需】用商店/微软账户则保留"),
            ["AudioEndpointBuilder"] = new(ServiceRecommend.Auto, "应保留", "【应保留】音频终结点，应开"),
            ["AudioSrv"] = new(ServiceRecommend.Auto, "应保留", "【应保留】音频，应开"),
            ["BcastDVRUserService"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】游戏录制，可禁"),
            ["BDESVC"] = new(ServiceRecommend.Manual, "按需", "【按需】BitLocker 加密时保留"),
            ["BITS"] = new(ServiceRecommend.Manual, "更新时开", "【更新时开】装补丁时再开"),
            ["BluetoothUserService"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】蓝牙相关，不用可禁"),
            ["BTAGService"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】蓝牙相关，不用可禁"),
            ["bthserv"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】蓝牙相关，不用可禁"),
            ["camsvc"] = new(ServiceRecommend.Manual, "应保留", "【应保留】禁了隐私设置会闪退"),
            ["ClipSVC"] = new(ServiceRecommend.Manual, "应保留", "【应保留】商店许可相关"),
            ["COMSysApp"] = new(ServiceRecommend.Manual, "应保留", "【应保留】COM+ 应用，保留"),
            ["CoreMessagingRegistrar"] = new(ServiceRecommend.Keep, "应保留", "【应保留】禁了进不了系统"),
            ["CryptSvc"] = new(ServiceRecommend.Auto, "应保留", "【应保留】加密服务，保留"),
            ["DeviceAssociationBroker"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】可禁，不影响打印"),
            ["DeviceAssociationService"] = new(ServiceRecommend.Manual, "应保留", "【应保留】禁了无法加打印机"),
            ["DevicePickerUserSvc"] = new(ServiceRecommend.Manual, "应保留", "【应保留】禁了可能影响打印"),
            ["DevicesFlowUserSvc"] = new(ServiceRecommend.Manual, "应保留", "【应保留】禁了可能影响打印"),
            ["diagsvc"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】诊断执行，可禁"),
            ["DisplayEnhancementService"] = new(ServiceRecommend.Manual, "按需", "【按需】禁了无法调亮度"),
            ["Dnscache"] = new(ServiceRecommend.Auto, "应保留", "【应保留】DNS 缓存，保留"),
            ["dot3svc"] = new(ServiceRecommend.Manual, "按需", "【按需】有线 802.1X 时保留"),
            ["DPS"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】诊断策略，可禁"),
            ["DsmSvc"] = new(ServiceRecommend.Manual, "应保留", "【应保留】设备安装，保留"),
            ["DusmSvc"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】流量统计，可禁"),
            ["edgeupdate"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】Edge 更新，可禁"),
            ["edgeupdatem"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】Edge 更新，可禁"),
            ["EFS"] = new(ServiceRecommend.Manual, "按需", "【按需】EFS 解密时需要"),
            ["EventSystem"] = new(ServiceRecommend.Auto, "应保留", "【应保留】COM+ 事件，保留"),
            ["Fax"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】传真，可禁"),
            ["FrameServer"] = new(ServiceRecommend.Manual, "按需", "【按需】摄像头帧服务"),
            ["HvHost"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】Hyper-V 宿主，不用可禁"),
            ["icssvc"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】移动热点，可禁"),
            ["IKEEXT"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】IPsec 密钥，可禁"),
            ["iphlpsvc"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】IPv6 转换，可禁"),
            ["KeyIso"] = new(ServiceRecommend.Manual, "应保留", "【应保留】加密密钥隔离"),
            ["KtmRm"] = new(ServiceRecommend.Manual, "应保留", "【应保留】分布式事务，保留"),
            ["LanmanServer"] = new(ServiceRecommend.Manual, "按需", "【按需】文件/打印共享时保留"),
            ["LanmanWorkstation"] = new(ServiceRecommend.Manual, "按需", "【按需】访问网络共享/企业邮箱时保留"),
            ["lfsvc"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】定位服务，可禁"),
            ["LicenseManager"] = new(ServiceRecommend.Manual, "应保留", "【应保留】系统许可，保留"),
            ["lmhosts"] = new(ServiceRecommend.Manual, "应保留", "【应保留】NetBIOS 辅助，保留"),
            ["LxpSvc"] = new(ServiceRecommend.Manual, "按需", "【按需】切换系统语言时保留"),
            ["MapsBroker"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】离线地图，可禁"),
            ["MicrosoftEdgeElevationService"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】Edge 更新，可禁"),
            ["mpssvc"] = new(ServiceRecommend.Keep, "应保留", "【应保留】防火墙，建议保留"),
            ["msiserver"] = new(ServiceRecommend.Manual, "应保留", "【应保留】安装程序，保留"),
            ["NgcCtnrSvc"] = new(ServiceRecommend.Manual, "按需", "【按需】微软账户容器"),
            ["NgcSvc"] = new(ServiceRecommend.Manual, "按需", "【按需】微软账户/Windows Hello"),
            ["PcaSvc"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】兼容性助手，可禁"),
            ["PolicyAgent"] = new(ServiceRecommend.Manual, "应保留", "【应保留】IPsec 策略，保留"),
            ["PrintWorkflowUserSvc"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】可禁，不影响使用"),
            ["RasMan"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】远程接入，个人可禁"),
            ["RemoteRegistry"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】远程注册表，应禁"),
            ["RmSvc"] = new(ServiceRecommend.Manual, "按需", "【按需】禁了无法进飞行模式"),
            ["RpcEptMapper"] = new(ServiceRecommend.Auto, "应保留", "【应保留】RPC 映射，保留"),
            ["SamSs"] = new(ServiceRecommend.Auto, "应保留", "【应保留】账户管理，保留"),
            ["SCardSvr"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】智能卡，少用可禁"),
            ["ScDeviceEnum"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】智能卡枚举，可禁"),
            ["Schedule"] = new(ServiceRecommend.Keep, "应保留", "【应保留】禁了无输入法指示器"),
            ["SCPolicySvc"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】智能卡策略，可禁"),
            ["SDRSVC"] = new(ServiceRecommend.Manual, "按需", "【按需】系统备份时保留"),
            ["SecurityHealthService"] = new(ServiceRecommend.Manual, "按需", "【按需】用 Defender 则保留"),
            ["Sense"] = new(ServiceRecommend.Manual, "按需", "【按需】用 Defender ATP 则保留"),
            ["SessionEnv"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】RDP 配置，个人可禁"),
            ["SgrmBroker"] = new(ServiceRecommend.Keep, "应保留", "【应保留】禁了网卡属性会空/计划任务异常"),
            ["Spooler"] = new(ServiceRecommend.Manual, "按需", "【按需】有打印机则保留"),
            ["sppsvc"] = new(ServiceRecommend.Auto, "应保留", "【应保留】软件保护，保留"),
            ["SysMain"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】超级抓取，建议禁"),
            ["TabletInputService"] = new(ServiceRecommend.Keep, "应保留", "【应保留】禁了无法用输入法"),
            ["TermService"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】远程桌面，个人可禁"),
            ["Themes"] = new(ServiceRecommend.Auto, "应保留", "【应保留】主题服务，保留"),
            ["TimeBrokerSvc"] = new(ServiceRecommend.Manual, "应保留", "【应保留】时间代理，保留"),
            ["TokenBroker"] = new(ServiceRecommend.Manual, "按需", "【按需】微软账户 Web 登录"),
            ["TroubleshootingSvc"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】推荐疑难解答，可禁"),
            ["TrustedInstaller"] = new(ServiceRecommend.Manual, "更新时开", "【更新时开】组件安装，更新时开"),
            ["UnistoreSvc"] = new(ServiceRecommend.Manual, "按需", "【按需】微软账户数据存储"),
            ["UserDataSvc"] = new(ServiceRecommend.Manual, "按需", "【按需】微软账户数据访问"),
            ["UserManager"] = new(ServiceRecommend.Keep, "应保留", "【应保留】禁了开始菜单异常"),
            ["UsoSvc"] = new(ServiceRecommend.Manual, "更新时开", "【更新时开】更新编排，更新时必开"),
            ["VaultSvc"] = new(ServiceRecommend.Auto, "应保留", "【应保留】凭据管理，保留"),
            ["vds"] = new(ServiceRecommend.Manual, "按需", "【按需】虚拟磁盘/装系统时需要"),
            ["vmicguestinterface"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】Hyper-V 客户机，不用可禁"),
            ["vmicheartbeat"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】Hyper-V 客户机，不用可禁"),
            ["vmickvpexchange"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】Hyper-V 客户机，不用可禁"),
            ["vmicrdv"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】Hyper-V 客户机，不用可禁"),
            ["vmicshutdown"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】Hyper-V 客户机，不用可禁"),
            ["vmictimesync"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】Hyper-V 客户机，不用可禁"),
            ["vmicvmsession"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】Hyper-V 客户机，不用可禁"),
            ["vmicvss"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】Hyper-V 客户机，不用可禁"),
            ["wbengine"] = new(ServiceRecommend.Manual, "按需", "【按需】系统备份时保留"),
            ["WbioSrvc"] = new(ServiceRecommend.Manual, "按需", "【按需】指纹识别时保留"),
            ["Wcmsvc"] = new(ServiceRecommend.Keep, "应保留", "【应保留】禁了任务栏网络打叉"),
            ["WdiServiceHost"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】诊断主机，可禁"),
            ["WdiSystemHost"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】诊断系统主机，可禁"),
            ["WebClient"] = new(ServiceRecommend.Manual, "按需", "【按需】WebDAV/微软账户时保留"),
            ["WEPHOSTSVC"] = new(ServiceRecommend.Manual, "按需", "【按需】解密文件时需要"),
            ["wercplsupport"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】问题报告面板，可禁"),
            ["WerSvc"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】错误报告，可禁"),
            ["WinHttpAutoProxySvc"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】代理自动发现，可禁"),
            ["wisvc"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】Insider，LTSC 应禁"),
            ["Wlansvc"] = new(ServiceRecommend.Manual, "按需", "【按需】无线网时保留"),
            ["wlidsvc"] = new(ServiceRecommend.Manual, "按需", "【按需】微软账户登录时保留"),
            ["WpnUserService"] = new(ServiceRecommend.Keep, "应保留", "【应保留】禁了打不开网络设置"),
            ["wscsvc"] = new(ServiceRecommend.Auto, "应保留", "【应保留】安全中心，保留"),
            ["WSearch"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】占资源，可用 Everything"),
            ["wuauserv"] = new(ServiceRecommend.Manual, "更新时开", "【更新时开】Windows 更新，更新时开"),
            ["XblAuthManager"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】Xbox 登录，可禁"),
            ["XblGameSave"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】Xbox 存档，可禁"),
            ["XboxGipSvc"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】Xbox 配件，可禁"),
            ["XboxNetApiSvc"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】Xbox 网络，可禁"),
        };

    private static readonly Dictionary<string, Hint> ByDisplay =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Agent Activation Runtime"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】可禁，不影响使用"),
            ["Application Layer Gateway Service"] = new(ServiceRecommend.Manual, "按需", "【按需】用系统防火墙则保留"),
            ["AppX Deployment Service (AppXSVC)"] = new(ServiceRecommend.Manual, "按需", "【按需】用商店/微软账户则保留"),
            ["Autodesk Desktop Licensing Service"] = new(ServiceRecommend.Manual, "按需", "【按需】AUTOCAD授权，不能关闭，否则无"),
            ["Background Intelligent Transfer Service"] = new(ServiceRecommend.Manual, "更新时开", "【更新时开】装补丁时再开"),
            ["BitLocker Drive Encryption Service"] = new(ServiceRecommend.Manual, "按需", "【按需】BitLocker 加密时保留"),
            ["Block Level Backup Engine Service"] = new(ServiceRecommend.Manual, "按需", "【按需】系统备份时保留"),
            ["Bluetooth Audio Gateway Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】蓝牙相关，不用可禁"),
            ["Bluetooth Support Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】蓝牙相关，不用可禁"),
            ["Bluetooth User Support Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】蓝牙相关，不用可禁"),
            ["Capability Access Manager Service"] = new(ServiceRecommend.Manual, "应保留", "【应保留】禁了隐私设置会闪退"),
            ["Client License Service (ClipSVC)"] = new(ServiceRecommend.Manual, "应保留", "【应保留】商店许可相关"),
            ["CNG Key Isolation"] = new(ServiceRecommend.Manual, "应保留", "【应保留】加密密钥隔离"),
            ["COM+ Event System"] = new(ServiceRecommend.Auto, "应保留", "【应保留】COM+ 事件，保留"),
            ["COM+ System Application"] = new(ServiceRecommend.Manual, "应保留", "【应保留】COM+ 应用，保留"),
            ["CoreMessaging"] = new(ServiceRecommend.Keep, "应保留", "【应保留】禁了进不了系统"),
            ["Credential Manager"] = new(ServiceRecommend.Auto, "应保留", "【应保留】凭据管理，保留"),
            ["Cryptographic Services"] = new(ServiceRecommend.Auto, "应保留", "【应保留】加密服务，保留"),
            ["Data Usage"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】流量统计，可禁"),
            ["Device Association Service"] = new(ServiceRecommend.Manual, "应保留", "【应保留】禁了无法加打印机"),
            ["Device Setup Manager"] = new(ServiceRecommend.Manual, "应保留", "【应保留】设备安装，保留"),
            ["DeviceAssociationBroker"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】可禁，不影响打印"),
            ["DevicePicker"] = new(ServiceRecommend.Manual, "应保留", "【应保留】禁了可能影响打印"),
            ["DevicesFlow"] = new(ServiceRecommend.Manual, "应保留", "【应保留】禁了可能影响打印"),
            ["Diagnostic Execution Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】诊断执行，可禁"),
            ["Diagnostic Policy Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】诊断策略，可禁"),
            ["Diagnostic Service Host"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】诊断主机，可禁"),
            ["Diagnostic System Host"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】诊断系统主机，可禁"),
            ["Display Enhancement Service"] = new(ServiceRecommend.Manual, "按需", "【按需】禁了无法调亮度"),
            ["DNS Client"] = new(ServiceRecommend.Auto, "应保留", "【应保留】DNS 缓存，保留"),
            ["Downloaded Maps Manager"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】离线地图，可禁"),
            ["Encrypting File System (EFS)"] = new(ServiceRecommend.Manual, "按需", "【按需】EFS 解密时需要"),
            ["Fax"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】传真，可禁"),
            ["FlexNet Licensing Service"] = new(ServiceRecommend.Manual, "按需", "【按需】AUTOCAD授权，不能关闭，否则无"),
            ["Foxit PDF Editor Update Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】Foxit PDF服务，可以关掉"),
            ["GameDVR and Broadcast User Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】游戏录制，可禁"),
            ["Gateway Session Service"] = new(ServiceRecommend.Manual, "按需", "【按需】VPN服务，没有安装这个的忽略"),
            ["Gateway Updater Service"] = new(ServiceRecommend.Manual, "按需", "【按需】VPN服务，没有安装这个的忽略"),
            ["Geolocation Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】定位服务，可禁"),
            ["Huawei APO service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】华为电脑专属，可以禁用， 不影响使用"),
            ["Huawei Hiview Windows Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】华为电脑专属，可以禁用， 不影响使用"),
            ["Huawei LCD_Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】华为电脑专属，可以禁用， 不影响使用"),
            ["Huawei PC Core Service"] = new(ServiceRecommend.Manual, "按需", "【按需】禁用后无法打开华为电脑管家"),
            ["Huawei PCManager Windows Service"] = new(ServiceRecommend.Manual, "按需", "【按需】禁用后无法打开华为电脑管家"),
            ["HV Host Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】Hyper-V 宿主，不用可禁"),
            ["HW OSD Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】华为电脑专属，可以禁用， 不影响使用"),
            ["Hyper-V Data Exchange Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】Hyper-V 客户机，不用可禁"),
            ["Hyper-V Guest Service Interface"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】Hyper-V 客户机，不用可禁"),
            ["Hyper-V Guest Shutdown Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】Hyper-V 客户机，不用可禁"),
            ["Hyper-V Heartbeat Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】Hyper-V 客户机，不用可禁"),
            ["Hyper-V PowerShell Direct Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】Hyper-V 客户机，不用可禁"),
            ["Hyper-V Remote Desktop Virtualization Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】Hyper-V 客户机，不用可禁"),
            ["Hyper-V Time Synchronization Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】Hyper-V 客户机，不用可禁"),
            ["Hyper-V Volume Shadow Copy Requestor"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】Hyper-V 客户机，不用可禁"),
            ["IKE and AuthIP IPsec Keying Modules"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】IPsec 密钥，可禁"),
            ["Intel(R) Audio Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】厂商驱动，不用可禁"),
            ["Intel(R) Capability Licensing Service TCP IP Interface"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】厂商驱动，不用可禁"),
            ["Intel(R) Content Protection HDCP Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】厂商驱动，不用可禁"),
            ["Intel(R) Content Protection HECI Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】厂商驱动，不用可禁"),
            ["Intel(R) Dynamic Application Loader Host Interface Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】厂商驱动，不用可禁"),
            ["Intel(R) Dynamic Tuning service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】厂商驱动，不用可禁"),
            ["Intel(R) Graphics Command Center Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】厂商驱动，不用可禁"),
            ["Intel(R) HD Graphics Control Panel Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】厂商驱动，不用可禁"),
            ["Intel(R) TPM Provisioning Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】厂商驱动，不用可禁"),
            ["Intel? PROSet/Wireless Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】厂商驱动，不用可禁"),
            ["Intel? SGX AESM"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】厂商驱动，不用可禁"),
            ["IP Helper"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】IPv6 转换，可禁"),
            ["IPsec Policy Agent"] = new(ServiceRecommend.Manual, "应保留", "【应保留】IPsec 策略，保留"),
            ["KtmRm for Distributed Transaction Coordinator"] = new(ServiceRecommend.Manual, "应保留", "【应保留】分布式事务，保留"),
            ["Language Experience Service"] = new(ServiceRecommend.Manual, "按需", "【按需】切换系统语言时保留"),
            ["Microsoft Account Sign-in Assistant"] = new(ServiceRecommend.Manual, "按需", "【按需】微软账户登录时保留"),
            ["Microsoft Edge Elevation Service (MicrosoftEdgeElevationService)"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】Edge 更新，可禁"),
            ["Microsoft Edge Update Service (edgeupdate)"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】Edge 更新，可禁"),
            ["Microsoft Edge Update Service (edgeupdatem)"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】Edge 更新，可禁"),
            ["Microsoft Passport"] = new(ServiceRecommend.Manual, "按需", "【按需】微软账户/Windows Hello"),
            ["Microsoft Passport Container"] = new(ServiceRecommend.Manual, "按需", "【按需】微软账户容器"),
            ["Nahimic service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】厂商驱动，不用可禁"),
            ["NVIDIA Display Container LS"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】厂商驱动，不用可禁"),
            ["NVIDIA LocalSystem Container"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】厂商驱动，不用可禁"),
            ["NVIDIA NetworkService Container"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】厂商驱动，不用可禁"),
            ["Print Spooler"] = new(ServiceRecommend.Manual, "按需", "【按需】有打印机则保留"),
            ["PrintWorkflow"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】可禁，不影响使用"),
            ["Problem Reports Control Panel Support"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】问题报告面板，可禁"),
            ["Program Compatibility Assistant Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】兼容性助手，可禁"),
            ["Radio Management Service"] = new(ServiceRecommend.Manual, "按需", "【按需】禁了无法进飞行模式"),
            ["Realtek Audio Universal Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】厂商驱动，不用可禁"),
            ["Recommended Troubleshooting Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】推荐疑难解答，可禁"),
            ["Remote Access Connection Manager"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】远程接入，个人可禁"),
            ["Remote Desktop Configuration"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】RDP 配置，个人可禁"),
            ["Remote Desktop Services"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】远程桌面，个人可禁"),
            ["Remote Registry"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】远程注册表，应禁"),
            ["RPC Endpoint Mapper"] = new(ServiceRecommend.Auto, "应保留", "【应保留】RPC 映射，保留"),
            ["Security Accounts Manager"] = new(ServiceRecommend.Auto, "应保留", "【应保留】账户管理，保留"),
            ["Security Center"] = new(ServiceRecommend.Auto, "应保留", "【应保留】安全中心，保留"),
            ["Server"] = new(ServiceRecommend.Manual, "按需", "【按需】文件/打印共享时保留"),
            ["Smart Card"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】智能卡，少用可禁"),
            ["Smart Card Device Enumeration Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】智能卡枚举，可禁"),
            ["Smart Card Removal Policy"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】智能卡策略，可禁"),
            ["Software Protection"] = new(ServiceRecommend.Auto, "应保留", "【应保留】软件保护，保留"),
            ["SysMain"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】超级抓取，建议禁"),
            ["System Guard Runtime Monitor Broker"] = new(ServiceRecommend.Keep, "应保留", "【应保留】禁了网卡属性会空/计划任务异常"),
            ["Task Scheduler"] = new(ServiceRecommend.Keep, "应保留", "【应保留】禁了无输入法指示器"),
            ["TCP/IP NetBIOS Helper"] = new(ServiceRecommend.Manual, "应保留", "【应保留】NetBIOS 辅助，保留"),
            ["Themes"] = new(ServiceRecommend.Auto, "应保留", "【应保留】主题服务，保留"),
            ["Time Broker"] = new(ServiceRecommend.Manual, "应保留", "【应保留】时间代理，保留"),
            ["Touch Keyboard and Handwriting Panel Service"] = new(ServiceRecommend.Keep, "应保留", "【应保留】禁了无法用输入法"),
            ["Update Orchestrator Service"] = new(ServiceRecommend.Manual, "更新时开", "【更新时开】更新编排，更新时必开"),
            ["User Data Access"] = new(ServiceRecommend.Manual, "按需", "【按需】微软账户数据访问"),
            ["User Data Storage"] = new(ServiceRecommend.Manual, "按需", "【按需】微软账户数据存储"),
            ["User Manager"] = new(ServiceRecommend.Keep, "应保留", "【应保留】禁了开始菜单异常"),
            ["Virtual Disk"] = new(ServiceRecommend.Manual, "按需", "【按需】虚拟磁盘/装系统时需要"),
            ["Web Account Manager"] = new(ServiceRecommend.Manual, "按需", "【按需】微软账户 Web 登录"),
            ["WebClient"] = new(ServiceRecommend.Manual, "按需", "【按需】WebDAV/微软账户时保留"),
            ["Windows Audio"] = new(ServiceRecommend.Auto, "应保留", "【应保留】音频，应开"),
            ["Windows Audio Endpoint Builder"] = new(ServiceRecommend.Auto, "应保留", "【应保留】音频终结点，应开"),
            ["Windows Backup"] = new(ServiceRecommend.Manual, "按需", "【按需】系统备份时保留"),
            ["Windows Biometric Service"] = new(ServiceRecommend.Manual, "按需", "【按需】指纹识别时保留"),
            ["Windows Camera Frame Server"] = new(ServiceRecommend.Manual, "按需", "【按需】摄像头帧服务"),
            ["Windows Connection Manager"] = new(ServiceRecommend.Keep, "应保留", "【应保留】禁了任务栏网络打叉"),
            ["Windows Defender Advanced Threat Protection Service"] = new(ServiceRecommend.Manual, "按需", "【按需】用 Defender ATP 则保留"),
            ["Windows Defender Firewall"] = new(ServiceRecommend.Keep, "应保留", "【应保留】防火墙，建议保留"),
            ["Windows Encryption Provider Host Service"] = new(ServiceRecommend.Manual, "按需", "【按需】解密文件时需要"),
            ["Windows Error Reporting Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】错误报告，可禁"),
            ["Windows Insider Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】Insider，LTSC 应禁"),
            ["Windows Installer"] = new(ServiceRecommend.Manual, "应保留", "【应保留】安装程序，保留"),
            ["Windows License Manager Service"] = new(ServiceRecommend.Manual, "应保留", "【应保留】系统许可，保留"),
            ["Windows Mobile Hotspot Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】移动热点，可禁"),
            ["Windows Modules Installer"] = new(ServiceRecommend.Manual, "更新时开", "【更新时开】组件安装，更新时开"),
            ["Windows Push Notifications User Service"] = new(ServiceRecommend.Keep, "应保留", "【应保留】禁了打不开网络设置"),
            ["Windows Search"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】占资源，可用 Everything"),
            ["Windows Security Service"] = new(ServiceRecommend.Manual, "按需", "【按需】用 Defender 则保留"),
            ["Windows Update"] = new(ServiceRecommend.Manual, "更新时开", "【更新时开】Windows 更新，更新时开"),
            ["WinHTTP Web Proxy Auto-Discovery Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】代理自动发现，可禁"),
            ["Wired AutoConfig"] = new(ServiceRecommend.Manual, "按需", "【按需】有线 802.1X 时保留"),
            ["WLAN AutoConfig"] = new(ServiceRecommend.Manual, "按需", "【按需】无线网时保留"),
            ["Workstation"] = new(ServiceRecommend.Manual, "按需", "【按需】访问网络共享/企业邮箱时保留"),
            ["Xbox Accessory Management Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】Xbox 配件，可禁"),
            ["Xbox Live Auth Manager"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】Xbox 登录，可禁"),
            ["Xbox Live Game Save"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】Xbox 存档，可禁"),
            ["Xbox Live Networking Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】Xbox 网络，可禁"),
        };

    public static Hint? Find(string? serviceName, string? displayName)
    {
        if (!string.IsNullOrWhiteSpace(serviceName)
            && ByService.TryGetValue(serviceName!.Trim(), out var bySvc))
            return bySvc;

        if (string.IsNullOrWhiteSpace(displayName)) return null;
        var d = displayName!.Trim();
        if (ByDisplay.TryGetValue(d, out var byDisp)) return byDisp;

        // 去掉用户服务随机后缀 _xxxx
        var idx = d.LastIndexOf('_');
        if (idx > 0 && idx < d.Length - 1)
        {
            var suffix = d.Substring(idx + 1);
            if (suffix.Length >= 4 && suffix.Length <= 8 && IsHex(suffix))
            {
                var baseName = d.Substring(0, idx);
                if (ByDisplay.TryGetValue(baseName, out byDisp)) return byDisp;
                if (ByService.TryGetValue(baseName, out bySvc)) return bySvc;
            }
        }
        return null;
    }

    private static bool IsHex(string s)
    {
        foreach (var c in s)
        {
            if (c is (>= '0' and <= '9') or (>= 'a' and <= 'f') or (>= 'A' and <= 'F')) continue;
            return false;
        }
        return true;
    }
}
