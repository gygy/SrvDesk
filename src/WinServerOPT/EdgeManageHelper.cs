using System.Diagnostics;
using System.ServiceProcess;
using Microsoft.Win32;

namespace WinOpt;

internal enum EdgeComponentKind
{
    Edge,
    WebView2,
    EdgeCore,
}

internal sealed class EdgeComponentStatus
{
    public EdgeComponentKind Kind { get; set; }
    public string Title { get; set; } = "";
    public bool Installed { get; set; }
    public string Version { get; set; } = "";
    public string? SetupExe { get; set; }
}

internal sealed class EdgeManageStatus
{
    public EdgeComponentStatus Edge { get; set; } = new() { Kind = EdgeComponentKind.Edge, Title = "Microsoft Edge" };
    public EdgeComponentStatus WebView2 { get; set; } = new() { Kind = EdgeComponentKind.WebView2, Title = "Edge WebView2" };
    public EdgeComponentStatus EdgeCore { get; set; } = new() { Kind = EdgeComponentKind.EdgeCore, Title = "Edge Core" };
    public bool UpdatesDisabled { get; set; }
}

/// <summary>Edge / WebView2 / EdgeCore 状态、卸载与禁用更新。</summary>
internal static class EdgeManageHelper
{
    private const string EdgeUpdatePolicy = @"SOFTWARE\Policies\Microsoft\EdgeUpdate";
    private static readonly string Pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

    // EdgeUpdate Clients GUID
    private const string GuidEdge = "{56EB18F8-B008-4CBD-B6D2-8C97FE7E9062}";
    private const string GuidWebView2 = "{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}";
    private const string GuidEdgeCore = "{2CD8A037-6C76-4EED-AA16-A3009A9385EA}"; // 部分环境存在

    public static EdgeManageStatus Query()
    {
        return new EdgeManageStatus
        {
            Edge = Probe(EdgeComponentKind.Edge, "Microsoft Edge",
                Path.Combine(Pf86, "Microsoft", "Edge", "Application"),
                GuidEdge,
                ["Microsoft Edge"]),
            WebView2 = Probe(EdgeComponentKind.WebView2, "Edge WebView2",
                Path.Combine(Pf86, "Microsoft", "EdgeWebView", "Application"),
                GuidWebView2,
                ["Microsoft Edge WebView2 Runtime", "WebView2 Runtime"]),
            EdgeCore = Probe(EdgeComponentKind.EdgeCore, "Edge Core",
                Path.Combine(Pf86, "Microsoft", "EdgeCore", "Application"),
                GuidEdgeCore,
                ["Microsoft Edge Core", "Edge Core"]),
            UpdatesDisabled = IsUpdatesDisabled(),
        };
    }

    public static void SetUpdatesDisabled(bool disable)
    {
        if (disable)
        {
            ApplyLog.Write("禁用 Edge 更新");
            SetDword(EdgeUpdatePolicy, "UpdateDefault", 0);
            SetDword(EdgeUpdatePolicy, "AutoUpdateCheckPeriodMinutes", 0);
            SetDword(EdgeUpdatePolicy, "UpdatesSuppressedStartHour", 0);
            SetDword(EdgeUpdatePolicy, "UpdatesSuppressedStartMin", 0);
            SetDword(EdgeUpdatePolicy, "UpdatesSuppressedDurationMin", 1440);
            // 各通道更新开关
            SetDword(EdgeUpdatePolicy, "Update" + GuidEdge, 0);
            SetDword(EdgeUpdatePolicy, "Update" + GuidWebView2, 0);
            SetDword(EdgeUpdatePolicy, "Update" + GuidEdgeCore, 0);

            foreach (var svc in new[] { "edgeupdate", "edgeupdatem", "MicrosoftEdgeElevationService" })
                TrySetService(svc, enable: false);
        }
        else
        {
            ApplyLog.Write("恢复 Edge 更新");
            DeleteValue(EdgeUpdatePolicy, "UpdateDefault");
            DeleteValue(EdgeUpdatePolicy, "AutoUpdateCheckPeriodMinutes");
            DeleteValue(EdgeUpdatePolicy, "UpdatesSuppressedStartHour");
            DeleteValue(EdgeUpdatePolicy, "UpdatesSuppressedStartMin");
            DeleteValue(EdgeUpdatePolicy, "UpdatesSuppressedDurationMin");
            DeleteValue(EdgeUpdatePolicy, "Update" + GuidEdge);
            DeleteValue(EdgeUpdatePolicy, "Update" + GuidWebView2);
            DeleteValue(EdgeUpdatePolicy, "Update" + GuidEdgeCore);

            foreach (var svc in new[] { "edgeupdate", "edgeupdatem", "MicrosoftEdgeElevationService" })
                TrySetService(svc, enable: true, autoStart: false);
        }
    }

