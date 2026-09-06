using System.Diagnostics;

namespace WinOpt;

/// <summary>自定义配置：多方案，每方案可含多个 .reg / .cmd / .ps1，可保存并可在界面运行。</summary>
internal sealed class CustomConfigDialog : Form, IEmbeddedSettingsPage
{
    private readonly ListBox _packs = new();
    private readonly ListView _items = new();
    private readonly Label _hint = new();
    private readonly Label _packTitle = new();
    private readonly ToolTip _tip = new();

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
        AllowDrop = true;

        var body = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Surface };
        var sidebar = BuildPackSidebar();
        sidebar.Dock = DockStyle.Left;

        var main = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 10, 12, 8) };
        var tools = BuildItemTools();
        tools.Dock = DockStyle.Top;

        _packTitle.Dock = DockStyle.Top;
        _packTitle.Height = 28;
        _packTitle.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
        _packTitle.ForeColor = AppTheme.PrimaryDeep;
        _packTitle.Text = "选择或新建方案";
        _packTitle.Padding = new Padding(0, 0, 0, 4);

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
        _items.Columns.Add("名称", 280);
        _items.Columns.Add("类型", 100);
        _items.Columns.Add("文件", 220);
        _items.ItemChecked += OnItemChecked;
        _items.DoubleClick += (_, _) => RunSelected();
        _items.KeyDown += OnItemsKeyDown;

        _hint.Dock = DockStyle.Bottom;
        _hint.Height = 40;
        _hint.ForeColor = AppTheme.TextMute;
        _hint.Padding = new Padding(0, 6, 0, 0);
        _hint.Text = "勾选参与「全部运行」的项。双击或点「运行选中」立即执行。文件已复制到本机，关闭软件后仍可打开。";

        main.Controls.Add(_items);
        main.Controls.Add(_hint);
        main.Controls.Add(tools);
        main.Controls.Add(_packTitle);

        body.Controls.Add(main);
        body.Controls.Add(sidebar);

        ThemedSettingsChrome.MountEmbedded(
            this,
            "自定义配置",
            "自建方案 · 导入注册表与脚本 · 保存后下次继续用",
            body,
            "方案保存在本机 AppData。运行 .reg（HKLM）或脚本通常需要管理员权限。",
            RefreshFromSystem);

        DragEnter += OnDragEnter;
        DragDrop += OnDragDrop;
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

    private Panel BuildPackSidebar()
    {
        var side = new Panel
        {
            Width = 200,
            BackColor = AppTheme.NavBg,
            Padding = new Padding(8, 10, 8, 10),
        };

        var title = new Label
        {
            Text = "方案",
            Dock = DockStyle.Top,
            Height = 24,
            ForeColor = AppTheme.TextMute,
            Font = new Font("Microsoft YaHei UI", 8.5F),
        };

        var packTools = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 108,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = AppTheme.NavBg,
            Padding = new Padding(0, 8, 0, 0),
        };
        packTools.Controls.Add(SideButton("新建方案", NewPack));
        packTools.Controls.Add(SideButton("重命名", RenamePack));
        packTools.Controls.Add(SideButton("删除方案", DeletePack));

        _packs.Dock = DockStyle.Fill;
        _packs.BorderStyle = BorderStyle.None;
        _packs.BackColor = AppTheme.NavBg;
        _packs.ForeColor = AppTheme.TextMain;
        _packs.IntegralHeight = false;
        _packs.ItemHeight = 36;
        _packs.DrawMode = DrawMode.OwnerDrawFixed;
        _packs.DrawItem += DrawPackItem;
        _packs.SelectedIndexChanged += (_, _) => OnPackSelected();

        side.Controls.Add(_packs);
        side.Controls.Add(packTools);
        side.Controls.Add(title);
        return side;
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

        tools.Controls.Add(ToolButton("添加文件…", "导入 .reg / .cmd / .bat / .ps1（可多选）", AddFiles));
        tools.Controls.Add(ToolButton("运行选中", "运行当前选中的项", RunSelected));
        tools.Controls.Add(ToolButton("全部运行", "按顺序运行已勾选的项", RunAll));
        tools.Controls.Add(ToolButton("编辑", "用记事本打开文件", EditSelected));
        tools.Controls.Add(ToolButton("上移", "调整运行顺序", () => MoveSelected(-1)));
        tools.Controls.Add(ToolButton("下移", "调整运行顺序", () => MoveSelected(1)));
        tools.Controls.Add(ToolButton("移除", "从方案中删除（不删源文件，仅删方案副本）", RemoveSelected));
        return tools;
    }

    private Button SideButton(string text, Action click)
    {
        var b = ThemedSettingsChrome.CreateButton(text, false);
        b.Width = 176;
        b.Height = 28;
        b.Margin = new Padding(0, 0, 0, 6);
        b.Click += (_, _) => click();
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

            if (_packs.Items.Count == 0)
            {
                _current = null;
                _packTitle.Text = "还没有方案 — 点左侧「新建方案」开始";
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
        _packTitle.Text = _current?.Name ?? summary.Name;
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
                row.SubItems.Add(item.FileName);
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
            "确定删除方案「" + _current.Name + "」？\r\n方案内的脚本副本也会删除（不影响你当初导入的源文件）。",
            "删除方案",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);
        if (answer != DialogResult.Yes) return;
        CustomPackStore.DeletePack(_current.Id);
        _index = CustomPackStore.LoadIndex();
        ReloadPackList(_index.LastPackId);
    }

    private void AddFiles()
    {
        EnsureCurrentPack();
        if (_current is null) return;

        using var dlg = new OpenFileDialog
        {
            Title = "添加注册表或脚本",
            Filter = "支持的文件|*.reg;*.cmd;*.bat;*.ps1|注册表 (*.reg)|*.reg|CMD (*.cmd;*.bat)|*.cmd;*.bat|PowerShell (*.ps1)|*.ps1|所有文件|*.*",
            Multiselect = true,
            CheckFileExists = true,
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        ImportPaths(dlg.FileNames);
    }

    private void ImportPaths(IEnumerable<string> paths)
    {
        EnsureCurrentPack();
        if (_current is null) return;

        var ok = 0;
        var errors = new List<string>();
        foreach (var path in paths)
        {
            try
            {
                CustomPackStore.AddFile(_current, path);
                ok++;
            }
            catch (Exception ex)
            {
                errors.Add(Path.GetFileName(path) + "：" + ex.Message);
            }
        }

        _current = CustomPackStore.LoadPack(_current.Id);
        _index = CustomPackStore.LoadIndex();
        ReloadPackList(_current?.Id);
        if (errors.Count > 0)
        {
            MessageBox.Show(
                this,
                "成功 " + ok + " 个；失败：\r\n" + string.Join("\r\n", errors.Take(8)),
                "添加文件",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
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

    private void EditSelected()
    {
        if (_current is null) return;
        var item = SelectedItems().FirstOrDefault();
        if (item is null)
        {
            MessageBox.Show(this, "请先选中一项。", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        var path = CustomPackStore.ItemPath(_current.Id, item);
        if (!File.Exists(path))
        {
            MessageBox.Show(this, "文件不存在：" + path, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "notepad.exe",
                Arguments = "\"" + path + "\"",
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void MoveSelected(int delta)
    {
        if (_current is null) return;
        var item = SelectedItems().FirstOrDefault();
        if (item is null) return;
        CustomPackStore.MoveItem(_current, item.Id, delta);
        _current = CustomPackStore.LoadPack(_current.Id);
        ReloadItems();
        SelectItemById(item.Id);
    }

    private void RemoveSelected()
    {
        if (_current is null) return;
        var selected = SelectedItems().ToList();
        if (selected.Count == 0) return;
        var answer = MessageBox.Show(
            this,
            "从方案中移除 " + selected.Count + " 项？\r\n（仅删除方案内副本）",
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

    private void OnItemsKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Delete)
        {
            RemoveSelected();
            e.Handled = true;
        }
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            e.Effect = DragDropEffects.Copy;
    }

    private void OnDragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0)
            return;
        ImportPaths(files);
    }

    private void LayoutColumns()
    {
        if (_items.Columns.Count < 3) return;
        var w = Math.Max(400, _items.ClientSize.Width);
        _items.Columns[0].Width = Math.Max(160, w - 340);
        _items.Columns[1].Width = 100;
        _items.Columns[2].Width = 220;
    }

    private void DrawPackItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;
        e.DrawBackground();
        var selected = (e.State & DrawItemState.Selected) != 0;
        var bg = selected ? AppTheme.PrimaryPale : AppTheme.NavBg;
        using (var b = new SolidBrush(bg))
            e.Graphics.FillRectangle(b, e.Bounds);
        if (selected)
        {
            using var pen = new Pen(AppTheme.Primary);
            e.Graphics.DrawRectangle(pen, e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 1, e.Bounds.Height - 1);
        }

        var text = _packs.Items[e.Index] is CustomPackSummary s ? s.Name : _packs.Items[e.Index]?.ToString() ?? "";
        TextRenderer.DrawText(
            e.Graphics,
            text,
            Font,
            new Rectangle(e.Bounds.X + 10, e.Bounds.Y, e.Bounds.Width - 14, e.Bounds.Height),
            AppTheme.TextMain,
            TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
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
