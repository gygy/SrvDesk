using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace WinOpt;

/// <summary>界面偏好（配置脚本面板显隐/宽度等），保存在 %LocalAppData%\WinOpt\ui-prefs.json</summary>
[DataContract]
internal sealed class UiPrefsData
{
    [DataMember] public bool ShowHelpPanel { get; set; } = true;
    /// <summary>右侧配置脚本面板宽度（像素）。</summary>
    [DataMember] public int HelpPanelWidth { get; set; } = 360;
}

internal static class UiPrefs
{
    public const int DefaultHelpPanelWidth = 360;
    public const int MinHelpPanelWidth = 260;
    public const int MaxHelpPanelWidth = 720;

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
            data.HelpPanelWidth = ClampWidth(data.HelpPanelWidth <= 0 ? DefaultHelpPanelWidth : data.HelpPanelWidth);
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
            data.HelpPanelWidth = ClampWidth(data.HelpPanelWidth);
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

    public static int ClampWidth(int width) =>
        Math.Max(MinHelpPanelWidth, Math.Min(MaxHelpPanelWidth, width));

    static UiPrefsData Defaults() => new()
    {
        ShowHelpPanel = true,
        HelpPanelWidth = DefaultHelpPanelWidth,
    };
}
