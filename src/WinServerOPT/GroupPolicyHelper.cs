using System.Diagnostics;
using System.Text;

namespace WinOpt;

internal sealed class GpUpdateResult
{
    public int ExitCode { get; set; }
    public string Output { get; set; } = "";
    public bool RebootRequired { get; set; }
    public bool Success => ExitCode is 0 or 1 or 2 or 3;
}

internal static class GroupPolicyHelper
{
    public static bool IsGpeditAvailable()
    {
        var sys = Environment.GetFolderPath(Environment.SpecialFolder.System);
        return File.Exists(Path.Combine(sys, "gpedit.msc"));
    }

    public static void OpenEditor()
    {
        if (!IsGpeditAvailable())
            throw new InvalidOperationException(
                "本机未找到 gpedit.msc。Server Core 或未安装「组策略管理」功能时不可用。");

        Process.Start(new ProcessStartInfo
        {
            FileName = "gpedit.msc",
            UseShellExecute = true,
        });
        ApplyLog.Write("打开组策略编辑器");
    }

    public static GpUpdateResult ForceUpdate()
    {
        ApplyLog.Write("强制更新组策略 gpupdate /force");
        using var p = Process.Start(new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "gpupdate.exe"),
            Arguments = "/force",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        }) ?? throw new InvalidOperationException("无法启动 gpupdate.exe");

        var output = new StringBuilder();
        output.AppendLine(p.StandardOutput.ReadToEnd());
        output.AppendLine(p.StandardError.ReadToEnd());
        p.WaitForExit(120_000);

        var text = output.ToString().Trim();
        return new GpUpdateResult
        {
            ExitCode = p.ExitCode,
            Output = text.Length > 0 ? text : DescribeExitCode(p.ExitCode),
            RebootRequired = p.ExitCode is 2 or 3
                || text.IndexOf("reboot", StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf("重新启动", StringComparison.OrdinalIgnoreCase) >= 0
                || text.IndexOf("重启", StringComparison.OrdinalIgnoreCase) >= 0,
        };
    }

    private static string DescribeExitCode(int code) => code switch
    {
        0 => "组策略更新成功完成。",
        1 => "组策略更新成功，但未检测到策略变更。",
        2 => "用户策略更新成功，需要重新启动或注销才能生效。",
        3 => "计算机策略更新成功，需要重新启动才能生效。",
        _ => "gpupdate 退出码：" + code,
    };
}
