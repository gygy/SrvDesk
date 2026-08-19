using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace WinOpt;

[DataContract]
internal sealed class OptProfileFile
{
    [DataMember] public int Version { get; set; } = 1;
    [DataMember] public string? Name { get; set; }
    [DataMember] public string? CreatedAt { get; set; }
    [DataMember] public List<SettingEntry>? Settings { get; set; }
}

[DataContract]
internal sealed class SettingEntry
{
    [DataMember] public string? Key { get; set; }
    [DataMember] public bool Value { get; set; }
}

internal static class ProfileStore
{
    public static void Save(string path, Optimizer.State state, string? name = null)
    {
        var map = StateMapper.ToMap(state);
        var profile = new OptProfileFile
        {
            Name = name ?? Path.GetFileNameWithoutExtension(path),
            CreatedAt = DateTime.Now.ToString("o"),
            Settings = map.Select(kv => new SettingEntry { Key = kv.Key, Value = kv.Value }).ToList(),
        };
        var json = Serialize(profile);
        File.WriteAllText(path, json, Encoding.UTF8);
    }

    public static Optimizer.State Load(string path)
    {
        var json = File.ReadAllText(path, Encoding.UTF8);
        var profile = Deserialize(json) ?? throw new InvalidOperationException("配置文件格式无效。");
        if (profile.Settings is null || profile.Settings.Count == 0)
            throw new InvalidOperationException("配置文件中没有设置项。");
        var map = profile.Settings
            .Where(e => !string.IsNullOrEmpty(e.Key))
            .ToDictionary(e => e.Key!, e => e.Value, StringComparer.Ordinal);
        return StateMapper.FromMap(map);
    }

    public static string DefaultProfileDir()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WinOpt", "profiles");
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static string Serialize(OptProfileFile profile)
    {
        using var ms = new MemoryStream();
        new DataContractJsonSerializer(typeof(OptProfileFile)).WriteObject(ms, profile);
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private static OptProfileFile? Deserialize(string json)
    {
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
        return new DataContractJsonSerializer(typeof(OptProfileFile)).ReadObject(ms) as OptProfileFile;
    }
}
