namespace SrvDesk;

/// <summary>健康总览：评分、问题列表与资源摘要。</summary>
internal sealed class HealthOverviewDialog : Form
{
    private readonly Label _score = new();
    private readonly Label _dims = new();
    private readonly ListView _issues = new();
    private readonly TextBox _insights = new();

    public HealthOverviewDialog()
    {
        Text = AppLang.L("健康总览", "Health overview");
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(860, 640);
        MinimumSize = new Size(720, 520);
        Font = UiFit.UiFont;
        BackColor = AppTheme.Surface;

        var body = ThemedSettingsChrome.CreateBodyPanel();

        var head = new Panel { Dock = DockStyle.Top, Height = UiScale.S(84), BackColor = AppTheme.SurfaceCard };
        _score.Font = UiFit.UiFontBold(28f);
        _score.ForeColor = AppTheme.Primary;
        _score.Location = new Point(16, 12);
        _score.AutoSize = true;
        _dims.Location = new Point(16, 52);
        _dims.AutoSize = true;
        _dims.ForeColor = AppTheme.TextMain;
        head.Controls.AddRange([_score, _dims]);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = UiFit.ControlHeight() + 12,
            Padding = new Padding(0, 8, 0, 4),
            WrapContents = false,
        };
        void AddBtn(string zh, string en, Action a)
        {
            var b = ThemedSettingsChrome.CreateButton(AppLang.L(zh, en), false);
            b.Margin = new Padding(0, 0, 8, 0);
            b.Click += (_, _) => a();
            actions.Controls.Add(b);
        }
        AddBtn("刷新评分", "Refresh", RefreshReport);
        AddBtn("服务器用途…", "Profile…", () => { using var d = new ServerProfileDialog(); d.ShowDialog(this); RefreshReport(); });
        AddBtn("优化建议…", "Advice…", () => { using var d = new RecommendCenterDialog(); d.ShowDialog(this); });
        AddBtn("端口暴露…", "Ports…", () => { using var d = new PortExposureDialog(); d.ShowDialog(this); });
        AddBtn("计划任务…", "Tasks…", () => { using var d = new ScheduledTaskDialog(); d.ShowDialog(this); });
        AddBtn("优化历史…", "History…", () => { using var d = new OptimizationHistoryDialog(); d.ShowDialog(this); });
        AddBtn("立即巡检", "Inspect now", () =>
        {
            var path = HealthInspectionService.RunOnce(silent: false);
            MessageBox.Show(this, AppLang.L("报告已写入：", "Report written: ") + path, Text,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            RefreshReport();
        });

        _issues.Dock = DockStyle.Fill;
        _issues.View = View.Details;
        _issues.FullRowSelect = true;
        _issues.BackColor = AppTheme.SurfaceCard;
        _issues.Columns.Add(AppLang.L("风险", "Risk"), 50);
        _issues.Columns.Add(AppLang.L("领域", "Area"), 70);
        _issues.Columns.Add(AppLang.L("问题", "Issue"), 180);
        _issues.Columns.Add(AppLang.L("详情", "Detail"), 220);
        _issues.Columns.Add(AppLang.L("建议", "Hint"), 220);

        _insights.Dock = DockStyle.Bottom;
        _insights.Height = UiScale.S(140);
        _insights.Multiline = true;
        _insights.ScrollBars = ScrollBars.Vertical;
        _insights.ReadOnly = true;
        _insights.BackColor = AppTheme.SurfaceCard;
        _insights.BorderStyle = BorderStyle.FixedSingle;

        body.Controls.Add(_issues);
        body.Controls.Add(_insights);
        body.Controls.Add(actions);
        body.Controls.Add(head);
        Controls.Add(body);

        Load += (_, _) => RefreshReport();
    }

    private void RefreshReport()
    {
        var r = HealthScoreEngine.Evaluate();
        _score.Text = AppLang.Lf("健康度  {0} / 100", "Health  {0} / 100", r.Total);
        _dims.Text = AppLang.Lf(
            "性能 {0} · 稳定 {1} · 安全 {2} · 网络 {3} · 存储 {4} · 系统 {5}",
            "Perf {0} · Stab {1} · Sec {2} · Net {3} · Stor {4} · Sys {5}",
            r.Performance, r.Stability, r.Security, r.Network, r.Storage, r.System);
        _issues.Items.Clear();
        foreach (var i in r.Issues)
        {
            var row = new ListViewItem(OptimizationLevelUi.RiskText(i.Risk));
            row.SubItems.Add(i.Area);
            row.SubItems.Add(i.Title);
            row.SubItems.Add(i.Detail);
            row.SubItems.Add(i.Hint);
            row.ForeColor = OptimizationLevelUi.RiskColor(i.Risk);
            _issues.Items.Add(row);
        }
        _insights.Text = DiskMemoryNetworkInsights.BuildReportText();
    }
}

