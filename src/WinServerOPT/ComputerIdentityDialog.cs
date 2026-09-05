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

    public ComputerIdentityDialog(ComputerIdentityInfo info, bool optional = false)
    {
        _info = info;
        Text = "计算机名 / 工作组";
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(500, 400);
        CancelButton = null;

        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 8, 16, 56), BackColor = AppTheme.Surface };

        var tip = new Label
        {
            Text = optional
                ? "此步可选：不改可直接点「跳过」。改名/改工作组后需重启才会完全生效。"
                : "通过 WMI 修改计算机名与工作组。NetBIOS 名称最长 15 字符，修改后需重启才能完全生效。",
            Dock = DockStyle.Top,
            Height = 40,
            ForeColor = AppTheme.TextMute,
        };

        _currentName.Text = "当前计算机名：" + info.ComputerName;
        _currentName.SetBounds(0, 48, 448, 20);
        _currentName.ForeColor = AppTheme.TextHeader;

        _currentGroup.Text = info.PartOfDomain
            ? $"当前：已加入域「{info.Domain}」（无法在此修改工作组）"
            : "当前工作组：" + info.Workgroup;
        _currentGroup.SetBounds(0, 70, 448, 20);
        _currentGroup.ForeColor = info.PartOfDomain ? AppTheme.ScopeServer : AppTheme.TextHeader;

        var form = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 96, 0, 0) };
        AddField(form, "新计算机名（与当前相同或留空 = 不改）", _newName, 0, info.ComputerName);
        AddField(form, "新工作组名（与当前相同或留空 = 不改）", _newWorkgroup, 72, info.PartOfDomain ? "" : info.Workgroup);
        _newWorkgroup.Enabled = !info.PartOfDomain;

        _restart.Text = "应用成功后 60 秒自动重启（可执行 shutdown /a 取消）";
        _restart.Location = new Point(0, 156);
        _restart.AutoSize = true;
        _restart.Checked = false; // 默认不重启，避免打断用户

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 6, 0, 0),
            BackColor = AppTheme.Surface,
        };

        var apply = ThemedSettingsChrome.CreateButton("应用修改", true);
        apply.Size = new Size(100, 34);
        apply.Margin = new Padding(6, 0, 0, 0);
        apply.Click += (_, _) => ApplyChanges();

        var skip = ThemedSettingsChrome.CreateButton(optional ? "跳过" : "取消", false);
        skip.Size = new Size(88, 34);
        skip.Margin = new Padding(6, 0, 0, 0);
        skip.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        buttons.Controls.Add(apply);
        buttons.Controls.Add(skip);

        body.Controls.Add(buttons);
        body.Controls.Add(_restart);
        body.Controls.Add(form);
        body.Controls.Add(_currentGroup);
        body.Controls.Add(_currentName);
        body.Controls.Add(tip);

        ThemedSettingsChrome.MountModal(
            this,
            "计算机名 / 工作组",
            optional ? "可选步骤 · 与系统属性相同" : "与「系统属性 → 计算机名」相同",
            body,
            "不修改可关闭本窗口；改名后请自行安排重启。");
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
            MessageBox.Show(this, "未修改任何项。若暂不改名，请点「跳过」或「取消」。", Text,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
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

    private static void AddField(Panel parent, string label, TextBox box, int y, string value)
    {
        parent.Controls.Add(new Label
        {
            Text = label,
            Location = new Point(0, y),
            AutoSize = true,
            ForeColor = AppTheme.TextHeader,
        });
        box.SetBounds(0, y + 22, 448, 26);
        box.Text = value;
        parent.Controls.Add(box);
    }
}
