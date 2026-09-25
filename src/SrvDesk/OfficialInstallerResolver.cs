using System.Net;
using System.Text.RegularExpressions;

namespace SrvDesk;

/// <summary>
/// 从官网 / GitHub Releases 解析最新 Windows 安装包直链，供常用软件自动下载安装。
/// </summary>
internal static class OfficialInstallerResolver
{
    private const string BrowserUa =
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36 SrvDesk/1.0";

    private static readonly Regex Href = new(
        "(?:href|src)\\s*=\\s*[\"']([^\"']+)[\"']",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex GithubAsset = new(
        "\"browser_download_url\"\\s*:\\s*\"(https:[^\"]+)\"",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex JsonDownloadField = new(
        "\"(?:downloadUrl|data|url|fileUrl|pkgUrl|pkg_url)\"\\s*:\\s*\"([^\"]+)\"",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static bool IsDirectInstallerUrl(string? url)
    {
        var text = (url ?? "").Trim();
        if (text.Length == 0) return false;
        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri)) return false;
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return false;

        var path = uri.AbsolutePath;
        if (HasInstallerExtension(path)) return true;
        if (LooksLikeOpaqueInstallerDownload(uri)) return true;
        if (path.IndexOf("/latest/download/", StringComparison.OrdinalIgnoreCase) >= 0) return true;
        if (uri.Host.IndexOf("aka.ms", StringComparison.OrdinalIgnoreCase) >= 0) return true;
        if (uri.Host.IndexOf("dl.google.com", StringComparison.OrdinalIgnoreCase) >= 0) return true;
        if (uri.Query.IndexOf("platform=win", StringComparison.OrdinalIgnoreCase) >= 0) return true;
        return false;
    }

    public static string GuessSilentArgs(string urlOrPath)
    {
        var name = urlOrPath ?? "";
        try
        {
            if (Uri.TryCreate(urlOrPath, UriKind.Absolute, out var uri))
                name = uri.AbsolutePath;
        }
        catch { /* use raw */ }

        if (name.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
            return "/qn /norestart";
        if (name.IndexOf("vc_redist", StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("windowsdesktop-runtime", StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("aspnetcore-runtime", StringComparison.OrdinalIgnoreCase) >= 0)
            return "/install /quiet /norestart";
        if (name.IndexOf("Git-", StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("XnView", StringComparison.OrdinalIgnoreCase) >= 0
            || name.IndexOf("NeatDM", StringComparison.OrdinalIgnoreCase) >= 0)
            return "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART";
        return "/S";
    }

    /// <summary>解析失败返回空串。</summary>
    public static string TryResolve(CommonSoftwareItem item)
    {
        if (item is null) return "";

        var offline = (item.OfflineInstallerUrl ?? "").Trim();
        if (IsFreshLatestUrl(offline))
            return offline;

        var api = (item.LatestApiUrl ?? "").Trim();
        if (api.Length > 0)
        {
            try
            {
                var fromApi = TryResolveFromJsonApi(api, item.InstallerLinkPattern);
                if (fromApi.Length > 0) return fromApi;
            }
            catch (Exception ex)
            {
                ApplyLog.Write("官网 API 解析失败 " + item.Id + "：" + ex.Message);
            }
        }

        var wingetId = (item.WingetId ?? "").Trim();
        if (wingetId.Length > 0 && wingetId.IndexOf('.') > 0 && !LooksLikeStoreProductId(wingetId))
        {
            try
            {
                var fromWingetPkgs = TryResolveFromWingetPkgs(wingetId, item.InstallerLinkPattern);
                if (fromWingetPkgs.Length > 0) return fromWingetPkgs;
            }
            catch (Exception ex)
            {
                ApplyLog.Write("winget-pkgs 解析失败 " + wingetId + "：" + ex.Message);
            }
        }

        var repo = (item.GitHubRepo ?? "").Trim();
        if (repo.Length == 0)
            repo = TryParseGithubRepo(item.DownloadUrl);
        if (repo.Length > 0)
        {
            var fromGh = TryResolveGithubLatest(repo, item.InstallerLinkPattern);
            if (fromGh.Length > 0) return fromGh;
        }

        var page = (item.DownloadUrl ?? "").Trim();
        if (page.Length > 0 && !IsDirectInstallerUrl(page))
        {
            var fromPage = TryResolveFromHtmlPage(page, item.InstallerLinkPattern);
            if (fromPage.Length > 0) return fromPage;
        }

        if (IsDirectInstallerUrl(offline))
            return offline;

        if (page.Length > 0)
            return TryResolveFromHtmlPage(page, item.InstallerLinkPattern);

        if (offline.Length > 0 && !IsDirectInstallerUrl(offline))
            return TryResolveFromHtmlPage(offline, item.InstallerLinkPattern);

        return "";
    }

    private static bool LooksLikeStoreProductId(string id) =>
        id.Length >= 12 && id.IndexOf('.') < 0;

    /// <summary>同一地址会持续指向最新包（版本号不写在 URL 里）。</summary>
    public static bool IsFreshLatestUrl(string? url)
    {
        var u = (url ?? "").Trim();
        if (!IsDirectInstallerUrl(u)) return false;
        if (u.IndexOf("/latest/", StringComparison.OrdinalIgnoreCase) >= 0) return true;
        if (u.IndexOf("/Latest/", StringComparison.Ordinal) >= 0) return true;
        if (u.IndexOf("aka.ms", StringComparison.OrdinalIgnoreCase) >= 0) return true;
        if (u.IndexOf("dl.google.com", StringComparison.OrdinalIgnoreCase) >= 0) return true;
        if (u.IndexOf("dldir1", StringComparison.OrdinalIgnoreCase) >= 0) return true;
        if (u.IndexOf("channel=release", StringComparison.OrdinalIgnoreCase) >= 0) return true;
        if (u.IndexOf("download.xnview.com", StringComparison.OrdinalIgnoreCase) >= 0) return true;
        if (u.IndexOf("downloadHr", StringComparison.OrdinalIgnoreCase) >= 0
            && u.IndexOf("huorong.cn", StringComparison.OrdinalIgnoreCase) >= 0) return true;
        return false;
    }

    public static string TryParseGithubRepo(string? url)
    {
        var text = (url ?? "").Trim();
        if (text.Length == 0) return "";
        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri)) return "";
        if (uri.Host.IndexOf("github.com", StringComparison.OrdinalIgnoreCase) < 0) return "";
        var parts = uri.AbsolutePath.Trim('/').Split('/');
        if (parts.Length < 2) return "";
        return parts[0] + "/" + parts[1];
    }

    private static string TryResolveGithubLatest(string repo, string? pattern)
    {
        var api = "https://api.github.com/repos/" + repo.Trim() + "/releases/latest";
        try
        {
            var json = FetchText(api, accept: "application/vnd.github+json");
            var urls = new List<string>();
            foreach (Match m in GithubAsset.Matches(json))
            {
                var u = UnescapeJson(m.Groups[1].Value);
                if (u.Length > 0) urls.Add(u);
            }

            var picked = PickBest(urls, pattern, repo);
            if (picked.Length > 0) return picked;
        }
        catch (Exception ex)
        {
            ApplyLog.Write("GitHub API 解析失败 " + repo + "：" + ex.Message);
        }

        try
        {
            return TryResolveFromHtmlPage("https://github.com/" + repo.Trim() + "/releases/latest", pattern);
        }
        catch (Exception ex)
        {
            ApplyLog.Write("GitHub 页面解析失败 " + repo + "：" + ex.Message);
            return "";
        }
    }

    private static string TryResolveFromHtmlPage(string pageUrl, string? pattern)
    {
        if (!Uri.TryCreate(pageUrl, UriKind.Absolute, out var baseUri)) return "";
        var html = FetchText(pageUrl);
        var urls = new List<string>();
        foreach (Match m in Href.Matches(html))
        {
            var href = WebUtility.HtmlDecode(m.Groups[1].Value.Trim());
            if (href.Length == 0 || href.StartsWith("#", StringComparison.Ordinal)) continue;
            if (href.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase)) continue;
            if (!Uri.TryCreate(baseUri, href, out var abs)) continue;
            if (abs.Scheme != Uri.UriSchemeHttp && abs.Scheme != Uri.UriSchemeHttps) continue;
            urls.Add(abs.AbsoluteUri);
        }

        return PickBest(urls, pattern, baseUri.Host);
    }

    private static string TryResolveFromJsonApi(string apiUrl, string? pattern)
    {
        var json = FetchText(apiUrl, accept: "application/json,text/plain,*/*");
        if (string.IsNullOrWhiteSpace(json)) return "";

        var byType = TryExtractByClientType(json, pattern);
        if (byType.Length > 0) return byType;

        var urls = new List<string>();
        foreach (Match m in JsonDownloadField.Matches(json))
        {
            var u = NormalizeJsonUrl(UnescapeJson(m.Groups[1].Value));
            if (u.Length > 0) urls.Add(u);
        }

        foreach (Match m in Regex.Matches(json, "https?://[^\"\\s\\\\]+"))
        {
            var u = NormalizeJsonUrl(UnescapeJson(m.Value.TrimEnd('\\')));
            if (u.Length > 0) urls.Add(u);
        }

        var host = "";
        if (Uri.TryCreate(apiUrl, UriKind.Absolute, out var apiUri))
            host = apiUri.Host;
        return PickBest(urls, pattern, host);
    }

    /// <summary>
    /// 从 microsoft/winget-pkgs 清单解析最新安装包直链（QQ 等 SPA 官网页解析不到时用）。
    /// PackageIdentifier 如 Tencent.QQ.NT → manifests/t/Tencent/QQ/NT
    /// </summary>
    private static string TryResolveFromWingetPkgs(string wingetId, string? pattern)
    {
        var dir = WingetPkgsManifestDir(wingetId);
        if (dir.Length == 0) return "";

        var api = "https://api.github.com/repos/microsoft/winget-pkgs/contents/" + dir;
        var listing = FetchText(api, accept: "application/vnd.github+json");
        if (string.IsNullOrWhiteSpace(listing)) return "";

        var versions = new List<string>();
        foreach (Match m in Regex.Matches(listing, "\"name\"\\s*:\\s*\"([^\"]+)\""))
        {
            var name = m.Groups[1].Value;
            if (name.Length == 0) continue;
            // 只要版本目录（数字开头），跳过子包名如 NT
            if (name[0] < '0' || name[0] > '9') continue;
            versions.Add(name);
        }

        if (versions.Count == 0) return "";
        versions.Sort(CompareWingetVersionDesc);
        var latest = versions[0];

        var versionApi = api + "/" + Uri.EscapeDataString(latest);
        var filesJson = FetchText(versionApi, accept: "application/vnd.github+json");
        if (string.IsNullOrWhiteSpace(filesJson)) return "";

        string? installerYamlUrl = null;
        foreach (Match m in Regex.Matches(
                     filesJson,
                     "\"name\"\\s*:\\s*\"([^\"]+\\.installer\\.yaml)\"[\\s\\S]{0,400}?\"download_url\"\\s*:\\s*\"([^\"]+)\""))
        {
            installerYamlUrl = UnescapeJson(m.Groups[2].Value);
            break;
        }

        if (string.IsNullOrWhiteSpace(installerYamlUrl))
        {
            // 宽松回退：任意 download_url 指向 installer.yaml
            foreach (Match m in Regex.Matches(filesJson, "\"download_url\"\\s*:\\s*\"(https:[^\"]+installer\\.yaml)\""))
            {
                installerYamlUrl = UnescapeJson(m.Groups[1].Value);
                break;
            }
        }

        if (string.IsNullOrWhiteSpace(installerYamlUrl)) return "";
        var yaml = FetchText(installerYamlUrl!);
        if (string.IsNullOrWhiteSpace(yaml)) return "";

        var urls = new List<string>();
        foreach (Match m in Regex.Matches(yaml, @"InstallerUrl:\s*(\S+)"))
        {
            var u = m.Groups[1].Value.Trim().Trim('"', '\'');
            if (u.Length > 0) urls.Add(u);
        }

        var prefer = (pattern ?? "").Trim();
        if (prefer.Length == 0)
            prefer = @"x64|win64|amd64";
        var picked = PickBest(urls, prefer, wingetId);
        if (picked.Length > 0)
        {
            ApplyLog.Write("winget-pkgs 最新包：" + wingetId + " @ " + latest + " → " + picked);
            return picked;
        }

        // 没有匹配时：优先带 x64 的
        foreach (var u in urls)
        {
            if (u.IndexOf("x64", StringComparison.OrdinalIgnoreCase) >= 0
                || u.IndexOf("win64", StringComparison.OrdinalIgnoreCase) >= 0)
                return u;
        }

        return urls.Count > 0 ? urls[0] : "";
    }

    private static string WingetPkgsManifestDir(string wingetId)
    {
        var parts = wingetId.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return "";
        var publisher = parts[0];
        if (publisher.Length == 0) return "";
        var rest = string.Join("/", parts, 1, parts.Length - 1);
        return "manifests/" + char.ToLowerInvariant(publisher[0]) + "/" + publisher + "/" + rest;
    }

    private static int CompareWingetVersionDesc(string a, string b)
    {
        if (Version.TryParse(NormalizeVersion(a), out var va) && Version.TryParse(NormalizeVersion(b), out var vb))
            return vb.CompareTo(va);
        return string.Compare(b, a, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeVersion(string raw)
    {
        var parts = raw.Split('.');
        if (parts.Length <= 4) return raw;
        return string.Join(".", parts[0], parts[1], parts[2], parts[3]);
    }

    private static string TryExtractByClientType(string json, string? pattern)
    {
        var text = (pattern ?? "").Trim();
        if (text.Length == 0) return "";
        foreach (var raw in text.Split('|'))
        {
            var token = raw.Trim();
            if (!IsSimpleApiToken(token)) continue;
            Regex rx;
            try
            {
                rx = new Regex(
                    "\"clientType\"\\s*:\\s*\"" + Regex.Escape(token) + "\"[\\s\\S]{0,500}?\"downloadUrl\"\\s*:\\s*\"([^\"]+)\"",
                    RegexOptions.IgnoreCase);
            }
            catch
            {
                continue;
            }

            var m = rx.Match(json);
            if (!m.Success) continue;
            var u = NormalizeJsonUrl(UnescapeJson(m.Groups[1].Value));
            if (u.Length > 0) return u;
        }

        return "";
    }

    private static bool IsSimpleApiToken(string s)
    {
        if (s.Length == 0 || s.Length > 40) return false;
        foreach (var c in s)
        {
            if (!(char.IsLetterOrDigit(c) || c == '_' || c == '-'))
                return false;
        }
        return true;
    }

    private static string NormalizeJsonUrl(string raw)
    {
        var text = (raw ?? "").Trim();
        if (text.Length == 0) return "";
        if (text.StartsWith("//", StringComparison.Ordinal))
            text = "https:" + text;
        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri)) return "";
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) return "";
        return uri.AbsoluteUri;
    }

    private static string PickBest(IReadOnlyList<string> urls, string? pattern, string hint)
    {
        string best = "";
        var bestScore = int.MinValue;
        Regex? rx = null;
        if (!string.IsNullOrWhiteSpace(pattern))
        {
            try { rx = new Regex(pattern, RegexOptions.IgnoreCase); }
            catch { rx = null; }
        }

        foreach (var url in urls)
        {
            var score = Score(url, rx, hint);
            if (score <= 0) continue;
            if (score > bestScore)
            {
                bestScore = score;
                best = url;
            }
        }

        return best;
    }

    private static int Score(string url, Regex? pattern, string hint)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return 0;
        var path = uri.AbsolutePath;
        var file = Path.GetFileName(path);
        var blob = url + " " + file;

        if (LooksLikeNonWindows(blob)) return 0;

        var score = 0;
        if (HasInstallerExtension(path)) score += 20;
        else if (path.IndexOf("/latest/download/", StringComparison.OrdinalIgnoreCase) >= 0) score += 12;
        else if (LooksLikeOpaqueInstallerDownload(uri)) score += 18;
        else return 0;

        if (pattern is not null && pattern.IsMatch(file + " " + url))
            score += 40;
        if (LooksLike64Bit(blob)) score += 10;
        if (LooksLikeSetup(blob)) score += 6;
        if (file.EndsWith(".msi", StringComparison.OrdinalIgnoreCase)) score += 2;
        if (file.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) score -= 8;
        var hintIsHost = hint.IndexOf('.') >= 0 && hint.IndexOf('/') < 0;
        if (hintIsHost)
        {
            var hintHost = hint.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
                ? hint.Substring(4) : hint;
            if (uri.Host.IndexOf(hintHost, StringComparison.OrdinalIgnoreCase) >= 0)
                score += 16;
            else
                score -= 8;
        }
        else if (hint.Length > 0 &&
            uri.Host.IndexOf(hint.Replace("www.", ""), StringComparison.OrdinalIgnoreCase) >= 0)
        {
            score += 3;
        }
        if (uri.Host.IndexOf("github.com", StringComparison.OrdinalIgnoreCase) >= 0 &&
            path.IndexOf("/releases/download/", StringComparison.OrdinalIgnoreCase) >= 0)
            score += 8;
        return score;
    }

