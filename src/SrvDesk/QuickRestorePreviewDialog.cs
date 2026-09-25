namespace SrvDesk;

/// <summary>一键快速恢复前：相对本机展示将开启/关闭的开关、将禁用/调整的服务等。</summary>
internal sealed class QuickRestorePreviewDialog : Form
{
    public QuickRestorePreviewDialog(IReadOnlyList<PreviewLine> lines, string sourcePath)
    {
        Text = AppLang.L("一键快速恢复 · 变更预览", "One-click restore · Preview");
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        Font = UiFit.UiFont;
        BackColor = AppTheme.Surface;
        ClientSize = UiScale.Size(820, 560);
        MinimumSize = UiScale.Size(720, 480);

        var body = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(UiScale.S(16), UiScale.S(12), UiScale.S(16), UiScale.S(8)),
            BackColor = AppTheme.Surface,
        };

        var onCount = lines.Count(x => x.Kind == PreviewKind.ToggleOn);
        var offCount = lines.Count(x => x.Kind == PreviewKind.ToggleOff);
        var propCount = lines.Count(x => x.Kind == PreviewKind.Property);
        var disableSvc = lines.Count(x => x.Kind == PreviewKind.ServiceDisable);
        var otherSvc = lines.Count(x => x.Kind is PreviewKind.ServiceAuto or PreviewKind.ServiceManual);
        var other = lines.Count(x => x.Kind == PreviewKind.Other);

        var summary = new Label
        {
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = UiScale.S(72),
            Text = AppLang.Lf(
                "相对当前电脑将发生以下变更（共 {0} 项）：\r\n开启开关 {1} · 关闭开关 {2} · 其它属性 {3} · 禁用服务 {4} · 调整服务 {5}{6}\r\n确认后才会写入系统。",
                "Relative to this PC ({0} change(s)):\r\nTurn on {1} · Turn off {2} · Other props {3} · Disable services {4} · Adjust services {5}{6}\r\nNothing is written until you confirm.",
                lines.Count, onCount, offCount, propCount, disableSvc, otherSvc,
                other > 0 ? AppLang.Lf(" · 其它 {0}", " · Other {0}", other) : ""),
            ForeColor = AppTheme.TextMain,
        };

        var pathLbl = new Label
        {
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = UiScale.S(28),
            AutoEllipsis = true,
            Text = AppLang.L("来源：", "From: ") + sourcePath,
            ForeColor = AppTheme.TextMute,
        };

