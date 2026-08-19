namespace WinOpt;

internal static class AppTheme
{
    // 主色：舒适 Windows 蓝（与图标渐变一致）
    public static readonly Color Primary = Color.FromArgb(42, 140, 240);
    public static readonly Color PrimaryDark = Color.FromArgb(13, 91, 184);
    public static readonly Color PrimaryDeep = Color.FromArgb(11, 61, 145);
    public static readonly Color PrimarySoft = Color.FromArgb(77, 163, 255);
    public static readonly Color PrimaryLight = Color.FromArgb(232, 244, 253);
    public static readonly Color PrimaryPale = Color.FromArgb(245, 249, 255);

    // 界面基底
    public static readonly Color Surface = Color.FromArgb(248, 250, 252);
    public static readonly Color SurfaceCard = Color.White;
    public static readonly Color NavBg = Color.FromArgb(241, 246, 252);
    public static readonly Color NavHover = Color.FromArgb(224, 236, 252);
    public static readonly Color RowAlt = Color.FromArgb(237, 246, 255);
    public static readonly Color GroupBg = Color.FromArgb(230, 241, 255);
    public static readonly Color Border = Color.FromArgb(208, 222, 240);
    public static readonly Color BorderLight = Color.FromArgb(226, 235, 246);

    // 文字
    public static readonly Color TextMain = Color.FromArgb(24, 42, 68);
    public static readonly Color TextMute = Color.FromArgb(96, 118, 148);
    public static readonly Color TextOnPrimary = Color.White;
    public static readonly Color TextHeader = Color.FromArgb(13, 71, 140);

    // 开关
    public static readonly Color ToggleOn = Primary;
    public static readonly Color ToggleOnDeep = PrimaryDark;
    public static readonly Color ToggleOff = Color.FromArgb(180, 196, 214);
    public static readonly Color ToggleOffDeep = Color.FromArgb(148, 168, 192);
    public static readonly Color ToggleKnob = Color.White;

    public static readonly Color HeaderBarTop = Color.FromArgb(30, 120, 220);
    public static readonly Color HeaderBarBottom = Color.FromArgb(11, 78, 168);
}
