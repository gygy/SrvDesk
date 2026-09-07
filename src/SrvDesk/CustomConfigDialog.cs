namespace SrvDesk;

/// <summary>自定义配置：多方案；项内容在界面粘贴/编辑并保存，不依赖外部文件路径。</summary>
internal sealed class CustomConfigDialog : Form, IEmbeddedSettingsPage
{
    private readonly ComboBox _packs = new();
    private readonly ListView _items = new();
    private readonly ToolTip _tip = new();
    private readonly Button _btnNewPack;
    private readonly Button _btnRenamePack;
    private readonly Button _btnDeletePack;

    private CustomPackIndex _index = new();
    private CustomPackDetail? _current;
    private bool _suppressPackSelect;

    public CustomConfigDialog()
    {
        Text = "自定义配置";
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(960, 600);
        MinimumSize = new Size(780, 480);
        ForeColor = AppTheme.TextMain;
        KeyPreview = true;

        _btnNewPack = CompactBtn("新建", "新建方案", NewPack);
        _btnRenamePack = CompactBtn("重命名", "重命名当前方案", RenamePack);
        _btnDeletePack = CompactBtn("删除", "删除当前方案", DeletePack);

        var body = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Surface,
            Padding = new Padding(12, 10, 12, 8),
        };

        var top = BuildTopBar();
        top.Dock = DockStyle.Top;

        var tools = BuildItemTools();
        tools.Dock = DockStyle.Top;

        _items.View = View.Details;
        _items.FullRowSelect = true;
        _items.GridLines = false;
        _items.HideSelection = false;
        _items.CheckBoxes = true;
        _items.MultiSelect = true;
        _items.BorderStyle = BorderStyle.FixedSingle;
        _items.Dock = DockStyle.Fill;
        _items.BackColor = AppTheme.SurfaceCard;
        _items.ForeColor = AppTheme.TextMain;
        UiBuffer.Enable(_items);
        _items.Columns.Add("名称", 320);
        _items.Columns.Add("类型", 120);
        _items.Columns.Add("说明", 200);
        _items.ItemChecked += OnItemChecked;
        _items.DoubleClick += (_, _) => EditSelected();
        _items.KeyDown += OnItemsKeyDown;

        // 先加列表再加工具栏/顶栏（后加的 Top 在上）
        body.Controls.Add(_items);
        body.Controls.Add(tools);
        body.Controls.Add(top);

        ThemedSettingsChrome.MountEmbedded(
            this,
            "自定义配置",
            "粘贴脚本/注册表 · 保存到方案 · 界面直接运行",
            body,
            "内容保存在本机 AppData，不引用外部文件路径。运行通常需要管理员权限。");

