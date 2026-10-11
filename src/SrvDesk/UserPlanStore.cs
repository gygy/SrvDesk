using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace SrvDesk;

[DataContract]
internal sealed class UserPlanIndex
{
    [DataMember(Name = "Plans")] public List<UserPlanSummary> Plans { get; set; } = [];
    [DataMember(Name = "LastPlanId")] public string? LastPlanId { get; set; }
}

[DataContract]
internal sealed class UserPlanSummary
{
    [DataMember(Name = "Id")] public string Id { get; set; } = "";
    [DataMember(Name = "Name")] public string Name { get; set; } = "";
    [DataMember(Name = "UpdatedUtc")] public string UpdatedUtc { get; set; } = "";
    [DataMember(Name = "ItemCount")] public int ItemCount { get; set; }

    public override string ToString() =>
        string.IsNullOrWhiteSpace(Name) ? AppLang.L("(未命名)", "(unnamed)") : Name;
}

[DataContract]
internal sealed class UserPlanDetail
{
    [DataMember(Name = "Id")] public string Id { get; set; } = "";
    [DataMember(Name = "Name")] public string Name { get; set; } = "";
    [DataMember(Name = "Items")] public List<UserPlanItem> Items { get; set; } = [];
}

/// <summary>
/// 方案项：引用 SettingCatalog 字段名（CatalogKey）+ 目标档位（ChoiceIndex）。
/// </summary>
[DataContract]
internal sealed class UserPlanItem
{
    [DataMember(Name = "Key")] public string Key { get; set; } = "";
    /// <summary>目标档位：与界面 ChoiceIndex 一致（开关项：优化档 / 非优化档）。</summary>
    [DataMember(Name = "Value")] public int Value { get; set; }
    [DataMember(Name = "OptimizedIndex")] public int OptimizedIndex { get; set; } = 1;
    [DataMember(Name = "Title")] public string Title { get; set; } = "";
    [DataMember(Name = "Module")] public string Module { get; set; } = "";
    [DataMember(Name = "DesiredLabel")] public string DesiredLabel { get; set; } = "";
    [DataMember(Name = "Enabled")] public bool Enabled { get; set; } = true;

    public int OffIndex => OptimizedIndex == 0 ? 1 : 0;
}

[DataContract]
internal sealed class UserPlanExportFile
{
    [DataMember(Name = "Version")] public int Version { get; set; } = 1;
    [DataMember(Name = "ExportedUtc")] public string ExportedUtc { get; set; } = "";
    [DataMember(Name = "LastPlanId")] public string? LastPlanId { get; set; }
    [DataMember(Name = "Plans")] public List<UserPlanDetail> Plans { get; set; } = [];
}

/// <summary>
/// 旧版轻度混淆曾把属性名改成 A/B/a…，导致换程序/测通后 index 对不上。
/// 仅用于读盘迁移到稳定字段名。
/// </summary>
[DataContract]
internal sealed class UserPlanLegacyObfDetail
{
    [DataMember(Name = "A")] public string? Id { get; set; }
    [DataMember(Name = "a")] public string? Name { get; set; }
    [DataMember(Name = "B")] public List<UserPlanLegacyObfItem>? Items { get; set; }
}

[DataContract]
internal sealed class UserPlanLegacyObfItem
{
    [DataMember(Name = "A")] public string? Key { get; set; }
    [DataMember(Name = "B")] public int Value { get; set; }
    [DataMember(Name = "C")] public string? Module { get; set; }
    [DataMember(Name = "D")] public bool Enabled { get; set; } = true;
    [DataMember(Name = "a")] public int OptimizedIndex { get; set; } = 1;
    [DataMember(Name = "b")] public string? Title { get; set; }
    [DataMember(Name = "c")] public string? DesiredLabel { get; set; }
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

            var index = LoadIndexUnlocked(reconcile: false);
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

            var index = LoadIndexUnlocked(reconcile: false);
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
            var index = LoadIndexUnlocked(reconcile: false);
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
            ReconcileIndexWithDisk(index, forceRewrite: true);
            if (!string.IsNullOrWhiteSpace(file.LastPlanId)
                && index.Plans.Any(p => string.Equals(p.Id, file.LastPlanId, StringComparison.OrdinalIgnoreCase)))
                index.LastPlanId = file.LastPlanId;
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

    private static UserPlanIndex LoadIndexUnlocked(bool reconcile = true)
    {
        EnsureRoot();
        UserPlanIndex data;
        try
        {
            if (!File.Exists(IndexPath))
                data = new UserPlanIndex();
            else
                data = ReadJson<UserPlanIndex>(IndexPath) ?? new UserPlanIndex();
        }
        catch
        {
            data = new UserPlanIndex();
        }

        data.Plans ??= [];
        if (reconcile)
            ReconcileIndexWithDisk(data, forceRewrite: false);
        return data;
    }

