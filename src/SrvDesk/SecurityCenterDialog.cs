namespace SrvDesk;

/// <summary>安全中心管理：查看状态，禁用 / 启用。</summary>
internal sealed class SecurityCenterDialog : Form
{
    private readonly Label _wsc = MakeValueLabel();
    private readonly Label _defender = MakeValueLabel();
    private readonly Label _policy = MakeValueLabel();
    private readonly Label _summary = new();

    public SecurityCenterDialog()
    {
        Text = "安全中心管理";
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = UiScale.Size(820, 560);
        MinimumSize = UiScale.Size(780, 520);

        var body = ThemedSettingsChrome.CreateBodyPanel();
        body.Padding = new Padding(UiScale.S(20), UiScale.S(14), UiScale.S(20), UiScale.S(10));

        var stack = ThemedSettingsChrome.CreateToggleStack();
        stack.Dock = DockStyle.Top;
        stack.AutoSize = true;
        stack.AutoSizeMode = AutoSizeMode.GrowAndShrink;

        stack.Controls.Add(MakeRow("安全中心服务 (wscsvc)", _wsc));
        stack.Controls.Add(MakeRow("Microsoft Defender (WinDefend)", _defender));
        stack.Controls.Add(MakeRow("组策略 / 禁用防间谍软件", _policy));

        var rowH = Math.Max(UiScale.S(36), UiFit.ControlHeight(UiFit.UiFontBold()) + UiScale.S(8));
        _summary.AutoSize = false;
        _summary.Height = rowH;
        _summary.Margin = new Padding(0, UiScale.S(10), 0, UiScale.S(10));
        _summary.Font = UiFit.UiFontBold();
        _summary.AutoEllipsis = true;
        _summary.TextAlign = ContentAlignment.MiddleLeft;
        stack.Controls.Add(_summary);

        var buttons = new TableLayoutPanel
        {
            Height = (UiFit.ControlHeight() + UiScale.S(12)) * 2 + UiScale.S(8),
            ColumnCount = 2,
            RowCount = 2,
            Margin = Padding.Empty,
        };
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        buttons.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        buttons.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
        AddGridBtn(buttons, 0, 0, MkBtn("禁用安全中心", true, DisableCenter));
        AddGridBtn(buttons, 1, 0, MkBtn("启用安全中心", false, EnableCenter));
        AddGridBtn(buttons, 0, 1, MkBtn("打开 Windows 安全中心", false, () =>
        {
            try { SecurityCenterHelper.OpenWindowsSecurity(); }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }));
        AddGridBtn(buttons, 1, 1, MkBtn("刷新状态", false, RefreshStatus));
        stack.Controls.Add(buttons);

        body.Controls.Add(stack);
        body.Resize += (_, _) => ThemedSettingsChrome.StretchStackChildren(stack);

        ThemedSettingsChrome.MountModal(
            this,
            "安全中心管理",
            "",
            body,
            "",
            showHeader: false,
            onRefresh: RefreshStatus);

        Load += (_, _) =>
        {
            ThemedSettingsChrome.StretchStackChildren(stack);
            RefreshStatus();
        };
    }

    private static void AddGridBtn(TableLayoutPanel grid, int col, int row, Button b)
    {
        b.Dock = DockStyle.Fill;
        b.Margin = new Padding(UiScale.S(4));
        grid.Controls.Add(b, col, row);
    }

    private void RefreshStatus()
    {
        var s = SecurityCenterHelper.Query();
        _wsc.Text = s.WscText;
        _wsc.ForeColor = StatusColor(s.WscText);
        _defender.Text = s.DefenderText;
        _defender.ForeColor = StatusColor(s.DefenderText);
        _policy.Text = s.PolicyText;
        _policy.ForeColor = s.LooksDisabled ? Color.FromArgb(180, 80, 40) : Color.FromArgb(40, 140, 70);
        _summary.Text = s.Summary;
        _summary.ForeColor = s.LooksDisabled ? Color.FromArgb(180, 80, 40) : AppTheme.PrimaryDeep;
    }

    private void DisableCenter()
    {
        if (!AdminHelper.IsRunningAsAdministrator())
        {
            MessageBox.Show(this, "请以管理员身份运行后再操作。", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var answer = MessageBox.Show(this,
            "将禁用安全中心与 Defender 相关服务/策略。\r\n\r\n可能导致系统提示「病毒和威胁防护已关闭」。是否继续？",
            Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (answer != DialogResult.Yes) return;

        try
        {
            Cursor = Cursors.WaitCursor;
            SecurityCenterHelper.Disable();
            RefreshStatus();
            MessageBox.Show(this, "已禁用安全中心相关组件。若托盘图标仍在，可注销或重启后再看。",
                Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "禁用失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }

    private void EnableCenter()
    {
        if (!AdminHelper.IsRunningAsAdministrator())
        {
            MessageBox.Show(this, "请以管理员身份运行后再操作。", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            Cursor = Cursors.WaitCursor;
            SecurityCenterHelper.Enable();
            RefreshStatus();
            MessageBox.Show(this, "已尝试启用安全中心。若服务未启动，请稍候再点「刷新状态」，或重启一次。",
                Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "启用失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }

    private static Color StatusColor(string text) =>
        text.IndexOf("运行中", StringComparison.Ordinal) >= 0 ? Color.FromArgb(40, 140, 70) :
        text.IndexOf("已禁用", StringComparison.Ordinal) >= 0 || text.IndexOf("已停止", StringComparison.Ordinal) >= 0
            ? Color.FromArgb(180, 80, 40) :
        AppTheme.TextMain;

    private static Label MakeValueLabel() => new()
    {
        AutoSize = false,
        Dock = DockStyle.Fill,
        Font = UiFit.UiFontBold(),
        ForeColor = AppTheme.PrimaryDeep,
        TextAlign = ContentAlignment.MiddleLeft,
    };

    private static Control MakeRow(string title, Label value)
    {
        var h = Math.Max(UiScale.S(36), UiFit.ControlHeight(UiFit.UiFontBold()) + UiScale.S(8));
        var panel = new Panel
        {
            Height = h,
            Margin = new Padding(0, 0, 0, UiScale.S(6)),
        };
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52F));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48F));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        var name = new SingleLineLabel
        {
            Text = title,
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.TextHeader,
            Font = UiFit.UiFont,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        grid.Controls.Add(name, 0, 0);
        grid.Controls.Add(value, 1, 0);
        panel.Controls.Add(grid);
        return panel;
    }

    private static Button MkBtn(string text, bool primary, Action click)
    {
        var b = ThemedSettingsChrome.CreateButton(text, primary);
        UiFit.FitButton(b, padding: 28);
        b.Click += (_, _) => click();
        return b;
    }
}
