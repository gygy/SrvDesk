using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace WinOpt;

internal sealed class HostsEntry
{
    public bool Enabled { get; set; } = true;
    public string Address { get; set; } = "";
    public string Hosts { get; set; } = "";
    public string Comment { get; set; } = "";
}

internal sealed class HostsDocument
{
    public string Header { get; set; } = "";
    public string Footer { get; set; } = "";
    public List<HostsEntry> Entries { get; } = [];
}

internal sealed class HostsPasteResult
{
    public List<HostsEntry> Entries { get; } = [];
    public int SkippedLines { get; set; }
}

internal static class HostsFileHelper
{
    private static readonly Regex MappingLine = new(
        @"^\s*(#)?\s*((?:\d{1,3}\.){3}\d{1,3}|[0-9a-fA-F:]+)\s+([^#]+?)(?:\s*#\s*(.*))?$",
        RegexOptions.Compiled);

    public static string FilePath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"drivers\etc\hosts");

    public static string BackupDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinOpt", "hosts-backup");

    public static HostsDocument Read()
    {
        var path = FilePath;
        if (!File.Exists(path))
            throw new FileNotFoundException("找不到 hosts 文件。", path);

        var lines = File.ReadAllLines(path, DetectEncoding(path));
        var doc = new HostsDocument();
        var header = new StringBuilder();
        var footer = new StringBuilder();
        var seenEntry = false;

        foreach (var raw in lines)
        {
            if (TryParseEntry(raw, out var entry))
            {
                seenEntry = true;
                doc.Entries.Add(entry);
            }
            else if (!seenEntry)
            {
                header.AppendLine(raw);
            }
            else
            {
                footer.AppendLine(raw);
            }
        }

        doc.Header = header.ToString().TrimEnd();
        doc.Footer = footer.ToString().TrimEnd();
        return doc;
    }

    public static void Save(HostsDocument doc, bool backup, bool flushDns)
    {
        var path = FilePath;
        if (backup)
            Backup(path);

        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(doc.Header))
        {
            sb.AppendLine(doc.Header.TrimEnd());
            sb.AppendLine();
        }

        foreach (var e in doc.Entries)
        {
            var address = (e.Address ?? "").Trim();
            var hosts = NormalizeHosts(e.Hosts);
            if (address.Length == 0 || hosts.Length == 0) continue;

            var line = $"{address}\t{hosts}";
            if (!string.IsNullOrWhiteSpace(e.Comment))
                line += "\t# " + e.Comment.Trim();
            if (!e.Enabled)
                line = "# " + line;
            sb.AppendLine(line);
        }

        if (!string.IsNullOrWhiteSpace(doc.Footer))
        {
            sb.AppendLine();
            sb.AppendLine(doc.Footer.TrimEnd());
        }

        var encoding = DetectEncoding(path);
        try
        {
            var attr = File.GetAttributes(path);
            if ((attr & FileAttributes.ReadOnly) != 0)
                File.SetAttributes(path, attr & ~FileAttributes.ReadOnly);
        }
        catch { /* 无权限时由后续写入抛出 */ }

        var temp = path + ".winopt.tmp";
        File.WriteAllText(temp, sb.ToString(), encoding);
        File.Copy(temp, path, overwrite: true);
        TryDelete(temp);

        ApplyLog.Write("已保存 hosts：" + path);
        if (flushDns)
            FlushDns();
    }

    public static string Backup(string? sourcePath = null)
    {
        var path = sourcePath ?? FilePath;
        Directory.CreateDirectory(BackupDir);
        var dest = Path.Combine(BackupDir, $"hosts.{DateTime.Now:yyyyMMdd-HHmmss}.bak");
        File.Copy(path, dest, overwrite: false);
        ApplyLog.Write("已备份 hosts → " + dest);
        return dest;
    }

    public static bool FlushDns()
    {
        using var p = Process.Start(new ProcessStartInfo
        {
            FileName = "ipconfig.exe",
            Arguments = "/flushdns",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        }) ?? throw new InvalidOperationException("无法启动 ipconfig.exe");
        p.WaitForExit(15_000);
        ApplyLog.Write(p.ExitCode == 0 ? "已刷新 DNS 缓存" : "刷新 DNS 缓存失败，exit=" + p.ExitCode);
        return p.ExitCode == 0;
    }

    public static string Validate(HostsEntry entry)
    {
        var address = (entry.Address ?? "").Trim();
        var hosts = NormalizeHosts(entry.Hosts);
        if (address.Length == 0) return "IP 地址不能为空。";
        if (!IPAddress.TryParse(address, out _)) return "IP 地址格式无效：" + address;
        if (hosts.Length == 0) return "主机名不能为空。";
        foreach (var name in hosts.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
        {
            if (!IsValidHostName(name))
                return "主机名无效：" + name;
        }
        return "";
    }

    /// <summary>解析外部复制的 hosts 文本（多行、含注释行会自动跳过）。</summary>
    public static HostsPasteResult ParseText(string text)
    {
        var result = new HostsPasteResult();
        if (string.IsNullOrWhiteSpace(text))
            return result;

        foreach (var raw in text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var line = raw.Trim();
            if (line.Length == 0)
            {
                result.SkippedLines++;
                continue;
            }

            if (TryParseEntry(raw, out var entry) || TryParseTabSeparated(raw, out entry))
            {
                result.Entries.Add(entry);
                continue;
            }

            result.SkippedLines++;
        }

        return result;
    }

    private static bool TryParseTabSeparated(string raw, out HostsEntry entry)
    {
        entry = new HostsEntry();
        if (raw.IndexOf('\t') < 0) return false;

        var parts = raw.Split('\t');
        if (parts.Length < 2) return false;

        var start = 0;
        var enabled = true;
        if (parts.Length >= 3 && TryParseEnabled(parts[0], out enabled))
            start = 1;

        if (parts.Length - start < 2) return false;

        var address = parts[start].Trim();
        var hosts = NormalizeHosts(parts[start + 1]);
        var comment = parts.Length > start + 2 ? parts[start + 2].Trim() : "";

        if (!IPAddress.TryParse(address, out _) || hosts.Length == 0)
            return false;

        entry.Enabled = enabled;
        entry.Address = address;
        entry.Hosts = hosts;
        entry.Comment = comment.TrimStart('#').Trim();
        return true;
    }

    private static bool TryParseEnabled(string text, out bool enabled)
    {
        enabled = true;
        var t = text.Trim();
        if (t.Length == 0) return false;
        if (bool.TryParse(t, out enabled)) return true;
        if (t == "1" || t.Equals("yes", StringComparison.OrdinalIgnoreCase)
            || t.Equals("y", StringComparison.OrdinalIgnoreCase)
            || t == "是" || t == "启用") { enabled = true; return true; }
        if (t == "0" || t.Equals("no", StringComparison.OrdinalIgnoreCase)
            || t.Equals("n", StringComparison.OrdinalIgnoreCase)
            || t == "否" || t == "禁用") { enabled = false; return true; }
        return false;
    }

    private static bool TryParseEntry(string raw, out HostsEntry entry)
    {
        entry = new HostsEntry();
        var line = raw.Trim();
        if (line.Length == 0) return false;

        var m = MappingLine.Match(line);
        if (!m.Success) return false;

        var address = m.Groups[2].Value.Trim();
        if (!IPAddress.TryParse(address, out _)) return false;

        entry.Enabled = m.Groups[1].Value != "#";
        entry.Address = address;
        entry.Hosts = NormalizeHosts(m.Groups[3].Value);
        entry.Comment = m.Groups[4].Success ? m.Groups[4].Value.Trim() : "";
        return entry.Hosts.Length > 0;
    }

    private static string NormalizeHosts(string? hosts) =>
        string.Join(" ", (hosts ?? "").Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries));

    private static bool IsValidHostName(string name)
    {
        if (name.Length is 0 or > 253) return false;
        if (name.Equals("localhost", StringComparison.OrdinalIgnoreCase)) return true;
        return Regex.IsMatch(name, @"^[A-Za-z0-9](?:[A-Za-z0-9._-]*[A-Za-z0-9])?$");
    }

    private static Encoding DetectEncoding(string path)
    {
        try
        {
            var bytes = File.ReadAllBytes(path);
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
                return Encoding.Unicode;
        }
        catch { /* 按系统默认读取 */ }
        return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); } catch { /* ignore */ }
    }
}
