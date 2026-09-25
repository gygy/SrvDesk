using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace SrvDesk;

/// <summary>
/// 静默安装后补建桌面 / 开始菜单快捷方式（写到「所有用户」+ 当前交互用户，避免管理员提权后图标建在 Admin 桌面）。
/// </summary>
internal static class SoftwareShortcutHelper
{
    public static string EnsureShortcuts(CommonSoftwareItem item)
    {
        if (item is null || !ShouldCreate(item))
            return "";

        try
        {
            var target = TryResolveTargetExe(item);
            if (string.IsNullOrWhiteSpace(target) || !File.Exists(target))
            {
                ApplyLog.Write("快捷方式：未找到主程序 " + item.Title);
                return "";
            }

            var title = SanitizeFileName(item.Title);
            if (title.Length == 0)
                title = item.Id;
            var workDir = Path.GetDirectoryName(target) ?? "";
            var created = 0;

            foreach (var dir in EnumShortcutFolders())
            {
                try
                {
                    Directory.CreateDirectory(dir);
                    var lnk = Path.Combine(dir, title + ".lnk");
                    if (File.Exists(lnk))
                    {
                        // 已存在且指向同一目标则跳过
                        if (ShortcutPointsTo(lnk, target))
                            continue;
                    }

                    CreateShortcut(lnk, target!, workDir, item.Title);
                    created++;
                }
                catch (Exception ex)
                {
                    ApplyLog.Write("快捷方式写入失败 " + dir + "：" + ex.Message);
                }
            }

            if (created > 0)
            {
                NotifyShellChanged();
                ApplyLog.Write("已补建快捷方式 ×" + created + "：" + item.Title + " → " + target);
                return "已补建桌面/开始菜单快捷方式。";
            }
        }
        catch (Exception ex)
        {
            ApplyLog.Write("补建快捷方式失败：" + item.Title + " — " + ex.Message);
        }

        return "";
    }

    private static bool ShouldCreate(CommonSoftwareItem item)
    {
        if (item.IsWingetBootstrap || item.IsScoopBootstrap)
            return false;
        // 商店/UWP：开始菜单由系统注册，勿造假快捷方式
        if (!string.IsNullOrWhiteSpace(item.AppxPackageName)
            || (!string.IsNullOrWhiteSpace(item.StoreProductId) && item.PreferAppxSideload))
            return false;

        var id = item.Id ?? "";
        if (id.IndexOf("vcredist", StringComparison.OrdinalIgnoreCase) >= 0)
            return false;
        if (id.IndexOf("runtime", StringComparison.OrdinalIgnoreCase) >= 0)
            return false;
        if (id.StartsWith("dotnet-", StringComparison.OrdinalIgnoreCase)
            || id.Equals("dotnet-sdk", StringComparison.OrdinalIgnoreCase))
            return false;
        if (id.Equals("windows-terminal", StringComparison.OrdinalIgnoreCase))
            return false; // Store 应用
        return true;
    }

