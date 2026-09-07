namespace SrvDesk;

internal sealed class StartupManagerDialog : Form, IEmbeddedSettingsPage
{
    private readonly ListView _list = new();
    private readonly TextBox _search = new();
    private readonly Label _detail = new();
    private readonly Label _count = new();
    private readonly ComboBox _filter = new();
    private List<StartupEntry> _items = [];

    private static readonly string[] Filters = ["全部", "当前用户", "所有用户", "已禁用"];

    public StartupManagerDialog()
    {
        Text = "登录启动项";
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(920, 580);
        MinimumSize = new Size(760, 480);
        ForeColor = AppTheme.TextMain;

        var body = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Surface, Padding = new Padding(12, 10, 12, 8) };
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
        // 用官方扩展样式双缓冲，避免反射 DoubleBuffered 导致表头右侧残影/文字被压扁
        _list.HandleCreated += (_, _) => UiBuffer.EnableListView(_list);

        body.Controls.Add(_list);
        body.Controls.Add(tools);

        ThemedSettingsChrome.MountEmbedded(
            this,
            "登录启动项",
            "登录时自动运行 · 注册表 Run 与启动文件夹",
            body,
            "禁用使用 StartupApproved，不删除条目。删除不可恢复。",
            RefreshList);

        _filter.SelectedIndex = 0;
        Shown += (_, _) =>
        {
            if (_items.Count == 0) RefreshList();
        };
        UiBuffer.BindListViewColumnFit(_list, 4, 180);
    }

    public void RefreshFromSystem()
    {
        RefreshList();
        _warmLoadSkip = true;
    }

    private bool _warmLoadSkip;

    public bool ConsumeWarmLoadSkip()
    {
        if (!_warmLoadSkip) return false;
        _warmLoadSkip = false;
        return true;
    }

    public bool SupportsApplyToSystem => false;
    public void ApplyToSystem() { }

    private Panel BuildToolStrip()
    {
        var bar = new Panel { Height = 64, BackColor = AppTheme.Surface };

        var filterLabel = new Label
        {
            Text = "筛选",
            Location = new Point(0, 8),
            AutoSize = true,
            ForeColor = AppTheme.TextHeader,
        };
        _filter.DropDownStyle = ComboBoxStyle.DropDownList;
        _filter.SetBounds(40, 4, 110, 26);
        _filter.Items.AddRange(Filters);
        _filter.SelectedIndexChanged += (_, _) => ApplyFilter();

        var searchLabel = new Label
        {
            Text = "搜索",
            Location = new Point(164, 8),
            AutoSize = true,
            ForeColor = AppTheme.TextHeader,
        };
        _search.SetBounds(204, 4, 200, 26);
        _search.BorderStyle = BorderStyle.FixedSingle;
        _search.TextChanged += (_, _) => ApplyFilter();

        _count.Location = new Point(416, 8);
        _count.AutoSize = true;
        _count.ForeColor = AppTheme.TextMute;

        _detail.AutoSize = false;
        _detail.AutoEllipsis = true;
        _detail.ForeColor = AppTheme.TextMute;
        _detail.TextAlign = ContentAlignment.MiddleLeft;
        _detail.Text = "双击切换启用/禁用。添加写入当前用户 Run；系统级项需管理员。";
        _detail.SetBounds(0, 34, 400, 26);

        bar.Controls.Add(filterLabel);
        bar.Controls.Add(_filter);
        bar.Controls.Add(searchLabel);
        bar.Controls.Add(_search);
        bar.Controls.Add(_count);
        bar.Controls.Add(_detail);

        var buttons = new[]
        {
            ToolBtn("启用", () => SetSelected(true)),
            ToolBtn("禁用", () => SetSelected(false)),
            ToolBtn("删除", DeleteSelected),
            ToolBtn("添加", AddItem),
            ToolBtn("打开位置", OpenSelected),
        };
        foreach (var b in buttons)
            bar.Controls.Add(b);

        void LayoutTools()
        {
            var x = bar.ClientSize.Width - 8;
            for (var i = buttons.Length - 1; i >= 0; i--)
            {
                var b = buttons[i];
                x -= b.Width;
                b.Location = new Point(Math.Max(8, x), 2);
                x -= 8;
            }
            var detailRight = buttons[0].Left - 12;
            _detail.SetBounds(0, 34, Math.Max(80, detailRight), 26);
            // 统计文字避开右侧按钮组
            _count.Visible = _count.Right <= buttons[0].Left - 8 || bar.ClientSize.Width > 780;
        }

        bar.Resize += (_, _) => LayoutTools();
        LayoutTools();
        return bar;
    }

    private Button ToolBtn(string text, Action click)
    {
        var b = ThemedSettingsChrome.CreateButton(text, false);
        b.Height = 28;
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
            : $"{item.Name}  ·  {item.Scope}  ·  {(item.Enabled ? "启用" : "禁用")}  ·  {item.Command}";
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
        browse.Height = 26;
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
