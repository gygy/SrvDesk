using System.Diagnostics;

namespace WinOpt;

internal sealed class QuickTool
{
    public string Category { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Target { get; set; } = "";
    public string Arguments { get; set; } = "";
    public bool RequiresServer { get; set; }
    public bool RequiresDesktop { get; set; }

    public bool IsAvailable(SystemFacts facts)
    {
        if (RequiresServer && !facts.IsServer) return false;
        if (RequiresDesktop && !facts.HasDesktopExperience) return false;

        if (Target.StartsWith("ms-settings:", StringComparison.OrdinalIgnoreCase) ||
            Target.StartsWith("windowsdefender:", StringComparison.OrdinalIgnoreCase))
            return facts.HasDesktopExperience;

        var path = ExpandPath(Target);
        if (Directory.Exists(path)) return true;
        if (File.Exists(path)) return true;

        var sys = Environment.GetFolderPath(Environment.SpecialFolder.System);
        if (Target.EndsWith(".msc", StringComparison.OrdinalIgnoreCase) ||
            Target.EndsWith(".cpl", StringComparison.OrdinalIgnoreCase))
            return File.Exists(Path.Combine(sys, Target));

        if (Target.IndexOf('\\') < 0 && Target.IndexOf('/') < 0 && Target.IndexOf(':') < 0)
            return File.Exists(Path.Combine(sys, Target));

        return false;
    }

    public void Launch()
    {
        var target = ExpandPath(Target);
        var start = new ProcessStartInfo
        {
            FileName = target,
            Arguments = Arguments,
            UseShellExecute = true,
        };
        Process.Start(start);
    }

    private static string ExpandPath(string path) =>
        Environment.ExpandEnvironmentVariables(path);
}

internal static class QuickToolsCatalog
{
    public static IReadOnlyList<QuickTool> All { get; } = Build();

