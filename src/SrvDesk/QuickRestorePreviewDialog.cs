namespace SrvDesk;

/// <summary>侧栏栏目元数据：对齐主界面分组，供一键恢复预览使用。</summary>
internal sealed class SettingUiMeta
{
    public string Nav { get; set; } = "";
    public string Section { get; set; } = "";
    public string Title { get; set; } = "";
    public string Meaning { get; set; } = "";
}

/// <summary>一键快速恢复 / 导入配置前：对齐侧栏栏目、说明含义，并允许勾选要写入的项。</summary>
internal sealed class QuickRestorePreviewDialog : Form
{
    private readonly ListView _list = new();
    private readonly List<PreviewLine> _lines;

    public IReadOnlyList<PreviewLine> SelectedLines =>
        _lines.Where(x => x.Selected).ToList();

    public QuickRestorePreviewDialog(
        IReadOnlyList<PreviewLine> lines,
        string sourcePath,
        string? windowTitle = null,
        string? confirmButtonText = null)
    {
        _ = sourcePath; // 调用方仍传入路径，界面不再展示以免干扰
        _lines = lines.Select(x => x.Clone()).ToList();
        foreach (var line in _lines)
            line.Selected = true;

        Text = windowTitle ?? AppLang.L("一键快速恢复 · 挑选变更", "One-click restore · Pick changes");
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.None;
        Font = UiFit.UiFont;
        BackColor = AppTheme.Surface;
        ClientSize = UiScale.Size(980, 560);
        MinimumSize = UiScale.Size(860, 480);

        var body = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(UiScale.S(16), UiScale.S(12), UiScale.S(16), UiScale.S(8)),
            BackColor = AppTheme.Surface,
        };

        _list.Dock = DockStyle.Fill;
        _list.View = View.Details;
        _list.FullRowSelect = true;
        _list.GridLines = false;
        _list.HideSelection = false;
        _list.MultiSelect = true;
        _list.CheckBoxes = true;
        _list.BorderStyle = BorderStyle.FixedSingle;
        _list.BackColor = AppTheme.SurfaceCard;
        _list.ForeColor = AppTheme.TextMain;
        _list.Font = UiFit.UiFont;
        _list.Columns.Add(AppLang.L("变更", "Change"), UiScale.S(80));
        _list.Columns.Add(AppLang.L("栏目", "Column"), UiScale.S(110));
        _list.Columns.Add(AppLang.L("分区", "Section"), UiScale.S(130));
        _list.Columns.Add(AppLang.L("项目", "Item"), UiScale.S(220));
        _list.Columns.Add(AppLang.L("含义", "Meaning"), UiScale.S(240));
        _list.Columns.Add(AppLang.L("当前", "Current"), UiScale.S(80));
        _list.Columns.Add(AppLang.L("恢复为", "To"), UiScale.S(80));
        _list.HandleCreated += (_, _) => UiBuffer.EnableListView(_list);
        _list.ItemChecked += (_, e) =>
        {
            if (e.Item?.Tag is PreviewLine line)
                line.Selected = e.Item.Checked;
        };

        foreach (var line in _lines)
        {
            var row = new ListViewItem(KindLabel(line.Kind))
            {
                Checked = true,
                Tag = line,
                ForeColor = KindColor(line.Kind),
            };
            row.SubItems.Add(line.Nav);
            row.SubItems.Add(line.Section);
            row.SubItems.Add(line.Title);
            row.SubItems.Add(line.Meaning);
            row.SubItems.Add(line.Current);
            row.SubItems.Add(line.Target);
            _list.Items.Add(row);
        }

