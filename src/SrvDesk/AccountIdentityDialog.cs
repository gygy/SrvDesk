namespace SrvDesk;

/// <summary>
/// 账户与登录（嵌入主窗标签）：Autologon、添加本地用户、计算机名/工作组。
/// </summary>
internal sealed class AccountIdentityDialog : Form, IEmbeddedSettingsPage
{
    public enum InitialTab
    {
        Autologon,
        LocalUser,
        ComputerName,
    }

    private readonly Func<AutologonSettings?> _getAutologon;
    private readonly Action<AutologonSettings> _saveAutologon;
    private readonly Action? _onAutologonCleared;

    private readonly RadioButton _tabAutologon = new();
    private readonly RadioButton _tabUser = new();
    private readonly RadioButton _tabComputer = new();
    private readonly Panel _pageAutologon = new();
    private readonly Panel _pageUser = new();
    private readonly Panel _pageComputer = new();
    private readonly Button _primary;

    // —— Autologon ——
    private readonly Label _autoStatus = new();
    private readonly TextBox _autoDomain = new();
    private readonly TextBox _autoUser = new();
    private readonly TextBox _autoPassword = new();
    private readonly CheckBox _keepPassword = new();
    private readonly Label _autoHint = new();
    private readonly Button _disableAuto;

    // —— 本地用户 ——
    private readonly TextBox _user = new();
    private readonly TextBox _password = new();
    private readonly TextBox _password2 = new();
    private readonly CheckBox _mustChange = new();
    private readonly CheckBox _cantChange = new();
    private readonly CheckBox _neverExpire = new();
    private readonly CheckBox _admin = new();

    // —— 计算机名 ——
    private ComputerIdentityInfo _info;
    private readonly string _suggestedName;
    private readonly FlowLayoutPanel _computerStack = new();
    private readonly TextBox _newName = new();
    private readonly TextBox _newWorkgroup = new();
    private readonly CheckBox _restart = new();

    public AccountIdentityDialog(
        Func<AutologonSettings?> getAutologon,
        Action<AutologonSettings> saveAutologon,
        Action? onAutologonCleared = null,
        InitialTab initial = InitialTab.LocalUser)
    {
        _getAutologon = getAutologon;
        _saveAutologon = saveAutologon;
        _onAutologonCleared = onAutologonCleared;
        _info = ComputerIdentityHelper.Read();
        _suggestedName = ComputerIdentityHelper.SuggestComputerName();

        Text = AppLang.L("账户与登录", "Account & sign-in");
        AppBrand.ApplyWindowIcon(this);
        AutoScaleMode = AutoScaleMode.None;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(720, 560);
        MinimumSize = new Size(560, 420);
        CancelButton = null;

        var body = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 8, 16, 4),
            BackColor = AppTheme.Surface,
            AutoScroll = true,
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

        _disableAuto = ThemedSettingsChrome.CreateButton(AppLang.L("禁用自动登录", "Disable Autologon"), false);
        _disableAuto.Height = 32;
        _disableAuto.Margin = new Padding(6, 0, 0, 0);
        _disableAuto.Click += (_, _) => TryDisableAutologon();

        _primary = ThemedSettingsChrome.CreateButton(AppLang.L("一键添加", "Add user"), true);
        _primary.Height = 32;
        _primary.Margin = new Padding(6, 0, 0, 0);
        _primary.Click += (_, _) => ApplyCurrentTab();

        buttons.Controls.Add(_primary);
        buttons.Controls.Add(_disableAuto);

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

        StyleTab(_tabUser, AppLang.L("添加本地用户", "Add local user"), 24);
        StyleTab(_tabComputer, AppLang.L("计算机名 / 工作组", "Computer name / workgroup"), 24);
        StyleTab(_tabAutologon, AppLang.L("Autologon 配置", "Autologon"), 0);

        tabs.Controls.Add(_tabUser);
        tabs.Controls.Add(_tabComputer);
        tabs.Controls.Add(_tabAutologon);

        BuildAutologonPage();
        BuildUserPage();
        BuildComputerPage();

        _pageAutologon.Dock = DockStyle.Fill;
        _pageUser.Dock = DockStyle.Fill;
        _pageComputer.Dock = DockStyle.Fill;

