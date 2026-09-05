using System.Globalization;
using System.Text;

namespace WinOpt;

/// <summary>
/// 日志分两类：
/// - 操作日志 apply.log：启动、打开工具等一般事件
/// - 变更日志 变更日志.log：仅记录真正改动的值（原来从 xx 变成 yy）
/// 目录：%LocalAppData%\WinOpt\
/// </summary>
internal static class ApplyLog
{
    private static readonly Encoding Utf8Bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

    [ThreadStatic]
    private static string? _context;

    [ThreadStatic]
    private static int _batchRealChanges;

    private static string LogDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinOpt");

    private static string OpsLogPath => Path.Combine(LogDir, "apply.log");
    private static string ChangeLogPath => Path.Combine(LogDir, "变更日志.log");

    public static string LogFilePath => OpsLogPath;
    public static string ChangeLogFilePath => ChangeLogPath;
    public static string? CurrentContext => _context;

    /// <summary>当前批次中「真正发生变更」的条数（不含未变化跳过）。</summary>
    public static int LastBatchRealChangeCount { get; private set; }

    public static IDisposable PushContext(string itemName)
    {
        var previous = _context;
        _context = itemName;
        return new ContextScope(previous);
    }

    public static void Write(string message)
    {
        Append(OpsLogPath, FormatLine(message));
    }

    public static void BeginBatch(string action)
    {
        _batchRealChanges = 0;
        Write("════ " + action + " 开始 ════");
        WriteChange("════ " + action + " 开始 ════");
    }

    public static void WriteApply(string action, IReadOnlyList<string> errors)
    {
        LastBatchRealChangeCount = _batchRealChanges;
        var summary = errors.Count == 0
            ? $"{action} 结束（成功），实际变更 {_batchRealChanges} 条"
            : $"{action} 结束（部分失败：{string.Join("; ", errors)}），实际变更 {_batchRealChanges} 条";
        Write("──── " + summary + " ────");
        WriteChange("──── " + summary + " ────");
        Write($"变更明细见：{ChangeLogPath}");
    }

    public static void WriteChange(string message)
    {
        Append(ChangeLogPath, FormatLine(message));
    }

    public static void RegistryDword(string hive, string key, string valueName, object? oldValue, int newValue)
    {
        var path = FormatRegPath(hive, key);
        var oldText = FormatValue(oldValue);
        var newText = FormatDword(newValue);
        if (SameValue(oldValue, newValue))
        {
            Write($"跳过未变：{ItemLabel()} {path}\\{valueName} = {oldText}");
            return;
        }

        _batchRealChanges++;
        WriteChange(
            $"【注册表变更】\r\n" +
            $"    优化项：{ItemLabel()}\r\n" +
            $"    注册表位置：{path}\r\n" +
            $"    值名称：{valueName}\r\n" +
            $"    值类型：REG_DWORD\r\n" +
            $"    变更：原来从 {oldText} 变成 {newText}");
    }

    public static void RegistryString(string hive, string key, string valueName, object? oldValue, string newValue, bool maskSecret = false)
    {
        var path = FormatRegPath(hive, key);
        var oldText = maskSecret ? Mask(oldValue?.ToString()) : FormatValue(oldValue);
        var newText = maskSecret ? Mask(newValue) : Quote(newValue);
        if (!maskSecret && SameValue(oldValue, newValue))
        {
            Write($"跳过未变：{ItemLabel()} {path}\\{valueName} = {oldText}");
            return;
        }

        _batchRealChanges++;
        WriteChange(
            $"【注册表变更】\r\n" +
            $"    优化项：{ItemLabel()}\r\n" +
            $"    注册表位置：{path}\r\n" +
            $"    值名称：{valueName}\r\n" +
            $"    值类型：REG_SZ\r\n" +
            $"    变更：原来从 {oldText} 变成 {newText}");
    }

    public static void RegistryDelete(string hive, string key, string valueName, object? oldValue)
    {
        var path = FormatRegPath(hive, key);
        if (oldValue is null)
        {
            Write($"跳过未变：{ItemLabel()} 删除 {path}\\{valueName}（本来就不存在）");
            return;
        }

        _batchRealChanges++;
        WriteChange(
            $"【注册表变更】\r\n" +
            $"    优化项：{ItemLabel()}\r\n" +
            $"    注册表位置：{path}\r\n" +
            $"    值名称：{valueName}\r\n" +
            $"    变更：原来从 {FormatValue(oldValue)} 变成 （已删除）");
    }

    public static void RegistryDeleteTree(string hive, string key, bool existed)
    {
        var path = FormatRegPath(hive, key);
        if (!existed)
        {
            Write($"跳过未变：{ItemLabel()} 删除键 {path}（本来就不存在）");
            return;
        }

        _batchRealChanges++;
        WriteChange(
            $"【注册表变更】\r\n" +
            $"    优化项：{ItemLabel()}\r\n" +
            $"    注册表位置：{path}\r\n" +
            $"    变更：原来从 （键存在） 变成 （整键已删除）");
    }

    public static void RegistryKeyWrite(string hive, string key, string detail)
    {
        _batchRealChanges++;
        WriteChange(
            $"【注册表变更】\r\n" +
            $"    优化项：{ItemLabel()}\r\n" +
            $"    注册表位置：{FormatRegPath(hive, key)}\r\n" +
            $"    变更：写入键 — {detail}");
    }

