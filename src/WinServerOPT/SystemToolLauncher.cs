using System.Diagnostics;

namespace WinOpt;

internal static class SystemToolLauncher
{
    private static readonly string SystemDir =
        Environment.GetFolderPath(Environment.SpecialFolder.System);

    public static void OpenCommandPrompt(IWin32Window? owner) =>
        OpenExecutable(owner, Path.Combine(SystemDir, "cmd.exe"), "", "命令提示符");

    public static void OpenWindowsPowerShell(IWin32Window? owner) =>
        OpenExecutable(owner,
            Path.Combine(SystemDir, "WindowsPowerShell", "v1.0", "powershell.exe"),
            "-NoExit -Command \"Write-Host 'Win一键优化 · Windows PowerShell' -ForegroundColor Cyan\"",
            "Windows PowerShell");

    public static void OpenTaskScheduler(IWin32Window? owner) =>
        OpenMsc(owner, "taskschd.msc", "计划任务");

    public static void OpenComputerManagement(IWin32Window? owner) =>
        OpenMsc(owner, "compmgmt.msc", "计算机管理");

    private static void OpenMsc(IWin32Window? owner, string mscName, string title)
    {
        var path = Path.Combine(SystemDir, mscName);
        if (!File.Exists(path))
        {
            ShowError(owner, title, "找不到 " + path);
            return;
        }

        OpenExecutable(owner, path, "", title);
    }

    private static void OpenExecutable(IWin32Window? owner, string fileName, string arguments, string title)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = true,
            });
            ApplyLog.Write("打开" + title);
        }
        catch (Exception ex)
        {
            ShowError(owner, title, ex.Message);
        }
    }

    private static void ShowError(IWin32Window? owner, string title, string message) =>
        MessageBox.Show(owner, $"无法打开「{title}」。\r\n\r\n{message}", title,
            MessageBoxButtons.OK, MessageBoxIcon.Warning);
}
