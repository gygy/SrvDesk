namespace SrvDesk;

/// <summary>
/// 「我的方案」：跨模块收藏的优化项；勾选 / 排序 / 一键应用 / 还原 / 状态。
/// </summary>
internal sealed class UserPlanDialog : Form, IEmbeddedSettingsPage
{
    private readonly MainForm _main;
    private readonly ComboBox _plans = new();
    private readonly ListView _items = new();
    private readonly Label _driftBanner = new();
    private readonly ToolTip _tip = new();
    private readonly Button _btnNew;
    private readonly Button _btnRename;
    private readonly Button _btnDelete;
    private readonly Button _btnApply;
    private readonly Button _btnRestore;
    private readonly Button _btnReapplyDrift;

    private UserPlanIndex _index = new();
    private UserPlanDetail? _current;
    private bool _suppressPackSelect;
    private bool _suppressCheck;
    private Optimizer.State? _system;

    public UserPlanDialog(MainForm main)
    {
        _main = main;
        Text = AppLang.L("我的方案", "My plans");
        AppBrand.ApplyWindowIcon(this);
        AutoScaleMode = AutoScaleMode.None;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = UiScale.Size(960, 600);
        MinimumSize = UiScale.Size(820, 520);
        ForeColor = AppTheme.TextMain;
        KeyPreview = true;

        _btnNew = CompactBtn(AppLang.L("新建", "New"), AppLang.L("新建方案", "New plan"), NewPlan);
        _btnRename = CompactBtn(AppLang.L("重命名", "Rename"), AppLang.L("重命名当前方案", "Rename current plan"), RenamePlan);
        _btnDelete = CompactBtn(AppLang.L("删除", "Delete"), AppLang.L("删除当前方案", "Delete current plan"), DeletePlan);
        _btnApply = CompactBtn(AppLang.L("一键应用", "Apply"), AppLang.L("按方案写入系统", "Write plan to system"), () => RunApply(restore: false));
        _btnRestore = CompactBtn(AppLang.L("一键还原", "Restore"), AppLang.L("将勾选项恢复为未优化", "Reset checked items to not optimized"), () => RunApply(restore: true));
        _btnReapplyDrift = CompactBtn(
            AppLang.L("重新应用被改回的项", "Re-apply changed items"),
            AppLang.L("只写入与方案不一致的项", "Write only items that differ from the plan"),
            ReapplyDrift);

        var body = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Surface,
            Padding = new Padding(12, 10, 12, 8),
        };

        _driftBanner.AutoSize = false;
        _driftBanner.Height = UiScale.S(32);
        _driftBanner.Dock = DockStyle.Top;
        _driftBanner.TextAlign = ContentAlignment.MiddleLeft;
        _driftBanner.Padding = new Padding(10, 0, 10, 0);
        _driftBanner.ForeColor = AppTheme.PrimaryDeep;
        _driftBanner.BackColor = AppTheme.PrimaryPale;
        _driftBanner.Visible = false;

        var top = BuildTopBar();
        top.Dock = DockStyle.Top;
        var tools = BuildItemTools();
        tools.Dock = DockStyle.Top;

        _items.View = View.Details;
        _items.FullRowSelect = true;
        _items.GridLines = false;
        _items.HideSelection = false;
        _items.CheckBoxes = true;
        _items.MultiSelect = true;
        _items.BorderStyle = BorderStyle.FixedSingle;
        _items.Dock = DockStyle.Fill;
        _items.BackColor = AppTheme.SurfaceCard;
        _items.ForeColor = AppTheme.TextMain;
        _items.Font = UiFit.UiFont;
        UiBuffer.Enable(_items);
        _items.Columns.Add(AppLang.L("优化项", "Item"), 280);
        _items.Columns.Add(AppLang.L("模块", "Module"), 120);
        _items.Columns.Add(AppLang.L("目标", "Target"), 120);
        _items.Columns.Add(AppLang.L("当前", "Current"), 120);
        _items.Columns.Add(AppLang.L("状态", "Status"), 100);
        _items.ItemChecked += OnItemChecked;
        _items.KeyDown += OnItemsKeyDown;

