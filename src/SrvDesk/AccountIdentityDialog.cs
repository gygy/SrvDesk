namespace SrvDesk;

/// <summary>
/// 账户与计算机名合一：可添加本地用户，或改计算机名/工作组（也可两项都做后点应用）。
/// </summary>
internal sealed class AccountIdentityDialog : Form
{
    public enum InitialTab
    {
        LocalUser,
        ComputerName,
    }

    private readonly RadioButton _tabUser = new();
    private readonly RadioButton _tabComputer = new();
    private readonly Panel _pageUser = new();
    private readonly Panel _pageComputer = new();
    private readonly Button _primary;
    private readonly Button _cancel;
    private readonly bool _optionalIdentity;

    // —— 本地用户 ——
    private readonly TextBox _user = new();
    private readonly TextBox _password = new();
    private readonly TextBox _password2 = new();
    private readonly CheckBox _mustChange = new();
    private readonly CheckBox _cantChange = new();
    private readonly CheckBox _neverExpire = new();
    private readonly CheckBox _admin = new();

    // —— 计算机名 ——
    private readonly ComputerIdentityInfo _info;
    private readonly string _suggestedName;
    private readonly TextBox _newName = new();
    private readonly TextBox _newWorkgroup = new();
    private readonly CheckBox _restart = new();

    public bool RestartScheduled { get; private set; }
    public bool UserCreated { get; private set; }
    public bool IdentityChanged { get; private set; }

    public AccountIdentityDialog(InitialTab initial = InitialTab.LocalUser, bool optionalIdentity = false)
    {
        _optionalIdentity = optionalIdentity;
        _info = ComputerIdentityHelper.Read();
        _suggestedName = ComputerIdentityHelper.SuggestComputerName();

        Text = "账户与计算机名";
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(520, 560);
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

        _cancel = ThemedSettingsChrome.CreateButton(optionalIdentity ? "跳过" : "取消", false);
        _cancel.Height = 32;
        _cancel.Margin = new Padding(6, 0, 0, 0);
        _cancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        _primary = ThemedSettingsChrome.CreateButton("一键添加", true);
        _primary.Height = 32;
        _primary.Margin = new Padding(6, 0, 0, 0);
        _primary.Click += (_, _) => ApplyCurrentTab();

        buttons.Controls.Add(_primary);
        buttons.Controls.Add(_cancel);

        var tabs = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 36,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = false,
            Padding = new Padding(0, 0, 0, 4),
            BackColor = AppTheme.Surface,
        };
        UiBuffer.ConfigureNoScrollRow(tabs);

        _tabUser.Text = "添加本地用户";
        _tabUser.AutoSize = true;
        _tabUser.Margin = new Padding(0, 6, 24, 0);
        _tabUser.ForeColor = AppTheme.TextMain;
        _tabUser.CheckedChanged += (_, _) => SyncTab();

        _tabComputer.Text = "计算机名 / 工作组";
        _tabComputer.AutoSize = true;
        _tabComputer.Margin = new Padding(0, 6, 0, 0);
        _tabComputer.ForeColor = AppTheme.TextMain;
        _tabComputer.CheckedChanged += (_, _) => SyncTab();

        tabs.Controls.Add(_tabUser);
        tabs.Controls.Add(_tabComputer);

        BuildUserPage();
        BuildComputerPage();

        _pageUser.Dock = DockStyle.Fill;
        _pageComputer.Dock = DockStyle.Fill;

