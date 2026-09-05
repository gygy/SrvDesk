using System.Globalization;

namespace WinOpt;

/// <summary>
/// 日志分两类：
/// - 操作日志 apply.log：启动、打开工具、导入导出等一般事件
/// - 变更日志 变更日志.log：用户优化时真正改动的值（原值 → 新值、注册表路径）
/// 目录：%LocalAppData%\WinOpt\
/// </summary>
internal static class ApplyLog
{
    [ThreadStatic]
    private static string? _context;

    private static string LogDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinOpt");

    private static string OpsLogPath => Path.Combine(LogDir, "apply.log");

    /// <summary>用户优化变更专用日志文件名。</summary>
    private static string ChangeLogPath => Path.Combine(LogDir, "变更日志.log");

    public static string LogFilePath => OpsLogPath;
    public static string ChangeLogFilePath => ChangeLogPath;
    public static string? CurrentContext => _context;

    public static IDisposable PushContext(string itemName)
    {
        var previous = _context;
        _context = itemName;
        return new ContextScope(previous);
    }

    /// <summary>一般操作日志（非优化变更）。</summary>
    public static void Write(string message)
    {
        Append(OpsLogPath, FormatLine(message));
    }

    public static void WriteApply(string action, IReadOnlyList<string> errors)
    {
        var summary = errors.Count == 0
            ? $"{action} 结束（成功）"
            : $"{action} 结束（部分失败：{string.Join("; ", errors)}）";
        Write("──── " + summary + " ────");
        WriteChange("──── " + summary + " ────");
    }

    public static void BeginBatch(string action)
    {
        Write("════ " + action + " 开始 ════");
        WriteChange("════ " + action + " 开始 ════");
    }

    /// <summary>写入变更日志（优化改动明细）。</summary>
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
            WriteChange($"【未变更】{ItemLabel()} {path}\\{valueName} 仍为 {oldText}");
            return;
        }

        WriteChange(
            $"【注册表变更】DWORD\r\n" +
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
            WriteChange($"【未变更】{ItemLabel()} {path}\\{valueName} 仍为 {oldText}");
            return;
        }

        WriteChange(
            $"【注册表变更】字符串\r\n" +
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
            WriteChange($"【未变更】{ItemLabel()} 删除 {path}\\{valueName}（值本来就不存在）");
            return;
        }

        WriteChange(
            $"【注册表变更】删除值\r\n" +
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
            WriteChange($"【未变更】{ItemLabel()} 删除键 {path}（键本来就不存在）");
            return;
        }

        WriteChange(
            $"【注册表变更】删除键\r\n" +
            $"    优化项：{ItemLabel()}\r\n" +
            $"    注册表位置：{path}\r\n" +
            $"    变更：原来从 （键存在） 变成 （整键已删除）");
    }

    public static void RegistryKeyWrite(string hive, string key, string detail)
    {
        WriteChange(
            $"【注册表变更】写入键\r\n" +
            $"    优化项：{ItemLabel()}\r\n" +
            $"    注册表位置：{FormatRegPath(hive, key)}\r\n" +
            $"    变更详情：{detail}");
    }

    public static void ServiceChange(string serviceName, string detail, string? oldStart = null, string? newStart = null)
    {
        var reg = $@"HKLM\SYSTEM\CurrentControlSet\Services\{serviceName}\Start";
        if (oldStart is not null && newStart is not null)
        {
            if (string.Equals(oldStart, newStart, StringComparison.Ordinal))
            {
                WriteChange($"【未变更】{ItemLabel()} 服务 {serviceName} 启动类型仍为 {oldStart}");
                return;
            }

            WriteChange(
                $"【服务变更】\r\n" +
                $"    优化项：{ItemLabel()}\r\n" +
                $"    服务名：{serviceName}\r\n" +
                $"    注册表位置：{reg}\r\n" +
                $"    变更：原来从 {oldStart} 变成 {newStart}\r\n" +
                $"    操作：{detail}");
        }
        else
        {
            WriteChange(
                $"【服务变更】\r\n" +
                $"    优化项：{ItemLabel()}\r\n" +
                $"    服务名：{serviceName}\r\n" +
                $"    注册表位置：HKLM\\SYSTEM\\CurrentControlSet\\Services\\{serviceName}\r\n" +
                $"    操作：{detail}");
        }
    }

    public static void SystemChange(string target, string detail, string? oldValue = null, string? newValue = null)
    {
        if (oldValue is not null || newValue is not null)
        {
            var from = oldValue ?? "（未知）";
            var to = newValue ?? "（未知）";
            if (string.Equals(from, to, StringComparison.Ordinal))
            {
                WriteChange($"【未变更】{ItemLabel()} {target} 仍为 {from}");
                return;
            }

            WriteChange(
                $"【系统设置变更】\r\n" +
                $"    优化项：{ItemLabel()}\r\n" +
                $"    修改位置：{target}\r\n" +
                $"    变更：原来从 {from} 变成 {to}\r\n" +
                $"    详情：{detail}");
        }
        else
        {
            WriteChange(
                $"【系统设置变更】\r\n" +
                $"    优化项：{ItemLabel()}\r\n" +
                $"    修改位置：{target}\r\n" +
                $"    详情：{detail}");
        }
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

    private static string FormatDword(int value) => $"{value} (0x{value:X8})";

    private static string ItemLabel() =>
        string.IsNullOrWhiteSpace(_context) ? "（未指定优化项）" : _context!;

    private static string FormatLine(string message)
    {
        var prefix = string.IsNullOrEmpty(_context) ? "" : $"[{_context}] ";
        return $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {prefix}{message}{Environment.NewLine}";
    }

    private static void Append(string path, string line)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.AppendAllText(path, line);
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
