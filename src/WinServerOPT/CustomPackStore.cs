using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace SrvDesk;

internal enum CustomPackItemKind
{
    Reg,
    Cmd,
    Ps1,
}

[DataContract]
internal sealed class CustomPackIndex
{
    [DataMember] public List<CustomPackSummary> Packs { get; set; } = [];
    [DataMember] public string? LastPackId { get; set; }
}

[DataContract]
internal sealed class CustomPackSummary
{
    [DataMember] public string Id { get; set; } = "";
    [DataMember] public string Name { get; set; } = "";
    [DataMember] public string UpdatedUtc { get; set; } = "";

    public override string ToString() =>
        string.IsNullOrWhiteSpace(Name) ? "(未命名)" : Name;
}

[DataContract]
internal sealed class CustomPackDetail
{
    [DataMember] public string Id { get; set; } = "";
    [DataMember] public string Name { get; set; } = "";
    [DataMember] public List<CustomPackItem> Items { get; set; } = [];
}

[DataContract]
internal sealed class CustomPackItem
{
    [DataMember] public string Id { get; set; } = "";
    [DataMember] public string Name { get; set; } = "";
    [DataMember] public string FileName { get; set; } = "";
    [DataMember] public string Kind { get; set; } = "reg";
    [DataMember] public bool Enabled { get; set; } = true;

    public CustomPackItemKind KindEnum => Kind switch
    {
        "cmd" or "bat" => CustomPackItemKind.Cmd,
        "ps1" => CustomPackItemKind.Ps1,
        _ => CustomPackItemKind.Reg,
    };

    public string KindLabel => KindEnum switch
    {
        CustomPackItemKind.Cmd => "CMD",
        CustomPackItemKind.Ps1 => "PowerShell",
        _ => "注册表",
    };
}

/// <summary>
/// 自定义配置方案：每个方案含多个 .reg / .cmd / .ps1（内容粘贴保存到本机 AppData，不依赖外部文件路径）。
/// 目录：%LocalAppData%\SrvDesk\custom-packs\
/// </summary>
internal static class CustomPackStore
{
    private static readonly object Gate = new();
    private static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    public static string RootDir =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SrvDesk", "custom-packs");

    private static string IndexPath => Path.Combine(RootDir, "index.json");

    public static string PackDir(string packId) => Path.Combine(RootDir, packId);
    public static string FilesDir(string packId) => Path.Combine(PackDir(packId), "files");
    public static string PackJsonPath(string packId) => Path.Combine(PackDir(packId), "pack.json");

    public static string ItemPath(string packId, CustomPackItem item) =>
        Path.Combine(FilesDir(packId), item.FileName);

    public static CustomPackIndex LoadIndex()
    {
        lock (Gate)
        {
            EnsureRoot();
            try
            {
                if (!File.Exists(IndexPath))
                    return new CustomPackIndex();
                var data = ReadJson<CustomPackIndex>(IndexPath) ?? new CustomPackIndex();
                data.Packs ??= [];
                return data;
            }
            catch
            {
                return new CustomPackIndex();
            }
        }
    }

    public static void SaveIndex(CustomPackIndex index)
    {
        lock (Gate)
        {
            EnsureRoot();
            index.Packs ??= [];
            WriteJson(IndexPath, index);
        }
    }

    public static CustomPackDetail? LoadPack(string packId)
    {
        if (string.IsNullOrWhiteSpace(packId)) return null;
        lock (Gate)
        {
            var path = PackJsonPath(packId);
            if (!File.Exists(path)) return null;
            try
            {
                var pack = ReadJson<CustomPackDetail>(path);
                if (pack is null) return null;
                pack.Items ??= [];
                return pack;
            }
            catch
            {
                return null;
            }
        }
    }

