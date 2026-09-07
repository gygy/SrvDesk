using Microsoft.Win32;

namespace SrvDesk;

/// <summary>资源管理器右键菜单扩展（对齐 Sophia / Optimizer 常见项）。</summary>
internal static class ContextMenuTweaks
{
    public static bool IsTakeOwnershipOn() => KeyExists(@"*\shell\SrvDeskTakeOwnership");
    public static bool IsOpenCmdOn() =>
        KeyExists(@"Directory\shell\SrvDeskOpenCmd")
        || KeyExists(@"Directory\Background\shell\SrvDeskOpenCmd")
        || KeyExists(@"Folder\shell\OpenDOSBox");

    public static bool IsOpenPowerShellOn() =>
        KeyExists(@"Directory\shell\SrvDeskOpenPS") || KeyExists(@"Directory\Background\shell\SrvDeskOpenPS");
    public static bool IsOpenPowerShellAdminOn() =>
        KeyExists(@"Directory\shell\SrvDeskOpenPSAdmin") || KeyExists(@"Directory\Background\shell\SrvDeskOpenPSAdmin");
    public static bool IsOpenTerminalOn() =>
        KeyExists(@"Directory\shell\SrvDeskOpenWT") || KeyExists(@"Directory\Background\shell\SrvDeskOpenWT");
    public static bool IsOpenTerminalAdminOn() =>
        KeyExists(@"Directory\shell\SrvDeskOpenWTAdmin") || KeyExists(@"Directory\Background\shell\SrvDeskOpenWTAdmin");
    public static bool IsCopyPathOn() => KeyExists(@"AllFilesystemObjects\shell\SrvDeskCopyPath");
    public static bool IsEditWithPaintOn() => KeyExists(@"SystemFileAssociations\image\shell\SrvDeskEditPaint");
    public static bool IsEditWithNotepadOn() => KeyExists(@"*\shell\SrvDeskEditNotepad");
    public static bool IsBlockAccessMenuOn() =>
        GetDword(@"Software\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked",
            "{f81e9010-6ea4-11ce-a7ff-00aa003ca9f6}") == 1;

