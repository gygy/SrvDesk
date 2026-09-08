namespace SrvDesk;

/// <summary>批量设置列表的列坐标（表头与行必须一致；随 DPI 缩放）。</summary>
internal static class SettingListLayout
{
    public static int InfoX => UiScale.S(8);
    public static int ItemX => UiScale.S(28);
    /// <summary>「设置操作」表头起点（开关/下拉居中对齐其下）。</summary>
    public static int RecommendHeaderX => UiScale.S(348);
    public static int RecommendHeaderW => UiScale.S(120);
    public static int ToggleX => UiScale.S(376);
    public static int ToggleW => UiScale.S(52);
    /// <summary>下拉选择控件（多档设置）。</summary>
    public static int ChoiceX => UiScale.S(350);
    public static int ChoiceW => UiScale.S(114);
    /// <summary>行内「配置脚本」图标按钮宽度。</summary>
    public static int ScriptW => UiScale.S(22);
    public static int ScriptX => ChoiceX - ScriptW - UiScale.S(4);
    /// <summary>系统默认值列（加宽避免「系统默认值」被裁成「系统…」）。</summary>
    public static int SystemX => UiScale.S(478);
    public static int SystemW => UiScale.S(100);
    /// <summary>系统当前值列。</summary>
    public static int CurrentX => UiScale.S(586);
    public static int CurrentW => UiScale.S(100);
    /// <summary>推荐强度（五星；宽度与 RecommendLevelUi 几何星一致，避免挤扁）。</summary>
    public static int LevelX => UiScale.S(686);
    public static int LevelW => Math.Max(UiScale.S(88), RecommendLevelUi.PreferredColumnWidth);
    /// <summary>「说明」列。</summary>
    public static int NoteX => LevelX + LevelW + UiScale.S(8);
    public static int NoteW => UiScale.S(200);
    public static int TextToggleGap => UiScale.S(12);

    /// <summary>标题与适用范围两行之间的空隙。</summary>
    public static int TitleScopeGap => Math.Max(2, UiScale.S(2));

    public static Font ItemFont => UiFit.UiFont;
    public static Font ScopeFont => UiFit.UiFontScope;
    public static Font NoteFont => UiFit.UiFontSmall;

    /// <summary>含适用范围副标题时的行高（随 DPI / 字体实测）。</summary>
    public static int RowHeight
    {
        get
        {
            var title = UiFit.LineHeight(ItemFont);
            var scope = UiFit.LineHeight(ScopeFont);
            return Math.Max(UiScale.S(48), UiScale.S(6) + title + TitleScopeGap + scope + UiScale.S(6));
        }
    }

    public static int NoteWidthFor(int rowWidth) =>
        Math.Max(UiScale.S(72), rowWidth - NoteX - UiScale.S(10));
}
