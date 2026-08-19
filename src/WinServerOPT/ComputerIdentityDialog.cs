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
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(460, 340);
        Font = new Font("Microsoft YaHei UI", 9F);
        BackColor = AppTheme.SurfaceCard;

        var tip = new Label
        {
            Text = "通过 WMI 修改计算机名与工作组（与「系统属性 → 计算机名」相同）。\r\n" +
                   "NetBIOS 名称最长 15 字符，修改后必须重启才能完全生效。",
            Location = new Point(16, 12),
            Size = new Size(428, 44),
            ForeColor = AppTheme.TextMute,
        };

        _currentName.Text = "当前计算机名：" + info.ComputerName;
        _currentName.SetBounds(16, 62, 428, 20);
        _currentName.ForeColor = AppTheme.TextHeader;

        _currentGroup.Text = info.PartOfDomain
            ? $"当前：已加入域「{info.Domain}」（无法在此修改工作组）"
            : "当前工作组：" + info.Workgroup;
        _currentGroup.SetBounds(16, 84, 428, 20);
        _currentGroup.ForeColor = info.PartOfDomain ? AppTheme.ScopeServer : AppTheme.TextHeader;

        AddField("新计算机名（留空表示不修改）", _newName, 118, info.ComputerName);
        AddField("新工作组名（留空表示不修改）", _newWorkgroup, 188, info.PartOfDomain ? "" : info.Workgroup);
        _newWorkgroup.Enabled = !info.PartOfDomain;

        _restart.Text = "应用成功后 60 秒后自动重启（可运行 shutdown /a 取消）";
        _restart.Location = new Point(16, 248);
        _restart.AutoSize = true;
        _restart.Checked = true;
        _restart.ForeColor = AppTheme.TextMain;

        var ok = new Button
        {
            Text = "应用",
            Location = new Point(268, 288),
            Size = new Size(88, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.Primary,
            ForeColor = AppTheme.TextOnPrimary,
        };
        ok.FlatAppearance.BorderSize = 0;
        ok.Click += (_, _) => ApplyChanges();

        var cancel = new Button
        {
            Text = "取消",
            DialogResult = DialogResult.Cancel,
            Location = new Point(364, 288),
            Size = new Size(80, 32),
            FlatStyle = FlatStyle.Flat,
        };

        CancelButton = cancel;
        Controls.AddRange([tip, _currentName, _currentGroup, _restart, ok, cancel]);
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

        var confirm = MessageBox.Show(
            (renameChanged ? $"计算机名 → {rename.ToUpperInvariant()}\r\n" : "") +
            (workgroupChanged ? $"工作组 → {workgroup.ToUpperInvariant()}\r\n" : "") +
            "\r\n更改后需重启生效。是否继续？",
            "确认修改",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);
        if (confirm != DialogResult.Yes) return;

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

    private void AddField(string label, TextBox box, int y, string value)
    {
        Controls.Add(new Label
        {
            Text = label,
            Location = new Point(16, y),
            AutoSize = true,
            ForeColor = AppTheme.TextHeader,
        });
        box.SetBounds(16, y + 22, 428, 26);
        box.Text = value;
        Controls.Add(box);
    }
}
