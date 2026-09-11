namespace SrvDesk;

/// <summary>
/// 用户数据根目录：默认与 SrvDesk.exe 同目录（便携）。
/// 若 exe 目录不可写（如 Program Files），回退到 %LocalAppData%\SrvDesk。
/// </summary>
internal static class AppPaths
{
    public const string FolderName = "SrvDesk";
    private const string LegacyFolderName = "WinOpt";

    private static string? _dataRoot;
    private static bool _migrated;

    /// <summary>exe 所在目录。</summary>
    public static string ExeDirectory
    {
        get
        {
            try
            {
                var exe = Application.ExecutablePath;
                if (!string.IsNullOrWhiteSpace(exe))
                {
                    var dir = Path.GetDirectoryName(exe);
                    if (!string.IsNullOrWhiteSpace(dir))
                        return dir;
                }
            }
            catch { /* design-time */ }

            return AppDomain.CurrentDomain.BaseDirectory.TrimEnd(
                Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }

    /// <summary>配置、日志、配置文件等落盘根目录。</summary>
    public static string DataRoot => _dataRoot ??= ResolveDataRoot();

    public static string Combine(params string[] parts) =>
        Path.Combine(new[] { DataRoot }.Concat(parts).ToArray());

    public static string LocalAppDataLegacyRoot =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), FolderName);

    public static string LocalAppDataWinOptRoot =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), LegacyFolderName);

    /// <summary>启动时：必要时从 %LocalAppData% 旧目录迁移到 exe 旁。</summary>
    public static void MigrateLegacyDataIfNeeded()
    {
        if (_migrated) return;
        _migrated = true;
        _ = DataRoot; // 先解析
        try
        {
            TryMigrateFrom(LocalAppDataLegacyRoot);
            TryMigrateFrom(LocalAppDataWinOptRoot);
        }
        catch
        {
            // 迁移失败不阻断启动
        }
    }

    private static string ResolveDataRoot()
    {
        var beside = ExeDirectory;
        if (IsWritableDirectory(beside))
            return beside;

        var fallback = LocalAppDataLegacyRoot;
        try { Directory.CreateDirectory(fallback); } catch { /* ignore */ }
        return fallback;
    }

    private static bool IsWritableDirectory(string dir)
    {
        try
        {
            Directory.CreateDirectory(dir);
            var probe = Path.Combine(dir, ".srvdesk-write-test");
            File.WriteAllText(probe, "ok");
            File.Delete(probe);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void TryMigrateFrom(string legacyRoot)
    {
        if (string.IsNullOrWhiteSpace(legacyRoot) || !Directory.Exists(legacyRoot))
            return;
        if (string.Equals(Path.GetFullPath(legacyRoot), Path.GetFullPath(DataRoot), StringComparison.OrdinalIgnoreCase))
            return;

        foreach (var file in Directory.EnumerateFiles(legacyRoot, "*", SearchOption.TopDirectoryOnly))
        {
            var name = Path.GetFileName(file);
            // 首次说明标记：勿从旧目录迁回，否则用户删掉后下次启动又会出现
            if (string.Equals(name, "first-run.ok", StringComparison.OrdinalIgnoreCase))
                continue;
            var dest = Path.Combine(DataRoot, name);
            if (File.Exists(dest)) continue;
            try { File.Copy(file, dest, overwrite: false); } catch { /* ignore */ }
        }

        foreach (var sub in Directory.EnumerateDirectories(legacyRoot))
        {
            var name = Path.GetFileName(sub);
            var destDir = Path.Combine(DataRoot, name);
            CopyDirectoryIfMissing(sub, destDir);
        }
    }

    private static void CopyDirectoryIfMissing(string source, string dest)
    {
        try
        {
            if (!Directory.Exists(dest))
                Directory.CreateDirectory(dest);
            foreach (var file in Directory.EnumerateFiles(source))
            {
                var target = Path.Combine(dest, Path.GetFileName(file));
                if (File.Exists(target)) continue;
                try { File.Copy(file, target, overwrite: false); } catch { /* ignore */ }
            }
            foreach (var dir in Directory.EnumerateDirectories(source))
                CopyDirectoryIfMissing(dir, Path.Combine(dest, Path.GetFileName(dir)));
        }
        catch { /* ignore */ }
    }
}
