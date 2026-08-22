using System.Diagnostics;
using System.Text.RegularExpressions;

namespace WinOpt;

internal enum WinFeatureKind
{
    OptionalFeature,
    Capability,
}

internal sealed class WinFeatureItem
{
    public string Name { get; set; } = "";
    public string State { get; set; } = "";
    public WinFeatureKind Kind { get; set; }
    public bool IsEnabledOrInstalled { get; set; }

    public string KindText => Kind == WinFeatureKind.OptionalFeature ? "可选功能" : "Capability";
    public string StateText => IsEnabledOrInstalled ? "已启用" : "未启用";
}

internal static class WindowsFeaturesHelper
{
    private static readonly string[] CriticalNameHints =
    [
        "NetFx", "NET-Framework", "PowerShell", "ServerCore", "Server-Gui",
        "FileAndStorage", "File-Services", "Microsoft-Windows-Server-AppCompat",
        "DirectoryServices", "ActiveDirectory", "DNS-Server", "DHCP",
        "Hyper-V", "Containers", "WindowsServerBackup",
    ];

    public static List<WinFeatureItem> ListAll()
    {
        var list = new List<WinFeatureItem>();
        list.AddRange(ListOptionalFeatures());
        list.AddRange(ListCapabilities());
        return list
            .OrderByDescending(x => x.IsEnabledOrInstalled)
            .ThenBy(x => x.Kind)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static List<WinFeatureItem> ListOptionalFeatures()
    {
        var list = new List<WinFeatureItem>();
        string output;
        try { output = RunCapture("dism.exe", "/online /Get-Features /Format:List"); }
        catch (Exception ex) { throw new InvalidOperationException("无法运行 DISM：" + ex.Message, ex); }

        EnsureDismReadable(output);
        string? name = null;
        foreach (var raw in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var line = raw.Trim();
            if (TryReadLabeledValue(line, out var label, out var value))
            {
                if (IsFeatureNameLabel(label))
                    name = value;
                else if (IsStateLabel(label) && name is not null)
                {
                    list.Add(new WinFeatureItem
                    {
                        Name = name,
                        State = value,
                        Kind = WinFeatureKind.OptionalFeature,
                        IsEnabledOrInstalled = IsEnabledState(value),
                    });
                    name = null;
                }
            }
        }
        return list;
    }

    public static List<WinFeatureItem> ListCapabilities()
    {
        var list = new List<WinFeatureItem>();
        string output;
        try { output = RunCapture("dism.exe", "/online /Get-Capabilities /Format:List"); }
        catch (Exception ex) { throw new InvalidOperationException("无法运行 DISM：" + ex.Message, ex); }

        EnsureDismReadable(output);
        string? name = null;
        foreach (var raw in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var line = raw.Trim();
            if (TryReadLabeledValue(line, out var label, out var value))
            {
                if (IsCapabilityNameLabel(label))
                    name = value;
                else if (IsStateLabel(label) && name is not null)
                {
                    list.Add(new WinFeatureItem
                    {
                        Name = name,
                        State = value,
                        Kind = WinFeatureKind.Capability,
                        IsEnabledOrInstalled = IsInstalledState(value),
                    });
                    name = null;
                }
            }
        }
        return list;
    }

    private static void EnsureDismReadable(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
            throw new InvalidOperationException("DISM 无输出。请确认以管理员运行本程序。");
        if (Regex.IsMatch(output, @"Error:\s*740", RegexOptions.IgnoreCase) ||
            output.Contains("错误: 740") || output.Contains("需要提升") ||
            output.IndexOf("elevation", StringComparison.OrdinalIgnoreCase) >= 0)
            throw new InvalidOperationException("DISM 需要管理员权限。请右键「以管理员身份运行」本程序后重试。");
        if (Regex.IsMatch(output, @"Error:\s*0x", RegexOptions.IgnoreCase) ||
            Regex.IsMatch(output, @"错误:\s*0x"))
        {
            var err = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(l =>
                    l.IndexOf("Error", StringComparison.OrdinalIgnoreCase) >= 0 || l.Contains("错误"))
                ?? "DISM 返回错误";
            throw new InvalidOperationException(err.Trim());
        }
    }

    private static bool TryReadLabeledValue(string line, out string label, out string value)
    {
        label = "";
        value = "";
        var idx = line.IndexOf(':');
        if (idx <= 0) return false;
        label = line.Substring(0, idx).Trim();
        value = line.Substring(idx + 1).Trim();
        return label.Length > 0;
    }

    private static bool IsFeatureNameLabel(string label) =>
        label.Equals("Feature Name", StringComparison.OrdinalIgnoreCase) ||
        label.Contains("功能名称");

    private static bool IsCapabilityNameLabel(string label) =>
        label.Equals("Capability Identity", StringComparison.OrdinalIgnoreCase) ||
        label.Equals("Capability Name", StringComparison.OrdinalIgnoreCase) ||
        label.Contains("功能标识") || label.Contains("功能名称") || label.Contains("能力");

    private static bool IsStateLabel(string label) =>
        label.Equals("State", StringComparison.OrdinalIgnoreCase) ||
        label.Contains("状态");

    private static bool IsEnabledState(string state)
    {
        if (state.Contains("未启用") || state.IndexOf("Disabled", StringComparison.OrdinalIgnoreCase) >= 0)
            return false;
        return state.IndexOf("Enabled", StringComparison.OrdinalIgnoreCase) >= 0
            || state.Contains("已启用");
    }

    private static bool IsInstalledState(string state)
    {
        if (state.Contains("未安装") || state.IndexOf("Not Present", StringComparison.OrdinalIgnoreCase) >= 0)
            return false;
        return state.IndexOf("Installed", StringComparison.OrdinalIgnoreCase) >= 0
            || state.Contains("已安装");
    }

    public static bool IsCritical(string name) =>
        CriticalNameHints.Any(h => name.IndexOf(h, StringComparison.OrdinalIgnoreCase) >= 0);

    public static string DisableOrRemove(WinFeatureItem item)
    {
        ServerDesktopTweaks.ResetDismCache();
        if (item.Kind == WinFeatureKind.OptionalFeature)
        {
            var output = RunCapture("dism.exe",
                $"/online /Disable-Feature /FeatureName:{Quote(item.Name)} /NoRestart");
            return SummarizeDism(output, "禁用");
        }

        var outCap = RunCapture("dism.exe",
            $"/online /Remove-Capability /CapabilityName:{Quote(item.Name)} /NoRestart");
        return SummarizeDism(outCap, "卸载");
    }

    public static string EnableOrAdd(WinFeatureItem item)
    {
        ServerDesktopTweaks.ResetDismCache();
        if (item.Kind == WinFeatureKind.OptionalFeature)
        {
            var output = RunCapture("dism.exe",
                $"/online /Enable-Feature /FeatureName:{Quote(item.Name)} /All /NoRestart");
            return SummarizeDism(output, "启用");
        }

        var outCap = RunCapture("dism.exe",
            $"/online /Add-Capability /CapabilityName:{Quote(item.Name)} /NoRestart");
        return SummarizeDism(outCap, "安装");
    }

    private static string Quote(string name) =>
        name.IndexOf(' ') >= 0 ? "\"" + name.Replace("\"", "") + "\"" : name;

    private static string SummarizeDism(string output, string action)
    {
        var hasError = Regex.IsMatch(output, @"Error:\s*0x", RegexOptions.IgnoreCase)
            || Regex.IsMatch(output, @"错误:\s*0x")
            || (output.IndexOf("Error:", StringComparison.OrdinalIgnoreCase) >= 0
                && output.IndexOf("0x00000000", StringComparison.OrdinalIgnoreCase) < 0)
            || (output.Contains("错误:") && !output.Contains("0x00000000"));
        if (hasError)
        {
            var err = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(l =>
                    l.IndexOf("Error", StringComparison.OrdinalIgnoreCase) >= 0 || l.Contains("错误"))
                ?? "DISM 返回错误";
            throw new InvalidOperationException($"{action}失败：{err.Trim()}");
        }

        if (output.IndexOf("The operation completed successfully", StringComparison.OrdinalIgnoreCase) >= 0 ||
            output.IndexOf("操作成功完成", StringComparison.Ordinal) >= 0)
            return action + "成功（可能需重启）";

        return action + "已提交（可能需重启）";
    }

    private static string RunCapture(string fileName, string arguments)
    {
        using var p = Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        }) ?? throw new InvalidOperationException("无法启动 " + fileName);
        var output = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
        p.WaitForExit(600_000);
        return output;
    }
}
