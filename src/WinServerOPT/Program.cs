namespace WinOpt;

static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        if (TryRunCli(args, out var exitCode))
            return exitCode;

        Application.Run(new MainForm());
        return 0;
    }

    private static bool TryRunCli(string[] args, out int exitCode)
    {
        exitCode = 0;
        if (args.Length == 0) return false;

        var cmd = args[0].ToLowerInvariant();
        try
        {
            switch (cmd)
            {
                case "--apply-preset":
                    exitCode = RunPreset(GetArg(args, 1), apply: true);
                    return true;
                case "--load-profile":
                    exitCode = RunProfile(GetArg(args, 1), apply: true);
                    return true;
                case "--export-profile":
                    exitCode = ExportCurrentProfile(GetArg(args, 1));
                    return true;
                case "--help":
                case "-h":
                    Console.WriteLine(
                        "Win一键优化 CLI（需管理员）\r\n\r\n" +
                        "  --apply-preset <id>      应用预设（server-desktop/security/remote-work/minimal）\r\n" +
                        "  --load-profile <file>    从 JSON 配置应用\r\n" +
                        "  --export-profile <file>  导出当前系统状态为配置\r\n" +
                        "  --help                   显示帮助");
                    return true;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("错误：" + ex.Message);
            exitCode = 1;
            return true;
        }

        return false;
    }

    private static string GetArg(string[] args, int index)
    {
        if (index >= args.Length || string.IsNullOrWhiteSpace(args[index]))
            throw new InvalidOperationException("缺少参数。");
        return args[index].Trim('"');
    }

    private static int RunPreset(string id, bool apply)
    {
        EnsureAdmin();
        var preset = OptPresets.Find(id) ?? throw new InvalidOperationException("未知预设：" + id);
        var state = preset.Build();
        if (!apply) return 0;
        var errors = Optimizer.Apply(state);
        ApplyLog.WriteApply($"CLI 预设 {preset.Title}", errors);
        if (errors.Count > 0)
        {
            foreach (var e in errors) Console.Error.WriteLine(e);
            return 2;
        }
        Console.WriteLine("已应用预设：" + preset.Title);
        return 0;
    }

    private static int RunProfile(string path, bool apply)
    {
        EnsureAdmin();
        var state = ProfileStore.Load(path);
        if (!apply) return 0;
        var errors = Optimizer.Apply(state);
        ApplyLog.WriteApply($"CLI 配置 {path}", errors);
        if (errors.Count > 0)
        {
            foreach (var e in errors) Console.Error.WriteLine(e);
            return 2;
        }
        Console.WriteLine("已应用配置：" + path);
        return 0;
    }

    private static int ExportCurrentProfile(string path)
    {
        var state = Optimizer.Read();
        ProfileStore.Save(path, state, "当前系统");
        Console.WriteLine("已导出：" + path);
        return 0;
    }

    private static void EnsureAdmin()
    {
        if (!AdminHelper.IsRunningAsAdministrator())
            throw new InvalidOperationException("需要以管理员身份运行。");
    }
}
