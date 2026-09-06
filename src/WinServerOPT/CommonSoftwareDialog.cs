using System.Collections.Concurrent;

namespace WinOpt;

internal sealed class CommonSoftwareDialog : Form
{
    private readonly CheckBox _askBeforeInstall = new();
    private readonly Panel _listHost = new BufferedPanel(composited: true);
    private readonly Label _wingetHint = new();
    private readonly Button _installWingetBtn = new();
    private readonly ListBox _categoryMenu = new();
    private readonly Dictionary<string, CommonSoftwareRow> _rows = new(StringComparer.OrdinalIgnoreCase);
    private readonly Panel _progressHost = new();
    private readonly Label _progressLabel = new();
    private readonly ProgressBar _progressBar = new();
    private readonly List<Control> _actionButtons = [];
    private readonly ToolTip _toolTip = new();
    private string _selectedCategory = "全部";
    private int _categoryHover = -1;
    private string? _busyItemId;

    private static readonly string[] Categories =
        ["全部", "必备", "微软运行库", "工具", "浏览器", "通讯", "网盘", "开发"];

    public CommonSoftwareDialog()
    {
        Text = "常用软件";
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(920, 620);
        MinimumSize = new Size(780, 520);
        ForeColor = AppTheme.TextMain;

        var body = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Surface, Padding = new Padding(0, 0, 0, 4) };

        var menu = new MenuStrip
        {
            BackColor = AppTheme.SurfaceCard,
            ForeColor = AppTheme.TextMain,
            Padding = new Padding(4, 2, 0, 2),
        };
        var op = new ToolStripMenuItem("操作(&A)");
        var mInstallSelected = new ToolStripMenuItem("安装所选");
        mInstallSelected.Click += (_, _) => InstallSelected();
        var mEssentials = new ToolStripMenuItem("安装系统必备");
        mEssentials.Click += (_, _) => InstallEssentials();
        var mUpdates = new ToolStripMenuItem("检查软件更新");
        mUpdates.Click += (_, _) => CheckSoftwareUpdates();
        var mClearCache = new ToolStripMenuItem("清理下载缓存");
        mClearCache.Click += (_, _) =>
        {
            CommonSoftwareHelper.ClearDownloadCache();
            MessageBox.Show(this, "已清理下载临时目录。", "常用软件", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };
        op.DropDownItems.AddRange([
            mInstallSelected, mEssentials, new ToolStripSeparator(), mUpdates, mClearCache
        ]);
        var edit = new ToolStripMenuItem("编辑(&E)");
        var mSelectAll = new ToolStripMenuItem("全选当前");
        mSelectAll.Click += (_, _) => SetAllSelected(true);
        var mSelectNone = new ToolStripMenuItem("全不选");
        mSelectNone.Click += (_, _) => SetAllSelected(false);
        edit.DropDownItems.AddRange([mSelectAll, mSelectNone]);
        menu.Items.AddRange([op, edit]);
        menu.Dock = DockStyle.Top;

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            AutoScroll = false,
            Padding = new Padding(12, 6, 12, 6),
            BackColor = AppTheme.Surface,
        };
        // 底部只留最常用：安装所选；其余进菜单
        actions.Controls.Add(TrackAction(MkBtn("安装所选", InstallSelected, true)));

        BuildProgressHost();

        var sidebar = BuildSidebar();
        sidebar.Dock = DockStyle.Left;

