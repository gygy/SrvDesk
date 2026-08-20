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
        catch { return list; }

        string? name = null;
        foreach (var raw in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var line = raw.Trim();
            if (line.StartsWith("Feature Name :", StringComparison.OrdinalIgnoreCase))
                name = line.Substring(line.IndexOf(':') + 1).Trim();
            else if (line.StartsWith("State :", StringComparison.OrdinalIgnoreCase) && name is not null)
            {
                var state = line.Substring(line.IndexOf(':') + 1).Trim();
                list.Add(new WinFeatureItem
                {
                    Name = name,
                    State = state,
                    Kind = WinFeatureKind.OptionalFeature,
                    IsEnabledOrInstalled = state.IndexOf("Enabled", StringComparison.OrdinalIgnoreCase) >= 0,
                });
                name = null;
            }
        }
        return list;
    }

    public static List<WinFeatureItem> ListCapabilities()
    {
        var list = new List<WinFeatureItem>();
        string output;
        try { output = RunCapture("dism.exe", "/online /Get-Capabilities /Format:List"); }
        catch { return list; }

        string? name = null;
        foreach (var raw in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var line = raw.Trim();
            if (line.StartsWith("Capability Identity :", StringComparison.OrdinalIgnoreCase) ||
                line.StartsWith("Capability Name :", StringComparison.OrdinalIgnoreCase))
                name = line.Substring(line.IndexOf(':') + 1).Trim();
            else if (line.StartsWith("State :", StringComparison.OrdinalIgnoreCase) && name is not null)
            {
                var state = line.Substring(line.IndexOf(':') + 1).Trim();
                list.Add(new WinFeatureItem
                {
                    Name = name,
                    State = state,
                    Kind = WinFeatureKind.Capability,
                    IsEnabledOrInstalled = state.IndexOf("Installed", StringComparison.OrdinalIgnoreCase) >= 0,
                });
                name = null;
            }
        }
        return list;
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
        if (Regex.IsMatch(output, @"Error:\s*0x", RegexOptions.IgnoreCase) ||
            output.IndexOf("Error:", StringComparison.OrdinalIgnoreCase) >= 0 &&
            output.IndexOf("0x00000000", StringComparison.OrdinalIgnoreCase) < 0)
        {
            var err = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(l => l.IndexOf("Error", StringComparison.OrdinalIgnoreCase) >= 0)
                ?? "DISM 返回错误";
            throw new InvalidOperationException($"{action}失败：{err.Trim()}");
        }

        if (output.IndexOf("The operation completed successfully", StringComparison.OrdinalIgnoreCase) >= 0 ||
            output.IndexOf("操作成功完成", StringComparison.Ordinal) >= 0)
            return action + "成功（可能需重启）";

        return action + "已提交（请查看 DISM 输出，可能需重启）";
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
