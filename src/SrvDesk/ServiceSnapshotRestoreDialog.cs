namespace SrvDesk;

/// <summary>选择服务快照并预览差异后还原。</summary>
internal sealed class ServiceSnapshotRestoreDialog : Form
{
    private readonly ListBox _snaps = new();
    private readonly ListView _diff = new();
    private readonly Label _meta = new();
    private readonly Label _hint = new();
    private readonly Button _restore = new();
    private readonly Button _export = new();
    private readonly Button _folder = new();
    private readonly Button _cancel = new();
    private readonly Button _refresh = new();
    private List<ServiceSnapshotInfo> _list = [];
    private ServiceSnapshotFile? _current;

    public ServiceSnapshotRestoreDialog()
    {
        Text = AppLang.L("从备份还原服务", "Restore services from backup");
        AppBrand.ApplyWindowIcon(this);
        AutoScaleMode = AutoScaleMode.None;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        MaximizeBox = true;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = UiScale.Size(860, 520);
        MinimumSize = UiScale.Size(720, 420);
        Font = UiFit.UiFont;
        BackColor = AppTheme.Surface;
        ForeColor = AppTheme.TextMain;

        var left = new Panel
        {
            Dock = DockStyle.Left,
            Width = UiScale.S(280),
            Padding = UiScale.Pad(8),
            BackColor = AppTheme.Surface,
        };
        var leftTop = new Panel { Dock = DockStyle.Top, Height = UiScale.S(36) };
        var leftTitle = new Label
        {
            Text = AppLang.L("备份列表", "Backups"),
            AutoSize = true,
            Font = UiFit.UiFontBold(),
            ForeColor = AppTheme.Primary,
            Location = new Point(0, UiScale.S(8)),
        };
        _refresh.Text = AppLang.L("刷新", "Refresh");
        UiFit.FitButton(_refresh, UiScale.S(28), minWidth: 56, padding: 12);
        _refresh.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _refresh.Location = new Point(left.Width - UiScale.S(24) - _refresh.Width, UiScale.S(4));
        _refresh.Click += (_, _) => ReloadList();
        leftTop.Controls.Add(leftTitle);
        leftTop.Controls.Add(_refresh);
        left.Resize += (_, _) =>
        {
            _refresh.Left = left.ClientSize.Width - left.Padding.Right - _refresh.Width - UiScale.S(8);
        };

        _snaps.Dock = DockStyle.Fill;
        _snaps.IntegralHeight = false;
        _snaps.BorderStyle = BorderStyle.FixedSingle;
        _snaps.SelectedIndexChanged += (_, _) => OnSnapSelected();
        left.Controls.Add(_snaps);
        left.Controls.Add(leftTop);

        var right = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = UiScale.Pad(8, 8, 8, 8),
            BackColor = AppTheme.Surface,
        };

        _meta.Dock = DockStyle.Top;
        _meta.Height = UiScale.S(22);
        _meta.ForeColor = AppTheme.TextMute;
        _meta.Text = AppLang.L("选择左侧备份以查看与当前的差异。", "Select a backup on the left to see diffs.");

        _hint.Dock = DockStyle.Top;
        _hint.Height = UiScale.S(22);
        _hint.ForeColor = AppTheme.TextMute;
        _hint.Text = AppLang.L("勾选要还原的项（默认同差异且可还原）。仅改启动类型。",
            "Check items to restore (defaults: differing & restorable). Start type only.");

