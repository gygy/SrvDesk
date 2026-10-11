using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace SrvDesk;

[DataContract]
internal sealed class UserPlanIndex
{
    [DataMember] public List<UserPlanSummary> Plans { get; set; } = [];
    [DataMember] public string? LastPlanId { get; set; }
}

[DataContract]
internal sealed class UserPlanSummary
{
    [DataMember] public string Id { get; set; } = "";
    [DataMember] public string Name { get; set; } = "";
    [DataMember] public string UpdatedUtc { get; set; } = "";
    [DataMember] public int ItemCount { get; set; }

    public override string ToString() =>
        string.IsNullOrWhiteSpace(Name) ? AppLang.L("(未命名)", "(unnamed)") : Name;
}

[DataContract]
internal sealed class UserPlanDetail
{
    [DataMember] public string Id { get; set; } = "";
    [DataMember] public string Name { get; set; } = "";
    [DataMember] public List<UserPlanItem> Items { get; set; } = [];
}

/// <summary>
/// 方案项：引用 SettingCatalog 字段名（CatalogKey）+ 目标档位（ChoiceIndex）。
/// </summary>
[DataContract]
internal sealed class UserPlanItem
{
    [DataMember] public string Key { get; set; } = "";
    /// <summary>目标档位：与界面 ChoiceIndex 一致（开关项：优化档 / 非优化档）。</summary>
    [DataMember] public int Value { get; set; }
    [DataMember] public int OptimizedIndex { get; set; } = 1;
    [DataMember] public string Title { get; set; } = "";
    [DataMember] public string Module { get; set; } = "";
    [DataMember] public string DesiredLabel { get; set; } = "";
    [DataMember] public bool Enabled { get; set; } = true;

    public int OffIndex => OptimizedIndex == 0 ? 1 : 0;
}

[DataContract]
internal sealed class UserPlanExportFile
{
    [DataMember] public int Version { get; set; } = 1;
    [DataMember] public string ExportedUtc { get; set; } = "";
    [DataMember] public string? LastPlanId { get; set; }
    [DataMember] public List<UserPlanDetail> Plans { get; set; } = [];
}

/// <summary>
/// 「我的方案」：收藏跨模块优化项，本地 JSON。
/// 目录：%LocalAppData%\SrvDesk\user-plans\
/// </summary>
internal static class UserPlanStore
{
    private static readonly object Gate = new();
    private static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    public static string RootDir =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SrvDesk", "user-plans");

    private static string IndexPath => Path.Combine(RootDir, "index.json");

    public static string PlanPath(string planId) => Path.Combine(RootDir, planId + ".json");

    public static bool HasAnyItems()
    {
        lock (Gate)
        {
            var index = LoadIndexUnlocked();
            foreach (var s in index.Plans)
            {
                if (s.ItemCount > 0) return true;
                var p = LoadPlanUnlocked(s.Id);
                if (p?.Items is { Count: > 0 }) return true;
            }
            return false;
        }
    }

    public static UserPlanIndex LoadIndex()
    {
        lock (Gate) return LoadIndexUnlocked();
    }

    public static void SaveIndex(UserPlanIndex index)
    {
        lock (Gate)
        {
            EnsureRoot();
            index.Plans ??= [];
            WriteJson(IndexPath, index);
        }
    }

    public static UserPlanDetail? LoadPlan(string planId)
    {
        if (string.IsNullOrWhiteSpace(planId)) return null;
        lock (Gate) return LoadPlanUnlocked(planId);
    }

    public static void SavePlan(UserPlanDetail plan)
    {
        lock (Gate)
        {
            EnsureRoot();
            plan.Items ??= [];
            if (string.IsNullOrWhiteSpace(plan.Id))
                plan.Id = Guid.NewGuid().ToString("N");
            WriteJson(PlanPath(plan.Id), plan);

            var index = LoadIndexUnlocked();
            var summary = index.Plans.FirstOrDefault(p =>
                string.Equals(p.Id, plan.Id, StringComparison.OrdinalIgnoreCase));
            if (summary is null)
            {
                summary = new UserPlanSummary { Id = plan.Id };
                index.Plans.Add(summary);
            }
            summary.Name = string.IsNullOrWhiteSpace(plan.Name)
                ? AppLang.L("未命名方案", "Unnamed plan")
                : plan.Name.Trim();
            summary.UpdatedUtc = DateTime.UtcNow.ToString("o");
            summary.ItemCount = plan.Items.Count;
            index.LastPlanId = plan.Id;
            WriteJson(IndexPath, index);
        }
    }

