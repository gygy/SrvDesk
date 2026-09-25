namespace SrvDesk;

/// <summary>侧栏条目元数据：一条 = 主界面右侧一个开关/下拉，供导入预览对齐。</summary>
internal sealed class SettingUiMeta
{
    public string Nav { get; set; } = "";
    public string Section { get; set; } = "";
    public string Title { get; set; } = "";
    /// <summary>列表「含义」列：与右侧面板 Summary 一致。</summary>
    public string Meaning { get; set; } = "";
    /// <summary>选中行详情：作用 + 好处（对齐右侧精简说明）。</summary>
    public string MeaningFull { get; set; } = "";
    /// <summary>Catalog 字段名（通常与 State 主键同名）。</summary>
    public string CatalogKey { get; set; } = "";
    /// <summary>勾选本行时要写入的全部 State 字段（含互斥附属键）。</summary>
    public string[] StateKeys { get; set; } = [];
    /// <summary>下拉选项文案；null 表示普通开关。</summary>
    public string[]? ChoiceLabels { get; set; }
    public int OptimizedIndex { get; set; } = 1;
    /// <summary>把 State 格式化成与右侧「系统当前值」一致的文案。</summary>
    public Func<Optimizer.State, string>? FormatValue { get; set; }
}

/// <summary>
/// 导入/一键恢复挑选：左侧仅显示有变更的栏目（对齐主界面侧栏风格），
/// 右侧按分区列出对应开关/设置条目。
/// </summary>
internal sealed class QuickRestorePreviewDialog : Form
{
    private readonly ListBox _nav = new();
    private readonly Panel _rightHost = new();
    private readonly Panel _scroll = new();
    private readonly Label _pageTitle = new();
    private readonly TextBox _detail = new();
    private readonly ToolTip _tip = new()
    {
        AutoPopDelay = 25000,
        InitialDelay = 350,
        ReshowDelay = 150,
        ShowAlways = true,
    };
    private readonly List<PreviewLine> _lines;
    private readonly List<string> _navTitles = [];
    private readonly int[] _navCounts = [];
    private int _navHover = -1;
    private PreviewLine? _focusLine;

    public IReadOnlyList<PreviewLine> SelectedLines =>
        _lines.Where(x => x.Selected).ToList();

    public QuickRestorePreviewDialog(
        IReadOnlyList<PreviewLine> lines,
        string sourcePath,
        string? windowTitle = null,
        string? confirmButtonText = null)
    {
        _ = sourcePath;
        _lines = lines.Select(x => x.Clone()).ToList();
        foreach (var line in _lines)
            line.Selected = true;

        // 左侧只保留「至少有一条变更」的栏目，顺序与条目首次出现一致
        foreach (var line in _lines)
        {
            var nav = string.IsNullOrWhiteSpace(line.Nav)
                ? AppLang.L("其它", "Other")
                : line.Nav;
            line.Nav = nav;
            if (!_navTitles.Contains(nav))
                _navTitles.Add(nav);
        }
        _navCounts = new int[_navTitles.Count];
        for (var i = 0; i < _navTitles.Count; i++)
            _navCounts[i] = _lines.Count(x => x.Nav == _navTitles[i]);

        Text = windowTitle ?? AppLang.L("一键快速恢复 · 挑选变更", "One-click restore · Pick changes");
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.None;
        Font = UiFit.UiFont;
        BackColor = AppTheme.Surface;
        ClientSize = UiScale.Size(1080, 680);
        MinimumSize = UiScale.Size(920, 560);

        var footerH = UiFit.ControlHeight() + UiScale.S(20);
        var detailH = Math.Max(UiScale.S(88), UiFit.LineHeight() * 3 + UiScale.S(16));
        var south = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = detailH + footerH,
            BackColor = AppTheme.SurfaceCard,
            Padding = new Padding(UiScale.S(12), UiScale.S(8), UiScale.S(12), UiScale.S(4)),
        };