        var list = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = false,
            HideSelection = false,
            MultiSelect = false,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = AppTheme.SurfaceCard,
            ForeColor = AppTheme.TextMain,
            Font = UiFit.UiFont,
        };
        list.Columns.Add(AppLang.L("类别", "Kind"), UiScale.S(110));
        list.Columns.Add(AppLang.L("项目", "Item"), UiScale.S(280));
        list.Columns.Add(AppLang.L("当前", "Current"), UiScale.S(120));
        list.Columns.Add(AppLang.L("恢复为", "Restore to"), UiScale.S(120));
        list.HandleCreated += (_, _) => UiBuffer.EnableListView(list);

        foreach (var line in lines)
        {
            var row = new ListViewItem(KindLabel(line.Kind)) { ForeColor = KindColor(line.Kind) };
            row.SubItems.Add(line.Title);
            row.SubItems.Add(line.Current);
            row.SubItems.Add(line.Target);
            list.Items.Add(row);
        }

        if (lines.Count == 0)
        {
            var empty = new ListViewItem(AppLang.L("提示", "Note"));
            empty.SubItems.Add(AppLang.L("与当前机器相比没有可写差异（脚本/方案仍可能覆盖本地）。",
                "No writable diffs vs this PC (scripts/packs may still overwrite local)."));
            empty.SubItems.Add("—");
            empty.SubItems.Add("—");
            list.Items.Add(empty);
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

        var ok = ThemedSettingsChrome.CreateButton(AppLang.L("确认并应用到系统", "Confirm & Apply"), true);
        UiFit.FitButton(ok, padding: 28);
        ok.DialogResult = DialogResult.Yes;
        ok.Margin = new Padding(UiScale.S(8), 0, 0, 0);
        ok.Enabled = true;

        footer.Controls.Add(cancel);
        footer.Controls.Add(ok);

        body.Controls.Add(list);
        body.Controls.Add(footer);
        body.Controls.Add(pathLbl);
        body.Controls.Add(summary);
        Controls.Add(body);

        AcceptButton = ok;
        CancelButton = cancel;
        Load += (_, _) => UiBuffer.FitListViewColumn(list, 1, UiScale.S(200));
        Resize += (_, _) =>
        {
            try { UiBuffer.FitListViewColumn(list, 1, UiScale.S(200)); }
            catch { /* ignore */ }
        };
    }

    public static List<PreviewLine> Compute(Optimizer.State current, OptProfileBundle bundle)
    {
        var lines = new List<PreviewLine>();

        if (bundle.HasSettings)
        {
            var curMap = StateMapper.ToMap(current);
            var impMap = StateMapper.ToMap(bundle.State);
            foreach (var kv in impMap.OrderBy(x => TitleFor(x.Key), StringComparer.CurrentCultureIgnoreCase))
            {
                if (!curMap.TryGetValue(kv.Key, out var cur) || cur == kv.Value) continue;
                var title = TitleFor(kv.Key);
                if (kv.Value)
                {
                    lines.Add(new PreviewLine(PreviewKind.ToggleOn, title,
                        BoolText(false), BoolText(true)));
                }
                else
                {
                    lines.Add(new PreviewLine(PreviewKind.ToggleOff, title,
                        BoolText(true), BoolText(false)));
                }
            }

            var curExtra = StateMapper.ToExtra(current)
                .Where(e => string.Equals(e.Kind, "int", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(e => e.Key!, e => e.Text ?? "", StringComparer.Ordinal);
            foreach (var e in StateMapper.ToExtra(bundle.State)
                         .Where(x => string.Equals(x.Kind, "int", StringComparison.OrdinalIgnoreCase))
                         .OrderBy(x => TitleFor(x.Key ?? ""), StringComparer.CurrentCultureIgnoreCase))
            {
                if (string.IsNullOrEmpty(e.Key)) continue;
                curExtra.TryGetValue(e.Key!, out var curText);
                curText ??= "";
                var impText = e.Text ?? "";
                if (string.Equals(curText, impText, StringComparison.Ordinal)) continue;
                lines.Add(new PreviewLine(PreviewKind.Property, TitleFor(e.Key!),
                    FormatExtra(e.Key!, curText), FormatExtra(e.Key!, impText)));
            }
        }

        if (bundle.HasServices && bundle.Services is { Count: > 0 })
        {
            foreach (var e in bundle.Services
                         .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                         .OrderBy(x => x.DisplayName ?? x.Name, StringComparer.CurrentCultureIgnoreCase))
            {
                var name = e.Name!;
                var target = ServiceSnapshotStore.ParseStartType(e.StartType);
                if (target is not (ServiceStartTypeKind.Automatic
                    or ServiceStartTypeKind.Manual
                    or ServiceStartTypeKind.Disabled))
                    continue;

                var currentStart = ServiceOptimizeHelper.ReadStartTypePublic(name);
                if (currentStart == ServiceStartTypeKind.Missing) continue;
                if (currentStart == target) continue;

                var title = string.IsNullOrWhiteSpace(e.DisplayName)
                    ? name
                    : e.DisplayName + " (" + name + ")";
                var curLabel = ServiceOptimizeHelper.StartTypeLabel(currentStart);
                var tgtLabel = ServiceOptimizeHelper.StartTypeLabel(target);
                var kind = target switch
                {
                    ServiceStartTypeKind.Disabled => PreviewKind.ServiceDisable,
                    ServiceStartTypeKind.Automatic => PreviewKind.ServiceAuto,
                    _ => PreviewKind.ServiceManual,
                };
                lines.Add(new PreviewLine(kind, title, curLabel, tgtLabel));
            }
        }

        if (bundle.HasServerProfile)
        {
            var sp = ServerProfile.Load();
            if (bundle.OptimizationLevel.HasValue
                && bundle.OptimizationLevel.Value != sp.OptimizationLevel)
            {
                lines.Add(new PreviewLine(PreviewKind.Other,
                    AppLang.L("优化等级", "Optimization level"),
                    LevelText(sp.OptimizationLevel),
                    LevelText(bundle.OptimizationLevel.Value)));
            }
            if (bundle.ServerRoles.HasValue && bundle.ServerRoles.Value != sp.Roles)
            {
                lines.Add(new PreviewLine(PreviewKind.Other,
                    AppLang.L("服务器用途", "Server roles"),
                    sp.Roles.ToString(),
                    bundle.ServerRoles.Value.ToString()));
            }
        }

        if (bundle.HasScriptOverrides)
            lines.Add(new PreviewLine(PreviewKind.Other,
                AppLang.L("配置脚本覆盖", "Script overrides"),
                AppLang.L("本机现有", "Local"),
                AppLang.L("用配置覆盖", "Overwrite from profile")));
        if (bundle.HasCustomPacks)
            lines.Add(new PreviewLine(PreviewKind.Other,
                AppLang.L("自定义方案", "Custom packs"),
                AppLang.L("本机现有", "Local"),
                AppLang.L("用配置覆盖", "Overwrite from profile")));

        // 排序：开启 → 关闭 → 属性 → 禁用服务 → 自动 → 手动 → 其它
        lines.Sort((a, b) =>
        {
            var c = a.Kind.CompareTo(b.Kind);
            return c != 0 ? c : string.Compare(a.Title, b.Title, StringComparison.CurrentCultureIgnoreCase);
        });
        return lines;
    }

    private static string KindLabel(PreviewKind k) => k switch
    {
        PreviewKind.ToggleOn => AppLang.L("开启开关", "Turn on"),
        PreviewKind.ToggleOff => AppLang.L("关闭开关", "Turn off"),
        PreviewKind.Property => AppLang.L("属性", "Property"),
        PreviewKind.ServiceDisable => AppLang.L("禁用服务", "Disable svc"),
        PreviewKind.ServiceAuto => AppLang.L("改为自动", "Set auto"),
        PreviewKind.ServiceManual => AppLang.L("改为手动", "Set manual"),
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
        if (string.Equals(key, "FolderGroupBy", StringComparison.Ordinal)
            || string.Equals(key, "FolderSortBy", StringComparison.Ordinal)
            || string.Equals(key, "TaskbarSearchMode", StringComparison.Ordinal)
            || string.Equals(key, "ShowDriveLetters", StringComparison.Ordinal))
            return text;
        return text;
    }

    private static string TitleFor(string key)
    {
        if (string.IsNullOrEmpty(key)) return key;
        try
        {
            var f = typeof(SettingCatalog).GetField(key,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (f?.GetValue(null) is SettingHelpInfo help && !string.IsNullOrWhiteSpace(help.Summary))
            {
                var s = help.Summary.Trim();
                var cut = s.IndexOfAny(['。', '；', '.', ';']);
                if (cut > 4 && cut < 36) s = s.Substring(0, cut);
                if (s.Length > 40) s = s.Substring(0, 39) + "…";
                return s;
            }
        }
        catch { /* ignore */ }

        return key switch
        {
            "UacNotifyLevel" => AppLang.L("UAC 通知级别", "UAC notify level"),
            "HighPerfPower" => AppLang.L("高性能电源", "High performance power"),
            "UltimatePerfPower" => AppLang.L("卓越性能电源", "Ultimate performance power"),
            _ => key,
        };
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

    internal readonly struct PreviewLine
    {
        public PreviewKind Kind { get; }
        public string Title { get; }
        public string Current { get; }
        public string Target { get; }

        public PreviewLine(PreviewKind kind, string title, string current, string target)
        {
            Kind = kind;
            Title = title;
            Current = current;
            Target = target;
        }
    }
}
