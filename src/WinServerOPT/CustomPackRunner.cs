using System.Diagnostics;
using System.Text;

namespace SrvDesk;

/// <summary>在界面中运行自定义配置项（.reg / .cmd / .ps1）。</summary>
internal static class CustomPackRunner
{
    public static string RunItem(string packId, CustomPackItem item)
    {
        var path = CustomPackStore.ItemPath(packId, item);
        if (!File.Exists(path))
            throw new FileNotFoundException("方案内文件丢失：" + item.Name, path);

        using (ApplyLog.PushContext("自定义配置 · " + item.Name))
        {
            return item.KindEnum switch
            {
                CustomPackItemKind.Reg => RunReg(path, item.Name),
                CustomPackItemKind.Cmd => RunCmd(path, item.Name),
                CustomPackItemKind.Ps1 => RunPs1(path, item.Name),
                _ => throw new InvalidOperationException("未知类型：" + item.Kind),
            };
        }
    }

    public static (int Ok, int Fail, List<string> Errors) RunEnabled(CustomPackDetail pack)
    {
        var ok = 0;
        var fail = 0;
        var errors = new List<string>();
        foreach (var item in pack.Items.Where(i => i.Enabled))
        {
            try
            {
                RunItem(pack.Id, item);
                ok++;
            }
            catch (Exception ex)
            {
                fail++;
                errors.Add(item.Name + "：" + ex.Message);
                ApplyLog.Write("自定义配置运行失败 · " + item.Name + " · " + ex.Message);
            }
        }
        return (ok, fail, errors);
    }

    private static string RunReg(string path, string name)
    {
        // /s 静默导入；HKLM 项需管理员
        var (code, output) = RunCapture("regedit.exe", "/s \"" + path + "\"", Path.GetDirectoryName(path)!);
        if (code != 0)
            throw new InvalidOperationException("regedit 退出码 " + code + Detail(output));
        ApplyLog.SystemChange(path, "导入注册表 .reg", "未导入", "已导入：" + name);
        return "已导入注册表：" + name;
    }

    private static string RunCmd(string path, string name)
    {
        var workDir = Path.GetDirectoryName(path)!;
        var (code, output) = RunCapture("cmd.exe", "/c \"" + path + "\"", workDir, timeoutMs: 120_000);
        if (code != 0)
            throw new InvalidOperationException("脚本退出码 " + code + Detail(output));
        ApplyLog.SystemChange(path, "运行 CMD 脚本", "未运行", "已运行：" + name);
        return "已运行 CMD：" + name;
    }

    private static string RunPs1(string path, string name)
    {
        var workDir = Path.GetDirectoryName(path)!;
        var args = "-NoProfile -ExecutionPolicy Bypass -File \"" + path + "\"";
        var (code, output) = RunCapture("powershell.exe", args, workDir, timeoutMs: 180_000);
        if (code != 0)
            throw new InvalidOperationException("PowerShell 退出码 " + code + Detail(output));
        ApplyLog.SystemChange(path, "运行 PowerShell 脚本", "未运行", "已运行：" + name);
        return "已运行 PowerShell：" + name;
    }

    private static string Detail(string output)
    {
        var t = (output ?? "").Trim();
        if (t.Length == 0) return "";
        if (t.Length > 240) t = t.Substring(0, 240) + "…";
        return "：" + t;
    }

    private static (int ExitCode, string Output) RunCapture(
        string file, string args, string workDir, int timeoutMs = 60_000)
    {
        using var p = Process.Start(new ProcessStartInfo
        {
            FileName = file,
            Arguments = args,
            WorkingDirectory = workDir,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.Default,
            StandardErrorEncoding = Encoding.Default,
        }) ?? throw new InvalidOperationException("无法启动 " + file);

        var stdout = p.StandardOutput.ReadToEnd();
        var stderr = p.StandardError.ReadToEnd();
        if (!p.WaitForExit(timeoutMs))
        {
            try { p.Kill(); } catch { /* ignore */ }
            throw new TimeoutException(Path.GetFileName(file) + " 运行超时。");
        }
        return (p.ExitCode, (stdout + "\r\n" + stderr).Trim());
    }
}
