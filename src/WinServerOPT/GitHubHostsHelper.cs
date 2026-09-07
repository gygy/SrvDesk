using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

namespace SrvDesk;

/// <summary>
/// 一键写入 GitHub hosts，数据源参考 maxiaof/github-hosts（每日更新）。
/// https://github.com/maxiaof/github-hosts
/// </summary>
internal static class GitHubHostsHelper
{
    public const string ProjectUrl = "https://github.com/maxiaof/github-hosts";
    public const string UpdateUrl = "https://raw.githubusercontent.com/maxiaof/github-hosts/master/hosts";
    public const string MarkerStart = "#Github Hosts Start";
    public const string MarkerEnd = "#Github Hosts End";
    public const string CommentTag = "GitHub Hosts";

    private static readonly string[] FetchUrls =
    [
        UpdateUrl,
        "https://cdn.jsdelivr.net/gh/maxiaof/github-hosts@master/hosts",
        "https://fastly.jsdelivr.net/gh/maxiaof/github-hosts@master/hosts",
        "https://ghproxy.net/" + UpdateUrl,
        "https://mirror.ghproxy.com/" + UpdateUrl,
    ];

    private static readonly Regex BlockRegex = new(
        @"#Github Hosts Start[\s\S]*?#Github Hosts End\s*",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public sealed class FetchResult
    {
        public string Text { get; set; } = "";
        public string SourceUrl { get; set; } = "";
        public List<HostsEntry> Entries { get; } = [];
    }

    /// <summary>从多个镜像拉取最新 hosts 文本并解析映射行。</summary>
    public static FetchResult FetchLatest()
    {
        EnsureTls();
        Exception? last = null;
        foreach (var url in FetchUrls)
        {
            try
            {
                var text = DownloadText(url);
                if (!LooksLikeGitHubHosts(text))
                    continue;

                var parsed = HostsFileHelper.ParseText(text);
                if (parsed.Entries.Count == 0)
                    continue;

                var result = new FetchResult { Text = text.Trim(), SourceUrl = url };
                foreach (var e in parsed.Entries)
                {
                    e.Enabled = true;
                    if (string.IsNullOrWhiteSpace(e.Comment))
                        e.Comment = CommentTag;
                    result.Entries.Add(e);
                }
                return result;
            }
            catch (Exception ex)
            {
                last = ex;
            }
        }

        throw new InvalidOperationException(
            "无法获取 GitHub hosts。请检查网络，或稍后再试。\r\n" +
            "项目：" + ProjectUrl +
            (last is null ? "" : "\r\n原因：" + last.Message));
    }

    /// <summary>
    /// 将 GitHub hosts 块写入系统 hosts：替换已有 Start/End 区块，或按主机名去重后追加。
    /// </summary>
    public static int ApplyToHostsFile(FetchResult fetch, bool backup, bool flushDns)
    {
        if (fetch.Entries.Count == 0)
            throw new InvalidOperationException("未解析到有效的 GitHub hosts 映射。");

        var path = HostsFileHelper.FilePath;
        if (!File.Exists(path))
            throw new FileNotFoundException("找不到 hosts 文件。", path);

        if (backup)
            HostsFileHelper.Backup(path);

        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        try
        {
            var bytes = File.ReadAllBytes(path);
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        }
        catch { /* 默认 UTF-8 */ }

        var current = File.ReadAllText(path, encoding);
        current = RemoveGitHubBlock(current);

        // 去掉与本次 GitHub 列表冲突的旧映射（避免重复）
        var githubHosts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var e in fetch.Entries)
        {
            foreach (var h in e.Hosts.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries))
                githubHosts.Add(h);
        }

        var kept = new StringBuilder();
        foreach (var raw in current.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
        {
            var line = raw.TrimEnd();
            if (line.Length == 0)
            {
                kept.AppendLine();
                continue;
            }

            if (IsGitHubManagedLine(line, githubHosts))
                continue;

            kept.AppendLine(line);
        }

        var block = BuildBlock(fetch);
        var output = kept.ToString().TrimEnd() + "\r\n\r\n" + block + "\r\n";

        try
        {
            var attr = File.GetAttributes(path);
            if ((attr & FileAttributes.ReadOnly) != 0)
                File.SetAttributes(path, attr & ~FileAttributes.ReadOnly);
        }
        catch { /* 后续写入抛出 */ }

        var temp = path + ".SrvDesk.tmp";
        File.WriteAllText(temp, output, encoding);
        File.Copy(temp, path, overwrite: true);
        try { File.Delete(temp); } catch { /* ignore */ }

        ApplyLog.SystemChange(
            path,
            "写入 GitHub hosts",
            "来源 " + (fetch.SourceUrl.Length > 0 ? fetch.SourceUrl : ProjectUrl),
            fetch.Entries.Count + " 条");

        if (flushDns)
            HostsFileHelper.FlushDns();

        return fetch.Entries.Count;
    }

    public static string RemoveGitHubBlock(string content)
    {
        if (string.IsNullOrEmpty(content)) return "";
        return BlockRegex.Replace(content, "").TrimEnd();
    }

    public static string BuildBlock(FetchResult fetch)
    {
        var sb = new StringBuilder();
        sb.AppendLine(MarkerStart);
        sb.AppendLine("#Update Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine("#Project Address: " + ProjectUrl);
        sb.AppendLine("#Update URL: " + UpdateUrl);
        if (fetch.SourceUrl.Length > 0 &&
            !fetch.SourceUrl.Equals(UpdateUrl, StringComparison.OrdinalIgnoreCase))
            sb.AppendLine("#Fetched From: " + fetch.SourceUrl);

        foreach (var e in fetch.Entries)
        {
            var address = (e.Address ?? "").Trim();
            var hosts = string.Join(" ", (e.Hosts ?? "").Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries));
            if (address.Length == 0 || hosts.Length == 0) continue;
            sb.AppendLine(address + " " + hosts);
        }

        sb.Append(MarkerEnd);
        return sb.ToString();
    }

    private static bool LooksLikeGitHubHosts(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        if (text.IndexOf("github.com", StringComparison.OrdinalIgnoreCase) < 0) return false;
        return HostsFileHelper.ParseText(text).Entries.Count >= 5;
    }

    private static bool IsGitHubManagedLine(string line, HashSet<string> githubHosts)
    {
        var t = line.Trim();
        if (t.StartsWith("#Github Hosts", StringComparison.OrdinalIgnoreCase))
            return true;
        if (t.IndexOf("GitHub Hosts", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        // 解析映射行，主机名落在本次 GitHub 列表内则视为旧条目
        var m = Regex.Match(t, @"^\s*#?\s*((?:\d{1,3}\.){3}\d{1,3}|[0-9a-fA-F:]+)\s+([^#]+)");
        if (!m.Success) return false;
        foreach (var h in m.Groups[2].Value.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (githubHosts.Contains(h))
                return true;
        }
        return false;
    }

    private static string DownloadText(string url)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(12) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("SrvDesk/1.0");
        using var response = client.GetAsync(url).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();
        return response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
    }

    private static void EnsureTls()
    {
        try
        {
            ServicePointManager.SecurityProtocol |=
                SecurityProtocolType.Tls12 | (SecurityProtocolType)0x3000; // Tls13 if available
        }
        catch
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }
    }
}