    /// <summary>扫描磁盘方案文件，补回空 index / 迁移旧混淆 JSON。</summary>
    private static void ReconcileIndexWithDisk(UserPlanIndex index, bool forceRewrite)
    {
        index.Plans ??= [];
        var dirty = forceRewrite;
        var known = new HashSet<string>(
            index.Plans.Select(p => p.Id),
            StringComparer.OrdinalIgnoreCase);

        foreach (var path in Directory.GetFiles(RootDir, "*.json"))
        {
            if (string.Equals(Path.GetFileName(path), "index.json", StringComparison.OrdinalIgnoreCase))
                continue;

            var plan = LoadPlanFromPath(path, migrate: true);
            if (plan is null || string.IsNullOrWhiteSpace(plan.Id))
                continue;

            var summary = index.Plans.FirstOrDefault(p =>
                string.Equals(p.Id, plan.Id, StringComparison.OrdinalIgnoreCase));
            if (summary is null)
            {
                index.Plans.Add(new UserPlanSummary
                {
                    Id = plan.Id,
                    Name = string.IsNullOrWhiteSpace(plan.Name)
                        ? AppLang.L("我的方案", "My plan")
                        : plan.Name,
                    UpdatedUtc = DateTime.UtcNow.ToString("o"),
                    ItemCount = plan.Items?.Count ?? 0,
                });
                dirty = true;
            }
            else
            {
                var count = plan.Items?.Count ?? 0;
                var name = string.IsNullOrWhiteSpace(plan.Name) ? summary.Name : plan.Name;
                if (summary.ItemCount != count || !string.Equals(summary.Name, name, StringComparison.Ordinal))
                {
                    summary.ItemCount = count;
                    summary.Name = name;
                    dirty = true;
                }
            }

            known.Add(plan.Id);
        }

        // 去掉指向已删文件的索引项
        var removed = index.Plans.RemoveAll(p =>
            string.IsNullOrWhiteSpace(p.Id) || !File.Exists(PlanPath(p.Id)));
        if (removed > 0) dirty = true;

        if (string.IsNullOrWhiteSpace(index.LastPlanId)
            || !index.Plans.Any(p => string.Equals(p.Id, index.LastPlanId, StringComparison.OrdinalIgnoreCase)))
        {
            index.LastPlanId = index.Plans
                .OrderByDescending(p => p.ItemCount)
                .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault()?.Id;
            dirty = true;
        }

        if (dirty)
            WriteJson(IndexPath, index);
    }

    private static UserPlanDetail? LoadPlanUnlocked(string planId)
    {
        var path = PlanPath(planId);
        if (!File.Exists(path))
        {
            // 兼容：文件名即 Id
            return null;
        }
        return LoadPlanFromPath(path, migrate: true);
    }

    private static UserPlanDetail? LoadPlanFromPath(string path, bool migrate)
    {
        try
        {
            var plan = ReadJson<UserPlanDetail>(path);
            if (IsUsablePlan(plan))
            {
                plan!.Items ??= [];
                if (string.IsNullOrWhiteSpace(plan.Id))
                    plan.Id = Path.GetFileNameWithoutExtension(path);
                return plan;
            }

            // 旧混淆字段名 A/B/a…
            var legacy = ReadJson<UserPlanLegacyObfDetail>(path);
            if (legacy?.Items is { Count: > 0 } || !string.IsNullOrWhiteSpace(legacy?.Id))
            {
                var migrated = new UserPlanDetail
                {
                    Id = string.IsNullOrWhiteSpace(legacy!.Id)
                        ? Path.GetFileNameWithoutExtension(path)
                        : legacy.Id!.Trim(),
                    Name = string.IsNullOrWhiteSpace(legacy.Name)
                        ? AppLang.L("我的方案", "My plan")
                        : legacy.Name!.Trim(),
                    Items = (legacy.Items ?? [])
                        .Where(x => !string.IsNullOrWhiteSpace(x.Key))
                        .Select(x => new UserPlanItem
                        {
                            Key = x.Key!.Trim(),
                            Value = x.Value,
                            OptimizedIndex = x.OptimizedIndex,
                            Title = x.Title ?? x.Key!,
                            Module = x.Module ?? "",
                            DesiredLabel = x.DesiredLabel ?? "",
                            Enabled = x.Enabled,
                        })
                        .ToList(),
                };

                if (migrate && migrated.Items.Count > 0)
                {
                    WriteJson(PlanPath(migrated.Id), migrated);
                    // 若旧文件名与 Id 不一致，删掉旧路径残留
                    if (!string.Equals(path, PlanPath(migrated.Id), StringComparison.OrdinalIgnoreCase))
                    {
                        try { File.Delete(path); }
                        catch { /* ignore */ }
                    }
                }

                return migrated;
            }

            if (plan is not null)
            {
                plan.Items ??= [];
                if (string.IsNullOrWhiteSpace(plan.Id))
                    plan.Id = Path.GetFileNameWithoutExtension(path);
                return plan;
            }
        }
        catch
        {
            /* fall through */
        }

        return null;
    }

    private static bool IsUsablePlan(UserPlanDetail? plan)
    {
        if (plan is null) return false;
        // 稳定字段：有 Id 或已有 Items/Name，且不是「全空的反序列化失败体」
        if (plan.Items is { Count: > 0 }) return true;
        if (!string.IsNullOrWhiteSpace(plan.Id) && !string.IsNullOrWhiteSpace(plan.Name))
            return true;
        return false;
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
