namespace SrvDesk;

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
        ClientSize = new Size(500, 420);
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
            AutoScroll = false,
            Padding = new Padding(0, 4, 0, 0),
            BackColor = AppTheme.Surface,
        };
        UiBuffer.ConfigureNoScrollRow(buttons);

        var ok = ThemedSettingsChrome.CreateButton("确定", true);
        ok.Height = 32;
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

        _keepPassword.Text = "保留现有密码（不改密码时勾选）";
        _keepPassword.AutoSize = true;
        _keepPassword.Margin = new Padding(0, 10, 0, 4);
        _keepPassword.ForeColor = AppTheme.TextMute;
        _keepPassword.Checked = editing && !initial.UpdatePassword;
        _keepPassword.Enabled = editing;

        _hint.Text = editing
            ? "不改密码可留空，并勾选下方「保留现有密码」。"
            : "首次启用请填写密码。";
        _hint.AutoSize = false;
        _hint.Width = 450;
        _hint.Height = 28;
        _hint.Margin = new Padding(0, 0, 0, 0);
        _hint.ForeColor = AppTheme.TextMute;

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
            "Autologon",
            body,
            "");

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
            MessageBox.Show("请填写密码，或勾选保留现有密码。", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
