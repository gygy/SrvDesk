using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace SrvDesk;

internal sealed class AppReleaseInfo
{
    public string Tag { get; set; } = "";
    public string Version { get; set; } = "";
    public string HtmlUrl { get; set; } = "";
    public string Notes { get; set; } = "";
    public string AssetName { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
    public long Size { get; set; }
    public bool IsNewer { get; set; }
}

/// <summary>从 GitHub Releases 检测并替换本机 SrvDesk.exe。</summary>
internal static class AppUpdate
{
    public const string RepoApiLatest = "https://api.github.com/repos/gygy/SrvDesk/releases/latest";
    public const string ReleasesPage = "https://github.com/gygy/SrvDesk/releases";

    private static readonly string[] ApiMirrors =
    [
        RepoApiLatest,
        "https://ghproxy.net/" + RepoApiLatest,
        "https://mirror.ghproxy.com/" + RepoApiLatest,
    ];

    public static void TryDeleteBackup()
    {
        try
        {
            var bak = Application.ExecutablePath + ".bak";
            if (File.Exists(bak))
                File.Delete(bak);
        }
        catch { /* 下次再删 */ }
    }

    public static AppReleaseInfo CheckLatest()
    {
        EnsureTls();
        Exception? last = null;
        foreach (var url in ApiMirrors)
        {
            try
            {
                var json = DownloadText(url, TimeSpan.FromSeconds(15));
                var info = ParseRelease(json);
                if (info is null) continue;
                info.IsNewer = IsNewer(info.Version, AppBrand.VersionText);
                return info;
            }
            catch (Exception ex)
            {
                last = ex;
            }
        }

        throw new InvalidOperationException(
            "无法检查更新。请确认能访问 GitHub Releases。\r\n" + ReleasesPage +
            (last is null ? "" : "\r\n原因：" + last.Message));
    }

    public static string DownloadAsset(AppReleaseInfo release, Action<int, string> progress, CancellationToken cancel)
    {
        if (string.IsNullOrWhiteSpace(release.DownloadUrl))
            throw new InvalidOperationException("该版本没有 SrvDesk.exe 附件。");

        var dest = Path.Combine(Path.GetTempPath(),
            $"SrvDesk-{Sanitize(release.Version)}.exe");
        if (File.Exists(dest))
        {
            try { File.Delete(dest); } catch { /* overwrite */ }
        }

        var urls = new List<string> { release.DownloadUrl };
        if (release.DownloadUrl.IndexOf("ghproxy", StringComparison.OrdinalIgnoreCase) < 0)
        {
            urls.Add("https://ghproxy.net/" + release.DownloadUrl);
            urls.Add("https://mirror.ghproxy.com/" + release.DownloadUrl);
        }

        EnsureTls();
        Exception? last = null;
        foreach (var url in urls)
        {
            cancel.ThrowIfCancellationRequested();
            try
            {
                DownloadFile(url, dest, progress, cancel);
                if (!LooksLikeExe(dest))
                {
                    try { File.Delete(dest); } catch { /* ignore */ }
                    last = new InvalidOperationException("下载文件不是有效的 exe。");
                    continue;
                }
                return dest;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                last = ex;
                try { if (File.Exists(dest)) File.Delete(dest); } catch { /* ignore */ }
            }
        }

        throw last ?? new InvalidOperationException("下载失败。");
    }

    public static void ReplaceAndRestart(string newExePath)
    {
        if (!LooksLikeExe(newExePath))
            throw new InvalidOperationException("新文件无效。");

        var current = Application.ExecutablePath;
        var bak = current + ".bak";
        try
        {
            if (File.Exists(bak))
                File.Delete(bak);
            File.Move(current, bak);
            File.Copy(newExePath, current, overwrite: true);
        }
        catch
        {
            LaunchCmdReplacer(current, newExePath);
            return;
        }

        ApplyLog.Write("自动更新：已替换 " + current);
        Process.Start(new ProcessStartInfo
        {
            FileName = current,
            UseShellExecute = true,
        });
        Environment.Exit(0);
    }

