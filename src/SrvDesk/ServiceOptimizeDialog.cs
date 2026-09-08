namespace SrvDesk;

/// <summary>服务优化嵌入页：按当前 OS 过滤，列表为主，说明走主窗口右侧配置脚本栏。</summary>
internal sealed class ServiceOptimizeDialog : Form, IEmbeddedSettingsPage
{
    private readonly ListView _list = new();
    private readonly TextBox _search = new();
    private readonly ComboBox _filter = new();
    private readonly ComboBox _category = new();
    private readonly Label _count = new();
    private readonly Label _detail = new();
    private readonly CheckBox _showMissingBox = new();

    private List<ServiceOptimizeRow> _items = [];
    private ServiceOsTarget _currentOs;
    private bool _warmLoadSkip;
    private bool _showMissing;
    private readonly HashSet<string> _collapsedCategories = new(StringComparer.Ordinal);
    private readonly Font _headerFont = UiFit.UiFontBold();
    private readonly ToolTip _listTip = new();

    /// <summary>-1=默认（推荐值+已禁用靠后）；否则为列索引。</summary>
    private int _sortColumn = -1;
    private bool _sortAscending;

    // 列顺序（与表头一致）：显示名 → 启动 → 状态 → 系统默认 → 处置 → 推荐值 → 说明 → 服务名
    private const int ColName = 0;
    private const int ColStart = 1;
    private const int ColStatus = 2;
    private const int ColDefault = 3;
    private const int ColTag = 4;
    private const int ColStars = 5;
    private const int ColNote = 6;
    private const int ColSvc = 7;

    private sealed class CategoryHeader
    {
        public string Category { get; }
        public CategoryHeader(string category) => Category = category;
    }

    /// <summary>选中行变化时通知主窗口，刷新右侧配置脚本栏。</summary>
    public Action<ServiceOptimizeRow?>? SelectionChanged { get; set; }

    private static readonly (string Key, string Zh, string En)[] FilterDefs =
    [
        ("all", "全部服务", "All services"),
        ("opt", "可优化", "Needs optimize"),
        ("disable", "应禁用", "Should disable"),
        ("keep", "应保留/启用", "Should keep/enable"),
        ("rec", "有建议", "Has recommendation"),
        ("disabled", "已禁用", "Disabled"),
        ("auto", "自动", "Automatic"),
        ("manual", "手动", "Manual"),
    ];

    public ServiceOptimizeDialog()
    {
        Text = AppLang.L("服务优化", "Service optimize");
        AppBrand.ApplyWindowIcon(this);
        AutoScaleMode = AutoScaleMode.None;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(920, 580);
        MinimumSize = new Size(760, 480);
        ForeColor = AppTheme.TextMain;

        _currentOs = ServiceOptimizeHelper.DetectOsTarget();

        var body = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Surface,
            Padding = new Padding(12, 8, 12, 8),
        };
        var tools = BuildToolStrip();
        tools.Dock = DockStyle.Top;

        _list.Font = UiFit.UiFont;
        _list.View = View.Details;
        _list.FullRowSelect = true;
        _list.GridLines = false;
        _list.HideSelection = false;
        _list.MultiSelect = true;
        _list.BorderStyle = BorderStyle.FixedSingle;
        _list.Dock = DockStyle.Fill;
        _list.BackColor = AppTheme.SurfaceCard;
        _list.ForeColor = AppTheme.TextMain;
        _list.OwnerDraw = true;
        _list.Columns.Add(AppLang.L("显示名", "Display name"), UiScale.S(260));
        _list.Columns.Add(AppLang.L("启动", "Startup"), UiScale.S(56));
        _list.Columns.Add(AppLang.L("状态", "Status"), UiScale.S(48));
        _list.Columns.Add(AppLang.L("系统默认", "OS default"), UiScale.S(72));
        _list.Columns.Add(AppLang.L("处置", "Action"), UiScale.S(72));
        _list.Columns.Add(AppLang.L("推荐值", "Recommend"), RecommendLevelUi.PreferredColumnWidth);
        _list.Columns.Add(AppLang.L("说明", "Note"), UiScale.S(180));
        _list.Columns.Add(AppLang.L("服务名", "Service"), UiScale.S(110));
        _list.SelectedIndexChanged += (_, _) => UpdateDetail();
        _list.MouseClick += OnListMouseClick;
        _list.MouseDoubleClick += OnListMouseDoubleClick;
        _list.KeyDown += OnListKeyDown;
        _list.ColumnClick += OnColumnClick;
        _list.DrawColumnHeader += (_, e) => e.DrawDefault = true;
        _list.DrawItem += OnDrawItem;
        _list.DrawSubItem += OnDrawSubItem;
        _list.HandleCreated += (_, _) =>
        {
            // 勿在 OnHandleCreated 栈内改 ImageList/列宽：会重创句柄并触发 NRE
            try { UiBuffer.EnableListView(_list); }
            catch { /* ignore */ }
            try
            {
                if (_list.IsHandleCreated)
                    _list.BeginInvoke(new Action(PostHandleLayout));
            }
            catch { /* ignore */ }
        };
        _list.ContextMenuStrip = BuildContextMenu();
        _listTip.SetToolTip(_list, RecommendLevelUi.LegendShort);

