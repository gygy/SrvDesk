namespace WinOpt;

/// <summary>
/// 品牌色板：logo 与界面共用，数值须与 IconGenerator 保持一致。
/// </summary>
internal static class BrandPalette
{
    // Windows Fluent 强调色（与系统「设置」「Microsoft Store」同类蓝）
    public static readonly Color Primary = Color.FromArgb(0, 120, 212);
    public static readonly Color PrimaryBright = Color.FromArgb(43, 136, 216);
    public static readonly Color PrimaryDark = Color.FromArgb(0, 90, 158);
    public static readonly Color PrimaryDeep = Color.FromArgb(0, 69, 120);

    // Logo 渐变（顶栏与之相同）
    public static readonly Color LogoTop = Color.FromArgb(52, 146, 220);
    public static readonly Color LogoMid = Primary;
    public static readonly Color LogoBottom = PrimaryDark;

    // 界面衍生色（由主色浅化，保证与 logo 同色相）
    public static readonly Color PrimarySoft = Color.FromArgb(96, 164, 228);
    public static readonly Color PrimaryLight = Color.FromArgb(222, 236, 249);
    public static readonly Color PrimaryPale = Color.FromArgb(243, 248, 253);

    public static readonly Color Surface = Color.FromArgb(250, 251, 252);
    public static readonly Color SurfaceCard = Color.White;
    public static readonly Color NavBg = Color.FromArgb(245, 248, 252);
    public static readonly Color NavHover = Color.FromArgb(229, 240, 250);
    public static readonly Color RowAlt = Color.FromArgb(240, 247, 253);
    public static readonly Color GroupBg = Color.FromArgb(232, 242, 251);
    public static readonly Color Border = Color.FromArgb(199, 218, 235);
    public static readonly Color BorderLight = Color.FromArgb(220, 232, 244);

    public static readonly Color TextMain = Color.FromArgb(32, 47, 62);
    public static readonly Color TextMute = Color.FromArgb(96, 112, 128);
    public static readonly Color TextOnPrimary = Color.White;
    public static readonly Color TextOnPrimarySoft = Color.FromArgb(204, 228, 248);
    public static readonly Color TextHeader = Color.FromArgb(0, 69, 120);

    public static readonly Color ToggleOn = Primary;
    public static readonly Color ToggleOff = Color.FromArgb(186, 199, 212);
    public static readonly Color ToggleKnob = Color.White;

    public static readonly Color HeaderTop = LogoTop;
    public static readonly Color HeaderBottom = LogoBottom;
}
