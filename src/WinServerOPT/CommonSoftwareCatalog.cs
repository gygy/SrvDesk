namespace WinOpt;

internal sealed class CommonSoftwareItem
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Category { get; set; } = "";
    public string WingetId { get; set; } = "";
    public string[] DetectPatterns { get; set; } = [];
    public string DownloadUrl { get; set; } = "";
    public bool Essential { get; set; }
}

internal static class CommonSoftwareCatalog
{
    public static IReadOnlyList<CommonSoftwareItem> All { get; } =
    [
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

        Item("qq-classic", "腾讯QQ（经典版）", "通讯", "Tencent.QQ",
            ["腾讯QQ"], "https://im.qq.com/pcqq", essential: false),
        Item("qq-nt", "腾讯QQ（全新体验版）", "通讯", "Tencent.QQ.NT",
            ["QQ NT", "腾讯QQ NT"], "https://im.qq.com/qq/newqq/index.html", essential: false),
        Item("wechat", "微信电脑版", "通讯", "Tencent.WeChat",
            ["微信"], "https://weixin.qq.com/", essential: false),
        Item("tim", "腾讯TIM（QQ简化版）", "通讯", "Tencent.TIM",
            ["TIM"], "https://office.qq.com/", essential: false),

        Item("baidunetdisk", "百度网盘", "网盘", "Baidu.BaiduNetdisk",
            ["百度网盘"], "https://pan.baidu.com/download", essential: false),
        Item("aliyundrive", "阿里云盘", "网盘", "Alibaba.aDrive",
            ["阿里云盘"], "https://www.aliyundrive.com/download", essential: false),

        Item("git", "Git For Windows", "开发", "Git.Git",
            ["Git"], "https://git-scm.com/download/win", essential: false),
        Item("tortoisegit", "TortoiseGit简体中文版", "开发", "TortoiseGit.TortoiseGit",
            ["TortoiseGit"], "https://tortoisegit.org/download/", essential: false),
    ];

    public static CommonSoftwareItem? Find(string id) =>
        All.FirstOrDefault(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    private static CommonSoftwareItem Item(
        string id, string title, string category, string wingetId,
        string[] detect, string downloadUrl, bool essential) => new()
    {
        Id = id,
        Title = title,
        Category = category,
        WingetId = wingetId,
        DetectPatterns = detect,
        DownloadUrl = downloadUrl,
        Essential = essential,
    };
}