    private static void LaunchCmdReplacer(string current, string newExePath)
    {
        var pid = Process.GetCurrentProcess().Id;
        var cmd = Path.Combine(Path.GetTempPath(), "srvdesk-update.cmd");
        var script =
            "@echo off\r\n" +
            $":wait\r\n" +
            "timeout /t 1 /nobreak >nul\r\n" +
            $"tasklist /fi \"PID eq {pid}\" | find \"{pid}\" >nul && goto wait\r\n" +
            $"copy /y \"{newExePath}\" \"{current}\" >nul\r\n" +
            $"start \"\" \"{current}\"\r\n" +
            "del \"%~f0\"\r\n";
        File.WriteAllText(cmd, script, Encoding.Default);
        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/c \"" + cmd + "\"",
            CreateNoWindow = true,
            UseShellExecute = false,
        });
        ApplyLog.Write("自动更新：通过脚本替换 " + current);
        Environment.Exit(0);
    }

    public static bool IsNewer(string remote, string local)
    {
        if (!TryParseVersion(remote, out var r) || !TryParseVersion(local, out var l))
            return !string.Equals(Normalize(remote), Normalize(local), StringComparison.OrdinalIgnoreCase);
        return r > l;
    }

    private static bool TryParseVersion(string text, out Version version)
    {
        version = new Version(0, 0);
        var n = Normalize(text);
        return Version.TryParse(n, out version);
    }

    private static string Normalize(string text)
    {
        var s = (text ?? "").Trim();
        if (s.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            s = s.Substring(1);
        var plus = s.IndexOf('+');
        if (plus >= 0) s = s.Substring(0, plus);
        var dash = s.IndexOf('-');
        if (dash >= 0) s = s.Substring(0, dash);
        return s.Trim();
    }

    private static AppReleaseInfo? ParseRelease(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var dto = new DataContractJsonSerializer(typeof(GhRelease)).ReadObject(ms) as GhRelease;
            var tag = dto?.TagName?.Trim() ?? "";
            if (tag.Length == 0)
                return FallbackParse(json);
            var asset = dto!.Assets?.FirstOrDefault(a =>
                a.Name != null &&
                a.Name.Equals("SrvDesk.exe", StringComparison.OrdinalIgnoreCase));
            asset ??= dto.Assets?.FirstOrDefault(a =>
                a.Name != null &&
                a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));
            return new AppReleaseInfo
            {
                Tag = tag,
                Version = Normalize(tag),
                HtmlUrl = string.IsNullOrWhiteSpace(dto.HtmlUrl) ? ReleasesPage : dto.HtmlUrl!,
                Notes = StripMd(dto.Body ?? ""),
                AssetName = asset?.Name ?? "",
                DownloadUrl = asset?.BrowserDownloadUrl ?? "",
                Size = asset?.Size ?? 0,
            };
        }
        catch
        {
            return FallbackParse(json);
        }
    }

    private static AppReleaseInfo? FallbackParse(string json)
    {
        var tag = Regex.Match(json, "\"tag_name\"\\s*:\\s*\"([^\"]+)\"");
        if (!tag.Success) return null;
        var url = Regex.Match(json,
            "\"browser_download_url\"\\s*:\\s*\"([^\"]*SrvDesk\\.exe)\"",
            RegexOptions.IgnoreCase);
        var html = Regex.Match(json, "\"html_url\"\\s*:\\s*\"([^\"]+)\"");
        return new AppReleaseInfo
        {
            Tag = tag.Groups[1].Value,
            Version = Normalize(tag.Groups[1].Value),
            HtmlUrl = html.Success ? html.Groups[1].Value : ReleasesPage,
            DownloadUrl = url.Success ? url.Groups[1].Value.Replace("\\/", "/") : "",
            AssetName = "SrvDesk.exe",
        };
    }

    private static string StripMd(string body)
    {
        var t = body.Replace("\r\n", "\n").Trim();
        if (t.Length > 800) t = t.Substring(0, 800) + "…";
        return t;
    }

    private static string DownloadText(string url, TimeSpan timeout)
    {
        using var handler = NewHandler();
        using var client = new HttpClient(handler) { Timeout = timeout };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("SrvDesk/" + AppBrand.VersionText);
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        using var resp = client.GetAsync(url).GetAwaiter().GetResult();
        resp.EnsureSuccessStatusCode();
        return resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
    }

    private static void DownloadFile(string url, string dest, Action<int, string> progress, CancellationToken cancel)
    {
        using var handler = NewHandler();
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(5) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("SrvDesk/" + AppBrand.VersionText);
        using var resp = client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancel)
            .GetAwaiter().GetResult();
        resp.EnsureSuccessStatusCode();
        var total = resp.Content.Headers.ContentLength ?? 0;
        using var input = resp.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
        using var output = File.Create(dest);
        var buf = new byte[81920];
        long read = 0;
        int n;
        while ((n = input.Read(buf, 0, buf.Length)) > 0)
        {
            cancel.ThrowIfCancellationRequested();
            output.Write(buf, 0, n);
            read += n;
            var pct = total > 0 ? (int)Math.Min(99, read * 100 / total) : 0;
            progress(pct, $"下载中 {FormatSize(read)}" + (total > 0 ? " / " + FormatSize(total) : ""));
        }
        progress(100, "下载完成 " + FormatSize(read));
    }

    private static HttpClientHandler NewHandler()
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            UseProxy = true,
            Proxy = WebRequest.GetSystemWebProxy(),
            UseDefaultCredentials = true,
        };
        try { handler.Proxy!.Credentials = CredentialCache.DefaultCredentials; }
        catch { /* ignore */ }
        return handler;
    }

    private static void EnsureTls()
    {
        try
        {
            ServicePointManager.SecurityProtocol |=
                SecurityProtocolType.Tls12 | (SecurityProtocolType)0x3000;
        }
        catch
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }
    }

    private static bool LooksLikeExe(string path)
    {
        try
        {
            var info = new FileInfo(path);
            if (!info.Exists || info.Length < 80_000) return false;
            using var fs = File.OpenRead(path);
            return fs.ReadByte() == 0x4D && fs.ReadByte() == 0x5A;
        }
        catch
        {
            return false;
        }
    }

    private static string Sanitize(string version)
    {
        var sb = new StringBuilder();
        foreach (var c in version)
        {
            if (char.IsLetterOrDigit(c) || c is '.' or '-')
                sb.Append(c);
        }
        return sb.Length > 0 ? sb.ToString() : "latest";
    }

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024) return bytes + " B";
        if (bytes < 1024 * 1024) return (bytes / 1024) + " KB";
        return $"{bytes / (1024.0 * 1024.0):0.0} MB";
    }

    [DataContract]
    private sealed class GhRelease
    {
        [DataMember(Name = "tag_name")] public string? TagName { get; set; }
        [DataMember(Name = "html_url")] public string? HtmlUrl { get; set; }
        [DataMember(Name = "body")] public string? Body { get; set; }
        [DataMember(Name = "assets")] public GhAsset[]? Assets { get; set; }
    }

    [DataContract]
    private sealed class GhAsset
    {
        [DataMember(Name = "name")] public string? Name { get; set; }
        [DataMember(Name = "browser_download_url")] public string? BrowserDownloadUrl { get; set; }
        [DataMember(Name = "size")] public long Size { get; set; }
    }
}
