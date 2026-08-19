namespace WinOpt;

/// <summary>设置项的平台与版本适用范围。</summary>
internal sealed class SettingScope
{
    public bool ServerOnly { get; }
    public bool RequiresDesktopExperience { get; }
    public string? MinServer { get; }
    public string? MinWindows { get; }
    public string? Note { get; }

    public SettingScope(
        bool serverOnly = false,
        bool requiresDesktopExperience = false,
        string? minServer = null,
        string? minWindows = null,
        string? note = null)
    {
        ServerOnly = serverOnly;
        RequiresDesktopExperience = requiresDesktopExperience;
        MinServer = minServer;
        MinWindows = minWindows;
        Note = note;
    }

    public static SettingScope Universal { get; } = new();

    public static SettingScope ServerExclusive { get; } = new(serverOnly: true);

    public static SettingScope ServerDesktop { get; } = new(serverOnly: true, requiresDesktopExperience: true);

    public static SettingScope DesktopExperience { get; } = new(requiresDesktopExperience: true);

    public bool HasBadge =>
        ServerOnly || RequiresDesktopExperience || MinServer is not null || MinWindows is not null;

    public string FormatBadges()
    {
        if (!HasBadge) return "";

        var parts = new List<string>();
        if (ServerOnly) parts.Add("Server 专属");
        if (RequiresDesktopExperience) parts.Add("需桌面体验");
        if (MinServer is not null) parts.Add($"Server {MinServer}");
        if (MinWindows is not null) parts.Add(MinWindows);
        return string.Join(" · ", parts);
    }

    public string FormatHelpSection()
    {
        if (!HasBadge && Note is null)
            return "【适用范围】Windows Server 2016 及以上（含桌面体验的标准安装）；与 Windows 10/11 通用的底层策略。";

        var lines = new List<string>();
        if (ServerOnly)
            lines.Add("仅适用于 Windows Server，不适用于 Windows 10/11 客户端。");
        else
            lines.Add("适用于 Windows Server；部分策略在 Windows 10/11 上同样存在。");

        if (RequiresDesktopExperience)
            lines.Add("需在 Server 中安装「桌面体验」角色；Server Core 无资源管理器/主题，此项无效。");

        if (MinServer is not null)
            lines.Add($"最低 Windows Server 版本：{MinServer}。");

        if (MinWindows is not null)
            lines.Add($"依赖 {MinWindows} 引入的系统组件或策略。");

        if (Note is not null)
            lines.Add(Note);

        return "【适用范围】" + string.Join("", lines);
    }
}
