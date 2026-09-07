namespace SrvDesk;

internal sealed class CommonSoftwareItem
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Category { get; set; } = "";
    public string WingetId { get; set; } = "";
    public string[] DetectPatterns { get; set; } = [];
    public string DownloadUrl { get; set; } = "";
    public bool Essential { get; set; }

    /// <summary>官方离线 EXE 安装包直链（Appx 旁加载失败时回退）。</summary>
    public string OfflineInstallerUrl { get; set; } = "";

    /// <summary>离线 EXE 安装参数，如 /quiet /norestart。</summary>
    public string OfflineInstallArgs { get; set; } = "";

    /// <summary>为 true 时先尝试 EXE 离线包（无 Appx 旁加载时）。</summary>
    public bool PreferOfflineInstall { get; set; }

    /// <summary>Windows Server 上优先离线包；非 Server 优先 winget。</summary>
    public bool PreferOfflineOnServer { get; set; }

    /// <summary>离线包为便携 EXE（非安装程序）：下载到本机工具目录并加入用户 PATH。</summary>
    public bool OfflinePortable { get; set; }

    /// <summary>通过可执行文件名检测是否已安装（如 codex.exe）。</summary>
    public string[] DetectExeNames { get; set; } = [];

    /// <summary>用户自定义项（来自 AppData，可用 winget 安装）。</summary>
    public bool IsCustom { get; set; }

    /// <summary>微软商店 ProductId（如 9PKTQ5699M62），用于解析 Appx 直链。</summary>
    public string StoreProductId { get; set; } = "";

    /// <summary>Appx 包族名前缀，如 AppleInc.iCloud。</summary>
    public string AppxPackageName { get; set; } = "";

    /// <summary>优先 Appx/Msix 旁加载（Server 无商店时推荐）。</summary>
    public bool PreferAppxSideload { get; set; }

    public bool IsWingetBootstrap => Id.Equals("winget", StringComparison.OrdinalIgnoreCase);
}

internal static class CommonSoftwareCatalog
{
    public const string CustomCategory = "自定义";

