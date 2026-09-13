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
        ClientSize = UiScale.Size(520, 440);
        CancelButton = null;

        var body = ThemedSettingsChrome.CreateBodyPanel();
        body.AutoScroll = false;
        body.Padding = new Padding(UiScale.S(20), UiScale.S(12), UiScale.S(20), UiScale.S(8));

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = UiFit.ControlHeight() + UiScale.S(16),
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            AutoScroll = false,
            Padding = new Padding(0, UiScale.S(6), 0, 0),
            BackColor = AppTheme.Surface,
        };
        UiBuffer.ConfigureNoScrollRow(buttons);

        var ok = ThemedSettingsChrome.CreateButton("确定", true);
        UiFit.FitButton(ok, padding: 28);
        ok.Margin = new Padding(UiScale.S(6), 0, 0, 0);
        ok.Click += (_, _) =>
        {
            if (TryAccept()) Close();
        };
        buttons.Controls.Add(ok);

        var stack = ThemedSettingsChrome.CreateToggleStack();
        stack.Dock = DockStyle.Fill;

        _keepPassword.Text = "保留现有密码（不改密码时勾选）";
        _keepPassword.AutoSize = false;
        _keepPassword.Height = Math.Max(UiScale.S(28), UiFit.ControlHeight());
        _keepPassword.Margin = new Padding(0, UiScale.S(10), 0, UiScale.S(4));
        _keepPassword.ForeColor = AppTheme.TextMute;
        _keepPassword.Checked = editing && !initial.UpdatePassword;
        _keepPassword.Enabled = editing;

        _hint.Text = editing
            ? "不改密码可留空，并勾选下方「保留现有密码」。"
            : "首次启用请填写密码。";
        _hint.AutoSize = false;
        _hint.Height = Math.Max(UiScale.S(28), UiFit.ControlHeight(UiFit.UiFontSmall));
        _hint.Margin = Padding.Empty;
        _hint.ForeColor = AppTheme.TextMute;
        _hint.TextAlign = ContentAlignment.MiddleLeft;

        stack.Controls.Add(MakeField("域（本地账户可留空）", _domain, initial.Domain));
        stack.Controls.Add(MakeField("用户名", _user, initial.Username));
        stack.Controls.Add(MakeField("密码", _password, "", password: true));
        stack.Controls.Add(_keepPassword);
        stack.Controls.Add(_hint);

        body.Controls.Add(stack);
        body.Controls.Add(buttons);
        body.Resize += (_, _) => ThemedSettingsChrome.StretchStackChildren(stack);

        ThemedSettingsChrome.MountModal(
            this,
            "Windows 自动登录",
            "",
            body,
            "",
            showHeader: false);

        AcceptButton = ok;
        Load += (_, _) => ThemedSettingsChrome.StretchStackChildren(stack);
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
        var h = UiFit.ControlHeight() + UiScale.S(28);
        var panel = new Panel
        {
            Height = h,
            Margin = new Padding(0, 0, 0, UiScale.S(8)),
        };
        var caption = new SingleLineLabel
        {
            Text = label,
            Dock = DockStyle.Top,
            Height = Math.Max(UiScale.S(22), UiFit.ControlHeight(UiFit.UiFontSmall)),
            ForeColor = AppTheme.TextHeader,
            Font = UiFit.UiFont,
            TextAlign = ContentAlignment.MiddleLeft,
        };
        box.Dock = DockStyle.Fill;
        box.Text = value;
        box.Font = UiFit.UiFont;
        if (password) box.UseSystemPasswordChar = true;
        panel.Controls.Add(box);
        panel.Controls.Add(caption);
        return panel;
    }
}
