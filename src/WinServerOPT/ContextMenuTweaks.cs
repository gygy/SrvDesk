using Microsoft.Win32;

namespace WinOpt;

/// <summary>资源管理器右键菜单扩展（对齐 Sophia / Optimizer 常见项）。</summary>
internal static class ContextMenuTweaks
{
    public static bool IsTakeOwnershipOn() => KeyExists(@"*\shell\WinOptTakeOwnership");
    public static bool IsOpenCmdOn() =>
        KeyExists(@"Directory\shell\WinOptOpenCmd")
        || KeyExists(@"Directory\Background\shell\WinOptOpenCmd")
        || KeyExists(@"Folder\shell\OpenDOSBox");

    public static bool IsOpenPowerShellOn() =>
        KeyExists(@"Directory\shell\WinOptOpenPS") || KeyExists(@"Directory\Background\shell\WinOptOpenPS");
    public static bool IsOpenPowerShellAdminOn() =>
        KeyExists(@"Directory\shell\WinOptOpenPSAdmin") || KeyExists(@"Directory\Background\shell\WinOptOpenPSAdmin");
    public static bool IsOpenTerminalOn() =>
        KeyExists(@"Directory\shell\WinOptOpenWT") || KeyExists(@"Directory\Background\shell\WinOptOpenWT");
    public static bool IsOpenTerminalAdminOn() =>
        KeyExists(@"Directory\shell\WinOptOpenWTAdmin") || KeyExists(@"Directory\Background\shell\WinOptOpenWTAdmin");
    public static bool IsCopyPathOn() => KeyExists(@"AllFilesystemObjects\shell\WinOptCopyPath");
    public static bool IsEditWithPaintOn() => KeyExists(@"SystemFileAssociations\image\shell\WinOptEditPaint");
    public static bool IsEditWithNotepadOn() => KeyExists(@"*\shell\WinOptEditNotepad");
    public static bool IsBlockAccessMenuOn() =>
        GetDword(@"Software\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked",
            "{f81e9010-6ea4-11ce-a7ff-00aa003ca9f6}") == 1;

    /// <summary>资源管理器右键「复制到文件夹 / 移动到文件夹」（两者同一开关）。</summary>
    public static bool IsCopyMoveToOn()
    {
        var copy = GetDefault(CopyToHandlerKey);
        var move = GetDefault(MoveToHandlerKey);
        return string.Equals(copy, CopyToClsid, StringComparison.OrdinalIgnoreCase)
            && string.Equals(move, MoveToClsid, StringComparison.OrdinalIgnoreCase);
    }

    private const string CopyToHandlerKey =
        @"AllFilesystemObjects\shellex\ContextMenuHandlers\Copy To";
    private const string MoveToHandlerKey =
        @"AllFilesystemObjects\shellex\ContextMenuHandlers\Move To";
    private const string CopyToClsid = "{C2FBB630-2971-11D1-A18C-00C04FD75D13}";
    private const string MoveToClsid = "{C2FBB631-2971-11D1-A18C-00C04FD75D13}";
    /// <summary>部分优化包使用的旧键（Folder\shell\OpenDOSBox）。</summary>
    private const string LegacyOpenCmdKey = @"Folder\shell\OpenDOSBox";

    public static void SetTakeOwnership(bool enable)
    {
        if (!enable) { DeleteTree(@"*\shell\WinOptTakeOwnership"); return; }
        SetShell(@"*\shell\WinOptTakeOwnership", "取得所有权",
            "cmd.exe /c takeown /f \"%1\" /r /d y & icacls \"%1\" /grant administrators:F /t");
    }

    public static void SetOpenCmd(bool enable)
    {
        // 关闭时同时清掉 WinOpt 键与常见优化包 OpenDOSBox 旧键，避免关不干净 / 重复菜单
        DeleteTree(@"Directory\shell\WinOptOpenCmd");
        DeleteTree(@"Directory\Background\shell\WinOptOpenCmd");
        DeleteTree(LegacyOpenCmdKey);
        if (!enable) return;

        // Directory + Background：文件夹本身与空白处均可；pushd "%V" 比 CD %1 更稳
        SetShell(@"Directory\shell\WinOptOpenCmd", "在此处打开命令提示符",
            "cmd.exe /s /k pushd \"%V\"");
        SetShell(@"Directory\Background\shell\WinOptOpenCmd", "在此处打开命令提示符",
            "cmd.exe /s /k pushd \"%V\"");
    }

