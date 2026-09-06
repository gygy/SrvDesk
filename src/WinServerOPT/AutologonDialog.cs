namespace WinOpt;

internal sealed class AutologonDialog : Form
{
    private readonly TextBox _domain = new();
    private readonly TextBox _user = new();
    private readonly TextBox _password = new();
    private readonly CheckBox _keepPassword = new();
    private readonly Label _hint = new();

    public AutologonSettings Settings { get; private set; } = new();

    public AutologonDialog(AutologonSettings initial, bool editing)
    {
        Text = "Windows 自动登录";
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        // 头部 + 说明 + 三字段 + 勾选/提示 + 确定 + 底栏，一次看全
        ClientSize = new Size(500, 520);
        CancelButton = null;

        var body = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 8, 16, 4),
            BackColor = AppTheme.Surface,
            AutoScroll = false,
        };

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 42,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 4, 0, 0),
            BackColor = AppTheme.Surface,
        };

        var ok = ThemedSettingsChrome.CreateButton("确定", true);
        ok.Size = new Size(88, 32);
        ok.Margin = new Padding(6, 0, 0, 0);
        ok.Click += (_, _) =>
        {
            if (TryAccept()) Close();
        };
        buttons.Controls.Add(ok);

        var stack = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = false,
            Padding = new Padding(0),
            BackColor = AppTheme.Surface,
        };

        var warn = new Label
        {
            Text = "实现方式与微软 Sysinternals Autologon 相同：密码存入 LSA 机密（非注册表明文）。\r\n仅建议在物理安全可控的个人 Server 桌面使用。",
            AutoSize = false,
            Width = 450,
            Height = 48,
            Margin = new Padding(0, 0, 0, 10),
            ForeColor = AppTheme.ScopeServer,
        };

        _keepPassword.Text = "保留现有 LSA 密码（不修改密码时勾选）";
        _keepPassword.AutoSize = true;
        _keepPassword.Margin = new Padding(0, 10, 0, 4);
        _keepPassword.ForeColor = AppTheme.TextMute;
        _keepPassword.Checked = editing && !initial.UpdatePassword;
        _keepPassword.Enabled = editing;

        _hint.Text = editing
            ? "留空密码并勾选「保留现有 LSA 密码」可只改用户名/域。"
            : "启用自动登录必须填写密码。";
        _hint.AutoSize = false;
        _hint.Width = 450;
        _hint.Height = 36;
        _hint.Margin = new Padding(0, 0, 0, 0);
        _hint.ForeColor = AppTheme.TextMute;

        stack.Controls.Add(warn);
        stack.Controls.Add(MakeField("域（本地账户可留空）", _domain, initial.Domain));
        stack.Controls.Add(MakeField("用户名", _user, initial.Username));
        stack.Controls.Add(MakeField("密码", _password, "", password: true));
        stack.Controls.Add(_keepPassword);
        stack.Controls.Add(_hint);

        body.Controls.Add(stack);
        body.Controls.Add(buttons);

        ThemedSettingsChrome.MountModal(
            this,
            "Windows 自动登录",
            "Autologon · LSA 机密存储",
            body,
            "凭据由管理员权限写入，请勿在不可信环境启用。");

        AcceptButton = ok;
    }

    private bool TryAccept()
    {
        if (string.IsNullOrWhiteSpace(_user.Text))
        {
            MessageBox.Show("请填写用户名。", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        if (!_keepPassword.Checked && string.IsNullOrEmpty(_password.Text))
        {
            MessageBox.Show("请填写密码，或勾选保留现有 LSA 密码。", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        Settings = new AutologonSettings
        {
            Domain = _domain.Text.Trim(),
            Username = _user.Text.Trim(),
            Password = _password.Text,
            UpdatePassword = !_keepPassword.Checked,
        };
        DialogResult = DialogResult.OK;
        return true;
    }

    private static Control MakeField(string label, TextBox box, string value, bool password = false)
    {
        var panel = new Panel
        {
            Width = 450,
            Height = 58,
            Margin = new Padding(0, 0, 0, 8),
        };
        panel.Controls.Add(new Label
        {
            Text = label,
            AutoSize = true,
            Location = new Point(0, 0),
            ForeColor = AppTheme.TextHeader,
        });
        box.SetBounds(0, 24, 450, 28);
        box.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        box.Text = value;
        if (password) box.UseSystemPasswordChar = true;
        panel.Controls.Add(box);
        return panel;
    }
}
