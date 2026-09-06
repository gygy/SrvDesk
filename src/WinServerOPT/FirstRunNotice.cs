namespace WinOpt;

/// <summary>首次启动短提示（只弹一次）。</summary>
internal static class FirstRunNotice
{
    private static string MarkerPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WinOpt",
            "first-run.ok");

    public static bool NeedShow() => !File.Exists(MarkerPath);

    public static void MarkDone()
    {
        try
        {
            var dir = Path.GetDirectoryName(MarkerPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(MarkerPath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        }
        catch
        {
            // 写不了也不反复打扰
        }
    }
}