    public static void SetOpenPowerShell(bool enable)
    {
        if (!enable)
        {
            DeleteTree(@"Directory\shell\WinOptOpenPS");
            DeleteTree(@"Directory\Background\shell\WinOptOpenPS");
            return;
        }
        SetShell(@"Directory\shell\WinOptOpenPS", "在此处打开 PowerShell",
            "powershell.exe -NoExit -Command \"Set-Location -LiteralPath '%V'\"");
        SetShell(@"Directory\Background\shell\WinOptOpenPS", "在此处打开 PowerShell",
            "powershell.exe -NoExit -Command \"Set-Location -LiteralPath '%V'\"");
    }

    public static void SetOpenPowerShellAdmin(bool enable)
    {
        if (!enable)
        {
            DeleteTree(@"Directory\shell\WinOptOpenPSAdmin");
            DeleteTree(@"Directory\Background\shell\WinOptOpenPSAdmin");
            return;
        }
        const string cmd =
            "powershell.exe -Command \"Start-Process powershell -Verb RunAs -ArgumentList '-NoExit -Command Set-Location -LiteralPath ''%V'''\"";
        SetShell(@"Directory\shell\WinOptOpenPSAdmin", "在此处打开 PowerShell（管理员）", cmd, luaShield: true);
        SetShell(@"Directory\Background\shell\WinOptOpenPSAdmin", "在此处打开 PowerShell（管理员）", cmd, luaShield: true);
    }

    public static void SetOpenTerminal(bool enable)
    {
        if (!enable)
        {
            DeleteTree(@"Directory\shell\WinOptOpenWT");
            DeleteTree(@"Directory\Background\shell\WinOptOpenWT");
            return;
        }
        if (!TerminalAvailable())
            throw new InvalidOperationException("未找到 Windows Terminal（wt.exe）。请先安装「Windows 终端」。");
        SetShell(@"Directory\shell\WinOptOpenWT", "在此处打开 Windows Terminal",
            "wt.exe -d \"%V\"");
        SetShell(@"Directory\Background\shell\WinOptOpenWT", "在此处打开 Windows Terminal",
            "wt.exe -d \"%V\"");
    }

    public static void SetOpenTerminalAdmin(bool enable)
    {
        if (!enable)
        {
            DeleteTree(@"Directory\shell\WinOptOpenWTAdmin");
            DeleteTree(@"Directory\Background\shell\WinOptOpenWTAdmin");
            return;
        }
        if (!TerminalAvailable())
            throw new InvalidOperationException("未找到 Windows Terminal（wt.exe）。请先安装「Windows 终端」。");
        const string cmd =
            "powershell.exe -NoProfile -Command \"Start-Process wt.exe -ArgumentList '-d','%V' -Verb RunAs\"";
        SetShell(@"Directory\shell\WinOptOpenWTAdmin", "在此处打开 Windows Terminal（管理员）", cmd, luaShield: true);
        SetShell(@"Directory\Background\shell\WinOptOpenWTAdmin", "在此处打开 Windows Terminal（管理员）", cmd, luaShield: true);
    }

    public static void SetCopyPath(bool enable)
    {
        if (!enable) { DeleteTree(@"AllFilesystemObjects\shell\WinOptCopyPath"); return; }
        SetShell(@"AllFilesystemObjects\shell\WinOptCopyPath", "复制完整路径",
            "powershell.exe -NoProfile -Command \"Set-Clipboard -Value '%1'\"");
    }

    /// <summary>开启/关闭右键「复制到文件夹」与「移动到文件夹」（对齐经典 shellex CLSID）。</summary>
    public static void SetCopyMoveTo(bool enable)
    {
        if (!enable)
        {
            DeleteTree(CopyToHandlerKey);
            DeleteTree(MoveToHandlerKey);
            return;
        }

        SetHandlerDefault(CopyToHandlerKey, CopyToClsid, "右键「复制到文件夹」");
        SetHandlerDefault(MoveToHandlerKey, MoveToClsid, "右键「移动到文件夹」");
    }

