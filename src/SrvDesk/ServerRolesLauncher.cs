using System.Diagnostics;

namespace SrvDesk;

/// <summary>打开 Windows Server「添加 / 删除角色和功能」（经服务器管理器）。</summary>
internal static class ServerRolesLauncher
{
    public static string? ServerManagerPath()
    {
        var p = Path.Combine(Environment.SystemDirectory, "ServerManager.exe");
        return File.Exists(p) ? p : null;
    }

    public static bool IsAvailable =>
        SystemInfoHelper.Detect().IsServer && ServerManagerPath() is not null;

    /// <summary>打开服务器管理器；非 Server 或未安装时返回 false。</summary>
    public static bool TryOpenServerManager(IWin32Window? owner, out string error)
    {
        error = "";
        var facts = SystemInfoHelper.Detect();
        if (!facts.IsServer)
        {
            error = AppLang.L(
                "「角色和功能」仅 Windows Server 提供。本机可改用「可选功能 / Capabilities」。",
                "Roles and Features are Server-only. Use Optional features / Capabilities on this PC.");
            return false;
        }

        var path = ServerManagerPath();
        if (path is null)
        {
            error = AppLang.L(
                "未找到 ServerManager.exe。",
                "ServerManager.exe was not found.");
            return false;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
            });
            ApplyLog.Write("打开服务器管理器（添加/删除角色和功能）");
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static void OpenFromMenu(IWin32Window owner, Action? openOptionalFeatures = null)
    {
        if (TryOpenServerManager(owner, out var err))
            return;

        if (openOptionalFeatures is not null
            && MessageBox.Show(
                owner,
                err + "\r\n\r\n" + AppLang.L("是否打开「可选功能」？", "Open Optional features instead?"),
                AppLang.L("添加 / 删除角色和功能", "Add / Remove Roles and Features"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information) == DialogResult.Yes)
        {
            openOptionalFeatures();
            return;
        }

        MessageBox.Show(
            owner,
            err,
            AppLang.L("添加 / 删除角色和功能", "Add / Remove Roles and Features"),
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }
}
