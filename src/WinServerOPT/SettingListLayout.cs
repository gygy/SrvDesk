namespace WinOpt;

/// <summary>批量设置列表的列坐标（表头与行必须一致，避免开关被文字盖住）。</summary>
internal static class SettingListLayout
{
    public const int InfoX = 16;
    public const int ItemX = 36;
    /// <summary>「优化建议值」表头起点（开关居中对齐其下）。</summary>
    public const int RecommendHeaderX = 420;
    public const int RecommendHeaderW = 100;
    public const int ToggleX = 442;
    public const int ToggleW = 56;
    /// <summary>系统默认值列。</summary>
    public const int SystemX = 560;
    public const int SystemW = 120;
    /// <summary>系统当前值列。</summary>
    public const int CurrentX = 700;
    public const int CurrentW = 120;
    /// <summary>项目文字右缘与开关左缘的间隙。</summary>
    public const int TextToggleGap = 12;
}