    internal static bool HasInstallerExtension(string path)
    {
        var p = (path ?? "").ToLowerInvariant();
        return p.EndsWith(".exe") || p.EndsWith(".msi") || p.EndsWith(".msix")
            || p.EndsWith(".appx") || p.EndsWith(".msixbundle") || p.EndsWith(".appxbundle")
            || p.EndsWith(".zip");
    }

    /// <summary>无 .exe 后缀的官方下载接口（如天翼 downloadFile.action）。</summary>
    public static bool IsOpaqueInstallerDownloadUrl(string? url)
    {
        if (!Uri.TryCreate((url ?? "").Trim(), UriKind.Absolute, out var uri)) return false;
        return LooksLikeOpaqueInstallerDownload(uri);
    }

    private static bool LooksLikeOpaqueInstallerDownload(Uri uri)
    {
        var path = uri.AbsolutePath;
        if (path.IndexOf("downloadFile.action", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        // 火绒个人版：downloadHr60.php?pro=hr60 → 301 到版本化 exe
        if (path.IndexOf("downloadHr", StringComparison.OrdinalIgnoreCase) >= 0
            && uri.Host.IndexOf("huorong.cn", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        if (uri.Host.IndexOf("download.cloud.189.cn", StringComparison.OrdinalIgnoreCase) >= 0
            && uri.Query.IndexOf("sig=", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        return false;
    }

    private static bool LooksLike64Bit(string blob) =>
        blob.IndexOf("x64", StringComparison.OrdinalIgnoreCase) >= 0
        || blob.IndexOf("win64", StringComparison.OrdinalIgnoreCase) >= 0
        || blob.IndexOf("64-bit", StringComparison.OrdinalIgnoreCase) >= 0
        || blob.IndexOf("64bit", StringComparison.OrdinalIgnoreCase) >= 0
        || blob.IndexOf("x86_64", StringComparison.OrdinalIgnoreCase) >= 0;

    private static bool LooksLikeSetup(string blob) =>
        blob.IndexOf("setup", StringComparison.OrdinalIgnoreCase) >= 0
        || blob.IndexOf("installer", StringComparison.OrdinalIgnoreCase) >= 0
        || blob.IndexOf("standalone", StringComparison.OrdinalIgnoreCase) >= 0
        || blob.IndexOf("enterprise", StringComparison.OrdinalIgnoreCase) >= 0;

    private static bool LooksLikeNonWindows(string blob)
    {
        if (blob.IndexOf(".dmg", StringComparison.OrdinalIgnoreCase) >= 0) return true;
        if (blob.IndexOf(".apk", StringComparison.OrdinalIgnoreCase) >= 0) return true;
        if (blob.IndexOf("macOS", StringComparison.OrdinalIgnoreCase) >= 0) return true;
        if (blob.IndexOf("/mac/", StringComparison.OrdinalIgnoreCase) >= 0) return true;
        if (blob.IndexOf("linux", StringComparison.OrdinalIgnoreCase) >= 0) return true;
        if (blob.IndexOf(".deb", StringComparison.OrdinalIgnoreCase) >= 0) return true;
        if (blob.IndexOf(".rpm", StringComparison.OrdinalIgnoreCase) >= 0) return true;
        if (blob.IndexOf("arm64", StringComparison.OrdinalIgnoreCase) >= 0
            && blob.IndexOf("x64", StringComparison.OrdinalIgnoreCase) < 0)
            return true;
        return false;
    }

    private static string FetchText(string url, string? accept = null)
    {
        ConfigureHttps();
        var handler = new System.Net.Http.HttpClientHandler
        {
            AllowAutoRedirect = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            UseProxy = true,
            Proxy = WebRequest.GetSystemWebProxy(),
            UseDefaultCredentials = true,
        };
        try { handler.Proxy!.Credentials = CredentialCache.DefaultCredentials; }
        catch { /* ignore */ }

        using var client = new System.Net.Http.HttpClient(handler) { Timeout = TimeSpan.FromSeconds(40) };
        client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", BrowserUa);
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", accept ?? "text/html,application/json,*/*");
        if (Uri.TryCreate(url, UriKind.Absolute, out var refererUri)
            && (refererUri.Host.IndexOf("cloud.189.cn", StringComparison.OrdinalIgnoreCase) >= 0
                || refererUri.Host.IndexOf("189.cn", StringComparison.OrdinalIgnoreCase) >= 0))
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation("Referer", "https://cloud.189.cn/");
        }

        var text = client.GetStringAsync(url).GetAwaiter().GetResult();
        return text ?? "";
    }

    private static void ConfigureHttps()
    {
        try
        {
            ServicePointManager.SecurityProtocol |=
                SecurityProtocolType.Tls12 | (SecurityProtocolType)3072;
        }
        catch
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }
    }

    private static string UnescapeJson(string s) =>
        s.Replace("\\u0026", "&").Replace("\\/", "/").Replace("\\\"", "\"");
}
