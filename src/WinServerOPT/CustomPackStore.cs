using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace WinOpt;

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
/// 自定义配置方案：每个方案含多个 .reg / .cmd / .ps1，文件复制到本地目录以便下次打开仍可用。
/// 目录：%LocalAppData%\WinOpt\custom-packs\
/// </summary>
internal static class CustomPackStore
{
    private static readonly object Gate = new();
    private static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    public static string RootDir =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WinOpt", "custom-packs");

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

    public static CustomPackItem AddFile(CustomPackDetail pack, string sourcePath)
    {
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("找不到文件：" + sourcePath, sourcePath);

        var ext = Path.GetExtension(sourcePath).TrimStart('.').ToLowerInvariant();
        var kind = ext switch
        {
            "reg" => "reg",
            "cmd" or "bat" => "cmd",
            "ps1" => "ps1",
            _ => throw new InvalidOperationException("仅支持 .reg / .cmd / .bat / .ps1 文件。"),
        };

        var itemId = Guid.NewGuid().ToString("N");
        var fileName = itemId + "." + (ext == "bat" ? "cmd" : ext);
        var destDir = FilesDir(pack.Id);
        Directory.CreateDirectory(destDir);
        var dest = Path.Combine(destDir, fileName);
        File.Copy(sourcePath, dest, overwrite: true);

        var item = new CustomPackItem
        {
            Id = itemId,
            Name = Path.GetFileName(sourcePath),
            FileName = fileName,
            Kind = kind,
            Enabled = true,
        };
        pack.Items.Add(item);
        SavePack(pack);
        return item;
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
