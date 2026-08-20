using Microsoft.Win32;

namespace WinOpt;

internal enum StartupKind
{
    RegistryRun,
    RegistryRunOnce,
    StartupFolder,
}

internal sealed class StartupEntry
{
    public string Name { get; set; } = "";
    public string Command { get; set; } = "";
    public string Scope { get; set; } = "";
    public string KindText { get; set; } = "";
    public StartupKind Kind { get; set; }
    public bool Enabled { get; set; }
    public bool IsHkcu { get; set; }
    public string RunKeyPath { get; set; } = "";
    public RegistryHive Hive { get; set; }
    public RegistryView View { get; set; } = RegistryView.Registry64;
    public string? FolderPath { get; set; }
    public string ApprovedStore { get; set; } = "Run";
}

internal static class StartupItemHelper
{
    private const string Run = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunOnce = @"Software\Microsoft\Windows\CurrentVersion\RunOnce";
    private const string ApprovedRoot = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved";

    public static IReadOnlyList<StartupEntry> ListAll()
    {
        var list = new List<StartupEntry>();
        AddRegistry(list, RegistryHive.CurrentUser, RegistryView.Default, Run, "当前用户", "注册表 Run", "Run", once: false);
        AddRegistry(list, RegistryHive.LocalMachine, RegistryView.Registry64, Run, "所有用户", "注册表 Run", "Run", once: false);
        AddRegistry(list, RegistryHive.LocalMachine, RegistryView.Registry32, Run, "所有用户 (32位)", "注册表 Run", "Run32", once: false);
        AddRegistry(list, RegistryHive.CurrentUser, RegistryView.Default, RunOnce, "当前用户", "注册表 RunOnce", "Run", once: true);
        AddRegistry(list, RegistryHive.LocalMachine, RegistryView.Registry64, RunOnce, "所有用户", "注册表 RunOnce", "Run", once: true);

        AddFolder(list,
            Environment.GetFolderPath(Environment.SpecialFolder.Startup),
            "当前用户", true);
        AddFolder(list,
            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup),
            "所有用户", false);

        return list
            .OrderBy(e => e.Enabled ? 0 : 1)
            .ThenBy(e => e.Scope)
            .ThenBy(e => e.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public static void SetEnabled(StartupEntry entry, bool enabled)
    {
        if (entry.Kind == StartupKind.RegistryRunOnce)
            throw new InvalidOperationException("RunOnce 为一次性启动项，请直接删除，不能切换启用状态。");

        SetApproved(entry, enabled);
        ApplyLog.Write($"{(enabled ? "启用" : "禁用")}启动项：{entry.Name}");
    }

    public static void Delete(StartupEntry entry)
    {
        if (entry.Kind == StartupKind.StartupFolder)
        {
            if (string.IsNullOrWhiteSpace(entry.FolderPath) || !File.Exists(entry.FolderPath))
                throw new InvalidOperationException("找不到启动文件夹中的文件。");
            File.Delete(entry.FolderPath);
        }
        else
        {
            using var baseKey = RegistryKey.OpenBaseKey(entry.Hive, entry.View);
            using var key = baseKey.OpenSubKey(entry.RunKeyPath, writable: true);
            key?.DeleteValue(entry.Name, throwOnMissingValue: false);
        }

        DeleteApproved(entry);
        ApplyLog.Write("删除启动项：" + entry.Name);
    }

    public static void AddUserRun(string name, string command)
    {
        name = name.Trim();
        command = command.Trim();
        if (name.Length == 0 || command.Length == 0)
            throw new ArgumentException("名称和命令不能为空。");

        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);
        using var key = baseKey.CreateSubKey(Run, true)
            ?? throw new InvalidOperationException("无法写入当前用户 Run。");
        key.SetValue(name, command, RegistryValueKind.String);
        ApplyLog.Write("添加启动项：" + name);
    }

