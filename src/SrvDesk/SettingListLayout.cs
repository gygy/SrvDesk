namespace SrvDesk;

/// <summary>批量设置列表的列坐标（表头与行必须一致，避免开关被文字盖住）。</summary>
internal static class SettingListLayout
{
    public const int InfoX = 8;
    public const int ItemX = 28;
    /// <summary>「设置操作」表头起点（开关/下拉居中对齐其下）。</summary>
    public const int RecommendHeaderX = 348;
    public const int RecommendHeaderW = 120;
    public const int ToggleX = 376;
    public const int ToggleW = 52;
    /// <summary>下拉选择控件（多档设置）。</summary>
    public const int ChoiceX = 350;
    public const int ChoiceW = 114;
    /// <summary>行内「配置脚本」图标按钮宽度（16px 图标 + 边距）。</summary>
    public const int ScriptW = 22;
    public static int ScriptX => ChoiceX - ScriptW - 4;
    /// <summary>系统默认值列。</summary>
    public const int SystemX = 478;
    public const int SystemW = 88;
    /// <summary>系统当前值列（需容纳「开启/关闭/已优化」及下拉文案）。</summary>
    public const int CurrentX = 574;
    public const int CurrentW = 100;
    /// <summary>推荐强度（五星，紧凑绘制约 55px）。</summary>
    public const int LevelX = 682;
    public const int LevelW = 72;
    /// <summary>「说明」列放最后；起点需小于内容区最小宽度，否则会被裁切且无省略号。</summary>
    public const int NoteX = 762;
    public const int NoteW = 200;
    /// <summary>项目文字右缘与开关左缘的间隙。</summary>
    public const int TextToggleGap = 12;

    public static int NoteWidthFor(int rowWidth) =>
        Math.Max(72, rowWidth - NoteX - 10);
}
