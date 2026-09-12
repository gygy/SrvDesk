namespace SrvDesk;

/// <summary>一键添加本地用户：默认管理员、不能改密、密码不过期、不强制下次改密。</summary>
internal sealed class LocalUserDialog : Form
{
    private readonly TextBox _user = new();
    private readonly TextBox _password = new();
    private readonly TextBox _password2 = new();
    private readonly CheckBox _mustChange = new();
    private readonly CheckBox _cantChange = new();
    private readonly CheckBox _neverExpire = new();
    private readonly CheckBox _admin = new();
    private readonly Label _status = new();

    public LocalUserDialog()
    {
        Text = "快速添加本地用户";
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
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
            AutoScroll = false,
            Padding = new Padding(0, 4, 0, 0),
            BackColor = AppTheme.Surface,
        };
        UiBuffer.ConfigureNoScrollRow(buttons);

        var cancel = ThemedSettingsChrome.CreateButton("取消", false);
        cancel.Height = 32;
        cancel.Margin = new Padding(6, 0, 0, 0);
        cancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        var ok = ThemedSettingsChrome.CreateButton("一键添加", true);
        ok.Height = 32;
        ok.Margin = new Padding(6, 0, 0, 0);
        ok.Click += (_, _) => TryCreate();

        buttons.Controls.Add(ok);
        buttons.Controls.Add(cancel);

        var stack = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = false,
            Padding = new Padding(0),
            BackColor = AppTheme.Surface,
        };

        stack.Controls.Add(MakeField("用户名", _user, password: false));
        stack.Controls.Add(MakeField("密码", _password, password: true));
        stack.Controls.Add(MakeField("确认密码", _password2, password: true));

        _mustChange.Text = "用户下次登录须更改密码";
        _mustChange.AutoSize = true;
        _mustChange.Margin = new Padding(0, 8, 0, 4);
        _mustChange.ForeColor = AppTheme.TextMain;
        _mustChange.Checked = false;

        _cantChange.Text = "用户不能修改密码";
        _cantChange.AutoSize = true;
        _cantChange.Margin = new Padding(0, 0, 0, 4);
        _cantChange.ForeColor = AppTheme.TextMain;
        _cantChange.Checked = true;

        _neverExpire.Text = "密码永远不过期";
        _neverExpire.AutoSize = true;
        _neverExpire.Margin = new Padding(0, 0, 0, 4);
        _neverExpire.ForeColor = AppTheme.TextMain;
        _neverExpire.Checked = true;

        _admin.Text = "添加到 Administrators（管理员）";
        _admin.AutoSize = true;
        _admin.Margin = new Padding(0, 4, 0, 8);
        _admin.ForeColor = AppTheme.TextMain;
        _admin.Checked = true;

        _mustChange.CheckedChanged += (_, _) => SyncExclusiveFlags();
        _cantChange.CheckedChanged += (_, _) =>
        {
            if (_cantChange.Checked && _mustChange.Checked)
                _mustChange.Checked = false;
        };
        _neverExpire.CheckedChanged += (_, _) =>
        {
            if (_neverExpire.Checked && _mustChange.Checked)
                _mustChange.Checked = false;
        };

        _status.AutoSize = false;
        _status.Width = 450;
        _status.Height = 40;
        _status.Margin = new Padding(0, 4, 0, 0);
        _status.ForeColor = AppTheme.TextMute;
        _status.Text = AdminHelper.IsRunningAsAdministrator()
            ? "将写入本机 SAM，需管理员权限。"
            : "当前未以管理员运行，添加可能失败。";

        stack.Controls.Add(_mustChange);
        stack.Controls.Add(_cantChange);
        stack.Controls.Add(_neverExpire);
        stack.Controls.Add(_admin);
        stack.Controls.Add(_status);

        body.Controls.Add(stack);
        body.Controls.Add(buttons);

        ThemedSettingsChrome.MountModal(
            this,
            "快速添加本地用户",
            "本地账户 · Administrators",
            body,
            "默认：不强制下次改密 · 不能改密 · 密码不过期 · 管理员。");

        AcceptButton = ok;
        CancelButton = cancel;
        SyncExclusiveFlags();
        Shown += (_, _) => _user.Focus();
    }

    private void SyncExclusiveFlags()
    {
        if (!_mustChange.Checked) return;
        if (_cantChange.Checked) _cantChange.Checked = false;
        if (_neverExpire.Checked) _neverExpire.Checked = false;
    }

    private void TryCreate()
    {
        if (!string.Equals(_password.Text, _password2.Text, StringComparison.Ordinal))
        {
            MessageBox.Show(this, "两次输入的密码不一致。", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _password2.Focus();
            return;
        }

        if (!AdminHelper.IsRunningAsAdministrator())
        {
            var go = MessageBox.Show(this,
                "当前未以管理员运行，创建本地用户通常会失败。仍要继续吗？",
                Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (go != DialogResult.Yes) return;
        }

        var req = new LocalUserCreateRequest
        {
            Username = _user.Text,
            Password = _password.Text,
            MustChangePasswordAtNextLogon = _mustChange.Checked,
            UserCannotChangePassword = _cantChange.Checked,
            PasswordNeverExpires = _neverExpire.Checked,
            AddToAdministrators = _admin.Checked,
        };

        var err = LocalUserHelper.Create(req);
        if (err is not null)
        {
            MessageBox.Show(this, err, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        MessageBox.Show(this,
            "已添加用户「" + req.Username.Trim() + "」。",
            Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        DialogResult = DialogResult.OK;
        Close();
    }

    private static Control MakeField(string label, TextBox box, bool password)
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
        if (password) box.UseSystemPasswordChar = true;
        panel.Controls.Add(box);
        return panel;
    }
}