        UiBuffer.BindListViewColumnFit(_items, 0, 180);
        KeyDown += OnFormKeyDown;
        Shown += (_, _) =>
        {
            if (_index.Packs.Count == 0)
                RefreshFromSystem();
        };
        Resize += (_, _) => LayoutColumns();
    }

    public bool SupportsApplyToSystem => false;
    public bool ConsumeWarmLoadSkip() => false;
    public void ApplyToSystem() { }

    public void RefreshFromSystem()
    {
        _index = CustomPackStore.LoadIndex();
        ReloadPackList(selectId: _index.LastPackId);
    }

    private Panel BuildTopBar()
    {
        var bar = new Panel { Height = 40, BackColor = AppTheme.Surface };

        var label = new Label
        {
            Text = "方案",
            Location = new Point(0, 10),
            AutoSize = true,
            ForeColor = AppTheme.TextHeader,
        };

        _packs.DropDownStyle = ComboBoxStyle.DropDownList;
        _packs.SetBounds(40, 6, 220, 26);
        _packs.SelectedIndexChanged += (_, _) => OnPackSelected();

        _btnNewPack.Location = new Point(272, 5);
        _btnRenamePack.Location = new Point(272 + _btnNewPack.Width + 6, 5);
        _btnDeletePack.Location = new Point(_btnRenamePack.Right + 6, 5);

        bar.Controls.Add(label);
        bar.Controls.Add(_packs);
        bar.Controls.Add(_btnNewPack);
        bar.Controls.Add(_btnRenamePack);
        bar.Controls.Add(_btnDeletePack);
        return bar;
    }

    private FlowLayoutPanel BuildItemTools()
    {
        var tools = new FlowLayoutPanel
        {
            Height = 40,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 0, 0, 6),
        };

        tools.Controls.Add(ToolButton("粘贴新建…", "粘贴或手写 .reg / CMD / PowerShell，保存到当前方案", PasteOrNew));
        tools.Controls.Add(ToolButton("编辑", "修改名称、类型与正文", EditSelected));
        tools.Controls.Add(ToolButton("运行选中", "运行当前选中的项", RunSelected));
        tools.Controls.Add(ToolButton("全部运行", "按顺序运行已勾选的项", RunAll));
        tools.Controls.Add(ToolButton("移除", "从方案中删除", RemoveSelected));
        return tools;
    }

    private Button CompactBtn(string text, string tip, Action click)
    {
        var b = ThemedSettingsChrome.CreateButton(text, false);
        b.Height = 28;
        b.AutoSize = true;
        b.MinimumSize = new Size(64, 28);
        b.Click += (_, _) => click();
        _tip.SetToolTip(b, tip);
        return b;
    }

    private Button ToolButton(string text, string tip, Action click)
    {
        var b = ThemedSettingsChrome.CreateButton(text, false);
        b.Height = 28;
        b.Margin = new Padding(0, 0, 8, 0);
        b.Click += (_, _) => click();
        _tip.SetToolTip(b, tip);
        return b;
    }

    private void ReloadPackList(string? selectId)
    {
        _suppressPackSelect = true;
        try
        {
            _packs.Items.Clear();
            foreach (var p in _index.Packs.OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase))
                _packs.Items.Add(p);

            _btnRenamePack.Enabled = _packs.Items.Count > 0;
            _btnDeletePack.Enabled = _packs.Items.Count > 0;

            if (_packs.Items.Count == 0)
            {
                _current = null;
                _items.Items.Clear();
                return;
            }

            var idx = 0;
            if (!string.IsNullOrEmpty(selectId))
            {
                for (var i = 0; i < _packs.Items.Count; i++)
                {
                    if (_packs.Items[i] is CustomPackSummary s &&
                        string.Equals(s.Id, selectId, StringComparison.OrdinalIgnoreCase))
                    {
                        idx = i;
                        break;
                    }
                }
            }
            _packs.SelectedIndex = idx;
        }
        finally
        {
            _suppressPackSelect = false;
        }
        OnPackSelected();
    }

    private void OnPackSelected()
    {
        if (_suppressPackSelect) return;
        if (_packs.SelectedItem is not CustomPackSummary summary)
        {
            _current = null;
            _items.Items.Clear();
            return;
        }

        _current = CustomPackStore.LoadPack(summary.Id);
        CustomPackStore.SetLastPackId(summary.Id);
        ReloadItems();
    }

    private void ReloadItems()
    {
        _items.BeginUpdate();
        try
        {
            _items.ItemChecked -= OnItemChecked;
            _items.Items.Clear();
            if (_current is null) return;
            foreach (var item in _current.Items)
            {
                var row = new ListViewItem(item.Name) { Tag = item, Checked = item.Enabled };
                row.SubItems.Add(item.KindLabel);
                row.SubItems.Add("已保存在方案内");
                _items.Items.Add(row);
            }
        }
        finally
        {
            _items.ItemChecked += OnItemChecked;
            _items.EndUpdate();
            LayoutColumns();
        }
    }

    private void OnItemChecked(object? sender, ItemCheckedEventArgs e)
    {
        if (_current is null || e.Item.Tag is not CustomPackItem item) return;
        if (item.Enabled == e.Item.Checked) return;
        CustomPackStore.SetItemEnabled(_current, item.Id, e.Item.Checked);
        item.Enabled = e.Item.Checked;
    }

    private void NewPack()
    {
        var name = PromptText("新建方案", "方案名称：", "我的配置");
        if (name is null) return;
        var pack = CustomPackStore.CreatePack(name);
        _index = CustomPackStore.LoadIndex();
        ReloadPackList(pack.Id);
    }

    private void RenamePack()
    {
        if (_current is null)
        {
            MessageBox.Show(this, "请先选择一个方案。", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var name = PromptText("重命名方案", "新名称：", _current.Name);
        if (name is null) return;
        CustomPackStore.RenamePack(_current.Id, name);
        _index = CustomPackStore.LoadIndex();
        ReloadPackList(_current.Id);
    }

    private void DeletePack()
    {
        if (_current is null)
        {
            MessageBox.Show(this, "请先选择一个方案。", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var answer = MessageBox.Show(
            this,
            "确定删除方案「" + _current.Name + "」？\r\n方案内已保存的脚本/注册表也会删除。",
            "删除方案",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);
        if (answer != DialogResult.Yes) return;
        CustomPackStore.DeletePack(_current.Id);
        _index = CustomPackStore.LoadIndex();
        ReloadPackList(_index.LastPackId);
    }

    private void PasteOrNew()
    {
        EnsureCurrentPack();
        if (_current is null) return;
        EditItem(null, seedContent: TryClipboardText() ?? "");
    }

    private void PasteFromClipboard()
    {
        EnsureCurrentPack();
        if (_current is null) return;
        var text = TryClipboardText();
        if (string.IsNullOrWhiteSpace(text))
            return;
        EditItem(null, seedContent: text);
    }

    private void EditSelected()
    {
        if (_current is null) return;
        var item = SelectedItems().FirstOrDefault();
        if (item is null)
        {
            MessageBox.Show(this, "请先选中一项，或点「粘贴新建」。", Text,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        EditItem(item, seedContent: null);
    }

    private void EditItem(CustomPackItem? existing, string? seedContent)
    {
        if (_current is null) return;

        var name = existing?.Name ?? "";
        var kind = existing?.Kind ?? CustomPackStore.DetectKind(seedContent ?? "");
        var content = existing is null
            ? (seedContent ?? "")
            : CustomPackStore.ReadContent(_current.Id, existing);

        if (existing is null && string.IsNullOrWhiteSpace(name))
            name = kind switch
            {
                "ps1" => "未命名 PowerShell",
                "cmd" => "未命名 CMD",
                _ => "未命名注册表",
            };

        using var dlg = new CustomPackItemEditDialog(name, kind, content, isNew: existing is null);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            if (existing is null)
            {
                var created = CustomPackStore.AddContent(_current, dlg.ItemName, dlg.ItemKind, dlg.ItemContent);
                _current = CustomPackStore.LoadPack(_current.Id);
                ReloadItems();
                if (created is not null) SelectItemById(created.Id);
            }
            else
            {
                CustomPackStore.UpdateContent(_current, existing.Id, dlg.ItemName, dlg.ItemKind, dlg.ItemContent);
                _current = CustomPackStore.LoadPack(_current.Id);
                ReloadItems();
                SelectItemById(existing.Id);
            }
            _index = CustomPackStore.LoadIndex();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void EnsureCurrentPack()
    {
        if (_current is not null) return;
        var name = PromptText("新建方案", "还没有方案。先起个名字：", "我的配置");
        if (name is null) return;
        var pack = CustomPackStore.CreatePack(name);
        _index = CustomPackStore.LoadIndex();
        ReloadPackList(pack.Id);
    }

    private void RunSelected()
    {
        if (_current is null) return;
        var selected = SelectedItems().ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show(this, "请先选中要运行的项。", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var ok = 0;
        var errors = new List<string>();
        UseWaitCursor = true;
        try
        {
            foreach (var item in selected)
            {
                try
                {
                    CustomPackRunner.RunItem(_current.Id, item);
                    ok++;
                }
                catch (Exception ex)
                {
                    errors.Add(item.Name + "：" + ex.Message);
                }
            }
        }
        finally
        {
            UseWaitCursor = false;
        }

        ShowRunResult(ok, errors);
    }

    private void RunAll()
    {
        if (_current is null) return;
        var enabled = _current.Items.Count(i => i.Enabled);
        if (enabled == 0)
        {
            MessageBox.Show(this, "没有勾选项。请勾选要运行的脚本/注册表。", Text,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var answer = MessageBox.Show(
            this,
            "将按顺序运行方案「" + _current.Name + "」中已勾选的 " + enabled + " 项。\r\n\r\n是否继续？",
            "全部运行",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (answer != DialogResult.Yes) return;

        UseWaitCursor = true;
        (int ok, int fail, List<string> errors) result;
        try
        {
            result = CustomPackRunner.RunEnabled(_current);
        }
        finally
        {
            UseWaitCursor = false;
        }

        ShowRunResult(result.ok, result.errors);
    }

    private void ShowRunResult(int ok, List<string> errors)
    {
        if (errors.Count == 0)
        {
            MessageBox.Show(this, "已成功运行 " + ok + " 项。", Text,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        MessageBox.Show(
            this,
            "成功 " + ok + " 项，失败 " + errors.Count + " 项：\r\n\r\n" + string.Join("\r\n", errors.Take(10)),
            Text,
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }

    private void RemoveSelected()
    {
        if (_current is null) return;
        var selected = SelectedItems().ToList();
        if (selected.Count == 0) return;
        var answer = MessageBox.Show(
            this,
            "从方案中移除 " + selected.Count + " 项？",
            "移除",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (answer != DialogResult.Yes) return;
        foreach (var item in selected)
            CustomPackStore.RemoveItem(_current, item.Id);
        _current = CustomPackStore.LoadPack(_current.Id);
        ReloadItems();
    }

    private IEnumerable<CustomPackItem> SelectedItems()
    {
        foreach (ListViewItem row in _items.SelectedItems)
        {
            if (row.Tag is CustomPackItem item)
                yield return item;
        }
    }

    private void SelectItemById(string itemId)
    {
        foreach (ListViewItem row in _items.Items)
        {
            if (row.Tag is CustomPackItem item &&
                string.Equals(item.Id, itemId, StringComparison.OrdinalIgnoreCase))
            {
                row.Selected = true;
                row.Focused = true;
                row.EnsureVisible();
                break;
            }
        }
    }

    private void OnFormKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.V)
        {
            PasteFromClipboard();
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }

    private void OnItemsKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Delete)
        {
            RemoveSelected();
            e.Handled = true;
        }
        else if (e.Control && e.KeyCode == Keys.V)
        {
            PasteFromClipboard();
            e.Handled = true;
        }
    }

    private void LayoutColumns()
    {
        if (_items.Columns.Count < 3) return;
        _items.Columns[1].Width = 120;
        _items.Columns[2].Width = 200;
        UiBuffer.FitListViewColumn(_items, 0, 180);
    }

    private static string? TryClipboardText()
    {
        try
        {
            if (!Clipboard.ContainsText()) return null;
            var t = Clipboard.GetText();
            return string.IsNullOrWhiteSpace(t) ? null : t;
        }
        catch
        {
            return null;
        }
    }

    private static string? PromptText(string title, string label, string initial)
    {
        using var dlg = new Form
        {
            Text = title,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(420, 140),
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            Font = new Font("Microsoft YaHei UI", 9F),
        };
        AppBrand.ApplyWindowIcon(dlg);

        var lbl = new Label
        {
            Text = label,
            Location = new Point(16, 16),
            AutoSize = true,
        };
        var box = new TextBox
        {
            Text = initial,
            Location = new Point(16, 44),
            Width = 388,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };
        var ok = ThemedSettingsChrome.CreateButton("确定", true);
        ok.DialogResult = DialogResult.OK;
        ok.Location = new Point(216, 90);
        var cancel = ThemedSettingsChrome.CreateButton("取消", false);
        cancel.DialogResult = DialogResult.Cancel;
        cancel.Location = new Point(312, 90);
        dlg.Controls.AddRange([lbl, box, ok, cancel]);
        dlg.AcceptButton = ok;
        dlg.CancelButton = cancel;
        box.SelectAll();
        if (dlg.ShowDialog() != DialogResult.OK) return null;
        var text = box.Text.Trim();
        return text.Length == 0 ? null : text;
    }
}

/// <summary>粘贴/编辑单项：名称 + 类型 + 正文，保存进方案。</summary>
internal sealed class CustomPackItemEditDialog : Form
{
    private readonly TextBox _name = new();
    private readonly ComboBox _kind = new();
    private readonly ScriptSyntaxEditor _editor = new();
    private readonly Button _btnPaste = ThemedSettingsChrome.CreateButton("粘贴剪贴板", false);
    private readonly Button _btnDetect = ThemedSettingsChrome.CreateButton("自动识别类型", false);

    public string ItemName => _name.Text.Trim();
    public string ItemKind => _kind.SelectedIndex switch
    {
        1 => "cmd",
        2 => "ps1",
        _ => "reg",
    };
    public string ItemContent => _editor.Text;

    public CustomPackItemEditDialog(string name, string kind, string content, bool isNew)
    {
        Text = isNew ? "粘贴新建" : "编辑项";
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(720, 520);
        MinimumSize = new Size(560, 400);
        Font = new Font("Microsoft YaHei UI", 9F);
        BackColor = AppTheme.Surface;

        var top = new Panel
        {
            Dock = DockStyle.Top,
            Height = 78,
            Padding = new Padding(12, 10, 12, 4),
            BackColor = AppTheme.Surface,
        };

        top.Controls.Add(new Label
        {
            Text = "名称",
            Location = new Point(12, 14),
            AutoSize = true,
            ForeColor = AppTheme.TextHeader,
        });
        _name.Location = new Point(52, 10);
        _name.Width = 280;
        _name.Text = name;
        top.Controls.Add(_name);

        top.Controls.Add(new Label
        {
            Text = "类型",
            Location = new Point(350, 14),
            AutoSize = true,
            ForeColor = AppTheme.TextHeader,
        });
        _kind.DropDownStyle = ComboBoxStyle.DropDownList;
        _kind.Location = new Point(390, 10);
        _kind.Width = 140;
        _kind.Items.AddRange(["注册表 (.reg)", "CMD (.cmd)", "PowerShell (.ps1)"]);
        _kind.SelectedIndex = kind switch
        {
            "cmd" => 1,
            "ps1" => 2,
            _ => 0,
        };
        _kind.SelectedIndexChanged += (_, _) => ApplyEditorKind();
        top.Controls.Add(_kind);

        _btnPaste.Location = new Point(12, 42);
        _btnPaste.Size = new Size(112, 28);
        _btnPaste.Click += (_, _) => PasteClipboard();
        top.Controls.Add(_btnPaste);

        _btnDetect.Location = new Point(132, 42);
        _btnDetect.Size = new Size(120, 28);
        _btnDetect.Click += (_, _) =>
        {
            var d = CustomPackStore.DetectKind(_editor.Text);
            _kind.SelectedIndex = d switch
            {
                "cmd" => 1,
                "ps1" => 2,
                _ => 0,
            };
            ApplyEditorKind();
        };
        top.Controls.Add(_btnDetect);

        _editor.Dock = DockStyle.Fill;
        _editor.BorderStyle = BorderStyle.FixedSingle;
        _editor.Margin = new Padding(12);
        ApplyEditorKind();
        _editor.SetScript(content ?? "", ToActionKind(ItemKind));

        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 52,
            Padding = new Padding(12, 8, 12, 8),
            BackColor = AppTheme.SurfaceCard,
        };
        var ok = ThemedSettingsChrome.CreateButton("保存到方案", true);
        ok.DialogResult = DialogResult.OK;
        ok.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        var cancel = ThemedSettingsChrome.CreateButton("取消", false);
        cancel.DialogResult = DialogResult.Cancel;
        cancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        footer.Controls.Add(ok);
        footer.Controls.Add(cancel);
        footer.Resize += (_, _) =>
        {
            cancel.Location = new Point(footer.ClientSize.Width - cancel.Width - 12, 10);
            ok.Location = new Point(cancel.Left - ok.Width - 8, 10);
        };

        var host = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 4, 12, 4) };
        host.Controls.Add(_editor);

        Controls.Add(host);
        Controls.Add(footer);
        Controls.Add(top);
        AcceptButton = ok;
        CancelButton = cancel;
        Shown += (_, _) =>
        {
            footer.PerformLayout();
            cancel.Location = new Point(footer.ClientSize.Width - cancel.Width - 12, 10);
            ok.Location = new Point(cancel.Left - ok.Width - 8, 10);
            if (string.IsNullOrWhiteSpace(content))
                _editor.Focus();
            else
                _name.Focus();
        };
    }

    private void PasteClipboard()
    {
        try
        {
            if (!Clipboard.ContainsText()) return;
            var t = Clipboard.GetText();
            if (string.IsNullOrEmpty(t)) return;
            _editor.SetScript(t, ToActionKind(CustomPackStore.DetectKind(t)));
            var d = CustomPackStore.DetectKind(t);
            _kind.SelectedIndex = d switch
            {
                "cmd" => 1,
                "ps1" => 2,
                _ => 0,
            };
        }
        catch { /* ignore */ }
    }

    private void ApplyEditorKind() =>
        _editor.SetScript(_editor.Text, ToActionKind(ItemKind));

    private static SettingActionKind ToActionKind(string kind) => kind switch
    {
        "cmd" => SettingActionKind.Cmd,
        "ps1" => SettingActionKind.PowerShell,
        _ => SettingActionKind.Reg,
    };

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (DialogResult == DialogResult.OK)
        {
            if (string.IsNullOrWhiteSpace(ItemName))
            {
                MessageBox.Show(this, "请填写名称。", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                e.Cancel = true;
                return;
            }
            if (string.IsNullOrWhiteSpace(ItemContent))
            {
                MessageBox.Show(this, "内容不能为空。", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                e.Cancel = true;
            }
        }
        base.OnFormClosing(e);
    }
}
