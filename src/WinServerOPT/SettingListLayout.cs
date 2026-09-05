namespace WinOpt;

/// <summary>批量设置列表的列坐标（表头与行必须一致，避免开关被文字盖住）。</summary>
internal static class SettingListLayout
{
    public const int InfoX = 8;
    public const int ItemX = 28;
    /// <summary>「优化建议值」表头起点（开关居中对齐其下）。</summary>
    public const int RecommendHeaderX = 448;
    public const int RecommendHeaderW = 90;
    public const int ToggleX = 466;
    public const int ToggleW = 52;
    /// <summary>系统默认值列。</summary>
    public const int SystemX = 560;
    public const int SystemW = 100;
    /// <summary>系统当前值列。</summary>
    public const int CurrentX = 680;
    public const int CurrentW = 100;
    /// <summary>「说明」列放最后，保证项目名能看全。</summary>
    public const int NoteX = 800;
    public const int NoteW = 280;
    /// <summary>项目文字右缘与开关左缘的间隙。</summary>
    public const int TextToggleGap = 12;
}