    /// <summary>内置目录（不含用户自定义）。</summary>
    public static IReadOnlyList<CommonSoftwareItem> BuiltIn { get; } =
    [
        Item("winget", "Windows 包管理器 (winget)", "必备", "Microsoft.AppInstaller",
            ["App Installer", "Windows Package Manager"], "https://aka.ms/getwinget", essential: true),
        Item("winrar", "WinRAR官方简体中文注册版", "必备", "RARLab.WinRAR",
            ["WinRAR"], "https://www.win-rar.com/download.html", essential: true),
        Item("notepad3", "NotePad3（替代记事本）", "必备", "Rizonesoft.Notepad3",
            ["Notepad3"], "https://www.rizonesoft.com/downloads/notepad3/", essential: true),
        Item("xnviewmp", "XnViewMP看图软件", "必备", "XnSoft.XnViewMP",
            ["XnView MP", "XnViewMP"], "https://www.xnview.com/en/xnviewmp/", essential: true),
        Item("potplayer", "PotPlayer媒体播放器", "必备", "Daum.PotPlayer",
            ["PotPlayer"], "https://potplayer.daum.net/", essential: true),
        Item("7zip", "7-Zip压缩解压软件", "必备", "7zip.7zip",
            ["7-Zip"], "https://www.7-zip.org/download.html", essential: true),
        Item("everything", "Everything极速文件搜索", "必备", "voidtools.Everything",
            ["Everything"], "https://www.voidtools.com/downloads/", essential: true),

        Item("git", "Git For Windows", "开发", "Git.Git",
            ["Git"], "https://git-scm.com/download/win", essential: false),
        Item("notepadpp", "Notepad++", "开发", "Notepad++.Notepad++",
            ["Notepad++"], "https://notepad-plus-plus.org/downloads/", essential: false),
        Item("tortoisegit", "TortoiseGit简体中文版", "开发", "TortoiseGit.TortoiseGit",
            ["TortoiseGit"], "https://tortoisegit.org/download/", essential: false),

        Item("codex", "OpenAI Codex CLI", "AI", "OpenAI.Codex",
            ["Codex CLI", "OpenAI Codex CLI"],
            "https://github.com/openai/codex/releases",
            essential: false,
            offlineInstallerUrl: "https://github.com/openai/codex/releases/latest/download/codex-x86_64-pc-windows-msvc.exe",
            preferOfflineOnServer: true,
            offlinePortable: true,
            detectExeNames: ["codex.exe"]),
        Item("codex-desktop", "OpenAI Codex Desktop（ChatGPT）", "AI", "9PLM9XGG6VKS",
            ["ChatGPT", "OpenAI Codex"],
            "https://openai.com/codex/",
            essential: false,
            storeProductId: "9PLM9XGG6VKS",
            appxPackageName: "OpenAI.Codex",
            preferAppxSideload: true),
        Item("pi-agent", "Pi Agent（终端 AI 编程助手）", "AI", "EarendilWorks.pi",
            ["pi", "EarendilWorks.pi", "Pi Agent"],
            "https://pi.dev/",
            essential: false,
            detectExeNames: ["pi.exe"]),

        Item("geek", "Geek Uninstaller（深度卸载）", "工具", "GeekUninstaller.GeekUninstaller",
            ["Geek Uninstaller", "Geek"], "https://geekuninstaller.com/download", essential: false),
        Item("virt-viewer", "Virt Viewer（SPICE/VNC 远程桌面）", "工具", "RedHat.VirtViewer",
            ["Virt Viewer", "VirtViewer", "virt-viewer"], "https://virt-manager.org/", essential: false),
        Item("neatdm", "Neat Download Manager", "工具", "JavadMotallebi.NeatDownloadManager",
            ["Neat Download Manager", "NeatDM", "Neat DownloadManager"],
            "https://www.neatdownloadmanager.com/",
            essential: false,
            offlineInstallerUrl: "https://www.neatdownloadmanager.com/file/NeatDM_setup.exe",
            offlineInstallArgs: "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART"),
        Item("pdfgear", "PDFgear（免费 PDF 编辑）", "工具", "PDFgear.PDFgear",
            ["PDFgear", "PDF gear"], "https://www.pdfgear.com/download.htm", essential: false),
        Item("stirling-pdf", "Stirling-PDF（本地 PDF 工具箱）", "工具", "StirlingTools.StirlingPDF",
            ["Stirling-PDF", "Stirling PDF"], "https://github.com/Stirling-Tools/Stirling-PDF/releases", essential: false),
        Item("zotero", "Zotero（文献管理）", "工具", "DigitalScholar.Zotero",
            ["Zotero"], "https://www.zotero.org/download/", essential: false),
        Item("hikconnect", "海康互联", "工具", "",
            ["海康互联"], "https://www.hikiot.com/download", essential: false),
        Item("pixpin", "PixPin（截图贴图）", "工具", "PixPin.PixPin",
            ["PixPin"], "https://pixpin.cn/", essential: false),
        Item("bitwarden", "Bitwarden（密码管理）", "工具", "Bitwarden.Bitwarden",
            ["Bitwarden"], "https://bitwarden.com/download/", essential: false),

        Item("yandex", "Yandex 浏览器", "浏览器", "Yandex.Browser",
            ["Yandex Browser", "Yandex"], "https://browser.yandex.com/", essential: false),
        Item("vivaldi", "Vivaldi 浏览器", "浏览器", "Vivaldi.Vivaldi",
            ["Vivaldi"], "https://vivaldi.com/download/", essential: false),
        Item("chrome", "Google Chrome", "浏览器", "Google.Chrome",
            ["Google Chrome", "Chrome"], "https://www.google.com/chrome/", essential: false),

        Item("qq-classic", "腾讯QQ（经典版）", "通讯", "Tencent.QQ",
            ["腾讯QQ"], "https://im.qq.com/pcqq", essential: false),
        Item("qq-nt", "腾讯QQ（全新体验版）", "通讯", "Tencent.QQ.NT",
            ["QQ NT", "腾讯QQ NT"], "https://im.qq.com/qq/newqq/index.html", essential: false),
        Item("wechat", "微信电脑版", "通讯", "Tencent.WeChat",
            ["微信"], "https://weixin.qq.com/", essential: false),
        Item("tim", "腾讯TIM（QQ简化版）", "通讯", "Tencent.TIM",
            ["TIM"], "https://office.qq.com/", essential: false),
        Item("mailmaster", "网易邮箱大师", "通讯", "NetEase.MailMaster",
            ["网易邮箱大师", "MailMaster"], "https://dashi.163.com/", essential: false),

        Item("baidunetdisk", "百度网盘", "网盘", "Baidu.BaiduNetdisk",
            ["百度网盘"], "https://pan.baidu.com/download", essential: false),
        Item("aliyundrive", "阿里云盘", "网盘", "Alibaba.aDrive",
            ["阿里云盘"], "https://www.aliyundrive.com/download", essential: false),
        Item("tianyiyun", "天翼云盘", "网盘", "",
            ["天翼云盘", "Cloud189", "eCloud"], "https://cloud.189.cn/", essential: false),
        // Server 无商店：离线自动安装（Appx 旁加载 → EXE 回退）
        Item("icloud", "iCloud for Windows（离线自动安装）", "网盘", "Apple.iCloud",
            ["iCloud"], "https://support.apple.com/zh-cn/103232", essential: false,
            storeProductId: "9PKTQ5699M62",
            appxPackageName: "AppleInc.iCloud",
            preferAppxSideload: true,
            offlineInstallerUrl: "https://updates.cdn-apple.com/2020/windows/001-39935-20200911-1A70AA56-F448-11EA-8CC0-99D41950005E/iCloudSetup.exe",
            offlineInstallArgs: "/quiet /norestart",
            preferOfflineInstall: true),

        // 微软运行库：按需勾选。多数软件只需 2015–2022 x64；装 32 位软件再补 x86
        Item("vcredist-2022-x64", "Visual C++ 2015–2022 (x64) · 推荐", "微软运行库",
            "Microsoft.VCRedist.2015+.x64",
            [
                "Microsoft Visual C++ 2015-2022 Redistributable (x64)",
                "Microsoft Visual C++ 2015-2019 Redistributable (x64)",
                "Microsoft Visual C++ 2017 Redistributable (x64)",
            ],
            "https://learn.microsoft.com/cpp/windows/latest-supported-vc-redist",
            essential: false,
            offlineInstallerUrl: "https://aka.ms/vs/17/release/vc_redist.x64.exe",
            offlineInstallArgs: "/install /quiet /norestart",
            preferOfflineInstall: true),
        Item("vcredist-2022-x86", "Visual C++ 2015–2022 (x86)", "微软运行库",
            "Microsoft.VCRedist.2015+.x86",
            [
                "Microsoft Visual C++ 2015-2022 Redistributable (x86)",
                "Microsoft Visual C++ 2015-2019 Redistributable (x86)",
            ],
            "https://learn.microsoft.com/cpp/windows/latest-supported-vc-redist",
            essential: false,
            offlineInstallerUrl: "https://aka.ms/vs/17/release/vc_redist.x86.exe",
            offlineInstallArgs: "/install /quiet /norestart",
            preferOfflineInstall: true),
        Item("vcredist-2013-x64", "Visual C++ 2013 (x64)", "微软运行库",
            "Microsoft.VCRedist.2013.x64",
            ["Microsoft Visual C++ 2013 Redistributable (x64)"],
            "https://learn.microsoft.com/cpp/windows/latest-supported-vc-redist",
            essential: false,
            offlineInstallerUrl: "https://aka.ms/highdpimfc2013x64enu",
            offlineInstallArgs: "/install /quiet /norestart",
            preferOfflineInstall: true),
        Item("vcredist-2013-x86", "Visual C++ 2013 (x86)", "微软运行库",
            "Microsoft.VCRedist.2013.x86",
            ["Microsoft Visual C++ 2013 Redistributable (x86)"],
            "https://learn.microsoft.com/cpp/windows/latest-supported-vc-redist",
            essential: false,
            offlineInstallerUrl: "https://aka.ms/highdpimfc2013x86enu",
            offlineInstallArgs: "/install /quiet /norestart",
            preferOfflineInstall: true),
        Item("vcredist-2012-x64", "Visual C++ 2012 (x64)", "微软运行库",
            "Microsoft.VCRedist.2012.x64",
            ["Microsoft Visual C++ 2012 Redistributable (x64)"],
            "https://learn.microsoft.com/cpp/windows/latest-supported-vc-redist",
            essential: false),
        Item("vcredist-2012-x86", "Visual C++ 2012 (x86)", "微软运行库",
            "Microsoft.VCRedist.2012.x86",
            ["Microsoft Visual C++ 2012 Redistributable (x86)"],
            "https://learn.microsoft.com/cpp/windows/latest-supported-vc-redist",
            essential: false),
        Item("vcredist-2010-x64", "Visual C++ 2010 (x64)", "微软运行库",
            "Microsoft.VCRedist.2010.x64",
            ["Microsoft Visual C++ 2010  x64 Redistributable", "Microsoft Visual C++ 2010 Redistributable (x64)"],
            "https://learn.microsoft.com/cpp/windows/latest-supported-vc-redist",
            essential: false),
        Item("vcredist-2010-x86", "Visual C++ 2010 (x86)", "微软运行库",
            "Microsoft.VCRedist.2010.x86",
            ["Microsoft Visual C++ 2010  x86 Redistributable", "Microsoft Visual C++ 2010 Redistributable (x86)"],
            "https://learn.microsoft.com/cpp/windows/latest-supported-vc-redist",
            essential: false),
        Item("dotnet-desktop-8", ".NET Desktop Runtime 8 (x64)", "微软运行库",
            "Microsoft.DotNet.DesktopRuntime.8",
            [".NET Desktop Runtime 8", "Microsoft Windows Desktop Runtime - 8"],
            "https://dotnet.microsoft.com/download/dotnet/8.0",
            essential: false),
        Item("dotnet-desktop-6", ".NET Desktop Runtime 6 (x64)", "微软运行库",
            "Microsoft.DotNet.DesktopRuntime.6",
            [".NET Desktop Runtime 6", "Microsoft Windows Desktop Runtime - 6"],
            "https://dotnet.microsoft.com/download/dotnet/6.0",
            essential: false),
        Item("dotnet-aspnet-8", "ASP.NET Core Runtime 8 (x64)", "微软运行库",
            "Microsoft.DotNet.AspNetCore.8",
            ["Microsoft ASP.NET Core 8", "ASP.NET Core 8"],
            "https://dotnet.microsoft.com/download/dotnet/8.0",
            essential: false),
    ];

