namespace SrvDesk;

/// <summary>优化顾问：按侧栏标签页归类的「未达推荐」诊断（可勾选 / 批量设推荐 / 应用到系统）。</summary>
internal sealed class HealthOverviewDialog : Form
{
    private readonly MainForm? _main;
    private readonly Label _summary = new();
    private readonly List<FindingRowChrome> _rows = [];
    private readonly List<GroupChrome> _groups = [];
    /// <summary>本会话已设为推荐/已处理的项，避免刷新时又跳回来。</summary>
    private readonly HashSet<string> _sessionResolved = new(StringComparer.Ordinal);
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
        ClientSize = new Size(960, 680);
        MinimumSize = new Size(760, 520);
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
        refresh.Click += (_, _) =>
        {
            _sessionResolved.Clear();
            Reload(refreshFromSystem: true);
        };
        var profile = ThemedSettingsChrome.CreateButton(AppLang.L("服务器用途…", "Profile…"), false);
        profile.Click += (_, _) =>
        {
            using var d = new ServerProfileDialog();
            d.ShowDialog(this);
            Reload(refreshFromSystem: true);
        };
        actions.Controls.Add(refresh);
        actions.Controls.Add(profile);

        var footer = BuildFooter();

        body.Controls.Add(_scroll);
        body.Controls.Add(footer);
        body.Controls.Add(actions);
        body.Controls.Add(head);
        Controls.Add(body);

