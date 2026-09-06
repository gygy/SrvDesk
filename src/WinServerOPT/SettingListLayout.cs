namespace WinOpt;

/// <summary>批量设置列表的列坐标（表头与行必须一致，避免开关被文字盖住）。</summary>
internal static class SettingListLayout
{
    public const int InfoX = 8;
    public const int ItemX = 28;
    /// <summary>「设置操作」表头起点（开关居中对齐其下）。</summary>
    public const int RecommendHeaderX = 360;
    public const int RecommendHeaderW = 84;
    public const int ToggleX = 376;
    public const int ToggleW = 52;
    /// <summary>行内「配置脚本」图标按钮宽度（16px 图标 + 边距）。</summary>
    public const int ScriptW = 22;
    public static int ScriptX => ToggleX - ScriptW - 4;
    /// <summary>系统默认值列。</summary>
    public const int SystemX = 456;
    public const int SystemW = 88;
    /// <summary>系统当前值列。</summary>
    public const int CurrentX = 552;
    public const int CurrentW = 96;
    /// <summary>推荐强度（五星，紧凑绘制约 55px）。</summary>
    public const int LevelX = 652;
    public const int LevelW = 72;
    /// <summary>「说明」列放最后；起点需小于内容区最小宽度，否则会被裁切且无省略号。</summary>
    public const int NoteX = 732;
    public const int NoteW = 200;
    /// <summary>项目文字右缘与开关左缘的间隙。</summary>
    public const int TextToggleGap = 12;

    public static int NoteWidthFor(int rowWidth) =>
        Math.Max(72, rowWidth - NoteX - 10);
}