    /// <summary>内置 + 用户自定义（每次读取最新自定义列表）。</summary>
    public static IReadOnlyList<CommonSoftwareItem> All => GetAll();

    public static IReadOnlyList<CommonSoftwareItem> GetAll()
    {
        var custom = CustomSoftwareStore.LoadAsItems();
        if (custom.Count == 0) return BuiltIn;
        var list = new List<CommonSoftwareItem>(BuiltIn.Count + custom.Count);
        list.AddRange(BuiltIn);
        list.AddRange(custom);
        return list;
    }

    public static CommonSoftwareItem? Find(string id) =>
        GetAll().FirstOrDefault(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    private static CommonSoftwareItem Item(
        string id, string title, string category, string wingetId,
        string[] detect, string downloadUrl, bool essential,
        string offlineInstallerUrl = "",
        string offlineInstallArgs = "",
        bool preferOfflineInstall = false,
        bool preferOfflineOnServer = false,
        bool offlinePortable = false,
        string[]? detectExeNames = null,
        string storeProductId = "",
        string appxPackageName = "",
        bool preferAppxSideload = false) => new()
    {
        Id = id,
        Title = title,
        Category = category,
        WingetId = wingetId,
        DetectPatterns = detect,
        DownloadUrl = downloadUrl,
        Essential = essential,
        OfflineInstallerUrl = offlineInstallerUrl,
        OfflineInstallArgs = offlineInstallArgs,
        PreferOfflineInstall = preferOfflineInstall,
        PreferOfflineOnServer = preferOfflineOnServer,
        OfflinePortable = offlinePortable,
        DetectExeNames = detectExeNames ?? [],
        StoreProductId = storeProductId,
        AppxPackageName = appxPackageName,
        PreferAppxSideload = preferAppxSideload,
    };
}
