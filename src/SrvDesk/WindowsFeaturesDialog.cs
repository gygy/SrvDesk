namespace SrvDesk;

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

    private static readonly (string Label, string[] Hints)[] QuickFeatureHints =
    [
        ("Hyper-V", ["Microsoft-Hyper-V", "Hyper-V"]),
        ("WSL", ["Microsoft-Windows-Subsystem-Linux", "VirtualMachinePlatform"]),
        ("Sandbox", ["Containers-DisposableClientVM"]),
        ("OpenSSH", ["OpenSSH.Server", "OpenSSH.Client"]),
        ("NFS", ["ServicesForNFS-ClientAndTools", "NFS-Administration", "ClientForNFS-Infrastructure"]),
    ];

    public WindowsFeaturesDialog()
    {
        Text = "可选功能 / Capabilities";
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = UiScale.Size(960, 640);
        MinimumSize = UiScale.Size(800, 520);

        var body = ThemedSettingsChrome.CreateBodyPanel();
        var btnH = UiFit.ControlHeight();
        var tools = new Panel
        {
            Dock = DockStyle.Top,
            Height = btnH + UiScale.S(16),
            BackColor = AppTheme.Surface,
        };
        _search.Width = UiScale.S(220);
        _search.Font = UiFit.UiFont;
        _search.Height = btnH;
        _search.Location = new Point(0, Math.Max(4, (tools.Height - btnH) / 2));
        _search.TextChanged += (_, _) => RenderList();
        _filter.DropDownStyle = ComboBoxStyle.DropDownList;
        _filter.Font = UiFit.UiFont;
        UiFit.FitCombo(_filter);
        _filter.Location = new Point(_search.Right + UiScale.S(12), Math.Max(4, (tools.Height - _filter.Height) / 2));
        _filter.Width = UiScale.S(220);
        foreach (var f in Filters) _filter.Items.Add(f);
        _filter.SelectedIndex = 0;
        _filter.SelectedIndexChanged += (_, _) => RenderList();
        tools.Controls.Add(_search);
        tools.Controls.Add(_filter);

        var quick = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = btnH + UiScale.S(16),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, Math.Max(4, (btnH + UiScale.S(16) - btnH) / 2), 0, 0),
            AutoScroll = false,
            BackColor = AppTheme.Surface,
        };
        UiBuffer.ConfigureNoScrollRow(quick);
        quick.Controls.Add(new Label
        {
            Text = AppLang.L("快捷：", "Quick:"),
            AutoSize = true,
            Margin = new Padding(0, Math.Max(4, (btnH - UiFit.LineHeight()) / 2), UiScale.S(4), 0),
            ForeColor = AppTheme.TextMute,
            Font = UiFit.UiFont,
        });
        foreach (var (label, hints) in QuickFeatureHints)
        {
            var btn = ThemedSettingsChrome.CreateButton(label, false);
            UiFit.FitButton(btn, btnH, minWidth: 64, padding: 24);
            btn.Margin = new Padding(UiScale.S(4), 0, 0, 0);
            var capture = hints;
            btn.Click += (_, _) => SelectQuickFeatures(capture);
            quick.Controls.Add(btn);
        }

        _list.View = View.Details;
        _list.FullRowSelect = true;
        _list.CheckBoxes = true;
        _list.GridLines = false;
        _list.HideSelection = false;
        _list.MultiSelect = true;
        _list.BorderStyle = BorderStyle.FixedSingle;
        _list.Dock = DockStyle.Fill;
        _list.BackColor = AppTheme.SurfaceCard;
        UiBuffer.Enable(_list);
        _list.Columns.Add("名称", 360);
        _list.Columns.Add("类型", 90);
        _list.Columns.Add("状态", 80);
        _list.Columns.Add(AppLang.L("用途建议", "Profile tip"), 140);
        _list.Columns.Add("DISM 状态", 140);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = btnH + UiScale.S(20),
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, UiScale.S(8), 0, UiScale.S(4)),
            WrapContents = false,
            AutoScroll = false,
            BackColor = AppTheme.Surface,
        };
        UiBuffer.ConfigureNoScrollRow(actions);
        UiFit.FitButton(_btnDisable, btnH, minWidth: 120, padding: 28);
        _btnDisable.Click += (_, _) => RunBatch(disable: true);
        UiFit.FitButton(_btnEnable, btnH, minWidth: 120, padding: 28);
        _btnEnable.Margin = new Padding(UiScale.S(8), 0, 0, 0);
        _btnEnable.Click += (_, _) => RunBatch(disable: false);
        var selectAll = ThemedSettingsChrome.CreateButton("全选可见", false);
        UiFit.FitButton(selectAll, btnH, minWidth: 72, padding: 24);
        selectAll.Margin = new Padding(UiScale.S(16), 0, 0, 0);
        selectAll.Click += (_, _) => SetVisibleChecked(true);
        var clear = ThemedSettingsChrome.CreateButton("全不选", false);
        UiFit.FitButton(clear, btnH, minWidth: 72, padding: 24);
        clear.Margin = new Padding(UiScale.S(8), 0, 0, 0);
        clear.Click += (_, _) => SetVisibleChecked(false);
        actions.Controls.AddRange([_btnDisable, _btnEnable, selectAll, clear]);

        _status.Dock = DockStyle.Bottom;
        _status.Height = Math.Max(UiScale.S(28), UiFit.LineHeight() + UiScale.S(10));
        _status.ForeColor = AppTheme.TextMute;
        _status.TextAlign = ContentAlignment.MiddleLeft;
        _status.Padding = new Padding(0, 2, 0, 2);
        _status.Text = "正在读取 DISM 列表…";

        // Dock：Fill 列表；Bottom 先动作再状态（状态贴底）；Top 先快捷再搜索（搜索贴顶）
        body.Controls.Add(_list);
        body.Controls.Add(actions);
        body.Controls.Add(_status);
        _status.BringToFront();
        body.Controls.Add(quick);
        body.Controls.Add(tools);
        tools.BringToFront();

        ThemedSettingsChrome.MountModal(
            this,
            "可选功能 / Capabilities",
            "DISM 可视化 · 禁用可选功能 / 卸载 Capability",
            body,
            "",
            () => BeginLoad());

        Shown += (_, _) => BeginLoad();
        DpiChanged += (_, _) =>
        {
            UiScale.OnHostDpiChanged(this);
            var h = UiFit.ControlHeight();
            tools.Height = h + UiScale.S(16);
            quick.Height = h + UiScale.S(16);
            actions.Height = h + UiScale.S(20);
            _status.Height = Math.Max(UiScale.S(28), UiFit.LineHeight() + UiScale.S(10));
            _search.Height = h;
            _search.Top = Math.Max(4, (tools.Height - h) / 2);
            UiFit.FitCombo(_filter);
            _filter.Top = Math.Max(4, (tools.Height - _filter.Height) / 2);
            foreach (Control c in quick.Controls)
            {
                if (c is Button b)
                    UiFit.FitButton(b, h, minWidth: 64, padding: 24);
            }
            UiFit.FitButton(_btnDisable, h, minWidth: 120, padding: 28);
            UiFit.FitButton(_btnEnable, h, minWidth: 120, padding: 28);
            UiFit.FitButton(selectAll, h, minWidth: 72, padding: 24);
            UiFit.FitButton(clear, h, minWidth: 72, padding: 24);
        };
        UiBuffer.BindListViewColumnFit(_list, 0, 200);
    }

    private static string FeatureProfileTip(string name)
    {
        if (string.IsNullOrEmpty(name)) return "—";
        if (name.IndexOf("SMB1", StringComparison.OrdinalIgnoreCase) >= 0)
            return AppLang.L("建议卸载", "Remove");
        if (name.IndexOf("Telnet", StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("TFTP", StringComparison.OrdinalIgnoreCase) >= 0)
            return AppLang.L("建议卸载", "Remove");
        if (ServerProfile.Has(ServerRoleFlags.WebIis) &&
            (name.IndexOf("IIS", StringComparison.OrdinalIgnoreCase) >= 0 ||
             name.IndexOf("Web-Server", StringComparison.OrdinalIgnoreCase) >= 0))
            return AppLang.L("用途·保留", "Keep (role)");
        if (ServerProfile.Has(ServerRoleFlags.HyperV) &&
            name.IndexOf("Hyper-V", StringComparison.OrdinalIgnoreCase) >= 0)
            return AppLang.L("用途·保留", "Keep (role)");
        if (ServerProfile.Has(ServerRoleFlags.Docker) &&
            (name.IndexOf("Containers", StringComparison.OrdinalIgnoreCase) >= 0 ||
             name.IndexOf("Microsoft-Windows-Subsystem-Linux", StringComparison.OrdinalIgnoreCase) >= 0))
            return AppLang.L("用途·保留", "Keep (role)");
        if (WindowsFeaturesHelper.IsCritical(name))
            return AppLang.L("关键·慎动", "Critical");
        return "—";
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
            row.SubItems.Add(FeatureProfileTip(item.Name));
            row.SubItems.Add(item.State);
            if (WindowsFeaturesHelper.IsCritical(item.Name))
                row.ForeColor = Color.DarkOrange;
            if (item.Name.IndexOf("SMB1", StringComparison.OrdinalIgnoreCase) >= 0 ||
                item.Name.IndexOf("SMB1Protocol", StringComparison.OrdinalIgnoreCase) >= 0)
                row.ForeColor = Color.FromArgb(200, 60, 40);
            _list.Items.Add(row);
        }
        _list.EndUpdate();
    }

    private void SetVisibleChecked(bool on)
    {
        foreach (ListViewItem row in _list.Items)
            row.Checked = on;
    }

    private void SelectQuickFeatures(string[] hints)
    {
        if (_items.Count == 0)
        {
            MessageBox.Show(this,
                AppLang.L("请等待 DISM 列表加载完成。", "Wait until the DISM list finishes loading."),
                Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _filter.SelectedIndex = 2; // 全部
        _search.Text = "";
        RenderList();
        var hit = 0;
        foreach (ListViewItem row in _list.Items)
        {
            if (row.Tag is not WinFeatureItem item) continue;
            var match = hints.Any(h => item.Name.IndexOf(h, StringComparison.OrdinalIgnoreCase) >= 0);
            row.Checked = match;
            if (match) hit++;
        }

        _status.Text = hit > 0
            ? AppLang.L($"已勾选 {hit} 项快捷匹配，可点「启用 / 安装所选」。",
                $"Checked {hit} quick match(es). Click Enable/Install.")
            : AppLang.L("当前系统未找到匹配项（SKU/版本可能不含）。",
                "No matches on this SKU/version.");
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
                    ApplyLog.SystemChange(
                        item.Name,
                        $"DISM {(disable ? "卸载" : "安装")} 可选功能",
                        disable ? "已安装" : "未安装",
                        msg);
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
