using System.Globalization;

namespace WinOpt;

/// <summary>
/// 操作日志：记录每次修改的项名、旧值→新值、注册表完整路径与类型。
/// 文件：%LocalAppData%\WinOpt\apply.log
/// </summary>
internal static class ApplyLog
{
    [ThreadStatic]
    private static string? _context;

    private static string LogPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WinOpt", "apply.log");

    public static string LogFilePath => LogPath;

    public static string? CurrentContext => _context;

    /// <summary>为当前线程设置「修改项」上下文（如「禁用 UAC」），写入日志时自动带上。</summary>
    public static IDisposable PushContext(string itemName)
    {
        var previous = _context;
        _context = itemName;
        return new ContextScope(previous);
    }

    public static void Write(string message)
    {
        try
        {
            var dir = Path.GetDirectoryName(LogPath)!;
            Directory.CreateDirectory(dir);
            var prefix = string.IsNullOrEmpty(_context) ? "" : $"[{_context}] ";
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {prefix}{message}{Environment.NewLine}";
            File.AppendAllText(LogPath, line);
        }
        catch { /* ignore */ }
    }

    public static void WriteApply(string action, IReadOnlyList<string> errors)
    {
        Write("──── " + (errors.Count == 0
            ? $"{action} 结束（成功）"
            : $"{action} 结束（部分失败：{string.Join("; ", errors)}）") + " ────");
    }

    public static void BeginBatch(string action)
    {
        Write("════ " + action + " 开始 ════");
    }

    /// <summary>注册表 DWORD：记录路径、值名、旧值→新值。</summary>
    public static void RegistryDword(string hive, string key, string valueName, object? oldValue, int newValue)
    {
        var path = FormatRegPath(hive, key);
        if (SameValue(oldValue, newValue))
        {
            Write($"跳过（未变化） DWORD  {path}\\{valueName} = {FormatValue(oldValue)}");
            return;
        }

        Write(
            $"修改注册表 DWORD\r\n" +
            $"    修改项：{_context ?? "（未指定）"}\r\n" +
            $"    位置：{path}\r\n" +
            $"    值名：{valueName}\r\n" +
            $"    类型：REG_DWORD\r\n" +
            $"    原值：{FormatValue(oldValue)}\r\n" +
            $"    新值：{newValue} (0x{newValue:X8})");
    }

    /// <summary>注册表字符串。</summary>
    public static void RegistryString(string hive, string key, string valueName, object? oldValue, string newValue, bool maskSecret = false)
    {
        var path = FormatRegPath(hive, key);
        var displayNew = maskSecret ? Mask(newValue) : Quote(newValue);
        var displayOld = maskSecret ? Mask(oldValue?.ToString()) : FormatValue(oldValue);
        if (!maskSecret && SameValue(oldValue, newValue))
        {
            Write($"跳过（未变化） SZ  {path}\\{valueName} = {displayOld}");
            return;
        }

        Write(
            $"修改注册表 字符串\r\n" +
            $"    修改项：{_context ?? "（未指定）"}\r\n" +
            $"    位置：{path}\r\n" +
            $"    值名：{valueName}\r\n" +
            $"    类型：REG_SZ\r\n" +
            $"    原值：{displayOld}\r\n" +
            $"    新值：{displayNew}");
    }

    /// <summary>删除注册表值。</summary>
    public static void RegistryDelete(string hive, string key, string valueName, object? oldValue)
    {
        var path = FormatRegPath(hive, key);
        if (oldValue is null)
        {
            Write($"跳过（值不存在）删除  {path}\\{valueName}");
            return;
        }

        Write(
            $"删除注册表值\r\n" +
            $"    修改项：{_context ?? "（未指定）"}\r\n" +
            $"    位置：{path}\r\n" +
            $"    值名：{valueName}\r\n" +
            $"    原值：{FormatValue(oldValue)}\r\n" +
            $"    新值：（已删除）");
    }

    /// <summary>删除注册表整键。</summary>
    public static void RegistryDeleteTree(string hive, string key, bool existed)
    {
        var path = FormatRegPath(hive, key);
        if (!existed)
        {
            Write($"跳过（键不存在）删除键  {path}");
            return;
        }

        Write(
            $"删除注册表键\r\n" +
            $"    修改项：{_context ?? "（未指定）"}\r\n" +
            $"    位置：{path}\r\n" +
            $"    操作：DeleteSubKeyTree");
    }

    /// <summary>创建/设置默认值的壳扩展键等。</summary>
    public static void RegistryKeyWrite(string hive, string key, string detail)
    {
        Write(
            $"写入注册表键\r\n" +
            $"    修改项：{_context ?? "（未指定）"}\r\n" +
            $"    位置：{FormatRegPath(hive, key)}\r\n" +
            $"    详情：{detail}");
    }

    /// <summary>Windows 服务启停 / 启动类型。</summary>
    public static void ServiceChange(string serviceName, string detail, string? oldStart = null, string? newStart = null)
    {
        if (oldStart is not null && newStart is not null)
        {
            Write(
                $"修改系统服务\r\n" +
                $"    修改项：{_context ?? "（未指定）"}\r\n" +
                $"    服务名：{serviceName}\r\n" +
                $"    注册表：HKLM\\SYSTEM\\CurrentControlSet\\Services\\{serviceName}\\Start\r\n" +
                $"    原启动类型：{oldStart}\r\n" +
                $"    新启动类型：{newStart}\r\n" +
                $"    操作：{detail}");
        }
        else
        {
            Write(
                $"修改系统服务\r\n" +
                $"    修改项：{_context ?? "（未指定）"}\r\n" +
                $"    服务名：{serviceName}\r\n" +
                $"    注册表：HKLM\\SYSTEM\\CurrentControlSet\\Services\\{serviceName}\r\n" +
                $"    操作：{detail}");
        }
    }

    /// <summary>命令行 / DISM / 其它系统修改。</summary>
    public static void SystemChange(string target, string detail, string? oldValue = null, string? newValue = null)
    {
        if (oldValue is not null || newValue is not null)
        {
            Write(
                $"修改系统设置\r\n" +
                $"    修改项：{_context ?? "（未指定）"}\r\n" +
                $"    目标：{target}\r\n" +
                $"    原值：{oldValue ?? "（未知）"}\r\n" +
                $"    新值：{newValue ?? "（未知）"}\r\n" +
                $"    详情：{detail}");
        }
        else
        {
            Write(
                $"修改系统设置\r\n" +
                $"    修改项：{_context ?? "（未指定）"}\r\n" +
                $"    目标：{target}\r\n" +
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
            int i => $"{i} (0x{i:X8})",
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
        0 => "0 Boot",
        1 => "1 System",
        2 => "2 Automatic",
        3 => "3 Manual",
        4 => "4 Disabled",
        null => "（不存在）",
        _ => start.Value.ToString(CultureInfo.InvariantCulture),
    };

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
