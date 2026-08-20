namespace WinOpt;

internal sealed class ComputerIdentityDialog : Form
{
    private readonly Label _currentName = new();
    private readonly Label _currentGroup = new();
    private readonly TextBox _newName = new();
    private readonly TextBox _newWorkgroup = new();
    private readonly CheckBox _restart = new();
    private readonly ComputerIdentityInfo _info;

    public bool RestartScheduled { get; private set; }

    public ComputerIdentityDialog(ComputerIdentityInfo info)
    {
        _info = info;
        Text = "计算机名 / 工作组";
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(480, 380);

        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 8, 16, 48), BackColor = AppTheme.Surface };

        var tip = new Label
        {
            Text = "通过 WMI 修改计算机名与工作组。NetBIOS 名称最长 15 字符，修改后必须重启才能完全生效。",
            Dock = DockStyle.Top,
            Height = 36,
            ForeColor = AppTheme.TextMute,
        };

        _currentName.Text = "当前计算机名：" + info.ComputerName;
        _currentName.SetBounds(0, 44, 428, 20);
        _currentName.ForeColor = AppTheme.TextHeader;

        _currentGroup.Text = info.PartOfDomain
            ? $"当前：已加入域「{info.Domain}」（无法在此修改工作组）"
            : "当前工作组：" + info.Workgroup;
        _currentGroup.SetBounds(0, 66, 428, 20);
        _currentGroup.ForeColor = info.PartOfDomain ? AppTheme.ScopeServer : AppTheme.TextHeader;

        var form = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 92, 0, 0) };
        AddField(form, "新计算机名（留空表示不修改）", _newName, 0, info.ComputerName);
        AddField(form, "新工作组名（留空表示不修改）", _newWorkgroup, 72, info.PartOfDomain ? "" : info.Workgroup);
        _newWorkgroup.Enabled = !info.PartOfDomain;

        _restart.Text = "应用成功后 60 秒后自动重启（可运行 shutdown /a 取消）";
        _restart.Location = new Point(0, 152);
        _restart.AutoSize = true;
        _restart.Checked = true;

        var apply = ThemedSettingsChrome.CreateButton("应用", true);
        apply.Size = new Size(88, 34);
        apply.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        apply.Click += (_, _) => ApplyChanges();

        body.Controls.Add(_restart);
        body.Controls.Add(form);
        body.Controls.Add(_currentGroup);
        body.Controls.Add(_currentName);
        body.Controls.Add(tip);
        body.Controls.Add(apply);

        ThemedSettingsChrome.MountModal(
            this,
            "计算机名 / 工作组",
            "与「系统属性 → 计算机名」相同",
            body,
            "修改后需重启才能完全生效。");

        body.Resize += (_, _) =>
            apply.Location = new Point(body.ClientSize.Width - apply.Width, body.ClientSize.Height - apply.Height);
    }

    private void ApplyChanges()
    {
        var rename = _newName.Text.Trim();
        var workgroup = _newWorkgroup.Text.Trim();
        var renameChanged = rename.Length > 0 &&
            !rename.Equals(_info.ComputerName, StringComparison.OrdinalIgnoreCase);
        var workgroupChanged = workgroup.Length > 0 &&
            !workgroup.Equals(_info.Workgroup, StringComparison.OrdinalIgnoreCase);

        if (!renameChanged && !workgroupChanged)
        {
            MessageBox.Show("未修改任何项。", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (renameChanged && !ComputerIdentityHelper.ValidateNetbiosName(rename, out var err1))
        {
            MessageBox.Show(err1, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (workgroupChanged && !ComputerIdentityHelper.ValidateNetbiosName(workgroup, out var err2))
        {
            MessageBox.Show(err2, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (MessageBox.Show(
                (renameChanged ? $"计算机名 → {rename.ToUpperInvariant()}\r\n" : "") +
                (workgroupChanged ? $"工作组 → {workgroup.ToUpperInvariant()}\r\n" : "") +
                "\r\n更改后需重启生效。是否继续？",
                "确认修改",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
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

            ApplyLog.Write($"计算机标识：名={renameChanged} 组={workgroupChanged} 重启={_restart.Checked}");
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "修改失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void AddField(Panel parent, string label, TextBox box, int y, string value)
    {
        parent.Controls.Add(new Label
        {
            Text = label,
            Location = new Point(0, y),
            AutoSize = true,
            ForeColor = AppTheme.TextHeader,
        });
        box.SetBounds(0, y + 22, 428, 26);
        box.Text = value;
        parent.Controls.Add(box);
    }
}
