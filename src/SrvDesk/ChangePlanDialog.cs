namespace SrvDesk;

internal sealed class ChangePlanDialog : Form
{
    private readonly ChangePlan _plan;
    private readonly ListView _list = new();
    public bool Confirmed { get; private set; }
    public bool DryRunOnly => _plan.DryRunOnly;

    public ChangePlanDialog(ChangePlan plan)
    {
        _plan = plan;
        Text = plan.DryRunOnly
            ? AppLang.L("变更计划（仅检测 / 干跑）", "Change plan (dry run)")
            : AppLang.L("变更计划 · 确认后执行", "Change plan · confirm to apply");
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(780, 520);
        MinimumSize = new Size(640, 400);
        Font = UiFit.UiFont;
        BackColor = AppTheme.Surface;

        var body = ThemedSettingsChrome.CreateBodyPanel();
        var summary = new Label
        {
            Dock = DockStyle.Top,
            Height = UiScale.S(48),
            Text = plan.SummaryText() + "\r\n" + OptimizationLevelUi.Describe(plan.Level),
            ForeColor = AppTheme.TextMain,
        };

        _list.Dock = DockStyle.Fill;
        _list.View = View.Details;
        _list.CheckBoxes = !plan.DryRunOnly;
        _list.FullRowSelect = true;
        _list.BackColor = AppTheme.SurfaceCard;
        _list.Columns.Add(AppLang.L("模块", "Module"), 90);
        _list.Columns.Add(AppLang.L("项目", "Item"), 260);
        _list.Columns.Add(AppLang.L("动作", "Action"), 140);
        _list.Columns.Add(AppLang.L("风险", "Risk"), 50);
        _list.Columns.Add(AppLang.L("原因", "Why"), 200);
        foreach (var it in plan.Items)
        {
            var row = new ListViewItem(it.Module) { Tag = it, Checked = it.Selected };
            row.SubItems.Add(it.Title);
            row.SubItems.Add(it.Action);
            row.SubItems.Add(OptimizationLevelUi.RiskText(it.Risk));
            row.SubItems.Add(it.Reason);
            row.ForeColor = OptimizationLevelUi.RiskColor(it.Risk);
            if (it.Risk == OptimizeRisk.Critical)
                row.Checked = false;
            _list.Items.Add(row);
        }

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = UiFit.ControlHeight() + 16,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 8, 0, 0),
        };
        var cancel = ThemedSettingsChrome.CreateButton(AppLang.L("取消", "Cancel"), false);
        cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        var ok = ThemedSettingsChrome.CreateButton(
            plan.DryRunOnly ? AppLang.L("关闭", "Close") : AppLang.L("确认执行", "Apply selected"),
            true);
        ok.Margin = new Padding(8, 0, 0, 0);
        ok.Click += (_, _) =>
        {
            if (!plan.DryRunOnly)
            {
                foreach (ListViewItem row in _list.Items)
                {
                    if (row.Tag is ChangePlanItem it)
                        it.Selected = row.Checked && it.Risk != OptimizeRisk.Critical;
                }
                Confirmed = true;
            }
            DialogResult = DialogResult.OK;
            Close();
        };
        footer.Controls.Add(cancel);
        footer.Controls.Add(ok);

        body.Controls.Add(_list);
        body.Controls.Add(footer);
        body.Controls.Add(summary);
        Controls.Add(body);
        AcceptButton = ok;
        CancelButton = cancel;
    }
}

internal sealed class ServerProfileDialog : Form
{
    private readonly Dictionary<ServerRoleFlags, CheckBox> _boxes = new();
    private readonly ComboBox _level = new();
    private readonly CheckBox _inspect = new();

    public ServerProfileDialog()
    {
        Text = AppLang.L("服务器用途 / 优化等级", "Server profile / level");
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(440, 480);
        Font = UiFit.UiFont;
        BackColor = AppTheme.SurfaceCard;

        var y = 16;
        foreach (ServerRoleFlags f in Enum.GetValues(typeof(ServerRoleFlags)))
        {
            if (f == ServerRoleFlags.None) continue;
            var cb = new CheckBox
            {
                Text = ServerProfile.RoleTitle(f),
                Location = new Point(20, y),
                AutoSize = true,
                Checked = ServerProfile.Has(f),
            };
            _boxes[f] = cb;
            Controls.Add(cb);
            y += 26;
        }

        Controls.Add(new Label
        {
            Text = AppLang.L("优化等级", "Optimization level"),
            Location = new Point(20, y + 8),
            AutoSize = true,
            ForeColor = AppTheme.TextHeader,
        });
        _level.DropDownStyle = ComboBoxStyle.DropDownList;
        _level.Location = new Point(20, y + 32);
        _level.Width = 280;
        foreach (OptimizationLevel lv in Enum.GetValues(typeof(OptimizationLevel)))
            _level.Items.Add(OptimizationLevelUi.Title(lv));
        _level.SelectedIndex = (int)ServerProfile.Level;
        UiFit.FitCombo(_level);

        _inspect.Text = AppLang.L("启用持续健康巡检（约每 6 小时）", "Enable health inspection (~every 6h)");
        _inspect.Location = new Point(20, y + 70);
        _inspect.AutoSize = true;
        _inspect.Checked = ServerProfile.Load().HealthInspectionEnabled;

        var detect = ThemedSettingsChrome.CreateButton(AppLang.L("自动探测", "Detect"), false);
        detect.Location = new Point(20, 430);
        detect.Click += (_, _) =>
        {
            ServerRoleDetector.MergeDetectedIntoProfile();
            foreach (var kv in _boxes)
                kv.Value.Checked = ServerProfile.Has(kv.Key);
        };
        var save = ThemedSettingsChrome.CreateButton(AppLang.L("保存", "Save"), true);
        save.Location = new Point(320, 430);
        save.Click += (_, _) =>
        {
            ServerRoleFlags roles = ServerRoleFlags.None;
            foreach (var kv in _boxes)
                if (kv.Value.Checked) roles |= kv.Key;
            var data = ServerProfile.Load();
            data.Roles = (int)roles;
            data.OptimizationLevel = Math.Max(0, _level.SelectedIndex);
            data.HealthInspectionEnabled = _inspect.Checked;
            data.ProfileConfigured = true;
            ServerProfile.Save(data);
            HealthInspectionService.Stop();
            HealthInspectionService.StartIfEnabled(Owner as Form);
            DialogResult = DialogResult.OK;
            Close();
        };

        Controls.AddRange([_level, _inspect, detect, save]);
        AcceptButton = save;
    }
}
