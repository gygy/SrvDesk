namespace WinOpt;

internal sealed class QuickToolsDialog : Form
{
    private readonly SystemFacts _facts;
    private readonly ListView _list = new();
    private readonly TextBox _search = new();
    private readonly Label _desc = new();
    private readonly Label _count = new();
    private List<QuickTool> _tools = [];

    public QuickToolsDialog(SystemFacts facts)
    {
        _facts = facts;
        Text = "快速工具";
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(640, 520);
        MinimumSize = new Size(520, 420);

        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 8, 12, 8), BackColor = AppTheme.Surface };

        var toolbar = new Panel { Dock = DockStyle.Top, Height = 36, BackColor = AppTheme.Surface };
        toolbar.Controls.Add(new Label
        {
            Text = "搜索",
            Location = new Point(0, 8),
            AutoSize = true,
            ForeColor = AppTheme.TextHeader,
        });
        _search.SetBounds(44, 4, 280, 26);
        _search.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        _search.BorderStyle = BorderStyle.FixedSingle;
        _search.ForeColor = AppTheme.TextMain;
        _search.TextChanged += (_, _) => ApplyFilter();
        toolbar.Controls.Add(_search);
        _count.AutoSize = true;
        _count.Location = new Point(336, 8);
        _count.ForeColor = AppTheme.TextMute;
        toolbar.Controls.Add(_count);

        _list.View = View.Details;
        _list.FullRowSelect = true;
        _list.GridLines = true;
        _list.HideSelection = false;
        _list.MultiSelect = false;
        _list.Dock = DockStyle.Fill;
        _list.BackColor = AppTheme.SurfaceCard;
        _list.BorderStyle = BorderStyle.FixedSingle;
        _list.Columns.Add("分类", 108);
        _list.Columns.Add("工具", 200);
        _list.Columns.Add("说明", 260);
        _list.DoubleClick += (_, _) => OpenSelected();
        _list.SelectedIndexChanged += (_, _) => UpdateDescription();

        _desc.Dock = DockStyle.Bottom;
        _desc.Height = 40;
        _desc.ForeColor = AppTheme.TextMute;
        _desc.Padding = new Padding(0, 6, 0, 0);

        var openBtn = ThemedSettingsChrome.CreateButton("打开", true);
        openBtn.Size = new Size(88, 34);
        openBtn.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        openBtn.Click += (_, _) => OpenSelected();

        body.Controls.Add(_list);
        body.Controls.Add(_desc);
        body.Controls.Add(toolbar);
        body.Controls.Add(openBtn);

        ThemedSettingsChrome.MountModal(
            this,
            "快速工具",
            "系统管理工具快捷入口 · 已按 Server 桌面场景筛选",
            body,
            "双击列表项或点「打开」启动。");

        Load += (_, _) =>
        {
            openBtn.Location = new Point(body.ClientSize.Width - openBtn.Width - 4, body.ClientSize.Height - openBtn.Height - 4);
            _tools = QuickToolsLauncher.GetAvailableTools(_facts).ToList();
            ReloadList(_tools);
        };
        body.Resize += (_, _) =>
        {
            openBtn.Location = new Point(body.ClientSize.Width - openBtn.Width - 4, body.ClientSize.Height - openBtn.Height - 4);
            if (_list.Columns.Count >= 3)
                _list.Columns[2].Width = Math.Max(120, _list.ClientSize.Width - _list.Columns[0].Width - _list.Columns[1].Width - 4);
        };
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
        _count.Text = $"当前 {_list.Items.Count} 项 / 共 {_tools.Count} 项";
        UpdateDescription();
    }

    private void ApplyFilter()
    {
        var q = _search.Text.Trim();
        ReloadList(q.Length == 0
            ? _tools
            : _tools.Where(t =>
                t.Title.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
                t.Category.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
                t.Description.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0));
    }

    private void UpdateDescription()
    {
        _desc.Text = _list.SelectedItems.Count > 0 && _list.SelectedItems[0].Tag is QuickTool t
            ? t.Description
            : "";
    }

    private void OpenSelected()
    {
        if (_list.SelectedItems.Count == 0) return;
        if (_list.SelectedItems[0].Tag is QuickTool tool)
            QuickToolsLauncher.Launch(tool, this);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Enter && _list.Focused)
        {
            OpenSelected();
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }
}
