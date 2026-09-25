namespace SrvDesk;

internal sealed class QuickToolsDialog : Form
{
    private readonly SystemFacts _facts;
    private readonly ListView _list = new();
    private readonly TextBox _search = new();
    private readonly Label _count = new();
    private List<QuickTool> _tools = [];
    private readonly ListViewStickySelection<QuickTool> _sticky;

    public QuickToolsDialog(SystemFacts facts)
    {
        _facts = facts;
        _sticky = new ListViewStickySelection<QuickTool>(_list);
        Text = AppLang.L("快速工具", "Quick tools");
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = UiScale.Size(860, 620);
        MinimumSize = UiScale.Size(720, 520);

        var body = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(UiScale.S(12), UiScale.S(8), UiScale.S(12), UiScale.S(8)),
            BackColor = AppTheme.Surface,
        };

        var toolH = Math.Max(UiScale.S(40), UiFit.ControlHeight() + UiScale.S(12));
        var toolbar = new Panel { Dock = DockStyle.Top, Height = toolH, BackColor = AppTheme.Surface };
        var searchLbl = new Label
        {
            Text = AppLang.L("搜索", "Search"),
            AutoSize = true,
            ForeColor = AppTheme.TextHeader,
            Font = UiFit.UiFont,
        };
        toolbar.Controls.Add(searchLbl);
        _search.BorderStyle = BorderStyle.FixedSingle;
        _search.ForeColor = AppTheme.TextMain;
        _search.Font = UiFit.UiFont;
        _search.TextChanged += (_, _) => ApplyFilter();
        toolbar.Controls.Add(_search);
        _count.AutoSize = true;
        _count.ForeColor = AppTheme.TextMute;
        _count.Font = UiFit.UiFont;
        toolbar.Controls.Add(_count);
        void LayoutToolbar()
        {
            var h = UiFit.ControlHeight(_search.Font);
            searchLbl.Location = new Point(0, Math.Max(0, (toolbar.Height - searchLbl.PreferredHeight) / 2));
            _search.SetBounds(searchLbl.Right + UiScale.S(8), Math.Max(4, (toolbar.Height - h) / 2),
                Math.Max(UiScale.S(220), toolbar.ClientSize.Width / 2), h);
            _count.Location = new Point(_search.Right + UiScale.S(12),
                Math.Max(0, (toolbar.Height - _count.PreferredHeight) / 2));
        }
        toolbar.Resize += (_, _) => LayoutToolbar();

        _list.View = View.Details;
        _list.FullRowSelect = true;
        _list.GridLines = true;
        _list.HideSelection = false;
        _list.MultiSelect = false;
        _list.Dock = DockStyle.Fill;
        _list.BackColor = AppTheme.SurfaceCard;
        _list.BorderStyle = BorderStyle.FixedSingle;
        UiBuffer.Enable(_list);
        _list.Columns.Add(AppLang.L("分类", "Category"), 120);
        _list.Columns.Add(AppLang.L("工具", "Tool"), 220);
        _list.Columns.Add(AppLang.L("说明", "Description"), 360);
        _list.DoubleClick += (_, _) => OpenSelected();

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = UiFit.ControlHeight() + UiScale.S(16),
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            AutoScroll = false,
            Padding = new Padding(0, UiScale.S(8), 0, 0),
        };
        UiBuffer.ConfigureNoScrollRow(actions);
        var openBtn = ThemedSettingsChrome.CreateButton(AppLang.L("打开", "Open"), true);
        UiFit.FitButton(openBtn, padding: 28);
        _sticky.BindToolbarButton(openBtn, OpenSelected);
        actions.Controls.Add(openBtn);

        body.Controls.Add(_list);
        body.Controls.Add(actions);
        body.Controls.Add(toolbar);

        ThemedSettingsChrome.MountModal(
            this,
            AppLang.L("快速工具", "Quick tools"),
            "",
            body,
            "",
            showHeader: false);

        UiBuffer.BindListViewColumnFit(_list, 2, 120);
        Load += (_, _) =>
        {
            LayoutToolbar();
            _tools = QuickToolsLauncher.GetAvailableTools(_facts).ToList();
            ReloadList(_tools);
        };
        body.Resize += (_, _) => UiBuffer.FitListViewColumn(_list, 2, 120);
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

    private void OpenSelected()
    {
        var tool = _sticky.Get().FirstOrDefault();
        if (tool is null) return;
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
