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
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(520, 380);
        BackColor = AppTheme.SurfaceCard;
        Font = new Font("Microsoft YaHei UI", 9F);
        ShowInTaskbar = false;

        _items = CustomSoftwareStore.Load();

        var hint = new Label
        {
            Text = "填写显示名称与 winget 包 ID（如 Google.Chrome）。不确定 ID 时可在终端运行 winget search 软件名。",
            Location = new Point(16, 12),
            Size = new Size(488, 36),
            ForeColor = AppTheme.TextMute,
        };

        _list.View = View.Details;
        _list.FullRowSelect = true;
        _list.HideSelection = false;
        _list.MultiSelect = false;
        _list.BorderStyle = BorderStyle.FixedSingle;
        _list.Location = new Point(16, 52);
        _list.Size = new Size(488, 240);
        _list.Columns.Add("软件名称", 200);
        _list.Columns.Add("Winget ID / 名称", 260);
        UiBuffer.Enable(_list);

        var add = ThemedSettingsChrome.CreateButton("添加", true);
        add.Size = new Size(80, 30);
        add.Location = new Point(16, 308);
        add.Click += (_, _) => AddItem();

        var edit = ThemedSettingsChrome.CreateButton("编辑", false);
        edit.Size = new Size(80, 30);
        edit.Location = new Point(104, 308);
        edit.Click += (_, _) => EditSelected();

        var remove = ThemedSettingsChrome.CreateButton("删除", false);
        remove.Size = new Size(80, 30);
        remove.Location = new Point(192, 308);
        remove.Click += (_, _) => RemoveSelected();

        var close = ThemedSettingsChrome.CreateButton("关闭", false);
        close.Size = new Size(80, 30);
        close.Location = new Point(424, 308);
        close.DialogResult = DialogResult.OK;
        AcceptButton = close;
        CancelButton = close;

        Controls.AddRange([hint, _list, add, edit, remove, close]);
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
            ClientSize = new Size(440, 168),
            MaximizeBox = false,
            MinimizeBox = false,
            Font = Font,
            BackColor = AppTheme.SurfaceCard,
            ShowInTaskbar = false,
        };
        AppBrand.ApplyWindowIcon(dlg);

        var nameLabel = new Label { Text = "软件名称", Location = new Point(16, 20), AutoSize = true };
        var nameBox = new TextBox
        {
            Location = new Point(100, 16),
            Size = new Size(320, 24),
            Text = existing?.Title ?? "",
        };
        var idLabel = new Label { Text = "Winget ID", Location = new Point(16, 56), AutoSize = true };
        var idBox = new TextBox
        {
            Location = new Point(100, 52),
            Size = new Size(320, 24),
            Text = existing?.WingetId ?? "",
        };
        var tip = new Label
        {
            Text = "例：Google.Chrome 或 NetEase.MailMaster",
            Location = new Point(100, 82),
            AutoSize = true,
            ForeColor = AppTheme.TextMute,
        };
        var ok = ThemedSettingsChrome.CreateButton("确定", true);
        ok.Size = new Size(88, 30);
        ok.Location = new Point(240, 120);
        ok.DialogResult = DialogResult.OK;
        var cancel = ThemedSettingsChrome.CreateButton("取消", false);
        cancel.Size = new Size(88, 30);
        cancel.Location = new Point(336, 120);
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
