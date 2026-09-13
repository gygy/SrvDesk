namespace SrvDesk;

/// <summary>导入配置前展示与当前界面开关的差异，确认后才绑定。</summary>
internal sealed class ProfileImportDiffDialog : Form
{
    public ProfileImportDiffDialog(IReadOnlyList<(string Key, string Current, string Imported)> diffs)
    {
        Text = AppLang.L("导入差异确认", "Import diff confirm");
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        Font = UiFit.UiFont;
        BackColor = AppTheme.Surface;
        MinimumSize = UiScale.Size(520, 360);
        ClientSize = UiScale.Size(560, 420);

        var body = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(UiScale.S(16), UiScale.S(12), UiScale.S(16), UiScale.S(8)),
            BackColor = AppTheme.Surface,
        };

        var summary = new Label
        {
            Dock = DockStyle.Top,
            AutoSize = false,
            Height = UiScale.S(40),
            Text = AppLang.Lf(
                "以下 {0} 项与当前界面不同，是否继续导入开关？",
                "{0} setting(s) differ from the current UI. Continue importing toggles?",
                diffs.Count),
            ForeColor = AppTheme.TextMain,
        };

        var list = new ListBox
        {
            Dock = DockStyle.Fill,
            IntegralHeight = false,
            BackColor = AppTheme.SurfaceCard,
            ForeColor = AppTheme.TextMain,
            BorderStyle = BorderStyle.FixedSingle,
            Font = UiFit.UiFont,
        };

        const int maxVisible = 15;
        var show = Math.Min(diffs.Count, maxVisible);
        for (var i = 0; i < show; i++)
        {
            var d = diffs[i];
            list.Items.Add($"{d.Key}: {d.Current} → {d.Imported}");
        }
        if (diffs.Count > maxVisible)
            list.Items.Add(AppLang.Lf("…还有 {0} 项", "…and {0} more", diffs.Count - maxVisible));

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

        var no = ThemedSettingsChrome.CreateButton(AppLang.L("否", "No"), false);
        UiFit.FitButton(no, padding: 28);
        no.DialogResult = DialogResult.No;
        no.Margin = new Padding(0, 0, 0, 0);

        var yes = ThemedSettingsChrome.CreateButton(AppLang.L("是", "Yes"), true);
        UiFit.FitButton(yes, padding: 28);
        yes.DialogResult = DialogResult.Yes;
        yes.Margin = new Padding(UiScale.S(8), 0, 0, 0);

        footer.Controls.Add(no);
        footer.Controls.Add(yes);

        body.Controls.Add(list);
        body.Controls.Add(footer);
        body.Controls.Add(summary);
        Controls.Add(body);

        AcceptButton = yes;
        CancelButton = no;

        var needW = yes.Width + no.Width + UiScale.S(80);
        if (ClientSize.Width < needW)
            ClientSize = new Size(needW, ClientSize.Height);
        MinimumSize = new Size(Math.Max(MinimumSize.Width, needW), MinimumSize.Height);
    }

    public static List<(string Key, string Current, string Imported)> Compute(
        Optimizer.State current,
        Optimizer.State imported)
    {
        var list = new List<(string, string, string)>();
        var curMap = StateMapper.ToMap(current);
        var impMap = StateMapper.ToMap(imported);
        foreach (var kv in impMap.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            if (!curMap.TryGetValue(kv.Key, out var cur) || cur == kv.Value) continue;
            list.Add((kv.Key, BoolText(cur), BoolText(kv.Value)));
        }

        var curExtra = StateMapper.ToExtra(current)
            .Where(e => string.Equals(e.Kind, "int", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(e => e.Key!, e => e.Text ?? "", StringComparer.Ordinal);
        foreach (var e in StateMapper.ToExtra(imported)
                     .Where(x => string.Equals(x.Kind, "int", StringComparison.OrdinalIgnoreCase))
                     .OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            if (string.IsNullOrEmpty(e.Key)) continue;
            curExtra.TryGetValue(e.Key!, out var curText);
            curText ??= "";
            var impText = e.Text ?? "";
            if (string.Equals(curText, impText, StringComparison.Ordinal)) continue;
            list.Add((e.Key!, curText, impText));
        }

        return list;
    }

    static string BoolText(bool v) => v ? AppLang.L("开", "On") : AppLang.L("关", "Off");
}
