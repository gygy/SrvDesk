namespace WinOpt;

internal sealed class ContextMenuSettingsDialog : Form
{
    private readonly InstantToggleRow _takeOwn = new("取得所有权");
    private readonly InstantToggleRow _openCmd = new("在此处打开 CMD");
    private readonly InstantToggleRow _openPs = new("在此处打开 PowerShell");
    private readonly InstantToggleRow _openPsAdmin = new("在此处打开 PowerShell（管理员）");
    private readonly InstantToggleRow _openWt = new("在此处打开 Windows Terminal");
    private readonly InstantToggleRow _openWtAdmin = new("在此处打开 Windows Terminal（管理员）");
    private readonly InstantToggleRow _copyPath = new("复制完整路径");
    private readonly InstantToggleRow _paint = new("图片：用画图编辑");
    private readonly InstantToggleRow _notepad = new("文件：用记事本编辑");
    private readonly InstantToggleRow _blockShare = new("屏蔽「授予访问权限」菜单");
    private readonly Label _hint = new();

    public ContextMenuSettingsDialog()
    {
        Text = "右键菜单";
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(720, 560);
        MinimumSize = new Size(600, 480);

        var body = ThemedSettingsChrome.CreateBodyPanel();
        var (card, sectionBody) = ThemedSettingsChrome.CreateSectionShell("右键菜单项");
        card.Dock = DockStyle.Fill;

        InstantToggleRow[] rows =
        [
            _takeOwn, _openCmd, _openPs, _openPsAdmin, _openWt, _openWtAdmin,
            _copyPath, _paint, _notepad, _blockShare,
        ];
        for (var i = rows.Length - 1; i >= 0; i--)
        {
            rows[i].Dock = DockStyle.Top;
            sectionBody.Controls.Add(rows[i]);
        }

        _hint.Dock = DockStyle.Bottom;
        _hint.Height = 48;
        _hint.ForeColor = AppTheme.TextMute;
        _hint.Padding = new Padding(4, 8, 4, 0);
        _hint.Text = ContextMenuTweaks.TerminalAvailable()
            ? "开关立即写入注册表。文件夹空白处与文件夹本身均可出现「在此处打开」项。"
            : "未检测到 wt.exe：Windows Terminal 相关项开启时会提示先安装终端。";
        sectionBody.Controls.Add(_hint);

        body.Controls.Add(card);

        ThemedSettingsChrome.MountModal(
            this,
            "右键菜单",
            "终端管理员 · 画图编辑 · 复制路径等",
            body,
            "部分项需刷新资源管理器后可见。",
            LoadValues);

        Shown += (_, _) => LoadValues();
    }

    private void LoadValues()
    {
        Bind(_takeOwn, ContextMenuTweaks.IsTakeOwnershipOn(), ContextMenuTweaks.SetTakeOwnership);
        Bind(_openCmd, ContextMenuTweaks.IsOpenCmdOn(), ContextMenuTweaks.SetOpenCmd);
        Bind(_openPs, ContextMenuTweaks.IsOpenPowerShellOn(), ContextMenuTweaks.SetOpenPowerShell);
        Bind(_openPsAdmin, ContextMenuTweaks.IsOpenPowerShellAdminOn(), ContextMenuTweaks.SetOpenPowerShellAdmin);
        Bind(_openWt, ContextMenuTweaks.IsOpenTerminalOn(), ContextMenuTweaks.SetOpenTerminal);
        Bind(_openWtAdmin, ContextMenuTweaks.IsOpenTerminalAdminOn(), ContextMenuTweaks.SetOpenTerminalAdmin);
        Bind(_copyPath, ContextMenuTweaks.IsCopyPathOn(), ContextMenuTweaks.SetCopyPath);
        Bind(_paint, ContextMenuTweaks.IsEditWithPaintOn(), ContextMenuTweaks.SetEditWithPaint);
        Bind(_notepad, ContextMenuTweaks.IsEditWithNotepadOn(), ContextMenuTweaks.SetEditWithNotepad);
        Bind(_blockShare, ContextMenuTweaks.IsBlockAccessMenuOn(), ContextMenuTweaks.SetBlockAccessMenu);
    }

    private static void Bind(InstantToggleRow row, bool on, Action<bool> apply)
    {
        row.Bind(on, v =>
        {
            apply(v);
            ApplyLog.Write($"右键菜单 {row.Title} → {(v ? "开" : "关")}");
        });
    }
}