        _detail.Multiline = true;
        _detail.ReadOnly = true;
        _detail.BorderStyle = BorderStyle.None;
        _detail.BackColor = AppTheme.SurfaceCard;
        _detail.ForeColor = AppTheme.TextMain;
        _detail.Font = UiFit.UiFont;
        _detail.TabStop = false;
        _detail.ScrollBars = ScrollBars.Vertical;
        _detail.Dock = DockStyle.Fill;
        _detail.Text = "";

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = footerH,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, UiScale.S(6), 0, 0),
            BackColor = AppTheme.SurfaceCard,
        };
        UiBuffer.ConfigureNoScrollRow(footer);

        var cancel = ThemedSettingsChrome.CreateButton(AppLang.L("取消", "Cancel"), false);
        UiFit.FitButton(cancel, padding: 28);
        cancel.DialogResult = DialogResult.Cancel;

        var ok = ThemedSettingsChrome.CreateButton(
            confirmButtonText ?? AppLang.L("恢复勾选项", "Restore checked"), true);
        UiFit.FitButton(ok, padding: 28);
        ok.Margin = new Padding(UiScale.S(8), 0, 0, 0);
        ok.Click += (_, _) =>
        {
            if (_lines.Count > 0 && !_lines.Any(x => x.Selected))
            {
                MessageBox.Show(this,
                    AppLang.L("请至少勾选一项，或点取消。", "Check at least one item, or Cancel."),
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            DialogResult = DialogResult.Yes;
            Close();
        };

        var allOff = ThemedSettingsChrome.CreateButton(AppLang.L("全不选", "None"), false);
        UiFit.FitButton(allOff, padding: 22);
        allOff.Margin = new Padding(UiScale.S(8), 0, 0, 0);
        allOff.Click += (_, _) => SetAllChecked(false);

        var allOn = ThemedSettingsChrome.CreateButton(AppLang.L("全选", "All"), false);
        UiFit.FitButton(allOn, padding: 22);
        allOn.Margin = new Padding(UiScale.S(8), 0, 0, 0);
        allOn.Click += (_, _) => SetAllChecked(true);

        footer.Controls.Add(cancel);
        footer.Controls.Add(ok);
        footer.Controls.Add(allOff);
        footer.Controls.Add(allOn);
        south.Controls.Add(_detail);
        south.Controls.Add(footer);

        var split = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Surface,
            Padding = new Padding(0),
        };

        var sidebar = NavMenuStyle.CreateSidebar(_navTitles, withIcon: true);
        sidebar.Dock = DockStyle.Left;
        NavMenuStyle.Apply(_nav);
        _nav.Items.AddRange(_navTitles.Cast<object>().ToArray());
        _nav.DrawItem += DrawNavItem;
        _nav.SelectedIndexChanged += (_, _) => ShowNav(_nav.SelectedIndex);
        NavMenuStyle.BindHover(_nav, () => _navHover, v => _navHover = v);
        sidebar.Controls.Add(_nav);

        _rightHost.Dock = DockStyle.Fill;
        _rightHost.BackColor = AppTheme.Surface;
        _rightHost.Padding = new Padding(UiScale.S(12), UiScale.S(8), UiScale.S(12), UiScale.S(8));

        _pageTitle.Dock = DockStyle.Top;
        _pageTitle.AutoSize = false;
        _pageTitle.Height = Math.Max(UiScale.S(36), UiFit.ControlHeight(UiFit.UiFontBold()));
        _pageTitle.Font = UiFit.UiFontBold();
        _pageTitle.ForeColor = AppTheme.TextHeader;
        _pageTitle.TextAlign = ContentAlignment.MiddleLeft;
        _pageTitle.BackColor = AppTheme.Surface;

        _scroll.Dock = DockStyle.Fill;
        _scroll.AutoScroll = true;
        _scroll.BackColor = AppTheme.Surface;
        _scroll.Padding = new Padding(0, UiScale.S(4), 0, 0);

        _rightHost.Controls.Add(_scroll);
        _rightHost.Controls.Add(_pageTitle);
        _rightHost.Resize += (_, _) =>
        {
            if (_nav.SelectedIndex >= 0)
                ShowNav(_nav.SelectedIndex);
        };

        split.Controls.Add(_rightHost);
        split.Controls.Add(sidebar);

        Controls.Add(split);
        Controls.Add(south);

        CancelButton = cancel;
        Load += (_, _) =>
        {
            if (_nav.Items.Count > 0)
                _nav.SelectedIndex = 0;
            else
                ShowEmpty();
        };
    }

    private void DrawNavItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || e.Index >= _navTitles.Count) return;
        var title = _navTitles[e.Index];
        NavMenuStyle.DrawItem(
            e,
            title,
            _nav.Font ?? UiFit.UiFont,
            e.Index == _navHover,
            separator: false,
            matchCount: e.Index < _navCounts.Length ? _navCounts[e.Index] : -1,
            icon: IconForNav(title));
    }

    private static Image IconForNav(string title)
    {
        if (title == AppLang.L("Server专属", "Server only")
            || title == AppLang.L("服务器用途", "Server profile"))
            return MenuIcons.ServerRoles;
        if (title == AppLang.L("账户策略", "Account policy"))
            return MenuIcons.Autologon;
        if (title == AppLang.L("账户与登录", "Account & sign-in"))
            return MenuIcons.Identity;
        if (title == AppLang.L("资源管理器", "File Explorer"))
            return MenuIcons.NavExplorer;
        if (title == AppLang.L("桌面外观", "Desktop look"))
            return MenuIcons.DesktopMaintenance;
        if (title == AppLang.L("远程与网络", "Remote & network"))
            return MenuIcons.NavNetwork;
        if (title == AppLang.L("隐私与体验", "Privacy & UX"))
            return MenuIcons.NavPrivacy;
        if (title == AppLang.L("性能及安全", "Performance & security"))
            return MenuIcons.SecurityCenter;
        if (title == AppLang.L("登录启动项", "Startup apps"))
            return MenuIcons.TaskScheduler;
        if (title == AppLang.L("电源与后台", "Power & background"))
            return MenuIcons.ShutdownTimer;
        if (title == AppLang.L("高级设置", "Advanced settings"))
            return MenuIcons.Advanced;
        if (title == AppLang.L("右键菜单", "Context menu"))
            return MenuIcons.ContextMenu;
        if (title == AppLang.L("服务优化", "Service optimize"))
            return MenuIcons.ComputerMgmt;
        if (title == AppLang.L("DNS 设置", "DNS settings"))
            return MenuIcons.NavDns;
        if (title == AppLang.L("自定义配置", "Custom config"))
            return MenuIcons.Script;
        if (title == AppLang.L("配置脚本", "Config scripts"))
            return MenuIcons.Script;
        return MenuIcons.Quick;
    }

    private void ShowEmpty()
    {
        _pageTitle.Text = AppLang.L("无差异项", "No changes");
        _scroll.Controls.Clear();
        _detail.Text = "";
    }

    private void ShowNav(int index)
    {
        _scroll.SuspendLayout();
        _scroll.Controls.Clear();
        _focusLine = null;
        _detail.Text = "";

        if (index < 0 || index >= _navTitles.Count)
        {
            _scroll.ResumeLayout();
            ShowEmpty();
            return;
        }

        var nav = _navTitles[index];
        var pageLines = _lines.Where(x => x.Nav == nav).ToList();
        _pageTitle.Text = AppLang.Lf("{0}（{1}）", "{0} ({1})", nav, pageLines.Count);

        var y = 0;
        var width = Math.Max(UiScale.S(480), _scroll.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - UiScale.S(8));
        string? lastSection = null;

        foreach (var line in pageLines)
        {
            if (!string.Equals(lastSection, line.Section, StringComparison.Ordinal))
            {
                lastSection = line.Section;
                var head = BuildSectionHeader(line.Section, width);
                head.Location = new Point(0, y);
                _scroll.Controls.Add(head);
                y += head.Height + UiScale.S(4);
            }

            var row = BuildEntryRow(line, width);
            row.Location = new Point(0, y);
            _scroll.Controls.Add(row);
            y += row.Height + UiScale.S(2);
        }

        _scroll.ResumeLayout();
        if (pageLines.Count > 0)
            FocusLine(pageLines[0]);
    }

    private Control BuildSectionHeader(string section, int width)
    {
        var h = Math.Max(UiScale.S(32), UiFit.LineHeight(UiFit.UiFontBold()) + UiScale.S(10));
        var head = new BufferedPanel
        {
            Size = new Size(width, h),
            BackColor = AppTheme.GroupBg,
        };
        var label = new Label
        {
            Text = string.IsNullOrWhiteSpace(section) ? AppLang.L("未分组", "Ungrouped") : section,
            Dock = DockStyle.Fill,
            Font = UiFit.UiFontBold(),
            ForeColor = AppTheme.TextHeader,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(UiScale.S(12), 0, 0, 0),
            BackColor = Color.Transparent,
        };
        head.Controls.Add(label);
        return head;
    }

    private Control BuildEntryRow(PreviewLine line, int width)
    {
        var rowH = Math.Max(UiScale.S(56), UiFit.LineHeight() * 2 + UiScale.S(18));
        var wrap = new BufferedPanel
        {
            Size = new Size(width, rowH),
            BackColor = AppTheme.SurfaceCard,
            Cursor = Cursors.Hand,
            Tag = line,
        };

        var check = new CheckBox
        {
            Checked = line.Selected,
            AutoSize = false,
            Size = new Size(UiScale.S(22), UiScale.S(22)),
            Location = new Point(UiScale.S(10), (rowH - UiScale.S(22)) / 2),
            BackColor = Color.Transparent,
            FlatStyle = FlatStyle.System,
        };
        check.CheckedChanged += (_, _) => { line.Selected = check.Checked; };

        var title = new SingleLineLabel
        {
            Text = line.Title,
            Font = SettingListLayout.ItemFont,
            ForeColor = KindColor(line.Kind),
            Location = new Point(UiScale.S(40), UiScale.S(8)),
            Size = new Size(Math.Max(80, width - UiScale.S(280)), UiFit.LineHeight()),
            BackColor = Color.Transparent,
        };

        var meaning = new SingleLineLabel
        {
            Text = line.Meaning,
            Font = UiFit.UiFontSmall,
            ForeColor = AppTheme.TextMute,
            Location = new Point(UiScale.S(40), UiScale.S(8) + UiFit.LineHeight() + UiScale.S(2)),
            Size = new Size(Math.Max(80, width - UiScale.S(280)), UiFit.LineHeight(UiFit.UiFontSmall)),
            BackColor = Color.Transparent,
        };
        _tip.SetToolTip(meaning, MeaningTipText(line));
        _tip.SetToolTip(title, MeaningTipText(line));

        var delta = new Label
        {
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleRight,
            Font = UiFit.UiFontSmall,
            ForeColor = AppTheme.TextMain,
            BackColor = Color.Transparent,
            Text = AppLang.Lf("{0}  →  {1}", "{0}  →  {1}", line.Current, line.Target),
            Size = new Size(UiScale.S(220), rowH - UiScale.S(8)),
            Location = new Point(width - UiScale.S(232), UiScale.S(4)),
        };

        void SelectMe(object? _, EventArgs __)
        {
            FocusLine(line);
            wrap.BackColor = AppTheme.PrimaryPale;
        }

        wrap.Click += SelectMe;
        title.Click += SelectMe;
        meaning.Click += SelectMe;
        delta.Click += SelectMe;
        wrap.Controls.Add(check);
        wrap.Controls.Add(title);
        wrap.Controls.Add(meaning);
        wrap.Controls.Add(delta);

        wrap.Resize += (_, _) =>
        {
            var w = wrap.ClientSize.Width;
            title.Width = Math.Max(80, w - UiScale.S(280));
            meaning.Width = title.Width;
            delta.Left = w - UiScale.S(232);
        };

        return wrap;
    }

    private void FocusLine(PreviewLine line)
    {
        _focusLine = line;
        foreach (Control c in _scroll.Controls)
        {
            if (c.Tag is PreviewLine)
                c.BackColor = ReferenceEquals(c.Tag, line) ? AppTheme.PrimaryPale : AppTheme.SurfaceCard;
        }

        _detail.Text = AppLang.Lf(
            "【{0} · {1}】{2}\r\n{3}\r\n当前：{4}  →  恢复为：{5}",
            "[{0} · {1}] {2}\r\n{3}\r\nCurrent: {4}  →  Restore to: {5}",
            line.Nav, line.Section, line.Title, MeaningTipText(line), line.Current, line.Target);
        _detail.SelectionStart = 0;
        _detail.SelectionLength = 0;
    }

    private void RefreshNavBadges()
    {
        for (var i = 0; i < _navTitles.Count; i++)
            _navCounts[i] = _lines.Count(x => x.Nav == _navTitles[i] && x.Selected);
        _nav.Invalidate();
    }

    private void SetAllChecked(bool on)
    {
        foreach (var line in _lines)
            line.Selected = on;
        // 重建当前页勾选状态
        ShowNav(_nav.SelectedIndex);
        RefreshNavBadges();
    }

    private static string MeaningTipText(PreviewLine line)
    {
        var summary = (line.Meaning ?? "").Trim();
        var full = (line.MeaningFull ?? "").Trim();
        if (full.Length == 0) return summary;
        if (summary.Length == 0) return full;
        if (string.Equals(summary, full, StringComparison.Ordinal)) return full;
        if (full.StartsWith(summary.TrimEnd('…', '.'), StringComparison.Ordinal))
            return full;
        return summary + "\r\n" + full;
    }

    /// <summary>
    /// 按主界面「侧栏栏目 → 分区 → 右侧每个开关/下拉」逐条对比；一条 UI 对应一行，不拆附属 State 字段。
    /// </summary>
    public static List<PreviewLine> Compute(
        Optimizer.State current,
        OptProfileBundle bundle,
        IReadOnlyList<SettingUiMeta>? uiRows = null)
    {
        var lines = new List<PreviewLine>();
        uiRows ??= Array.Empty<SettingUiMeta>();

        if (bundle.HasSettings && uiRows.Count > 0)
        {
            foreach (var meta in uiRows)
            {
                if (meta.StateKeys.Length == 0) continue;
                var curText = meta.FormatValue?.Invoke(current) ?? "";
                var impText = meta.FormatValue?.Invoke(bundle.State) ?? "";
                if (string.Equals(curText, impText, StringComparison.Ordinal))
                    continue;

                var kind = meta.ChoiceLabels is { Length: > 0 }
                    ? PreviewKind.Property
                    : (LooksEnabled(impText) ? PreviewKind.ToggleOn : PreviewKind.ToggleOff);

                lines.Add(new PreviewLine
                {
                    Kind = kind,
                    StateKey = meta.StateKeys[0],
                    StateKeys = meta.StateKeys.ToArray(),
                    Nav = meta.Nav,
                    Section = meta.Section,
                    Title = meta.Title,
                    Meaning = meta.Meaning,
                    MeaningFull = string.IsNullOrWhiteSpace(meta.MeaningFull) ? meta.Meaning : meta.MeaningFull,
                    Current = curText,
                    Target = impText,
                    Selected = true,
                });
            }
        }
        else if (bundle.HasSettings)
        {
            AppendLegacyFieldDiffs(lines, current, bundle.State);
        }

        if (bundle.HasServices && bundle.Services is { Count: > 0 })
        {
            foreach (var e in bundle.Services.Where(x => !string.IsNullOrWhiteSpace(x.Name)))
            {
                var name = e.Name!;
                var target = ServiceSnapshotStore.ParseStartType(e.StartType);
                if (target is not (ServiceStartTypeKind.Automatic
                    or ServiceStartTypeKind.Manual
                    or ServiceStartTypeKind.Disabled))
                    continue;

                var currentStart = ServiceOptimizeHelper.ReadStartTypePublic(name);
                if (currentStart is ServiceStartTypeKind.Missing || currentStart == target)
                    continue;

                var title = string.IsNullOrWhiteSpace(e.DisplayName) ? name : e.DisplayName!;
                var kind = target switch
                {
                    ServiceStartTypeKind.Disabled => PreviewKind.ServiceDisable,
                    ServiceStartTypeKind.Automatic => PreviewKind.ServiceAuto,
                    _ => PreviewKind.ServiceManual,
                };
                var meaning = AppLang.Lf(
                    "将服务「{0}」的启动类型从「{1}」改为「{2}」。对应侧栏「服务优化」。",
                    "Change service “{0}” start type from “{1}” to “{2}”. Matches Service optimize.",
                    name,
                    ServiceOptimizeHelper.StartTypeLabel(currentStart),
                    ServiceOptimizeHelper.StartTypeLabel(target));
                lines.Add(new PreviewLine
                {
                    Kind = kind,
                    ServiceName = name,
                    Nav = AppLang.L("服务优化", "Service optimize"),
                    Section = AppLang.L("服务启动类型", "Start type"),
                    Title = title,
                    Meaning = Compact(meaning, 48),
                    MeaningFull = meaning,
                    Current = ServiceOptimizeHelper.StartTypeLabel(currentStart),
                    Target = ServiceOptimizeHelper.StartTypeLabel(target),
                    Selected = true,
                });
            }
        }

        if (bundle.HasServerProfile)
        {
            var sp = ServerProfile.Load();
            if (bundle.OptimizationLevel.HasValue
                && bundle.OptimizationLevel.Value != sp.OptimizationLevel)
            {
                lines.Add(new PreviewLine
                {
                    Kind = PreviewKind.Other,
                    OtherId = "OptimizationLevel",
                    Nav = AppLang.L("服务器用途", "Server profile"),
                    Section = AppLang.L("优化等级", "Level"),
                    Title = AppLang.L("优化等级", "Optimization level"),
                    Meaning = AppLang.L("控制可自动写入的风险档位（仅检测/保守/标准/激进）。",
                        "Controls which risk level may be written (detect/conservative/standard/aggressive)."),
                    MeaningFull = AppLang.L("控制可自动写入的风险档位（仅检测/保守/标准/激进）。",
                        "Controls which risk level may be written (detect/conservative/standard/aggressive)."),
                    Current = LevelText(sp.OptimizationLevel),
                    Target = LevelText(bundle.OptimizationLevel.Value),
                    Selected = true,
                });
            }
            if (bundle.ServerRoles.HasValue && bundle.ServerRoles.Value != sp.Roles)
            {
                lines.Add(new PreviewLine
                {
                    Kind = PreviewKind.Other,
                    OtherId = "ServerRoles",
                    Nav = AppLang.L("服务器用途", "Server profile"),
                    Section = AppLang.L("角色勾选", "Roles"),
                    Title = AppLang.L("服务器用途角色", "Server roles"),
                    Meaning = AppLang.L("影响优化顾问与推荐保留的服务。",
                        "Affects advisor tips and services kept by role."),
                    MeaningFull = AppLang.L("影响优化顾问与推荐保留的服务。",
                        "Affects advisor tips and services kept by role."),
                    Current = sp.Roles.ToString(),
                    Target = bundle.ServerRoles.Value.ToString(),
                    Selected = true,
                });
            }
        }

        if (bundle.HasScriptOverrides)
        {
            lines.Add(new PreviewLine
            {
                Kind = PreviewKind.Other,
                OtherId = "ScriptOverrides",
                Nav = AppLang.L("配置脚本", "Config scripts"),
                Section = AppLang.L("脚本覆盖", "Overrides"),
                Title = AppLang.L("配置脚本覆盖", "Script overrides"),
                Meaning = AppLang.L("覆盖本机已保存的开启/关闭脚本正文。",
                    "Overwrite saved on/off script bodies on this PC."),
                MeaningFull = AppLang.L("覆盖本机已保存的开启/关闭脚本正文。",
                    "Overwrite saved on/off script bodies on this PC."),
                Current = AppLang.L("本机", "Local"),
                Target = AppLang.L("导入", "Import"),
                Selected = true,
            });
        }
        if (bundle.HasCustomPacks)
        {
            lines.Add(new PreviewLine
            {
                Kind = PreviewKind.Other,
                OtherId = "CustomPacks",
                Nav = AppLang.L("自定义配置", "Custom config"),
                Section = AppLang.L("方案", "Packs"),
                Title = AppLang.L("自定义方案", "Custom packs"),
                Meaning = AppLang.L("用配置文件中的自定义方案替换本机方案。",
                    "Replace local custom packs with those in the profile."),
                MeaningFull = AppLang.L("用配置文件中的自定义方案替换本机方案。",
                    "Replace local custom packs with those in the profile."),
                Current = AppLang.L("本机", "Local"),
                Target = AppLang.L("导入", "Import"),
                Selected = true,
            });
        }

        return lines;
    }

    private static void AppendLegacyFieldDiffs(
        List<PreviewLine> lines, Optimizer.State current, Optimizer.State imported)
    {
        var curMap = StateMapper.ToMap(current);
        var impMap = StateMapper.ToMap(imported);
        foreach (var kv in impMap)
        {
            if (!curMap.TryGetValue(kv.Key, out var cur) || cur == kv.Value) continue;
            lines.Add(new PreviewLine
            {
                Kind = kv.Value ? PreviewKind.ToggleOn : PreviewKind.ToggleOff,
                StateKey = kv.Key,
                StateKeys = [kv.Key],
                Nav = AppLang.L("优化开关", "Toggles"),
                Section = AppLang.L("未分组", "Ungrouped"),
                Title = kv.Key,
                Meaning = kv.Key,
                MeaningFull = kv.Key,
                Current = BoolText(cur),
                Target = BoolText(kv.Value),
                Selected = true,
            });
        }
    }

    private static bool LooksEnabled(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        return text.IndexOf(AppLang.L("开启", "On"), StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static Color KindColor(PreviewKind k) => k switch
    {
        PreviewKind.ToggleOn => Color.FromArgb(40, 140, 70),
        PreviewKind.ToggleOff => Color.FromArgb(180, 80, 40),
        PreviewKind.ServiceDisable => Color.FromArgb(180, 80, 40),
        PreviewKind.ServiceAuto => Color.FromArgb(40, 100, 160),
        PreviewKind.ServiceManual => Color.FromArgb(40, 100, 160),
        PreviewKind.Property => Color.FromArgb(40, 100, 160),
        _ => AppTheme.TextMain,
    };

    private static string BoolText(bool v) => v ? AppLang.L("开启", "On") : AppLang.L("关闭", "Off");

    private static string LevelText(int level) => level switch
    {
        0 => AppLang.L("仅检测", "Detect only"),
        1 => AppLang.L("保守", "Conservative"),
        2 => AppLang.L("标准", "Standard"),
        3 => AppLang.L("激进", "Aggressive"),
        _ => level.ToString(),
    };

    private static string Compact(string text, int max)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        var t = text.Trim();
        var cut = t.IndexOfAny(['。', '；', '.', ';']);
        if (cut > 4 && cut < max) t = t.Substring(0, cut);
        if (t.Length > max) t = t.Substring(0, max - 1) + "…";
        return t;
    }

    internal enum PreviewKind
    {
        ToggleOn = 0,
        ToggleOff = 1,
        Property = 2,
        ServiceDisable = 3,
        ServiceAuto = 4,
        ServiceManual = 5,
        Other = 6,
    }

    internal sealed class PreviewLine
    {
        public PreviewKind Kind { get; set; }
        public string Nav { get; set; } = "";
        public string Section { get; set; } = "";
        public string Title { get; set; } = "";
        public string Meaning { get; set; } = "";
        public string MeaningFull { get; set; } = "";
        public string Current { get; set; } = "";
        public string Target { get; set; } = "";
        public string? StateKey { get; set; }
        public string[] StateKeys { get; set; } = [];
        public string? ServiceName { get; set; }
        public string? OtherId { get; set; }
        public bool Selected { get; set; } = true;

        public PreviewLine Clone() => new()
        {
            Kind = Kind,
            Nav = Nav,
            Section = Section,
            Title = Title,
            Meaning = Meaning,
            MeaningFull = MeaningFull,
            Current = Current,
            Target = Target,
            StateKey = StateKey,
            StateKeys = StateKeys.Length == 0 ? [] : StateKeys.ToArray(),
            ServiceName = ServiceName,
            OtherId = OtherId,
            Selected = Selected,
        };
    }
}
