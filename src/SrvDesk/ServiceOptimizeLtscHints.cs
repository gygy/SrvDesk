namespace SrvDesk;

/// <summary>
/// 来自 docs/Win10 LTSC 2021 系统服务.xlsx 与同目录截图备注的处置说明。
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
            ["AarSvc"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】可禁，不影响系统使用"),
            ["AJRouter"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】AllJoyn IoT，可禁"),
            ["ALG"] = new(ServiceRecommend.Manual, "按需", "【按需】使用系统防火墙则保留"),
            ["AppXSvc"] = new(ServiceRecommend.Manual, "按需", "【按需】用微软商店和微软账户的保留"),
            ["AudioEndpointBuilder"] = new(ServiceRecommend.Auto, "应保留", "【应保留】音频终结点，保留"),
            ["AudioSrv"] = new(ServiceRecommend.Auto, "应保留", "【应保留】音频，保留"),
            ["BcastDVRUserService"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】游戏录制与广播，用不着的关掉"),
            ["BDESVC"] = new(ServiceRecommend.Manual, "按需", "【按需】硬盘加密（BitLocker）时保留"),
            ["BITS"] = new(ServiceRecommend.Manual, "更新时开", "【更新时开】更新补丁时打开"),
            ["BluetoothUserService"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】与蓝牙有关，不需要的关闭；不影响无线鼠标"),
            ["BTAGService"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】与蓝牙有关，不需要的关闭；不影响无线鼠标"),
            ["bthserv"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】与蓝牙有关，不需要的关闭；不影响无线鼠标"),
            ["camsvc"] = new(ServiceRecommend.Manual, "应保留", "【应保留】禁用无法打开隐私权限，点隐私会闪退，语音通话无声音"),
            ["ClipSVC"] = new(ServiceRecommend.Manual, "应保留", "【应保留】商店许可相关，保留"),
            ["COMSysApp"] = new(ServiceRecommend.Manual, "应保留", "【应保留】COM+ 应用，保留"),
            ["CoreMessagingRegistrar"] = new(ServiceRecommend.Keep, "应保留", "【应保留】禁用后无法进入系统"),
            ["CryptSvc"] = new(ServiceRecommend.Auto, "应保留", "【应保留】加密服务，保留"),
            ["DeviceAssociationBroker"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】禁用不影响打印机使用"),
            ["DeviceAssociationService"] = new(ServiceRecommend.Manual, "应保留", "【应保留】禁用无法添加打印机"),
            ["DevicePickerUserSvc"] = new(ServiceRecommend.Manual, "应保留", "【应保留】禁用可能无法使用打印机"),
            ["DevicesFlowUserSvc"] = new(ServiceRecommend.Manual, "应保留", "【应保留】禁用可能无法使用打印机"),
            ["diagsvc"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】诊断模式，不需要的关闭"),
            ["DisplayEnhancementService"] = new(ServiceRecommend.Manual, "按需", "【按需】禁用后亮度无法调节"),
            ["Dnscache"] = new(ServiceRecommend.Auto, "应保留", "【应保留】DNS 客户端，保留"),
            ["dot3svc"] = new(ServiceRecommend.Manual, "按需", "【按需】有线 802.1X 时保留"),
            ["DPS"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】诊断模式，不需要的关闭"),
            ["DsmSvc"] = new(ServiceRecommend.Manual, "应保留", "【应保留】设备安装服务，保留"),
            ["DusmSvc"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】网络数据使用统计，用不到的关掉"),
            ["edgeupdate"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】Edge 浏览器更新服务，不需要时关闭"),
            ["edgeupdatem"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】Edge 浏览器更新服务，不需要时关闭"),
            ["EFS"] = new(ServiceRecommend.Manual, "按需", "【按需】解密文件时需要"),
            ["EventSystem"] = new(ServiceRecommend.Auto, "应保留", "【应保留】COM+ 事件，保留"),
            ["Fax"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】传真，一般人用不着"),
            ["FrameServer"] = new(ServiceRecommend.Manual, "按需", "【按需】摄像头帧服务，用摄像头时保留"),
            ["HvHost"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】HV 虚拟机，一般人不用，可禁或保留"),
            ["icssvc"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】移动热点，没啥用"),
            ["IKEEXT"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】没啥用，直接禁用"),
            ["iphlpsvc"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】IPv4↔IPv6 转换，暂时用不着"),
            ["KeyIso"] = new(ServiceRecommend.Manual, "应保留", "【应保留】CNG 密钥隔离，保留"),
            ["KtmRm"] = new(ServiceRecommend.Manual, "应保留", "【应保留】分布式事务，必须保留"),
            ["LanmanServer"] = new(ServiceRecommend.Manual, "按需", "【按需】文件或者打印机共享时保留"),
            ["LanmanWorkstation"] = new(ServiceRecommend.Manual, "按需", "【按需】访问网络共享或用企业邮箱时保留"),
            ["lfsvc"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】地址/定位服务，用不着可以关掉"),
            ["LicenseManager"] = new(ServiceRecommend.Manual, "应保留", "【应保留】微软系统许可服务，保留"),
            ["lmhosts"] = new(ServiceRecommend.Manual, "应保留", "【应保留】NetBIOS 辅助，保留"),
            ["LxpSvc"] = new(ServiceRecommend.Manual, "按需", "【按需】切换系统语言时保留"),
            ["MapsBroker"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】下载地图服务，用不着可以关掉"),
            ["MicrosoftEdgeElevationService"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】Edge 浏览器更新服务，不需要时关闭"),
            ["mpssvc"] = new(ServiceRecommend.Keep, "应保留", "【应保留】防火墙，建议保留"),
            ["msiserver"] = new(ServiceRecommend.Manual, "应保留", "【应保留】安装程序，保留"),
            ["NgcCtnrSvc"] = new(ServiceRecommend.Manual, "按需", "【按需】使用微软账户要保留"),
            ["NgcSvc"] = new(ServiceRecommend.Manual, "按需", "【按需】使用微软账户要保留"),
            ["PcaSvc"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】程序兼容性服务，一般没啥用"),
            ["PolicyAgent"] = new(ServiceRecommend.Manual, "应保留", "【应保留】IPsec 策略，必须保留"),
            ["PrintWorkflowUserSvc"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】可禁，不影响系统使用"),
            ["RasMan"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】个人电脑一般用不着远程控制"),
            ["RemoteRegistry"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】个人电脑一般用不着远程控制，建议禁"),
            ["RmSvc"] = new(ServiceRecommend.Manual, "按需", "【按需】禁用后网络无法进入飞行模式"),
            ["RpcEptMapper"] = new(ServiceRecommend.Auto, "应保留", "【应保留】RPC 映射，保留"),
            ["SamSs"] = new(ServiceRecommend.Auto, "应保留", "【应保留】账户管理，保留"),
            ["SCardSvr"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】外部智能卡不多用，可禁或保留"),
            ["ScDeviceEnum"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】外部智能卡不多用，可禁或保留"),
            ["Schedule"] = new(ServiceRecommend.Keep, "应保留", "【应保留】禁用后没有输入法指示器"),
            ["SCPolicySvc"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】外部智能卡不多用，可禁或保留"),
            ["SDRSVC"] = new(ServiceRecommend.Manual, "按需", "【按需】使用系统备份时保留"),
            ["SecurityHealthService"] = new(ServiceRecommend.Manual, "按需", "【按需】用微软杀毒软件的开着"),
            ["Sense"] = new(ServiceRecommend.Manual, "按需", "【按需】用微软杀毒/ATP 的开着"),
            ["SessionEnv"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】个人电脑一般用不着远程控制"),
            ["SgrmBroker"] = new(ServiceRecommend.Keep, "应保留", "【应保留】禁用后网卡属性空白、无法改 IP，且 Task Scheduler 无法启动"),
            ["Spooler"] = new(ServiceRecommend.Manual, "按需", "【按需】打印机，需要的保留"),
            ["sppsvc"] = new(ServiceRecommend.Auto, "应保留", "【应保留】软件保护，保留"),
            ["SysMain"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】没啥用，直接禁用"),
            ["TabletInputService"] = new(ServiceRecommend.Keep, "应保留", "【应保留】禁用后无法使用输入法"),
            ["TermService"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】个人电脑一般用不着远程控制"),
            ["Themes"] = new(ServiceRecommend.Auto, "应保留", "【应保留】系统主题，保留"),
            ["TimeBrokerSvc"] = new(ServiceRecommend.Manual, "应保留", "【应保留】时间代理，保留"),
            ["TokenBroker"] = new(ServiceRecommend.Manual, "按需", "【按需】使用微软账户要保留"),
            ["TroubleshootingSvc"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】推荐的解决问题服务，一般用不着"),
            ["TrustedInstaller"] = new(ServiceRecommend.Manual, "更新时开", "【更新时开】更新补丁时打开"),
            ["UnistoreSvc"] = new(ServiceRecommend.Manual, "按需", "【按需】使用微软账户要保留"),
            ["UserDataSvc"] = new(ServiceRecommend.Manual, "按需", "【按需】使用微软账户要保留"),
            ["UserManager"] = new(ServiceRecommend.Keep, "应保留", "【应保留】禁用后开始菜单会出错"),
            ["UsoSvc"] = new(ServiceRecommend.Manual, "更新时开", "【更新时开】系统更新时必须打开，否则设置里点更新会闪退"),
            ["VaultSvc"] = new(ServiceRecommend.Auto, "应保留", "【应保留】凭据管理，保留"),
            ["vds"] = new(ServiceRecommend.Manual, "按需", "【按需】从硬盘安装系统时需要"),
            ["vmicguestinterface"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】HV 虚拟机，一般人不用，可禁或保留"),
            ["vmicheartbeat"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】HV 虚拟机，一般人不用，可禁或保留"),
            ["vmickvpexchange"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】HV 虚拟机，一般人不用，可禁或保留"),
            ["vmicrdv"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】HV 虚拟机，一般人不用，可禁或保留"),
            ["vmicshutdown"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】HV 虚拟机，一般人不用，可禁或保留"),
            ["vmictimesync"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】HV 虚拟机，一般人不用，可禁或保留"),
            ["vmicvmsession"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】HV 虚拟机，一般人不用，可禁或保留"),
            ["vmicvss"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】HV 虚拟机，一般人不用，可禁或保留"),
            ["wbengine"] = new(ServiceRecommend.Manual, "按需", "【按需】使用系统备份时保留"),
            ["WbioSrvc"] = new(ServiceRecommend.Manual, "按需", "【按需】用到指纹识别时保留"),
            ["Wcmsvc"] = new(ServiceRecommend.Keep, "应保留", "【应保留】禁用后任务栏网络指示器会打 XX"),
            ["WdiServiceHost"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】诊断模式，不需要的关闭"),
            ["WdiSystemHost"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】诊断模式，不需要的关闭"),
            ["WebClient"] = new(ServiceRecommend.Manual, "按需", "【按需】使用微软账户要保留"),
            ["WEPHOSTSVC"] = new(ServiceRecommend.Manual, "按需", "【按需】解密文件时需要"),
            ["wercplsupport"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】系统问题报告控制面板，一般没啥用"),
            ["WerSvc"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】系统错误报告，可以关闭"),
            ["WinHttpAutoProxySvc"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】可禁，不影响系统使用"),
            ["wisvc"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】LTSC 里无需打开系统预览版服务"),
            ["Wlansvc"] = new(ServiceRecommend.Manual, "按需", "【按需】使用无线网时保留"),
            ["wlidsvc"] = new(ServiceRecommend.Manual, "按需", "【按需】使用微软账户要保留"),
            ["WpnUserService"] = new(ServiceRecommend.Keep, "应保留", "【应保留】禁用后无法打开网络设置"),
            ["wscsvc"] = new(ServiceRecommend.Auto, "应保留", "【应保留】安全中心，保留"),
            ["WSearch"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】占资源较大，可用 Everything 代替"),
            ["wuauserv"] = new(ServiceRecommend.Manual, "更新时开", "【更新时开】更新补丁时打开"),
            ["XblAuthManager"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】用不着 Xbox 的关掉"),
            ["XblGameSave"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】用不着 Xbox 的关掉"),
            ["XboxGipSvc"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】用不着 Xbox 的关掉"),
            ["XboxNetApiSvc"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】用不着 Xbox 的关掉"),
        };

    private static readonly Dictionary<string, Hint> ByDisplay =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Agent Activation Runtime"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】可禁，不影响系统使用"),
            ["Application Layer Gateway Service"] = new(ServiceRecommend.Manual, "按需", "【按需】使用系统防火墙则保留"),
            ["AppX Deployment Service (AppXSVC)"] = new(ServiceRecommend.Manual, "按需", "【按需】用微软商店和微软账户的保留"),
            ["Autodesk Desktop Licensing Service"] = new(ServiceRecommend.Manual, "按需", "【按需】AutoCAD 授权，不能关闭，否则无法打开 CAD"),
            ["Background Intelligent Transfer Service"] = new(ServiceRecommend.Manual, "更新时开", "【更新时开】更新补丁时打开"),
            ["BitLocker Drive Encryption Service"] = new(ServiceRecommend.Manual, "按需", "【按需】硬盘加密时保留"),
            ["Block Level Backup Engine Service"] = new(ServiceRecommend.Manual, "按需", "【按需】使用系统备份时保留"),
            ["Bluetooth Audio Gateway Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】与蓝牙有关，不需要的关闭；不影响无线鼠标"),
            ["Bluetooth Support Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】与蓝牙有关，不需要的关闭；不影响无线鼠标"),
            ["Bluetooth User Support Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】与蓝牙有关，不需要的关闭；不影响无线鼠标"),
            ["Capability Access Manager Service"] = new(ServiceRecommend.Manual, "应保留", "【应保留】禁用无法打开隐私权限，点隐私会闪退，语音通话无声音"),
            ["Client License Service (ClipSVC)"] = new(ServiceRecommend.Manual, "应保留", "【应保留】商店许可相关，保留"),
            ["CNG Key Isolation"] = new(ServiceRecommend.Manual, "应保留", "【应保留】CNG 密钥隔离，保留"),
            ["COM+ Event System"] = new(ServiceRecommend.Auto, "应保留", "【应保留】COM+ 事件，保留"),
            ["COM+ System Application"] = new(ServiceRecommend.Manual, "应保留", "【应保留】COM+ 应用，保留"),
            ["CoreMessaging"] = new(ServiceRecommend.Keep, "应保留", "【应保留】禁用后无法进入系统"),
            ["Credential Manager"] = new(ServiceRecommend.Auto, "应保留", "【应保留】凭据管理，保留"),
            ["Cryptographic Services"] = new(ServiceRecommend.Auto, "应保留", "【应保留】加密服务，保留"),
            ["Data Usage"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】网络数据使用统计，用不到的关掉"),
            ["Device Association Service"] = new(ServiceRecommend.Manual, "应保留", "【应保留】禁用无法添加打印机"),
            ["Device Setup Manager"] = new(ServiceRecommend.Manual, "应保留", "【应保留】设备安装服务，保留"),
            ["DeviceAssociationBroker"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】禁用不影响打印机使用"),
            ["DevicePicker"] = new(ServiceRecommend.Manual, "应保留", "【应保留】禁用可能无法使用打印机"),
            ["DevicesFlow"] = new(ServiceRecommend.Manual, "应保留", "【应保留】禁用可能无法使用打印机"),
            ["Diagnostic Execution Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】诊断模式，不需要的关闭"),
            ["Diagnostic Policy Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】诊断模式，不需要的关闭"),
            ["Diagnostic Service Host"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】诊断模式，不需要的关闭"),
            ["Diagnostic System Host"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】诊断模式，不需要的关闭"),
            ["Display Enhancement Service"] = new(ServiceRecommend.Manual, "按需", "【按需】禁用后亮度无法调节"),
            ["DNS Client"] = new(ServiceRecommend.Auto, "应保留", "【应保留】DNS 客户端，保留"),
            ["Downloaded Maps Manager"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】下载地图服务，用不着可以关掉"),
            ["Encrypting File System (EFS)"] = new(ServiceRecommend.Manual, "按需", "【按需】解密文件时需要"),
            ["Fax"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】传真，一般人用不着"),
            ["FlexNet Licensing Service"] = new(ServiceRecommend.Manual, "按需", "【按需】AutoCAD 授权，不能关闭，否则无法打开 CAD"),
            ["Foxit PDF Editor Update Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】Foxit PDF 服务，可以关掉"),
            ["GameDVR and Broadcast User Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】游戏录制与广播，用不着的关掉"),
            ["Gateway Session Service"] = new(ServiceRecommend.Manual, "按需", "【按需】VPN 服务，没有安装这个的忽略"),
            ["Gateway Updater Service"] = new(ServiceRecommend.Manual, "按需", "【按需】VPN 服务，没有安装这个的忽略"),
            ["Geolocation Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】地址服务，用不着可以关掉"),
            ["Huawei APO service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】华为电脑专属，可以禁用，不影响使用"),
            ["Huawei Hiview Windows Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】华为电脑专属，可以禁用，不影响使用"),
            ["Huawei LCD_Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】华为电脑专属，可以禁用，不影响使用"),
            ["Huawei PC Core Service"] = new(ServiceRecommend.Manual, "按需", "【按需】禁用后无法打开华为电脑管家"),
            ["Huawei PCManager Windows Service"] = new(ServiceRecommend.Manual, "按需", "【按需】禁用后无法打开华为电脑管家"),
            ["HV Host Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】HV 虚拟机，一般人不用，可禁或保留"),
            ["HW OSD Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】华为电脑专属，可以禁用，不影响使用"),
            ["Hyper-V Data Exchange Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】HV 虚拟机，一般人不用，可禁或保留"),
            ["Hyper-V Guest Service Interface"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】HV 虚拟机，一般人不用，可禁或保留"),
            ["Hyper-V Guest Shutdown Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】HV 虚拟机，一般人不用，可禁或保留"),
            ["Hyper-V Heartbeat Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】HV 虚拟机，一般人不用，可禁或保留"),
            ["Hyper-V PowerShell Direct Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】HV 虚拟机，一般人不用，可禁或保留"),
            ["Hyper-V Remote Desktop Virtualization Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】HV 虚拟机，一般人不用，可禁或保留"),
            ["Hyper-V Time Synchronization Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】HV 虚拟机，一般人不用，可禁或保留"),
            ["Hyper-V Volume Shadow Copy Requestor"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】HV 虚拟机，一般人不用，可禁或保留"),
            ["IKE and AuthIP IPsec Keying Modules"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】没啥用，直接禁用"),
            ["Intel(R) Audio Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】本机硬件驱动服务，可以关闭"),
            ["Intel(R) Capability Licensing Service TCP IP Interface"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】本机硬件驱动服务，可以关闭"),
            ["Intel(R) Content Protection HDCP Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】本机硬件驱动服务，可以关闭"),
            ["Intel(R) Content Protection HECI Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】本机硬件驱动服务，可以关闭"),
            ["Intel(R) Dynamic Application Loader Host Interface Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】本机硬件驱动服务，可以关闭"),
            ["Intel(R) Dynamic Tuning service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】本机硬件驱动服务，可以关闭"),
            ["Intel(R) Graphics Command Center Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】本机硬件驱动服务，可以关闭"),
            ["Intel(R) HD Graphics Control Panel Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】本机硬件驱动服务，可以关闭"),
            ["Intel(R) TPM Provisioning Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】本机硬件驱动服务，可以关闭"),
            ["Intel? PROSet/Wireless Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】本机硬件驱动服务，可以关闭"),
            ["Intel? SGX AESM"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】本机硬件驱动服务，可以关闭"),
            ["IP Helper"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】IPv4↔IPv6 转换，暂时用不着"),
            ["IPsec Policy Agent"] = new(ServiceRecommend.Manual, "应保留", "【应保留】IPsec 策略，必须保留"),
            ["KtmRm for Distributed Transaction Coordinator"] = new(ServiceRecommend.Manual, "应保留", "【应保留】分布式事务，必须保留"),
            ["Language Experience Service"] = new(ServiceRecommend.Manual, "按需", "【按需】切换系统语言时保留"),
            ["Microsoft Account Sign-in Assistant"] = new(ServiceRecommend.Manual, "按需", "【按需】使用微软账户要保留"),
            ["Microsoft Edge Elevation Service (MicrosoftEdgeElevationService)"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】Edge 浏览器更新服务，不需要时关闭"),
            ["Microsoft Edge Update Service (edgeupdate)"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】Edge 浏览器更新服务，不需要时关闭"),
            ["Microsoft Edge Update Service (edgeupdatem)"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】Edge 浏览器更新服务，不需要时关闭"),
            ["Microsoft Passport"] = new(ServiceRecommend.Manual, "按需", "【按需】使用微软账户要保留"),
            ["Microsoft Passport Container"] = new(ServiceRecommend.Manual, "按需", "【按需】使用微软账户要保留"),
            ["Nahimic service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】本机硬件驱动服务，可以关闭"),
            ["NVIDIA Display Container LS"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】本机硬件驱动服务，可以关闭"),
            ["NVIDIA LocalSystem Container"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】本机硬件驱动服务，可以关闭"),
            ["NVIDIA NetworkService Container"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】本机硬件驱动服务，可以关闭"),
            ["Print Spooler"] = new(ServiceRecommend.Manual, "按需", "【按需】打印机，需要的保留"),
            ["PrintWorkflow"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】可禁，不影响系统使用"),
            ["Problem Reports Control Panel Support"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】系统问题报告控制面板，一般没啥用"),
            ["Program Compatibility Assistant Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】程序兼容性服务，一般没啥用"),
            ["Radio Management Service"] = new(ServiceRecommend.Manual, "按需", "【按需】禁用后网络无法进入飞行模式"),
            ["Realtek Audio Universal Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】本机硬件驱动服务，可以关闭"),
            ["Recommended Troubleshooting Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】推荐的解决问题服务，一般用不着"),
            ["Remote Access Connection Manager"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】个人电脑一般用不着远程控制"),
            ["Remote Desktop Configuration"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】个人电脑一般用不着远程控制"),
            ["Remote Desktop Services"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】个人电脑一般用不着远程控制"),
            ["Remote Registry"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】个人电脑一般用不着远程控制，建议禁"),
            ["RPC Endpoint Mapper"] = new(ServiceRecommend.Auto, "应保留", "【应保留】RPC 映射，保留"),
            ["Security Accounts Manager"] = new(ServiceRecommend.Auto, "应保留", "【应保留】账户管理，保留"),
            ["Security Center"] = new(ServiceRecommend.Auto, "应保留", "【应保留】安全中心，保留"),
            ["Server"] = new(ServiceRecommend.Manual, "按需", "【按需】文件或者打印机共享时保留"),
            ["Smart Card"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】外部智能卡不多用，可禁或保留"),
            ["Smart Card Device Enumeration Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】外部智能卡不多用，可禁或保留"),
            ["Smart Card Removal Policy"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】外部智能卡不多用，可禁或保留"),
            ["Software Protection"] = new(ServiceRecommend.Auto, "应保留", "【应保留】软件保护，保留"),
            ["SysMain"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】没啥用，直接禁用"),
            ["System Guard Runtime Monitor Broker"] = new(ServiceRecommend.Keep, "应保留", "【应保留】禁用后网卡属性空白、无法改 IP，且 Task Scheduler 无法启动"),
            ["Task Scheduler"] = new(ServiceRecommend.Keep, "应保留", "【应保留】禁用后没有输入法指示器"),
            ["TCP/IP NetBIOS Helper"] = new(ServiceRecommend.Manual, "应保留", "【应保留】NetBIOS 辅助，保留"),
            ["Themes"] = new(ServiceRecommend.Auto, "应保留", "【应保留】系统主题，保留"),
            ["Time Broker"] = new(ServiceRecommend.Manual, "应保留", "【应保留】时间代理，保留"),
            ["Touch Keyboard and Handwriting Panel Service"] = new(ServiceRecommend.Keep, "应保留", "【应保留】禁用后无法使用输入法"),
            ["Update Orchestrator Service"] = new(ServiceRecommend.Manual, "更新时开", "【更新时开】系统更新时必须打开，否则设置里点更新会闪退"),
            ["User Data Access"] = new(ServiceRecommend.Manual, "按需", "【按需】使用微软账户要保留"),
            ["User Data Storage"] = new(ServiceRecommend.Manual, "按需", "【按需】使用微软账户要保留"),
            ["User Manager"] = new(ServiceRecommend.Keep, "应保留", "【应保留】禁用后开始菜单会出错"),
            ["Virtual Disk"] = new(ServiceRecommend.Manual, "按需", "【按需】从硬盘安装系统时需要"),
            ["Web Account Manager"] = new(ServiceRecommend.Manual, "按需", "【按需】使用微软账户要保留"),
            ["WebClient"] = new(ServiceRecommend.Manual, "按需", "【按需】使用微软账户要保留"),
            ["Windows Audio"] = new(ServiceRecommend.Auto, "应保留", "【应保留】音频，保留"),
            ["Windows Audio Endpoint Builder"] = new(ServiceRecommend.Auto, "应保留", "【应保留】音频终结点，保留"),
            ["Windows Backup"] = new(ServiceRecommend.Manual, "按需", "【按需】使用系统备份时保留"),
            ["Windows Biometric Service"] = new(ServiceRecommend.Manual, "按需", "【按需】用到指纹识别时保留"),
            ["Windows Camera Frame Server"] = new(ServiceRecommend.Manual, "按需", "【按需】摄像头帧服务，用摄像头时保留"),
            ["Windows Connection Manager"] = new(ServiceRecommend.Keep, "应保留", "【应保留】禁用后任务栏网络指示器会打 XX"),
            ["Windows Defender Advanced Threat Protection Service"] = new(ServiceRecommend.Manual, "按需", "【按需】用微软杀毒/ATP 的开着"),
            ["Windows Defender Firewall"] = new(ServiceRecommend.Keep, "应保留", "【应保留】防火墙，建议保留"),
            ["Windows Encryption Provider Host Service"] = new(ServiceRecommend.Manual, "按需", "【按需】解密文件时需要"),
            ["Windows Error Reporting Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】系统错误报告，可以关闭"),
            ["Windows Insider Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】LTSC 里无需打开系统预览版服务"),
            ["Windows Installer"] = new(ServiceRecommend.Manual, "应保留", "【应保留】安装程序，保留"),
            ["Windows License Manager Service"] = new(ServiceRecommend.Manual, "应保留", "【应保留】微软系统许可服务，保留"),
            ["Windows Mobile Hotspot Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】移动热点，没啥用"),
            ["Windows Modules Installer"] = new(ServiceRecommend.Manual, "更新时开", "【更新时开】更新补丁时打开"),
            ["Windows Push Notifications User Service"] = new(ServiceRecommend.Keep, "应保留", "【应保留】禁用后无法打开网络设置"),
            ["Windows Search"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】占资源较大，可用 Everything 代替"),
            ["Windows Security Service"] = new(ServiceRecommend.Manual, "按需", "【按需】用微软杀毒软件的开着"),
            ["Windows Update"] = new(ServiceRecommend.Manual, "更新时开", "【更新时开】更新补丁时打开"),
            ["WinHTTP Web Proxy Auto-Discovery Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】可禁，不影响系统使用"),
            ["Wired AutoConfig"] = new(ServiceRecommend.Manual, "按需", "【按需】有线 802.1X 时保留"),
            ["WLAN AutoConfig"] = new(ServiceRecommend.Manual, "按需", "【按需】使用无线网时保留"),
            ["Workstation"] = new(ServiceRecommend.Manual, "按需", "【按需】访问网络共享或用企业邮箱时保留"),
            ["Xbox Accessory Management Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】用不着 Xbox 的关掉"),
            ["Xbox Live Auth Manager"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】用不着 Xbox 的关掉"),
            ["Xbox Live Game Save"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】用不着 Xbox 的关掉"),
            ["Xbox Live Networking Service"] = new(ServiceRecommend.Disable, "应禁用", "【应禁用】用不着 Xbox 的关掉"),
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
