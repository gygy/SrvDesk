namespace SrvDesk;

internal sealed class ComputerIdentityDialog : Form
{
    private readonly TextBox _newName = new();
    private readonly TextBox _newWorkgroup = new();
    private readonly CheckBox _restart = new();
    private readonly ComputerIdentityInfo _info;
    private readonly string _suggestedName;
    private readonly FlowLayoutPanel _stack = new();

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
        // 固定尺寸：头部 + 正文 + 按钮 + 底栏，内容一次看全、不出现滚动条
        ClientSize = new Size(540, 470);
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

        var apply = ThemedSettingsChrome.CreateButton("应用修改", true);
        apply.Height = 32;
        apply.Margin = new Padding(6, 0, 0, 0);
        apply.Click += (_, _) => ApplyChanges();

        var skip = ThemedSettingsChrome.CreateButton(optional ? "跳过" : "取消", false);
        skip.Height = 32;
        skip.Margin = new Padding(6, 0, 0, 0);
        skip.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        buttons.Controls.Add(apply);
        buttons.Controls.Add(skip);

        _stack.Dock = DockStyle.Fill;
        _stack.FlowDirection = FlowDirection.TopDown;
        _stack.WrapContents = false;
        _stack.AutoScroll = false;
        _stack.BackColor = AppTheme.Surface;
        _stack.Padding = new Padding(0);

        _stack.Controls.Add(MakeTip(optional
            ? "此步可选：不改可直接点「跳过」。改名后需重启才会完全生效。"
            : "通过 WMI 修改。NetBIOS 名称最长 15 字符，修改后需重启才会完全生效。"));

        _stack.Controls.Add(MakeInfoLine("当前计算机名", info.ComputerName));
        _stack.Controls.Add(MakeInfoLine(
            info.PartOfDomain ? "当前域" : "当前工作组",
            info.PartOfDomain ? info.Domain : info.Workgroup,
            info.PartOfDomain ? AppTheme.ScopeServer : AppTheme.TextHeader));

        if (info.PartOfDomain)
            _stack.Controls.Add(MakeTip("已加入域，无法在此修改工作组；仅可改计算机名。", compact: true));

        _stack.Controls.Add(MakeSpacer(6));
        _stack.Controls.Add(MakeFieldLabel($"新计算机名（建议：{_suggestedName}）"));
        _newName.Height = 26;
        _newName.Margin = new Padding(0, 2, 0, 2);
        _newName.MaxLength = 15;
        _newName.Text = _suggestedName;
        _stack.Controls.Add(_newName);
        _stack.Controls.Add(MakeTip(
            "按产品名称生成（如 Windows Server 2022 → win2022）。与当前相同或清空 = 不改。",
            compact: true));

        _stack.Controls.Add(MakeSpacer(8));
        _stack.Controls.Add(MakeFieldLabel("新工作组名（与当前相同或留空 = 不改）"));
        _newWorkgroup.Height = 26;
        _newWorkgroup.Margin = new Padding(0, 2, 0, 2);
        _newWorkgroup.MaxLength = 15;
        _newWorkgroup.Text = info.PartOfDomain ? "" : info.Workgroup;
        _newWorkgroup.Enabled = !info.PartOfDomain;
        _stack.Controls.Add(_newWorkgroup);

        _stack.Controls.Add(MakeSpacer(10));
        _restart.Text = "应用成功后 60 秒自动重启（可执行 shutdown /a 取消）";
        _restart.AutoSize = true;
        _restart.Margin = new Padding(0, 0, 0, 2);
        _restart.Checked = false;
        _stack.Controls.Add(_restart);

        body.Controls.Add(_stack);
        body.Controls.Add(buttons);
        body.Resize += (_, _) => SyncContentWidth();

        ThemedSettingsChrome.MountModal(
            this,
            "计算机名 / 工作组",
            optional ? "可选步骤 · 与系统属性相同" : "与「系统属性 → 计算机名」相同",
            body,
            "");

        Shown += (_, _) => SyncContentWidth();
    }

    private void SyncContentWidth()
    {
        var w = Math.Max(360, _stack.ClientSize.Width - 4);
        foreach (Control c in _stack.Controls)
        {
            if (c is TextBox or Panel or Label { AutoSize: false })
                c.Width = w;
            else if (c is Label { AutoSize: true } lbl)
                lbl.MaximumSize = new Size(w, 0);
        }
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
        Width = 480,
        Height = compact ? 32 : 36,
        Margin = new Padding(0, 0, 0, compact ? 2 : 6),
        ForeColor = AppTheme.TextMute,
    };

    private static Panel MakeInfoLine(string caption, string value, Color? valueColor = null)
    {
        var row = new Panel
        {
            Width = 480,
            Height = 22,
            Margin = new Padding(0, 0, 0, 2),
            BackColor = Color.Transparent,
        };
        var left = new Label
        {
            Text = caption + "：",
            AutoSize = true,
            Location = new Point(0, 2),
            ForeColor = AppTheme.TextMute,
        };
        var right = new Label
        {
            Text = value,
            AutoSize = true,
            Location = new Point(110, 2),
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
        MaximumSize = new Size(480, 0),
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