internal sealed class RecommendCenterDialog : Form
{
    public RecommendCenterDialog()
    {
        Text = AppLang.L("优化建议中心", "Recommendation center");
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(820, 560);
        Font = UiFit.UiFont;

        var body = ThemedSettingsChrome.CreateBodyPanel();
        var list = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            BackColor = AppTheme.SurfaceCard,
        };
        list.Columns.Add(AppLang.L("模块", "Module"), 80);
        list.Columns.Add(AppLang.L("建议", "Advice"), 280);
        list.Columns.Add(AppLang.L("风险", "Risk"), 50);
        list.Columns.Add(AppLang.L("原因", "Why"), 280);

        var plan = new ChangePlan { Level = ServerProfile.Level };
        ChangePlanBuilder.AppendServiceSuggestions(plan, 80);
        foreach (var issue in HealthScoreEngine.Evaluate().Issues)
        {
            plan.Items.Add(new ChangePlanItem
            {
                Module = issue.Area,
                Title = issue.Title,
                Action = issue.Hint,
                Reason = issue.Detail,
                Risk = issue.Risk,
            });
        }
        foreach (var it in plan.Items)
        {
            var row = new ListViewItem(it.Module);
            row.SubItems.Add(string.IsNullOrEmpty(it.Action) ? it.Title : it.Title + " → " + it.Action);
            row.SubItems.Add(OptimizationLevelUi.RiskText(it.Risk));
            row.SubItems.Add(it.Reason);
            row.ForeColor = OptimizationLevelUi.RiskColor(it.Risk);
            list.Items.Add(row);
        }

        var bar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = UiFit.ControlHeight() + 12, Padding = new Padding(0, 8, 0, 0) };
        var dry = ThemedSettingsChrome.CreateButton(AppLang.L("生成干跑计划…", "Dry-run plan…"), true);
        dry.Click += (_, _) =>
        {
            var p = new ChangePlan { Level = OptimizationLevel.DetectOnly, DryRunOnly = true };
            try
            {
                var cur = Optimizer.Read(fullScan: false);
                p = ChangePlanBuilder.FromToggleDiff(cur, cur, OptimizationLevel.DetectOnly);
                p.DryRunOnly = true;
            }
            catch { /* ignore */ }
            ChangePlanBuilder.AppendServiceSuggestions(p, 30);
            using var d = new ChangePlanDialog(p);
            d.ShowDialog(this);
        };
        bar.Controls.Add(dry);

        body.Controls.Add(list);
        body.Controls.Add(bar);
        Controls.Add(body);
    }
}

internal sealed class PortExposureDialog : Form
{
    public PortExposureDialog()
    {
        Text = AppLang.L("端口与暴露面", "Ports & exposure");
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(720, 480);
        Font = UiFit.UiFont;

        var body = ThemedSettingsChrome.CreateBodyPanel();
        var list = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            BackColor = AppTheme.SurfaceCard,
        };
        list.Columns.Add("Proto", 50);
        list.Columns.Add("Port", 60);
        list.Columns.Add(AppLang.L("本地地址", "Local"), 140);
        list.Columns.Add("PID", 60);
        list.Columns.Add(AppLang.L("进程", "Process"), 160);
        list.Columns.Add(AppLang.L("标记", "Flag"), 120);
        foreach (var p in PortExposureHelper.Scan())
        {
            var row = new ListViewItem(p.Protocol);
            row.SubItems.Add(p.Port.ToString());
            row.SubItems.Add(p.LocalAddress);
            row.SubItems.Add(p.Pid.ToString());
            row.SubItems.Add(p.ProcessName);
            var flag = "";
            if (p.IsSensitive) flag += "敏感 ";
            if (p.ListensOnAllInterfaces) flag += AppLang.L("全网卡", "all-NIC");
            row.SubItems.Add(flag.Trim());
            if (p.ListensOnAllInterfaces && p.IsSensitive)
                row.ForeColor = OptimizationLevelUi.RiskColor(OptimizeRisk.High);
            list.Items.Add(row);
        }
        body.Controls.Add(list);
        Controls.Add(body);
    }
}

internal sealed class ScheduledTaskDialog : Form
{
    private List<ScheduledTaskAdvice> _items = [];
    private readonly ListView _list = new();