        body.Controls.Add(_list);
        body.Controls.Add(tools);

        ThemedSettingsChrome.MountEmbedded(
            this,
            AppLang.L("服务优化", "Service optimize"),
            AppLang.Lf("当前系统：{0} · 列表来自本机实时服务", "Current OS: {0} · live local services",
                ServiceOptimizeHelper.OsLabel(_currentOs)),
            body,
            "",
            RefreshList);

        Shown += (_, _) =>
        {
            if (_items.Count == 0) RefreshList();
        };
        _list.SizeChanged += (_, _) => AutoSizeColumns();
    }

    private void PostHandleLayout()
    {
        try
        {
            EnsureStarsColumnLayout();
            AutoSizeColumns();
        }
        catch { /* ignore */ }
    }

    private void EnsureStarsColumnLayout()
    {
        if (_list is null || _list.IsDisposed || !_list.IsHandleCreated) return;
        if (_list.Columns.Count <= ColStars) return;
        var need = RecommendLevelUi.PreferredColumnWidth;
        if (_list.Columns[ColStars].Width != need)
            _list.Columns[ColStars].Width = need;

        // 行高：两行显示名换行 + 几何星
        var minH = Math.Max(
            RecommendLevelUi.StarSize + UiScale.S(10),
            UiFit.LineHeight(_list.Font) * 2 + UiScale.S(10));
        var imgs = _list.SmallImageList;
        if (imgs is null || imgs.ImageSize.Height < minH)
        {
            var old = imgs;
            _list.SmallImageList = new ImageList
            {
                ColorDepth = ColorDepth.Depth32Bit,
                ImageSize = new Size(1, minH),
            };
            // 延后释放，避免句柄重建过程中访问已释放对象
            if (old is not null)
            {
                try { _list.BeginInvoke(new Action(() => { try { old.Dispose(); } catch { /* ignore */ } })); }
                catch { try { old.Dispose(); } catch { /* ignore */ } }
            }
        }
    }

    /// <summary>
    /// 按表头与可见内容自动列宽：短列刚好放下，推荐值固定，剩余给「说明」。
    /// </summary>
    private bool _autosizingColumns;

    private void AutoSizeColumns()
    {
        if (_autosizingColumns) return;
        if (_list is null || _list.IsDisposed) return;
        if (!_list.IsHandleCreated || _list.Columns.Count <= ColSvc)
            return;
        if (_list.ClientSize.Width < UiScale.S(80))
            return;

        _autosizingColumns = true;
        try
        {
            try { EnsureStarsColumnLayout(); }
            catch { /* ignore */ }

            var font = _list.Font ?? UiFit.UiFont;
            int Measure(string? text)
            {
                if (string.IsNullOrEmpty(text)) return 0;
                return TextRenderer.MeasureText(
                    text,
                    font,
                    new Size(int.MaxValue, 256),
                    TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding).Width;
            }

            var pad = UiScale.S(18);
            var tagW = Measure(_list.Columns[ColTag].Text);
            var svcW = Measure(_list.Columns[ColSvc].Text);
            var startW = Measure(_list.Columns[ColStart].Text);
            var defW = Math.Max(
                Measure(_list.Columns[ColDefault].Text),
                Measure(AppLang.L("系统默认", "OS default")));
            var statusW = Measure(_list.Columns[ColStatus].Text);
            var noteMin = Measure(_list.Columns[ColNote].Text);

            // 抽样时显示名不再按单行全宽撑列（允许换行），只取表头与适度下限
            var sampled = 0;
            foreach (ListViewItem it in _list.Items)
            {
                if (it?.Tag is not ServiceOptimizeRow row) continue;
                tagW = Math.Max(tagW, Measure(row.AdviceTag));
                svcW = Math.Max(svcW, Measure(row.ActualServiceName));
                startW = Math.Max(startW, Measure(ServiceOptimizeHelper.StartTypeLabel(row.StartType)));
                defW = Math.Max(defW, Measure(
                    row.SystemDefault == ServiceStartTypeKind.Unknown
                        ? "—"
                        : ServiceOptimizeHelper.StartTypeLabel(row.SystemDefault)));
                statusW = Math.Max(statusW, Measure(
                    row.Installed
                        ? (row.Running ? AppLang.L("运行", "Running") : AppLang.L("已停", "Stopped"))
                        : "-"));
                if (!string.IsNullOrEmpty(row.AdviceNote))
                    noteMin = Math.Max(noteMin, Math.Min(Measure(row.AdviceNote), UiScale.S(240)));
                if (++sampled >= 80) break;
            }

            int Fit(int content, int min, int max) =>
                Math.Min(max, Math.Max(min, content + pad));

            _list.Columns[ColStars].Width = RecommendLevelUi.PreferredColumnWidth;
            _list.Columns[ColTag].Width = Fit(tagW, UiScale.S(56), UiScale.S(80));
            _list.Columns[ColSvc].Width = Fit(svcW, UiScale.S(72), UiScale.S(140));
            _list.Columns[ColStart].Width = Fit(startW, UiScale.S(48), UiScale.S(72));
            // 「系统默认」表头必须完整显示
            _list.Columns[ColDefault].Width = Fit(defW, UiScale.S(72), UiScale.S(100));
            _list.Columns[ColStatus].Width = Fit(statusW, UiScale.S(44), UiScale.S(64));

            var fixedUsed = 0;
            for (var i = 0; i < _list.Columns.Count; i++)
            {
                if (i is ColName or ColNote) continue;
                fixedUsed += _list.Columns[i].Width;
            }

            var avail = _list.ClientSize.Width
                - SystemInformation.VerticalScrollBarWidth
                - fixedUsed
                - 4;
            if (avail < UiScale.S(280))
                avail = UiScale.S(280);

            // 显示名加宽（约 55%），说明拿走剩余
            var noteFloor = Math.Max(UiScale.S(120), Math.Min(noteMin + pad, UiScale.S(200)));
            var nameFloor = UiScale.S(220);
            var nameCap = Math.Max(nameFloor, (int)(avail * 0.58));
            var nameFinal = Math.Min(nameCap, Math.Max(nameFloor, avail - noteFloor));
            if (nameFinal > avail - UiScale.S(100))
                nameFinal = Math.Max(UiScale.S(180), avail - UiScale.S(100));

            _list.Columns[ColName].Width = nameFinal;
            _list.Columns[ColNote].Width = Math.Max(noteFloor, avail - nameFinal);

            var usedAll = 0;
            foreach (ColumnHeader c in _list.Columns)
            {
                if (c is null) continue;
                usedAll += c.Width;
            }
            var over = usedAll + SystemInformation.VerticalScrollBarWidth + 4 - _list.ClientSize.Width;
            if (over > 0 && _list.Columns[ColNote].Width > UiScale.S(120) + over)
                _list.Columns[ColNote].Width -= over;
        }
        catch
        {
            // 布局阶段偶发 NRE，忽略以免整页崩溃
        }
        finally
        {
            _autosizingColumns = false;
        }
    }

    public void RefreshFromSystem()
    {
        RefreshList();
        _warmLoadSkip = true;
    }

    public bool ConsumeWarmLoadSkip()
    {
        if (!_warmLoadSkip) return false;
        _warmLoadSkip = false;
        return true;
    }

    public bool SupportsApplyToSystem => false;
    public void ApplyToSystem() { }

    private Panel BuildToolStrip()
    {
        // 上行：说明/操作按钮；下行：筛选 —— 避免按钮盖住说明
        var btnH = UiScale.S(30);
        var row1 = btnH + UiScale.S(10);
        var bar = new Panel { Height = row1 + UiScale.S(34), BackColor = AppTheme.Surface };

        _count.AutoSize = true;
        _count.ForeColor = AppTheme.TextMute;

        var filterCap = Cap(AppLang.L("筛选", "Filter"));
        _filter.DropDownStyle = ComboBoxStyle.DropDownList;
        foreach (var f in FilterDefs)
            _filter.Items.Add(AppLang.L(f.Zh, f.En));
        _filter.SelectedIndex = 0;
        _filter.SelectedIndexChanged += (_, _) => ApplyFilter();

        var catCap = Cap(AppLang.L("分类", "Category"));
        _category.DropDownStyle = ComboBoxStyle.DropDownList;
        _category.Items.Add(AppLang.L("全部分类", "All categories"));
        _category.SelectedIndex = 0;
        _category.SelectedIndexChanged += (_, _) => ApplyFilter();

        var searchCap = Cap(AppLang.L("搜索", "Search"));
        _search.BorderStyle = BorderStyle.FixedSingle;
        _search.TextChanged += (_, _) => ApplyFilter();

        _showMissingBox.Text = AppLang.L("含未安装建议", "Include missing advice");
        _showMissingBox.AutoSize = true;
        _showMissingBox.FlatStyle = FlatStyle.Flat;
        _showMissingBox.ForeColor = AppTheme.TextMain;
        _toolTipSafe(_showMissingBox, AppLang.L(
            "额外显示目录中有优化建议、但本机尚未安装的服务",
            "Also list catalog advice for services not installed on this PC"));
        _showMissingBox.CheckedChanged += (_, _) =>
        {
            _showMissing = _showMissingBox.Checked;
            RefreshList();
        };

        _detail.AutoSize = false;
        _detail.AutoEllipsis = true;
        _detail.ForeColor = AppTheme.TextMute;
        _detail.TextAlign = ContentAlignment.MiddleLeft;
        _detail.Text = AppLang.L("可备份/还原；按分类折叠；推荐值高优先。批量修改前会自动备份。",
            "Backup/restore; collapsible categories; high score first. Auto-backup before batch changes.");


        bar.Controls.AddRange([
            _count, filterCap, _filter, catCap, _category, searchCap, _search, _showMissingBox, _detail,
        ]);

        var buttons = new[]
        {
            ToolBtn(AppLang.L("备份", "Backup"), BackupCurrentState, btnH),
            ToolBtn(AppLang.L("还原…", "Restore…"), RestoreFromBackup, btnH),
            ToolBtn(AppLang.L("按建议", "Apply advice"), ApplyRecommendSelected, btnH, primary: true),
            ToolBtn(AppLang.L("禁用", "Disable"), () => SetSelected(ServiceStartTypeKind.Disabled), btnH),
            ToolBtn(AppLang.L("手动", "Manual"), () => SetSelected(ServiceStartTypeKind.Manual), btnH),
            ToolBtn(AppLang.L("自动", "Automatic"), () => SetSelected(ServiceStartTypeKind.Automatic), btnH),
        };
        foreach (var b in buttons)
            bar.Controls.Add(b);

        _toolTipSafe(buttons[0], AppLang.L(
            "备份本机全部服务的当前启动类型，出问题可从备份还原。",
            "Backup current start types of all local services for later restore."));
        _toolTipSafe(buttons[1], AppLang.L(
            "从历史备份还原启动类型（含改前自动备份）。",
            "Restore start types from a previous backup (includes auto before-change)."));


        void LayoutTools()
        {
            var gap = UiScale.S(6);
            var pad = UiScale.S(4);
            foreach (var b in buttons)
                UiFit.FitButton(b, btnH, minWidth: 64, padding: 16);

            var x = bar.ClientSize.Width - pad;
            for (var i = buttons.Length - 1; i >= 0; i--)
            {
                var b = buttons[i];
                x -= b.Width;
                b.Location = new Point(Math.Max(pad, x), UiScale.S(4));
                x -= gap;
            }

            var btnLeft = buttons[0].Left;
            var y1 = UiScale.S(8);
            _count.Location = new Point(0, y1);

            var detailLeft = _count.Right + UiScale.S(12);
            var detailRight = btnLeft - UiScale.S(12);
            if (detailRight - detailLeft > UiScale.S(80))
            {
                _detail.Visible = true;
                _detail.SetBounds(detailLeft, UiScale.S(6), detailRight - detailLeft, btnH);
            }
            else
            {
                _detail.Visible = false;
            }

            var y2 = row1 + UiScale.S(4);
            var y2Label = row1 + UiScale.S(8);
            filterCap.Location = new Point(0, y2Label);
            _filter.SetBounds(filterCap.Right + UiScale.S(4), y2, UiScale.S(110), UiScale.S(26));
            catCap.Location = new Point(_filter.Right + UiScale.S(10), y2Label);
            _category.SetBounds(catCap.Right + UiScale.S(4), y2, UiScale.S(120), UiScale.S(26));
            searchCap.Location = new Point(_category.Right + UiScale.S(10), y2Label);

            var missW = _showMissingBox.PreferredSize.Width;
            _showMissingBox.Location = new Point(
                Math.Max(_category.Right + UiScale.S(8), bar.ClientSize.Width - missW - pad),
                y2Label);
            var searchW = Math.Max(UiScale.S(90), _showMissingBox.Left - searchCap.Right - UiScale.S(12));
            _search.SetBounds(searchCap.Right + UiScale.S(4), y2, searchW, UiScale.S(26));
        }

        bar.Resize += (_, _) => LayoutTools();
        LayoutTools();
        return bar;
    }

    private static void _toolTipSafe(Control c, string tip)
    {
        var t = new ToolTip();
        t.SetToolTip(c, tip);
    }

    private static Label Cap(string text) => new()
    {
        Text = text,
        AutoSize = true,
        ForeColor = AppTheme.TextHeader,
    };

    private Button ToolBtn(string text, Action click, int height, bool primary = false)
    {
        var b = ThemedSettingsChrome.CreateButton(text, primary);
        UiFit.FitButton(b, height, minWidth: 64, padding: 16);
        b.Click += (_, _) => click();
        return b;
    }

    private void RefreshList()
    {
        try
        {
            _currentOs = ServiceOptimizeHelper.DetectOsTarget();
            _items = ServiceOptimizeHelper.LoadApplicable(installedOnly: !_showMissing);
            RebuildCategoryCombo();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, AppLang.L("服务优化", "Service optimize"),
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void RebuildCategoryCombo()
    {
        var selected = _category.SelectedItem as string;
        var cats = _items.Select(x => x.Entry.Category).Distinct().OrderBy(x => x).ToList();
        _category.BeginUpdate();
        _category.Items.Clear();
        _category.Items.Add(AppLang.L("全部分类", "All categories"));
        foreach (var c in cats)
            _category.Items.Add(c);
        var idx = 0;
        if (!string.IsNullOrEmpty(selected))
        {
            for (var i = 0; i < _category.Items.Count; i++)
            {
                if (Equals(_category.Items[i], selected))
                {
                    idx = i;
                    break;
                }
            }
        }
        _category.SelectedIndex = idx;
        _category.EndUpdate();
    }

    private void ApplyFilter()
    {
        var q = _search.Text.Trim();
        var filterKey = _filter.SelectedIndex >= 0 && _filter.SelectedIndex < FilterDefs.Length
            ? FilterDefs[_filter.SelectedIndex].Key
            : "all";
        var cat = _category.SelectedIndex <= 0 ? null : _category.SelectedItem as string;

        var filtered = new List<ServiceOptimizeRow>();
        foreach (var item in _items)
        {
            if (cat is not null && item.Entry.Category != cat) continue;
            if (filterKey == "opt" && !item.CanOptimize) continue;
            if (filterKey == "disable" && item.AdviceTag is not ("应禁用" or "Disable")) continue;
            if (filterKey == "keep"
                && item.AdviceTag is not ("应保留" or "应启用" or "Keep" or "Enable")) continue;
            if (filterKey == "rec" && item.Recommend == ServiceRecommend.Keep) continue;
            if (filterKey == "disabled" && item.StartType != ServiceStartTypeKind.Disabled) continue;
            if (filterKey == "auto" && item.StartType != ServiceStartTypeKind.Automatic) continue;
            if (filterKey == "manual" && item.StartType != ServiceStartTypeKind.Manual) continue;

            if (q.Length > 0
                && item.DisplayName.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0
                && item.ActualServiceName.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0
                && item.AdviceTag.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0
                && item.AdviceNote.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0
                && item.Entry.Title.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0
                && item.Entry.Note.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            filtered.Add(item);
        }

        filtered.Sort(CompareVisible);

        _list.BeginUpdate();
        _list.Items.Clear();
        var shown = 0;
        var optimizable = 0;

        // 按当前排序结果分组，组序跟随首条出现位置
        var groups = filtered
            .GroupBy(x => x.Entry.Category)
            .OrderBy(g => filtered.FindIndex(x => ReferenceEquals(x, g.First())));

        foreach (var group in groups)
        {
            var catName = group.Key;
            var list = group.ToList(); // filtered 已按 CompareVisible 排好，组内保持相对序
            var optInCat = list.Count(x => x.CanOptimize);
            optimizable += optInCat;
            var collapsed = _collapsedCategories.Contains(catName);
            var marker = collapsed ? "▶" : "▼";
            var headerText = AppLang.Lf("{0} {1}（{2}）", "{0} {1} ({2})",
                marker, catName, list.Count);
            if (optInCat > 0)
                headerText += AppLang.Lf(" · 可优化 {0}", " · {0} to optimize", optInCat);

            var header = new ListViewItem(headerText)
            {
                Tag = new CategoryHeader(catName),
                BackColor = AppTheme.PrimaryLight,
                ForeColor = AppTheme.TextHeader,
                Font = _headerFont,
                UseItemStyleForSubItems = false,
            };
            while (header.SubItems.Count < _list.Columns.Count)
                header.SubItems.Add("");
            for (var i = 0; i < header.SubItems.Count; i++)
            {
                header.SubItems[i].BackColor = AppTheme.PrimaryLight;
                header.SubItems[i].ForeColor = AppTheme.TextHeader;
            }
            _list.Items.Add(header);

            if (collapsed) continue;

            foreach (var item in list)
            {
                _list.Items.Add(MakeRow(item));
                shown++;
            }
        }

        _list.EndUpdate();
        _count.Text = AppLang.Lf("共 {0} · 显示 {1} · 可优化 {2}",
            "{0} total · {1} shown · {2} to optimize",
            _items.Count, shown, optimizable);
        UpdateDetail();
        AutoSizeColumns();
    }

    private int CompareVisible(ServiceOptimizeRow a, ServiceOptimizeRow b)
    {
        if (_sortColumn < 0)
            return ServiceOptimizeHelper.CompareRows(a, b);
        var cmp = CompareByColumn(a, b, _sortColumn);
        return _sortAscending ? cmp : -cmp;
    }

    private static int CompareByColumn(ServiceOptimizeRow a, ServiceOptimizeRow b, int col)
    {
        switch (col)
        {
            case ColName:
                return string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase);
            case ColStars:
            {
                var c = ((int)a.OptimizeLevel).CompareTo((int)b.OptimizeLevel);
                return c != 0 ? c : a.RecommendScore.CompareTo(b.RecommendScore);
            }
            case ColTag:
                return string.Compare(a.AdviceTag, b.AdviceTag, StringComparison.Ordinal);
            case ColNote:
                return string.Compare(a.AdviceNote, b.AdviceNote, StringComparison.Ordinal);
            case ColSvc:
                return string.Compare(a.ActualServiceName, b.ActualServiceName, StringComparison.OrdinalIgnoreCase);
            case ColStart:
                return a.StartType.CompareTo(b.StartType);
            case ColDefault:
                return a.SystemDefault.CompareTo(b.SystemDefault);
            case ColStatus:
                return (a.Running ? 1 : 0).CompareTo(b.Running ? 1 : 0);
            default:
                return ServiceOptimizeHelper.CompareRows(a, b);
        }
    }

    private void OnColumnClick(object? sender, ColumnClickEventArgs e)
    {
        if (_sortColumn == e.Column)
            _sortAscending = !_sortAscending;
        else
        {
            _sortColumn = e.Column;
            // 推荐值默认高→低；其它列默认升序
            _sortAscending = e.Column != ColStars;
        }
        ApplyFilter();
    }

    private void OnDrawItem(object? sender, DrawListViewItemEventArgs e)
    {
        // Details 视图以 DrawSubItem 为准
        e.DrawDefault = false;
    }

    private void OnDrawSubItem(object? sender, DrawListViewSubItemEventArgs e)
    {
        if (e.Item is null) return;

        var selected = e.Item.Selected;
        var back = selected
            ? Color.FromArgb(210, 228, 245)
            : e.Item.BackColor;
        if (e.Item.Tag is CategoryHeader)
            back = AppTheme.PrimaryLight;

        using (var brush = new SolidBrush(back))
            e.Graphics.FillRectangle(brush, e.Bounds);

        if (e.ColumnIndex == ColStars && e.Item.Tag is ServiceOptimizeRow row)
        {
            // 裁剪在本列内绘制，避免画出列宽被邻列背景盖住
            RecommendLevelUi.DrawStarsInBounds(e.Graphics, row.OptimizeLevel, e.Bounds, paddingLeft: 4);
            return;
        }

        if (e.ColumnIndex == ColStars && e.Item.Tag is CategoryHeader)
            return;

        var text = e.SubItem?.Text ?? "";
        if (text.Length == 0) return;

        var fore = selected ? AppTheme.TextMain : e.SubItem!.ForeColor;
        if (fore.IsEmpty) fore = e.Item.ForeColor;
        if (fore.IsEmpty) fore = AppTheme.TextMain;

        var font = e.Item.Tag is CategoryHeader ? _headerFont : _list.Font;
        var state = e.Graphics.Save();
        try
        {
            e.Graphics.SetClip(e.Bounds);
            var rect = new Rectangle(e.Bounds.X + 2, e.Bounds.Y, e.Bounds.Width - 4, e.Bounds.Height);
            // 显示名：自动换行；分类头与其它列仍单行省略
            var flags = e.ColumnIndex == ColName && e.Item.Tag is ServiceOptimizeRow
                ? TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPrefix
                  | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis
                : TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter
                  | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
            TextRenderer.DrawText(e.Graphics, text, font, rect, fore, flags);
        }
        finally
        {
            e.Graphics.Restore(state);
        }
    }

    private ListViewItem MakeRow(ServiceOptimizeRow item)
    {
        var row = new ListViewItem(item.DisplayName) { Tag = item, UseItemStyleForSubItems = false };
        // 顺序：启动 → 状态 → 系统默认 → 处置 → 推荐值(空) → 说明 → 服务名
        row.SubItems.Add(ServiceOptimizeHelper.StartTypeLabel(item.StartType));
        row.SubItems.Add(item.Installed
            ? (item.Running ? AppLang.L("运行", "Running") : AppLang.L("已停", "Stopped"))
            : "-");
        row.SubItems.Add(item.SystemDefault == ServiceStartTypeKind.Unknown
            ? "—"
            : ServiceOptimizeHelper.StartTypeLabel(item.SystemDefault));
        row.SubItems.Add(item.AdviceTag);
        row.SubItems.Add(""); // ColStars：不放文字，避免叠在自绘星星上
        row.SubItems.Add(item.AdviceNote);
        row.SubItems.Add(item.ActualServiceName);
        if (!item.Installed)
            row.ForeColor = AppTheme.TextMute;
        else if (item.AdviceTag is "应禁用" or "Disable")
            row.ForeColor = Color.FromArgb(180, 60, 50);
        else if (item.AdviceTag is "应保留" or "应启用" or "Keep" or "Enable")
            row.ForeColor = Color.FromArgb(30, 120, 70);
        else if (item.CanOptimize)
            row.ForeColor = AppTheme.PrimaryDark;
        else if (item.StartType == ServiceStartTypeKind.Disabled)
            row.ForeColor = AppTheme.TextMute;

        foreach (ListViewItem.ListViewSubItem sub in row.SubItems)
        {
            sub.BackColor = AppTheme.SurfaceCard;
            sub.ForeColor = row.ForeColor;
        }

        return row;
    }

    private void OnListMouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        var hit = _list.HitTest(e.Location);
        if (hit.Item?.Tag is CategoryHeader header)
            ToggleCategory(header.Category);
    }

    private void OnListMouseDoubleClick(object? sender, MouseEventArgs e)
    {
        var hit = _list.HitTest(e.Location);
        if (hit.Item?.Tag is CategoryHeader header)
        {
            ToggleCategory(header.Category);
            return;
        }
        ApplyRecommendSelected();
    }

    private void OnListKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Left && e.KeyCode != Keys.Right && e.KeyCode != Keys.Space)
            return;
        if (_list.SelectedItems.Count != 1) return;
        if (_list.SelectedItems[0].Tag is not CategoryHeader header) return;
        if (e.KeyCode == Keys.Left)
        {
            _collapsedCategories.Add(header.Category);
            ApplyFilter();
            e.Handled = true;
        }
        else if (e.KeyCode == Keys.Right)
        {
            _collapsedCategories.Remove(header.Category);
            ApplyFilter();
            e.Handled = true;
        }
        else
        {
            ToggleCategory(header.Category);
            e.Handled = true;
        }
    }

    private void ToggleCategory(string category)
    {
        if (!_collapsedCategories.Add(category))
            _collapsedCategories.Remove(category);
        ApplyFilter();
    }

    private void CollapseAllCategories()
    {
        foreach (var c in _items.Select(x => x.Entry.Category).Distinct())
            _collapsedCategories.Add(c);
        ApplyFilter();
    }

    private void ExpandAllCategories()
    {
        _collapsedCategories.Clear();
        ApplyFilter();
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add(AppLang.L("按建议", "Apply advice"), null, (_, _) => ApplyRecommendSelected());
        menu.Items.Add(AppLang.L("恢复系统默认", "Restore OS default"), null, (_, _) => RestoreDefaultSelected());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(AppLang.L("设为禁用", "Set Disabled"), null, (_, _) => SetSelected(ServiceStartTypeKind.Disabled));
        menu.Items.Add(AppLang.L("设为手动", "Set Manual"), null, (_, _) => SetSelected(ServiceStartTypeKind.Manual));
        menu.Items.Add(AppLang.L("设为自动", "Set Automatic"), null, (_, _) => SetSelected(ServiceStartTypeKind.Automatic));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(AppLang.L("备份当前服务状态", "Backup current services"), null, (_, _) => BackupCurrentState());
        menu.Items.Add(AppLang.L("从备份还原…", "Restore from backup…"), null, (_, _) => RestoreFromBackup());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(AppLang.L("折叠全部分类", "Collapse all"), null, (_, _) => CollapseAllCategories());
        menu.Items.Add(AppLang.L("展开全部分类", "Expand all"), null, (_, _) => ExpandAllCategories());
        menu.Opening += (_, _) =>
        {
            var rows = SelectedRows().ToList();
            var has = rows.Count > 0;
            var canOpt = rows.Any(x => x.CanOptimize);
            var canDef = rows.Any(x => x.CanRestoreDefault);
            menu.Items[0].Enabled = canOpt;
            menu.Items[1].Enabled = canDef;
            menu.Items[3].Enabled = has;
            menu.Items[4].Enabled = has;
            menu.Items[5].Enabled = has;
        };
        return menu;
    }

    private IEnumerable<ServiceOptimizeRow> SelectedRows()
    {
        foreach (ListViewItem row in _list.SelectedItems)
        {
            if (row.Tag is ServiceOptimizeRow item)
                yield return item;
        }
    }

    private void UpdateDetail()
    {
        var item = SelectedRows().FirstOrDefault();
        SelectionChanged?.Invoke(item);

        if (item is null)
        {
            _detail.Text = AppLang.L("列表按分类折叠；右键可操作。点分类行可展开/收起。",
                "Grouped by category; right-click for actions. Click a category row to expand/collapse.");
            return;
        }

        _detail.Text = AppLang.Lf("{0}  ·  {1}  ·  {2}",
            "{0}  ·  {1}  ·  {2}",
            RecommendLevelUi.Title(item.OptimizeLevel),
            item.AdviceTag,
            item.AdviceNote);
    }

    private void BackupCurrentState()
    {
        try
        {
            UseWaitCursor = true;
            var info = ServiceSnapshotStore.Capture("manual",
                AppLang.L("用户手动备份", "Manual backup by user"));
            MessageBox.Show(this,
                AppLang.Lf("已备份 {0} 个服务的启动类型。\r\n\r\n{1}\r\n\r\n可用「还原…」回退到此状态。",
                    "Backed up start types of {0} services.\r\n\r\n{1}\r\n\r\nUse Restore… to roll back.",
                    info.ServiceCount, info.Path),
                AppLang.L("备份完成", "Backup done"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message,
                AppLang.L("备份失败", "Backup failed"),
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private void RestoreFromBackup()
    {
        using var dlg = new ServiceSnapshotRestoreDialog();
        if (dlg.ShowDialog(this) == DialogResult.OK)
            RefreshList();
    }

    /// <summary>修改服务前自动打快照（2 分钟内复用）。</summary>
    private void AutoBackupBeforeChange()
    {
        try
        {
            UseWaitCursor = true;
            ServiceSnapshotStore.EnsureAutoBackupBeforeChange();
        }
        catch
        {
            // 自动备份失败不阻断修改；已写日志
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private void RestoreDefaultSelected()
    {
        var rows = SelectedRows().Where(x => x.CanRestoreDefault).ToList();
        if (rows.Count == 0)
        {
            MessageBox.Show(this,
                AppLang.L("请先选择可恢复系统默认的服务（需已知该 OS 的默认启动类型）。",
                    "Select services that have a known OS default different from current."),
                AppLang.L("服务优化", "Service optimize"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        AutoBackupBeforeChange();
        var errors = new List<string>();
        foreach (var row in rows)
        {
            try
            {
                using (ApplyLog.PushContext(row.ActualServiceName))
                    ServiceOptimizeHelper.RestoreSystemDefault(row);
            }
            catch (Exception ex)
            {
                errors.Add($"{row.ActualServiceName}: {ex.Message}");
            }
        }

        RefreshList();
        if (errors.Count > 0)
        {
            MessageBox.Show(this, string.Join("\r\n", errors.Take(8)),
                AppLang.L("部分失败", "Some failed"),
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void SetSelected(ServiceStartTypeKind target)
    {
        var rows = SelectedRows().Where(x => x.Installed).ToList();
        if (rows.Count == 0)
        {
            MessageBox.Show(this,
                AppLang.L("请先选择已安装的服务。", "Select an installed service first."),
                AppLang.L("服务优化", "Service optimize"),
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        AutoBackupBeforeChange();
        var errors = new List<string>();
        foreach (var row in rows)
        {
            try
            {
                using (ApplyLog.PushContext(row.ActualServiceName))
                    ServiceOptimizeHelper.SetStartType(row.ActualServiceName, target);
            }
            catch (Exception ex)
            {
                errors.Add($"{row.ActualServiceName}: {ex.Message}");
            }
        }

        RefreshList();
        if (errors.Count > 0)
        {
            MessageBox.Show(this, string.Join("\r\n", errors.Take(8)),
                AppLang.L("部分失败", "Some failed"),
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ApplyRecommendSelected()
    {
        var rows = SelectedRows().Where(x => x.CanOptimize).ToList();
        if (rows.Count == 0)
        {
            var any = SelectedRows().FirstOrDefault();
            if (any is null)
            {
                MessageBox.Show(this,
                    AppLang.L("请先选择可优化的服务。", "Select services that need optimizing."),
                    AppLang.L("服务优化", "Service optimize"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            return;
        }

        AutoBackupBeforeChange();
        var errors = new List<string>();
        foreach (var row in rows)
        {
            try
            {
                using (ApplyLog.PushContext(row.ActualServiceName))
                    ServiceOptimizeHelper.ApplyRecommend(row);
            }
            catch (Exception ex)
            {
                errors.Add($"{row.ActualServiceName}: {ex.Message}");
            }
        }

        RefreshList();
        if (errors.Count > 0)
        {
            MessageBox.Show(this, string.Join("\r\n", errors.Take(8)),
                AppLang.L("部分失败", "Some failed"),
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
