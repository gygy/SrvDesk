using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace WinOpt;

/// <summary>
/// 用户自定义常用软件（winget 安装）。
/// 存于 %LocalAppData%\WinOpt\custom-software.json
/// </summary>
internal static class CustomSoftwareStore
{
    [DataContract]
    private sealed class StoreFile
    {
        [DataMember] public List<CustomSoftwareEntry> Items { get; set; } = [];
    }

    [DataContract]
    internal sealed class CustomSoftwareEntry
    {
        [DataMember] public string Id { get; set; } = "";
        [DataMember] public string Title { get; set; } = "";
        /// <summary>winget 包 ID，如 Google.Chrome；也可填可搜索名称。</summary>
        [DataMember] public string WingetId { get; set; } = "";
    }

    private static readonly object Gate = new();

    private static string FilePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WinOpt", "custom-software.json");

    public static List<CustomSoftwareEntry> Load()
    {
        lock (Gate)
        {
            try
            {
                if (!File.Exists(FilePath))
                    return [];
                using var ms = new MemoryStream(Encoding.UTF8.GetBytes(File.ReadAllText(FilePath, Encoding.UTF8)));
                var data = new DataContractJsonSerializer(typeof(StoreFile)).ReadObject(ms) as StoreFile;
                return data?.Items ?? [];
            }
            catch
            {
                return [];
            }
        }
    }

    public static List<CommonSoftwareItem> LoadAsItems() =>
        Load().Select(ToItem).Where(x => x is not null).Cast<CommonSoftwareItem>().ToList();

    public static void Save(IEnumerable<CustomSoftwareEntry> items)
    {
        lock (Gate)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            var data = new StoreFile { Items = items.ToList() };
            using var ms = new MemoryStream();
            new DataContractJsonSerializer(typeof(StoreFile)).WriteObject(ms, data);
            File.WriteAllText(FilePath, Encoding.UTF8.GetString(ms.ToArray()), Encoding.UTF8);
        }
    }

    public static CustomSoftwareEntry Add(string title, string wingetId)
    {
        title = (title ?? "").Trim();
        wingetId = (wingetId ?? "").Trim();
        if (title.Length == 0)
            throw new InvalidOperationException("请填写软件名称。");
        if (wingetId.Length == 0)
            throw new InvalidOperationException("请填写 winget 包 ID 或可搜索名称。");

        var list = Load();
        if (list.Any(x =>
                string.Equals(x.WingetId, wingetId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(x.Title, title, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("已存在同名或相同 winget ID 的自定义项。");

        var entry = new CustomSoftwareEntry
        {
            Id = "custom-" + Guid.NewGuid().ToString("N")[..12],
            Title = title,
            WingetId = wingetId,
        };
        list.Add(entry);
        Save(list);
        return entry;
    }

    public static void Update(string id, string title, string wingetId)
    {
        title = (title ?? "").Trim();
        wingetId = (wingetId ?? "").Trim();
        if (title.Length == 0)
            throw new InvalidOperationException("请填写软件名称。");
        if (wingetId.Length == 0)
            throw new InvalidOperationException("请填写 winget 包 ID 或可搜索名称。");

        var list = Load();
        var entry = list.FirstOrDefault(x =>
            string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("自定义项不存在。");
        if (list.Any(x =>
                !string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase)
                && (string.Equals(x.WingetId, wingetId, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(x.Title, title, StringComparison.OrdinalIgnoreCase))))
            throw new InvalidOperationException("已存在同名或相同 winget ID 的自定义项。");

        entry.Title = title;
        entry.WingetId = wingetId;
        Save(list);
    }

    public static void Remove(string id)
    {
        var list = Load();
        list.RemoveAll(x => string.Equals(x.Id, id, StringComparison.OrdinalIgnoreCase));
        Save(list);
    }

    public static CommonSoftwareItem ToItem(CustomSoftwareEntry e)
    {
        var title = string.IsNullOrWhiteSpace(e.Title) ? e.WingetId : e.Title.Trim();
        var winget = (e.WingetId ?? "").Trim();
        return new CommonSoftwareItem
        {
            Id = string.IsNullOrWhiteSpace(e.Id) ? "custom-" + Guid.NewGuid().ToString("N")[..12] : e.Id,
            Title = title,
            Category = CommonSoftwareCatalog.CustomCategory,
            WingetId = winget,
            DetectPatterns = [title],
            DownloadUrl = "",
            Essential = false,
            IsCustom = true,
        };
    }
}
