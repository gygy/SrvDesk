using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace SrvDesk;

[DataContract]
internal sealed class OptProfileFile
{
    [DataMember] public int Version { get; set; } = 3;
    [DataMember] public string? Name { get; set; }
    [DataMember] public string? CreatedAt { get; set; }
    [DataMember] public List<SettingEntry>? Settings { get; set; }
    /// <summary>配置脚本面板中用户改过的开启/关闭脚本（key = SettingScriptStore.MakeKey）。</summary>
    [DataMember] public Dictionary<string, string>? ScriptOverrides { get; set; }
    /// <summary>自定义配置方案（含脚本正文）。</summary>
    [DataMember] public List<CustomPackExport>? CustomPacks { get; set; }
    [DataMember] public string? CustomPacksLastId { get; set; }
    /// <summary>int / string 开关（分组、排序、盘符、任务栏搜索、Autologon 用户名等）。不含密码。</summary>
    [DataMember] public List<TypedSettingEntry>? Extra { get; set; }
    /// <summary>本机全部服务启动类型快照（跨机一键恢复用）。</summary>
    [DataMember] public List<ServiceSnapshotEntry>? Services { get; set; }
    /// <summary>服务器用途画像（角色位掩码）。</summary>
    [DataMember] public int? ServerRoles { get; set; }
    /// <summary>优化等级 0–3。</summary>
    [DataMember] public int? OptimizationLevel { get; set; }
}

[DataContract]
internal sealed class SettingEntry
{
    [DataMember] public string? Key { get; set; }
    [DataMember] public bool Value { get; set; }
}

[DataContract]
internal sealed class CustomPackExport
{
    [DataMember] public string Id { get; set; } = "";
    [DataMember] public string Name { get; set; } = "";
    [DataMember] public List<CustomPackItemExport>? Items { get; set; }
}

[DataContract]
internal sealed class CustomPackItemExport
{
    [DataMember] public string Id { get; set; } = "";
    [DataMember] public string Name { get; set; } = "";
    [DataMember] public string FileName { get; set; } = "";
    [DataMember] public string Kind { get; set; } = "reg";
    [DataMember] public bool Enabled { get; set; } = true;
    [DataMember] public string Content { get; set; } = "";
}

/// <summary>bool 以外的状态（int / string）。旧配置没有本段时保持类型默认值。</summary>
[DataContract]
internal sealed class TypedSettingEntry
{
    [DataMember] public string? Key { get; set; }
    [DataMember] public string? Kind { get; set; }
    [DataMember] public string? Text { get; set; }
}

/// <summary>导入结果：开关状态 + 可选的脚本覆盖与自定义方案 + 服务快照。</summary>
internal sealed class OptProfileBundle
{
    public Optimizer.State State { get; set; } = new();
    public bool HasSettings { get; set; }
    public Dictionary<string, string>? ScriptOverrides { get; set; }
    public bool HasScriptOverrides { get; set; }
    public List<CustomPackExport>? CustomPacks { get; set; }
    public string? CustomPacksLastId { get; set; }
    public bool HasCustomPacks { get; set; }
    public List<ServiceSnapshotEntry>? Services { get; set; }
    public bool HasServices { get; set; }
    public int? ServerRoles { get; set; }
    public int? OptimizationLevel { get; set; }
    public bool HasServerProfile { get; set; }

    public string SummarizeContents()
    {
        var parts = new List<string>();
        if (HasSettings) parts.Add(AppLang.L("优化开关", "Optimization toggles"));
        if (HasServices)
            parts.Add(AppLang.Lf("服务启动类型（{0} 项）", "Service start types ({0})", Services!.Count));
        if (HasServerProfile) parts.Add(AppLang.L("服务器用途/优化等级", "Server profile / level"));
        if (HasScriptOverrides) parts.Add(AppLang.L("脚本覆盖", "Script overrides"));
        if (HasCustomPacks) parts.Add(AppLang.L("自定义方案", "Custom packs"));
        return parts.Count == 0
            ? AppLang.L("（空）", "(empty)")
            : string.Join(AppLang.L("、", ", "), parts);
    }
}

