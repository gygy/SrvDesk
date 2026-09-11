using System.Collections.Concurrent;

namespace SrvDesk;

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
    private string _selectedCategoryKey = "全部";
    private int _categoryHover = -1;
    private string? _busyItemId;

    private static readonly (string Key, string En)[] CategoryDefs =
    [
        ("全部", "All"),
        ("必备", "Essentials"),
        ("AI", "AI"),
        ("微软运行库", "Microsoft runtimes"),
        ("工具", "Tools"),
        ("浏览器", "Browsers"),
        ("通讯", "Comms"),
        ("网盘", "Cloud drives"),
        ("开发", "Dev"),
        ("自定义", "Custom"),
    ];

    private static string CategoryLabel(string key)
    {
        foreach (var c in CategoryDefs)
        {
            if (c.Key.Equals(key, StringComparison.Ordinal))
                return AppLang.L(c.Key, c.En);
        }
        return key;
    }

    private static string CategoryKeyFromLabel(string label)
    {
        foreach (var c in CategoryDefs)
        {
            if (AppLang.L(c.Key, c.En).Equals(label, StringComparison.Ordinal)
                || c.Key.Equals(label, StringComparison.Ordinal))
                return c.Key;
        }
        return label;
    }

    public CommonSoftwareDialog()
    {
        Text = AppLang.L("常用软件", "Common software");
        AppBrand.ApplyWindowIcon(this);
        AutoScaleMode = AutoScaleMode.None;
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
        var mCustom = new ToolStripMenuItem("自定义软件...");
        mCustom.Click += (_, _) => ManageCustomSoftware();
        var mClearCache = new ToolStripMenuItem("清理下载缓存");
        mClearCache.Click += (_, _) =>
        {
            CommonSoftwareHelper.ClearDownloadCache();
            MessageBox.Show(this, "已清理下载临时目录。", "常用软件", MessageBoxButtons.OK, MessageBoxIcon.Information);
        };
        op.DropDownItems.AddRange([
            mInstallSelected, mEssentials, new ToolStripSeparator(), mUpdates, mCustom, mClearCache
        ]);
        var edit = new ToolStripMenuItem("编辑(&E)");
        var mSelectAll = new ToolStripMenuItem("全选当前");
        mSelectAll.Click += (_, _) => SetAllSelected(true);
        var mSelectNone = new ToolStripMenuItem("全不选");
        mSelectNone.Click += (_, _) => SetAllSelected(false);
        edit.DropDownItems.AddRange([mSelectAll, mSelectNone]);
        menu.Items.AddRange([op, edit]);
        menu.Dock = DockStyle.Top;

        var actions = new NoScrollFlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            AutoScroll = false,
            Padding = new Padding(12, 6, 12, 6),
            BackColor = AppTheme.Surface,
        };
        UiBuffer.ConfigureNoScrollRow(actions);
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
            AppLang.L("常用软件", "Common software"),
            AppLang.L(
                "官方下载安装 · 优先 winget，否则取官网安装包",
                "Official installers · prefer winget, else vendor download"),
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
            _wingetHint.Text = AppLang.L("正在检测软件状态…", "Detecting software status…");
            // 勿在此处 WarmUp（winget source update）；等状态刷完后再预热，否则会抢锁导致一直「检测中」
            ReloadStatusesAsync(forceRefresh: false);
        };
        _categoryMenu.SelectedIndexChanged += (_, _) =>
        {
            if (_categoryMenu.SelectedIndex >= 0 && _categoryMenu.SelectedIndex < CategoryDefs.Length)
            {
                _selectedCategoryKey = CategoryDefs[_categoryMenu.SelectedIndex].Key;
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
        var sidebar = NavMenuStyle.CreateSidebar();
        NavMenuStyle.Apply(_categoryMenu);
        _categoryMenu.Items.AddRange(CategoryDefs.Select(c => CategoryLabel(c.Key)).Cast<object>().ToArray());
        _categoryMenu.DrawItem += DrawCategoryItem;
        NavMenuStyle.BindHover(_categoryMenu, () => _categoryHover, v => _categoryHover = v);
        sidebar.Controls.Add(_categoryMenu);
        return sidebar;
    }

    private void DrawCategoryItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || sender is not ListBox box) return;
        NavMenuStyle.DrawItem(
            e,
            box.Items[e.Index]?.ToString() ?? "",
            Font,
            e.Index == _categoryHover);
    }

    private Panel BuildToolStrip()
    {
        var strip = new Panel
        {
            Height = 48,
            BackColor = AppTheme.Surface,
            Padding = new Padding(0, 0, 0, 0),
        };

        _wingetHint.AutoSize = false;
        _wingetHint.Font = UiFit.UiFont;
        _wingetHint.ForeColor = AppTheme.TextMute;
        _wingetHint.TextAlign = ContentAlignment.MiddleLeft;
        _wingetHint.AutoEllipsis = true;

        _installWingetBtn.Text = "一键安装 winget";
        _installWingetBtn.Font = UiFit.UiFont;
        _installWingetBtn.Size = UiFit.ButtonSize("一键安装 winget", UiFit.ControlHeight(), padding: 22);
        _installWingetBtn.FlatStyle = FlatStyle.Flat;
        _installWingetBtn.BackColor = AppTheme.Primary;
        _installWingetBtn.ForeColor = AppTheme.TextOnPrimary;
        _installWingetBtn.Cursor = Cursors.Hand;
        _installWingetBtn.FlatAppearance.BorderSize = 0;
        _installWingetBtn.Visible = false;
        UiFit.EnableCenteredFlatText(_installWingetBtn);
        _installWingetBtn.Click += (_, _) => InstallWingetNow();

        _askBeforeInstall.Text = "安装前确认";
        _askBeforeInstall.Font = UiFit.UiFont;
        _askBeforeInstall.Checked = false;
        _askBeforeInstall.AutoSize = false;
        _askBeforeInstall.ForeColor = AppTheme.TextMain;
        _askBeforeInstall.BackColor = Color.Transparent;
        _askBeforeInstall.FlatStyle = FlatStyle.System;
        _toolTip.SetToolTip(_askBeforeInstall, "勾选后，安装前会弹出确认对话框");

        var selectBtn = ThemedSettingsChrome.CreateButton("选择 ▾", false);
        selectBtn.Font = UiFit.UiFont;
        selectBtn.Padding = new Padding(0);
        UiFit.FitButton(selectBtn, 30, minWidth: 72, padding: 20);
        var selectMenu = new ContextMenuStrip();
        selectMenu.Items.Add("全选当前分类", null, (_, _) => SetAllSelected(true));
        selectMenu.Items.Add("全不选", null, (_, _) => SetAllSelected(false));
        selectMenu.Items.Add(new ToolStripSeparator());
        selectMenu.Items.Add("仅选必备", null, (_, _) => SelectBy(r => r.Item.Essential));
        selectMenu.Items.Add("仅选运行库推荐", null, (_, _) =>
            SelectBy(r => r.Item.Id.Equals("vcredist-2022-x64", StringComparison.OrdinalIgnoreCase)));
        selectMenu.Items.Add("仅选未安装", null, (_, _) => SelectBy(r => !r.IsInstalled));
        selectBtn.Click += (_, _) => selectMenu.Show(selectBtn, new Point(0, selectBtn.Height));
        _toolTip.SetToolTip(selectBtn, "快速勾选，配合「安装所选」");

        var customBtn = ThemedSettingsChrome.CreateButton("自定义…", false);
        customBtn.Font = UiFit.UiFont;
        UiFit.FitButton(customBtn, 30, minWidth: 88, padding: 20);
        customBtn.Click += (_, _) => ManageCustomSoftware();
        _toolTip.SetToolTip(customBtn, "添加可用 winget 安装的软件");

        // 不用 FlowLayout：窄宽 + 隐藏滚动条时易把按钮顶部裁掉
        strip.Controls.AddRange([_wingetHint, _installWingetBtn, _askBeforeInstall, selectBtn, customBtn]);

        void LayoutStrip()
        {
            const int y = 9;
            const int gap = 10;
            // 右侧留足边距，避免「自定义…」贴边被裁
            var right = Math.Max(0, strip.ClientSize.Width - UiScale.S(14));

            UiFit.FitButton(customBtn, 30, minWidth: 88, padding: 20);
            UiFit.FitButton(selectBtn, 30, minWidth: 72, padding: 20);

            customBtn.Location = new Point(right - customBtn.Width, y);
            right = customBtn.Left - gap;
            selectBtn.Location = new Point(right - selectBtn.Width, y);
            right = selectBtn.Left - gap;

            // 勾选框宽度按实际字形量，避免 PreferredSize 偏小导致文字伸进右侧按钮下
            var askTextW = TextRenderer.MeasureText(
                _askBeforeInstall.Text,
                _askBeforeInstall.Font ?? UiFit.UiFont,
                new Size(int.MaxValue, 32),
                TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding).Width;
            var askW = askTextW + UiScale.S(22);
            var askH = Math.Max(22, UiFit.LineHeight(_askBeforeInstall.Font));
            _askBeforeInstall.Size = new Size(askW, askH);
            _askBeforeInstall.Location = new Point(right - askW, y + (30 - askH) / 2);
            right = _askBeforeInstall.Left - gap;

            if (_installWingetBtn.Visible)
            {
                _installWingetBtn.Size = UiFit.ButtonSize(_installWingetBtn.Text, UiFit.ControlHeight(), padding: 22);
                _installWingetBtn.Location = new Point(Math.Max(0, right - _installWingetBtn.Width), y);
                right = _installWingetBtn.Left - gap;
            }

            _wingetHint.SetBounds(0, y, Math.Max(60, right), 30);
            // 保证右侧控件在提示文字之上，不被盖住
            customBtn.BringToFront();
            selectBtn.BringToFront();
            _askBeforeInstall.BringToFront();
            if (_installWingetBtn.Visible)
                _installWingetBtn.BringToFront();
        }

        strip.Resize += (_, _) => LayoutStrip();
        _installWingetBtn.VisibleChanged += (_, _) => LayoutStrip();
        strip.HandleCreated += (_, _) => LayoutStrip();
        LayoutStrip();
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
        header.Controls.Add(MakeHeaderCell(AppLang.L("选", "Sel"), 8, 36, ContentAlignment.MiddleCenter));
        header.Controls.Add(MakeHeaderCell(AppLang.L("软件名称", "Name"), 44, 300));
        header.Controls.Add(MakeHeaderCell(AppLang.L("安装", "Install"), 352, 96, ContentAlignment.MiddleCenter));
        header.Controls.Add(MakeHeaderCell(AppLang.L("卸载", "Uninstall"), 456, 72, ContentAlignment.MiddleCenter));
        header.Controls.Add(MakeHeaderCell(AppLang.L("状态", "Status"), 536, 300));
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

        var items = _selectedCategoryKey == "全部"
            ? CommonSoftwareCatalog.GetAll()
            : CommonSoftwareCatalog.GetAll().Where(x => x.Category == _selectedCategoryKey).ToList();

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

    private void ManageCustomSoftware()
    {
        using var dlg = new CustomSoftwareManageDialog();
        dlg.ShowDialog(this);
        if (!dlg.Changed) return;
        if (_selectedCategoryKey == CommonSoftwareCatalog.CustomCategory
            || _selectedCategoryKey == "全部")
        {
            BuildList();
            ReloadStatusesAsync(forceRefresh: true);
        }
        else
        {
            // 切到自定义分类方便查看
            var idx = Array.FindIndex(CategoryDefs, c => c.Key == CommonSoftwareCatalog.CustomCategory);
            if (idx >= 0) _categoryMenu.SelectedIndex = idx;
        }
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
        var gen = ++_statusLoadGen;
        var done = false;
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
            finally
            {
                done = true;
                if (gen == _statusLoadGen)
                    Ui(() =>
                    {
                        RefreshAll();
                        CommonSoftwareHelper.WarmUpInBackground();
                    });
            }
        });

        // 看门狗：最长约 12s 必须结束「检测中」，避免个别探测挂死 UI
        System.Threading.Tasks.Task.Delay(12_000).ContinueWith(_ =>
        {
            if (done || gen != _statusLoadGen) return;
            Ui(() =>
            {
                if (gen != _statusLoadGen) return;
                RefreshAll();
                _wingetHint.Text = (_wingetHint.Text ?? "").Contains("winget")
                    ? _wingetHint.Text
                    : "状态检测超时，已显示当前结果；可点刷新重试。";
            });
        });
    }

    private int _statusLoadGen;

    private void RefreshAll()
    {
        // 不每次重置探测：重复 Probe winget 很慢；安装/修复后再 Reset
        var wingetOk = CommonSoftwareHelper.IsWingetAvailable();
        _installWingetBtn.Visible = !wingetOk;
        _wingetHint.Text = wingetOk
            ? "已检测到 winget · 可静默安装"
            : "未检测到 winget：有 winget 的项走商店源，其余仍可从官网下载安装。";

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
                "将按 asheroto/winget-install 的路径安装「应用安装程序」(winget)。\r\n\r\n" +
                (Optimizer.IsWindowsServer()
                    ? "Server 2019/2022：依赖包 + 许可证预配安装，并修正 PATH/目录权限。\r\n桌面或 Server 2025：先 Repair-WinGetPackageManager，失败再走离线预配。\r\n"
                    : "先 Repair-WinGetPackageManager（微软官方修复），失败再下载离线包预配。\r\n") +
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
                    ShowNotice("安装 winget 失败", error.Message, MessageBoxIcon.Error);
                else
                    ShowNotice(
                        "安装 winget",
                        string.IsNullOrWhiteSpace(msg) ? "winget 安装完成。" : msg,
                        CommonSoftwareHelper.IsWingetAvailable() ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            });
        });
    }

    private void ShowNotice(string title, string text, MessageBoxIcon icon)
    {
        var wrapWidth = 520;
        var flags = TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPrefix;
        var measured = TextRenderer.MeasureText(text ?? "", Font, new Size(wrapWidth - 8, int.MaxValue), flags);
        var work = Screen.FromControl(this).WorkingArea;
        // 限高 + 可滚动，避免批量安装结果撑满半屏
        var maxTextH = Math.Min(360, Math.Max(160, (int)(work.Height * 0.45)));
        var textH = Math.Min(Math.Max(measured.Height + 12, 64), maxTextH);
        using var f = new Form
        {
            Text = title,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            AutoScaleMode = AutoScaleMode.Font,
            Font = Font,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextMain,
            ClientSize = new Size(wrapWidth + 40, textH + 72),
        };
        var box = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackColor = AppTheme.Surface,
            ForeColor = AppTheme.TextMain,
            Location = new Point(16, 16),
            Size = new Size(wrapWidth, textH),
            Text = text ?? "",
            ScrollBars = ScrollBars.Vertical,
            TabStop = false,
            WordWrap = true,
        };
        var ok = ThemedSettingsChrome.CreateButton("确定", true);
        ok.DialogResult = DialogResult.OK;
        ok.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        ok.Location = new Point(f.ClientSize.Width - 16 - ok.Width, f.ClientSize.Height - 16 - ok.Height);
        f.Controls.Add(box);
        f.Controls.Add(ok);
        f.AcceptButton = ok;
        f.CancelButton = ok;
        _ = icon;
        f.ShowDialog(this);
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
                $"即将对「{item.Title}」执行{action}。\r\n\r\n优先 winget；失败则自动从官网下载最新安装包并静默安装。\r\n都失败才会打开下载页。\r\n安装期间可继续使用主窗口。\r\n是否继续？",
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
                ShowNotice("批量更新", string.Join("\r\n", notes), MessageBoxIcon.Information);
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
                ShowNotice(title, string.Join("\r\n", notes), MessageBoxIcon.Information);
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
            // 行内有 CheckBox/Button：不要开 UserPaint，否则子控件易残影/只剩边线
            SetStyle(
                ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw,
                true);
            DoubleBuffered = true;
            Height = height;
            BackColor = bg;

            _select.AutoSize = false;
            _select.Text = "";
            _select.Size = new Size(16, 16);
            _select.Location = new Point(14, (height - 16) / 2);
            _select.BackColor = bg;
            _select.FlatStyle = FlatStyle.Standard;
            _select.UseVisualStyleBackColor = true;

            var name = new SingleLineLabel
            {
                Text = item.Title,
                Font = UiFit.UiFont,
                Location = new Point(44, 0),
                Size = new Size(300, height),
                ForeColor = AppTheme.TextMain,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
            };

            var btnY = Math.Max(6, (height - 28) / 2);
            _install = RowButton("一键安装", 352, btnY);
            _install.Click += (_, _) => _onInstall(_item);

            _uninstall = RowButton("卸载", 352 + _install.Width + 12, btnY);
            _uninstall.Click += (_, _) => _onUninstall(_item);
            if (item.IsWingetBootstrap)
            {
                _uninstall.Enabled = false;
                _uninstall.ForeColor = AppTheme.TextMute;
            }

            _status = new SingleLineLabel
            {
                Location = new Point(_uninstall.Right + 12, 0),
                Size = new Size(Math.Max(160, 280), height),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Microsoft YaHei UI", 8.75F),
                Padding = new Padding(8, 0, 8, 0),
                BackColor = Color.Transparent,
            };

            Controls.AddRange([_select, name, _install, _uninstall, _status]);
            Resize += (_, _) =>
            {
                if (Width <= 0) return;
                _status.Width = Math.Max(120, Width - _status.Left - 12);
                _status.Height = Height;
                name.Height = Height;
                _select.Top = (Height - _select.Height) / 2;
                var by = Math.Max(6, (Height - _install.Height) / 2);
                _install.Top = by;
                _uninstall.Top = by;
            };
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

        private static Button RowButton(string text, int x, int y)
        {
            var font = UiFit.UiFont;
            var b = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = UiFit.ButtonSize(text, UiFit.ControlHeight(font), font, minWidth: 72, padding: 20),
                FlatStyle = FlatStyle.Flat,
                ForeColor = AppTheme.PrimaryDeep,
                BackColor = AppTheme.SurfaceCard,
                Cursor = Cursors.Hand,
                Font = font,
                TextAlign = ContentAlignment.MiddleCenter,
                UseVisualStyleBackColor = false,
                UseCompatibleTextRendering = false,
                Padding = Padding.Empty,
            };
            b.FlatAppearance.BorderColor = AppTheme.Border;
            b.MouseEnter += (_, _) => { b.BackColor = AppTheme.PrimaryPale; b.Invalidate(); };
            b.MouseLeave += (_, _) => { b.BackColor = AppTheme.SurfaceCard; b.Invalidate(); };
            UiFit.EnableCenteredFlatText(b);
            return b;
        }
    }
}
