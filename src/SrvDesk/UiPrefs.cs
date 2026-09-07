using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace SrvDesk;

/// <summary>配置脚本面板停靠位置。</summary>
internal enum ConfigScriptDock
{
    Right = 0,
    Bottom = 1,
}

/// <summary>界面与调试偏好，保存在 %LocalAppData%\SrvDesk\ui-prefs.json</summary>
[DataContract]
internal sealed class UiPrefsData
{
    [DataMember] public bool ShowHelpPanel { get; set; } = true;
    /// <summary>右侧时的宽度。</summary>
    [DataMember] public int HelpPanelWidth { get; set; } = 360;
    /// <summary>底部时的高度。</summary>
    [DataMember] public int HelpPanelHeight { get; set; } = 280;
    /// <summary>0=右侧 1=底部。</summary>
    [DataMember] public int HelpPanelDock { get; set; } = (int)ConfigScriptDock.Right;
    /// <summary>启动时默认勾选「隐藏不适用项」。</summary>
    [DataMember] public bool HideIncompatibleByDefault { get; set; } = true;
    /// <summary>写入 debug.log（命令行、分支、软跳过原因等）。默认开启。</summary>
    [DataMember] public bool EnableDebugLog { get; set; } = true;
    /// <summary>固件不支持休眠、防火墙规则组名不匹配等记为「跳过/提示」，不计入部分失败。默认开启。</summary>
    [DataMember] public bool SoftSkipUnsupported { get; set; } = true;
}

internal static class UiPrefs
{
    public const int DefaultHelpPanelWidth = 420;
    public const int MinHelpPanelWidth = 240;
    public const int MaxHelpPanelWidth = 720;
    public const int DefaultHelpPanelHeight = 280;
    public const int MinHelpPanelHeight = 160;
    public const int MaxHelpPanelHeight = 520;

    private static string FilePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SrvDesk", "ui-prefs.json");

    public static UiPrefsData Load()
    {
        try
        {
            if (!File.Exists(FilePath))
                return Defaults();
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(File.ReadAllText(FilePath, Encoding.UTF8)));
            var data = new DataContractJsonSerializer(typeof(UiPrefsData)).ReadObject(ms) as UiPrefsData
                       ?? Defaults();
            Normalize(data);
            return data;
        }
        catch
        {
            return Defaults();
        }
    }

    public static void Save(UiPrefsData data)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            Normalize(data);
            using var ms = new MemoryStream();
            new DataContractJsonSerializer(typeof(UiPrefsData)).WriteObject(ms, data);
            File.WriteAllText(FilePath, Encoding.UTF8.GetString(ms.ToArray()), Encoding.UTF8);
        }
        catch { /* ignore */ }
    }

    public static void SetShowHelpPanel(bool show)
    {
        var data = Load();
        data.ShowHelpPanel = show;
        Save(data);
    }

    public static void SetHelpPanelWidth(int width)
    {
        var data = Load();
        data.HelpPanelWidth = ClampWidth(width);
        Save(data);
    }

    public static void SetHelpPanelHeight(int height)
    {
        var data = Load();
        data.HelpPanelHeight = ClampHeight(height);
        Save(data);
    }

    public static void SetHelpPanelDock(ConfigScriptDock dock)
    {
        var data = Load();
        data.HelpPanelDock = (int)dock;
        Save(data);
    }

    public static bool EnableDebugLog => Load().EnableDebugLog;
    public static bool SoftSkipUnsupported => Load().SoftSkipUnsupported;

    public static ConfigScriptDock GetDock(UiPrefsData data) =>
        data.HelpPanelDock == (int)ConfigScriptDock.Bottom
            ? ConfigScriptDock.Bottom
            : ConfigScriptDock.Right;

    public static int ClampWidth(int width) =>
        Math.Max(MinHelpPanelWidth, Math.Min(MaxHelpPanelWidth, width));

    public static int ClampHeight(int height) =>
        Math.Max(MinHelpPanelHeight, Math.Min(MaxHelpPanelHeight, height));

    static void Normalize(UiPrefsData data)
    {
        data.HelpPanelWidth = ClampWidth(data.HelpPanelWidth <= 0 ? DefaultHelpPanelWidth : data.HelpPanelWidth);
        data.HelpPanelHeight = ClampHeight(data.HelpPanelHeight <= 0 ? DefaultHelpPanelHeight : data.HelpPanelHeight);
        if (data.HelpPanelDock != (int)ConfigScriptDock.Bottom)
            data.HelpPanelDock = (int)ConfigScriptDock.Right;
    }

    static UiPrefsData Defaults() => new()
    {
        ShowHelpPanel = true,
        HelpPanelWidth = DefaultHelpPanelWidth,
        HelpPanelHeight = DefaultHelpPanelHeight,
        HelpPanelDock = (int)ConfigScriptDock.Right,
        HideIncompatibleByDefault = true,
        EnableDebugLog = true,
        SoftSkipUnsupported = true,
    };
}