    public static UserPlanDetail CreatePlan(string name)
    {
        var plan = new UserPlanDetail
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = string.IsNullOrWhiteSpace(name)
                ? AppLang.L("我的方案", "My plan")
                : name.Trim(),
            Items = [],
        };
        SavePlan(plan);
        return plan;
    }

    public static UserPlanDetail GetOrCreateDefaultPlan()
    {
        lock (Gate)
        {
            var index = LoadIndexUnlocked();
            if (!string.IsNullOrWhiteSpace(index.LastPlanId))
            {
                var last = LoadPlanUnlocked(index.LastPlanId!);
                if (last is not null) return last;
            }
            if (index.Plans.Count > 0)
            {
                var first = LoadPlanUnlocked(index.Plans[0].Id);
                if (first is not null) return first;
            }
        }
        return CreatePlan(AppLang.L("我的方案", "My plan"));
    }

    public static void RenamePlan(string planId, string name)
    {
        var plan = LoadPlan(planId) ?? throw new InvalidOperationException(AppLang.L("方案不存在。", "Plan not found."));
        plan.Name = string.IsNullOrWhiteSpace(name)
            ? AppLang.L("未命名方案", "Unnamed plan")
            : name.Trim();
        SavePlan(plan);
    }

    public static void DeletePlan(string planId)
    {
        lock (Gate)
        {
            var path = PlanPath(planId);
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch { /* ignore */ }

            var index = LoadIndexUnlocked();
            index.Plans.RemoveAll(p => string.Equals(p.Id, planId, StringComparison.OrdinalIgnoreCase));
            if (string.Equals(index.LastPlanId, planId, StringComparison.OrdinalIgnoreCase))
                index.LastPlanId = index.Plans.FirstOrDefault()?.Id;
            WriteJson(IndexPath, index);
        }
    }

    public static void SetLastPlanId(string planId)
    {
        lock (Gate)
        {
            var index = LoadIndexUnlocked();
            if (index.Plans.Any(p => string.Equals(p.Id, planId, StringComparison.OrdinalIgnoreCase)))
            {
                index.LastPlanId = planId;
                WriteJson(IndexPath, index);
            }
        }
    }

    /// <summary>加入或更新项；返回 true=新加入，false=已存在并更新。</summary>
    public static bool UpsertItem(UserPlanDetail plan, UserPlanItem item)
    {
        plan.Items ??= [];
        var existing = plan.Items.FirstOrDefault(x =>
            string.Equals(x.Key, item.Key, StringComparison.Ordinal));
        if (existing is null)
        {
            plan.Items.Add(item);
            SavePlan(plan);
            return true;
        }

        existing.Value = item.Value;
        existing.OptimizedIndex = item.OptimizedIndex;
        existing.Title = item.Title;
        existing.Module = item.Module;
        existing.DesiredLabel = item.DesiredLabel;
        existing.Enabled = true;
        SavePlan(plan);
        return false;
    }

    public static bool RemoveItem(UserPlanDetail plan, string key)
    {
        plan.Items ??= [];
        var n = plan.Items.RemoveAll(x => string.Equals(x.Key, key, StringComparison.Ordinal));
        if (n > 0) SavePlan(plan);
        return n > 0;
    }

    public static bool ContainsKey(string? planId, string catalogKey)
    {
        if (string.IsNullOrWhiteSpace(planId) || string.IsNullOrWhiteSpace(catalogKey))
            return false;
        var plan = LoadPlan(planId!);
        return plan?.Items.Any(x => string.Equals(x.Key, catalogKey, StringComparison.Ordinal)) == true;
    }

    public static bool IsInActivePlan(string catalogKey)
    {
        var index = LoadIndex();
        var id = index.LastPlanId ?? index.Plans.FirstOrDefault()?.Id;
        return !string.IsNullOrEmpty(id) && ContainsKey(id!, catalogKey);
    }

    public static void MoveItem(UserPlanDetail plan, string key, int delta)
    {
        plan.Items ??= [];
        var i = plan.Items.FindIndex(x => string.Equals(x.Key, key, StringComparison.Ordinal));
        if (i < 0) return;
        var j = i + delta;
        if (j < 0 || j >= plan.Items.Count) return;
        (plan.Items[i], plan.Items[j]) = (plan.Items[j], plan.Items[i]);
        SavePlan(plan);
    }

    public static void SetItemEnabled(UserPlanDetail plan, string key, bool enabled)
    {
        var item = plan.Items?.FirstOrDefault(x => string.Equals(x.Key, key, StringComparison.Ordinal));
        if (item is null || item.Enabled == enabled) return;
        item.Enabled = enabled;
        SavePlan(plan);
    }

    public static void ExportToFile(string path, string? onlyPlanId = null)
    {
        lock (Gate)
        {
            var index = LoadIndexUnlocked();
            var file = new UserPlanExportFile
            {
                Version = 1,
                ExportedUtc = DateTime.UtcNow.ToString("o"),
                LastPlanId = index.LastPlanId,
                Plans = [],
            };
            foreach (var s in index.Plans)
            {
                if (onlyPlanId is not null
                    && !string.Equals(s.Id, onlyPlanId, StringComparison.OrdinalIgnoreCase))
                    continue;
                var p = LoadPlanUnlocked(s.Id);
                if (p is not null) file.Plans.Add(p);
            }
            WriteJson(path, file);
        }
    }

    public static int ImportFromFile(string path, bool merge)
    {
        var raw = File.ReadAllText(path, Encoding.UTF8);
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(raw));
        var file = new DataContractJsonSerializer(typeof(UserPlanExportFile)).ReadObject(ms) as UserPlanExportFile;
        if (file?.Plans is null || file.Plans.Count == 0)
            throw new InvalidOperationException(AppLang.L("文件中没有可用方案。", "No plans in file."));

        lock (Gate)
        {
            EnsureRoot();
            if (!merge)
            {
                foreach (var f in Directory.Exists(RootDir) ? Directory.GetFiles(RootDir, "*.json") : [])
                {
                    try { File.Delete(f); }
                    catch { /* ignore */ }
                }
                WriteJson(IndexPath, new UserPlanIndex());
            }

            var imported = 0;
            foreach (var src in file.Plans)
            {
                if (src.Items is null) src.Items = [];
                if (string.IsNullOrWhiteSpace(src.Id) || !merge)
                    src.Id = Guid.NewGuid().ToString("N");
                else if (LoadPlanUnlocked(src.Id) is not null)
                    src.Id = Guid.NewGuid().ToString("N");
                if (string.IsNullOrWhiteSpace(src.Name))
                    src.Name = AppLang.L("导入的方案", "Imported plan");
                WriteJson(PlanPath(src.Id), src);
                imported++;
            }

            var index = new UserPlanIndex { Plans = [], LastPlanId = null };
            foreach (var f in Directory.GetFiles(RootDir, "*.json"))
            {
                if (string.Equals(Path.GetFileName(f), "index.json", StringComparison.OrdinalIgnoreCase))
                    continue;
                try
                {
                    var p = ReadJson<UserPlanDetail>(f);
                    if (p is null || string.IsNullOrWhiteSpace(p.Id)) continue;
                    p.Items ??= [];
                    index.Plans.Add(new UserPlanSummary
                    {
                        Id = p.Id,
                        Name = p.Name,
                        UpdatedUtc = DateTime.UtcNow.ToString("o"),
                        ItemCount = p.Items.Count,
                    });
                }
                catch { /* ignore */ }
            }

            if (!string.IsNullOrWhiteSpace(file.LastPlanId)
                && index.Plans.Any(p => string.Equals(p.Id, file.LastPlanId, StringComparison.OrdinalIgnoreCase)))
                index.LastPlanId = file.LastPlanId;
            else
                index.LastPlanId = index.Plans.FirstOrDefault()?.Id;
            WriteJson(IndexPath, index);
            return imported;
        }
    }

    public static int CountDrift(UserPlanDetail plan, Optimizer.State system)
    {
        var n = 0;
        foreach (var item in plan.Items ?? [])
        {
            if (!item.Enabled) continue;
            if (!UserPlanState.Matches(system, item))
                n++;
        }
        return n;
    }

    private static UserPlanIndex LoadIndexUnlocked()
    {
        EnsureRoot();
        try
        {
            if (!File.Exists(IndexPath))
                return new UserPlanIndex();
            var data = ReadJson<UserPlanIndex>(IndexPath) ?? new UserPlanIndex();
            data.Plans ??= [];
            return data;
        }
        catch
        {
            return new UserPlanIndex();
        }
    }

    private static UserPlanDetail? LoadPlanUnlocked(string planId)
    {
        var path = PlanPath(planId);
        if (!File.Exists(path)) return null;
        try
        {
            var plan = ReadJson<UserPlanDetail>(path);
            if (plan is null) return null;
            plan.Items ??= [];
            return plan;
        }
        catch
        {
            return null;
        }
    }

    private static void EnsureRoot() => Directory.CreateDirectory(RootDir);

    private static T? ReadJson<T>(string path) where T : class
    {
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(File.ReadAllText(path, Encoding.UTF8)));
        return new DataContractJsonSerializer(typeof(T)).ReadObject(ms) as T;
    }

    private static void WriteJson<T>(string path, T data)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var ms = new MemoryStream();
        new DataContractJsonSerializer(typeof(T)).WriteObject(ms, data);
        File.WriteAllText(path, Encoding.UTF8.GetString(ms.ToArray()), Utf8);
    }
}

