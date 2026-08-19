namespace WinOpt;

internal sealed class QuickToolsDialog : Form
{
    private readonly SystemFacts _facts;
    private readonly ListView _list = new();
    private readonly TextBox _search = new();
    private readonly Label _desc = new();
    private List<QuickTool> _tools = [];

    public QuickToolsDialog(SystemFacts facts)
    {
        _facts = facts;
        Text = "快速打开工具";
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(620, 520);
        MinimumSize = new Size(520, 420);
        Font = new Font("Microsoft YaHei UI", 9F);
        BackColor = AppTheme.SurfaceCard;

        var tip = new Label
        {
            Text = "双击或点「打开」启动系统管理工具。列表已按 Server 桌面场景筛选可用项。",
            Location = new Point(16, 12),
            Size = new Size(588, 36),
            ForeColor = AppTheme.TextMute,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };

        var searchLabel = new Label
        {
            Text = "搜索",
            Location = new Point(16, 54),
            AutoSize = true,
            ForeColor = AppTheme.TextHeader,
        };
        _search.SetBounds(52, 50, 300, 26);
        _search.BorderStyle = BorderStyle.FixedSingle;
        _search.ForeColor = AppTheme.TextMain;
        _search.TextChanged += (_, _) => ApplyFilter();

        var countLabel = new Label
        {
            Name = "countLabel",
            Location = new Point(360, 54),
            AutoSize = true,
            ForeColor = AppTheme.TextMute,
        };

        _list.View = View.Details;
        _list.FullRowSelect = true;
        _list.GridLines = true;
        _list.HideSelection = false;
        _list.MultiSelect = false;
        _list.Location = new Point(16, 84);
        _list.Size = new Size(588, 340);
        _list.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _list.Columns.Add("分类", 108);
        _list.Columns.Add("工具", 200);
        _list.Columns.Add("说明", 260);
        _list.DoubleClick += (_, _) => OpenSelected();
        _list.SelectedIndexChanged += (_, _) => UpdateDescription();

        _desc.SetBounds(16, 432, 420, 40);
        _desc.ForeColor = AppTheme.TextMute;
        _desc.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        var openBtn = new Button
        {
            Text = "打开",
            Size = new Size(88, 34),
            FlatStyle = FlatStyle.Flat,
            BackColor = AppTheme.Primary,
            ForeColor = AppTheme.TextOnPrimary,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
        };
        openBtn.FlatAppearance.BorderSize = 0;
        openBtn.Click += (_, _) => OpenSelected();

        var closeBtn = new Button
        {
            Text = "关闭",
            DialogResult = DialogResult.Cancel,
            Size = new Size(80, 34),
            FlatStyle = FlatStyle.Flat,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
        };

        Load += (_, _) =>
        {
            openBtn.Location = new Point(ClientSize.Width - 192, ClientSize.Height - 48);
            closeBtn.Location = new Point(ClientSize.Width - 96, ClientSize.Height - 48);
        };
        Resize += (_, _) =>
        {
            openBtn.Location = new Point(ClientSize.Width - 192, ClientSize.Height - 48);
            closeBtn.Location = new Point(ClientSize.Width - 96, ClientSize.Height - 48);
            if (_list.Columns.Count >= 3)
                _list.Columns[2].Width = Math.Max(120, _list.ClientSize.Width - _list.Columns[0].Width - _list.Columns[1].Width - 4);
        };

        CancelButton = closeBtn;
        Controls.AddRange([tip, searchLabel, _search, countLabel, _list, _desc, openBtn, closeBtn]);

        _tools = QuickToolsLauncher.GetAvailableTools(_facts).ToList();
        ReloadList(_tools);
        UpdateCountLabel(countLabel);
    }

    private void ReloadList(IEnumerable<QuickTool> tools)
    {
        _list.BeginUpdate();
        _list.Items.Clear();
        foreach (var t in tools)
        {
            var item = new ListViewItem(t.Category);
            item.SubItems.Add(t.Title);
            item.SubItems.Add(t.Description);
            item.Tag = t;
            _list.Items.Add(item);
        }
        _list.EndUpdate();
        if (_list.Items.Count > 0) _list.Items[0].Selected = true;
        UpdateDescription();
    }

    private void ApplyFilter()
    {
        var q = _search.Text.Trim();
        if (q.Length == 0)
        {
            ReloadList(_tools);
            return;
        }

        var filtered = _tools.Where(t =>
            t.Title.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
            t.Category.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
            t.Description.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        ReloadList(filtered);
    }

    private void UpdateDescription()
    {
        if (_list.SelectedItems.Count == 0)
        {
            _desc.Text = "";
            return;
        }

        if (_list.SelectedItems[0].Tag is QuickTool t)
            _desc.Text = t.Description;
    }

    private void UpdateCountLabel(Label label) =>
        label.Text = $"共 {_tools.Count} 项可用";

    private void OpenSelected()
    {
        if (_list.SelectedItems.Count == 0) return;
        if (_list.SelectedItems[0].Tag is not QuickTool tool) return;
        QuickToolsLauncher.Launch(tool, this);
    }
}
