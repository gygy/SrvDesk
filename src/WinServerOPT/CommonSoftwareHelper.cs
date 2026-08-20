using System.Diagnostics;
using Microsoft.Win32;

namespace WinOpt;

internal sealed class CommonSoftwareStatus
{
    public bool Installed { get; set; }
    public string Version { get; set; } = "";
    public string? UninstallCommand { get; set; }
}

internal static class CommonSoftwareHelper
{
    public static string DownloadDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinOpt", "software-downloads");

    public static bool IsWingetAvailable()
    {
        try
        {
            var output = RunCapture("winget.exe", "--version");
            return output.IndexOf("v", StringComparison.OrdinalIgnoreCase) >= 0;
        }
        catch
        {
            return false;
        }
    }

    public static CommonSoftwareStatus Query(CommonSoftwareItem item)
    {
        foreach (var keyPath in UninstallKeyPaths())
        {
            using var baseKey = RegistryKey.OpenBaseKey(
                keyPath.Hive, RegistryView.Registry64);
            using var uninstall = baseKey.OpenSubKey(keyPath.SubKey);
            if (uninstall is null) continue;

            foreach (var subName in uninstall.GetSubKeyNames())
            {
                using var sub = uninstall.OpenSubKey(subName);
                if (sub is null) continue;
                var display = sub.GetValue("DisplayName") as string ?? "";
                if (!Matches(display, item.DetectPatterns)) continue;

                return new CommonSoftwareStatus
                {
                    Installed = true,
                    Version = sub.GetValue("DisplayVersion") as string ?? "",
                    UninstallCommand = sub.GetValue("QuietUninstallString") as string
                        ?? sub.GetValue("UninstallString") as string,
                };
            }
        }

        return new CommonSoftwareStatus();
    }

    public static string Install(CommonSoftwareItem item)
    {
        ApplyLog.Write("常用软件安装：" + item.Title);
        if (IsWingetAvailable())
        {
            var code = Run("winget.exe",
                $"install -e --id {item.WingetId} --accept-package-agreements --accept-source-agreements");
            if (code == 0) return "";
            if (code == -1978335189) // 0x8A150013 already installed
                return "软件已安装或无需重复安装。";
        }

        OpenDownloadPage(item);
        return IsWingetAvailable()
            ? "winget 安装未成功，已在浏览器打开官方下载页，请手动安装。"
            : "本机未检测到 winget，已在浏览器打开官方下载页，请手动安装。";
    }

    public static string Uninstall(CommonSoftwareItem item)
    {
        ApplyLog.Write("常用软件卸载：" + item.Title);
        if (IsWingetAvailable())
        {
            var code = Run("winget.exe", $"uninstall -e --id {item.WingetId}");
            if (code == 0) return "";
        }

        var status = Query(item);
        var cmd = status.UninstallCommand;
        if (string.IsNullOrWhiteSpace(cmd))
            return "未找到可用的卸载命令。请在「设置 → 应用」中手动卸载。";

        RunShell(cmd!);
        return "";
    }

    public static void OpenDownloadPage(CommonSoftwareItem item)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = item.DownloadUrl,
            UseShellExecute = true,
        });
        ApplyLog.Write("打开下载页：" + item.Title);
    }

    public static void ClearDownloadCache()
    {
        if (!Directory.Exists(DownloadDir)) return;
        Directory.Delete(DownloadDir, recursive: true);
        ApplyLog.Write("已删除常用软件下载临时文件");
    }

    public static string CheckUpdates(IReadOnlyList<CommonSoftwareItem> items)
    {
        if (!IsWingetAvailable())
            return "未检测到 winget，无法批量检查更新。可逐一点击「一键安装」或访问官方下载页。";

        var output = RunCapture("winget.exe", "upgrade --include-unknown");
        var upgradable = items.Count(i =>
        {
            var status = Query(i);
            return status.Installed &&
                output.IndexOf(i.WingetId, StringComparison.OrdinalIgnoreCase) >= 0;
        });
        return upgradable > 0
            ? $"检测到 {upgradable} 款已安装软件可能有更新（详见 winget upgrade）。"
            : "已检查：当前列表中的已安装软件未发现 winget 可用更新。";
    }

    private static bool Matches(string displayName, string[] patterns)
    {
        foreach (var p in patterns)
        {
            if (displayName.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }
        return false;
    }

    private static IEnumerable<(RegistryHive Hive, string SubKey)> UninstallKeyPaths()
    {
        const string sub = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
        yield return (RegistryHive.LocalMachine, sub);
        yield return (RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall");
        yield return (RegistryHive.CurrentUser, sub);
    }

    private static void RunShell(string command)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/c " + command,
            UseShellExecute = false,
            CreateNoWindow = true,
        })?.WaitForExit(600_000);
    }

    private static int Run(string file, string args)
    {
        using var p = Process.Start(new ProcessStartInfo
        {
            FileName = file,
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        }) ?? throw new InvalidOperationException("无法启动 " + file);
        p.WaitForExit(600_000);
        return p.ExitCode;
    }

    private static string RunCapture(string file, string args)
    {
        using var p = Process.Start(new ProcessStartInfo
        {
            FileName = file,
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        }) ?? throw new InvalidOperationException("无法启动 " + file);
        var output = p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
        p.WaitForExit(120_000);
        return output;
    }
}