/// <summary>方案项 ↔ Optimizer.State 读写与克隆。</summary>
internal static class UserPlanState
{
    public static Optimizer.State Clone(Optimizer.State src)
    {
        var dst = new Optimizer.State();
        foreach (var f in typeof(Optimizer.State).GetFields(BindingFlags.Instance | BindingFlags.Public))
            f.SetValue(dst, f.GetValue(src));
        return dst;
    }

    public static bool Matches(Optimizer.State system, UserPlanItem item)
    {
        var cur = ReadChoiceIndex(system, item.Key, item.OptimizedIndex);
        return cur == item.Value;
    }

    public static int ReadChoiceIndex(Optimizer.State s, string catalogKey, int optimizedIndex)
    {
        switch (catalogKey)
        {
            case "DisableUac":
                return s.UacNotifyLevel is >= 0 and <= 2 ? s.UacNotifyLevel : (s.DisableUac ? 2 : 1);
            case "HighPerfPower":
                return s.UltimatePerfPower ? 2 : (s.HighPerfPower ? 1 : 0);
            case "HideTaskbarSearch":
                return s.TaskbarSearchMode is >= 0 and <= 2 ? s.TaskbarSearchMode : 1;
            case "ShowDriveLettersMode":
                return Clamp(s.ShowDriveLettersMode, 0, 2);
            case "FolderGroupByMode":
                return Clamp(s.FolderGroupByMode, 0, 4);
            case "FolderSortByMode":
                return Clamp(s.FolderSortByMode, 0, 5);
            default:
            {
                var map = StateMapper.ToMap(s);
                if (map.TryGetValue(catalogKey, out var on))
                    return on ? optimizedIndex : (optimizedIndex == 0 ? 1 : 0);
                return optimizedIndex == 0 ? 1 : 0;
            }
        }
    }

