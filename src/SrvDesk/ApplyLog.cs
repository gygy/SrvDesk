using System.Globalization;
using System.Text;

namespace SrvDesk;

/// <summary>
/// 日志分三类：
/// - 操作日志 apply.log：启动、打开工具等一般事件
/// - 变更日志 变更日志.log：仅记录真正改动的值（原来从 xx 变成 yy）
/// - 调试日志 debug.log：优化项/设置项差分、写入与跳过细节（程序设置中开启）
/// 目录：与 SrvDesk.exe 同目录（不可写时回退 %LocalAppData%\SrvDesk\）
/// </summary>
internal static class ApplyLog
{
    private static readonly Encoding Utf8Bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    [ThreadStatic]
    private static string? _context;

    [ThreadStatic]
    private static int _batchRealChanges;

    [ThreadStatic]
    private static bool _debugThisBatch;

    private static string LogDir => AppPaths.DataRoot;

    private static string OpsLogPath => Path.Combine(LogDir, "apply.log");
    private static string ChangeLogPath => Path.Combine(LogDir, "变更日志.log");
    private static string DebugLogPath => Path.Combine(LogDir, "debug.log");

    public static string LogFilePath => OpsLogPath;
    public static string ChangeLogFilePath => ChangeLogPath;
    public static string DebugLogFilePath => DebugLogPath;
    public static string LogDirectory => LogDir;
    public static string? CurrentContext => _context;
    public static int CurrentBatchRealChanges => _batchRealChanges;

    /// <summary>当前批次中「真正发生变更」的条数（不含未变化跳过）。</summary>
    public static int LastBatchRealChangeCount { get; private set; }

    public static bool IsDebugEnabled => _debugThisBatch || UiPrefs.EnableDebugLog;

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

    /// <summary>调试日志（需在「程序设置」开启）；同时写入操作日志时请另调 Write。</summary>
    public static void Debug(string message)
    {
        if (!IsDebugEnabled) return;
        var ctx = string.IsNullOrWhiteSpace(_context) ? "" : $"[{_context}] ";
        Append(DebugLogPath, FormatLine("DEBUG " + ctx + message));
    }

    /// <summary>优化项生命周期：跳过 / 开始 / 结束。</summary>
    public static void DebugItem(string item, string phase, string? detail = null)
    {
        if (!IsDebugEnabled) return;
        var extra = string.IsNullOrWhiteSpace(detail) ? "" : " · " + detail;
        Debug($"优化项「{item}」{phase}{extra}");
    }

    /// <summary>设置/字段级差分（组内单项写入前）。</summary>
    public static void DebugField(string field, object? from, object? to)
    {
        if (!IsDebugEnabled) return;
        Debug($"  字段 {field}：{FmtDebug(from)} → {FmtDebug(to)}");
    }

