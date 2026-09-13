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
        ClientSize = UiScale.Size(560, 560);
        Font = UiFit.UiFont;
        BackColor = AppTheme.SurfaceCard;

        var scroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(UiScale.S(20), UiScale.S(16), UiScale.S(20), UiScale.S(8)),
        };

        var stack = ThemedSettingsChrome.CreateToggleStack();
        stack.Dock = DockStyle.Top;
        stack.AutoSize = true;
        stack.AutoSizeMode = AutoSizeMode.GrowAndShrink;

        foreach (ServerRoleFlags f in Enum.GetValues(typeof(ServerRoleFlags)))
        {
            if (f == ServerRoleFlags.None) continue;
            var cb = new CheckBox
            {
                Text = ServerProfile.RoleTitle(f),
                AutoSize = false,
                Height = Math.Max(UiScale.S(28), UiFit.ControlHeight()),
                Checked = ServerProfile.Has(f),
                Font = UiFit.UiFont,
                Margin = new Padding(0, 0, 0, UiScale.S(2)),
            };
            _boxes[f] = cb;
            stack.Controls.Add(cb);
        }

        var levelLbl = new Label
        {
            Text = AppLang.L("优化等级", "Optimization level"),
            AutoSize = false,
            Height = Math.Max(UiScale.S(24), UiFit.ControlHeight(UiFit.UiFontSmall)),
            ForeColor = AppTheme.TextHeader,
            Font = UiFit.UiFont,
            Margin = new Padding(0, UiScale.S(12), 0, UiScale.S(4)),
            TextAlign = ContentAlignment.MiddleLeft,
        };
        _level.DropDownStyle = ComboBoxStyle.DropDownList;
        foreach (OptimizationLevel lv in Enum.GetValues(typeof(OptimizationLevel)))
            _level.Items.Add(OptimizationLevelUi.Title(lv));
        _level.SelectedIndex = (int)ServerProfile.Level;
        UiFit.FitCombo(_level);
        _level.Margin = new Padding(0, 0, 0, UiScale.S(8));

        _inspect.Text = AppLang.L("启用持续健康巡检（约每 6 小时）", "Enable health inspection (~every 6h)");
        _inspect.AutoSize = false;
        _inspect.Height = Math.Max(UiScale.S(28), UiFit.ControlHeight());
        _inspect.Checked = ServerProfile.Load().HealthInspectionEnabled;
        _inspect.Font = UiFit.UiFont;
        _inspect.Margin = new Padding(0, UiScale.S(4), 0, 0);

        stack.Controls.AddRange([levelLbl, _level, _inspect]);
        scroll.Controls.Add(stack);
        scroll.Resize += (_, _) => ThemedSettingsChrome.StretchStackChildren(stack);

        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = UiFit.ControlHeight() + UiScale.S(20),
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(UiScale.S(16), UiScale.S(8), UiScale.S(16), UiScale.S(8)),
            WrapContents = false,
            AutoScroll = false,
        };
        UiBuffer.ConfigureNoScrollRow(bar);
        var save = ThemedSettingsChrome.CreateButton(AppLang.L("保存", "Save"), true);
        UiFit.FitButton(save, padding: 28);
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
        var detect = ThemedSettingsChrome.CreateButton(AppLang.L("自动探测", "Detect"), false);
        UiFit.FitButton(detect, padding: 28);
        detect.Margin = new Padding(0, 0, UiScale.S(8), 0);
        detect.Click += (_, _) =>
        {
            ServerRoleDetector.MergeDetectedIntoProfile();
            foreach (var kv in _boxes)
                kv.Value.Checked = ServerProfile.Has(kv.Key);
        };
        bar.Controls.AddRange([save, detect]);

        Controls.Add(scroll);
        Controls.Add(bar);
        AcceptButton = save;
        Load += (_, _) => ThemedSettingsChrome.StretchStackChildren(stack);
    }
}