    public ScheduledTaskDialog()
    {
        Text = AppLang.L("计划任务优化", "Scheduled tasks");
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(860, 560);
        Font = UiFit.UiFont;

        var body = ThemedSettingsChrome.CreateBodyPanel();
        _list.Dock = DockStyle.Fill;
        _list.View = View.Details;
        _list.FullRowSelect = true;
        _list.MultiSelect = true;
        _list.BackColor = AppTheme.SurfaceCard;
        _list.Columns.Add(AppLang.L("分类", "Bucket"), 80);
        _list.Columns.Add(AppLang.L("任务", "Task"), 280);
        _list.Columns.Add(AppLang.L("状态", "Status"), 70);
        _list.Columns.Add(AppLang.L("风险", "Risk"), 50);
        _list.Columns.Add(AppLang.L("建议", "Advice"), 220);

        var bar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = UiFit.ControlHeight() + 12, Padding = new Padding(0, 8, 0, 0) };
        var refresh = ThemedSettingsChrome.CreateButton(AppLang.L("刷新", "Refresh"), false);
        refresh.Click += (_, _) => Reload();
        var disable = ThemedSettingsChrome.CreateButton(AppLang.L("禁用所选（仅允许项）", "Disable selected (allowed)"), true);
        disable.Margin = new Padding(8, 0, 0, 0);
        disable.Click += (_, _) => ToggleSelected(false);
        var enable = ThemedSettingsChrome.CreateButton(AppLang.L("启用所选", "Enable selected"), false);
        enable.Margin = new Padding(8, 0, 0, 0);
        enable.Click += (_, _) => ToggleSelected(true);
        bar.Controls.AddRange([refresh, disable, enable]);

        body.Controls.Add(_list);
        body.Controls.Add(bar);
        Controls.Add(body);
        Load += (_, _) => Reload();
    }

    private void Reload()
    {
        _list.Items.Clear();
        _items = ScheduledTaskHelper.ListMicrosoftTasks().ToList();
        foreach (var t in _items)
        {
            var bucket = t.Bucket switch
            {
                ScheduledTaskBucket.Core => AppLang.L("核心", "Core"),
                ScheduledTaskBucket.Security => AppLang.L("安全", "Security"),
                ScheduledTaskBucket.Update => AppLang.L("更新", "Update"),
                ScheduledTaskBucket.Maintenance => AppLang.L("维护", "Maint"),
                ScheduledTaskBucket.Telemetry => AppLang.L("遥测", "Telemetry"),
                _ => AppLang.L("第三方", "3rd"),
            };
            var row = new ListViewItem(bucket) { Tag = t };
            row.SubItems.Add(t.Path);
            row.SubItems.Add(t.Enabled ? AppLang.L("启用", "On") : AppLang.L("禁用", "Off"));
            row.SubItems.Add(OptimizationLevelUi.RiskText(t.Risk));
            row.SubItems.Add(t.Advice);
            row.ForeColor = OptimizationLevelUi.RiskColor(t.Risk);
            _list.Items.Add(row);
        }
    }

    private void ToggleSelected(bool enable)
    {
        foreach (ListViewItem row in _list.SelectedItems)
        {
            if (row.Tag is not ScheduledTaskAdvice t) continue;
            if (!enable && !t.CanToggle)
            {
                MessageBox.Show(this, AppLang.L("该项不允许禁用：", "Not allowed to disable: ") + t.Path,
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                continue;
            }
            try { ScheduledTaskHelper.SetEnabled(t.Path, enable); }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        Reload();
    }
}

internal sealed class OptimizationHistoryDialog : Form
{
    public OptimizationHistoryDialog()
    {
        Text = AppLang.L("优化历史 / 回滚入口", "Optimization history / rollback");
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(720, 480);
        Font = UiFit.UiFont;

        var body = ThemedSettingsChrome.CreateBodyPanel();
        var tip = new Label
        {
            Dock = DockStyle.Top,
            Height = 40,
            Text = AppLang.L("服务启动类型可通过快照还原；系统还原点请用系统 rstrui。",
                "Service start types restore via snapshots; OS restore points via rstrui."),
            ForeColor = AppTheme.TextMute,
        };
        var list = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            BackColor = AppTheme.SurfaceCard,
        };
        list.Columns.Add(AppLang.L("时间", "Time"), 140);
        list.Columns.Add(AppLang.L("标题", "Title"), 200);
        list.Columns.Add(AppLang.L("详情", "Detail"), 300);
        foreach (var e in OptimizationHistory.List())
        {
            var row = new ListViewItem(e.TimeLocal) { Tag = e };
            row.SubItems.Add(e.Title);
            row.SubItems.Add(e.Detail);
            list.Items.Add(row);
        }

        var bar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = UiFit.ControlHeight() + 12, Padding = new Padding(0, 8, 0, 0) };
        var snap = ThemedSettingsChrome.CreateButton(AppLang.L("打开服务快照还原…", "Service snapshot restore…"), true);
        snap.Click += (_, _) =>
        {
            using var d = new ServiceSnapshotRestoreDialog();
            d.ShowDialog(this);
        };
        var rstrui = ThemedSettingsChrome.CreateButton(AppLang.L("系统还原 rstrui", "System Restore"), false);
        rstrui.Margin = new Padding(8, 0, 0, 0);
        rstrui.Click += (_, _) =>
        {
            try { System.Diagnostics.Process.Start("rstrui.exe"); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        };
        bar.Controls.AddRange([snap, rstrui]);
        body.Controls.Add(list);
        body.Controls.Add(bar);
        body.Controls.Add(tip);
        Controls.Add(body);
    }
}
