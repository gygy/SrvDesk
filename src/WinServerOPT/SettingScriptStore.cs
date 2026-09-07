using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace SrvDesk;

/// <summary>
/// 用户在配置脚本编辑器中的修改持久化。
/// 存于 %LocalAppData%\SrvDesk\script-overrides.json，下次打开同一项仍显示改过的内容。
/// </summary>
internal static class SettingScriptStore
{
    [DataContract]
    private sealed class StoreFile
    {
        [DataMember] public Dictionary<string, string> Scripts { get; set; } = new();
    }

    private static readonly object Gate = new();
    private static StoreFile? _cache;

    private static string FilePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SrvDesk", "script-overrides.json");

    public static string MakeKey(string itemTitle, bool enable) =>
        (itemTitle ?? "").Trim() + "\u001f" + (enable ? "on" : "off");

    public static bool TryGet(string itemTitle, bool enable, out string script)
    {
        script = "";
        var key = MakeKey(itemTitle, enable);
        lock (Gate)
        {
            var data = Load();
            if (data.Scripts.TryGetValue(key, out var s) && !string.IsNullOrEmpty(s))
            {
                script = s;
                return true;
            }
        }
        return false;
    }

    public static void Set(string itemTitle, bool enable, string script)
    {
        if (string.IsNullOrWhiteSpace(itemTitle)) return;
        var key = MakeKey(itemTitle, enable);
        var text = Normalize(script);
        lock (Gate)
        {
            var data = Load();
            if (data.Scripts.TryGetValue(key, out var old) && old == text)
                return;
            data.Scripts[key] = text;
            Save(data);
        }
    }

    public static void Remove(string itemTitle, bool enable)
    {
        var key = MakeKey(itemTitle, enable);
        lock (Gate)
        {
            var data = Load();
            if (!data.Scripts.Remove(key)) return;
            Save(data);
        }
    }

    public static void RemoveBoth(string itemTitle)
    {
        Remove(itemTitle, true);
        Remove(itemTitle, false);
    }

    public static bool HasOverride(string itemTitle, bool enable) =>
        TryGet(itemTitle, enable, out _);

    /// <summary>导出全部脚本覆盖（供配置备份）。</summary>
    public static Dictionary<string, string> ExportAll()
    {
        lock (Gate)
        {
            var data = Load();
            return new Dictionary<string, string>(data.Scripts, StringComparer.Ordinal);
        }
    }

    /// <summary>用导入内容整体替换本机脚本覆盖。</summary>
    public static void ReplaceAll(Dictionary<string, string> scripts)
    {
        lock (Gate)
        {
            var data = new StoreFile
            {
                Scripts = new Dictionary<string, string>(
                    scripts ?? new Dictionary<string, string>(),
                    StringComparer.Ordinal),
            };
            // 规范化换行
            var keys = data.Scripts.Keys.ToList();
            foreach (var key in keys)
                data.Scripts[key] = Normalize(data.Scripts[key]);
            Save(data);
        }
    }

    private static string Normalize(string script) =>
        (script ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Replace("\n", "\r\n").TrimEnd() + "\r\n";

    private static StoreFile Load()
    {
        if (_cache is not null) return _cache;
        try
        {
            if (!File.Exists(FilePath))
                return _cache = new StoreFile();
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(File.ReadAllText(FilePath, Encoding.UTF8)));
            _cache = new DataContractJsonSerializer(typeof(StoreFile)).ReadObject(ms) as StoreFile
                     ?? new StoreFile();
            _cache.Scripts ??= new Dictionary<string, string>();
            return _cache;
        }
        catch
        {
            return _cache = new StoreFile();
        }
    }

    private static void Save(StoreFile data)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            using var ms = new MemoryStream();
            new DataContractJsonSerializer(typeof(StoreFile)).WriteObject(ms, data);
            File.WriteAllText(FilePath, Encoding.UTF8.GetString(ms.ToArray()), Encoding.UTF8);
            _cache = data;
        }
        catch
        {
            /* ignore disk errors */
        }
    }
}
