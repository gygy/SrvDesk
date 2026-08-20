namespace WinOpt;

internal sealed class StartupManagerDialog : Form
{
    private readonly ListView _list = new();
    private readonly TextBox _search = new();
    private readonly Label _detail = new();
    private readonly Label _count = new();
    private readonly ListBox _filter = new();
    private int _filterHover = -1;
    private List<StartupEntry> _items = [];

    private static readonly string[] Filters = ["全部", "当前用户", "所有用户", "已禁用"];

    public StartupManagerDialog()
    {
        Text = "启动项管理";
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(920, 580);
        MinimumSize = new Size(760, 480);
        Font = new Font("Microsoft YaHei UI", 9F);
        BackColor = AppTheme.Surface;
        ForeColor = AppTheme.TextMain;

        var header = ThemedSettingsChrome.CreateHeader("启动项管理", "登录时自动运行的程序 · 注册表 Run 与启动文件夹");
        var footer = ThemedSettingsChrome.CreateFooter(this, "禁用使用系统 StartupApproved，不删除条目。删除不可恢复。", RefreshList);

        var body = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Surface };
        var sidebar = BuildSidebar();
        sidebar.Dock = DockStyle.Left;

        var main = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 10, 12, 8) };
        var tools = BuildToolStrip();
        tools.Dock = DockStyle.Top;

        _list.View = View.Details;
        _list.FullRowSelect = true;
        _list.GridLines = false;
        _list.HideSelection = false;
        _list.MultiSelect = false;
        _list.BorderStyle = BorderStyle.FixedSingle;
        _list.Dock = DockStyle.Fill;
        _list.BackColor = AppTheme.SurfaceCard;
        _list.ForeColor = AppTheme.TextMain;
        _list.Columns.Add("名称", 180);
        _list.Columns.Add("状态", 70);
        _list.Columns.Add("范围", 110);
        _list.Columns.Add("类型", 110);
        _list.Columns.Add("命令", 360);
        _list.SelectedIndexChanged += (_, _) => UpdateDetail();
        _list.DoubleClick += (_, _) => ToggleSelected();

        _detail.Dock = DockStyle.Bottom;
        _detail.Height = 44;
        _detail.ForeColor = AppTheme.TextMute;
        _detail.Padding = new Padding(0, 6, 0, 0);

        main.Controls.Add(_list);
        main.Controls.Add(_detail);
        main.Controls.Add(tools);

        body.Controls.Add(main);
        body.Controls.Add(sidebar);

        Controls.Add(body);
        Controls.Add(footer);
        Controls.Add(header);

        _filter.SelectedIndex = 0;
        Load += (_, _) => RefreshList();
        Resize += (_, _) =>
        {
            if (_list.Columns.Count >= 5)
                _list.Columns[4].Width = Math.Max(180, _list.ClientSize.Width - 480);
        };
    }

    private Panel BuildSidebar()
    {
        var sidebar = new Panel { Width = 150, BackColor = AppTheme.NavBg };
        var cap = new Label
        {
            Text = "  筛选",
            Dock = DockStyle.Top,
            Height = 36,
            ForeColor = AppTheme.TextMute,
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = AppTheme.NavBg,
        };
        _filter.Dock = DockStyle.Fill;
        _filter.BorderStyle = BorderStyle.None;
        _filter.BackColor = AppTheme.NavBg;
        _filter.ForeColor = AppTheme.TextMain;
        _filter.IntegralHeight = false;
        _filter.DrawMode = DrawMode.OwnerDrawFixed;
        _filter.ItemHeight = 40;
        _filter.Items.AddRange(Filters);
        _filter.DrawItem += (_, e) =>
        {
            if (e.Index < 0) return;
            var selected = (e.State & DrawItemState.Selected) != 0;
            var hover = e.Index == _filterHover;
            using var bg = new SolidBrush(selected ? AppTheme.Primary : hover ? AppTheme.NavHover : AppTheme.NavBg);
            e.Graphics.FillRectangle(bg, e.Bounds);
            TextRenderer.DrawText(e.Graphics, Filters[e.Index],
                selected ? new Font(Font, FontStyle.Bold) : Font,
                new Rectangle(e.Bounds.X + 16, e.Bounds.Y, e.Bounds.Width - 20, e.Bounds.Height),
                selected ? AppTheme.TextOnPrimary : AppTheme.TextMain,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        };
        _filter.MouseMove += (_, e) =>
        {
            var i = _filter.IndexFromPoint(e.Location);
            if (i != _filterHover) { _filterHover = i; _filter.Invalidate(); }
        };
        _filter.MouseLeave += (_, _) => { _filterHover = -1; _filter.Invalidate(); };
        _filter.SelectedIndexChanged += (_, _) => ApplyFilter();
        sidebar.Controls.Add(_filter);
        sidebar.Controls.Add(cap);
        return sidebar;
    }

    private Panel BuildToolStrip()
    {
        var bar = new Panel { Height = 40, BackColor = AppTheme.Surface };
        var searchLabel = new Label
        {
            Text = "搜索",
            Location = new Point(0, 10),
            AutoSize = true,
            ForeColor = AppTheme.TextHeader,
        };
        _search.SetBounds(40, 6, 220, 26);
        _search.BorderStyle = BorderStyle.FixedSingle;
        _search.TextChanged += (_, _) => ApplyFilter();

        _count.Location = new Point(270, 10);
        _count.AutoSize = true;
        _count.ForeColor = AppTheme.TextMute;

        var x = 430;
        bar.Controls.Add(searchLabel);
        bar.Controls.Add(_search);
        bar.Controls.Add(_count);
        bar.Controls.Add(ToolBtn("启用", () => SetSelected(true), x));
        bar.Controls.Add(ToolBtn("禁用", () => SetSelected(false), x + 76));
        bar.Controls.Add(ToolBtn("删除", DeleteSelected, x + 152));
        bar.Controls.Add(ToolBtn("添加", AddItem, x + 228));
        bar.Controls.Add(ToolBtn("打开位置", OpenSelected, x + 304));
        bar.Resize += (_, _) =>
        {
            var right = bar.Width - 8;
            foreach (Control c in bar.Controls)
            {
                if (c is Button b && b.Tag is int offset)
                    b.Location = new Point(Math.Max(400, right - 380 + offset - 430), 4);
            }
        };
        return bar;
    }

    private Button ToolBtn(string text, Action click, int x)
    {
        var b = ThemedSettingsChrome.CreateButton(text, false);
        b.Size = new Size(72, 30);
        b.Location = new Point(x, 4);
        b.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        b.Tag = x;
        b.Click += (_, _) => click();
        return b;
    }

    private void RefreshList()
    {
        try
        {
            _items = StartupItemHelper.ListAll().ToList();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "启动项管理", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ApplyFilter()
    {
        var q = _search.Text.Trim();
        var cat = _filter.SelectedItem as string ?? "全部";
        _list.BeginUpdate();
        _list.Items.Clear();
        var shown = 0;
        foreach (var item in _items)
        {
            if (cat == "当前用户" && !item.IsHkcu) continue;
            if (cat == "所有用户" && item.IsHkcu) continue;
            if (cat == "已禁用" && item.Enabled) continue;
            if (q.Length > 0
                && item.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0
                && item.Command.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            var row = new ListViewItem(item.Name) { Tag = item };
            row.SubItems.Add(item.Enabled ? "启用" : "禁用");
            row.SubItems.Add(item.Scope);
            row.SubItems.Add(item.KindText);
            row.SubItems.Add(item.Command);
            if (!item.Enabled) row.ForeColor = AppTheme.TextMute;
            _list.Items.Add(row);
            shown++;
        }
        _list.EndUpdate();
        _count.Text = $"共 {_items.Count} 项，显示 {shown} 项";
        UpdateDetail();
    }

    private StartupEntry? Selected() =>
        _list.SelectedItems.Count > 0 ? _list.SelectedItems[0].Tag as StartupEntry : null;

    private void UpdateDetail()
    {
        var item = Selected();
        _detail.Text = item is null
            ? "双击切换启用/禁用。添加写入当前用户 Run；系统级项需管理员。"
            : $"{item.Name}  ·  {item.Scope}  ·  {(item.Enabled ? "启用" : "禁用")}\r\n{item.Command}";
    }

    private void ToggleSelected()
    {
        var item = Selected();
        if (item is null) return;
        SetSelected(!item.Enabled);
    }

    private void SetSelected(bool enabled)
    {
        var item = Selected();
        if (item is null)
        {
            MessageBox.Show(this, "请先选择一项。", "启动项管理", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        try
        {
            StartupItemHelper.SetEnabled(item, enabled);
            RefreshList();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "启动项管理", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void DeleteSelected()
    {
        var item = Selected();
        if (item is null) return;
        if (MessageBox.Show(this,
                $"确定删除启动项「{item.Name}」？\r\n\r\n{item.Command}",
                "删除启动项", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;
        try
        {
            StartupItemHelper.Delete(item);
            RefreshList();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "删除失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void AddItem()
    {
        using var dlg = new Form
        {
            Text = "添加启动项",
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(480, 168),
            MaximizeBox = false,
            MinimizeBox = false,
            Font = Font,
            BackColor = AppTheme.SurfaceCard,
        };
        AppBrand.ApplyWindowIcon(dlg);
        var nameLabel = new Label { Text = "名称", Location = new Point(16, 18), AutoSize = true };
        var nameBox = new TextBox { Location = new Point(80, 14), Size = new Size(380, 24) };
        var cmdLabel = new Label { Text = "命令", Location = new Point(16, 54), AutoSize = true };
        var cmdBox = new TextBox { Location = new Point(80, 50), Size = new Size(300, 24) };
        var browse = ThemedSettingsChrome.CreateButton("浏览...", false);
        browse.Size = new Size(72, 26);
        browse.Location = new Point(388, 49);
        browse.Click += (_, _) =>
        {
            using var ofd = new OpenFileDialog { Filter = "程序|*.exe;*.bat;*.cmd;*.lnk|所有文件|*.*" };
            if (ofd.ShowDialog(dlg) == DialogResult.OK)
            {
                cmdBox.Text = "\"" + ofd.FileName + "\"";
                if (nameBox.Text.Length == 0)
                    nameBox.Text = Path.GetFileNameWithoutExtension(ofd.FileName);
            }
        };
        var hint = new Label
        {
            Text = "写入当前用户注册表 Run，登录后自动运行。",
            Location = new Point(80, 82),
            AutoSize = true,
            ForeColor = AppTheme.TextMute,
        };
        var ok = ThemedSettingsChrome.CreateButton("添加", true);
        ok.Size = new Size(88, 32);
        ok.Location = new Point(280, 118);
        ok.Click += (_, _) =>
        {
            try
            {
                StartupItemHelper.AddUserRun(nameBox.Text, cmdBox.Text);
                dlg.DialogResult = DialogResult.OK;
            }
            catch (Exception ex)
            {
                MessageBox.Show(dlg, ex.Message, "添加启动项", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };
        var cancel = ThemedSettingsChrome.CreateButton("取消", false);
        cancel.Size = new Size(88, 32);
        cancel.Location = new Point(376, 118);
        cancel.DialogResult = DialogResult.Cancel;
        dlg.AcceptButton = ok;
        dlg.CancelButton = cancel;
        dlg.Controls.AddRange([nameLabel, nameBox, cmdLabel, cmdBox, browse, hint, ok, cancel]);
        if (dlg.ShowDialog(this) == DialogResult.OK)
            RefreshList();
    }

    private void OpenSelected()
    {
        var item = Selected();
        try
        {
            if (item is null) StartupItemHelper.OpenUserStartupFolder();
            else StartupItemHelper.OpenLocation(item);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "打开位置", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
