using Microsoft.Win32;

namespace SrvDesk;

internal enum ContextMenuKind
{
    Shell,
    ShellEx,
}

/// <summary>已安装右键菜单项（扫描结果，可安全启停）。</summary>
internal sealed class ContextMenuEntry
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Scene { get; init; }
    public required ContextMenuKind Kind { get; init; }
    public required string Source { get; init; }
    public required string Advice { get; init; }
    public required string RegistryPath { get; init; }
    public string? Clsid { get; init; }
    public bool Protected { get; init; }
    public bool Enabled { get; set; }

    public string KindText => Kind == ContextMenuKind.Shell
        ? AppLang.L("命令", "Shell")
        : AppLang.L("扩展", "ShellEx");
}

/// <summary>
/// 扫描 HKCR 常见 shell / shellex，用 LegacyDisable / Blocked 安全启停（不删键）。
/// 相对纯管理器：给出 Server 精简建议与来源标签。
/// </summary>
internal static class ContextMenuScanHelper
{
    private const string BlockedKey =
        @"Software\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked";

    private static readonly (string Relative, string Scene)[] ShellRoots =
    [
        (@"*\shell", AppLang.L("文件", "File")),
        (@"Directory\shell", AppLang.L("文件夹", "Folder")),
        (@"Directory\Background\shell", AppLang.L("空白处", "Background")),
        (@"Folder\shell", AppLang.L("文件夹(Folder)", "Folder key")),
        (@"Drive\shell", AppLang.L("驱动器", "Drive")),
        (@"AllFilesystemObjects\shell", AppLang.L("所有对象", "All objects")),
        (@"LibraryFolder\Background\shell", AppLang.L("库空白处", "Library bg")),
        (@"DesktopBackground\Shell", AppLang.L("桌面空白", "Desktop bg")),
    ];

    private static readonly (string Relative, string Scene)[] ShellExRoots =
    [
        (@"*\shellex\ContextMenuHandlers", AppLang.L("文件", "File")),
        (@"Directory\shellex\ContextMenuHandlers", AppLang.L("文件夹", "Folder")),
        (@"Directory\Background\shellex\ContextMenuHandlers", AppLang.L("空白处", "Background")),
        (@"Folder\shellex\ContextMenuHandlers", AppLang.L("文件夹(Folder)", "Folder key")),
        (@"Drive\shellex\ContextMenuHandlers", AppLang.L("驱动器", "Drive")),
        (@"AllFilesystemObjects\shellex\ContextMenuHandlers", AppLang.L("所有对象", "All objects")),
        (@"Directory\Background\shellex\ContextMenuHandlers", AppLang.L("空白处", "Background")),
    ];

