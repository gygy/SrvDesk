namespace WinOpt;

/// <summary>批量设置列表的列坐标（表头与行必须一致，避免开关被文字盖住）。</summary>
internal static class SettingListLayout
{
    public const int InfoX = 16;
    public const int ItemX = 36;
    public const int RecommendHeaderX = 448;
    public const int RecommendHeaderW = 160;
    public const int ToggleX = 500;
    public const int ToggleW = 56;
    public const int SystemX = 628;
    public const int SystemW = 160;
    /// <summary>项目文字右缘与开关左缘的间隙。</summary>
    public const int TextToggleGap = 12;
}