        var main = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.Surface, Padding = new Padding(12, 8, 12, 8) };
        var toolStrip = BuildToolStrip();
        toolStrip.Dock = DockStyle.Top;

        var tableWrap = new Panel { Dock = DockStyle.Fill, BackColor = AppTheme.SurfaceCard };
        tableWrap.Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.BorderLight);
            e.Graphics.DrawRectangle(pen, 0, 0, tableWrap.Width - 1, tableWrap.Height - 1);
        };
        var tableHeader = BuildTableHeader();
        tableHeader.Dock = DockStyle.Top;
        _listHost.Dock = DockStyle.Fill;
        _listHost.AutoScroll = true;
        _listHost.BackColor = AppTheme.SurfaceCard;
        tableWrap.Controls.Add(_listHost);
        tableWrap.Controls.Add(tableHeader);
        main.Controls.Add(tableWrap);
        main.Controls.Add(toolStrip);

        var content = new Panel { Dock = DockStyle.Fill };
        content.Controls.Add(main);
        content.Controls.Add(sidebar);
        body.Controls.Add(content);
        body.Controls.Add(_progressHost);
        body.Controls.Add(actions);
        body.Controls.Add(menu);

        ThemedSettingsChrome.MountModal(
            this,
            "常用软件",
            "官方源下载与安装 · 优先 winget",
            body,
            "",
            () => ReloadStatusesAsync(),
            showHeader: false);

        MainMenuStrip = menu;

        Load += (_, _) =>
        {
            // 先快速画出列表，状态检测放到后台，避免卡在「正在打开」
            if (_categoryMenu.SelectedIndex < 0) _categoryMenu.SelectedIndex = 0;
            BuildList();
            SetRowsPendingStatus();
            _wingetHint.Text = "正在检测软件状态…";
            CommonSoftwareHelper.WarmUpInBackground();
            ReloadStatusesAsync(forceRefresh: false);
        };
        _categoryMenu.SelectedIndexChanged += (_, _) =>
        {
            if (_categoryMenu.SelectedItem is string cat)
            {
                _selectedCategory = cat;
                BuildList();
            }
        };
    }

    private Control TrackAction(Control c)
    {
        _actionButtons.Add(c);
        return c;
    }

    private void BuildProgressHost()
    {
        _progressHost.Dock = DockStyle.Bottom;
        _progressHost.Height = 52;
        _progressHost.Padding = new Padding(12, 6, 12, 6);
        _progressHost.BackColor = AppTheme.PrimaryPale;
        _progressHost.Visible = false;
        _progressHost.Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.BorderLight);
            e.Graphics.DrawLine(pen, 0, 0, _progressHost.Width, 0);
        };

        _progressLabel.Dock = DockStyle.Top;
        _progressLabel.Height = 20;
        _progressLabel.ForeColor = AppTheme.PrimaryDeep;
        _progressLabel.Font = new Font("Microsoft YaHei UI", 9F);
        _progressLabel.TextAlign = ContentAlignment.MiddleLeft;
        _progressLabel.AutoEllipsis = true;
        _progressLabel.Text = "准备中…";

        _progressBar.Dock = DockStyle.Bottom;
        _progressBar.Height = 14;
        _progressBar.Style = ProgressBarStyle.Continuous;
        _progressBar.Minimum = 0;
        _progressBar.Maximum = 100;
        _progressBar.Value = 0;

        _progressHost.Controls.Add(_progressBar);
        _progressHost.Controls.Add(_progressLabel);
    }

    private static Button MkBtn(string text, Action click, bool primary)
    {
        var b = ThemedSettingsChrome.CreateButton(text, primary);
        b.AutoSize = true;
        b.Height = 32;
        b.Margin = new Padding(6, 0, 0, 0);
        b.Click += (_, _) => click();
        return b;
    }

    private Panel BuildSidebar()
    {
        var sidebar = new Panel { Width = 176, BackColor = AppTheme.NavBg };

        var cap = new Label
        {
            Text = "  软件分类",
            Dock = DockStyle.Top,
            Height = 36,
            ForeColor = AppTheme.TextMute,
            Font = new Font("Microsoft YaHei UI", 8.5F),
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = AppTheme.NavBg,
        };

        _categoryMenu.Dock = DockStyle.Fill;
        _categoryMenu.BorderStyle = BorderStyle.None;
        _categoryMenu.BackColor = AppTheme.NavBg;
        _categoryMenu.ForeColor = AppTheme.TextMain;
        _categoryMenu.IntegralHeight = false;
        _categoryMenu.DrawMode = DrawMode.OwnerDrawFixed;
        _categoryMenu.ItemHeight = 48;
        _categoryMenu.Items.AddRange(Categories);
        _categoryMenu.DrawItem += DrawCategoryItem;
        _categoryMenu.MouseMove += (_, e) =>
        {
            var i = _categoryMenu.IndexFromPoint(e.Location);
            if (i != _categoryHover) { _categoryHover = i; _categoryMenu.Invalidate(); }
        };
        _categoryMenu.MouseLeave += (_, _) => { _categoryHover = -1; _categoryMenu.Invalidate(); };

        sidebar.Controls.Add(_categoryMenu);
        sidebar.Controls.Add(cap);
        sidebar.Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.BorderLight);
            e.Graphics.DrawLine(pen, sidebar.Width - 1, 0, sidebar.Width - 1, sidebar.Height);
        };
        return sidebar;
    }

    private void DrawCategoryItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || sender is not ListBox box) return;
        var selected = (e.State & DrawItemState.Selected) != 0;
        var hover = e.Index == _categoryHover;
        var back = selected ? AppTheme.PrimaryPale : hover ? AppTheme.NavHover : AppTheme.NavBg;
        using var backBrush = new SolidBrush(back);
        e.Graphics.FillRectangle(backBrush, e.Bounds);
        if (selected)
        {
            using var accent = new SolidBrush(AppTheme.PrimarySoft);
            e.Graphics.FillRectangle(accent, e.Bounds.X, e.Bounds.Y + 8, 3, e.Bounds.Height - 16);
        }
        TextRenderer.DrawText(
            e.Graphics,
            box.Items[e.Index]?.ToString() ?? "",
            selected ? new Font(Font, FontStyle.Bold) : Font,
            new Rectangle(e.Bounds.X + 18, e.Bounds.Y, e.Bounds.Width - 22, e.Bounds.Height),
            selected ? AppTheme.PrimaryDeep : AppTheme.TextMain,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
    }

    private Panel BuildToolStrip()
    {
        var strip = new Panel
        {
            Height = 40,
            BackColor = AppTheme.Surface,
            Padding = new Padding(0, 0, 0, 8),
        };

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = true,
            BackColor = AppTheme.Surface,
            Padding = new Padding(0),
        };

        _wingetHint.AutoSize = false;
        _wingetHint.Size = new Size(280, 24);
        _wingetHint.Margin = new Padding(0, 8, 12, 0);
        _wingetHint.ForeColor = AppTheme.TextMute;

        _installWingetBtn.Text = "一键安装 winget";
        _installWingetBtn.Size = new Size(120, 28);
        _installWingetBtn.Margin = new Padding(0, 4, 12, 0);
        _installWingetBtn.FlatStyle = FlatStyle.Flat;
        _installWingetBtn.BackColor = AppTheme.Primary;
        _installWingetBtn.ForeColor = AppTheme.TextOnPrimary;
        _installWingetBtn.Cursor = Cursors.Hand;
        _installWingetBtn.FlatAppearance.BorderSize = 0;
        _installWingetBtn.Visible = false;
        _installWingetBtn.Click += (_, _) => InstallWingetNow();

        // 左侧：状态 / winget；右侧紧凑区：确认开关 + 选择快捷
        _askBeforeInstall.Text = "安装前确认";
        _askBeforeInstall.Checked = false;
        _askBeforeInstall.AutoSize = true;
        _askBeforeInstall.Margin = new Padding(8, 8, 4, 0);
        _askBeforeInstall.ForeColor = AppTheme.TextMain;
        _toolTip.SetToolTip(_askBeforeInstall, "勾选后，安装前会弹出确认对话框");

        var selectBtn = ThemedSettingsChrome.CreateButton("选择 ▾", false);
        selectBtn.Size = new Size(72, 28);
        selectBtn.Margin = new Padding(4, 4, 0, 0);
        selectBtn.Padding = new Padding(0);
        var selectMenu = new ContextMenuStrip();
        selectMenu.Items.Add("全选当前分类", null, (_, _) => SetAllSelected(true));
        selectMenu.Items.Add("全不选", null, (_, _) => SetAllSelected(false));
        selectMenu.Items.Add(new ToolStripSeparator());
        selectMenu.Items.Add("仅选必备", null, (_, _) => SelectBy(r => r.Item.Essential));
        selectMenu.Items.Add("仅选运行库推荐", null, (_, _) =>
            SelectBy(r => r.Item.Id.Equals("vcredist-2022-x64", StringComparison.OrdinalIgnoreCase)));
        selectMenu.Items.Add("仅选未安装", null, (_, _) => SelectBy(r => !r.IsInstalled));
        selectBtn.Click += (_, _) => selectMenu.Show(selectBtn, new Point(0, selectBtn.Height));
        _toolTip.SetToolTip(selectBtn, "快速勾选列表项，便于「安装所选」");

        flow.Controls.Add(_wingetHint);
        flow.Controls.Add(_installWingetBtn);
        flow.Controls.Add(_askBeforeInstall);
        flow.Controls.Add(selectBtn);
        strip.Controls.Add(flow);
        return strip;
    }

    private static Panel BuildTableHeader()
    {
        const int h = 36;
        var header = new Panel
        {
            Height = h,
            BackColor = AppTheme.PrimaryLight,
        };
        header.Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.Border);
            e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
        };
        header.Controls.Add(MakeHeaderCell("选", 8, 36, ContentAlignment.MiddleCenter));
        header.Controls.Add(MakeHeaderCell("软件名称", 44, 300));
        header.Controls.Add(MakeHeaderCell("安装", 352, 96, ContentAlignment.MiddleCenter));
        header.Controls.Add(MakeHeaderCell("卸载", 456, 72, ContentAlignment.MiddleCenter));
        header.Controls.Add(MakeHeaderCell("状态", 536, 300));
        header.Resize += (_, _) => header.Invalidate();
        return header;
    }

    private void BuildList()
    {
        _listHost.SuspendLayout();
        _listHost.AutoScroll = false;
        _listHost.AutoScrollMinSize = Size.Empty;
        _listHost.AutoScrollPosition = Point.Empty;
        _listHost.Controls.Clear();
        _rows.Clear();

        var items = _selectedCategory == "全部"
            ? CommonSoftwareCatalog.All
            : CommonSoftwareCatalog.All.Where(x => x.Category == _selectedCategory).ToList();

        const int rowH = 44;
        var y = 0;
        var alt = false;
        var w = Math.Max(680, Math.Max(0, _listHost.ClientSize.Width - SystemInformation.VerticalScrollBarWidth));
        foreach (var item in items)
        {
            var row = new CommonSoftwareRow(item, rowH, alt ? AppTheme.RowAlt : AppTheme.SurfaceCard, OnInstall, OnUninstall);
            row.SetBounds(0, y, w, rowH);
            row.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            _listHost.Controls.Add(row);
            _rows[item.Id] = row;
            y += rowH;
            alt = !alt;
        }

        _listHost.AutoScrollMinSize = new Size(0, y);
        _listHost.AutoScroll = true;
        _listHost.AutoScrollPosition = Point.Empty;
        _listHost.ResumeLayout(true);
        _listHost.Resize -= OnListHostResize;
        _listHost.Resize += OnListHostResize;
    }

    private void OnListHostResize(object? sender, EventArgs e)
    {
        var w = Math.Max(680, _listHost.ClientSize.Width - 4);
        foreach (Control c in _listHost.Controls)
            c.Width = w;
    }

    private void SetRowsPendingStatus()
    {
        foreach (var row in _rows.Values)
            row.SetPendingDetect();
    }

    /// <param name="forceRefresh">安装/卸载后强制重扫；首次打开可复用已有缓存。</param>
    private void ReloadStatusesAsync(bool forceRefresh = true)
    {
        if (_rows.Count == 0) BuildList();
        SetRowsPendingStatus();
        _wingetHint.Text = "正在检测软件状态…";
        System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                if (forceRefresh)
                    CommonSoftwareHelper.InvalidateStatusCache();
                CommonSoftwareHelper.PrefetchStatuses(CommonSoftwareCatalog.All);
                _ = CommonSoftwareHelper.IsWingetAvailable();
            }
            catch { /* ignore */ }

            Ui(RefreshAll);
        });
    }

    private void RefreshAll()
    {
        // 不每次重置探测：重复 Probe winget 很慢；安装/修复后再 Reset
        var wingetOk = CommonSoftwareHelper.IsWingetAvailable();
        _installWingetBtn.Visible = !wingetOk;
        _wingetHint.Text = wingetOk
            ? "已检测到 winget · 静默安装 · 源索引后台预热"
            : "未检测到 winget，请先安装后再使用一键安装其它软件。";

        if (_categoryMenu.SelectedIndex < 0) _categoryMenu.SelectedIndex = 0;
        if (_rows.Count == 0) BuildList();
        foreach (var row in _rows.Values)
            row.RefreshStatus();
    }

    private bool _installBusy;

    private void SetInstallBusy(bool busy, string? progress = null, int current = -1, int total = -1, string? itemId = null, int percent = -1)
    {
        _installBusy = busy;
        _installWingetBtn.Enabled = !busy;
        if (MainMenuStrip is not null)
            MainMenuStrip.Enabled = !busy;
        foreach (var btn in _actionButtons)
            btn.Enabled = !busy;

        var text = string.IsNullOrWhiteSpace(progress) ? (busy ? "处理中…" : "") : progress!;
        if (busy && percent >= 0 && text.IndexOf('%') < 0)
            text += "  " + percent + "%";

        Text = busy ? "常用软件 — " + text : "常用软件";
        Cursor = Cursors.Default;
        UseWaitCursor = false;

        if (!busy)
        {
            _busyItemId = null;
            _progressHost.Visible = false;
            _progressBar.Style = ProgressBarStyle.Continuous;
            _progressBar.MarqueeAnimationSpeed = 0;
            _progressBar.Value = 0;
            _progressLabel.Text = "";
            foreach (var row in _rows.Values)
                row.SetBusy(false);
            return;
        }

        _busyItemId = itemId;
        _progressHost.Visible = true;
        _progressLabel.Text = text;

        if (percent >= 0)
        {
            _progressBar.Style = ProgressBarStyle.Continuous;
            _progressBar.Minimum = 0;
            _progressBar.Maximum = 100;
            var v = Math.Max(0, Math.Min(100, percent));
            if (v < _progressBar.Value && v < 100)
                v = _progressBar.Value; // 避免回退闪烁
            _progressBar.Value = v;
        }
        else if (total > 0 && current >= 0)
        {
            _progressBar.Style = ProgressBarStyle.Continuous;
            _progressBar.Minimum = 0;
            _progressBar.Maximum = Math.Max(1, total * 100);
            _progressBar.Value = Math.Max(0, Math.Min(_progressBar.Maximum, current * 100));
        }
        else
        {
            _progressBar.Style = ProgressBarStyle.Marquee;
            _progressBar.MarqueeAnimationSpeed = 30;
        }

        foreach (var row in _rows.Values)
        {
            var on = itemId is not null && row.Item.Id.Equals(itemId, StringComparison.OrdinalIgnoreCase);
            row.SetBusy(on, on ? DeriveRowBusyText(text, percent) : null);
        }
    }

    private static string DeriveRowBusyText(string progress, int percent)
    {
        var baseText = progress.IndexOf("卸载", StringComparison.Ordinal) >= 0 ? "卸载中" : "安装中";
        return percent >= 0 ? baseText + " " + percent + "%" : baseText + "…";
    }

    private Action<SoftwareInstallProgress> MakeProgressHandler(string title, string itemId, int batchIndex = -1, int batchTotal = -1)
    {
        return p =>
        {
            var overall = p.Percent;
            if (batchTotal > 0 && batchIndex >= 0)
                overall = (int)Math.Min(99, (batchIndex + p.Percent / 100.0) / batchTotal * 100);

            var text = batchTotal > 0
                ? $"{title}（{batchIndex + 1}/{batchTotal}）· {p.Message}"
                : $"{title} · {p.Message}";

            Ui(() => SetInstallBusy(true, text, itemId: itemId, percent: overall));
        };
    }

    private void Ui(Action action)
    {
        if (IsDisposed) return;
        if (InvokeRequired) BeginInvoke(action);
        else action();
    }

    private void InstallWingetNow()
    {
        if (_installBusy)
        {
            MessageBox.Show(this, "已有安装任务进行中，请稍候。", "常用软件", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (_askBeforeInstall.Checked)
        {
            var answer = MessageBox.Show(this,
                "将下载并安装「应用安装程序」(winget) 及其依赖。\r\n\r\n" +
                (Optimizer.IsWindowsServer()
                    ? "Server：离线包安装 + 部署便携目录 C:\\Tools\\winget（绕过 WindowsApps 别名许可证问题）。\r\n"
                    : "") +
                "安装期间可继续使用主窗口。是否继续？",
                "安装 winget", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer != DialogResult.Yes) return;
        }

        SetInstallBusy(true, "正在安装 winget…", itemId: "winget", percent: 1);
        System.Threading.Tasks.Task.Run(() =>
        {
            string msg;
            Exception? error = null;
            try { msg = CommonSoftwareHelper.InstallWinget(MakeProgressHandler("winget", "winget")); }
            catch (Exception ex)
            {
                error = ex;
                msg = "";
            }

            Ui(() =>
            {
                CommonSoftwareHelper.ResetWingetDiscovery();
                SetInstallBusy(false);
                ReloadStatusesAsync();
                if (error is not null)
                    MessageBox.Show(this, error.Message, "安装 winget 失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else
                    MessageBox.Show(this,
                        string.IsNullOrWhiteSpace(msg) ? "winget 安装完成。" : msg,
                        "安装 winget",
                        MessageBoxButtons.OK,
                        CommonSoftwareHelper.IsWingetAvailable() ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            });
        });
    }

    private void OnInstall(CommonSoftwareItem item)
    {
        if (item.IsWingetBootstrap)
        {
            InstallWingetNow();
            return;
        }

        if (_installBusy)
        {
            MessageBox.Show(this, "已有安装任务进行中，请稍候。", "常用软件", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var pending = CommonSoftwareHelper.FindPendingUpdate(item.Id);
        if (pending is not null)
        {
            if (_askBeforeInstall.Checked)
            {
                var answer = MessageBox.Show(this,
                    $"「{item.Title}」有可用更新：\r\n{pending.CurrentVersion} → {pending.AvailableVersion}\r\n\r\n是否更新？",
                    "软件更新", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (answer != DialogResult.Yes) return;
            }

            RunBatchUpgrade([pending]);
            return;
        }

        var status = CommonSoftwareHelper.Query(item);
        var action = status.Installed ? "修复安装" : "一键安装";
        if (_askBeforeInstall.Checked)
        {
            var answer = MessageBox.Show(this,
                $"即将对「{item.Title}」执行{action}。\r\n\r\n优先使用 winget；失败则打开官方下载页。\r\n安装期间可继续使用主窗口。\r\n是否继续？",
                "常用软件", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer != DialogResult.Yes) return;
        }

        SetInstallBusy(true, "正在安装 " + item.Title + "…", itemId: item.Id, percent: 1);
        System.Threading.Tasks.Task.Run(() =>
        {
            string msg = "";
            Exception? error = null;
            try { msg = CommonSoftwareHelper.Install(item, MakeProgressHandler(item.Title, item.Id)); }
            catch (Exception ex) { error = ex; }

            Ui(() =>
            {
                SetInstallBusy(false);
                ReloadStatusesAsync();
                if (error is not null)
                    MessageBox.Show(this, error.Message, item.Title + " 安装失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else if (!string.IsNullOrWhiteSpace(msg))
                    MessageBox.Show(this, msg, item.Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
        });
    }

    private void OnUninstall(CommonSoftwareItem item)
    {
        if (_installBusy)
        {
            MessageBox.Show(this, "已有安装任务进行中，请稍候。", "常用软件", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var status = CommonSoftwareHelper.Query(item);
        if (!status.Installed)
        {
            MessageBox.Show(this, "该软件当前未安装。", item.Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var answer = MessageBox.Show(this,
            $"确定卸载「{item.Title}」？\r\n当前版本：{status.Version}",
            "卸载确认", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
        if (answer != DialogResult.Yes) return;

        SetInstallBusy(true, "正在卸载 " + item.Title + "…", itemId: item.Id, percent: 1);
        System.Threading.Tasks.Task.Run(() =>
        {
            string msg = "";
            Exception? error = null;
            try { msg = CommonSoftwareHelper.Uninstall(item, MakeProgressHandler("卸载 " + item.Title, item.Id)); }
            catch (Exception ex) { error = ex; }

            Ui(() =>
            {
                SetInstallBusy(false);
                ReloadStatusesAsync();
                if (error is not null)
                    MessageBox.Show(this, error.Message, "卸载失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                else if (msg.Length > 0)
                    MessageBox.Show(this, msg, item.Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                else
                    MessageBox.Show(this, "卸载命令已执行，请稍候刷新状态。", item.Title,
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
        });
    }

    private void SetAllSelected(bool on)
    {
        foreach (var row in _rows.Values)
            row.Selected = on;
    }

    private void SelectBy(Func<CommonSoftwareRow, bool> predicate)
    {
        foreach (var row in _rows.Values)
            row.Selected = predicate(row);
    }

    private void InstallSelected()
    {
        if (_installBusy)
        {
            MessageBox.Show(this, "已有安装任务进行中，请稍候。", "常用软件", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var selected = _rows.Values.Where(r => r.Selected).Select(r => r.Item).ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show(this, "请先勾选要安装的软件。", "安装所选", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var ordered = selected.OrderBy(x => x.IsWingetBootstrap ? 0 : 1).ToList();
        var names = string.Join("\r\n", ordered.Select(x => "· " + x.Title));
        if (_askBeforeInstall.Checked)
        {
            var answer = MessageBox.Show(this,
                $"将并行安装已选的 {ordered.Count} 款软件（最多 3 路同时进行；winget 包自动排队）：\r\n\r\n{names}\r\n\r\n安装期间可继续使用主窗口。是否继续？",
                "安装所选", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer != DialogResult.Yes) return;
        }

        RunBatchInstall(ordered, "安装所选");
    }

    private void CheckSoftwareUpdates()
    {
        if (_installBusy)
        {
            MessageBox.Show(this, "已有安装任务进行中，请稍候。", "常用软件", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!CommonSoftwareHelper.IsWingetAvailable())
        {
            MessageBox.Show(this,
                "未检测到 winget，无法检查更新。\r\n请先在必备列表中安装/修复 winget。",
                "检查更新", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        SetInstallBusy(true, "正在检查软件更新…", percent: 5);
        System.Threading.Tasks.Task.Run(() =>
        {
            List<SoftwareUpdateInfo> updates = [];
            Exception? error = null;
            try
            {
                updates = CommonSoftwareHelper.ListAvailableUpdates(
                    CommonSoftwareCatalog.All,
                    MakeProgressHandler("检查更新", "winget"));
            }
            catch (Exception ex) { error = ex; }

            Ui(() =>
            {
                SetInstallBusy(false);
                RefreshAll();
                if (error is not null)
                {
                    MessageBox.Show(this, error.Message, "检查更新失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (updates.Count == 0)
                {
                    MessageBox.Show(this,
                        "已检查：常用软件列表中的已安装软件暂无可用更新。",
                        "检查更新", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using var dlg = new SoftwareUpdateDialog(updates);
                if (dlg.ShowDialog(this) != DialogResult.OK || dlg.SelectedUpdates.Count == 0)
                    return;

                RunBatchUpgrade(dlg.SelectedUpdates);
            });
        });
    }

    private void RunBatchUpgrade(List<SoftwareUpdateInfo> updates)
    {
        if (_installBusy) return;
        const int maxParallel = 3;
        SetInstallBusy(true, $"准备并行更新（共 {updates.Count} 项，最多 {maxParallel} 路）…", percent: 0);
        System.Threading.Tasks.Task.Run(() =>
        {
            var notes = new ConcurrentBag<string>();
            var done = 0;
            var active = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            void RefreshUi(string? focusId = null)
            {
                var finished = Volatile.Read(ref done);
                var overall = updates.Count == 0 ? 100
                    : (int)Math.Min(99, finished * 100.0 / updates.Count);
                var running = active.Count == 0
                    ? "排队中"
                    : string.Join("、", active.Values.Take(3));
                if (active.Count > 3) running += $" 等{active.Count}项";
                var text = $"并行更新 {finished}/{updates.Count} · {running}";
                Ui(() => SetInstallBusy(true, text, itemId: focusId, percent: overall));
            }

            System.Threading.Tasks.Parallel.ForEach(
                updates,
                new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = maxParallel },
                u =>
                {
                    var item = u.Item;
                    active[item.Id] = item.Title;
                    RefreshUi(item.Id);
                    var progress = MakeParallelProgressHandler(
                        item.Title, item.Id, () => Volatile.Read(ref done), updates.Count, active);
                    try
                    {
                        var msg = CommonSoftwareHelper.Upgrade(item, progress);
                        notes.Add(item.Title + "：" +
                            (string.IsNullOrWhiteSpace(msg)
                                ? $"完成 {u.CurrentVersion} → {u.AvailableVersion}"
                                : msg));
                    }
                    catch (Exception ex)
                    {
                        notes.Add(item.Title + "：失败 — " + ex.Message);
                    }
                    finally
                    {
                        active.TryRemove(item.Id, out _);
                        Interlocked.Increment(ref done);
                        RefreshUi();
                    }
                });

            Ui(() =>
            {
                SetInstallBusy(true, $"全部完成（{updates.Count}/{updates.Count}）", percent: 100);
                SetInstallBusy(false);
                ReloadStatusesAsync();
                MessageBox.Show(this, string.Join("\r\n", notes), "批量更新",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
        });
    }

    private void RunBatchInstall(List<CommonSoftwareItem> items, string title)
    {
        if (_installBusy) return;
        const int maxParallel = 3;
        SetInstallBusy(true, $"准备并行安装（共 {items.Count} 项，最多 {maxParallel} 路）…", percent: 0);
        System.Threading.Tasks.Task.Run(() =>
        {
            var notes = new ConcurrentBag<string>();
            var done = 0;
            var active = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // winget 本体必须先装好，其它包才能走 winget
            var bootstrap = items.Where(x => x.IsWingetBootstrap).ToList();
            var rest = items.Where(x => !x.IsWingetBootstrap).ToList();
            var ordered = bootstrap.Concat(rest).ToList();

            void RefreshUi(string? focusId = null)
            {
                var finished = Volatile.Read(ref done);
                var overall = ordered.Count == 0 ? 100
                    : (int)Math.Min(99, finished * 100.0 / ordered.Count);
                var running = active.Count == 0
                    ? "排队中"
                    : string.Join("、", active.Values.Take(3));
                if (active.Count > 3) running += $" 等{active.Count}项";
                var text = $"并行安装 {finished}/{ordered.Count} · {running}";
                Ui(() => SetInstallBusy(true, text, itemId: focusId, percent: overall));
            }

            void InstallOne(CommonSoftwareItem item)
            {
                active[item.Id] = item.Title;
                RefreshUi(item.Id);
                var progress = MakeParallelProgressHandler(
                    item.Title, item.Id, () => Volatile.Read(ref done), ordered.Count, active);
                try
                {
                    var msg = item.IsWingetBootstrap
                        ? CommonSoftwareHelper.InstallWinget(progress)
                        : CommonSoftwareHelper.Install(item, progress);
                    notes.Add(item.Title + "：" + (string.IsNullOrWhiteSpace(msg) ? "完成" : msg));
                }
                catch (Exception ex)
                {
                    notes.Add(item.Title + "：失败 — " + ex.Message);
                }
                finally
                {
                    active.TryRemove(item.Id, out _);
                    Interlocked.Increment(ref done);
                    RefreshUi();
                }
            }

            foreach (var item in bootstrap)
                InstallOne(item);

            if (rest.Count > 0)
            {
                System.Threading.Tasks.Parallel.ForEach(
                    rest,
                    new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = maxParallel },
                    InstallOne);
            }

            Ui(() =>
            {
                SetInstallBusy(true, $"全部完成（{ordered.Count}/{ordered.Count}）", percent: 100);
                SetInstallBusy(false);
                ReloadStatusesAsync();
                MessageBox.Show(this, string.Join("\r\n", notes), title, MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
        });
    }

    /// <summary>并行批量时的进度：按完成数估算总进度，文案突出当前软件状态。</summary>
    private Action<SoftwareInstallProgress> MakeParallelProgressHandler(
        string title,
        string itemId,
        Func<int> getDone,
        int total,
        ConcurrentDictionary<string, string> active)
    {
        return p =>
        {
            active[itemId] = $"{title}·{p.Message}";
            var finished = getDone();
            var overall = total <= 0 ? p.Percent
                : (int)Math.Min(99, (finished + p.Percent / 100.0) / total * 100);
            var running = string.Join("、", active.Values.Take(2));
            if (active.Count > 2) running += $" 等{active.Count}项";
            var text = $"并行 {finished}/{total} · {running}";
            Ui(() => SetInstallBusy(true, text, itemId: itemId, percent: overall));
        };
    }

    private void InstallEssentials()
    {
        if (_installBusy)
        {
            MessageBox.Show(this, "已有安装任务进行中，请稍候。", "常用软件", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var essentials = CommonSoftwareCatalog.All.Where(x => x.Essential).ToList();
        var missing = essentials
            .Where(x => x.IsWingetBootstrap
                ? !CommonSoftwareHelper.IsWingetAvailable()
                : !CommonSoftwareHelper.Query(x).Installed)
            .OrderBy(x => x.IsWingetBootstrap ? 0 : 1)
            .ToList();
        if (missing.Count == 0)
        {
            MessageBox.Show(this, "系统必备软件均已安装。", "常用软件", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var names = string.Join("\r\n", missing.Select(x => "· " + x.Title));
        if (_askBeforeInstall.Checked)
        {
            var answer = MessageBox.Show(this,
                $"将并行安装以下 {missing.Count} 款必备软件（最多 3 路）：\r\n\r\n{names}\r\n\r\n安装期间可继续使用主窗口。是否继续？",
                "安装系统必备软件", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer != DialogResult.Yes) return;
        }

        RunBatchInstall(missing, "安装系统必备软件");
    }

    private static Label MakeHeaderCell(string text, int x, int w, ContentAlignment align = ContentAlignment.MiddleLeft) => new()
    {
        Text = text,
        Location = new Point(x, 0),
        Size = new Size(w, 36),
        ForeColor = AppTheme.TextHeader,
        Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
        TextAlign = align,
        BackColor = Color.Transparent,
    };

    private sealed class CommonSoftwareRow : Panel
    {
        private readonly CommonSoftwareItem _item;
        private readonly CheckBox _select = new();
        private readonly Button _install;
        private readonly Button _uninstall;
        private readonly Label _status;
        private readonly ToolTip _statusTip = new() { ShowAlways = true };
        private readonly Action<CommonSoftwareItem> _onInstall;
        private readonly Action<CommonSoftwareItem> _onUninstall;
        private bool _busy;

        public CommonSoftwareItem Item => _item;

        public bool IsInstalled { get; private set; }

        public bool Selected
        {
            get => _select.Checked;
            set => _select.Checked = value;
        }

        public CommonSoftwareRow(
            CommonSoftwareItem item,
            int height,
            Color bg,
            Action<CommonSoftwareItem> onInstall,
            Action<CommonSoftwareItem> onUninstall)
        {
            _item = item;
            _onInstall = onInstall;
            _onUninstall = onUninstall;
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.UserPaint
                | ControlStyles.ResizeRedraw,
                true);
            DoubleBuffered = true;
            Height = height;
            BackColor = bg;

            _select.Location = new Point(12, (height - 18) / 2);
            _select.Size = new Size(18, 18);
            _select.BackColor = Color.Transparent;

            var name = new Label
            {
                Text = item.Title,
                Location = new Point(44, 0),
                Size = new Size(300, height),
                ForeColor = AppTheme.TextMain,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
            };

            _install = RowButton("一键安装", 352);
            _install.Click += (_, _) => _onInstall(_item);

            _uninstall = RowButton("卸载", 456);
            _uninstall.Click += (_, _) => _onUninstall(_item);
            if (item.IsWingetBootstrap)
            {
                _uninstall.Enabled = false;
                _uninstall.ForeColor = AppTheme.TextMute;
            }

            _status = new Label
            {
                Location = new Point(536, 10),
                Size = new Size(280, 24),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Microsoft YaHei UI", 8.75F),
                Padding = new Padding(8, 0, 8, 0),
                AutoEllipsis = true,
            };

            Controls.AddRange([_select, name, _install, _uninstall, _status]);
            Paint += (_, e) =>
            {
                using var pen = new Pen(AppTheme.BorderLight);
                e.Graphics.DrawLine(pen, 0, Height - 1, Width, Height - 1);
            };

            SetPendingDetect();
        }

        public void SetPendingDetect()
        {
            _busy = false;
            IsInstalled = false;
            _install.Enabled = false;
            _uninstall.Enabled = false;
            _status.Text = "检测中…";
            _status.ForeColor = AppTheme.TextMute;
            _status.BackColor = AppTheme.Surface;
            _statusTip.SetToolTip(_status, "正在读取安装状态…");
        }

        public void RefreshStatus()
        {
            if (_busy)
            {
                ApplyBusyStatus(_status.Text.Length > 0 ? _status.Text : "安装中…");
                return;
            }

            var s = CommonSoftwareHelper.Query(_item);
            IsInstalled = s.Installed;
            if (_item.IsWingetBootstrap)
            {
                // 包在但命令不可用：提示点「修复安装」
                _install.Text = CommonSoftwareHelper.IsWingetAvailable() ? "修复安装" : "一键安装";
                if (!CommonSoftwareHelper.IsWingetAvailable() && s.Version.Length > 0)
                    _install.Text = "修复安装";
            }

            if (s.Installed)
            {
                if (!_item.IsWingetBootstrap)
                    _install.Text = "修复安装";
                var pending = CommonSoftwareHelper.FindPendingUpdate(_item.Id);
                if (pending is not null)
                {
                    _status.Text = $"可更新 {pending.CurrentVersion} → {pending.AvailableVersion}";
                    _status.ForeColor = AppTheme.ScopeServer;
                    _status.BackColor = AppTheme.PrimaryPale;
                    _install.Text = "立即更新";
                }
                else
                {
                    _status.Text = string.IsNullOrWhiteSpace(s.Version)
                        ? "已安装"
                        : "已安装 · " + s.Version;
                    _status.ForeColor = AppTheme.PrimaryDeep;
                    _status.BackColor = AppTheme.PrimaryPale;
                }
            }
            else if (_item.IsWingetBootstrap && s.Version.Length > 0)
            {
                _status.Text = s.Version;
                _status.ForeColor = AppTheme.ScopeServer;
                _status.BackColor = AppTheme.PrimaryPale;
            }
            else
            {
                if (!_item.IsWingetBootstrap)
                    _install.Text = "一键安装";
                _status.Text = "未安装";
                _status.ForeColor = AppTheme.TextMute;
                _status.BackColor = AppTheme.Surface;
            }

            _statusTip.SetToolTip(_status, _status.Text);
            _install.Enabled = true;
            _uninstall.Enabled = true;
        }

        public void SetBusy(bool busy, string? statusText = null)
        {
            _busy = busy;
            if (busy)
            {
                ApplyBusyStatus(statusText ?? "安装中…");
                return;
            }

            RefreshStatus();
        }

        private void ApplyBusyStatus(string text)
        {
            _install.Enabled = false;
            _uninstall.Enabled = false;
            _status.Text = text;
            _status.ForeColor = AppTheme.ScopeServer;
            _status.BackColor = AppTheme.PrimaryPale;
            _statusTip.SetToolTip(_status, "任务进行中，请稍候…");
        }

        private static Button RowButton(string text, int x)
        {
            var b = new Button
            {
                Text = text,
                Location = new Point(x, 8),
                Size = new Size(text == "卸载" ? 72 : 88, 28),
                FlatStyle = FlatStyle.Flat,
                ForeColor = AppTheme.PrimaryDeep,
                BackColor = AppTheme.SurfaceCard,
                Cursor = Cursors.Hand,
                Font = new Font("Microsoft YaHei UI", 9F),
            };
            b.FlatAppearance.BorderColor = AppTheme.Border;
            b.MouseEnter += (_, _) => b.BackColor = AppTheme.PrimaryPale;
            b.MouseLeave += (_, _) => b.BackColor = AppTheme.SurfaceCard;
            return b;
        }
    }
}
