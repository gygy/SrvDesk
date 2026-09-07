namespace SrvDesk;

/// <summary>展示可更新软件列表，支持勾选后批量更新。</summary>
internal sealed class SoftwareUpdateDialog : Form
{
    private readonly ListView _list = new();
    private readonly Label _summary = new();

    public List<SoftwareUpdateInfo> SelectedUpdates { get; private set; } = [];

    public SoftwareUpdateDialog(IReadOnlyList<SoftwareUpdateInfo> updates)
    {
        Text = "软件更新";
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(720, 420);
        MinimumSize = new Size(560, 320);

        var body = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 8, 16, 56),
            BackColor = AppTheme.Surface,
        };

        _summary.Dock = DockStyle.Top;
        _summary.Height = 36;
        _summary.ForeColor = AppTheme.TextMute;
        _summary.Text = updates.Count > 0
            ? $"共 {updates.Count} 款软件有新版本。勾选后点「更新所选」即可批量升级。"
            : "当前常用软件列表中没有检测到可更新项。";

        _list.Dock = DockStyle.Fill;
        _list.View = View.Details;
        UiBuffer.Enable(_list);
        _list.FullRowSelect = true;
        _list.CheckBoxes = true;
        _list.GridLines = true;
        _list.HideSelection = false;
        _list.Font = new Font("Microsoft YaHei UI", 9F);
        _list.Columns.Add("软件名称", 260);
        _list.Columns.Add("当前版本", 120);
        _list.Columns.Add("新版本", 120);
        _list.Columns.Add("包 ID", 180);

        foreach (var u in updates)
        {
            var row = new ListViewItem(u.Item.Title) { Checked = true, Tag = u };
            row.SubItems.Add(u.CurrentVersion);
            row.SubItems.Add(u.AvailableVersion);
            row.SubItems.Add(u.Item.WingetId);
            _list.Items.Add(row);
        }

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 6, 0, 0),
            BackColor = AppTheme.Surface,
        };

        var upgrade = ThemedSettingsChrome.CreateButton("更新所选", true);
        upgrade.Margin = new Padding(6, 0, 0, 0);
        upgrade.Enabled = updates.Count > 0;
        upgrade.Click += (_, _) => ConfirmUpgrade();

        var selectAll = ThemedSettingsChrome.CreateButton("全选", false);
        selectAll.Margin = new Padding(6, 0, 0, 0);
        selectAll.Enabled = updates.Count > 0;
        selectAll.Click += (_, _) => SetAll(true);

        var clear = ThemedSettingsChrome.CreateButton("全不选", false);
        clear.Margin = new Padding(6, 0, 0, 0);
        clear.Enabled = updates.Count > 0;
        clear.Click += (_, _) => SetAll(false);

        var close = ThemedSettingsChrome.CreateButton("关闭", false);
        close.Margin = new Padding(6, 0, 0, 0);
        close.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        buttons.Controls.Add(upgrade);
        buttons.Controls.Add(selectAll);
        buttons.Controls.Add(clear);
        buttons.Controls.Add(close);

        body.Controls.Add(_list);
        body.Controls.Add(_summary);
        body.Controls.Add(buttons);

        ThemedSettingsChrome.MountModal(
            this,
            "软件更新",
            "显示当前版本与新版本 · 可批量更新",
            body,
            "仅列出「常用软件」目录内、且 winget 报告有更新的已安装软件。");
        UiBuffer.BindListViewColumnFit(_list, 3, 120);
    }

    private void SetAll(bool on)
    {
        foreach (ListViewItem item in _list.Items)
            item.Checked = on;
    }

    private void ConfirmUpgrade()
    {
        SelectedUpdates = _list.Items.Cast<ListViewItem>()
            .Where(i => i.Checked && i.Tag is SoftwareUpdateInfo)
            .Select(i => (SoftwareUpdateInfo)i.Tag!)
            .ToList();
        if (SelectedUpdates.Count == 0)
        {
            MessageBox.Show(this, "请先勾选要更新的软件。", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var names = string.Join("\r\n", SelectedUpdates.Select(u =>
            $"· {u.Item.Title}  {u.CurrentVersion} → {u.AvailableVersion}"));
        var answer = MessageBox.Show(this,
            $"将更新以下 {SelectedUpdates.Count} 款软件：\r\n\r\n{names}\r\n\r\n是否继续？",
            "确认批量更新",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button1);
        if (answer != DialogResult.Yes) return;

        DialogResult = DialogResult.OK;
        Close();
    }
}