    private static IEnumerable<string> EnumShortcutFolders()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 所有用户：管理员安装后当前用户也能看到
        foreach (var p in new[]
                 {
                     Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory),
                     Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms),
                 })
        {
            foreach (var x in AddYield(p, seen))
                yield return x;
        }

        // 当前进程用户（若非 SYSTEM）
        foreach (var p in new[]
                 {
                     Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                     Environment.GetFolderPath(Environment.SpecialFolder.Programs),
                 })
        {
            foreach (var x in AddYield(p, seen))
                yield return x;
        }

        // 交互登录用户（提权后 Desktop 常指向 Administrator，这里补真正登录用户）
        var profile = TryGetInteractiveUserProfile();
        if (!string.IsNullOrWhiteSpace(profile))
        {
            foreach (var x in AddYield(Path.Combine(profile!, "Desktop"), seen))
                yield return x;
            foreach (var x in AddYield(
                         Path.Combine(profile!, "AppData", "Roaming", "Microsoft", "Windows", "Start Menu", "Programs"),
                         seen))
                yield return x;
        }
    }

    private static IEnumerable<string> AddYield(string? path, HashSet<string> seen)
    {
        if (string.IsNullOrWhiteSpace(path)) yield break;
        try
        {
            var full = Path.GetFullPath(path);
            if (seen.Add(full))
                yield return full;
        }
        catch { /* ignore */ }
    }

    private static string? TryGetInteractiveUserProfile()
    {
        try
        {
            foreach (var p in Process.GetProcessesByName("explorer"))
            {
                try
                {
                    if (p.SessionId <= 0) continue;
                    if (!TryGetProcessUserProfile(p.Id, out var profile))
                        continue;
                    if (string.IsNullOrWhiteSpace(profile) || !Directory.Exists(profile))
                        continue;
                    // 跳过系统默认配置
                    if (profile.IndexOf(@"\Windows\system32\config\systemprofile",
                            StringComparison.OrdinalIgnoreCase) >= 0)
                        continue;
                    return profile;
                }
                catch { /* next */ }
                finally
                {
                    try { p.Dispose(); } catch { /* ignore */ }
                }
            }
        }
        catch { /* ignore */ }

        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList");
            if (key is null) return null;
            // 取最近登录的非系统 SID（粗略：有 ProfileImagePath 且含 Users）
            foreach (var sid in key.GetSubKeyNames())
            {
                if (!sid.StartsWith("S-1-5-21-", StringComparison.Ordinal)) continue;
                using var sub = key.OpenSubKey(sid);
                var path = sub?.GetValue("ProfileImagePath") as string;
                if (string.IsNullOrWhiteSpace(path)) continue;
                path = Environment.ExpandEnvironmentVariables(path);
                if (Directory.Exists(path) && path.IndexOf(@"\Users\", StringComparison.OrdinalIgnoreCase) >= 0)
                    return path;
            }
        }
        catch { /* ignore */ }

        return null;
    }

    private static bool TryGetProcessUserProfile(int pid, out string? profile)
    {
        profile = null;
        IntPtr hProcess = IntPtr.Zero;
        IntPtr hToken = IntPtr.Zero;
        try
        {
            hProcess = OpenProcess(0x0400 | 0x0010 /*QUERY_INFORMATION|VM_READ*/, false, pid);
            if (hProcess == IntPtr.Zero) return false;
            if (!OpenProcessToken(hProcess, 0x0008 /*TOKEN_QUERY*/, out hToken))
                return false;

            var sb = new System.Text.StringBuilder(260);
            var len = sb.Capacity;
            if (!GetUserProfileDirectory(hToken, sb, ref len))
                return false;
            profile = sb.ToString();
            return !string.IsNullOrWhiteSpace(profile);
        }
        catch
        {
            return false;
        }
        finally
        {
            if (hToken != IntPtr.Zero) CloseHandle(hToken);
            if (hProcess != IntPtr.Zero) CloseHandle(hProcess);
        }
    }

    public static string? TryResolveTargetExe(CommonSoftwareItem item)
    {
        // 1) DisplayIcon / InstallLocation（卸载项）
        var fromReg = TryFindFromUninstallRegistry(item);
        if (!string.IsNullOrWhiteSpace(fromReg) && File.Exists(fromReg))
            return fromReg;

        // 2) 显式主程序名
        foreach (var raw in item.DetectExeNames ?? [])
        {
            var name = (raw ?? "").Trim();
            if (name.Length == 0) continue;
            if (!name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                name += ".exe";

            var hit = FindExeByName(item, name);
            if (hit is not null) return hit;
        }

        // 3) 用检测名猜：DisplayName 相关
        foreach (var pat in item.DetectPatterns ?? [])
        {
            var guess = SanitizeFileName(pat) + ".exe";
            var hit = FindExeByName(item, guess);
            if (hit is not null) return hit;
        }

        return null;
    }

    private static string? FindExeByName(CommonSoftwareItem item, string exeName)
    {
        var tools = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SrvDesk", "tools", item.Id, exeName);
        if (File.Exists(tools)) return tools;

        foreach (var root in new[]
                 {
                     Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                     Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                     Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                 })
        {
            if (string.IsNullOrWhiteSpace(root)) continue;
            foreach (var sub in new[]
                     {
                         Path.Combine(root, exeName),
                         Path.Combine(root, item.Id, exeName),
                         Path.Combine(root, "Huorong", "Sysdiag", "bin", exeName),
                         Path.Combine(root, "Huorong", "SysDiag", "bin", exeName),
                         Path.Combine(root, "ecloud", "ecloud", exeName),
                         Path.Combine(root, "Microsoft VS Code", exeName),
                         Path.Combine(root, "Notepad++", exeName),
                     })
            {
                if (File.Exists(sub)) return sub;
            }

            // 浅层搜索：最多两级子目录
            try
            {
                foreach (var dir in Directory.EnumerateDirectories(root))
                {
                    var direct = Path.Combine(dir, exeName);
                    if (File.Exists(direct)) return direct;
                    try
                    {
                        foreach (var sub in Directory.EnumerateDirectories(dir))
                        {
                            var nested = Path.Combine(sub, exeName);
                            if (File.Exists(nested)) return nested;
                            var bin = Path.Combine(sub, "bin", exeName);
                            if (File.Exists(bin)) return bin;
                        }
                    }
                    catch { /* access */ }
                }
            }
            catch { /* access */ }
        }

        try
        {
            var where = RunWhere(exeName);
            if (!string.IsNullOrWhiteSpace(where) && File.Exists(where))
                return where;
        }
        catch { /* ignore */ }

        return null;
    }

    private static string? TryFindFromUninstallRegistry(CommonSoftwareItem item)
    {
        foreach (var keyPath in new (RegistryHive Hive, string Sub)[]
                 {
                     (RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"),
                     (RegistryHive.LocalMachine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"),
                     (RegistryHive.CurrentUser, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"),
                 })
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(keyPath.Hive, RegistryView.Registry64);
                using var uninstall = baseKey.OpenSubKey(keyPath.SubKey);
                if (uninstall is null) continue;
                foreach (var subName in uninstall.GetSubKeyNames())
                {
                    using var sub = uninstall.OpenSubKey(subName);
                    if (sub is null) continue;
                    var display = sub.GetValue("DisplayName") as string ?? "";
                    if (!Matches(display, item.DetectPatterns)) continue;

                    var icon = sub.GetValue("DisplayIcon") as string;
                    var fromIcon = ExtractExePath(icon);
                    if (fromIcon is not null) return fromIcon;

                    var loc = sub.GetValue("InstallLocation") as string;
                    if (!string.IsNullOrWhiteSpace(loc) && Directory.Exists(loc))
                    {
                        foreach (var raw in item.DetectExeNames ?? Array.Empty<string>())
                        {
                            var name = raw.Trim();
                            if (name.Length == 0) continue;
                            if (!name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                                name += ".exe";
                            var candidate = Path.Combine(loc.Trim().TrimEnd('\\'), name);
                            if (File.Exists(candidate)) return candidate;
                        }

                        // InstallLocation 下找最大的非卸载 exe
                        try
                        {
                            var best = Directory.EnumerateFiles(loc, "*.exe", SearchOption.AllDirectories)
                                .Where(f =>
                                {
                                    var n = Path.GetFileName(f);
                                    return n.IndexOf("uninstall", StringComparison.OrdinalIgnoreCase) < 0
                                           && n.IndexOf("update", StringComparison.OrdinalIgnoreCase) < 0
                                           && n.IndexOf("setup", StringComparison.OrdinalIgnoreCase) < 0;
                                })
                                .OrderByDescending(f => new FileInfo(f).Length)
                                .FirstOrDefault();
                            if (best is not null) return best;
                        }
                        catch { /* ignore */ }
                    }

                    var uninstallCmd = sub.GetValue("DisplayIcon") as string
                        ?? sub.GetValue("UninstallString") as string;
                    var fromCmd = ExtractExePath(uninstallCmd);
                    // UninstallString 常是 unins000.exe，跳过
                    if (fromCmd is not null
                        && Path.GetFileName(fromCmd).IndexOf("unins", StringComparison.OrdinalIgnoreCase) < 0
                        && Path.GetFileName(fromCmd).IndexOf("uninstall", StringComparison.OrdinalIgnoreCase) < 0)
                        return fromCmd;
                }
            }
            catch { /* ignore */ }
        }

        return null;
    }

    private static bool Matches(string display, string[]? patterns)
    {
        if (string.IsNullOrWhiteSpace(display) || patterns is null || patterns.Length == 0)
            return false;
        foreach (var p in patterns)
        {
            if (string.IsNullOrWhiteSpace(p)) continue;
            if (display.IndexOf(p.Trim(), StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }
        return false;
    }

    private static string? ExtractExePath(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var s = raw.Trim().Trim('"');
        // "C:\path\app.exe",0
        var comma = s.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
        if (comma < 0) return null;
        s = s.Substring(0, comma + 4).Trim().Trim('"');
        return File.Exists(s) ? s : null;
    }

    private static void CreateShortcut(string lnkPath, string target, string workDir, string description)
    {
        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("WScript.Shell 不可用");
        var shell = Activator.CreateInstance(shellType)
            ?? throw new InvalidOperationException("无法创建 WScript.Shell");
        var shortcut = shellType.InvokeMember(
            "CreateShortcut",
            BindingFlags.InvokeMethod,
            null,
            shell,
            [lnkPath]);
        if (shortcut is null)
            throw new InvalidOperationException("CreateShortcut 失败");

        var t = shortcut.GetType();
        void Set(string prop, object value) =>
            t.InvokeMember(prop, BindingFlags.SetProperty, null, shortcut, [value]);

        Set("TargetPath", target);
        if (!string.IsNullOrWhiteSpace(workDir))
            Set("WorkingDirectory", workDir);
        Set("WindowStyle", 1);
        Set("Description", description ?? "");
        Set("IconLocation", target + ",0");
        t.InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);

        try
        {
            if (shell is IDisposable d) d.Dispose();
            else Marshal.FinalReleaseComObject(shell);
        }
        catch { /* ignore */ }
    }

    private static bool ShortcutPointsTo(string lnkPath, string target)
    {
        try
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType is null) return false;
            var shell = Activator.CreateInstance(shellType);
            if (shell is null) return false;
            var shortcut = shellType.InvokeMember(
                "CreateShortcut", BindingFlags.InvokeMethod, null, shell, [lnkPath]);
            if (shortcut is null) return false;
            var path = shortcut.GetType().InvokeMember(
                "TargetPath", BindingFlags.GetProperty, null, shortcut, null) as string;
            try { Marshal.FinalReleaseComObject(shell); } catch { /* ignore */ }
            return string.Equals(path, target, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static string SanitizeFileName(string name)
    {
        var s = (name ?? "").Trim();
        // 去掉括号说明：火绒安全软件 → 保留；「Foo（说明）」→ Foo
        var idx = s.IndexOf('（');
        if (idx > 0) s = s.Substring(0, idx).Trim();
        idx = s.IndexOf('(');
        if (idx > 0) s = s.Substring(0, idx).Trim();
        foreach (var c in Path.GetInvalidFileNameChars())
            s = s.Replace(c, ' ');
        while (s.Contains("  "))
            s = s.Replace("  ", " ");
        return s.Trim();
    }

    private static string? RunWhere(string exeName)
    {
        try
        {
            using var p = Process.Start(new ProcessStartInfo
            {
                FileName = "where.exe",
                Arguments = exeName,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });
            if (p is null) return null;
            var o = p.StandardOutput.ReadToEnd();
            p.WaitForExit(3_000);
            return o.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(l => l.Trim().Trim('"'))
                .FirstOrDefault(File.Exists);
        }
        catch
        {
            return null;
        }
    }

    private static void NotifyShellChanged()
    {
        try
        {
            SHChangeNotify(0x08000000 /*SHCNE_ASSOCCHANGED*/, 0x1000 /*SHCNF_IDLIST*/, IntPtr.Zero, IntPtr.Zero);
        }
        catch { /* ignore */ }
    }

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

    [DllImport("userenv.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool GetUserProfileDirectory(IntPtr hToken, System.Text.StringBuilder lpProfileDir, ref int lpcchSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);
}