    public static void ServiceChange(string serviceName, string detail, string? oldStart = null, string? newStart = null)
    {
        var reg = $@"HKLM\SYSTEM\CurrentControlSet\Services\{serviceName}\Start";
        if (oldStart is not null && newStart is not null)
        {
            if (string.Equals(oldStart, newStart, StringComparison.Ordinal))
            {
                Write($"跳过未变：{ItemLabel()} 服务 {serviceName} = {oldStart}");
                return;
            }

            _batchRealChanges++;
            WriteChange(
                $"【服务变更】\r\n" +
                $"    优化项：{ItemLabel()}\r\n" +
                $"    服务名：{serviceName}\r\n" +
                $"    注册表位置：{reg}\r\n" +
                $"    变更：原来从 {oldStart} 变成 {newStart}\r\n" +
                $"    操作：{detail}");
            return;
        }

        _batchRealChanges++;
        WriteChange(
            $"【服务变更】\r\n" +
            $"    优化项：{ItemLabel()}\r\n" +
            $"    服务名：{serviceName}\r\n" +
            $"    注册表位置：HKLM\\SYSTEM\\CurrentControlSet\\Services\\{serviceName}\r\n" +
            $"    变更：{detail}");
    }

    public static void SystemChange(string target, string detail, string? oldValue = null, string? newValue = null)
    {
        if (oldValue is not null || newValue is not null)
        {
            var from = oldValue ?? "（未知）";
            var to = newValue ?? "（未知）";
            if (string.Equals(from, to, StringComparison.Ordinal))
            {
                Write($"跳过未变：{ItemLabel()} {target} = {from}");
                return;
            }

            _batchRealChanges++;
            WriteChange(
                $"【系统设置变更】\r\n" +
                $"    优化项：{ItemLabel()}\r\n" +
                $"    修改位置：{target}\r\n" +
                $"    变更：原来从 {from} 变成 {to}\r\n" +
                $"    详情：{detail}");
            return;
        }

        _batchRealChanges++;
        WriteChange(
            $"【系统设置变更】\r\n" +
            $"    优化项：{ItemLabel()}\r\n" +
            $"    修改位置：{target}\r\n" +
            $"    变更：{detail}");
    }

    public static string FormatRegPath(string hive, string key)
    {
        var h = hive.ToUpperInvariant() switch
        {
            "HKLM" or "HKEY_LOCAL_MACHINE" or "LOCALMACHINE" => "HKLM",
            "HKCU" or "HKEY_CURRENT_USER" or "CURRENTUSER" => "HKCU",
            "HKCR" or "HKEY_CLASSES_ROOT" or "CLASSESROOT" => "HKCR",
            _ => hive,
        };
        return $"{h}\\{key.TrimStart('\\')}";
    }

    public static string FormatValue(object? value)
    {
        if (value is null) return "（不存在）";
        return value switch
        {
            int i => FormatDword(i),
            uint u => $"{u} (0x{u:X8})",
            long l => l.ToString(CultureInfo.InvariantCulture),
            byte[] bytes => $"二进制[{bytes.Length}字节] {BitConverter.ToString(bytes, 0, Math.Min(16, bytes.Length))}" +
                            (bytes.Length > 16 ? "…" : ""),
            string s => Quote(s),
            _ => Quote(Convert.ToString(value, CultureInfo.InvariantCulture) ?? value.ToString() ?? ""),
        };
    }

    public static string StartTypeLabel(int? start) => start switch
    {
        0 => "0 Boot（引导）",
        1 => "1 System（系统）",
        2 => "2 Automatic（自动）",
        3 => "3 Manual（手动）",
        4 => "4 Disabled（禁用）",
        null => "（不存在）",
        _ => start.Value.ToString(CultureInfo.InvariantCulture),
    };

    public static bool HasRealChangeEntries()
    {
        try
        {
            if (!File.Exists(ChangeLogPath)) return false;
            var text = File.ReadAllText(ChangeLogPath, Utf8Bom);
            return text.IndexOf("【注册表变更】", StringComparison.Ordinal) >= 0
                || text.IndexOf("【服务变更】", StringComparison.Ordinal) >= 0
                || text.IndexOf("【系统设置变更】", StringComparison.Ordinal) >= 0;
        }
        catch
        {
            return false;
        }
    }

    private static string FormatDword(int value) => $"{value} (0x{value:X8})";

    private static string ItemLabel() =>
        string.IsNullOrWhiteSpace(_context) ? "（未指定优化项）" : _context!;

    private static string FormatLine(string message) =>
        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";

    private static void Append(string path, string line)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            if (!File.Exists(path))
            {
                var header = path.EndsWith("变更日志.log", StringComparison.OrdinalIgnoreCase)
                    ? "# 变更日志 — 仅记录优化时真正改动的值（原来从 xx 变成 yy）\r\n" +
                      "# 请先点击「应用推荐」或切换即时页开关后再查看。\r\n\r\n"
                    : "# 操作日志\r\n\r\n";
                File.WriteAllText(path, header, Utf8Bom);
            }
            File.AppendAllText(path, line, Utf8Bom);
        }
        catch { /* ignore */ }
    }

    private static string Quote(string s) => "\"" + s.Replace("\"", "\\\"") + "\"";

    private static string Mask(string? s) =>
        string.IsNullOrEmpty(s) ? "（空）" : "******（已隐藏）";

    private static bool SameValue(object? oldValue, object newValue)
    {
        if (oldValue is null) return false;
        if (oldValue is int oi && newValue is int ni) return oi == ni;
        if (oldValue is string os && newValue is string ns) return string.Equals(os, ns, StringComparison.Ordinal);
        return Equals(oldValue, newValue);
    }

    private sealed class ContextScope : IDisposable
    {
        private readonly string? _previous;
        public ContextScope(string? previous) => _previous = previous;
        public void Dispose() => _context = _previous;
    }
}
