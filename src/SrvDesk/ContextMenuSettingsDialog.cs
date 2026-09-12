namespace SrvDesk;

internal sealed class ContextMenuSettingsDialog : Form, IEmbeddedSettingsPage
{
    private readonly InstantToggleRow _takeOwn = new("取得所有权");
    private readonly InstantToggleRow _openCmd = new("在此处打开 CMD");
    private readonly InstantToggleRow _openPs = new("在此处打开 PowerShell");
    private readonly InstantToggleRow _openPsAdmin = new("PowerShell（管理员）");
    private readonly InstantToggleRow _openWt = new("在此处打开 Windows Terminal");
    private readonly InstantToggleRow _openWtAdmin = new("Terminal（管理员）");
    private readonly InstantToggleRow _copyPath = new("复制完整路径");
    private readonly InstantToggleRow _copyMoveTo = new("复制到 / 移动到文件夹");
    private readonly InstantToggleRow _quickOps = new("空白处「快捷操作组」");
    private readonly InstantToggleRow _paint = new("用画图编辑图片");
    private readonly InstantToggleRow _notepad = new("用记事本编辑文件");
    private readonly InstantToggleRow _blockShare = new("屏蔽「授予访问权限」");
    private readonly Label _hint = new();
    private readonly Action? _onChanged;

    public ContextMenuSettingsDialog(Action? onChanged = null)
    {
        _onChanged = onChanged;
        Text = AppLang.L("右键菜单", "Context menu");
        AppBrand.ApplyWindowIcon(this);
        AutoScaleMode = AutoScaleMode.None;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(720, 560);
        MinimumSize = new Size(600, 480);

        var body = ThemedSettingsChrome.CreateBodyPanel();

        var common = ThemedSettingsChrome.CreateSection("常用", [_takeOwn, _openCmd, _copyPath, _copyMoveTo, _quickOps]);
        var terminal = ThemedSettingsChrome.CreateSection("终端", [
            _openPs, _openPsAdmin, _openWt, _openWtAdmin,
        ]);
        var edit = ThemedSettingsChrome.CreateSection("用…打开", [_paint, _notepad]);
        var other = ThemedSettingsChrome.CreateSection("其它", [_blockShare]);

        _hint.AutoSize = true;
        _hint.MaximumSize = new Size(640, 0);
        _hint.ForeColor = AppTheme.TextMute;
        _hint.Margin = new Padding(4, 8, 4, 4);
        if (!ContextMenuTweaks.TerminalAvailable())
        {
            _hint.Text = AppLang.L("未检测到 Windows 终端（wt.exe），相关项开启前请先安装。",
                "Windows Terminal (wt.exe) not found — install it before enabling those items.");
            body.Controls.Add(_hint);
        }

        body.Controls.Add(other);
        body.Controls.Add(edit);
        body.Controls.Add(terminal);
        body.Controls.Add(common);

        ThemedSettingsChrome.MountEmbedded(
            this,
            AppLang.L("右键菜单", "Context menu"),
            AppLang.L("常用 · 终端 · 编辑", "Common · Terminal · Edit"),
            body,
            "",
            RefreshFromSystem);

        Load += (_, _) => RefreshFromSystem();
        Resize += (_, _) =>
            _hint.MaximumSize = new Size(Math.Max(280, ClientSize.Width - 80), 0);
    }

    public bool SupportsApplyToSystem => false;
    public void ApplyToSystem() { }
    public bool ConsumeWarmLoadSkip() => false;

    public void RefreshFromSystem() => LoadValues();

    private void LoadValues()
    {
        Bind(_takeOwn, ContextMenuTweaks.IsTakeOwnershipOn(), ContextMenuTweaks.SetTakeOwnership);
        Bind(_openCmd, ContextMenuTweaks.IsOpenCmdOn(), ContextMenuTweaks.SetOpenCmd);
        Bind(_openPs, ContextMenuTweaks.IsOpenPowerShellOn(), ContextMenuTweaks.SetOpenPowerShell);
        Bind(_openPsAdmin, ContextMenuTweaks.IsOpenPowerShellAdminOn(), ContextMenuTweaks.SetOpenPowerShellAdmin);
        Bind(_openWt, ContextMenuTweaks.IsOpenTerminalOn(), ContextMenuTweaks.SetOpenTerminal);
        Bind(_openWtAdmin, ContextMenuTweaks.IsOpenTerminalAdminOn(), ContextMenuTweaks.SetOpenTerminalAdmin);
        Bind(_copyPath, ContextMenuTweaks.IsCopyPathOn(), ContextMenuTweaks.SetCopyPath);
        Bind(_copyMoveTo, ContextMenuTweaks.IsCopyMoveToOn(), ContextMenuTweaks.SetCopyMoveTo);
        Bind(_quickOps, ContextMenuTweaks.IsQuickOpsMenuOn(), ContextMenuTweaks.SetQuickOpsMenu);
        Bind(_paint, ContextMenuTweaks.IsEditWithPaintOn(), ContextMenuTweaks.SetEditWithPaint);
        Bind(_notepad, ContextMenuTweaks.IsEditWithNotepadOn(), ContextMenuTweaks.SetEditWithNotepad);
        Bind(_blockShare, ContextMenuTweaks.IsBlockAccessMenuOn(), ContextMenuTweaks.SetBlockAccessMenu);
    }

    private void Bind(InstantToggleRow row, bool on, Action<bool> apply)
    {
        row.Bind(on, value =>
        {
            apply(value);
            _onChanged?.Invoke();
        });
    }
}
