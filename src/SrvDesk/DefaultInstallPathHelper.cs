using Microsoft.Win32;

namespace SrvDesk;

/// <summary>
/// 修改/恢复 Windows 默认软件安装路径（ProgramFilesDir / CommonFilesDir）。
/// 多数安装程序会读取这些键作为默认目标；已安装软件不会移动。
/// </summary>
internal static class DefaultInstallPathHelper
{
    private const string CurrentVersion = @"SOFTWARE\Microsoft\Windows\CurrentVersion";
    private const string CurrentVersionWow = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion";

    internal sealed class Snapshot
    {
        public string ProgramFiles { get; set; } = "";
        public string ProgramFilesX86 { get; set; } = "";
        public string CommonFiles { get; set; } = "";
        public string CommonFilesX86 { get; set; } = "";
        public string ProgramW6432 { get; set; } = "";
        public string CommonW6432 { get; set; } = "";
        public string SystemDriveRoot { get; set; } = @"C:\";

        public bool IsAtSystemDefault
        {
            get
            {
                var root = SystemDriveRoot.TrimEnd('\\') + @"\";
                return PathsEqual(ProgramFiles, root + "Program Files")
                    && PathsEqual(ProgramFilesX86, root + "Program Files (x86)")
                    && PathsEqual(CommonFiles, root + @"Program Files\Common Files")
                    && PathsEqual(CommonFilesX86, root + @"Program Files (x86)\Common Files");
            }
        }
    }

    public static string SystemDriveRoot
    {
        get
        {
            var sys = Environment.GetEnvironmentVariable("SystemDrive");
            if (!string.IsNullOrWhiteSpace(sys))
                return sys.TrimEnd('\\') + @"\";
            return Path.GetPathRoot(Environment.SystemDirectory) ?? @"C:\";
        }
    }

    public static Snapshot Read()
    {
        var root = SystemDriveRoot;
        return new Snapshot
        {
            SystemDriveRoot = root,
            ProgramFiles = ReadSz(CurrentVersion, "ProgramFilesDir")
                ?? root + "Program Files",
            ProgramFilesX86 = ReadSz(CurrentVersion, "ProgramFilesDir (x86)")
                ?? root + "Program Files (x86)",
            CommonFiles = ReadSz(CurrentVersion, "CommonFilesDir")
                ?? root + @"Program Files\Common Files",
            CommonFilesX86 = ReadSz(CurrentVersion, "CommonFilesDir (x86)")
                ?? root + @"Program Files (x86)\Common Files",
            ProgramW6432 = ReadSz(CurrentVersion, "ProgramW6432Dir")
                ?? root + "Program Files",
            CommonW6432 = ReadSz(CurrentVersion, "CommonW6432Dir")
                ?? root + @"Program Files\Common Files",
        };
    }

    public static IReadOnlyList<DriveInfo> ListCandidateDrives()
    {
        var systemRoot = SystemDriveRoot;
        var list = new List<DriveInfo>();
        foreach (var d in DriveInfo.GetDrives())
        {
            try
            {
                if (!d.IsReady) continue;
                if (d.DriveType != DriveType.Fixed) continue;
                var root = d.RootDirectory.FullName;
                if (PathsEqual(root, systemRoot)) continue;
                list.Add(d);
            }
            catch
            {
                /* 忽略不可访问盘 */
            }
        }

        list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        return list;
    }

    public static Snapshot BuildForDrive(string driveRoot)
    {
        var root = NormalizeDriveRoot(driveRoot);
        return new Snapshot
        {
            SystemDriveRoot = SystemDriveRoot,
            ProgramFiles = root + "Program Files",
            ProgramFilesX86 = root + "Program Files (x86)",
            CommonFiles = root + @"Program Files\Common Files",
            CommonFilesX86 = root + @"Program Files (x86)\Common Files",
            ProgramW6432 = root + "Program Files",
            CommonW6432 = root + @"Program Files\Common Files",
        };
    }

    public static Snapshot BuildSystemDefault() => BuildForDrive(SystemDriveRoot);

    public static void Apply(Snapshot target, bool createFolders)
    {
        if (string.IsNullOrWhiteSpace(target.ProgramFiles)
            || string.IsNullOrWhiteSpace(target.ProgramFilesX86))
            throw new InvalidOperationException(AppLang.L("目标路径无效。", "Target path is invalid."));

        if (createFolders)
        {
            Directory.CreateDirectory(target.ProgramFiles);
            Directory.CreateDirectory(target.ProgramFilesX86);
            Directory.CreateDirectory(target.CommonFiles);
            Directory.CreateDirectory(target.CommonFilesX86);
        }

        WriteSz(CurrentVersion, "ProgramFilesDir", target.ProgramFiles);
        WriteSz(CurrentVersion, "ProgramFilesDir (x86)", target.ProgramFilesX86);
        WriteSz(CurrentVersion, "CommonFilesDir", target.CommonFiles);
        WriteSz(CurrentVersion, "CommonFilesDir (x86)", target.CommonFilesX86);
        WriteSz(CurrentVersion, "ProgramW6432Dir", target.ProgramW6432);
        WriteSz(CurrentVersion, "CommonW6432Dir", target.CommonW6432);

        if (Environment.Is64BitOperatingSystem)
        {
            WriteSz(CurrentVersionWow, "ProgramFilesDir", target.ProgramFilesX86);
            WriteSz(CurrentVersionWow, "CommonFilesDir", target.CommonFilesX86);
        }

        ApplyLog.Write(AppLang.L(
            "默认安装路径已更新：" + target.ProgramFiles,
            "Default install path updated: " + target.ProgramFiles));
    }

    public static void RestoreSystemDefault(bool createFolders = false) =>
        Apply(BuildSystemDefault(), createFolders);

    private static string NormalizeDriveRoot(string driveRoot)
    {
        if (string.IsNullOrWhiteSpace(driveRoot))
            throw new ArgumentException("driveRoot");
        var root = Path.GetPathRoot(driveRoot.Trim());
        if (string.IsNullOrEmpty(root))
            throw new InvalidOperationException(AppLang.L("无法识别磁盘根路径。", "Cannot parse drive root."));
        return root.TrimEnd('\\') + @"\";
    }

    private static string? ReadSz(string subKey, string name)
    {
        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var k = baseKey.OpenSubKey(subKey);
        return k?.GetValue(name) as string;
    }

    private static void WriteSz(string subKey, string name, string value)
    {
        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var k = baseKey.CreateSubKey(subKey, true)
            ?? throw new InvalidOperationException(AppLang.L("无法写入注册表：", "Cannot write registry: ") + subKey);
        var old = k.GetValue(name);
        ApplyLog.RegistryString("HKLM", subKey, name, old, value);
        k.SetValue(name, value, RegistryValueKind.String);
    }

    private static bool PathsEqual(string a, string b)
    {
        a = (a ?? "").TrimEnd('\\', '/');
        b = (b ?? "").TrimEnd('\\', '/');
        return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }
}
