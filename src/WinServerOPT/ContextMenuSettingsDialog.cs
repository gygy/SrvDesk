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
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SurfaceCard,
            Padding = new Padding(12),
            AutoScroll = true,
        };
        card.Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.BorderLight);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Width = 660,
        };
        InstantToggleRow[] rows =
        [
            _takeOwn, _openCmd, _openPs, _openPsAdmin, _openWt, _openWtAdmin,
            _copyPath, _paint, _notepad, _blockShare,
        ];
        foreach (var r in rows)
        {
            r.Width = 640;
            flow.Controls.Add(r);
        }

        _hint.AutoSize = true;
        _hint.ForeColor = AppTheme.TextMute;
        _hint.MaximumSize = new Size(640, 0);
        _hint.Margin = new Padding(0, 12, 0, 0);
        _hint.Text = ContextMenuTweaks.TerminalAvailable()
            ? "开关立即写入注册表。文件夹空白处与文件夹本身均可出现「在此处打开」项。"
            : "未检测到 wt.exe：Windows Terminal 相关项开启时会提示先安装终端。";
        flow.Controls.Add(_hint);

        card.Controls.Add(flow);
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