        var pages = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Surface,
        };
        pages.Controls.Add(_pageComputer);
        pages.Controls.Add(_pageUser);
        pages.Controls.Add(_pageAutologon);

        body.Controls.Add(pages);
        body.Controls.Add(buttons);
        body.Controls.Add(tabs);

        ThemedSettingsChrome.MountEmbedded(
            this,
            AppLang.L("账户与登录", "Account & sign-in"),
            AppLang.L("Autologon · 本地用户 · 计算机名", "Autologon · local user · computer name"),
            body,
            "",
            RefreshFromSystem);

        AcceptButton = _primary;
        SelectTab(initial);
        Load += (_, _) => RefreshFromSystem();
    }

    public bool SupportsApplyToSystem => false;
    public void ApplyToSystem() { }
    public bool ConsumeWarmLoadSkip() => false;

    public void SelectTab(InitialTab tab)
    {
        switch (tab)
        {
            case InitialTab.Autologon:
                _tabAutologon.Checked = true;
                break;
            case InitialTab.ComputerName:
                _tabComputer.Checked = true;
                break;
            default:
                _tabUser.Checked = true;
                break;
        }

        SyncTab();
    }

    public void RefreshFromSystem()
    {
        try
        {
            var status = AutologonHelper.Read();
            _autoStatus.Text = AppLang.L("系统状态：", "System: ") + status.DisplayDefault();

            var cached = _getAutologon();
            if (cached is not null && !string.IsNullOrWhiteSpace(cached.Username))
            {
                _autoDomain.Text = cached.Domain;
                _autoUser.Text = cached.Username;
            }
            else
            {
                _autoDomain.Text = status.Domain;
                _autoUser.Text = status.Username;
            }

            var editing = status.Enabled || (cached is not null && !string.IsNullOrWhiteSpace(cached.Username));
            _keepPassword.Enabled = editing;
            if (!editing)
                _keepPassword.Checked = false;
            else if (!_keepPassword.Enabled)
                _keepPassword.Checked = false;
            else if (cached is not null)
                _keepPassword.Checked = !cached.UpdatePassword;
            else
                _keepPassword.Checked = status.HasStoredPassword;

            _autoHint.Text = editing
                ? AppLang.L("不改密码可留空，并勾选下方「保留现有密码」。", "Leave password blank and keep existing password to change only user/domain.")
                : AppLang.L("首次启用请填写密码。", "Enter a password to enable Autologon.");

            _info = ComputerIdentityHelper.Read();
            RebuildComputerPage();
        }
        catch
        {
            /* 读取失败时保留界面现有值 */
        }
    }

    private void StyleTab(RadioButton tab, string text, int rightMargin)
    {
        tab.Text = text;
        tab.AutoSize = true;
        tab.Margin = new Padding(0, 6, rightMargin, 0);
        tab.ForeColor = AppTheme.TextMain;
        tab.CheckedChanged += (_, _) => SyncTab();
    }

    private void BuildAutologonPage()
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

        _autoStatus.AutoSize = false;
        _autoStatus.Width = 640;
        _autoStatus.Height = 22;
        _autoStatus.Margin = new Padding(0, 0, 0, 8);
        _autoStatus.ForeColor = AppTheme.TextHeader;
        _autoStatus.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);

        _keepPassword.Text = AppLang.L("保留现有密码（不改密码时勾选）", "Keep existing password");
        _keepPassword.AutoSize = true;
        _keepPassword.Margin = new Padding(0, 10, 0, 4);
        _keepPassword.ForeColor = AppTheme.TextMute;

        _autoHint.AutoSize = false;
        _autoHint.Width = 640;
        _autoHint.Height = 28;
        _autoHint.Margin = new Padding(0, 0, 0, 0);
        _autoHint.ForeColor = AppTheme.TextMute;

        stack.Controls.Add(_autoStatus);
        stack.Controls.Add(MakeField(AppLang.L("域（本地账户可留空）", "Domain (blank for local)"), _autoDomain, password: false));
        stack.Controls.Add(MakeField(AppLang.L("用户名", "Username"), _autoUser, password: false));
        stack.Controls.Add(MakeField(AppLang.L("密码", "Password"), _autoPassword, password: true));
        stack.Controls.Add(_keepPassword);
        stack.Controls.Add(_autoHint);
        _pageAutologon.Controls.Add(stack);
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

        stack.Controls.Add(MakeField(AppLang.L("用户名", "Username"), _user, password: false));
        stack.Controls.Add(MakeField(AppLang.L("密码", "Password"), _password, password: true));
        stack.Controls.Add(MakeField(AppLang.L("确认密码", "Confirm password"), _password2, password: true));

        _mustChange.Text = AppLang.L("用户下次登录须更改密码", "User must change password at next logon");
        _mustChange.AutoSize = true;
        _mustChange.Margin = new Padding(0, 8, 0, 4);
        _mustChange.ForeColor = AppTheme.TextMain;
        _mustChange.Checked = false;

        _cantChange.Text = AppLang.L("用户不能修改密码", "User cannot change password");
        _cantChange.AutoSize = true;
        _cantChange.Margin = new Padding(0, 0, 0, 4);
        _cantChange.ForeColor = AppTheme.TextMain;
        _cantChange.Checked = true;

        _neverExpire.Text = AppLang.L("密码永远不过期", "Password never expires");
        _neverExpire.AutoSize = true;
        _neverExpire.Margin = new Padding(0, 0, 0, 4);
        _neverExpire.ForeColor = AppTheme.TextMain;
        _neverExpire.Checked = true;

        _admin.Text = AppLang.L("添加到 Administrators（管理员）", "Add to Administrators");
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
        _computerStack.Dock = DockStyle.Fill;
        _computerStack.FlowDirection = FlowDirection.TopDown;
        _computerStack.WrapContents = false;
        _computerStack.AutoScroll = false;
        _computerStack.Padding = new Padding(0);
        _computerStack.BackColor = AppTheme.Surface;
        _pageComputer.Controls.Add(_computerStack);
        RebuildComputerPage();
    }

    private void RebuildComputerPage()
    {
        _computerStack.SuspendLayout();
        _computerStack.Controls.Clear();

        _computerStack.Controls.Add(MakeInfoLine(AppLang.L("当前计算机名", "Computer name"), _info.ComputerName));
        _computerStack.Controls.Add(MakeInfoLine(
            _info.PartOfDomain ? AppLang.L("当前域", "Domain") : AppLang.L("当前工作组", "Workgroup"),
            _info.PartOfDomain ? _info.Domain : _info.Workgroup,
            _info.PartOfDomain ? AppTheme.ScopeServer : AppTheme.TextHeader));

        if (_info.PartOfDomain)
        {
            _computerStack.Controls.Add(new Label
            {
                Text = AppLang.L("已加入域，此处仅可改计算机名。", "Domain-joined: computer name only here."),
                AutoSize = false,
                Width = 640,
                Height = 28,
                Margin = new Padding(0, 4, 0, 6),
                ForeColor = AppTheme.TextMute,
            });
        }

        _computerStack.Controls.Add(MakeField(
            AppLang.Lf("新计算机名（建议 {0}）", "New computer name (suggest {0})", _suggestedName),
            _newName, password: false, maxLength: 15));
        if (string.IsNullOrWhiteSpace(_newName.Text))
            _newName.Text = _suggestedName;

        _computerStack.Controls.Add(MakeField(
            AppLang.L("新工作组名（留空或与当前相同 = 不改）", "New workgroup (blank/same = no change)"),
            _newWorkgroup, password: false, maxLength: 15));
        if (string.IsNullOrWhiteSpace(_newWorkgroup.Text))
            _newWorkgroup.Text = _info.PartOfDomain ? "" : _info.Workgroup;
        _newWorkgroup.Enabled = !_info.PartOfDomain;

        _restart.Text = AppLang.L("应用成功后 60 秒自动重启（shutdown /a 可取消）", "Restart in 60s after apply (shutdown /a to cancel)");
        _restart.AutoSize = true;
        _restart.Margin = new Padding(0, 8, 0, 2);
        _computerStack.Controls.Add(_restart);

        _computerStack.ResumeLayout();
    }

    private void SyncTab()
    {
        var auto = _tabAutologon.Checked;
        var user = _tabUser.Checked;
        _pageAutologon.Visible = auto;
        _pageUser.Visible = user;
        _pageComputer.Visible = !auto && !user;
        if (auto) _pageAutologon.BringToFront();
        else if (user) _pageUser.BringToFront();
        else _pageComputer.BringToFront();

        _disableAuto.Visible = auto;
        _primary.Text = auto
            ? AppLang.L("保存配置", "Save")
            : user
                ? AppLang.L("一键添加", "Add user")
                : AppLang.L("应用修改", "Apply");
        FocusActiveField();
    }

    private void FocusActiveField()
    {
        if (_tabAutologon.Checked) _autoUser.Focus();
        else if (_tabUser.Checked) _user.Focus();
        else _newName.Focus();
    }

    private void SyncExclusiveFlags()
    {
        if (!_mustChange.Checked) return;
        if (_cantChange.Checked) _cantChange.Checked = false;
        if (_neverExpire.Checked) _neverExpire.Checked = false;
    }

    private void ApplyCurrentTab()
    {
        if (_tabAutologon.Checked) TrySaveAutologon();
        else if (_tabUser.Checked) TryCreateUser();
        else TryApplyIdentity();
    }

    private void TrySaveAutologon()
    {
        if (string.IsNullOrWhiteSpace(_autoUser.Text))
        {
            MessageBox.Show(this, AppLang.L("请填写用户名。", "Enter a username."), Text,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!_keepPassword.Checked && string.IsNullOrEmpty(_autoPassword.Text))
        {
            MessageBox.Show(this, AppLang.L("请填写密码，或勾选保留现有密码。", "Enter a password, or keep the existing password."),
                Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var settings = new AutologonSettings
        {
            Domain = _autoDomain.Text.Trim(),
            Username = _autoUser.Text.Trim(),
            Password = _autoPassword.Text,
            UpdatePassword = !_keepPassword.Checked,
        };
        _saveAutologon(settings);
        _autoPassword.Clear();
        RefreshFromSystem();
        MessageBox.Show(this,
            AppLang.Lf("已保存 Autologon：{0}\r\n勾选账户策略中的「启用 Autologon」并应用到系统后，下次重启生效。",
                "Autologon saved for {0}\r\nEnable Autologon in Account policy and Apply; takes effect after reboot.",
                settings.Username),
            Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void TryDisableAutologon()
    {
        if (MessageBox.Show(this,
                AppLang.L("确定禁用系统自动登录，并清除本会话已保存的 Autologon 配置？",
                    "Disable system Autologon and clear the session credentials?"),
                Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            return;

        try
        {
            AutologonHelper.Disable();
            _onAutologonCleared?.Invoke();
            _autoPassword.Clear();
            RefreshFromSystem();
            MessageBox.Show(this, AppLang.L("已禁用自动登录。", "Autologon disabled."),
                Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, AppLang.L("禁用失败", "Disable failed"),
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void TryCreateUser()
    {
        if (!string.Equals(_password.Text, _password2.Text, StringComparison.Ordinal))
        {
            MessageBox.Show(this, AppLang.L("两次输入的密码不一致。", "Passwords do not match."), Text,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _password2.Focus();
            return;
        }

        if (!AdminHelper.IsRunningAsAdministrator())
        {
            var go = MessageBox.Show(this,
                AppLang.L("当前未以管理员运行，创建本地用户通常会失败。仍要继续吗？",
                    "Not running as admin; creating a local user will likely fail. Continue?"),
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
            AppLang.Lf("已添加用户「{0}」。", "User \"{0}\" added.", req.Username.Trim()),
            Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        _user.Clear();
        _password.Clear();
        _password2.Clear();
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
            MessageBox.Show(this, AppLang.L("未修改任何项。", "Nothing changed."),
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
            ? AppLang.L("\r\n将在约 60 秒后自动重启。", "\r\nWill restart in about 60 seconds.")
            : AppLang.L("\r\n不会自动重启，请稍后自行重启。", "\r\nNo auto-restart; reboot later yourself.");

        if (MessageBox.Show(this,
                (renameChanged ? AppLang.Lf("计算机名 → {0}\r\n", "Computer name → {0}\r\n", rename.ToUpperInvariant()) : "") +
                (workgroupChanged ? AppLang.Lf("工作组 → {0}\r\n", "Workgroup → {0}\r\n", workgroup.ToUpperInvariant()) : "") +
                restartHint + AppLang.L("\r\n\r\n是否继续？", "\r\n\r\nContinue?"),
                AppLang.L("确认修改", "Confirm"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            return;

        try
        {
            if (renameChanged) ComputerIdentityHelper.RenameComputer(rename);
            if (workgroupChanged) ComputerIdentityHelper.SetWorkgroup(workgroup);

            if (_restart.Checked)
                ComputerIdentityHelper.ScheduleRestart(60);

            ApplyLog.SystemChange(
                "计算机名/工作组",
                $"改名={renameChanged} 改组={workgroupChanged} 计划重启={_restart.Checked}",
                ComputerIdentityHelper.Read().Summary,
                $"名={rename} 组={workgroup}");

            var msg = _restart.Checked
                ? AppLang.L("计算机名/工作组已修改，系统将在 60 秒后重启（命令行执行 shutdown /a 可取消）。",
                    "Computer name/workgroup changed. Restart in 60s (run shutdown /a to cancel).")
                : AppLang.L("计算机名/工作组已修改，请自行选择合适时间重启以完全生效。",
                    "Computer name/workgroup changed. Restart when ready for full effect.");
            MessageBox.Show(this, msg, AppLang.L("修改成功", "Done"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            RefreshFromSystem();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, AppLang.L("修改失败", "Failed"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static Control MakeField(string label, TextBox box, bool password, int maxLength = 0)
    {
        var panel = new Panel
        {
            Width = 640,
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
        box.SetBounds(0, 24, 640, 28);
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
            Width = 640,
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