        if (_lines.Count == 0)
        {
            var empty = new ListViewItem(AppLang.L("提示", "Note"));
            empty.SubItems.Add("—");
            empty.SubItems.Add("—");
            empty.SubItems.Add(AppLang.L("与当前机器无差异", "No diffs vs this PC"));
            empty.SubItems.Add(AppLang.L("可仍写入脚本/方案（若配置含有）", "Scripts/packs may still apply"));
            empty.SubItems.Add("—");
            empty.SubItems.Add("—");
            _list.Items.Add(empty);
        }

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = UiFit.ControlHeight() + UiScale.S(20),
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, UiScale.S(8), 0, 0),
            BackColor = AppTheme.Surface,
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

        body.Controls.Add(_list);
        body.Controls.Add(footer);
        Controls.Add(body);

        CancelButton = cancel;
        Load += (_, _) => UiBuffer.FitListViewColumn(_list, 4, UiScale.S(160));
        Resize += (_, _) =>
        {
            try { UiBuffer.FitListViewColumn(_list, 4, UiScale.S(160)); }
            catch { /* ignore */ }
        };
    }

    private void SetAllChecked(bool on)
    {
        _list.BeginUpdate();
        try
        {
            foreach (ListViewItem row in _list.Items)
            {
                if (row.Tag is not PreviewLine) continue;
                row.Checked = on;
            }
        }
        finally
        {
            _list.EndUpdate();
        }
    }

    public static List<PreviewLine> Compute(
        Optimizer.State current,
        OptProfileBundle bundle,
        IReadOnlyDictionary<string, SettingUiMeta>? uiMeta = null)
    {
        var lines = new List<PreviewLine>();
        uiMeta ??= new Dictionary<string, SettingUiMeta>(StringComparer.Ordinal);

        if (bundle.HasSettings)
        {
            var curMap = StateMapper.ToMap(current);
            var impMap = StateMapper.ToMap(bundle.State);
            foreach (var kv in impMap)
            {
                if (!curMap.TryGetValue(kv.Key, out var cur) || cur == kv.Value) continue;
                var meta = ResolveMeta(kv.Key, uiMeta);
                var kind = kv.Value ? PreviewKind.ToggleOn : PreviewKind.ToggleOff;
                lines.Add(new PreviewLine
                {
                    Kind = kind,
                    StateKey = kv.Key,
                    Nav = meta.Nav,
                    Section = meta.Section,
                    Title = meta.Title,
                    Meaning = Compact(meta.Meaning, 36),
                    MeaningFull = meta.Meaning,
                    Current = BoolText(cur),
                    Target = BoolText(kv.Value),
                    Selected = true,
                });
            }

            var curExtra = StateMapper.ToExtra(current)
                .Where(e => string.Equals(e.Kind, "int", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(e => e.Key!, e => e.Text ?? "", StringComparer.Ordinal);
            foreach (var e in StateMapper.ToExtra(bundle.State)
                         .Where(x => string.Equals(x.Kind, "int", StringComparison.OrdinalIgnoreCase)))
            {
                if (string.IsNullOrEmpty(e.Key)) continue;
                curExtra.TryGetValue(e.Key!, out var curText);
                curText ??= "";
                var impText = e.Text ?? "";
                if (string.Equals(curText, impText, StringComparison.Ordinal)) continue;
                var meta = ResolveMeta(e.Key!, uiMeta);
                lines.Add(new PreviewLine
                {
                    Kind = PreviewKind.Property,
                    StateKey = e.Key!,
                    Nav = meta.Nav,
                    Section = meta.Section,
                    Title = meta.Title,
                    Meaning = Compact(meta.Meaning, 36),
                    MeaningFull = meta.Meaning,
                    Current = FormatExtra(e.Key!, curText),
                    Target = FormatExtra(e.Key!, impText),
                    Selected = true,
                });
            }
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
                    Meaning = Compact(meaning, 36),
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

        lines.Sort((a, b) =>
        {
            var c = string.Compare(a.Nav, b.Nav, StringComparison.CurrentCultureIgnoreCase);
            if (c != 0) return c;
            c = string.Compare(a.Section, b.Section, StringComparison.CurrentCultureIgnoreCase);
            if (c != 0) return c;
            c = a.Kind.CompareTo(b.Kind);
            return c != 0 ? c : string.Compare(a.Title, b.Title, StringComparison.CurrentCultureIgnoreCase);
        });
        return lines;
    }

    private static SettingUiMeta ResolveMeta(string key, IReadOnlyDictionary<string, SettingUiMeta> uiMeta)
    {
        if (uiMeta.TryGetValue(key, out var m))
            return m;

        // 常见别名
        if (string.Equals(key, "UacNotifyLevel", StringComparison.Ordinal)
            && uiMeta.TryGetValue("DisableUac", out m))
            return m;
        if (string.Equals(key, "UltimatePerfPower", StringComparison.Ordinal)
            && uiMeta.TryGetValue("HighPerfPower", out m))
            return m;

        var title = TitleFromCatalog(key);
        return new SettingUiMeta
        {
            Nav = AppLang.L("优化开关", "Toggles"),
            Section = AppLang.L("未分组", "Ungrouped"),
            Title = title,
            Meaning = title,
        };
    }

    private static string TitleFromCatalog(string key)
    {
        try
        {
            var f = typeof(SettingCatalog).GetField(key,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (f?.GetValue(null) is SettingHelpInfo help && !string.IsNullOrWhiteSpace(help.Summary))
                return Compact(help.Summary, 28);
        }
        catch { /* ignore */ }

        return key switch
        {
            "UacNotifyLevel" => AppLang.L("UAC 通知级别", "UAC notify level"),
            _ => key,
        };
    }

    private static string KindLabel(PreviewKind k) => k switch
    {
        PreviewKind.ToggleOn => AppLang.L("开启", "On"),
        PreviewKind.ToggleOff => AppLang.L("关闭", "Off"),
        PreviewKind.Property => AppLang.L("属性", "Prop"),
        PreviewKind.ServiceDisable => AppLang.L("禁服务", "Disable"),
        PreviewKind.ServiceAuto => AppLang.L("改自动", "Auto"),
        PreviewKind.ServiceManual => AppLang.L("改手动", "Manual"),
        _ => AppLang.L("其它", "Other"),
    };

    private static Color KindColor(PreviewKind k) => k switch
    {
        PreviewKind.ToggleOn => Color.FromArgb(40, 140, 70),
        PreviewKind.ToggleOff => Color.FromArgb(180, 80, 40),
        PreviewKind.ServiceDisable => Color.FromArgb(180, 80, 40),
        PreviewKind.ServiceAuto => Color.FromArgb(40, 100, 160),
        PreviewKind.ServiceManual => Color.FromArgb(40, 100, 160),
        _ => AppTheme.TextMain,
    };

    private static string BoolText(bool v) => v ? AppLang.L("开", "On") : AppLang.L("关", "Off");

    private static string LevelText(int level) => level switch
    {
        0 => AppLang.L("仅检测", "Detect only"),
        1 => AppLang.L("保守", "Conservative"),
        2 => AppLang.L("标准", "Standard"),
        3 => AppLang.L("激进", "Aggressive"),
        _ => level.ToString(),
    };

    private static string FormatExtra(string key, string text)
    {
        if (string.Equals(key, "UacNotifyLevel", StringComparison.Ordinal))
            return text switch
            {
                "0" => AppLang.L("始终通知", "Always"),
                "1" => AppLang.L("默认", "Default"),
                "2" => AppLang.L("从不", "Never"),
                _ => text,
            };
        return text;
    }

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
            ServiceName = ServiceName,
            OtherId = OtherId,
            Selected = Selected,
        };
    }
}
