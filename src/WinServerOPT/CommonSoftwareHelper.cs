using System.Diagnostics;
using System.Net;
using System.Text;
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
    private static string? _wingetPath;

    public static string DownloadDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinOpt", "software-downloads");

    public static bool IsWingetAvailable() =>
        TryRunWinget("--version", out _);

    public static string ResolveWingetPath()
    {
        if (_wingetPath is not null && File.Exists(_wingetPath))
            return _wingetPath;

        try
        {
            var output = RunCapture("where.exe", "winget.exe");
            foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var path = line.Trim().Trim('"');
                if (path.EndsWith("winget.exe", StringComparison.OrdinalIgnoreCase) && File.Exists(path))
                {
                    _wingetPath = path;
                    return path;
                }
            }
        }
        catch { /* ignore */ }

        var localApps = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Microsoft", "WindowsApps", "winget.exe");
        if (File.Exists(localApps))
        {
            _wingetPath = localApps;
            return localApps;
        }

        return "winget.exe";
    }

    public static CommonSoftwareStatus Query(CommonSoftwareItem item)
    {
        if (item.IsWingetBootstrap)
            return QueryWingetStatus();

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
        if (item.IsWingetBootstrap)
            return InstallWinget();

        if (IsWingetAvailable() && !string.IsNullOrWhiteSpace(item.WingetId))
        {
            var code = Run(ResolveWingetPath(),
                $"install -e --id {item.WingetId} --accept-package-agreements --accept-source-agreements");
            if (code == 0) return "";
            if (code == -1978335189) // 0x8A150013 already installed
                return "软件已安装或无需重复安装。";
        }

        OpenDownloadPage(item);
        return IsWingetAvailable() && !string.IsNullOrWhiteSpace(item.WingetId)
            ? "winget 安装未成功，已在浏览器打开官方下载页，请手动安装。"
            : string.IsNullOrWhiteSpace(item.WingetId)
                ? "该软件暂无 winget 包，已在浏览器打开官方下载页。"
                : "本机未检测到 winget，已在浏览器打开官方下载页，请手动安装。";
    }

    public static string Uninstall(CommonSoftwareItem item)
    {
        ApplyLog.Write("常用软件卸载：" + item.Title);
        if (item.IsWingetBootstrap)
            return "winget（应用安装程序）为系统组件，不建议在此卸载。请在「设置 → 应用」中操作。";

        if (IsWingetAvailable() && !string.IsNullOrWhiteSpace(item.WingetId))
        {
            var code = Run(ResolveWingetPath(), $"uninstall -e --id {item.WingetId}");
            if (code == 0) return "";
        }

        var status = Query(item);
        var cmd = status.UninstallCommand;
        if (string.IsNullOrWhiteSpace(cmd))
            return "未找到可用的卸载命令。请在「设置 → 应用」中手动卸载。";

        RunShell(cmd!);
        return "";
    }

    public static string InstallWinget()
    {
        if (IsWingetAvailable())
            return "";

        ApplyLog.Write("安装 winget（App Installer）");
        var notes = new List<string>();

        var repair = TryRepairWinGetPackageManager();
        if (repair.Length > 0) notes.Add(repair);
        if (IsWingetAvailable()) return FormatWingetReady(notes);

        var bootstrap = TryBootstrapWingetPackages();
        if (bootstrap.Length > 0) notes.Add(bootstrap);
        if (IsWingetAvailable()) return FormatWingetReady(notes);

        var register = TryRegisterAppInstaller();
        if (register.Length > 0) notes.Add(register);
        if (IsWingetAvailable()) return FormatWingetReady(notes);

        var wingetItem = CommonSoftwareCatalog.Find("winget");
        if (wingetItem is not null)
            OpenDownloadPage(wingetItem);

        notes.Add("自动安装未完成。已打开官方下载页；也可重启本程序或注销后再试。");
        return string.Join("\r\n", notes);
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
            return "未检测到 winget，无法批量检查更新。可在必备列表中安装 winget，或逐一点击「一键安装」。";

        var output = RunCapture(ResolveWingetPath(), "upgrade --include-unknown");
        var upgradable = items.Count(i =>
        {
            if (i.IsWingetBootstrap || string.IsNullOrWhiteSpace(i.WingetId)) return false;
            var status = Query(i);
            return status.Installed &&
                output.IndexOf(i.WingetId, StringComparison.OrdinalIgnoreCase) >= 0;
        });
        return upgradable > 0
            ? $"检测到 {upgradable} 款已安装软件可能有更新（详见 winget upgrade）。"
            : "已检查：当前列表中的已安装软件未发现 winget 可用更新。";
    }

    private static CommonSoftwareStatus QueryWingetStatus()
    {
        if (!IsWingetAvailable())
        {
            if (IsAppInstallerPackagePresent())
            {
                return new CommonSoftwareStatus
                {
                    Installed = false,
                    Version = "已安装应用安装程序，winget 未注册",
                };
            }
            return new CommonSoftwareStatus();
        }

        TryRunWinget("--version", out var version);
        version = version.Trim();
        if (version.StartsWith("v", StringComparison.OrdinalIgnoreCase) && version.Length > 1)
            version = version.Substring(1).Trim();
        return new CommonSoftwareStatus
        {
            Installed = true,
            Version = version.Length > 0 ? "v" + version : "已就绪",
        };
    }

    private static bool IsAppInstallerPackagePresent()
    {
        try
        {
            var output = RunCapture("powershell.exe",
                "-NoProfile -ExecutionPolicy Bypass -Command \"Get-AppxPackage -Name Microsoft.DesktopAppInstaller | Select-Object -First 1 -ExpandProperty Name\"");
            return output.IndexOf("DesktopAppInstaller", StringComparison.OrdinalIgnoreCase) >= 0;
        }
        catch
        {
            return false;
        }
    }

    private static string TryRepairWinGetPackageManager()
    {
        const string script = @"
$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
if (-not (Get-PSRepository -Name PSGallery -ErrorAction SilentlyContinue)) {
  Register-PSRepository -Default -ErrorAction SilentlyContinue
}
Set-PSRepository -Name PSGallery -InstallationPolicy Trusted -ErrorAction SilentlyContinue
if (-not (Get-Module -ListAvailable -Name Microsoft.WinGet.Client)) {
  Install-Module Microsoft.WinGet.Client -Force -AllowClobber -Scope AllUsers
}
Import-Module Microsoft.WinGet.Client -Force
Repair-WinGetPackageManager -AllUsers
";
        try
        {
            var code = RunPowerShell(script);
            return code == 0
                ? "已通过 Microsoft.WinGet.Client 修复 winget。"
                : "Microsoft.WinGet.Client 修复返回码 " + code + "。";
        }
        catch (Exception ex)
        {
            return "Microsoft.WinGet.Client 修复失败：" + ex.Message;
        }
    }

    private static string TryBootstrapWingetPackages()
    {
        Directory.CreateDirectory(DownloadDir);
        var vcLibs = Path.Combine(DownloadDir, "Microsoft.VCLibs.x64.14.00.Desktop.appx");
        var uiXaml = Path.Combine(DownloadDir, "Microsoft.UI.Xaml.2.8.x64.appx");
        var bundle = Path.Combine(DownloadDir, "Microsoft.DesktopAppInstaller.msixbundle");

        try
        {
            DownloadIfMissing("https://aka.ms/Microsoft.VCLibs.x64.14.00.Desktop.appx", vcLibs);
            DownloadIfMissing(
                "https://github.com/microsoft/microsoft-ui-xaml/releases/download/v2.8.6/Microsoft.UI.Xaml.2.8.x64.appx",
                uiXaml);
            DownloadIfMissing("https://aka.ms/getwinget", bundle);

            AddAppxPackage(vcLibs);
            AddAppxPackage(uiXaml);
            AddAppxPackage(bundle);

            return "已下载并安装 App Installer 依赖与主包。";
        }
        catch (Exception ex)
        {
            return "离线包安装失败：" + ex.Message;
        }
    }

    private static string TryRegisterAppInstaller()
    {
        const string script = @"
$ErrorActionPreference = 'SilentlyContinue'
Add-AppxPackage -RegisterByFamilyName -MainPackage Microsoft.DesktopAppInstaller_8wekyb3d8bbwe
";
        try
        {
            RunPowerShell(script);
            return "已尝试注册 App Installer 应用别名。";
        }
        catch (Exception ex)
        {
            return "注册 App Installer 失败：" + ex.Message;
        }
    }

    private static string FormatWingetReady(List<string> notes)
    {
        _wingetPath = null;
        notes.Add("winget 已可用，可继续一键安装其它软件。");
        return string.Join("\r\n", notes.Where(n => n.Length > 0));
    }

    private static void DownloadIfMissing(string url, string dest)
    {
        if (File.Exists(dest) && new FileInfo(dest).Length > 0) return;
        using var wc = new WebClient();
        wc.DownloadFile(url, dest);
    }

    private static void AddAppxPackage(string path)
    {
        var escaped = path.Replace("'", "''");
        RunPowerShell($"Add-AppxPackage -Path '{escaped}'");
    }

    private static bool TryRunWinget(string args, out string output)
    {
        output = "";
        try
        {
            output = RunCapture(ResolveWingetPath(), args);
            if (output.IndexOf("v", StringComparison.OrdinalIgnoreCase) >= 0
                || output.IndexOf("Windows Package Manager", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }
        catch { /* ignore */ }

        _wingetPath = null;
        try
        {
            output = RunCapture("winget.exe", args);
            return output.IndexOf("v", StringComparison.OrdinalIgnoreCase) >= 0
                || output.IndexOf("Windows Package Manager", StringComparison.OrdinalIgnoreCase) >= 0;
        }
        catch
        {
            return false;
        }
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

    private static int RunPowerShell(string script)
    {
        var file = Path.Combine(DownloadDir, "winget-install-" + Guid.NewGuid().ToString("N") + ".ps1");
        Directory.CreateDirectory(DownloadDir);
        File.WriteAllText(file, script, Encoding.UTF8);
        try
        {
            return Run("powershell.exe",
                $"-NoProfile -ExecutionPolicy Bypass -File \"{file}\"");
        }
        finally
        {
            try { File.Delete(file); } catch { /* ignore */ }
        }
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
