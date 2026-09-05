namespace WinOpt;

internal sealed class CommonSoftwareDialog : Form
{
    private readonly CheckBox _askBeforeInstall = new();
    private readonly Panel _listHost = new();
    private readonly Label _wingetHint = new();
    private readonly Button _installWingetBtn = new();
    private readonly ListBox _categoryMenu = new();
    private readonly Dictionary<string, CommonSoftwareRow> _rows = new(StringComparer.OrdinalIgnoreCase);
    private readonly Panel _progressHost = new();
    private readonly Label _progressLabel = new();
    private readonly ProgressBar _progressBar = new();
    private readonly List<Control> _actionButtons = [];
    private string _selectedCategory = "全部";
    private int _categoryHover = -1;
    private string? _busyItemId;

    private static readonly string[] Categories = ["全部", "必备", "工具", "浏览器", "通讯", "网盘", "开发"];

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

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(12, 6, 12, 6),
            BackColor = AppTheme.Surface,
        };
        actions.Controls.Add(TrackAction(MkBtn("清理下载缓存", () =>
        {
            CommonSoftwareHelper.ClearDownloadCache();
            MessageBox.Show(this, "已清理下载临时目录。", "常用软件", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }, false)));
        actions.Controls.Add(TrackAction(MkBtn("检查软件更新", () =>
        {
            var msg = CommonSoftwareHelper.CheckUpdates(CommonSoftwareCatalog.All);
            MessageBox.Show(this, msg, "检查更新", MessageBoxButtons.OK, MessageBoxIcon.Information);
            RefreshAll();
        }, false)));
        actions.Controls.Add(TrackAction(MkBtn("安装系统必备", InstallEssentials, false)));
        actions.Controls.Add(TrackAction(MkBtn("安装所选", InstallSelected, true)));
        actions.Controls.Add(TrackAction(MkBtn("全选当前", () => SetAllSelected(true), false)));
        actions.Controls.Add(TrackAction(MkBtn("全不选", () => SetAllSelected(false), false)));

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

        ThemedSettingsChrome.MountModal(
            this,
            "常用软件",
            "官方源下载与安装 · 优先 winget",
            body,
            "安装前请确认来源可信；Server 环境 winget 需先安装应用安装程序。",
            RefreshAll);

        Load += (_, _) => RefreshAll();
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
        _progressBar.Style = ProgressBarStyle.Marquee;
        _progressBar.MarqueeAnimationSpeed = 30;

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
        _wingetHint.Size = new Size(360, 24);
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

        _askBeforeInstall.Text = "安装前询问确认";
        _askBeforeInstall.Checked = true;
        _askBeforeInstall.AutoSize = true;
        _askBeforeInstall.Margin = new Padding(0, 8, 0, 0);
        _askBeforeInstall.ForeColor = AppTheme.TextMain;

        flow.Controls.Add(_wingetHint);
        flow.Controls.Add(_installWingetBtn);
        flow.Controls.Add(_askBeforeInstall);
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

    private void RefreshAll()
    {
        CommonSoftwareHelper.ResetWingetDiscovery();
        var wingetOk = CommonSoftwareHelper.IsWingetAvailable();
        _installWingetBtn.Visible = !wingetOk;
        _wingetHint.Text = wingetOk
            ? "已检测到 winget，可一键静默安装其它软件。"
            : "未检测到 winget，请先安装后再使用一键安装其它软件。";

        if (_categoryMenu.SelectedIndex < 0) _categoryMenu.SelectedIndex = 0;
        if (_rows.Count == 0) BuildList();
        foreach (var row in _rows.Values)
            row.RefreshStatus();
    }

    private bool _installBusy;

    private void SetInstallBusy(bool busy, string? progress = null, int current = -1, int total = -1, string? itemId = null)
    {
        _installBusy = busy;
        _installWingetBtn.Enabled = !busy;
        foreach (var btn in _actionButtons)
            btn.Enabled = !busy;

        var text = string.IsNullOrWhiteSpace(progress) ? (busy ? "处理中…" : "") : progress!;
        Text = busy ? "常用软件 — " + text : "常用软件";
        Cursor = Cursors.Default;
        UseWaitCursor = false;

        if (!busy)
        {
            _busyItemId = null;
            _progressHost.Visible = false;
            _progressBar.Style = ProgressBarStyle.Marquee;
            _progressBar.MarqueeAnimationSpeed = 0;
            _progressLabel.Text = "";
            foreach (var row in _rows.Values)
                row.SetBusy(false);
            return;
        }

        _busyItemId = itemId;
        _progressHost.Visible = true;
        _progressLabel.Text = text;
        if (total > 0 && current >= 0)
        {
            _progressBar.Style = ProgressBarStyle.Continuous;
            _progressBar.Minimum = 0;
            _progressBar.Maximum = Math.Max(1, total);
            _progressBar.Value = Math.Max(0, Math.Min(current, total));
        }
        else
        {
            _progressBar.Style = ProgressBarStyle.Marquee;
            _progressBar.MarqueeAnimationSpeed = 30;
        }

        foreach (var row in _rows.Values)
        {
            var on = itemId is not null && row.Item.Id.Equals(itemId, StringComparison.OrdinalIgnoreCase);
            row.SetBusy(on, on ? DeriveRowBusyText(text) : null);
        }
    }

    private static string DeriveRowBusyText(string progress)
    {
        if (progress.IndexOf("卸载", StringComparison.Ordinal) >= 0)
            return "卸载中…";
        return "安装中…";
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
                "Server 环境可能需要数分钟。安装期间可继续使用主窗口。\r\n是否继续？",
                "安装 winget", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer != DialogResult.Yes) return;
        }

        SetInstallBusy(true, "正在安装 winget（下载/注册可能需要几分钟）…", itemId: "winget");
        System.Threading.Tasks.Task.Run(() =>
        {
            string msg;
            Exception? error = null;
            try { msg = CommonSoftwareHelper.InstallWinget(); }
            catch (Exception ex)
            {
                error = ex;
                msg = "";
            }

            Ui(() =>
            {
                SetInstallBusy(false);
                RefreshAll();
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

        var status = CommonSoftwareHelper.Query(item);
        var action = status.Installed ? "修复安装" : "一键安装";
        if (_askBeforeInstall.Checked)
        {
            var answer = MessageBox.Show(this,
                $"即将对「{item.Title}」执行{action}。\r\n\r\n优先使用 winget；失败则打开官方下载页。\r\n安装期间可继续使用主窗口。\r\n是否继续？",
                "常用软件", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer != DialogResult.Yes) return;
        }

        SetInstallBusy(true, "正在安装 " + item.Title + "…", itemId: item.Id);
        System.Threading.Tasks.Task.Run(() =>
        {
            string msg = "";
            Exception? error = null;
            try { msg = CommonSoftwareHelper.Install(item); }
            catch (Exception ex) { error = ex; }

            Ui(() =>
            {
                SetInstallBusy(false);
                RefreshAll();
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

        SetInstallBusy(true, "正在卸载 " + item.Title + "…", itemId: item.Id);
        System.Threading.Tasks.Task.Run(() =>
        {
            string msg = "";
            Exception? error = null;
            try { msg = CommonSoftwareHelper.Uninstall(item); }
            catch (Exception ex) { error = ex; }

            Ui(() =>
            {
                SetInstallBusy(false);
                RefreshAll();
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
                $"将依次安装已选的 {ordered.Count} 款软件：\r\n\r\n{names}\r\n\r\n安装期间可继续使用主窗口。是否继续？",
                "安装所选", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer != DialogResult.Yes) return;
        }

        RunBatchInstall(ordered, "安装所选");
    }

    private void RunBatchInstall(List<CommonSoftwareItem> items, string title)
    {
        if (_installBusy) return;
        SetInstallBusy(true, $"准备安装（共 {items.Count} 项）…", current: 0, total: items.Count);
        System.Threading.Tasks.Task.Run(() =>
        {
            var notes = new List<string>();
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var n = i + 1;
                Ui(() => SetInstallBusy(true,
                    $"正在安装 {item.Title}（{n}/{items.Count}）…",
                    current: n - 1,
                    total: items.Count,
                    itemId: item.Id));
                try
                {
                    var msg = item.IsWingetBootstrap
                        ? CommonSoftwareHelper.InstallWinget()
                        : CommonSoftwareHelper.Install(item);
                    notes.Add(item.Title + "：" + (string.IsNullOrWhiteSpace(msg) ? "完成" : msg));
                }
                catch (Exception ex)
                {
                    notes.Add(item.Title + "：失败 — " + ex.Message);
                }
            }

            Ui(() =>
            {
                SetInstallBusy(true, $"全部完成（{items.Count}/{items.Count}）", current: items.Count, total: items.Count);
                SetInstallBusy(false);
                RefreshAll();
                MessageBox.Show(this, string.Join("\r\n", notes), title, MessageBoxButtons.OK, MessageBoxIcon.Information);
            });
        });
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
                $"将依次安装以下 {missing.Count} 款必备软件：\r\n\r\n{names}\r\n\r\n安装期间可继续使用主窗口。是否继续？",
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

            RefreshStatus();
        }

        public void RefreshStatus()
        {
            if (_busy)
            {
                ApplyBusyStatus(_status.Text.Length > 0 ? _status.Text : "安装中…");
                return;
            }

            var s = CommonSoftwareHelper.Query(_item);
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
                _status.Text = string.IsNullOrWhiteSpace(s.Version)
                    ? "已安装"
                    : "已安装 · " + s.Version;
                _status.ForeColor = AppTheme.PrimaryDeep;
                _status.BackColor = AppTheme.PrimaryPale;
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
