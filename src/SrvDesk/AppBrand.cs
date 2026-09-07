using System.Reflection;

namespace SrvDesk;

/// <summary>
/// 程序品牌资源：窗口、任务栏与界面 logo 共用同一套 app.ico / app.png。
/// </summary>
internal static class AppBrand
{
    public const string ProductName = "Windows server优化助手SrvDesk";
    public const string ShortName = "SrvDesk";
    public const string Author = "gygy";
    public const string FeedbackUrl = "https://github.com/gygy/SrvDesk";

    public static string ExeFileName => $"{ShortName}.exe";

    public static string SupportDialogTitle => "支持";

    /// <summary>短版本号（如 1.0.1），不含 git 提交哈希等后缀。</summary>
    public static string VersionText
    {
        get
        {
            var asm = Assembly.GetExecutingAssembly();
            var raw = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? asm.GetName().Version?.ToString(3)
                ?? "1.0";
            var plus = raw.IndexOf('+');
            if (plus >= 0)
                raw = raw.Substring(0, plus);
            var dash = raw.IndexOf('-');
            if (dash >= 0)
                raw = raw.Substring(0, dash);
            raw = raw.Trim();
            return raw.Length > 0 ? raw : "1.0";
        }
    }

    private static Icon? _applicationIcon;

    public static Icon ApplicationIcon => _applicationIcon ??= LoadApplicationIcon();

    /// <summary>统一设置窗口标题栏与任务栏图标（与主程序 logo 一致）。</summary>
    public static void ApplyWindowIcon(Form form) => form.Icon = ApplicationIcon;

    public static Image? LoadLogoImage()
    {
        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream("SrvDesk.app.png");
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
        using var stream = asm.GetManifestResourceStream("SrvDesk.app.ico");
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