    public static void SavePack(CustomPackDetail pack)
    {
        lock (Gate)
        {
            Directory.CreateDirectory(FilesDir(pack.Id));
            pack.Items ??= [];
            WriteJson(PackJsonPath(pack.Id), pack);

            var index = LoadIndexUnlocked();
            var summary = index.Packs.FirstOrDefault(p =>
                string.Equals(p.Id, pack.Id, StringComparison.OrdinalIgnoreCase));
            if (summary is null)
            {
                summary = new CustomPackSummary { Id = pack.Id };
                index.Packs.Add(summary);
            }
            summary.Name = pack.Name;
            summary.UpdatedUtc = DateTime.UtcNow.ToString("o");
            index.LastPackId = pack.Id;
            WriteJson(IndexPath, index);
        }
    }

    public static CustomPackDetail CreatePack(string name)
    {
        var pack = new CustomPackDetail
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = string.IsNullOrWhiteSpace(name) ? "未命名方案" : name.Trim(),
            Items = [],
        };
        SavePack(pack);
        return pack;
    }

    public static void RenamePack(string packId, string newName)
    {
        var pack = LoadPack(packId) ?? throw new InvalidOperationException("方案不存在。");
        pack.Name = string.IsNullOrWhiteSpace(newName) ? pack.Name : newName.Trim();
        SavePack(pack);
    }

    public static void DeletePack(string packId)
    {
        lock (Gate)
        {
            var dir = PackDir(packId);
            if (Directory.Exists(dir))
            {
                try { Directory.Delete(dir, recursive: true); }
                catch { /* ignore partial */ }
            }

            var index = LoadIndexUnlocked();
            index.Packs.RemoveAll(p =>
                string.Equals(p.Id, packId, StringComparison.OrdinalIgnoreCase));
            if (string.Equals(index.LastPackId, packId, StringComparison.OrdinalIgnoreCase))
                index.LastPackId = index.Packs.FirstOrDefault()?.Id;
            WriteJson(IndexPath, index);
        }
    }

    public static CustomPackItem AddContent(
        CustomPackDetail pack, string name, string kind, string content)
    {
        kind = NormalizeKind(kind);
        content = NormalizeContent(content);
        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException("内容不能为空。");

        var itemId = Guid.NewGuid().ToString("N");
        var ext = ExtForKind(kind);
        var fileName = itemId + "." + ext;
        var destDir = FilesDir(pack.Id);
        Directory.CreateDirectory(destDir);
        var dest = Path.Combine(destDir, fileName);
        WriteItemFile(dest, kind, content);

        var item = new CustomPackItem
        {
            Id = itemId,
            Name = string.IsNullOrWhiteSpace(name) ? DefaultName(kind) : name.Trim(),
            FileName = fileName,
            Kind = kind,
            Enabled = true,
        };
        pack.Items.Add(item);
        SavePack(pack);
        return item;
    }

    public static void UpdateContent(CustomPackDetail pack, string itemId, string name, string kind, string content)
    {
        var item = pack.Items.FirstOrDefault(i =>
            string.Equals(i.Id, itemId, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("项不存在。");

        kind = NormalizeKind(kind);
        content = NormalizeContent(content);
        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException("内容不能为空。");

        var oldPath = ItemPath(pack.Id, item);
        var newExt = ExtForKind(kind);
        var newFileName = Path.GetFileNameWithoutExtension(item.FileName) + "." + newExt;
        var newPath = Path.Combine(FilesDir(pack.Id), newFileName);
        Directory.CreateDirectory(FilesDir(pack.Id));
        WriteItemFile(newPath, kind, content);

        if (!string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase)
            && File.Exists(oldPath))
        {
            try { File.Delete(oldPath); } catch { /* ignore */ }
        }

        item.Name = string.IsNullOrWhiteSpace(name) ? item.Name : name.Trim();
        item.Kind = kind;
        item.FileName = newFileName;
        SavePack(pack);
    }

    public static string ReadContent(string packId, CustomPackItem item)
    {
        var path = ItemPath(packId, item);
        if (!File.Exists(path)) return "";
        return File.ReadAllText(path, Encoding.UTF8);
    }

    /// <summary>根据正文猜测类型：.reg / .ps1 / .cmd。</summary>
    public static string DetectKind(string content)
    {
        var t = (content ?? "").TrimStart();
        if (t.Length == 0) return "cmd";
        if (t.StartsWith("Windows Registry Editor", StringComparison.OrdinalIgnoreCase)
            || t.StartsWith("REGEDIT", StringComparison.OrdinalIgnoreCase)
            || t.IndexOf("[HKEY_", StringComparison.OrdinalIgnoreCase) >= 0)
            return "reg";
        if (t.StartsWith("#", StringComparison.Ordinal)
            || t.StartsWith("<#", StringComparison.Ordinal)
            || t.IndexOf("param(", StringComparison.OrdinalIgnoreCase) >= 0
            || t.IndexOf("$PSVersionTable", StringComparison.OrdinalIgnoreCase) >= 0
            || t.IndexOf("Write-Host", StringComparison.OrdinalIgnoreCase) >= 0
            || t.IndexOf("Get-", StringComparison.OrdinalIgnoreCase) >= 0)
            return "ps1";
        return "cmd";
    }

    public static string NormalizeKind(string kind) =>
        (kind ?? "").Trim().ToLowerInvariant() switch
        {
            "ps1" or "powershell" or "ps" => "ps1",
            "cmd" or "bat" or "command" => "cmd",
            _ => "reg",
        };

    private static string ExtForKind(string kind) => NormalizeKind(kind) switch
    {
        "ps1" => "ps1",
        "cmd" => "cmd",
        _ => "reg",
    };

    private static string DefaultName(string kind) => NormalizeKind(kind) switch
    {
        "ps1" => "未命名 PowerShell",
        "cmd" => "未命名 CMD",
        _ => "未命名注册表",
    };

    private static string NormalizeContent(string content) =>
        (content ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Replace("\n", "\r\n").TrimEnd() + "\r\n";

    private static void WriteItemFile(string path, string kind, string content)
    {
        // .reg 用 UTF-8 BOM，便于 regedit 识别；脚本用 UTF-8
        var enc = NormalizeKind(kind) == "reg"
            ? new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)
            : Utf8;
        File.WriteAllText(path, content, enc);
    }

    public static void RemoveItem(CustomPackDetail pack, string itemId)
    {
        var item = pack.Items.FirstOrDefault(i =>
            string.Equals(i.Id, itemId, StringComparison.OrdinalIgnoreCase));
        if (item is null) return;
        pack.Items.Remove(item);
        try
        {
            var path = ItemPath(pack.Id, item);
            if (File.Exists(path)) File.Delete(path);
        }
        catch { /* ignore */ }
        SavePack(pack);
    }

    public static void MoveItem(CustomPackDetail pack, string itemId, int delta)
    {
        var i = pack.Items.FindIndex(x =>
            string.Equals(x.Id, itemId, StringComparison.OrdinalIgnoreCase));
        if (i < 0) return;
        var j = i + delta;
        if (j < 0 || j >= pack.Items.Count) return;
        (pack.Items[i], pack.Items[j]) = (pack.Items[j], pack.Items[i]);
        SavePack(pack);
    }

    public static void SetItemEnabled(CustomPackDetail pack, string itemId, bool enabled)
    {
        var item = pack.Items.FirstOrDefault(i =>
            string.Equals(i.Id, itemId, StringComparison.OrdinalIgnoreCase));
        if (item is null) return;
        item.Enabled = enabled;
        SavePack(pack);
    }

    public static void SetLastPackId(string? packId)
    {
        lock (Gate)
        {
            var index = LoadIndexUnlocked();
            index.LastPackId = packId;
            WriteJson(IndexPath, index);
        }
    }

    /// <summary>导出全部自定义方案（含脚本正文），供配置备份。</summary>
    public static List<CustomPackExport> ExportAll(out string? lastPackId)
    {
        lock (Gate)
        {
            var index = LoadIndexUnlocked();
            lastPackId = index.LastPackId;
            var list = new List<CustomPackExport>();
            foreach (var summary in index.Packs)
            {
                var pack = LoadPackUnlocked(summary.Id);
                if (pack is null) continue;
                var export = new CustomPackExport
                {
                    Id = pack.Id,
                    Name = pack.Name,
                    Items = [],
                };
                foreach (var item in pack.Items)
                {
                    export.Items.Add(new CustomPackItemExport
                    {
                        Id = item.Id,
                        Name = item.Name,
                        FileName = item.FileName,
                        Kind = item.Kind,
                        Enabled = item.Enabled,
                        Content = ReadContentUnlocked(pack.Id, item),
                    });
                }
                list.Add(export);
            }
            return list;
        }
    }

    /// <summary>用导入内容整体替换本机自定义方案。</summary>
    public static void ReplaceAll(IReadOnlyList<CustomPackExport> packs, string? lastPackId)
    {
        lock (Gate)
        {
            EnsureRoot();
            // 清空旧方案目录
            foreach (var dir in Directory.Exists(RootDir)
                         ? Directory.GetDirectories(RootDir)
                         : [])
            {
                try { Directory.Delete(dir, recursive: true); }
                catch { /* ignore */ }
            }

            var index = new CustomPackIndex { Packs = [], LastPackId = null };
            foreach (var src in packs ?? Array.Empty<CustomPackExport>())
            {
                if (string.IsNullOrWhiteSpace(src.Id)) continue;
                var pack = new CustomPackDetail
                {
                    Id = src.Id.Trim(),
                    Name = string.IsNullOrWhiteSpace(src.Name) ? "未命名方案" : src.Name.Trim(),
                    Items = [],
                };
                Directory.CreateDirectory(FilesDir(pack.Id));
                foreach (var srcItem in src.Items ?? [])
                {
                    var kind = NormalizeKind(srcItem.Kind);
                    var itemId = string.IsNullOrWhiteSpace(srcItem.Id)
                        ? Guid.NewGuid().ToString("N")
                        : srcItem.Id.Trim();
                    var fileName = string.IsNullOrWhiteSpace(srcItem.FileName)
                        ? itemId + "." + ExtForKind(kind)
                        : Path.GetFileName(srcItem.FileName);
                    // 确保扩展名与类型一致
                    fileName = Path.GetFileNameWithoutExtension(fileName) + "." + ExtForKind(kind);
                    var content = NormalizeContent(srcItem.Content ?? "");
                    var dest = Path.Combine(FilesDir(pack.Id), fileName);
                    WriteItemFile(dest, kind, content);
                    pack.Items.Add(new CustomPackItem
                    {
                        Id = itemId,
                        Name = string.IsNullOrWhiteSpace(srcItem.Name) ? DefaultName(kind) : srcItem.Name.Trim(),
                        FileName = fileName,
                        Kind = kind,
                        Enabled = srcItem.Enabled,
                    });
                }
                WriteJson(PackJsonPath(pack.Id), pack);
                index.Packs.Add(new CustomPackSummary
                {
                    Id = pack.Id,
                    Name = pack.Name,
                    UpdatedUtc = DateTime.UtcNow.ToString("o"),
                });
            }

            if (!string.IsNullOrWhiteSpace(lastPackId)
                && index.Packs.Any(p => string.Equals(p.Id, lastPackId, StringComparison.OrdinalIgnoreCase)))
                index.LastPackId = lastPackId;
            else
                index.LastPackId = index.Packs.FirstOrDefault()?.Id;

            WriteJson(IndexPath, index);
        }
    }

    private static CustomPackDetail? LoadPackUnlocked(string packId)
    {
        var path = PackJsonPath(packId);
        if (!File.Exists(path)) return null;
        try
        {
            var pack = ReadJson<CustomPackDetail>(path);
            if (pack is null) return null;
            pack.Items ??= [];
            return pack;
        }
        catch
        {
            return null;
        }
    }

    private static string ReadContentUnlocked(string packId, CustomPackItem item)
    {
        var path = ItemPath(packId, item);
        if (!File.Exists(path)) return "";
        return File.ReadAllText(path, Encoding.UTF8);
    }

    private static CustomPackIndex LoadIndexUnlocked()
    {
        EnsureRoot();
        if (!File.Exists(IndexPath))
            return new CustomPackIndex();
        var data = ReadJson<CustomPackIndex>(IndexPath) ?? new CustomPackIndex();
        data.Packs ??= [];
        return data;
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