    /// <summary>桌面/文件夹空白处「快捷操作组」（对齐 QwhMenu .reg）。</summary>
    public static bool IsQuickOpsMenuOn() =>
        KeyExists(@"Directory\Background\shell\QwhMenu")
        || KeyExists(@"LibraryFolder\Background\shell\QwhMenu")
        || KeyExists(@"*\shell\QwhMenu");

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
        if (!enable) { DeleteTree(@"*\shell\SrvDeskTakeOwnership"); return; }
        SetShell(@"*\shell\SrvDeskTakeOwnership", "取得所有权",
            "cmd.exe /c takeown /f \"%1\" /r /d y & icacls \"%1\" /grant administrators:F /t");
    }

    public static void SetOpenCmd(bool enable)
    {
        // 关闭时同时清掉 SrvDesk/旧 SrvDesk 键与常见优化包 OpenDOSBox 旧键，避免关不干净 / 重复菜单
        DeleteTree(@"Directory\shell\SrvDeskOpenCmd");
        DeleteTree(@"Directory\Background\shell\SrvDeskOpenCmd");
        DeleteTree(LegacyOpenCmdKey);
        if (!enable) return;

        // Directory + Background：文件夹本身与空白处均可；pushd "%V" 比 CD %1 更稳
        SetShell(@"Directory\shell\SrvDeskOpenCmd", "在此处打开命令提示符",
            "cmd.exe /s /k pushd \"%V\"");
        SetShell(@"Directory\Background\shell\SrvDeskOpenCmd", "在此处打开命令提示符",
            "cmd.exe /s /k pushd \"%V\"");
    }

    public static void SetOpenPowerShell(bool enable)
    {
        if (!enable)
        {
            DeleteTree(@"Directory\shell\SrvDeskOpenPS");
            DeleteTree(@"Directory\Background\shell\SrvDeskOpenPS");
            return;
        }
        SetShell(@"Directory\shell\SrvDeskOpenPS", "在此处打开 PowerShell",
            "powershell.exe -NoExit -Command \"Set-Location -LiteralPath '%V'\"");
        SetShell(@"Directory\Background\shell\SrvDeskOpenPS", "在此处打开 PowerShell",
            "powershell.exe -NoExit -Command \"Set-Location -LiteralPath '%V'\"");
    }

    public static void SetOpenPowerShellAdmin(bool enable)
    {
        if (!enable)
        {
            DeleteTree(@"Directory\shell\SrvDeskOpenPSAdmin");
            DeleteTree(@"Directory\Background\shell\SrvDeskOpenPSAdmin");
            return;
        }
        const string cmd =
            "powershell.exe -Command \"Start-Process powershell -Verb RunAs -ArgumentList '-NoExit -Command Set-Location -LiteralPath ''%V'''\"";
        SetShell(@"Directory\shell\SrvDeskOpenPSAdmin", "在此处打开 PowerShell（管理员）", cmd, luaShield: true);
        SetShell(@"Directory\Background\shell\SrvDeskOpenPSAdmin", "在此处打开 PowerShell（管理员）", cmd, luaShield: true);
    }

    public static void SetOpenTerminal(bool enable)
    {
        if (!enable)
        {
            DeleteTree(@"Directory\shell\SrvDeskOpenWT");
            DeleteTree(@"Directory\Background\shell\SrvDeskOpenWT");
            return;
        }
        if (!TerminalAvailable())
            throw new InvalidOperationException("未找到 Windows Terminal（wt.exe）。请先安装「Windows 终端」。");
        SetShell(@"Directory\shell\SrvDeskOpenWT", "在此处打开 Windows Terminal",
            "wt.exe -d \"%V\"");
        SetShell(@"Directory\Background\shell\SrvDeskOpenWT", "在此处打开 Windows Terminal",
            "wt.exe -d \"%V\"");
    }

    public static void SetOpenTerminalAdmin(bool enable)
    {
        if (!enable)
        {
            DeleteTree(@"Directory\shell\SrvDeskOpenWTAdmin");
            DeleteTree(@"Directory\Background\shell\SrvDeskOpenWTAdmin");
            return;
        }
        if (!TerminalAvailable())
            throw new InvalidOperationException("未找到 Windows Terminal（wt.exe）。请先安装「Windows 终端」。");
        const string cmd =
            "powershell.exe -NoProfile -Command \"Start-Process wt.exe -ArgumentList '-d','%V' -Verb RunAs\"";
        SetShell(@"Directory\shell\SrvDeskOpenWTAdmin", "在此处打开 Windows Terminal（管理员）", cmd, luaShield: true);
        SetShell(@"Directory\Background\shell\SrvDeskOpenWTAdmin", "在此处打开 Windows Terminal（管理员）", cmd, luaShield: true);
    }

    public static void SetCopyPath(bool enable)
    {
        if (!enable) { DeleteTree(@"AllFilesystemObjects\shell\SrvDeskCopyPath"); return; }
        SetShell(@"AllFilesystemObjects\shell\SrvDeskCopyPath", "复制完整路径",
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
        if (!enable) { DeleteTree(@"SystemFileAssociations\image\shell\SrvDeskEditPaint"); return; }
        SetShell(@"SystemFileAssociations\image\shell\SrvDeskEditPaint", "用画图编辑",
            "mspaint.exe \"%1\"");
    }

    public static void SetEditWithNotepad(bool enable)
    {
        if (!enable) { DeleteTree(@"*\shell\SrvDeskEditNotepad"); return; }
        SetShell(@"*\shell\SrvDeskEditNotepad", "用记事本编辑",
            "notepad.exe \"%1\"");
    }

    /// <summary>
    /// 空白处右键「快捷操作组」（对齐「桌面和文件夹空白处右键菜单添加快捷操作组」.reg /
    /// 「删除右键快捷操作组菜单」.reg；键名 QwhMenu + CommandStore）。
    /// </summary>
    public static void SetQuickOpsMenu(bool enable)
    {
        // 关闭时先清级联菜单（与删除 .reg 一致）；CommandStore 条目保留无害，开启时再覆盖写入
        DeleteTree(@"Directory\Background\shell\QwhMenu");
        DeleteTree(@"LibraryFolder\Background\shell\QwhMenu");
        DeleteTree(@"*\shell\QwhMenu");
        if (!enable) return;

        const string dirSubs =
            "My Computer;My Documents;Control Panel;AddremoveRro;Command Prompt;Wordpad;Notepad;Paint;Calculator;Regedit;Restart Explorer";
        const string libSubs =
            "My Computer;Control Panel;AddremoveRro;Command Prompt;Wordpad;Notepad;Paint;Calculator;Regedit;Restart Explorer";

        SetCascadeMenu(@"Directory\Background\shell\QwhMenu", dirSubs);
        SetCascadeMenu(@"LibraryFolder\Background\shell\QwhMenu", libSubs);

        SetCommandStore("Calculator", "计算器", "calc.exe", "calc.exe");
        SetCommandStore("Command Prompt", "命令提示符", "cmd.exe", "cmd.exe");
        SetCommandStore("Control Panel", "控制面板", "shell32.dll,21",
            "rundll32.exe shell32.dll,Control_RunDLL");
        SetCommandStore("My Computer", "此电脑", "imageres.dll,105",
            "explorer.exe /e,::{20D04FE0-3AEA-1069-A2D8-08002B30309D}");
        SetCommandStore("My Documents", "我的文档", "shell32.dll,4",
            "explorer.exe /e,::{450D8FBA-AD25-11D0-98A8-0800361B1103}");
        SetCommandStore("Notepad", "记事本", "notepad.exe", "notepad.exe");
        SetCommandStore("Paint", "画图", "mspaint.exe", "mspaint.exe");
        SetCommandStore("Regedit", "注册表编辑器", "Regedit.exe", "Regedit.exe");
        SetCommandStore("Restart Explorer", "重启资源管理器", "shell32.dll,238", "tskill explorer");
        SetCommandStore("Wordpad", "写字板",
            @"%ProgramFiles%\Windows NT\Accessories\wordpad.exe", "wordpad.exe",
            iconExpand: true);
        SetCommandStore("AddremoveRro", "添加或删除程序", "shell32.dll,162",
            "rundll32.exe shell32.dll,Control_RunDLL appwiz.cpl",
            commandExpand: true);
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

    private static void SetCascadeMenu(string path, string subCommands)
    {
        ApplyLog.RegistryKeyWrite("HKCR", path, $"快捷操作组 MUIVerb；SubCommands={subCommands}");
        using var k = Registry.ClassesRoot.CreateSubKey(path)
            ?? throw new InvalidOperationException("无法写入：" + path);
        k.SetValue("Position", "top");
        k.SetValue("Icon", "shell32.dll,319");
        k.SetValue("MUIVerb", "快捷操作组");
        k.SetValue("SubCommands", subCommands);
    }

    /// <summary>Explorer CommandStore 项（HKLM，供 SubCommands 引用）。</summary>
    private static void SetCommandStore(
        string name, string title, string icon, string command,
        bool iconExpand = false, bool commandExpand = false)
    {
        const string root = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\CommandStore\shell";
        var path = root + @"\" + name;
        ApplyLog.RegistryKeyWrite("HKLM", path, $"@=\"{title}\"; command=\"{command}\"");
        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var k = baseKey.CreateSubKey(path, true)
            ?? throw new InvalidOperationException("无法写入：" + path);
        k.SetValue("", title);
        k.SetValue("icon", icon,
            iconExpand ? RegistryValueKind.ExpandString : RegistryValueKind.String);
        using var cmd = baseKey.CreateSubKey(path + @"\command", true);
        cmd?.SetValue("", command,
            commandExpand ? RegistryValueKind.ExpandString : RegistryValueKind.String);
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