    private static List<QuickTool> Build()
    {
        var sys = Environment.GetFolderPath(Environment.SpecialFolder.System);
        var win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        return
        [
            // Server 专属 — 与 WinOpt 优化项高度相关
            T("Server 专属", "服务器管理器 → 本地服务器",
                "计算机名、域/工作组、IE ESC、Windows Update、远程管理、NIC 团队等（左侧点「本地服务器」）。",
                Path.Combine(sys, "ServerManager.exe"), requiresServer: true),
            T("Server 专属", "服务器管理器 → 仪表板",
                "角色/服务状态总览；可从此进入「管理 → 添加角色和功能」。",
                Path.Combine(sys, "ServerManager.exe"), requiresServer: true),
            T("Server 专属", "Windows 功能（可选组件）",
                "启用 .NET、Telnet、桌面体验组件等（Server 带桌面体验时可用）。",
                Path.Combine(win, "OptionalFeatures.exe"), requiresDesktop: true),
            T("Server 专属", "Windows Admin Center",
                "若已安装 WAC，打开浏览器管理入口（未安装则跳过）。",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    @"Windows Admin Center\WindowsAdminCenter.exe"), requiresServer: true),

            // 系统与硬件
            T("系统与硬件", "计算机管理",
                "磁盘、设备、服务、事件、共享文件夹等一站式管理。",
                "compmgmt.msc"),
            T("系统与硬件", "服务",
                "启动/禁用 SysMain、Themes、Audio、RDP 等服务。",
                "services.msc"),
            T("系统与硬件", "设备管理器",
                "驱动、禁用设备、更新硬件。",
                "devmgmt.msc"),
            T("系统与硬件", "磁盘管理",
                "分区、卷标、脱机/联机磁盘。",
                "diskmgmt.msc"),
            T("系统与硬件", "事件查看器",
                "系统/应用程序日志，排查更新与驱动问题。",
                "eventvwr.msc"),
            T("系统与硬件", "任务计划程序",
                "计划任务、自动维护、备份作业。",
                "taskschd.msc"),
            T("系统与硬件", "性能监视器",
                "计数器、数据收集器、长期性能基线。",
                "perfmon.msc"),
            T("系统与硬件", "资源监视器",
                "实时 CPU/内存/磁盘/网络占用。",
                Path.Combine(win, "resmon.exe")),
            T("系统与硬件", "系统信息",
                "硬件摘要、组件版本、冲突/共享。",
                Path.Combine(win, "msinfo32.exe")),
            T("系统与硬件", "存储设置",
                "存储感知、磁盘清理、存储池（Win10+ 设置）。",
                "ms-settings:storagesense", requiresDesktop: true),

            // 网络与安全 — 对应「远程与网络」分组
            T("网络与安全", "网络连接",
                "网卡 IP/DNS、禁用/启用适配器。",
                "ncpa.cpl"),
            T("网络与安全", "Windows 防火墙",
                "入站/出站规则、RDP/文件共享放行。",
                "wf.msc"),
            T("网络与安全", "远程桌面设置",
                "允许远程连接、NLA、会话限制。",
                Path.Combine(sys, "SystemPropertiesRemote.exe")),
            T("网络与安全", "组策略编辑器",
                "QoS、更新、RDP、账户策略等（Server/Pro 自带）。",
                "gpedit.msc"),
            T("网络与安全", "本地安全策略",
                "密码策略、审核、用户权限分配（secpol）。",
                "secpol.msc"),
            T("网络与安全", "证书管理（当前用户）",
                "RDP/HTTPS 证书查看与导入。",
                "certmgr.msc"),
            T("网络与安全", "Windows 安全中心",
                "Defender、防火墙与网络保护状态。",
                "windowsdefender:"),

            // 控制面板与设置
            T("控制面板与设置", "系统属性",
                "计算机名、硬件、高级（性能/虚拟内存/启动恢复）。",
                "sysdm.cpl"),
            T("控制面板与设置", "性能选项",
                "视觉效果、处理器计划、虚拟内存。",
                Path.Combine(win, "SystemPropertiesPerformance.exe")),
            T("控制面板与设置", "程序和功能",
                "卸载程序、启用/关闭 Windows 功能入口。",
                "appwiz.cpl"),
            T("控制面板与设置", "电源选项",
                "电源计划、休眠、关闭盖子/按钮行为。",
                "powercfg.cpl"),
            T("控制面板与设置", "区域",
                "日期/时间格式（与任务栏时钟优化相关）。",
                "intl.cpl"),
            T("控制面板与设置", "日期和时间",
                "时区、Internet 时间同步。",
                "timedate.cpl"),
            T("控制面板与设置", "声音",
                "播放设备、系统提示音（需 Audio 服务）。",
                "mmsys.cpl", requiresDesktop: true),
            T("控制面板与设置", "显示设置",
                "分辨率、缩放、多显示器。",
                "ms-settings:display", requiresDesktop: true),
            T("控制面板与设置", "Windows 更新",
                "检查更新、暂停更新、更新历史。",
                "ms-settings:windowsupdate", requiresDesktop: true),
            T("控制面板与设置", "激活",
                "产品密钥、激活状态。",
                "ms-settings:activation", requiresDesktop: true),

            // 账户与维护
            T("账户与维护", "本地用户和组",
                "用户/组、密码策略快捷入口（非域控）。",
                "lusrmgr.msc"),
            T("账户与维护", "用户账户",
                "自动登录、密码提示、账户类型。",
                Path.Combine(win, "netplwiz.exe"), requiresDesktop: true),
            T("账户与维护", "注册表编辑器",
                "高级调试；修改前请备份。",
                "regedit.exe"),
            T("账户与维护", "组件服务",
                "COM+/DCOM 配置（部分旧程序需要）。",
                "comexp.msc"),
            T("账户与维护", "命令提示符（管理员）",
                "netsh、sc、gpupdate、secedit 等命令行。",
                Path.Combine(sys, "cmd.exe"), arguments: $"/k title {AppBrand.ShortName} 快速工具"),
            T("账户与维护", "PowerShell（管理员）",
                "Get-Service、Set-NetTCPSetting 等自动化。",
                Path.Combine(sys, "WindowsPowerShell", "v1.0", "powershell.exe"),
                arguments: $"-NoExit -Command \"Write-Host '{AppBrand.ShortName} 快速工具' -ForegroundColor Cyan\""),
            T("账户与维护", "操作日志",
                $"{AppBrand.ShortName} 写入的 apply.log 所在文件夹。",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinOpt"),
                arguments: ""),
        ];
    }

    private static QuickTool T(
        string category,
        string title,
        string description,
        string target,
        string arguments = "",
        bool requiresServer = false,
        bool requiresDesktop = false) =>
        new()
        {
            Category = category,
            Title = title,
            Description = description,
            Target = target,
            Arguments = arguments,
            RequiresServer = requiresServer,
            RequiresDesktop = requiresDesktop,
        };
}

internal static class QuickToolsLauncher
{
    public static IReadOnlyList<QuickTool> GetAvailableTools(SystemFacts facts) =>
        QuickToolsCatalog.All.Where(t => t.IsAvailable(facts)).ToList();

    public static void Launch(QuickTool tool, IWin32Window? owner)
    {
        try
        {
            if (Directory.Exists(tool.Target) && string.IsNullOrEmpty(tool.Arguments))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{tool.Target}\"",
                    UseShellExecute = true,
                });
            }
            else
            {
                tool.Launch();
            }

            ApplyLog.Write("快速工具：" + tool.Title);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                owner,
                $"无法打开「{tool.Title}」\r\n\r\n{ex.Message}",
                "快速工具",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }
}