    public static void OpenLocation(StartupEntry entry)
    {
        if (entry.Kind == StartupKind.StartupFolder && !string.IsNullOrWhiteSpace(entry.FolderPath))
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = "/select,\"" + entry.FolderPath + "\"",
                UseShellExecute = true,
            });
            return;
        }

        var folder = entry.Kind == StartupKind.StartupFolder
            ? entry.FolderPath
            : Environment.GetFolderPath(entry.IsHkcu
                ? Environment.SpecialFolder.Startup
                : Environment.SpecialFolder.CommonStartup);
        if (!string.IsNullOrWhiteSpace(folder) && Directory.Exists(Path.GetDirectoryName(folder) ?? folder))
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = folder is not null && Directory.Exists(folder) ? folder : "shell:startup",
                UseShellExecute = true,
            });
        }
    }

    public static void OpenUserStartupFolder() =>
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = Environment.GetFolderPath(Environment.SpecialFolder.Startup),
            UseShellExecute = true,
        });

    private static void AddRegistry(
        List<StartupEntry> list,
        RegistryHive hive,
        RegistryView view,
        string keyPath,
        string scope,
        string kindText,
        string approvedStore,
        bool once)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, view);
            using var key = baseKey.OpenSubKey(keyPath);
            if (key is null) return;

            foreach (var name in key.GetValueNames())
            {
                if (string.IsNullOrWhiteSpace(name) || name.StartsWith(".", StringComparison.Ordinal))
                    continue;
                var cmd = key.GetValue(name)?.ToString() ?? "";
                if (cmd.Length == 0) continue;

                var entry = new StartupEntry
                {
                    Name = name,
                    Command = cmd,
                    Scope = scope,
                    KindText = kindText,
                    Kind = once ? StartupKind.RegistryRunOnce : StartupKind.RegistryRun,
                    IsHkcu = hive == RegistryHive.CurrentUser,
                    Hive = hive,
                    View = view,
                    RunKeyPath = keyPath,
                    ApprovedStore = approvedStore,
                };
                entry.Enabled = once || IsApprovedEnabled(entry);
                list.Add(entry);
            }
        }
        catch { /* ignore inaccessible keys */ }
    }

    private static void AddFolder(List<StartupEntry> list, string folder, string scope, bool hkcu)
    {
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder)) return;

        foreach (var file in Directory.GetFiles(folder))
        {
            var name = Path.GetFileName(file);
            if (name.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase))
                continue;

            var entry = new StartupEntry
            {
                Name = Path.GetFileNameWithoutExtension(file),
                Command = file,
                Scope = scope,
                KindText = "启动文件夹",
                Kind = StartupKind.StartupFolder,
                IsHkcu = hkcu,
                FolderPath = file,
                Hive = hkcu ? RegistryHive.CurrentUser : RegistryHive.LocalMachine,
                ApprovedStore = "StartupFolder",
            };
            entry.Enabled = IsApprovedEnabled(entry);
            list.Add(entry);
        }
    }

    private static bool IsApprovedEnabled(StartupEntry entry)
    {
        var data = GetApprovedBytes(entry);
        if (data is null || data.Length == 0) return true;
        return (data[0] & 1) == 0;
    }

    private static byte[]? GetApprovedBytes(StartupEntry entry)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(entry.Hive,
                entry.Hive == RegistryHive.LocalMachine ? RegistryView.Registry64 : RegistryView.Default);
            using var key = baseKey.OpenSubKey(ApprovedRoot + "\\" + entry.ApprovedStore);
            return key?.GetValue(ApprovedValueName(entry)) as byte[];
        }
        catch
        {
            return null;
        }
    }

    private static void SetApproved(StartupEntry entry, bool enabled)
    {
        using var baseKey = RegistryKey.OpenBaseKey(entry.Hive,
            entry.Hive == RegistryHive.LocalMachine ? RegistryView.Registry64 : RegistryView.Default);
        using var key = baseKey.CreateSubKey(ApprovedRoot + "\\" + entry.ApprovedStore, true)
            ?? throw new InvalidOperationException("无法写入 StartupApproved。");
        var flag = (byte)(enabled ? 0x02 : 0x03);
        var data = new byte[12];
        data[0] = flag;
        var ticks = (ulong)DateTime.Now.ToFileTimeUtc();
        var time = BitConverter.GetBytes(ticks);
        Array.Copy(time, 0, data, 4, Math.Min(8, time.Length));
        key.SetValue(ApprovedValueName(entry), data, RegistryValueKind.Binary);
    }

    private static void DeleteApproved(StartupEntry entry)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(entry.Hive,
                entry.Hive == RegistryHive.LocalMachine ? RegistryView.Registry64 : RegistryView.Default);
            using var key = baseKey.OpenSubKey(ApprovedRoot + "\\" + entry.ApprovedStore, writable: true);
            key?.DeleteValue(ApprovedValueName(entry), throwOnMissingValue: false);
        }
        catch { /* ignore */ }
    }

    private static string ApprovedValueName(StartupEntry entry) =>
        entry.Kind == StartupKind.StartupFolder
            ? Path.GetFileName(entry.FolderPath ?? entry.Name)
            : entry.Name;
}
