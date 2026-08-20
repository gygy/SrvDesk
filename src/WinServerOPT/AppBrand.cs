using System.Reflection;

namespace WinOpt;

/// <summary>
/// 程序品牌资源：窗口、任务栏与界面 logo 共用同一套 app.ico / app.png。
/// </summary>
internal static class AppBrand
{
    private static Icon? _applicationIcon;

    public static Icon ApplicationIcon => _applicationIcon ??= LoadApplicationIcon();

    /// <summary>统一设置窗口标题栏与任务栏图标（与主程序 logo 一致）。</summary>
    public static void ApplyWindowIcon(Form form) => form.Icon = ApplicationIcon;

    public static Image? LoadLogoImage()
    {
        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream("WinOpt.app.png");
        if (stream is not null)
            return Image.FromStream(stream);

        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.png");
        if (File.Exists(path))
            return Image.FromFile(path);

        return ApplicationIcon.ToBitmap();
    }

    private static Icon LoadApplicationIcon()
    {
        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream("WinOpt.app.ico");
        if (stream is not null)
            return new Icon(stream);

        try
        {
            var fromExe = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            if (fromExe is not null)
                return fromExe;
        }
        catch { /* 设计时 */ }

        return SystemIcons.Application;
    }
}
