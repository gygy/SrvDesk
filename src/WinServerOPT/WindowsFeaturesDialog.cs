namespace WinOpt;

internal sealed class WindowsFeaturesDialog : Form
{
    private readonly ListView _list = new();
    private readonly TextBox _search = new();
    private readonly ComboBox _filter = new();
    private readonly Label _status = new();
    private readonly Button _btnDisable = ThemedSettingsChrome.CreateButton("禁用 / 卸载所选", true);
    private readonly Button _btnEnable = ThemedSettingsChrome.CreateButton("启用 / 安装所选", false);
    private List<WinFeatureItem> _items = [];
    private bool _busy;

    private static readonly string[] Filters =
    [
        "已启用",
        "未启用",
        "全部",
        "仅可选功能（已启用）",
        "仅 Capability（已安装）",
    ];

    public WindowsFeaturesDialog()
    {
        Text = "可选功能 / Capabilities";
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(900, 560);
        MinimumSize = new Size(720, 440);

        var body = ThemedSettingsChrome.CreateBodyPanel();
        var tools = new Panel
        {
            Dock = DockStyle.Top,
            Height = 40,
            BackColor = AppTheme.Surface,
        };
        _search.Width = 220;
        _search.Location = new Point(0, 6);
        _search.TextChanged += (_, _) => RenderList();
        _filter.DropDownStyle = ComboBoxStyle.DropDownList;
        _filter.Location = new Point(232, 6);
        _filter.Width = 200;
        foreach (var f in Filters) _filter.Items.Add(f);
        _filter.SelectedIndex = 0;
        _filter.SelectedIndexChanged += (_, _) => RenderList();
        tools.Controls.Add(_search);
        tools.Controls.Add(_filter);

        _list.View = View.Details;
        _list.FullRowSelect = true;
        _list.CheckBoxes = true;
        _list.GridLines = false;
        _list.HideSelection = false;
        _list.MultiSelect = true;
        _list.BorderStyle = BorderStyle.FixedSingle;
        _list.Dock = DockStyle.Fill;
        _list.BackColor = AppTheme.SurfaceCard;
        _list.Columns.Add("名称", 420);
        _list.Columns.Add("类型", 100);
        _list.Columns.Add("状态", 90);
        _list.Columns.Add("DISM 状态", 160);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 8, 0, 0),
            WrapContents = false,
        };
        _btnDisable.Size = new Size(140, 34);
        _btnDisable.Click += (_, _) => RunBatch(disable: true);
        _btnEnable.Size = new Size(140, 34);
        _btnEnable.Margin = new Padding(8, 0, 0, 0);
        _btnEnable.Click += (_, _) => RunBatch(disable: false);
        var selectAll = ThemedSettingsChrome.CreateButton("全选可见", false);
        selectAll.Size = new Size(90, 34);
        selectAll.Margin = new Padding(16, 0, 0, 0);
        selectAll.Click += (_, _) => SetVisibleChecked(true);
        var clear = ThemedSettingsChrome.CreateButton("全不选", false);
        clear.Size = new Size(80, 34);
        clear.Margin = new Padding(8, 0, 0, 0);
        clear.Click += (_, _) => SetVisibleChecked(false);
        actions.Controls.AddRange([_btnDisable, _btnEnable, selectAll, clear]);

        _status.Dock = DockStyle.Bottom;
        _status.Height = 28;
        _status.ForeColor = AppTheme.TextMute;
        _status.Text = "正在读取 DISM 列表…";

        body.Controls.Add(_list);
        body.Controls.Add(actions);
        body.Controls.Add(_status);
        body.Controls.Add(tools);

        ThemedSettingsChrome.MountModal(
            this,
            "可选功能 / Capabilities",
            "DISM 可视化 · 禁用可选功能 / 卸载 Capability",
            body,
            "危险组件会二次确认。完成后建议重启。",
            () => BeginLoad());

        Shown += (_, _) => BeginLoad();
        Resize += (_, _) =>
        {
            if (_list.Columns.Count > 0)
                _list.Columns[0].Width = Math.Max(200, _list.ClientSize.Width - 370);
        };
    }

    private void BeginLoad()
    {
        if (_busy) return;
        SetBusy(true, "正在读取 DISM（可能需要数十秒）…");
        System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                var items = WindowsFeaturesHelper.ListAll();
                BeginInvoke(new Action(() =>
                {
                    _items = items;
                    RenderList();
                    SetBusy(false, $"共 {_items.Count} 项 · 已启用 {_items.Count(x => x.IsEnabledOrInstalled)}");
                }));
            }
            catch (Exception ex)
            {
                BeginInvoke(new Action(() => SetBusy(false, "读取失败：" + ex.Message)));
            }
        });
    }

    private void RenderList()
    {
        var q = _search.Text.Trim();
        var mode = _filter.SelectedIndex;
        IEnumerable<WinFeatureItem> query = _items;
        query = mode switch
        {
            0 => query.Where(x => x.IsEnabledOrInstalled),
            1 => query.Where(x => !x.IsEnabledOrInstalled),
            3 => query.Where(x => x.Kind == WinFeatureKind.OptionalFeature && x.IsEnabledOrInstalled),
            4 => query.Where(x => x.Kind == WinFeatureKind.Capability && x.IsEnabledOrInstalled),
            _ => query,
        };
        if (q.Length > 0)
            query = query.Where(x => x.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);

        _list.BeginUpdate();
        _list.Items.Clear();
        foreach (var item in query)
        {
            var row = new ListViewItem(item.Name) { Tag = item };
            row.SubItems.Add(item.KindText);
            row.SubItems.Add(item.StateText);
            row.SubItems.Add(item.State);
            if (WindowsFeaturesHelper.IsCritical(item.Name))
                row.ForeColor = Color.DarkOrange;
            _list.Items.Add(row);
        }
        _list.EndUpdate();
    }

    private void SetVisibleChecked(bool on)
    {
        foreach (ListViewItem row in _list.Items)
            row.Checked = on;
    }

    private void RunBatch(bool disable)
    {
        if (_busy) return;
        var selected = _list.CheckedItems.Cast<ListViewItem>()
            .Select(i => i.Tag as WinFeatureItem)
            .Where(x => x is not null)
            .Cast<WinFeatureItem>()
            .ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show(this, "请先勾选要操作的项。", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (disable)
        {
            var critical = selected.Where(x => WindowsFeaturesHelper.IsCritical(x.Name)).Select(x => x.Name).ToList();
            if (critical.Count > 0)
            {
                var msg = "以下项可能影响系统核心功能，仍要继续？\r\n\r\n" +
                          string.Join("\r\n", critical.Take(12)) +
                          (critical.Count > 12 ? "\r\n…" : "");
                if (MessageBox.Show(this, msg, "危险确认", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    return;
            }
            else if (MessageBox.Show(this,
                         $"将禁用/卸载 {selected.Count} 项，可能需重启。继续？",
                         Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
        }
        else if (MessageBox.Show(this,
                     $"将启用/安装 {selected.Count} 项，可能需重启与联网。继续？",
                     Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        SetBusy(true, disable ? "正在禁用/卸载…" : "正在启用/安装…");
        System.Threading.Tasks.Task.Run(() =>
        {
            var ok = 0;
            var errors = new List<string>();
            foreach (var item in selected)
            {
                try
                {
                    var msg = disable
                        ? WindowsFeaturesHelper.DisableOrRemove(item)
                        : WindowsFeaturesHelper.EnableOrAdd(item);
                    ApplyLog.Write($"DISM {(disable ? "卸载" : "安装")} {item.Name} → {msg}");
                    ok++;
                }
                catch (Exception ex)
                {
                    errors.Add(item.Name + "：" + ex.Message);
                }
            }

            BeginInvoke(new Action(() =>
            {
                SetBusy(false, $"完成：成功 {ok}，失败 {errors.Count}");
                if (errors.Count > 0)
                    MessageBox.Show(this, string.Join("\r\n", errors.Take(8)), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                else
                    MessageBox.Show(this, $"已处理 {ok} 项。建议重启使更改完全生效。", Text,
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                BeginLoad();
            }));
        });
    }

    private void SetBusy(bool busy, string status)
    {
        _busy = busy;
        UseWaitCursor = busy;
        _btnDisable.Enabled = !busy;
        _btnEnable.Enabled = !busy;
        _status.Text = status;
    }
}