        body.Controls.Add(_items);
        body.Controls.Add(tools);
        body.Controls.Add(top);
        body.Controls.Add(_driftBanner);

        ThemedSettingsChrome.MountEmbedded(
            this,
            AppLang.L("我的方案", "My plans"),
            "",
            body,
            "");

        UiBuffer.BindListViewColumnFit(_items, 0, 160);
        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.F5)
            {
                RefreshFromSystem();
                e.Handled = true;
            }
        };
        Shown += (_, _) =>
        {
            if (_index.Plans.Count == 0)
                RefreshFromSystem();
        };
        Resize += (_, _) => LayoutColumns();
    }

    public bool SupportsApplyToSystem => true;
    public bool ConsumeWarmLoadSkip() => false;

    public void ApplyToSystem() => RunApply(restore: false);

    public void RefreshFromSystem()
    {
        try { _system = Optimizer.Read(fullScan: false); }
        catch { _system = null; }

        _index = UserPlanStore.LoadIndex();
        ReloadPlanList(selectId: _index.LastPlanId);
        _main.RefreshFavoriteStars();
    }

    private Panel BuildTopBar()
    {
        var btnH = UiFit.ControlHeight();
        var bar = new FlowLayoutPanel
        {
            Height = btnH + 14,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = false,
            BackColor = AppTheme.Surface,
            Padding = new Padding(0, 2, 0, 2),
        };
        UiBuffer.ConfigureNoScrollRow(bar);

        var label = new Label
        {
            Text = AppLang.L("方案", "Plan"),
            AutoSize = true,
            ForeColor = AppTheme.TextHeader,
            Margin = new Padding(0, (btnH - UiFit.LineHeight(UiFit.UiFont)) / 2 + 2, UiScale.S(8), 0),
            TextAlign = ContentAlignment.MiddleLeft,
        };

        _plans.DropDownStyle = ComboBoxStyle.DropDownList;
        _plans.Font = UiFit.UiFont;
        UiFit.FitCombo(_plans);
        _plans.Width = UiScale.S(220);
        _plans.Margin = new Padding(0, Math.Max(0, (btnH - _plans.Height) / 2), UiScale.S(8), 0);
        _plans.SelectedIndexChanged += (_, _) => OnPlanSelected();

        void PlaceBtn(Button b, int extraLeft = 0)
        {
            b.Margin = new Padding(extraLeft, Math.Max(0, (btnH - b.Height) / 2), UiScale.S(6), 0);
        }

        PlaceBtn(_btnNew);
        PlaceBtn(_btnRename);
        PlaceBtn(_btnDelete);
        PlaceBtn(_btnApply, UiScale.S(10));
        PlaceBtn(_btnRestore);

        bar.Controls.Add(label);
        bar.Controls.Add(_plans);
        bar.Controls.Add(_btnNew);
        bar.Controls.Add(_btnRename);
        bar.Controls.Add(_btnDelete);
        bar.Controls.Add(_btnApply);
        bar.Controls.Add(_btnRestore);
        return bar;
    }

    private FlowLayoutPanel BuildItemTools()
    {
        var btnH = UiFit.ControlHeight();
        var tools = new FlowLayoutPanel
        {
            Height = btnH + 14,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 2, 0, 6),
        };
        UiBuffer.ConfigureNoScrollRow(tools);

        tools.Controls.Add(ToolButton(AppLang.L("上移", "Up"), AppLang.L("上移选中项", "Move up"), () => MoveSelected(-1)));
        tools.Controls.Add(ToolButton(AppLang.L("下移", "Down"), AppLang.L("下移选中项", "Move down"), () => MoveSelected(1)));
        tools.Controls.Add(ToolButton(AppLang.L("移除", "Remove"), AppLang.L("从方案中移除", "Remove from plan"), RemoveSelected));
        tools.Controls.Add(ToolButton(AppLang.L("导出…", "Export…"), AppLang.L("导出当前方案", "Export current plan"), ExportPlan));
        tools.Controls.Add(ToolButton(AppLang.L("导入…", "Import…"), AppLang.L("导入方案文件", "Import plan file"), ImportPlans));
        _btnReapplyDrift.Margin = new Padding(12, 0, 0, 0);
        tools.Controls.Add(_btnReapplyDrift);
        return tools;
    }

    private Button CompactBtn(string text, string tip, Action click)
    {
        var b = ThemedSettingsChrome.CreateButton(text, false);
        UiFit.FitButton(b, UiFit.ControlHeight(), padding: 20);
        _tip.SetToolTip(b, tip);
        b.Click += (_, _) => click();
        return b;
    }

    private Control ToolButton(string text, string tip, Action click)
    {
        var b = CompactBtn(text, tip, click);
        b.Margin = new Padding(0, 0, 6, 0);
        return b;
    }

    private void ReloadPlanList(string? selectId)
    {
        _suppressPackSelect = true;
        try
        {
            _plans.Items.Clear();
            foreach (var s in _index.Plans)
                _plans.Items.Add(s);

            if (_plans.Items.Count == 0)
            {
                _current = null;
                _items.Items.Clear();
                UpdateDriftBanner(0);
                return;
            }

            var idx = 0;
            if (!string.IsNullOrWhiteSpace(selectId))
            {
                for (var i = 0; i < _index.Plans.Count; i++)
                {
                    if (string.Equals(_index.Plans[i].Id, selectId, StringComparison.OrdinalIgnoreCase))
                    {
                        idx = i;
                        break;
                    }
                }
            }
            _plans.SelectedIndex = idx;
        }
        finally
        {
            _suppressPackSelect = false;
        }
        OnPlanSelected();
    }

    private void OnPlanSelected()
    {
        if (_suppressPackSelect) return;
        if (_plans.SelectedItem is not UserPlanSummary summary)
        {
            _current = null;
            _items.Items.Clear();
            UpdateDriftBanner(0);
            return;
        }

        UserPlanStore.SetLastPlanId(summary.Id);
        _current = UserPlanStore.LoadPlan(summary.Id);
        ReloadItems();
    }

    private void ReloadItems()
    {
        _suppressCheck = true;
        try
        {
            _items.BeginUpdate();
            _items.Items.Clear();
            if (_current?.Items is null)
            {
                UpdateDriftBanner(0);
                return;
            }

            var drift = 0;
            foreach (var item in _current.Items)
            {
                var currentLabel = "—";
                var status = AppLang.L("未知", "Unknown");
                var drifted = false;
                if (_system is not null)
                {
                    var curIdx = UserPlanState.ReadChoiceIndex(_system, item.Key, item.OptimizedIndex);
                    currentLabel = FormatValueLabel(item, curIdx);
                    drifted = curIdx != item.Value;
                    status = drifted
                        ? AppLang.L("被改回", "Changed back")
                        : AppLang.L("已生效", "In effect");
                    if (item.Enabled && drifted) drift++;
                }

                var lv = new ListViewItem(item.Title) { Tag = item, Checked = item.Enabled };
                if (drifted && item.Enabled)
                    lv.ForeColor = Color.FromArgb(180, 90, 0);
                lv.SubItems.Add(item.Module);
                lv.SubItems.Add(string.IsNullOrEmpty(item.DesiredLabel)
                    ? FormatValueLabel(item, item.Value)
                    : item.DesiredLabel);
                lv.SubItems.Add(currentLabel);
                lv.SubItems.Add(status);
                _items.Items.Add(lv);
            }
            UpdateDriftBanner(drift);
        }
        finally
        {
            _items.EndUpdate();
            _suppressCheck = false;
            LayoutColumns();
        }
    }

    private static string FormatValueLabel(UserPlanItem item, int index)
    {
        if (!string.IsNullOrEmpty(item.DesiredLabel) && index == item.Value)
            return item.DesiredLabel;
        if (index == item.OptimizedIndex)
            return AppLang.L("开启", "On");
        return AppLang.L("关闭", "Off");
    }

    private void UpdateDriftBanner(int drift)
    {
        if (drift <= 0)
        {
            _driftBanner.Visible = false;
            _btnReapplyDrift.Enabled = false;
            return;
        }

        _driftBanner.Text = AppLang.Lf(
            "有 {0} 项与方案不一致，可重新应用。",
            "{0} item(s) differ from this plan — you can re-apply them.",
            drift);
        _tip.SetToolTip(_driftBanner, AppLang.L(
            "系统更新或其他软件可能改回这些设置。",
            "Windows Update or other software may have changed these settings."));
        _driftBanner.Visible = true;
        _btnReapplyDrift.Enabled = true;
    }

    private void LayoutColumns()
    {
        if (_items.Columns.Count < 5 || _items.ClientSize.Width < 100) return;
        var w = _items.ClientSize.Width - 4;
        _items.Columns[0].Width = Math.Max(160, (int)(w * 0.32));
        _items.Columns[1].Width = Math.Max(90, (int)(w * 0.16));
        _items.Columns[2].Width = Math.Max(90, (int)(w * 0.16));
        _items.Columns[3].Width = Math.Max(90, (int)(w * 0.16));
        _items.Columns[4].Width = Math.Max(80, w
            - _items.Columns[0].Width
            - _items.Columns[1].Width
            - _items.Columns[2].Width
            - _items.Columns[3].Width);
    }

    private void OnItemChecked(object? sender, ItemCheckedEventArgs e)
    {
        if (_suppressCheck || _current is null || e.Item.Tag is not UserPlanItem item) return;
        if (item.Enabled == e.Item.Checked) return;
        UserPlanStore.SetItemEnabled(_current, item.Key, e.Item.Checked);
        item.Enabled = e.Item.Checked;
        ReloadItems();
    }

    private void OnItemsKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Delete)
        {
            RemoveSelected();
            e.Handled = true;
        }
    }

    private void NewPlan()
    {
        var name = PromptText(
            AppLang.L("新建方案", "New plan"),
            AppLang.L("方案名称：", "Plan name:"),
            AppLang.L("办公", "Office"));
        if (name is null) return;
        var plan = UserPlanStore.CreatePlan(name);
        _index = UserPlanStore.LoadIndex();
        ReloadPlanList(plan.Id);
        _main.RefreshFavoriteStars();
    }

    private void RenamePlan()
    {
        if (_current is null)
        {
            MessageBox.Show(this,
                AppLang.L("请先选择一个方案。", "Select a plan first."),
                Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var name = PromptText(
            AppLang.L("重命名方案", "Rename plan"),
            AppLang.L("新名称：", "New name:"),
            _current.Name);
        if (name is null) return;
        UserPlanStore.RenamePlan(_current.Id, name);
        _index = UserPlanStore.LoadIndex();
        ReloadPlanList(_current.Id);
    }

    private void DeletePlan()
    {
        if (_current is null)
        {
            MessageBox.Show(this,
                AppLang.L("请先选择一个方案。", "Select a plan first."),
                Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var answer = MessageBox.Show(
            this,
            AppLang.Lf("确定删除方案「{0}」？\r\n只删方案列表，不改系统设置。",
                "Delete plan “{0}”?\r\nOnly the plan list is removed; system settings stay unchanged.",
                _current.Name),
            AppLang.L("删除方案", "Delete plan"),
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);
        if (answer != DialogResult.Yes) return;
        UserPlanStore.DeletePlan(_current.Id);
        _index = UserPlanStore.LoadIndex();
        ReloadPlanList(_index.LastPlanId);
        _main.RefreshFavoriteStars();
    }

    private void MoveSelected(int delta)
    {
        if (_current is null) return;
        var key = SelectedKey();
        if (key is null) return;
        UserPlanStore.MoveItem(_current, key, delta);
        _current = UserPlanStore.LoadPlan(_current.Id);
        ReloadItems();
        SelectKey(key);
    }

    private void RemoveSelected()
    {
        if (_current is null) return;
        var keys = _items.SelectedItems.Cast<ListViewItem>()
            .Select(x => x.Tag as UserPlanItem)
            .Where(x => x is not null)
            .Select(x => x!.Key)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (keys.Count == 0)
        {
            MessageBox.Show(this,
                AppLang.L("请先选中要移除的项。", "Select items to remove."),
                Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        foreach (var k in keys)
            UserPlanStore.RemoveItem(_current, k);
        _current = UserPlanStore.LoadPlan(_current.Id);
        ReloadItems();
        _main.RefreshFavoriteStars();
    }

    private string? SelectedKey() =>
        _items.SelectedItems.Count > 0 && _items.SelectedItems[0].Tag is UserPlanItem it
            ? it.Key
            : null;

    private void SelectKey(string key)
    {
        foreach (ListViewItem lv in _items.Items)
        {
            if (lv.Tag is UserPlanItem it && string.Equals(it.Key, key, StringComparison.Ordinal))
            {
                lv.Selected = true;
                lv.EnsureVisible();
                break;
            }
        }
    }

    private void RunApply(bool restore)
    {
        if (_current?.Items is null || _current.Items.Count == 0)
        {
            MessageBox.Show(this,
                AppLang.L("当前方案还没有项。请到各分类里点星标收藏。",
                    "This plan is empty. Star items in the setting tabs to add them."),
                Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var enabled = _current.Items.Where(x => x.Enabled).ToList();
        if (enabled.Count == 0)
        {
            MessageBox.Show(this,
                AppLang.L("请先勾选要写入的项。", "Check items to write first."),
                Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show(
            this,
            restore
                ? AppLang.Lf("将把 {0} 项恢复为未优化并写入系统，是否继续？",
                    "Restore {0} item(s) to not optimized and write to the system?", enabled.Count)
                : AppLang.Lf("将按方案写入 {0} 项到系统，是否继续？",
                    "Write {0} plan item(s) to the system?", enabled.Count),
            restore ? AppLang.L("一键还原", "Restore") : AppLang.L("一键应用", "Apply"),
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button1);
        if (confirm != DialogResult.Yes) return;

        var (ok, attempted, errors) = _main.ApplyUserPlanItems(enabled, restore, askRestorePoint: true);
        RefreshFromSystem();
        if (!ok) return;

        var msg = AppLang.Lf("已写入 {0} 项。", "Wrote {0} item(s).", attempted);
        if (errors.Count > 0)
            msg += "\r\n\r\n" + AppLang.Lf("其中 {0} 项失败（已跳过）：\r\n", "{0} failed (skipped):\r\n", errors.Count)
                   + string.Join("\r\n", errors.Take(12));
        MessageBox.Show(this, msg, Text, MessageBoxButtons.OK,
            errors.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
    }

    private void ReapplyDrift()
    {
        if (_current?.Items is null || _system is null) return;
        var drifted = _current.Items
            .Where(x => x.Enabled && !UserPlanState.Matches(_system, x))
            .ToList();
        if (drifted.Count == 0)
        {
            MessageBox.Show(this,
                AppLang.L("没有与方案不一致的项。", "No items differ from the plan."),
                Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show(
            this,
            AppLang.Lf("将重新应用 {0} 个被改回的项，是否继续？",
                "Re-apply {0} item(s) that were changed back?", drifted.Count),
            AppLang.L("重新应用被改回的项", "Re-apply changed items"),
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (confirm != DialogResult.Yes) return;

        var (ok, attempted, errors) = _main.ApplyUserPlanItems(drifted, restore: false, askRestorePoint: true);
        RefreshFromSystem();
        if (!ok) return;

        var msg = AppLang.Lf("已重新应用 {0} 项。", "Re-applied {0} item(s).", attempted);
        if (errors.Count > 0)
            msg += "\r\n\r\n" + string.Join("\r\n", errors.Take(12));
        MessageBox.Show(this, msg, Text, MessageBoxButtons.OK,
            errors.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
    }

    private void ExportPlan()
    {
        if (_current is null)
        {
            MessageBox.Show(this,
                AppLang.L("请先选择一个方案。", "Select a plan first."),
                Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        using var dlg = new SaveFileDialog
        {
            Filter = AppLang.L("方案文件 (*.json)|*.json", "Plan file (*.json)|*.json"),
            FileName = "SrvDesk-Plan-" + SanitizeFileName(_current.Name) + ".json",
            Title = AppLang.L("导出我的方案", "Export my plan"),
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            UserPlanStore.ExportToFile(dlg.FileName, _current.Id);
            MessageBox.Show(this,
                AppLang.L("已导出。", "Exported."),
                Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ImportPlans()
    {
        using var dlg = new OpenFileDialog
        {
            Filter = AppLang.L("方案文件 (*.json)|*.json", "Plan file (*.json)|*.json"),
            Title = AppLang.L("导入我的方案", "Import my plans"),
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        var mergeAsk = MessageBox.Show(
            this,
            AppLang.L(
                "合并到现有方案，还是清空后只保留导入内容？\r\n\r\n是 = 合并\r\n否 = 清空后导入\r\n取消 = 不导入",
                "Merge with existing plans, or replace them?\r\n\r\nYes = merge\r\nNo = replace\r\nCancel = don’t import"),
            AppLang.L("导入我的方案", "Import my plans"),
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Question);
        if (mergeAsk == DialogResult.Cancel) return;

        try
        {
            var n = UserPlanStore.ImportFromFile(dlg.FileName, merge: mergeAsk == DialogResult.Yes);
            _index = UserPlanStore.LoadIndex();
            ReloadPlanList(_index.LastPlanId);
            _main.RefreshFavoriteStars();
            MessageBox.Show(this,
                AppLang.Lf("已导入 {0} 个方案。", "Imported {0} plan(s).", n),
                Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = name.Select(c => invalid.Contains(c) ? '-' : c).ToArray();
        var s = new string(chars).Trim();
        return s.Length == 0 ? "plan" : s;
    }

    private static string? PromptText(string title, string label, string initial)
    {
        using var dlg = new Form
        {
            Text = title,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            ClientSize = UiScale.Size(420, 140),
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            Font = UiFit.UiFont,
        };
        AppBrand.ApplyWindowIcon(dlg);

        var lbl = new Label
        {
            Text = label,
            Location = new Point(16, 16),
            AutoSize = true,
        };
        var box = new TextBox
        {
            Text = initial,
            Location = new Point(16, 44),
            Width = 388,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };
        var ok = ThemedSettingsChrome.CreateButton(AppLang.L("确定", "OK"), true);
        ok.DialogResult = DialogResult.OK;
        UiFit.FitButton(ok, UiFit.ControlHeight(), padding: 24);
        ok.Location = new Point(216, 90);
        var cancel = ThemedSettingsChrome.CreateButton(AppLang.L("取消", "Cancel"), false);
        cancel.DialogResult = DialogResult.Cancel;
        UiFit.FitButton(cancel, UiFit.ControlHeight(), padding: 24);
        cancel.Location = new Point(312, 90);
        dlg.Controls.AddRange([lbl, box, ok, cancel]);
        dlg.AcceptButton = ok;
        dlg.CancelButton = cancel;
        box.SelectAll();
        if (dlg.ShowDialog() != DialogResult.OK) return null;
        var text = box.Text.Trim();
        return text.Length == 0 ? null : text;
    }
}
