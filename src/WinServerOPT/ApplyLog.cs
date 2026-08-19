namespace WinOpt;

internal static class ApplyLog
{
    private static string LogPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WinOpt", "apply.log");

    public static void Write(string message)
    {
        try
        {
            var dir = Path.GetDirectoryName(LogPath)!;
            Directory.CreateDirectory(dir);
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
            File.AppendAllText(LogPath, line);
        }
        catch { /* ignore */ }
    }

    public static void WriteApply(string action, IReadOnlyList<string> errors)
    {
        Write(errors.Count == 0
            ? $"{action} 成功"
            : $"{action} 部分失败：{string.Join("; ", errors)}");
    }

    public static string LogFilePath => LogPath;
}