    /// <summary>列出相对基线有差异的 State 字段（应用到系统前）。</summary>
    public static void DebugStateDiff(string title, object? baseline, object target)
    {
        if (!IsDebugEnabled) return;
        Debug("════ " + title + " ════");
        if (baseline is null)
        {
            Debug("基线为空：将按「相对无基线」评估（可能写入较多项）");
            return;
        }

        var type = target.GetType();
        if (baseline.GetType() != type)
        {
            Debug("基线与目标类型不一致，跳过字段差分");
            return;
        }

        var n = 0;
        foreach (var f in type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
        {
            var a = f.GetValue(baseline);
            var b = f.GetValue(target);
            if (Equals(a, b)) continue;
            n++;
            Debug($"  · {f.Name}: {FmtDebug(a)} → {FmtDebug(b)}");
        }

        Debug($"相对基线共 {n} 个字段有差异");
    }

    /// <summary>环境不支持 / 无害退出等：记操作日志「跳过」，调试日志留细节，不抛给上层。</summary>
    public static void SoftSkip(string item, string reason)
    {
        Write($"跳过：{item} — {reason}");
        Debug("SoftSkip 「" + item + "」| " + reason);
    }

    public static void BeginBatch(string action)
    {
        _batchRealChanges = 0;
        _debugThisBatch = UiPrefs.EnableDebugLog;
        Write("════ " + action + " 开始 ════");
        WriteChange("════ " + action + " 开始 ════");
        if (_debugThisBatch)
        {
            Debug("════ 调试批次开始：" + action + " ════");
            Debug("调试日志将记录：优化项跳过/写入、注册表与服务细节、字段差分");
        }
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
        if (_debugThisBatch)
        {
            Debug("──── " + summary + " ────");
            if (errors.Count > 0)
            {
                foreach (var e in errors)
                    Debug("失败项：" + e);
            }

            Debug("变更明细：" + ChangeLogPath);
            Debug("════ 调试批次结束 ════");
        }

        _debugThisBatch = false;
    }

    public static void WriteChange(string message)
    {
        Append(ChangeLogPath, FormatLine(message));
    }

    public static void RegistryBinary(string hive, string key, string valueName, byte[]? oldValue, byte[] newValue, string? meaning = null)
    {
        var path = FormatRegPath(hive, key);
        var oldText = FormatValue(oldValue);
        var newText = FormatValue(newValue);
        if (oldValue is not null && oldValue.Length == newValue.Length && oldValue.SequenceEqual(newValue))
        {
            NoteSkip($"REG_BINARY {path}\\{valueName} = {oldText}");
            return;
        }

        _batchRealChanges++;
        var extra = string.IsNullOrWhiteSpace(meaning) ? "" : $"\r\n    含义：{meaning}";
        WriteChange(
            $"【注册表变更】\r\n" +
            $"    优化项：{ItemLabel()}\r\n" +
            $"    注册表位置：{path}\r\n" +
            $"    值名称：{valueName}\r\n" +
            $"    值类型：REG_BINARY\r\n" +
            $"    变更：原来从 {oldText} 变成 {newText}{extra}");
        NoteWrite($"REG_BINARY {path}\\{valueName}：{oldText} → {newText}");
    }

    public static void RegistryDword(string hive, string key, string valueName, object? oldValue, int newValue)
    {
        var path = FormatRegPath(hive, key);
        var oldText = FormatValue(oldValue);
        var newText = FormatDword(newValue);
        if (SameValue(oldValue, newValue))
        {
            NoteSkip($"REG_DWORD {path}\\{valueName} = {oldText}");
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
        NoteWrite($"REG_DWORD {path}\\{valueName}：{oldText} → {newText}");
    }

    public static void RegistryString(string hive, string key, string valueName, object? oldValue, string newValue, bool maskSecret = false)
    {
        var path = FormatRegPath(hive, key);
        var oldText = maskSecret ? Mask(oldValue?.ToString()) : FormatValue(oldValue);
        var newText = maskSecret ? Mask(newValue) : Quote(newValue);
        if (!maskSecret && SameValue(oldValue, newValue))
        {
            NoteSkip($"REG_SZ {path}\\{valueName} = {oldText}");
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
        NoteWrite($"REG_SZ {path}\\{valueName}：{oldText} → {newText}");
    }

    public static void RegistryDelete(string hive, string key, string valueName, object? oldValue)
    {
        var path = FormatRegPath(hive, key);
        if (oldValue is null)
        {
            NoteSkip($"删除 {path}\\{valueName}（本来就不存在）");
            return;
        }

        _batchRealChanges++;
        WriteChange(
            $"【注册表变更】\r\n" +
            $"    优化项：{ItemLabel()}\r\n" +
            $"    注册表位置：{path}\r\n" +
            $"    值名称：{valueName}\r\n" +
            $"    变更：原来从 {FormatValue(oldValue)} 变成 （已删除）");
        NoteWrite($"删除 {path}\\{valueName}：{FormatValue(oldValue)} → （已删除）");
    }

    public static void RegistryDeleteTree(string hive, string key, bool existed)
    {
        var path = FormatRegPath(hive, key);
        if (!existed)
        {
            NoteSkip($"删除键 {path}（本来就不存在）");
            return;
        }

        _batchRealChanges++;
        WriteChange(
            $"【注册表变更】\r\n" +
            $"    优化项：{ItemLabel()}\r\n" +
            $"    注册表位置：{path}\r\n" +
            $"    变更：原来从 （键存在） 变成 （整键已删除）");
        NoteWrite($"删除键 {path}");
    }

    public static void RegistryKeyWrite(string hive, string key, string detail)
    {
        _batchRealChanges++;
        WriteChange(
            $"【注册表变更】\r\n" +
            $"    优化项：{ItemLabel()}\r\n" +
            $"    注册表位置：{FormatRegPath(hive, key)}\r\n" +
            $"    变更：写入键 — {detail}");
        NoteWrite($"写入键 {FormatRegPath(hive, key)} — {detail}");
    }

    public static void ServiceChange(string serviceName, string detail, string? oldStart = null, string? newStart = null)
    {
        var reg = $@"HKLM\SYSTEM\CurrentControlSet\Services\{serviceName}\Start";
        if (oldStart is not null && newStart is not null)
        {
            if (string.Equals(oldStart, newStart, StringComparison.Ordinal))
            {
                NoteSkip($"服务 {serviceName} = {oldStart}");
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
            NoteWrite($"服务 {serviceName}：{oldStart} → {newStart} · {detail}");
            return;
        }

        _batchRealChanges++;
        WriteChange(
            $"【服务变更】\r\n" +
            $"    优化项：{ItemLabel()}\r\n" +
            $"    服务名：{serviceName}\r\n" +
            $"    注册表位置：HKLM\\SYSTEM\\CurrentControlSet\\Services\\{serviceName}\r\n" +
            $"    变更：{detail}");
        NoteWrite($"服务 {serviceName} · {detail}");
    }

    public static void SystemChange(string target, string detail, string? oldValue = null, string? newValue = null)
    {
        if (oldValue is not null || newValue is not null)
        {
            var from = oldValue ?? "（未知）";
            var to = newValue ?? "（未知）";
            if (string.Equals(from, to, StringComparison.Ordinal))
            {
                NoteSkip($"{target} = {from}");
                return;
            }

            _batchRealChanges++;
            WriteChange(
                $"【系统设置变更】\r\n" +
                $"    优化项：{ItemLabel()}\r\n" +
                $"    修改位置：{target}\r\n" +
                $"    变更：原来从 {from} 变成 {to}\r\n" +
                $"    详情：{detail}");
            NoteWrite($"{target}：{from} → {to} · {detail}");
            return;
        }

        _batchRealChanges++;
        WriteChange(
            $"【系统设置变更】\r\n" +
            $"    优化项：{ItemLabel()}\r\n" +
            $"    修改位置：{target}\r\n" +
            $"    变更：{detail}");
        NoteWrite($"{target} · {detail}");
    }

    private static void NoteSkip(string detail)
    {
        Write($"跳过未变：{ItemLabel()} {detail}");
        Debug($"跳过未变 · {detail}");
    }

    private static void NoteWrite(string detail)
    {
        Debug($"写入 · {detail}");
    }

    private static string FmtDebug(object? value)
    {
        if (value is null) return "（null）";
        if (value is bool b) return b ? "开/true" : "关/false";
        if (value is int or long or uint) return Convert.ToString(value, CultureInfo.InvariantCulture) ?? value.ToString() ?? "";
        if (value is string s) return string.IsNullOrEmpty(s) ? "（空字符）" : Quote(s);
        return value.ToString() ?? "";
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
            byte[] bytes => FormatBinary(bytes),
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
                || text.IndexOf("【系统设置变更】", StringComparison.Ordinal) >= 0
                || text.IndexOf("变更：原来从", StringComparison.Ordinal) >= 0;
        }
        catch
        {
            return false;
        }
    }

    private static string FormatBinary(byte[] bytes)
    {
        var hex = BitConverter.ToString(bytes);
        var first = bytes.Length > 0 ? $"首字节=0x{bytes[0]:X2}" : "空";
        return $"REG_BINARY[{bytes.Length}字节] {hex}（{first}）";
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
                var header = path.IndexOf("变更日志", StringComparison.OrdinalIgnoreCase) >= 0
                    ? "# 变更日志 — 仅记录优化时真正改动的值（原来从 xx 变成 yy）\r\n\r\n"
                    : path.IndexOf("debug.log", StringComparison.OrdinalIgnoreCase) >= 0
                        ? "# 调试日志 — 命令细节、分支与软跳过原因（程序设置中开启）\r\n\r\n"
                        : "# 操作日志\r\n\r\n";
                File.WriteAllText(path, header, Utf8Bom);
            }
            // 追加时不用 BOM，避免中间插入 BOM
            File.AppendAllText(path, line, Utf8NoBom);
        }
        catch (Exception ex)
        {
            try
            {
                var fallback = Path.Combine(LogDir, "changelog-fallback.log");
                File.AppendAllText(fallback,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 写入失败 path={path} err={ex.Message}\r\n{line}",
                    Utf8NoBom);
            }
            catch { /* ignore */ }
        }
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