        var pages = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Surface,
        };
        pages.Controls.Add(_pageComputer);
        pages.Controls.Add(_pageUser);

        body.Controls.Add(pages);
        body.Controls.Add(buttons);
        body.Controls.Add(tabs);

        ThemedSettingsChrome.MountModal(
            this,
            "账户与计算机名",
            "本地用户 · 计算机名 / 工作组",
            body,
            "");

        AcceptButton = _primary;
        CancelButton = _cancel;

        if (initial == InitialTab.ComputerName)
            _tabComputer.Checked = true;
        else
            _tabUser.Checked = true;

        SyncTab();
        Shown += (_, _) => FocusActiveField();
    }

    private void BuildUserPage()
    {
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

        stack.Controls.Add(_mustChange);
        stack.Controls.Add(_cantChange);
        stack.Controls.Add(_neverExpire);
        stack.Controls.Add(_admin);
        _pageUser.Controls.Add(stack);
        SyncExclusiveFlags();
    }

    private void BuildComputerPage()
    {
        var stack = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = false,
            Padding = new Padding(0),
            BackColor = AppTheme.Surface,
        };

        stack.Controls.Add(MakeInfoLine("当前计算机名", _info.ComputerName));
        stack.Controls.Add(MakeInfoLine(
            _info.PartOfDomain ? "当前域" : "当前工作组",
            _info.PartOfDomain ? _info.Domain : _info.Workgroup,
            _info.PartOfDomain ? AppTheme.ScopeServer : AppTheme.TextHeader));

        if (_info.PartOfDomain)
        {
            stack.Controls.Add(new Label
            {
                Text = "已加入域，此处仅可改计算机名。",
                AutoSize = false,
                Width = 470,
                Height = 28,
                Margin = new Padding(0, 4, 0, 6),
                ForeColor = AppTheme.TextMute,
            });
        }

        stack.Controls.Add(MakeField($"新计算机名（建议 {_suggestedName}）", _newName, password: false, maxLength: 15));
        _newName.Text = _suggestedName;

        stack.Controls.Add(MakeField("新工作组名（留空或与当前相同 = 不改）", _newWorkgroup, password: false, maxLength: 15));
        _newWorkgroup.Text = _info.PartOfDomain ? "" : _info.Workgroup;
        _newWorkgroup.Enabled = !_info.PartOfDomain;

        _restart.Text = "应用成功后 60 秒自动重启（shutdown /a 可取消）";
        _restart.AutoSize = true;
        _restart.Margin = new Padding(0, 8, 0, 2);
        _restart.Checked = false;
        stack.Controls.Add(_restart);

        _pageComputer.Controls.Add(stack);
    }

    private void SyncTab()
    {
        var user = _tabUser.Checked;
        _pageUser.Visible = user;
        _pageComputer.Visible = !user;
        if (user)
            _pageUser.BringToFront();
        else
            _pageComputer.BringToFront();

        _primary.Text = user ? "一键添加" : "应用修改";
        if (_optionalIdentity)
            _cancel.Text = user ? "取消" : "跳过";
        FocusActiveField();
    }

    private void FocusActiveField()
    {
        if (_tabUser.Checked)
            _user.Focus();
        else
            _newName.Focus();
    }

    private void SyncExclusiveFlags()
    {
        if (!_mustChange.Checked) return;
        if (_cantChange.Checked) _cantChange.Checked = false;
        if (_neverExpire.Checked) _neverExpire.Checked = false;
    }

    private void ApplyCurrentTab()
    {
        if (_tabUser.Checked)
            TryCreateUser();
        else
            TryApplyIdentity();
    }

    private void TryCreateUser()
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

        UserCreated = true;
        MessageBox.Show(this,
            "已添加用户「" + req.Username.Trim() + "」。",
            Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        DialogResult = DialogResult.OK;
        Close();
    }

    private void TryApplyIdentity()
    {
        var rename = _newName.Text.Trim();
        var workgroup = _newWorkgroup.Text.Trim();
        var renameChanged = rename.Length > 0 &&
            !rename.Equals(_info.ComputerName, StringComparison.OrdinalIgnoreCase);
        var workgroupChanged = workgroup.Length > 0 &&
            !workgroup.Equals(_info.Workgroup, StringComparison.OrdinalIgnoreCase);

        if (!renameChanged && !workgroupChanged)
        {
            MessageBox.Show(this,
                _optionalIdentity
                    ? "未修改任何项。若不改名，请点「跳过」。"
                    : "未修改任何项。",
                Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (renameChanged && !ComputerIdentityHelper.ValidateNetbiosName(rename, out var err1))
        {
            MessageBox.Show(this, err1, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (workgroupChanged && !ComputerIdentityHelper.ValidateNetbiosName(workgroup, out var err2))
        {
            MessageBox.Show(this, err2, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var restartHint = _restart.Checked
            ? "\r\n将在约 60 秒后自动重启。"
            : "\r\n不会自动重启，请稍后自行重启。";

        if (MessageBox.Show(this,
                (renameChanged ? $"计算机名 → {rename.ToUpperInvariant()}\r\n" : "") +
                (workgroupChanged ? $"工作组 → {workgroup.ToUpperInvariant()}\r\n" : "") +
                restartHint + "\r\n\r\n是否继续？",
                "确认修改",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            return;

        try
        {
            if (renameChanged) ComputerIdentityHelper.RenameComputer(rename);
            if (workgroupChanged) ComputerIdentityHelper.SetWorkgroup(workgroup);

            if (_restart.Checked)
            {
                ComputerIdentityHelper.ScheduleRestart(60);
                RestartScheduled = true;
            }

            IdentityChanged = true;
            ApplyLog.SystemChange(
                "计算机名/工作组",
                $"改名={renameChanged} 改组={workgroupChanged} 计划重启={_restart.Checked}",
                ComputerIdentityHelper.Read().Summary,
                $"名={rename} 组={workgroup}");
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "修改失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static Control MakeField(string label, TextBox box, bool password, int maxLength = 0)
    {
        var panel = new Panel
        {
            Width = 470,
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
        box.SetBounds(0, 24, 470, 28);
        box.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        if (password) box.UseSystemPasswordChar = true;
        if (maxLength > 0) box.MaxLength = maxLength;
        panel.Controls.Add(box);
        return panel;
    }

    private static Panel MakeInfoLine(string caption, string value, Color? valueColor = null)
    {
        var row = new Panel
        {
            Width = 470,
            Height = 24,
            Margin = new Padding(0, 0, 0, 4),
            BackColor = Color.Transparent,
        };
        row.Controls.Add(new Label
        {
            Text = caption + "：",
            AutoSize = true,
            Location = new Point(0, 2),
            ForeColor = AppTheme.TextMute,
        });
        row.Controls.Add(new Label
        {
            Text = value,
            AutoSize = true,
            Location = new Point(110, 2),
            ForeColor = valueColor ?? AppTheme.TextHeader,
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
        });
        return row;
    }
}