        Load += (_, _) => Reload(refreshFromSystem: true);
        Resize += (_, _) => LayoutGroups();
    }

    private Panel BuildFooter()
    {
        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = UiFit.ControlHeight() + 28,
            BackColor = AppTheme.Surface,
            Padding = new Padding(0, 8, 0, 0),
        };

        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            WrapContents = false,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = AppTheme.Surface,
        };

        Button Btn(string zh, string en, bool primary, Action a)
        {
            var b = ThemedSettingsChrome.CreateButton(AppLang.L(zh, en), primary);
            b.Margin = new Padding(0, 0, 8, 0);
            b.Click += (_, _) => a();
            bar.Controls.Add(b);
            return b;
        }

        Btn("全选", "Select all", false, () => SetAllChecked(true));
        Btn("全不选", "Select none", false, () => SetAllChecked(false));
        Btn("设为推荐值", "Set recommended", false, ApplyRecommendedSelected);
        Btn("应用到系统…", "Apply to system…", true, ApplyToSystem);

        footer.Controls.Add(bar);
        return footer;
    }

    private MainForm? Main => _main ?? Owner as MainForm;

    private void Reload(bool refreshFromSystem)
    {
        var main = Main;
        IReadOnlyList<TabOptimizeGroup> groups = Array.Empty<TabOptimizeGroup>();
        if (main is not null)
            groups = main.CollectTabOptimizeFindings(refreshFromSystem);

        groups = ApplySessionFilter(groups);

        var total = groups.Sum(g => g.Findings.Count);
        _summary.Text = total == 0
            ? AppLang.L("未发现需优化项（已达推荐值）", "Nothing to optimize — matches recommendations")
            : AppLang.Lf("待优化 {0} 项", "{0} items to optimize", total);

        _rows.Clear();
        _groups.Clear();
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

    private IReadOnlyList<TabOptimizeGroup> ApplySessionFilter(IReadOnlyList<TabOptimizeGroup> groups)
    {
        if (_sessionResolved.Count == 0)
            return groups;

        var result = new List<TabOptimizeGroup>();
        foreach (var g in groups)
        {
            var kept = g.Findings.Where(f => !_sessionResolved.Contains(TabOptimizeFinding.KeyOf(f))).ToList();
            if (kept.Count == 0)
                continue;
            result.Add(new TabOptimizeGroup { TabTitle = g.TabTitle, Findings = kept });
        }
        return result;
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
        var rowH = Math.Max(30, UiFit.LineHeight() + 12);
        var findings = group.Findings;
        var groupRows = new List<FindingRowChrome>();

        var section = new BufferedPanel
        {
            Width = width,
            BackColor = AppTheme.SurfaceCard,
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
            Size = new Size(Math.Max(80, width - UiScale.S(220)), headerH),
            ForeColor = AppTheme.TextHeader,
            Font = UiFit.UiFontBold(),
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent,
        };

        var selectAll = MakeHeaderLink(AppLang.L("全选", "Select all"));
        var setGroup = MakeHeaderLink(AppLang.L("本组设为推荐", "Set group recommended"));
        void LayoutHeaderLinks()
        {
            var gap = UiScale.S(12);
            var x = width - UiScale.S(12);
            setGroup.Location = new Point(x - setGroup.Width, (headerH - setGroup.Height) / 2);
            x = setGroup.Left - gap;
            selectAll.Location = new Point(x - selectAll.Width, (headerH - selectAll.Height) / 2);
            titleLabel.Width = Math.Max(80, selectAll.Left - titleLabel.Left - 8);
        }

        selectAll.Click += (_, _) =>
        {
            var on = groupRows.Any(r => !r.Check.Checked);
            foreach (var r in groupRows)
                r.Check.Checked = on;
        };
        setGroup.Click += (_, _) =>
        {
            foreach (var r in groupRows)
                r.Check.Checked = true;
            ApplyRecommendedSelected();
        };

        head.Controls.Add(arrow);
        head.Controls.Add(titleLabel);
        head.Controls.Add(selectAll);
        head.Controls.Add(setGroup);
        LayoutHeaderLinks();

        var body = new BufferedPanel
        {
            Location = new Point(0, headerH),
            Size = new Size(width, (findings.Count + 1) * rowH),
            BackColor = AppTheme.SurfaceCard,
        };

        body.Controls.Add(BuildHeaderRow(0, rowH, width));

        for (var i = 0; i < findings.Count; i++)
        {
            var f = findings[i];
            var bg = i % 2 == 0 ? AppTheme.SurfaceCard : AppTheme.RowAlt;
            var title = string.IsNullOrWhiteSpace(f.SectionTitle) || f.SectionTitle == group.TabTitle
                ? f.ItemTitle
                : f.SectionTitle + " · " + f.ItemTitle;
            var row = BuildFindingRow(f, title, (i + 1) * rowH, rowH, width, bg);
            groupRows.Add(row);
            _rows.Add(row);
            body.Controls.Add(row.Wrap);
        }

        section.Height = headerH + body.Height;

        var chrome = new GroupChrome(section, head, body, arrow, titleLabel, selectAll, setGroup, headerH, LayoutHeaderLinks);
        section.Tag = chrome;
        _groups.Add(chrome);

        void Toggle(object? sender, EventArgs e)
        {
            if (sender is LinkLabel)
                return;
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

    private static LinkLabel MakeHeaderLink(string text) => new()
    {
        Text = text,
        AutoSize = true,
        LinkColor = AppTheme.PrimaryDark,
        ActiveLinkColor = AppTheme.Primary,
        VisitedLinkColor = AppTheme.PrimaryDark,
        BackColor = Color.Transparent,
    };

    private static Control BuildHeaderRow(int y, int h, int width)
    {
        var wrap = new BufferedPanel
        {
            Location = new Point(0, y),
            Size = new Size(width, h),
            BackColor = AppTheme.PrimaryPale,
        };
        PlaceCells(
            wrap, h, width,
            checkSlot: true,
            item: AppLang.L("项目", "Item"),
            current: AppLang.L("当前", "Current"),
            recommend: AppLang.L("推荐", "Recommended"),
            level: AppLang.L("强度", "Level"),
            hint: AppLang.L("说明", "Hint"),
            action: AppLang.L("操作", "Action"),
            header: true,
            levelColor: AppTheme.TextHeader);
        return wrap;
    }

    private FindingRowChrome BuildFindingRow(
        TabOptimizeFinding finding,
        string title,
        int y,
        int h,
        int width,
        Color bg)
    {
        var wrap = new BufferedPanel
        {
            Location = new Point(0, y),
            Size = new Size(width, h),
            BackColor = bg,
        };

        var check = new CheckBox
        {
            AutoSize = false,
            Size = new Size(UiScale.S(18), UiScale.S(18)),
            Location = new Point(UiScale.S(10), (h - UiScale.S(18)) / 2),
            BackColor = Color.Transparent,
            FlatStyle = FlatStyle.Flat,
        };
        wrap.Controls.Add(check);

        PlaceCells(
            wrap, h, width,
            checkSlot: true,
            item: title,
            current: finding.CurrentValue,
            recommend: finding.RecommendedValue,
            level: RecommendLevelUi.Title(finding.Level),
            hint: finding.Hint,
            action: "",
            header: false,
            levelColor: RecommendLevelUi.ForeColorOf(finding.Level));

        var action = MakeHeaderLink(AppLang.L("设为推荐", "Set"));
        action.Click += (_, _) =>
        {
            check.Checked = true;
            ApplyRecommendedFindings(new[] { finding }, reload: true);
        };
        wrap.Controls.Add(action);

        var chrome = new FindingRowChrome(finding, wrap, check, action);
        wrap.Tag = chrome;
        chrome.LayoutAction();
        return chrome;
    }

    private static void PlaceCells(
        Control wrap,
        int h,
        int width,
        bool checkSlot,
        string item,
        string current,
        string recommend,
        string level,
        string hint,
        string action,
        bool header,
        Color levelColor)
    {
        var pad = UiScale.S(10);
        var checkW = checkSlot ? UiScale.S(28) : 0;
        var actionW = UiScale.S(72);
        var avail = width - pad * 2 - checkW - actionW;
        var wItem = Math.Max(140, (int)(avail * 0.34));
        var wCur = Math.Max(64, (int)(avail * 0.12));
        var wRec = Math.Max(64, (int)(avail * 0.12));
        var wLvl = Math.Max(64, (int)(avail * 0.12));
        var wHint = Math.Max(60, avail - wItem - wCur - wRec - wLvl);

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

        var x = pad + checkW;
        wrap.Controls.Add(Cell(item, x, wItem, header ? AppTheme.TextHeader : AppTheme.TextMain, header));
        x += wItem;
        wrap.Controls.Add(Cell(current, x, wCur, header ? AppTheme.TextHeader : AppTheme.TextMute, header));
        x += wCur;
        wrap.Controls.Add(Cell(recommend, x, wRec, header ? AppTheme.TextHeader : AppTheme.PrimaryDark, header));
        x += wRec;
        wrap.Controls.Add(Cell(level, x, wLvl, levelColor, header));
        x += wLvl;
        wrap.Controls.Add(Cell(hint, x, wHint, header ? AppTheme.TextHeader : AppTheme.TextMute, header));
        if (header && !string.IsNullOrEmpty(action))
        {
            wrap.Controls.Add(Cell(action, width - pad - actionW, actionW, AppTheme.TextHeader, true));
        }
    }

    private void SetAllChecked(bool on)
    {
        foreach (var r in _rows)
            r.Check.Checked = on;
    }

    private void ApplyRecommendedSelected()
    {
        var selected = _rows.Where(r => r.Check.Checked).Select(r => r.Finding).ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show(this,
                AppLang.L("请先勾选要处理的项。", "Select items first."),
                Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        ApplyRecommendedFindings(selected, reload: true);
    }

    private void ApplyRecommendedFindings(IReadOnlyList<TabOptimizeFinding> findings, bool reload)
    {
        var main = Main;
        if (main is null)
        {
            MessageBox.Show(this,
                AppLang.L("无法关联主窗口，请从主程序打开优化顾问。", "No main window — open advisor from the app."),
                Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var (settings, services, errors, resolvedKeys) = main.ApplyRecommendedFindings(findings);
        foreach (var key in resolvedKeys)
            _sessionResolved.Add(key);

        if (errors.Count > 0)
        {
            MessageBox.Show(this,
                AppLang.L("部分失败：\r\n", "Some failed:\r\n") + string.Join("\r\n", errors),
                Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        else if (settings + services == 0 && resolvedKeys.Count == 0)
        {
            MessageBox.Show(this,
                AppLang.L("没有可设置的项。", "Nothing to set."),
                Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        if (reload)
        {
            // 不要从系统重绑：否则未写入时会把刚勾上的推荐冲掉，列表又涨回去
            Reload(refreshFromSystem: false);
            if (settings + services > 0 || resolvedKeys.Count > 0)
            {
                _summary.Text = AppLang.Lf(
                    "已设推荐：开关 {0} · 服务 {1}。待优化已同步减少；开关请再点「应用到系统…」写入。",
                    "Set recommended: {0} toggle(s) · {1} service(s). List updated; Apply to write toggles.",
                    settings, services);
            }
        }
    }

    private void ApplyToSystem()
    {
        var main = Main;
        if (main is null)
        {
            MessageBox.Show(this,
                AppLang.L("无法关联主窗口，请从主程序打开优化顾问。", "No main window — open advisor from the app."),
                Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // 若有勾选项且尚未设推荐，先设推荐再应用
        var selected = _rows.Where(r => r.Check.Checked).Select(r => r.Finding).ToList();
        if (selected.Count > 0)
        {
            var (_, _, _, keys) = main.ApplyRecommendedFindings(selected);
            foreach (var key in keys)
                _sessionResolved.Add(key);
        }

        var (ok, wrote) = main.ApplyToSystemFromAdvisor();
        if (!ok)
            return;

        if (wrote)
        {
            // 已同步读回系统：清空会话遮罩，按真实状态重建
            _sessionResolved.Clear();
            Reload(refreshFromSystem: false);
            _summary.Text = AppLang.L(
                "已写入系统，待优化列表已按当前状态更新。",
                "Applied. Pending list refreshed from current state.");
        }
        else
        {
            // 仅检测 / 无差量：保留「已设推荐」遮罩，列表继续减少
            Reload(refreshFromSystem: false);
            _summary.Text = AppLang.L(
                "未写入新变更（仅检测或无差量）。已设推荐的项仍从待优化中隐藏；可点「刷新诊断」核对系统。",
                "Nothing new written (detect-only or no diff). Recommended items stay hidden; Refresh to re-check system.");
        }
    }

    private sealed class FindingRowChrome
    {
        public TabOptimizeFinding Finding { get; }
        public Panel Wrap { get; }
        public CheckBox Check { get; }
        public LinkLabel Action { get; }

        public FindingRowChrome(TabOptimizeFinding finding, Panel wrap, CheckBox check, LinkLabel action)
        {
            Finding = finding;
            Wrap = wrap;
            Check = check;
            Action = action;
        }

        public void LayoutAction()
        {
            var pad = UiScale.S(10);
            Action.Location = new Point(
                Math.Max(pad, Wrap.Width - pad - Action.Width),
                Math.Max(0, (Wrap.Height - Action.Height) / 2));
        }
    }

    private sealed class GroupChrome
    {
        private readonly Panel _section;
        private readonly Panel _head;
        private readonly Panel _body;
        private readonly Label _title;
        private readonly LinkLabel _selectAll;
        private readonly LinkLabel _setGroup;
        private readonly Action _layoutLinks;

        public bool Expanded { get; set; } = true;
        public Label Arrow { get; }

        public GroupChrome(
            Panel section,
            Panel head,
            Panel body,
            Label arrow,
            Label title,
            LinkLabel selectAll,
            LinkLabel setGroup,
            int headerH,
            Action layoutLinks)
        {
            _section = section;
            _head = head;
            _body = body;
            Arrow = arrow;
            _title = title;
            _selectAll = selectAll;
            _setGroup = setGroup;
            _layoutLinks = layoutLinks;
            _ = headerH;
        }

        public void ApplyWidth(int width)
        {
            _section.Width = width;
            _head.Width = width;
            _body.Width = width;
            foreach (Control row in _body.Controls)
            {
                row.Width = width;
                if (row.Tag is FindingRowChrome chrome)
                    chrome.LayoutAction();
                else
                {
                    foreach (Control child in row.Controls)
                    {
                        if (child is LinkLabel link && link.Text is "设为推荐" or "Set")
                        {
                            link.Location = new Point(
                                Math.Max(UiScale.S(10), width - UiScale.S(10) - link.Width),
                                Math.Max(0, (row.Height - link.Height) / 2));
                        }
                    }
                }
            }
            _layoutLinks();
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
