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
        ClientSize = new Size(460, 360);

        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 8, 16, 48), BackColor = AppTheme.Surface };

        var warn = new Label
        {
            Text = "实现方式与微软 Sysinternals Autologon 相同：密码存入 LSA 机密（非注册表明文）。\r\n仅建议在物理安全可控的个人 Server 桌面使用。",
            Dock = DockStyle.Top,
            Height = 44,
            ForeColor = AppTheme.ScopeServer,
        };

        var form = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 8, 0, 0) };
        AddField(form, "域（本地账户可留空）", _domain, 0, initial.Domain);
        AddField(form, "用户名", _user, 72, initial.Username);
        AddField(form, "密码", _password, 144, "", password: true);

        _keepPassword.Text = "保留现有 LSA 密码（不修改密码时勾选）";
        _keepPassword.Location = new Point(0, 220);
        _keepPassword.AutoSize = true;
        _keepPassword.ForeColor = AppTheme.TextMute;
        _keepPassword.Checked = editing && !initial.UpdatePassword;
        _keepPassword.Enabled = editing;

        _hint.Text = editing
            ? "留空密码并勾选「保留现有 LSA 密码」可只改用户名/域。"
            : "启用自动登录必须填写密码。";
        _hint.SetBounds(0, 246, 420, 32);
        _hint.ForeColor = AppTheme.TextMute;

        form.Controls.Add(_hint);
        form.Controls.Add(_keepPassword);

        var ok = ThemedSettingsChrome.CreateButton("确定", true);
        ok.Size = new Size(88, 34);
        ok.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        ok.Click += (_, _) =>
        {
            if (TryAccept()) Close();
        };

        body.Controls.Add(form);
        body.Controls.Add(warn);
        body.Controls.Add(ok);

        ThemedSettingsChrome.MountModal(
            this,
            "Windows 自动登录",
            "Autologon · LSA 机密存储",
            body,
            "凭据由管理员权限写入，请勿在不可信环境启用。");

        body.Resize += (_, _) =>
            ok.Location = new Point(body.ClientSize.Width - ok.Width, body.ClientSize.Height - ok.Height);

        AcceptButton = ok;
        CancelButton = null;
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

    private static void AddField(Panel parent, string label, TextBox box, int y, string value, bool password = false)
    {
        parent.Controls.Add(new Label
        {
            Text = label,
            Location = new Point(0, y),
            AutoSize = true,
            ForeColor = AppTheme.TextHeader,
        });
        box.SetBounds(0, y + 22, 420, 26);
        box.Text = value;
        if (password) box.UseSystemPasswordChar = true;
        parent.Controls.Add(box);
    }
}
