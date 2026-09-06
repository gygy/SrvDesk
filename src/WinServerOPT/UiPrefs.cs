using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace WinOpt;

/// <summary>配置脚本面板停靠位置。</summary>
internal enum ConfigScriptDock
{
    Right = 0,
    Bottom = 1,
}

/// <summary>界面偏好（配置脚本面板显隐/宽度/位置等），保存在 %LocalAppData%\WinOpt\ui-prefs.json</summary>
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
            "WinOpt", "ui-prefs.json");

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
    };
}
