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
        Text = "Windows 自动登录（Autologon）";
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(440, 320);
        Font = new Font("Microsoft YaHei UI", 9F);
        BackColor = AppTheme.SurfaceCard;

        var warn = new Label
        {
            Text = "实现方式与微软 Sysinternals Autologon 相同：\r\n" +
                   "密码存入 LSA 机密（非注册表明文），管理员仍可解密。\r\n" +
                   "仅建议在物理安全可控的个人 Server 桌面使用。",
            Location = new Point(16, 12),
            Size = new Size(408, 56),
            ForeColor = AppTheme.ScopeServer,
        };

        AddField("域（本地账户可留空）", _domain, 78, initial.Domain);
        AddField("用户名", _user, 130, initial.Username);
        AddField("密码", _password, 182, "", password: true);

        _keepPassword.Text = "保留现有 LSA 密码（不修改密码时勾选）";
        _keepPassword.Location = new Point(16, 228);
        _keepPassword.AutoSize = true;
        _keepPassword.ForeColor = AppTheme.TextMute;
        _keepPassword.Checked = editing && !initial.UpdatePassword;
        _keepPassword.Enabled = editing;

        _hint.Text = editing
            ? "留空密码并勾选「保留现有 LSA 密码」可只改用户名/域。"
            : "启用自动登录必须填写密码。";
        _hint.SetBounds(16, 252, 408, 32);
        _hint.ForeColor = AppTheme.TextMute;

        var ok = new Button
        {
            Text = "确定",
            DialogResult = DialogResult.OK,
            Location = new Point(248, 278),
            Size = new Size(88, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.Primary,
            ForeColor = AppTheme.TextOnPrimary,
        };
        ok.FlatAppearance.BorderSize = 0;

        var cancel = new Button
        {
            Text = "取消",
            DialogResult = DialogResult.Cancel,
            Location = new Point(344, 278),
            Size = new Size(80, 32),
            FlatStyle = FlatStyle.Flat,
        };

        AcceptButton = ok;
        CancelButton = cancel;
        Controls.AddRange([warn, _keepPassword, _hint, ok, cancel]);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (DialogResult != DialogResult.OK)
        {
            base.OnFormClosing(e);
            return;
        }

        if (string.IsNullOrWhiteSpace(_user.Text))
        {
            MessageBox.Show("请填写用户名。", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            e.Cancel = true;
            return;
        }

        if (!_keepPassword.Checked && string.IsNullOrEmpty(_password.Text))
        {
            MessageBox.Show("请填写密码，或勾选保留现有 LSA 密码。", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            e.Cancel = true;
            return;
        }

        Settings = new AutologonSettings
        {
            Domain = _domain.Text.Trim(),
            Username = _user.Text.Trim(),
            Password = _password.Text,
            UpdatePassword = !_keepPassword.Checked,
        };
        base.OnFormClosing(e);
    }

    private void AddField(string label, TextBox box, int y, string value, bool password = false)
    {
        Controls.Add(new Label
        {
            Text = label,
            Location = new Point(16, y),
            AutoSize = true,
            ForeColor = AppTheme.TextHeader,
        });
        box.SetBounds(16, y + 22, 408, 26);
        box.Text = value;
        if (password) box.UseSystemPasswordChar = true;
        Controls.Add(box);
    }
}
