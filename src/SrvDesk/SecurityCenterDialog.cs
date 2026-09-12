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
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(480, 360);

        var body = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20, 12, 20, 8),
            BackColor = AppTheme.Surface,
        };

        var stack = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = false,
        };

        stack.Controls.Add(MakeRow("安全中心服务 (wscsvc)", _wsc));
        stack.Controls.Add(MakeRow("Microsoft Defender (WinDefend)", _defender));
        stack.Controls.Add(MakeRow("组策略 / 禁用防间谍软件", _policy));

        _summary.AutoSize = false;
        _summary.Width = 420;
        _summary.Height = 28;
        _summary.Margin = new Padding(0, 8, 0, 4);
        _summary.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
        stack.Controls.Add(_summary);

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = new Padding(0),
        };
        buttons.Controls.Add(MkBtn("禁用安全中心", true, DisableCenter));
        buttons.Controls.Add(MkBtn("启用安全中心", false, EnableCenter));
        buttons.Controls.Add(MkBtn("打开 Windows 安全中心", false, () =>
        {
            try { SecurityCenterHelper.OpenWindowsSecurity(); }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }));
        buttons.Controls.Add(MkBtn("刷新状态", false, RefreshStatus));
        stack.Controls.Add(buttons);

        body.Controls.Add(stack);

        ThemedSettingsChrome.MountModal(
            this,
            "安全中心管理",
            "禁用 / 启用 Windows 安全中心",
            body,
            "",
            showHeader: false);

        Load += (_, _) => RefreshStatus();
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
        AutoSize = true,
        Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
        ForeColor = AppTheme.PrimaryDeep,
    };

    private static Control MakeRow(string title, Label value)
    {
        var panel = new Panel { Width = 420, Height = 36, Margin = new Padding(0, 0, 0, 4) };
        var name = new Label
        {
            Text = title,
            AutoSize = true,
            Location = new Point(0, 8),
            ForeColor = AppTheme.TextHeader,
        };
        value.Location = new Point(240, 8);
        value.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        panel.Controls.Add(name);
        panel.Controls.Add(value);
        return panel;
    }

    private Button MkBtn(string text, bool primary, Action click)
    {
        var b = ThemedSettingsChrome.CreateButton(text, primary);
        b.AutoSize = true;
        b.Margin = new Padding(0, 0, 8, 8);
        b.MinimumSize = new Size(120, 34);
        b.Click += (_, _) => click();
        return b;
    }
}
