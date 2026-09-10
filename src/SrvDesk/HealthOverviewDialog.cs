namespace SrvDesk;

/// <summary>优化顾问：按侧栏标签页归类的「未达推荐」诊断（可折叠）。</summary>
internal sealed class HealthOverviewDialog : Form
{
    private readonly MainForm? _main;
    private readonly Label _summary = new();
    private readonly BufferedPanel _scroll = new(composited: false)
    {
        Dock = DockStyle.Fill,
        AutoScroll = true,
        BackColor = AppTheme.Surface,
        Padding = new Padding(0, 4, 0, 0),
    };

    public HealthOverviewDialog(MainForm? main = null)
    {
        _main = main;
        Text = AppLang.L("优化顾问", "Optimization advisor");
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(900, 660);
        MinimumSize = new Size(720, 520);
        Font = UiFit.UiFont;
        BackColor = AppTheme.Surface;

        var body = new BufferedPanel(composited: false)
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            BackColor = AppTheme.Surface,
        };

        var head = new Panel { Dock = DockStyle.Top, Height = UiScale.S(56), BackColor = AppTheme.SurfaceCard };
        _summary.Font = UiFit.UiFontBold(14f);
        _summary.ForeColor = AppTheme.Primary;
        _summary.Location = new Point(16, 16);
        _summary.AutoSize = true;
        _summary.BackColor = Color.Transparent;
        head.Controls.Add(_summary);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = UiFit.ControlHeight() + 12,
            Padding = new Padding(0, 8, 0, 4),
            WrapContents = false,
            BackColor = AppTheme.Surface,
        };
        var refresh = ThemedSettingsChrome.CreateButton(AppLang.L("刷新诊断", "Refresh"), false);
        refresh.Margin = new Padding(0, 0, 8, 0);
        refresh.Click += (_, _) => Reload();
        var profile = ThemedSettingsChrome.CreateButton(AppLang.L("服务器用途…", "Profile…"), false);
        profile.Click += (_, _) =>
        {
            using var d = new ServerProfileDialog();
            d.ShowDialog(this);
            Reload();
        };
        actions.Controls.Add(refresh);
        actions.Controls.Add(profile);

        body.Controls.Add(_scroll);
        body.Controls.Add(actions);
        body.Controls.Add(head);
        Controls.Add(body);

        Load += (_, _) => Reload();
        Resize += (_, _) => LayoutGroups();
    }

    private void Reload()
    {
        var main = _main ?? Owner as MainForm;
        IReadOnlyList<TabOptimizeGroup> groups = Array.Empty<TabOptimizeGroup>();
        if (main is not null)
            groups = main.CollectTabOptimizeFindings(refreshFromSystem: true);

        var total = groups.Sum(g => g.Findings.Count);
        _summary.Text = total == 0
            ? AppLang.L("未发现需优化项（已达推荐值）", "Nothing to optimize — matches recommendations")
            : AppLang.Lf("待优化 {0} 项 · {1} 个标签页", "{0} items · {1} tabs", total, groups.Count);

        _scroll.SuspendLayout();
        _scroll.Controls.Clear();
        var y = 0;
        var width = Math.Max(200, _scroll.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 4);
        foreach (var g in groups)
        {
            var panel = BuildCollapsibleGroup(g, width);
            panel.Location = new Point(0, y);
            _scroll.Controls.Add(panel);
            y += panel.Height + UiScale.S(8);
        }
        _scroll.ResumeLayout(true);
        LayoutGroups();
    }

    private void LayoutGroups()
    {
        var width = Math.Max(200, _scroll.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 4);
        var y = 0;
        foreach (Control c in _scroll.Controls)
        {
            c.Width = width;
            if (c.Tag is GroupChrome chrome)
                chrome.ApplyWidth(width);
            c.Location = new Point(0, y);
            y += c.Height + UiScale.S(8);
        }
    }

    private Panel BuildCollapsibleGroup(TabOptimizeGroup group, int width)
    {
        const int headerH = 36;
        var rowH = Math.Max(28, UiFit.LineHeight() + 10);
        var findings = group.Findings;

        var section = new BufferedPanel
        {
            Width = width,
            Height = headerH + findings.Count * rowH,
            BackColor = AppTheme.SurfaceCard,
            Tag = null,
        };

        var head = new BufferedPanel
        {
            Location = new Point(0, 0),
            Size = new Size(width, headerH),
            BackColor = AppTheme.GroupBg,
            Cursor = Cursors.Hand,
        };
        var arrow = new Label
        {
            Text = "▼",
            Location = new Point(UiScale.S(12), UiScale.S(9)),
            AutoSize = true,
            ForeColor = AppTheme.PrimaryDark,
            Font = new Font(UiFit.UiFontFamily, 8F),
            BackColor = Color.Transparent,
        };
        var titleLabel = new Label
        {
            Text = AppLang.Lf("{0}（{1}）", "{0} ({1})", group.TabTitle, findings.Count),
            Location = new Point(UiScale.S(32), 0),
            Size = new Size(width - UiScale.S(40), headerH),
            ForeColor = AppTheme.TextHeader,
            Font = UiFit.UiFontBold(),
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent,
        };
        head.Controls.Add(arrow);
        head.Controls.Add(titleLabel);

        var body = new BufferedPanel
        {
            Location = new Point(0, headerH),
            Size = new Size(width, findings.Count * rowH),
            BackColor = AppTheme.SurfaceCard,
        };

        // 表头
        var colHeader = BuildFindingRow(
            AppLang.L("项目", "Item"),
            AppLang.L("当前", "Current"),
            AppLang.L("推荐", "Recommended"),
            AppLang.L("强度", "Level"),
            AppLang.L("说明", "Hint"),
            0, rowH, width, AppTheme.PrimaryPale, header: true);
        body.Controls.Add(colHeader);

        for (var i = 0; i < findings.Count; i++)
        {
            var f = findings[i];
            var bg = i % 2 == 0 ? AppTheme.SurfaceCard : AppTheme.RowAlt;
            var title = string.IsNullOrWhiteSpace(f.SectionTitle) || f.SectionTitle == group.TabTitle
                ? f.ItemTitle
                : f.SectionTitle + " · " + f.ItemTitle;
            body.Controls.Add(BuildFindingRow(
                title,
                f.CurrentValue,
                f.RecommendedValue,
                RecommendLevelUi.Title(f.Level),
                f.Hint,
                (i + 1) * rowH,
                rowH,
                width,
                bg,
                header: false,
                level: f.Level));
        }

        body.Height = (findings.Count + 1) * rowH;
        section.Height = headerH + body.Height;

        var chrome = new GroupChrome(section, head, body, arrow, titleLabel, headerH);
        section.Tag = chrome;

        void Toggle(object? _, EventArgs __)
        {
            chrome.Expanded = !chrome.Expanded;
            arrow.Text = chrome.Expanded ? "▼" : "▶";
            body.Visible = chrome.Expanded;
            section.Height = chrome.Expanded ? headerH + body.Height : headerH;
            LayoutGroups();
        }

        head.Click += Toggle;
        arrow.Click += Toggle;
        titleLabel.Click += Toggle;

        section.Controls.Add(body);
        section.Controls.Add(head);
        return section;
    }

    private static Control BuildFindingRow(
        string item,
        string current,
        string recommend,
        string levelText,
        string hint,
        int y,
        int h,
        int width,
        Color bg,
        bool header,
        RecommendLevel level = RecommendLevel.Suggested)
    {
        var wrap = new BufferedPanel
        {
            Location = new Point(0, y),
            Size = new Size(width, h),
            BackColor = bg,
        };

        var pad = UiScale.S(10);
        var wItem = Math.Max(160, (int)(width * 0.32));
        var wCur = Math.Max(70, (int)(width * 0.12));
        var wRec = Math.Max(70, (int)(width * 0.12));
        var wLvl = Math.Max(72, (int)(width * 0.12));
        var wHint = Math.Max(80, width - pad * 2 - wItem - wCur - wRec - wLvl);

        Label Cell(string text, int x, int w, Color fg, bool bold = false) => new()
        {
            Text = text,
            Location = new Point(x, 0),
            Size = new Size(w, h),
            ForeColor = fg,
            Font = bold ? UiFit.UiFontBold(9f) : UiFit.UiFont,
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent,
            AutoEllipsis = true,
        };

        var x = pad;
        wrap.Controls.Add(Cell(item, x, wItem, header ? AppTheme.TextHeader : AppTheme.TextMain, header));
        x += wItem;
        wrap.Controls.Add(Cell(current, x, wCur, header ? AppTheme.TextHeader : AppTheme.TextMute, header));
        x += wCur;
        wrap.Controls.Add(Cell(recommend, x, wRec, header ? AppTheme.TextHeader : AppTheme.PrimaryDark, header));
        x += wRec;
        var lvlFg = header ? AppTheme.TextHeader : RecommendLevelUi.ForeColorOf(level);
        wrap.Controls.Add(Cell(levelText, x, wLvl, lvlFg, header));
        x += wLvl;
        wrap.Controls.Add(Cell(hint, x, wHint, header ? AppTheme.TextHeader : AppTheme.TextMute, header));
        return wrap;
    }

    private sealed class GroupChrome
    {
        private readonly Panel _section;
        private readonly Panel _head;
        private readonly Panel _body;
        private readonly Label _title;

        public bool Expanded { get; set; } = true;
        public Label Arrow { get; }

        public GroupChrome(Panel section, Panel head, Panel body, Label arrow, Label title, int headerH)
        {
            _section = section;
            _head = head;
            _body = body;
            Arrow = arrow;
            _title = title;
            _ = headerH;
        }

        public void ApplyWidth(int width)
        {
            _section.Width = width;
            _head.Width = width;
            _body.Width = width;
            _title.Width = Math.Max(80, width - UiScale.S(40));
            foreach (Control row in _body.Controls)
                row.Width = width;
        }
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
        Text = AppLang.L("端口暴露", "Port exposure");
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
        Text = AppLang.L("回滚优化", "Rollback optimization");
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
        Controls.Add(body);
    }
}
