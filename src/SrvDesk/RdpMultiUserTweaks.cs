using System.Diagnostics;
using Microsoft.Win32;

namespace SrvDesk;

/// <summary>
/// 多用户同时登录（对齐常见 Server 批处理）：
/// 开启 RDP、安装 RDS-RD-Server、允许同账号多会话、抬高 MaxSessions、禁止注销控制台管理员。
/// </summary>
internal static class RdpMultiUserTweaks
{
    private const string TermServer = @"SYSTEM\CurrentControlSet\Control\Terminal Server";
    private const string TermPolicies = @"SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services";
    private const string FeatureRdServer = "RDS-RD-Server";
    private const int MaxSessionsValue = 999999;

    public static void ReadInto(Optimizer.State s, bool fullScan)
    {
        s.RdpMultiUserLogin = IsConfigured(fullScan);
    }

    public static void Apply(bool enable)
    {
        if (enable)
            Enable();
        else
            Disable();
    }

    public static bool IsConfigured(bool checkFeature)
    {
        if (!DwordEquals(Hive.HkLm, TermServer, "fDenyTSConnections", 0))
            return false;
        if (!DwordEquals(Hive.HkLm, TermServer, "fSingleSessionPerUser", 0))
            return false;
        if (!DwordEquals(Hive.HkLm, TermServer, "MaxSessions", MaxSessionsValue))
            return false;
        if (!DwordEquals(Hive.HkLm, TermPolicies, "fAllowConsoleLogout", 0))
            return false;

        if (!checkFeature || !Optimizer.IsWindowsServer())
            return true;

        return IsFeatureEnabled(FeatureRdServer);
    }

    private static void Enable()
    {
        // 与批处理一致：允许远程连接 + 防火墙
        SetDword(Hive.HkLm, TermServer, "fDenyTSConnections", 0);
        TryEnableFirewallRdp();

        SetDword(Hive.HkLm, TermServer, "fSingleSessionPerUser", 0);
        SetDword(Hive.HkLm, TermServer, "MaxSessions", MaxSessionsValue);
        SetDword(Hive.HkLm, TermPolicies, "fAllowConsoleLogout", 0);

        if (Optimizer.IsWindowsServer())
            TryEnableRdSessionHost();

        ApplyLog.Write("多用户同时登录：已写入会话策略"
            + (Optimizer.IsWindowsServer() ? "（并尝试启用 RDS-RD-Server）" : ""));
    }

    private static void Disable()
    {
        // 恢复单会话限制；不卸载 RDS 角色、不关 RDP（避免误伤）
        SetDword(Hive.HkLm, TermServer, "fSingleSessionPerUser", 1);
        DeleteValue(Hive.HkLm, TermServer, "MaxSessions");
        DeleteValue(Hive.HkLm, TermPolicies, "fAllowConsoleLogout");
        ApplyLog.Write("多用户同时登录：已恢复单会话限制（未卸载 RDS、未关闭 RDP）");
    }

    private static void TryEnableFirewallRdp()
    {
        foreach (var g in new[] { "remote desktop", "远程桌面", "Remote Desktop" })
        {
            try
            {
                Run("netsh.exe",
                    "advfirewall firewall set rule group=\"" + g + "\" new enable=Yes");
                return;
            }
            catch (Exception ex)
            {
                ApplyLog.Debug("多用户 RDP 防火墙组失败：" + g + " → " + ex.Message);
            }
        }
    }

    private static void TryEnableRdSessionHost()
    {
        try
        {
            if (IsFeatureEnabled(FeatureRdServer))
            {
                ApplyLog.Debug("RDS-RD-Server 已启用");
                return;
            }

            ApplyLog.Write("正在 DISM 启用 RDS-RD-Server（可能需数分钟）…");
            var code = Run("dism.exe",
                "/online /Enable-Feature /FeatureName:" + FeatureRdServer + " /All /NoRestart");
            if (code != 0 && code != 3010)
                ApplyLog.Write("DISM 启用 RDS-RD-Server 退出码 " + code + "（功能名因 SKU 可能不同，可到「可选功能」手动安装）");
            else
                ApplyLog.Write("RDS-RD-Server 已请求启用"
                    + (code == 3010 ? "（需重启）" : ""));
        }
        catch (Exception ex)
        {
            ApplyLog.Write("启用 RDS-RD-Server 失败：" + ex.Message);
        }
    }

    private static bool IsFeatureEnabled(string name)
    {
        try
        {
            var output = RunCapture("dism.exe", "/online /Get-FeatureInfo /FeatureName:" + name);
            if (output.IndexOf("not found", StringComparison.OrdinalIgnoreCase) >= 0)
                return false;
            return output.IndexOf("State : Enabled", StringComparison.OrdinalIgnoreCase) >= 0
                   || output.Contains("状态 : 已启用");
        }
        catch
        {
            return false;
        }
    }

    private enum Hive { HkLm, HkCu }

    private static RegistryKey? Open(Hive hive, string subKey, bool writable)
    {
        var root = hive == Hive.HkLm ? Registry.LocalMachine : Registry.CurrentUser;
        return writable ? root.CreateSubKey(subKey, true) : root.OpenSubKey(subKey, false);
    }

    private static void SetDword(Hive hive, string subKey, string name, int value)
    {
        using var k = Open(hive, subKey, writable: true)
            ?? throw new InvalidOperationException("无法写入 " + subKey);
        k.SetValue(name, value, RegistryValueKind.DWord);
    }

    private static void DeleteValue(Hive hive, string subKey, string name)
    {
        using var k = Open(hive, subKey, writable: true);
        try { k?.DeleteValue(name, throwOnMissingValue: false); }
        catch { /* ignore */ }
    }

    private static bool DwordEquals(Hive hive, string subKey, string name, int expected)
    {
        using var k = Open(hive, subKey, writable: false);
        if (k?.GetValue(name) is not int i) return false;
        return i == expected;
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
        var stdout = p.StandardOutput.ReadToEnd();
        var stderr = p.StandardError.ReadToEnd();
        p.WaitForExit(120_000);
        return stdout + "\r\n" + stderr;
    }
}