    public static void WriteChoiceIndex(Optimizer.State s, string catalogKey, int index, int optimizedIndex)
    {
        switch (catalogKey)
        {
            case "DisableUac":
                s.UacNotifyLevel = Clamp(index, 0, 2);
                s.DisableUac = s.UacNotifyLevel == 2;
                break;
            case "HighPerfPower":
                s.HighPerfPower = index == 1;
                s.UltimatePerfPower = index == 2;
                break;
            case "HideTaskbarSearch":
                s.TaskbarSearchMode = Clamp(index, 0, 2);
                s.TaskbarSearchBox = s.TaskbarSearchMode == 2;
                break;
            case "ShowDriveLettersMode":
                s.ShowDriveLettersMode = Clamp(index, 0, 2);
                break;
            case "FolderGroupByMode":
                s.FolderGroupByMode = Clamp(index, 0, 4);
                break;
            case "FolderSortByMode":
                s.FolderSortByMode = Clamp(index, 0, 5);
                break;
            default:
            {
                var on = index == optimizedIndex;
                var fi = typeof(Optimizer.State).GetField(catalogKey, BindingFlags.Instance | BindingFlags.Public);
                if (fi?.FieldType == typeof(bool))
                    fi.SetValue(s, on);
                break;
            }
        }
    }

    public static void ApplyItems(Optimizer.State target, IEnumerable<UserPlanItem> items, bool restore)
    {
        foreach (var item in items)
        {
            if (!item.Enabled) continue;
            if (string.IsNullOrWhiteSpace(item.Key)) continue;
            var value = restore ? item.OffIndex : item.Value;
            WriteChoiceIndex(target, item.Key, value, item.OptimizedIndex);
        }
    }

    private static int Clamp(int v, int min, int max) =>
        v < min ? min : (v > max ? max : v);
}