        _diff.Dock = DockStyle.Fill;
        _diff.View = View.Details;
        _diff.FullRowSelect = true;
        _diff.CheckBoxes = true;
        _diff.GridLines = false;
        _diff.HideSelection = false;
        _diff.BorderStyle = BorderStyle.FixedSingle;
        _diff.Columns.Add(AppLang.L("服务", "Service"), UiScale.S(200));
        _diff.Columns.Add(AppLang.L("显示名", "Display"), UiScale.S(220));
        _diff.Columns.Add(AppLang.L("备份", "Backup"), UiScale.S(72));
        _diff.Columns.Add(AppLang.L("当前", "Current"), UiScale.S(72));
        _diff.Columns.Add(AppLang.L("说明", "Note"), UiScale.S(120));
        UiBuffer.Enable(_diff);
        UiBuffer.BindListViewColumnFit(_diff, 1, 160);

        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = UiScale.S(48),
            Padding = UiScale.Pad(0, 8, 0, 0),
        };
        _restore.Text = AppLang.L("还原勾选", "Restore checked");
        _export.Text = AppLang.L("导出副本…", "Export copy…");
        _folder.Text = AppLang.L("打开目录", "Open folder");
        _cancel.Text = AppLang.L("关闭", "Close");
        foreach (var b in new[] { _restore, _export, _folder, _cancel })
        {
            UiFit.FitButton(b, UiScale.S(32), minWidth: 72, padding: 16);
            b.FlatStyle = FlatStyle.Flat;
            b.BackColor = AppTheme.SurfaceCard;
            b.ForeColor = AppTheme.TextMain;
            b.FlatAppearance.BorderColor = AppTheme.Border;
        }
        _restore.BackColor = AppTheme.Primary;
        _restore.ForeColor = Color.White;
        _restore.FlatAppearance.BorderSize = 0;
        _restore.Click += (_, _) => DoRestore();
        _export.Click += (_, _) => DoExport();
        _folder.Click += (_, _) =>
        {
            try { ServiceSnapshotStore.OpenSnapshotFolder(); }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };
        _cancel.Click += (_, _) => Close();

        footer.Controls.AddRange([_restore, _export, _folder, _cancel]);
        footer.Resize += (_, _) =>
        {
            var gap = UiScale.S(8);
            var x = footer.ClientSize.Width;
            foreach (var b in new[] { _cancel, _folder, _export, _restore })
            {
                x -= b.Width;
                b.Location = new Point(x, UiScale.S(8));
                x -= gap;
            }
        };

        right.Controls.Add(_diff);
        right.Controls.Add(_hint);
        right.Controls.Add(_meta);
        right.Controls.Add(footer);

        Controls.Add(right);
        Controls.Add(left);

        Shown += (_, _) => ReloadList();
    }

    private void ReloadList()
    {
        _list = ServiceSnapshotStore.ListSnapshots();
        _snaps.BeginUpdate();
        _snaps.Items.Clear();
        foreach (var s in _list)
            _snaps.Items.Add(s.ListLabel);
        _snaps.EndUpdate();

        _current = null;
        _diff.Items.Clear();
        _meta.Text = _list.Count == 0
            ? AppLang.L("暂无备份。可先点「备份」或在改服务前自动生成。",
                "No backups yet. Use Backup, or change services to auto-create one.")
            : AppLang.Lf("共 {0} 份备份（最多保留 {1}）。",
                "{0} backups (keep last {1}).", _list.Count, ServiceSnapshotStore.MaxKeep);

        if (_list.Count > 0)
            _snaps.SelectedIndex = 0;
    }

    private void OnSnapSelected()
    {
        _diff.Items.Clear();
        _current = null;
        if (_snaps.SelectedIndex < 0 || _snaps.SelectedIndex >= _list.Count)
            return;

        var info = _list[_snaps.SelectedIndex];
        var file = ServiceSnapshotStore.Load(info.Path);
        if (file is null)
        {
            _meta.Text = AppLang.L("无法读取该备份文件。", "Cannot read this backup file.");
            return;
        }

        _current = file;
        _meta.Text = AppLang.Lf("{0}  ·  {1}  ·  {2} 项  ·  {3}",
            "{0}  ·  {1}  ·  {2} items  ·  {3}",
            info.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"),
            info.ReasonLabel,
            info.ServiceCount,
            string.IsNullOrEmpty(info.Machine) ? info.Os : info.Machine + " / " + info.Os);

        var diffs = ServiceSnapshotStore.Diff(file);
        _diff.BeginUpdate();
        foreach (var d in diffs)
        {
            var note = d.MissingNow
                ? AppLang.L("本机已无此服务", "Not installed now")
                : d.CanRestore
                    ? AppLang.L("可还原", "Restorable")
                    : AppLang.L("备份类型不可还原", "Backup type not restorable");
            var row = new ListViewItem(d.Name) { Tag = d, Checked = d.CanRestore };
            row.SubItems.Add(d.DisplayName);
            row.SubItems.Add(ServiceOptimizeHelper.StartTypeLabel(d.BackupStart));
            row.SubItems.Add(d.MissingNow
                ? AppLang.L("未安装", "Missing")
                : ServiceOptimizeHelper.StartTypeLabel(d.CurrentStart));
            row.SubItems.Add(note);
            if (!d.CanRestore)
                row.ForeColor = AppTheme.TextMute;
            _diff.Items.Add(row);
        }
        _diff.EndUpdate();

        if (diffs.Count == 0)
            _hint.Text = AppLang.L("与当前完全一致，无需还原。", "Matches current state — nothing to restore.");
        else
            _hint.Text = AppLang.Lf("差异 {0} 项；已勾选可还原 {1} 项。",
                "{0} differ; {1} restorable checked.",
                diffs.Count, diffs.Count(x => x.CanRestore));
    }

    private void DoRestore()
    {
        if (_current is null)
        {
            MessageBox.Show(this,
                AppLang.L("请先选择一份备份。", "Select a backup first."),
                Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var names = new List<string>();
        foreach (ListViewItem it in _diff.Items)
        {
            if (!it.Checked || it.Tag is not ServiceSnapshotDiff d || !d.CanRestore)
                continue;
            names.Add(d.Name);
        }

        if (names.Count == 0)
        {
            MessageBox.Show(this,
                AppLang.L("请勾选至少一项可还原的差异。", "Check at least one restorable item."),
                Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (MessageBox.Show(this,
                AppLang.Lf("将把 {0} 个服务的启动类型还原为备份值。是否继续？",
                    "Restore start type of {0} service(s) from backup. Continue?",
                    names.Count),
                Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        var result = ServiceSnapshotStore.Restore(_current, names);
        OnSnapSelected();

        var msg = AppLang.Lf("还原完成：成功 {0}，跳过 {1}，失败 {2}。",
            "Restore done: applied {0}, skipped {1}, failed {2}.",
            result.Applied, result.Skipped, result.Failed);
        if (result.Errors.Count > 0)
            msg += "\r\n\r\n" + string.Join("\r\n", result.Errors.Take(8));

        MessageBox.Show(this, msg, Text,
            result.Failed > 0 ? MessageBoxButtons.OK : MessageBoxButtons.OK,
            result.Failed > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);

        if (result.Applied > 0)
            DialogResult = DialogResult.OK;
    }

    private void DoExport()
    {
        if (_snaps.SelectedIndex < 0 || _snaps.SelectedIndex >= _list.Count)
        {
            MessageBox.Show(this,
                AppLang.L("请先选择一份备份。", "Select a backup first."),
                Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var info = _list[_snaps.SelectedIndex];
        using var dlg = new SaveFileDialog
        {
            Title = AppLang.L("导出服务备份", "Export service backup"),
            Filter = AppLang.L("服务快照 (*.json)|*.json|所有文件|*.*",
                "Service snapshot (*.json)|*.json|All files|*.*"),
            FileName = Path.GetFileName(info.Path),
            OverwritePrompt = true,
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            ServiceSnapshotStore.ExportCopy(info.Path, dlg.FileName);
            MessageBox.Show(this,
                AppLang.L("已导出：", "Exported: ") + dlg.FileName,
                Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
