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
        // 嵌入主窗后按字体自动缩放会打乱手工坐标，导致按钮盖住说明
        AutoScaleMode = AutoScaleMode.None;
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
        _list.Columns.Add("名称", 160);
        _list.Columns.Add("状态", 60);
        _list.Columns.Add(AppLang.L("建议", "Advice"), 90);
        _list.Columns.Add("范围", 100);
        _list.Columns.Add("类型", 100);
        _list.Columns.Add("命令", 320);
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
        UiBuffer.BindListViewColumnFit(_list, 5, 180);
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
        // 两行：上行筛选/按钮，下行说明；说明绝不与按钮重叠
        var btnH = UiScale.S(30);
        var row1 = btnH + UiScale.S(10);
        var bar = new Panel { Height = row1 + UiScale.S(28), BackColor = AppTheme.Surface };

        var filterLabel = new Label
        {
            Text = "筛选",
            AutoSize = true,
            ForeColor = AppTheme.TextHeader,
        };
        _filter.DropDownStyle = ComboBoxStyle.DropDownList;
        _filter.Items.AddRange(Filters);
        _filter.SelectedIndexChanged += (_, _) => ApplyFilter();

        var searchLabel = new Label
        {
            Text = "搜索",
            AutoSize = true,
            ForeColor = AppTheme.TextHeader,
        };
        _search.BorderStyle = BorderStyle.FixedSingle;
        _search.TextChanged += (_, _) => ApplyFilter();

        _count.AutoSize = true;
        _count.ForeColor = AppTheme.TextMute;

        _detail.AutoSize = false;
        _detail.AutoEllipsis = true;
        _detail.ForeColor = AppTheme.TextMute;
        _detail.TextAlign = ContentAlignment.MiddleLeft;
        _detail.Text = "双击切换启用/禁用。添加写入当前用户 Run；系统级项需管理员。";

        bar.Controls.Add(filterLabel);
        bar.Controls.Add(_filter);
        bar.Controls.Add(searchLabel);
        bar.Controls.Add(_search);
        bar.Controls.Add(_count);
        bar.Controls.Add(_detail);

        var buttons = new[]
        {
            ToolBtn("启用", () => SetSelected(true), btnH),
            ToolBtn("禁用", () => SetSelected(false), btnH),
            ToolBtn("删除", DeleteSelected, btnH),
            ToolBtn("添加", AddItem, btnH),
            ToolBtn("打开位置", OpenSelected, btnH),
        };
        foreach (var b in buttons)
            bar.Controls.Add(b);

        void LayoutTools()
        {
            var gap = UiScale.S(6);
            var pad = UiScale.S(4);
            foreach (var b in buttons)
                UiFit.FitButton(b, btnH, minWidth: 64, padding: 16);

            var x = bar.ClientSize.Width - pad;
            for (var i = buttons.Length - 1; i >= 0; i--)
            {
                var b = buttons[i];
                x -= b.Width;
                b.Location = new Point(Math.Max(pad, x), UiScale.S(4));
                x -= gap;
            }

            var btnLeft = buttons[0].Left;
            var y1 = UiScale.S(8);
            filterLabel.Location = new Point(0, y1);
            _filter.SetBounds(filterLabel.Right + UiScale.S(4), UiScale.S(4), UiScale.S(110), UiScale.S(26));
            searchLabel.Location = new Point(_filter.Right + UiScale.S(12), y1);

            var countW = _count.PreferredSize.Width;
            var countVisible = btnLeft - countW - UiScale.S(16) > searchLabel.Right + UiScale.S(100);
            _count.Visible = countVisible;
            if (countVisible)
                _count.Location = new Point(btnLeft - countW - UiScale.S(12), y1);

            var searchRight = countVisible ? _count.Left - UiScale.S(12) : btnLeft - UiScale.S(12);
            var searchW = Math.Max(UiScale.S(80), searchRight - (searchLabel.Right + UiScale.S(4)));
            _search.SetBounds(searchLabel.Right + UiScale.S(4), UiScale.S(4), searchW, UiScale.S(26));

            // 说明独占第二行，避开按钮高度
            _detail.SetBounds(0, row1, Math.Max(80, bar.ClientSize.Width - pad), UiScale.S(24));
        }

        bar.Resize += (_, _) => LayoutTools();
        LayoutTools();
        return bar;
    }

    private Button ToolBtn(string text, Action click, int height)
    {
        var b = ThemedSettingsChrome.CreateButton(text, false);
        UiFit.FitButton(b, height, minWidth: 64, padding: 16);
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
            row.SubItems.Add(StartupAdviceHelper.Tag(item));
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