    private static readonly HashSet<string> ProtectedShellNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "open", "explore", "opennewwindow", "opennewprocess", "find",
        "cut", "copy", "paste", "pastelink", "delete", "rename", "link",
        "properties", "runas", "runasuser", "pintohome", "unpinfromhome",
        "pintostartscreen", "taskbarpin", "windows.modernshare",
        "windows.share", "cmd", "powershell", "powershellise",
    };

    private static readonly HashSet<string> KnownMsClsid = new(StringComparer.OrdinalIgnoreCase)
    {
        "{093FFA82-F46A-47E1-95AA-323E57004034}", // Sharing
        "{f81e9010-6ea4-11ce-a7ff-00aa003ca9f6}", // Sharing / Access
        "{7BA4C740-9E81-11CF-99D3-00AA004AE837}", // Send To
        "{C2FBB630-2971-11D1-A18C-00C04FD75D13}", // Copy To
        "{C2FBB631-2971-11D1-A18C-00C04FD75D13}", // Move To
        "{090502A0-748D-41B2-8A1B-E59C6D4CAD47}", // Encryption
        "{474C98EE-CF3D-41f5-80E3-4EFAC6C28CE1}", // Work Folders
        "{596AB062-B4D2-4215-9F74-E9109B0A8153}", // Previous Versions
        "{E357FCCD-A995-4576-B01F-234630154E96}", // Photo thumbnail
        "{B298D29A-A6ED-11DE-BA8C-A68E55D89593}", // Open with
    };

    private static readonly HashSet<string> ClutterShellHints = new(StringComparer.OrdinalIgnoreCase)
    {
        "share", "cast", "onedrive", "dropbox", "baidu", "360", "qq", "wechat",
        "editwith", "git", "vscode", "notepad++", "winrar", "7-zip", "bandizip",
        "adobe", "nvidia", "intel", "vlc", "potplayer",
    };

    public static IReadOnlyList<ContextMenuEntry> ScanAll()
    {
        var list = new List<ContextMenuEntry>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (root, scene) in ShellRoots)
            ScanShell(root, scene, list, seen);

        foreach (var (root, scene) in ShellExRoots)
            ScanShellEx(root, scene, list, seen);

        return list
            .OrderBy(e => e.Scene, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(e => e.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public static int ApplyServerSlim(IEnumerable<ContextMenuEntry> entries)
    {
        var n = 0;
        foreach (var e in entries)
        {
            if (e.Protected || !e.Enabled) continue;
            if (!IsSlimCandidate(e)) continue;
            try
            {
                SetEnabled(e, false);
                e.Enabled = false;
                n++;
            }
            catch
            {
                /* 单项失败继续 */
            }
        }

        return n;
    }

    public static bool IsSlimCandidate(ContextMenuEntry e) =>
        !e.Protected
        && (e.Source == AppLang.L("第三方", "Third-party")
            || e.Advice == AppLang.L("可精简", "Can slim")
            || string.Equals(e.Name, "授予访问权限", StringComparison.OrdinalIgnoreCase)
            || (e.Clsid is not null && string.Equals(e.Clsid, "{f81e9010-6ea4-11ce-a7ff-00aa003ca9f6}", StringComparison.OrdinalIgnoreCase)));

    public static void SetEnabled(ContextMenuEntry entry, bool enable)
    {
        if (entry.Protected)
            throw new InvalidOperationException(AppLang.L("系统核心项不可禁用。", "Protected system item cannot be disabled."));

        if (entry.Kind == ContextMenuKind.Shell)
            SetShellEnabled(entry.RegistryPath, enable);
        else
            SetShellExEnabled(entry.RegistryPath, entry.Clsid, enable);

        ApplyLog.Write(AppLang.Lf(
            "右键菜单{0}：{1} ({2})",
            "Context menu {0}: {1} ({2})",
            enable ? AppLang.L("启用", "enable") : AppLang.L("禁用", "disable"),
            entry.Name,
            entry.RegistryPath));
    }

    public static void OpenInRegedit(ContextMenuEntry entry)
    {
        var full = @"HKEY_CLASSES_ROOT\" + entry.RegistryPath.Replace('/', '\\');
        try
        {
            using (var k = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Applets\Regedit"))
                k?.SetValue("LastKey", full);
            Process.Start(new ProcessStartInfo("regedit.exe") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(AppLang.L("无法打开注册表编辑器：", "Cannot open Registry Editor: ") + ex.Message);
        }
    }

    private static void ScanShell(string root, string scene, List<ContextMenuEntry> list, HashSet<string> seen)
    {
        using var key = OpenClasses(root);
        if (key is null) return;

        foreach (var name in key.GetSubKeyNames())
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            if (name.StartsWith("Windows.", StringComparison.OrdinalIgnoreCase)
                && ProtectedShellNames.Contains(name))
                continue;

            var path = root + @"\" + name;
            if (!seen.Add("shell:" + path)) continue;

            using var sub = key.OpenSubKey(name);
            if (sub is null) continue;
            if (sub.GetValue("ProgrammaticAccessOnly") is not null) continue;

            var display = ResolveDisplayName(sub, name);
            var protectedItem = IsProtectedShell(name);
            var source = ClassifySource(name, display, null);
            var enabled = sub.GetValue("LegacyDisable") is null;
            var advice = BuildAdvice(protectedItem, source, display, name, isShellEx: false);

            list.Add(new ContextMenuEntry
            {
                Id = "shell:" + path,
                Name = display,
                Scene = scene,
                Kind = ContextMenuKind.Shell,
                Source = source,
                Advice = advice,
                RegistryPath = path,
                Protected = protectedItem,
                Enabled = enabled,
            });
        }
    }

    private static void ScanShellEx(string root, string scene, List<ContextMenuEntry> list, HashSet<string> seen)
    {
        using var key = OpenClasses(root);
        if (key is null) return;

        foreach (var name in key.GetSubKeyNames())
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            var path = root + @"\" + name;
            if (!seen.Add("shellex:" + path)) continue;

            using var sub = key.OpenSubKey(name);
            if (sub is null) continue;

            var clsid = (sub.GetValue("") as string)?.Trim() ?? "";
            if (clsid.Length == 0) continue;

            var disabledByDash = clsid.StartsWith("-", StringComparison.Ordinal);
            var clsidNorm = disabledByDash ? clsid.TrimStart('-').Trim() : clsid;
            if (!clsidNorm.StartsWith("{", StringComparison.Ordinal)) continue;

            var display = ResolveShellExName(name, clsidNorm);
            var blocked = IsBlocked(clsidNorm);
            var enabled = !disabledByDash && !blocked;
            var source = ClassifySource(name, display, clsidNorm);
            var protectedItem = IsProtectedShellEx(name, clsidNorm);
            var advice = BuildAdvice(protectedItem, source, display, name, isShellEx: true);

            list.Add(new ContextMenuEntry
            {
                Id = "shellex:" + path,
                Name = display,
                Scene = scene,
                Kind = ContextMenuKind.ShellEx,
                Source = source,
                Advice = advice,
                RegistryPath = path,
                Clsid = clsidNorm,
                Protected = protectedItem,
                Enabled = enabled,
            });
        }
    }

    private static void SetShellEnabled(string relative, bool enable)
    {
        using var k = Registry.ClassesRoot.OpenSubKey(relative, writable: true)
            ?? throw new InvalidOperationException(AppLang.L("无法打开：", "Cannot open: ") + relative);
        if (enable)
            k.DeleteValue("LegacyDisable", throwOnMissingValue: false);
        else
            k.SetValue("LegacyDisable", "", RegistryValueKind.String);
    }

    private static void SetShellExEnabled(string relative, string? clsid, bool enable)
    {
        if (string.IsNullOrWhiteSpace(clsid))
            throw new InvalidOperationException(AppLang.L("缺少 CLSID。", "Missing CLSID."));

        // 优先走 Blocked（可逆、不改供应商键值）；同时清理默认值前的 "-" 脏状态
        SetBlocked(clsid, !enable);

        using var k = Registry.ClassesRoot.OpenSubKey(relative, writable: true);
        if (k is null) return;
        var cur = (k.GetValue("") as string)?.Trim() ?? "";
        if (enable)
        {
            if (cur.StartsWith("-", StringComparison.Ordinal))
                k.SetValue("", cur.TrimStart('-').Trim());
        }
        else
        {
            // Blocked 已足够；若键仅有本地权限问题再尝试破折号
            if (!cur.StartsWith("-", StringComparison.Ordinal) && !IsBlocked(clsid))
                k.SetValue("", "-" + clsid);
        }
    }

    private static void SetBlocked(string clsid, bool block)
    {
        using var k = Registry.LocalMachine.CreateSubKey(BlockedKey)
            ?? throw new InvalidOperationException(AppLang.L("无法写入 Blocked。", "Cannot write Blocked."));
        if (block)
            k.SetValue(clsid, 1, RegistryValueKind.DWord);
        else
            k.DeleteValue(clsid, throwOnMissingValue: false);
    }

    private static bool IsBlocked(string clsid)
    {
        using var k = Registry.LocalMachine.OpenSubKey(BlockedKey);
        return k?.GetValue(clsid) is not null;
    }

    private static RegistryKey? OpenClasses(string relative)
    {
        try { return Registry.ClassesRoot.OpenSubKey(relative); }
        catch { return null; }
    }

    private static string ResolveDisplayName(RegistryKey sub, string keyName)
    {
        foreach (var n in new[] { "MUIVerb", "MuteVerb", "" })
        {
            var v = sub.GetValue(n) as string;
            if (string.IsNullOrWhiteSpace(v)) continue;
            var resolved = TryExpandMui(v.Trim());
            if (!string.IsNullOrWhiteSpace(resolved))
                return resolved!;
        }

        return PrettifyKeyName(keyName);
    }

    private static string ResolveShellExName(string keyName, string clsid)
    {
        var pretty = PrettifyKeyName(keyName);
        if (!pretty.Equals(keyName, StringComparison.Ordinal)
            || !Guid.TryParse(keyName.Trim('{', '}'), out _))
            return pretty;

        try
        {
            using var k = Registry.ClassesRoot.OpenSubKey(@"CLSID\" + clsid);
            var name = k?.GetValue("") as string;
            if (!string.IsNullOrWhiteSpace(name))
                return TryExpandMui(name.Trim()) ?? name.Trim();
        }
        catch { /* ignore */ }

        return pretty;
    }

    private static string? TryExpandMui(string value)
    {
        if (!value.StartsWith("@", StringComparison.Ordinal))
            return value;
        try
        {
            var sb = new System.Text.StringBuilder(512);
            var hr = SHLoadIndirectString(value, sb, sb.Capacity, IntPtr.Zero);
            if (hr == 0 && sb.Length > 0)
                return sb.ToString();
        }
        catch { /* ignore */ }
        return value;
    }

    private static string PrettifyKeyName(string name)
    {
        if (name.StartsWith("SrvDesk", StringComparison.OrdinalIgnoreCase))
            return name["SrvDesk".Length..];
        if (name.StartsWith("WinOpt", StringComparison.OrdinalIgnoreCase))
            return name["WinOpt".Length..];
        return name;
    }

    private static bool IsProtectedShell(string name) =>
        ProtectedShellNames.Contains(name)
        || name.StartsWith("Windows.", StringComparison.OrdinalIgnoreCase);

    private static bool IsProtectedShellEx(string name, string clsid)
    {
        if (name.Equals("Copy To", StringComparison.OrdinalIgnoreCase)
            || name.Equals("Move To", StringComparison.OrdinalIgnoreCase)
            || name.Equals("ModernSharing", StringComparison.OrdinalIgnoreCase))
            return false;
        // 未知名系统扩展：有已知 MS CLSID 且不在干扰名单 → 保护
        return KnownMsClsid.Contains(clsid)
               && !clsid.Equals("{f81e9010-6ea4-11ce-a7ff-00aa003ca9f6}", StringComparison.OrdinalIgnoreCase)
               && !clsid.Equals("{093FFA82-F46A-47E1-95AA-323E57004034}", StringComparison.OrdinalIgnoreCase);
    }

    private static string ClassifySource(string keyName, string display, string? clsid)
    {
        if (keyName.StartsWith("SrvDesk", StringComparison.OrdinalIgnoreCase)
            || keyName.StartsWith("WinOpt", StringComparison.OrdinalIgnoreCase)
            || keyName.Equals("QwhMenu", StringComparison.OrdinalIgnoreCase))
            return AppLang.L("本工具", "SrvDesk");

        if (clsid is not null && KnownMsClsid.Contains(clsid))
            return AppLang.L("系统", "System");

        if (keyName.StartsWith("Windows.", StringComparison.OrdinalIgnoreCase)
            || ProtectedShellNames.Contains(keyName))
            return AppLang.L("系统", "System");

        foreach (var hint in ClutterShellHints)
        {
            if (keyName.IndexOf(hint, StringComparison.OrdinalIgnoreCase) >= 0
                || display.IndexOf(hint, StringComparison.OrdinalIgnoreCase) >= 0)
                return AppLang.L("第三方", "Third-party");
        }

        // 无 CLSID 的自定义 shell 命令多半是安装程序加的
        if (clsid is null && !ProtectedShellNames.Contains(keyName))
            return AppLang.L("第三方", "Third-party");

        return AppLang.L("系统", "System");
    }

    private static string BuildAdvice(bool protectedItem, string source, string display, string keyName, bool isShellEx)
    {
        if (protectedItem)
            return AppLang.L("勿动", "Keep");

        if (source == AppLang.L("本工具", "SrvDesk"))
            return AppLang.L("按需", "Optional");

        if (source == AppLang.L("第三方", "Third-party"))
            return AppLang.L("可精简", "Can slim");

        foreach (var hint in ClutterShellHints)
        {
            if (display.IndexOf(hint, StringComparison.OrdinalIgnoreCase) >= 0
                || keyName.IndexOf(hint, StringComparison.OrdinalIgnoreCase) >= 0)
                return AppLang.L("可精简", "Can slim");
        }

        if (isShellEx
            && (keyName.IndexOf("Share", StringComparison.OrdinalIgnoreCase) >= 0
                || display.IndexOf("共享", StringComparison.OrdinalIgnoreCase) >= 0
                || display.IndexOf("授予访问", StringComparison.OrdinalIgnoreCase) >= 0))
            return AppLang.L("可精简", "Can slim");

        return AppLang.L("建议保留", "Keep");
    }

    [System.Runtime.InteropServices.DllImport("shlwapi.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    private static extern int SHLoadIndirectString(string pszSource, System.Text.StringBuilder pszOutBuf, int cchOutBuf, IntPtr ppvReserved);
}