    public static string Uninstall(EdgeComponentKind kind)
    {
        var status = Query();
        var item = kind switch
        {
            EdgeComponentKind.Edge => status.Edge,
            EdgeComponentKind.WebView2 => status.WebView2,
            EdgeComponentKind.EdgeCore => status.EdgeCore,
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

        if (!item.Installed)
            return item.Title + " 未安装。";

        if (string.IsNullOrWhiteSpace(item.SetupExe) || !File.Exists(item.SetupExe))
            throw new InvalidOperationException(item.Title + " 未找到 setup.exe，无法自动卸载。");

        var args = kind switch
        {
            EdgeComponentKind.WebView2 =>
                "--uninstall --msedgewebview --system-level --verbose-logging --force-uninstall",
            EdgeComponentKind.EdgeCore =>
                "--uninstall --system-level --verbose-logging --force-uninstall",
            _ =>
                "--uninstall --system-level --verbose-logging --force-uninstall",
        };

        ApplyLog.Write("卸载 " + item.Title + "：" + item.SetupExe + " " + args);
        var code = RunSetup(item.SetupExe!, args);
        // Edge/WebView2 常见：0 成功；19 常表示已处理完毕
        if (code is not (0 or 19))
            throw new InvalidOperationException(item.Title + " 卸载退出码 " + code + "。");

        return item.Title + " 卸载命令已执行。";
    }

    public static string UninstallAll()
    {
        var notes = new List<string>();
        foreach (var kind in new[] { EdgeComponentKind.Edge, EdgeComponentKind.WebView2, EdgeComponentKind.EdgeCore })
        {
            try
            {
                notes.Add(Uninstall(kind));
            }
            catch (Exception ex)
            {
                notes.Add(kind + "：" + ex.Message);
            }
        }
        return string.Join("\r\n", notes);
    }

    private static EdgeComponentStatus Probe(
        EdgeComponentKind kind,
        string title,
        string appRoot,
        string clientGuid,
        string[] uninstallNames)
    {
        var st = new EdgeComponentStatus { Kind = kind, Title = title };
        var version = ReadClientVersion(clientGuid) ?? ReadVersionFromFolder(appRoot) ?? ReadUninstallVersion(uninstallNames);
        var setup = FindSetupExe(appRoot);

        st.Version = version ?? "";
        st.SetupExe = setup;
        st.Installed = !string.IsNullOrWhiteSpace(version) || setup is not null || Directory.Exists(appRoot);
        if (st.Installed && st.Version.Length == 0)
            st.Version = "已安装（版本未知）";
        return st;
    }

    private static string? ReadClientVersion(string guid)
    {
        foreach (var view in new[] { RegistryView.Registry32, RegistryView.Registry64 })
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                using var k = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\EdgeUpdate\Clients\" + guid);
                var pv = k?.GetValue("pv") as string;
                if (!string.IsNullOrWhiteSpace(pv) && pv != "0.0.0.0")
                    return pv;
            }
            catch { /* ignore */ }
        }
        return null;
    }

    private static string? ReadVersionFromFolder(string appRoot)
    {
        try
        {
            if (!Directory.Exists(appRoot)) return null;
            var verDir = Directory.GetDirectories(appRoot)
                .Select(Path.GetFileName)
                .Where(n => n is not null && char.IsDigit(n![0]))
                .OrderByDescending(n => n, StringComparer.OrdinalIgnoreCase)
                .FirstOrDefault();
            return verDir;
        }
        catch
        {
            return null;
        }
    }

    private static string? ReadUninstallVersion(string[] names)
    {
        foreach (var (hive, sub) in UninstallRoots())
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
                using var root = baseKey.OpenSubKey(sub);
                if (root is null) continue;
                foreach (var name in root.GetSubKeyNames())
                {
                    using var k = root.OpenSubKey(name);
                    var display = k?.GetValue("DisplayName") as string ?? "";
                    if (!names.Any(n => display.IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0))
                        continue;
                    var ver = k?.GetValue("DisplayVersion") as string;
                    if (!string.IsNullOrWhiteSpace(ver)) return ver;
                }
            }
            catch { /* ignore */ }
        }
        return null;
    }

    private static string? FindSetupExe(string appRoot)
    {
        try
        {
            if (!Directory.Exists(appRoot)) return null;
            foreach (var dir in Directory.GetDirectories(appRoot)
                         .OrderByDescending(d => d, StringComparer.OrdinalIgnoreCase))
            {
                var setup = Path.Combine(dir, "Installer", "setup.exe");
                if (File.Exists(setup)) return setup;
            }
        }
        catch { /* ignore */ }
        return null;
    }

    private static bool IsUpdatesDisabled()
    {
        using var k = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
            .OpenSubKey(EdgeUpdatePolicy);
        return k?.GetValue("UpdateDefault") is int i && i == 0;
    }

    private static IEnumerable<(RegistryHive Hive, string Sub)> UninstallRoots()
    {
        const string sub = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
        yield return (RegistryHive.LocalMachine, sub);
        yield return (RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall");
    }

    private static void SetDword(string key, string name, int value)
    {
        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var k = baseKey.CreateSubKey(key, true)
            ?? throw new InvalidOperationException("无法写入：" + key);
        var old = k.GetValue(name);
        ApplyLog.RegistryDword("HKLM", key, name, old, value);
        k.SetValue(name, value, RegistryValueKind.DWord);
    }

    private static void DeleteValue(string key, string name)
    {
        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var k = baseKey.OpenSubKey(key, writable: true);
        if (k is null) return;
        var old = k.GetValue(name);
        if (old is null) return;
        ApplyLog.RegistryDelete("HKLM", key, name, old);
        k.DeleteValue(name, throwOnMissingValue: false);
    }

    private static void TrySetService(string name, bool enable, bool autoStart = true)
    {
        try
        {
            using var sc = new ServiceController(name);
            _ = sc.DisplayName;
        }
        catch
        {
            return;
        }

        var start = enable ? (autoStart ? "auto" : "demand") : "disabled";
        RunSc($"config {name} start= {start}");
        if (enable) RunSc($"start {name}");
        else RunSc($"stop {name}");
    }

    private static void RunSc(string args)
    {
        using var p = Process.Start(new ProcessStartInfo
        {
            FileName = "sc.exe",
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        });
        if (p is null) return;
        _ = p.StandardOutput.ReadToEnd();
        _ = p.StandardError.ReadToEnd();
        p.WaitForExit(20_000);
    }

    private static int RunSetup(string exe, string args)
    {
        using var p = Process.Start(new ProcessStartInfo
        {
            FileName = exe,
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(exe) ?? "",
        }) ?? throw new InvalidOperationException("无法启动 " + exe);
        _ = p.StandardOutput.ReadToEnd();
        _ = p.StandardError.ReadToEnd();
        p.WaitForExit(300_000);
        return p.ExitCode;
    }
}
