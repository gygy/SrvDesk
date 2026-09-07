namespace SrvDesk;

/// <summary>本机数据目录（%LocalAppData%\SrvDesk），并兼容迁移旧版 WinOpt 目录。</summary>
internal static class AppPaths
{
    public const string FolderName = "SrvDesk";
    private const string LegacyFolderName = "WinOpt";

    public static string DataRoot =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), FolderName);

    /// <summary>若新目录不存在且旧 WinOpt 目录存在，则整目录迁移到 SrvDesk。</summary>
    public static void MigrateLegacyDataIfNeeded()
    {
        try
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var neu = Path.Combine(local, FolderName);
            var old = Path.Combine(local, LegacyFolderName);
            if (Directory.Exists(neu) || !Directory.Exists(old))
                return;

            Directory.CreateDirectory(Path.GetDirectoryName(neu)!);
            Directory.Move(old, neu);
        }
        catch
        {
            // 迁移失败不阻断启动；用户可手动复制
        }
    }
}
