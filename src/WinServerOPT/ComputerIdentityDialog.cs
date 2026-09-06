namespace WinOpt;

internal sealed class ComputerIdentityDialog : Form
{
    private readonly TextBox _newName = new();
    private readonly TextBox _newWorkgroup = new();
    private readonly CheckBox _restart = new();
    private readonly ComputerIdentityInfo _info;
    private readonly string _suggestedName;

    public bool RestartScheduled { get; private set; }

    public ComputerIdentityDialog(ComputerIdentityInfo info, bool optional = false)
    {
        _info = info;
        _suggestedName = ComputerIdentityHelper.SuggestComputerName();
        Text = "计算机名 / 工作组";
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(520, 460);
        CancelButton = null;

        var body = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 10, 16, 8),
            BackColor = AppTheme.Surface,
        };

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

        var stack = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            BackColor = AppTheme.Surface,
            Padding = new Padding(0),
        };

        stack.Controls.Add(MakeTip(optional
            ? "此步可选：不改可直接点「跳过」。改名/改工作组后需重启才会完全生效。"
            : "通过 WMI 修改计算机名与工作组。NetBIOS 名称最长 15 字符，修改后需重启才能完全生效。"));

        stack.Controls.Add(MakeInfoLine("当前计算机名", info.ComputerName));
        stack.Controls.Add(MakeInfoLine(
            info.PartOfDomain ? "当前域" : "当前工作组",
            info.PartOfDomain ? info.Domain : info.Workgroup,
            info.PartOfDomain ? AppTheme.ScopeServer : AppTheme.TextHeader));

        if (info.PartOfDomain)
        {
            stack.Controls.Add(MakeTip("已加入域，无法在此修改工作组；仅可改计算机名。", compact: true));
        }

        stack.Controls.Add(MakeSpacer(8));
        stack.Controls.Add(MakeFieldLabel($"新计算机名（建议：{_suggestedName}）"));
        _newName.Width = 470;
        _newName.Height = 26;
        _newName.Margin = new Padding(0, 2, 0, 4);
        _newName.MaxLength = 15;
        _newName.Text = _suggestedName;
        stack.Controls.Add(_newName);
        stack.Controls.Add(MakeTip(
            "根据系统「产品名称」自动生成（如 Windows Server 2022 → win2022）。与当前相同或清空 = 不改。",
            compact: true));

        stack.Controls.Add(MakeSpacer(10));
        stack.Controls.Add(MakeFieldLabel("新工作组名（与当前相同或留空 = 不改）"));
        _newWorkgroup.Width = 470;
        _newWorkgroup.Height = 26;
        _newWorkgroup.Margin = new Padding(0, 2, 0, 4);
        _newWorkgroup.MaxLength = 15;
        _newWorkgroup.Text = info.PartOfDomain ? "" : info.Workgroup;
        _newWorkgroup.Enabled = !info.PartOfDomain;
        stack.Controls.Add(_newWorkgroup);

        stack.Controls.Add(MakeSpacer(12));
        _restart.Text = "应用成功后 60 秒自动重启（可执行 shutdown /a 取消）";
        _restart.AutoSize = true;
        _restart.Margin = new Padding(0, 0, 0, 4);
        _restart.Checked = false;
        stack.Controls.Add(_restart);

        body.Controls.Add(stack);
        body.Controls.Add(buttons);

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

    private static Label MakeTip(string text, bool compact = false) => new()
    {
        Text = text,
        AutoSize = false,
        Width = 470,
        Height = compact ? 36 : 40,
        Margin = new Padding(0, 0, 0, compact ? 4 : 8),
        ForeColor = AppTheme.TextMute,
    };

    private static Panel MakeInfoLine(string caption, string value, Color? valueColor = null)
    {
        var row = new Panel
        {
            Width = 470,
            Height = 24,
            Margin = new Padding(0, 0, 0, 4),
            BackColor = Color.Transparent,
        };
        var left = new Label
        {
            Text = caption + "：",
            AutoSize = true,
            Location = new Point(0, 3),
            ForeColor = AppTheme.TextMute,
        };
        var right = new Label
        {
            Text = value,
            AutoSize = true,
            Location = new Point(110, 3),
            ForeColor = valueColor ?? AppTheme.TextHeader,
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
        };
        row.Controls.Add(left);
        row.Controls.Add(right);
        return row;
    }

    private static Label MakeFieldLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        MaximumSize = new Size(470, 0),
        Margin = new Padding(0, 0, 0, 0),
        ForeColor = AppTheme.TextHeader,
    };

    private static Panel MakeSpacer(int height) => new()
    {
        Width = 10,
        Height = height,
        Margin = new Padding(0),
        BackColor = Color.Transparent,
    };
}
