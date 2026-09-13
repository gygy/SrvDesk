namespace SrvDesk;

/// <summary>管理用户自定义常用软件（填写名称 + winget ID，用 winget 安装）。</summary>
internal sealed class CustomSoftwareManageDialog : Form
{
    private readonly ListView _list = new();
    private readonly List<CustomSoftwareStore.CustomSoftwareEntry> _items;

    public CustomSoftwareManageDialog()
    {
        Text = "自定义软件";
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = UiScale.Size(820, 520);
        MinimumSize = UiScale.Size(780, 480);
        BackColor = AppTheme.SurfaceCard;
        Font = UiFit.UiFont;
        ShowInTaskbar = false;

        _items = CustomSoftwareStore.Load();

        var hint = new Label
        {
            Text = "名称 + winget 包 ID（如 Google.Chrome）",
            Dock = DockStyle.Top,
            Height = Math.Max(UiScale.S(28), UiFit.ControlHeight(UiFit.UiFontSmall)),
            Padding = new Padding(UiScale.S(16), UiScale.S(8), UiScale.S(16), UiScale.S(4)),
            ForeColor = AppTheme.TextMute,
            Font = UiFit.UiFontSmall,
        };

        _list.View = View.Details;
        _list.FullRowSelect = true;
        _list.HideSelection = false;
        _list.MultiSelect = false;
        _list.BorderStyle = BorderStyle.FixedSingle;
        _list.Dock = DockStyle.Fill;
        _list.Columns.Add("软件名称", 220);
        _list.Columns.Add("Winget ID / 名称", 300);
        UiBuffer.Enable(_list);

        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = UiFit.ControlHeight() + UiScale.S(20),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = false,
            Padding = new Padding(UiScale.S(16), UiScale.S(8), UiScale.S(16), UiScale.S(8)),
        };
        UiBuffer.ConfigureNoScrollRow(bar);

        var add = ThemedSettingsChrome.CreateButton("添加", true);
        UiFit.FitButton(add, padding: 28);
        add.Click += (_, _) => AddItem();

        var edit = ThemedSettingsChrome.CreateButton("编辑", false);
        UiFit.FitButton(edit, padding: 28);
        edit.Margin = new Padding(UiScale.S(8), 0, 0, 0);
        edit.Click += (_, _) => EditSelected();

        var remove = ThemedSettingsChrome.CreateButton("删除", false);
        UiFit.FitButton(remove, padding: 28);
        remove.Margin = new Padding(UiScale.S(8), 0, 0, 0);
        remove.Click += (_, _) => RemoveSelected();

        var close = ThemedSettingsChrome.CreateButton("关闭", false);
        UiFit.FitButton(close, padding: 28);
        close.Margin = new Padding(UiScale.S(16), 0, 0, 0);
        close.DialogResult = DialogResult.OK;
        AcceptButton = close;
        CancelButton = close;

        bar.Controls.AddRange([add, edit, remove, close]);
        Controls.Add(_list);
        Controls.Add(bar);
        Controls.Add(hint);
        UiBuffer.BindListViewColumnFit(_list, 1, 160);
        ReloadList();
    }

    public bool Changed { get; private set; }

    private void ReloadList()
    {
        _list.BeginUpdate();
        _list.Items.Clear();
        foreach (var e in _items)
        {
            var row = new ListViewItem(e.Title) { Tag = e };
            row.SubItems.Add(e.WingetId);
            _list.Items.Add(row);
        }
        _list.EndUpdate();
    }

    private void AddItem()
    {
        if (!PromptEdit(null, out var title, out var wingetId)) return;
        try
        {
            CustomSoftwareStore.Add(title, wingetId);
            _items.Clear();
            _items.AddRange(CustomSoftwareStore.Load());
            ReloadList();
            Changed = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "自定义软件", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void EditSelected()
    {
        if (_list.SelectedItems.Count == 0)
        {
            MessageBox.Show(this, "请先选择一项。", "自定义软件", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var entry = (CustomSoftwareStore.CustomSoftwareEntry)_list.SelectedItems[0].Tag!;
        if (!PromptEdit(entry, out var title, out var wingetId)) return;
        try
        {
            CustomSoftwareStore.Update(entry.Id, title, wingetId);
            _items.Clear();
            _items.AddRange(CustomSoftwareStore.Load());
            ReloadList();
            Changed = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "自定义软件", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void RemoveSelected()
    {
        if (_list.SelectedItems.Count == 0) return;
        var entry = (CustomSoftwareStore.CustomSoftwareEntry)_list.SelectedItems[0].Tag!;
        if (MessageBox.Show(this, $"删除自定义项「{entry.Title}」？", "自定义软件",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;
        CustomSoftwareStore.Remove(entry.Id);
        _items.Clear();
        _items.AddRange(CustomSoftwareStore.Load());
        ReloadList();
        Changed = true;
    }

    private bool PromptEdit(CustomSoftwareStore.CustomSoftwareEntry? existing, out string title, out string wingetId)
    {
        title = "";
        wingetId = "";
        using var dlg = new Form
        {
            Text = existing is null ? "添加自定义软件" : "编辑自定义软件",
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            ClientSize = UiScale.Size(520, 220),
            MaximizeBox = false,
            MinimizeBox = false,
            Font = Font,
            BackColor = AppTheme.SurfaceCard,
            ShowInTaskbar = false,
        };
        AppBrand.ApplyWindowIcon(dlg);

        var nameLabel = new Label { Text = "软件名称", Location = new Point(UiScale.S(16), UiScale.S(20)), AutoSize = true };
        var nameBox = new TextBox
        {
            Location = new Point(UiScale.S(110), UiScale.S(16)),
            Size = new Size(UiScale.S(360), UiFit.ControlHeight()),
            Text = existing?.Title ?? "",
            Font = UiFit.UiFont,
        };
        var idLabel = new Label { Text = "Winget ID", Location = new Point(UiScale.S(16), UiScale.S(64)), AutoSize = true };
        var idBox = new TextBox
        {
            Location = new Point(UiScale.S(110), UiScale.S(60)),
            Size = new Size(UiScale.S(360), UiFit.ControlHeight()),
            Text = existing?.WingetId ?? "",
            Font = UiFit.UiFont,
        };
        var tip = new Label
        {
            Text = "例：Google.Chrome 或 NetEase.MailMaster",
            Location = new Point(UiScale.S(110), UiScale.S(100)),
            AutoSize = true,
            ForeColor = AppTheme.TextMute,
        };
        var ok = ThemedSettingsChrome.CreateButton("确定", true);
        UiFit.FitButton(ok, padding: 28);
        ok.Location = new Point(UiScale.S(280), UiScale.S(140));
        ok.DialogResult = DialogResult.OK;
        var cancel = ThemedSettingsChrome.CreateButton("取消", false);
        UiFit.FitButton(cancel, padding: 28);
        cancel.Location = new Point(UiScale.S(380), UiScale.S(140));
        cancel.DialogResult = DialogResult.Cancel;
        dlg.AcceptButton = ok;
        dlg.CancelButton = cancel;
        dlg.Controls.AddRange([nameLabel, nameBox, idLabel, idBox, tip, ok, cancel]);

        if (dlg.ShowDialog(this) != DialogResult.OK)
            return false;
        title = nameBox.Text.Trim();
        wingetId = idBox.Text.Trim();
        return true;
    }
}