internal static class ProfileStore
{
    public static void Save(string path, Optimizer.State state, string? name = null)
    {
        var map = StateMapper.ToMap(state);
        var sp = ServerProfile.Load();
        var profile = new OptProfileFile
        {
            Version = 3,
            Name = name ?? Path.GetFileNameWithoutExtension(path),
            CreatedAt = DateTime.Now.ToString("o"),
            Settings = map.Select(kv => new SettingEntry { Key = kv.Key, Value = kv.Value }).ToList(),
            ScriptOverrides = SettingScriptStore.ExportAll(),
            CustomPacks = CustomPackStore.ExportAll(out var lastId),
            CustomPacksLastId = lastId,
            Extra = StateMapper.ToExtra(state),
            Services = ServiceSnapshotStore.CaptureEntriesForExport(),
            ServerRoles = sp.Roles,
            OptimizationLevel = sp.OptimizationLevel,
        };
        File.WriteAllText(path, Serialize(profile), Encoding.UTF8);
    }

    public static Optimizer.State Load(string path) => LoadBundle(path).State;

    public static OptProfileBundle LoadBundle(string path)
    {
        var json = File.ReadAllText(path, Encoding.UTF8);
        var profile = Deserialize(json) ?? throw new InvalidOperationException("配置文件格式无效。");

        var hasSettings = profile.Settings is { Count: > 0 };
        var hasScripts = profile.ScriptOverrides is { Count: > 0 };
        var hasPacks = profile.CustomPacks is { Count: > 0 };
        var hasExtra = profile.Extra is { Count: > 0 };
        var hasServices = profile.Services is { Count: > 0 };
        var hasServer = profile.ServerRoles.HasValue || profile.OptimizationLevel.HasValue;
        if (!hasSettings && !hasScripts && !hasPacks && !hasExtra && !hasServices && !hasServer)
            throw new InvalidOperationException("配置文件中没有可导入的内容。");

        var state = hasSettings
            ? StateMapper.FromMap(profile.Settings!
                .Where(e => !string.IsNullOrEmpty(e.Key))
                .ToDictionary(e => e.Key!, e => e.Value, StringComparer.Ordinal))
            : Optimizer.Read();
        StateMapper.ApplyExtra(state, profile.Extra);

        var hasUacLevel = profile.Extra?.Any(e =>
            string.Equals(e.Key, "UacNotifyLevel", StringComparison.Ordinal)) == true;
        if (hasUacLevel)
            state.DisableUac = state.UacNotifyLevel == 2;
        else
            state.UacNotifyLevel = state.DisableUac ? 2 : 1;

        return new OptProfileBundle
        {
            State = state,
            HasSettings = hasSettings,
            ScriptOverrides = profile.ScriptOverrides,
            HasScriptOverrides = profile.ScriptOverrides is not null,
            CustomPacks = profile.CustomPacks,
            CustomPacksLastId = profile.CustomPacksLastId,
            HasCustomPacks = profile.CustomPacks is not null,
            Services = profile.Services,
            HasServices = hasServices,
            ServerRoles = profile.ServerRoles,
            OptimizationLevel = profile.OptimizationLevel,
            HasServerProfile = hasServer,
        };
    }

    /// <summary>把捆绑包中的脚本覆盖与自定义方案写入本机 AppData。</summary>
    public static void ApplyLocalData(OptProfileBundle bundle)
    {
        if (bundle.HasScriptOverrides)
            SettingScriptStore.ReplaceAll(bundle.ScriptOverrides ?? new Dictionary<string, string>());

        if (bundle.HasCustomPacks)
            CustomPackStore.ReplaceAll(bundle.CustomPacks ?? [], bundle.CustomPacksLastId);
    }

    /// <summary>写入服务器用途与优化等级（若配置中有）。</summary>
    public static void ApplyServerProfile(OptProfileBundle bundle)
    {
        if (!bundle.HasServerProfile) return;
        var data = ServerProfile.Load();
        if (bundle.ServerRoles.HasValue)
            data.Roles = bundle.ServerRoles.Value;
        if (bundle.OptimizationLevel.HasValue)
            data.OptimizationLevel = Math.Max(0, Math.Min(3, bundle.OptimizationLevel.Value));
        data.ProfileConfigured = true;
        ServerProfile.Save(data);
    }

    public static string DefaultProfileDir()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SrvDesk", "profiles");
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
