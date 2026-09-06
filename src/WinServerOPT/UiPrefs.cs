using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace WinOpt;

/// <summary>界面偏好（帮助面板显隐等），保存在 %LocalAppData%\WinOpt\ui-prefs.json</summary>
[DataContract]
internal sealed class UiPrefsData
{
    [DataMember] public bool ShowHelpPanel { get; set; }
}

internal static class UiPrefs
{
    private static string FilePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WinOpt", "ui-prefs.json");

    public static UiPrefsData Load()
    {
        try
        {
            if (!File.Exists(FilePath))
                return new UiPrefsData { ShowHelpPanel = true };
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(File.ReadAllText(FilePath, Encoding.UTF8)));
            return new DataContractJsonSerializer(typeof(UiPrefsData)).ReadObject(ms) as UiPrefsData
                   ?? new UiPrefsData { ShowHelpPanel = true };
        }
        catch
        {
            return new UiPrefsData { ShowHelpPanel = true };
        }
    }

    public static void Save(UiPrefsData data)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
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
}