    private static void SetHandlerDefault(string relative, string clsid, string logTitle)
    {
        var old = GetDefault(relative);
        ApplyLog.RegistryKeyWrite("HKCR", relative, $"{logTitle} @={clsid}（原={old ?? "(无)"}）");
        using var k = Registry.ClassesRoot.CreateSubKey(relative)
            ?? throw new InvalidOperationException("无法写入：" + relative);
        k.SetValue("", clsid);
    }

    private static string? GetDefault(string relative)
    {
        using var k = Registry.ClassesRoot.OpenSubKey(relative);
        return k?.GetValue("") as string;
    }

    public static void SetEditWithPaint(bool enable)
    {
        if (!enable) { DeleteTree(@"SystemFileAssociations\image\shell\WinOptEditPaint"); return; }
        SetShell(@"SystemFileAssociations\image\shell\WinOptEditPaint", "用画图编辑",
            "mspaint.exe \"%1\"");
    }

    public static void SetEditWithNotepad(bool enable)
    {
        if (!enable) { DeleteTree(@"*\shell\WinOptEditNotepad"); return; }
        SetShell(@"*\shell\WinOptEditNotepad", "用记事本编辑",
            "notepad.exe \"%1\"");
    }

    /// <summary>屏蔽「授予访问权限 / 共享」相关 shell 扩展（常见干扰项）。</summary>
    public static void SetBlockAccessMenu(bool block)
    {
        const string key = @"Software\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked";
        const string clsid = "{f81e9010-6ea4-11ce-a7ff-00aa003ca9f6}";
        object? old;
        using (var r = Registry.LocalMachine.OpenSubKey(key))
            old = r?.GetValue(clsid);
        using (ApplyLog.PushContext("屏蔽共享/授予访问权限菜单"))
        {
            if (block)
                ApplyLog.RegistryDword("HKLM", key, clsid, old, 1);
            else
                ApplyLog.RegistryDelete("HKLM", key, clsid, old);
        }
        using var k = Registry.LocalMachine.CreateSubKey(key);
        if (block) k?.SetValue(clsid, 1, RegistryValueKind.DWord);
        else k?.DeleteValue(clsid, throwOnMissingValue: false);
    }

    public static bool TerminalAvailable()
    {
        try
        {
            var path = Environment.GetEnvironmentVariable("PATH") ?? "";
            foreach (var dir in path.Split(';'))
            {
                if (string.IsNullOrWhiteSpace(dir)) continue;
                if (File.Exists(Path.Combine(dir.Trim(), "wt.exe"))) return true;
            }
            var local = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                @"Microsoft\WindowsApps\wt.exe");
            return File.Exists(local);
        }
        catch { return false; }
    }

    private static void SetShell(string path, string title, string command, bool luaShield = false)
    {
        ApplyLog.RegistryKeyWrite("HKCR", path,
            $"默认值=\"{title}\"; command=\"{command}\"" + (luaShield ? "; HasLUAShield" : ""));
        using var k = Registry.ClassesRoot.CreateSubKey(path)
            ?? throw new InvalidOperationException("无法写入：" + path);
        k.SetValue("", title);
        if (luaShield) k.SetValue("HasLUAShield", "");
        using var cmd = Registry.ClassesRoot.CreateSubKey(path + @"\command");
        cmd?.SetValue("", command);
    }

    private static bool KeyExists(string relative)
    {
        using var k = Registry.ClassesRoot.OpenSubKey(relative);
        return k is not null;
    }

    private static void DeleteTree(string relative)
    {
        var existed = KeyExists(relative);
        ApplyLog.RegistryDeleteTree("HKCR", relative, existed);
        try { Registry.ClassesRoot.DeleteSubKeyTree(relative, throwOnMissingSubKey: false); }
        catch { /* ignore */ }
    }

    private static int GetDword(string key, string name)
    {
        using var k = Registry.LocalMachine.OpenSubKey(key);
        return k?.GetValue(name) is int i ? i : -1;
    }
}
