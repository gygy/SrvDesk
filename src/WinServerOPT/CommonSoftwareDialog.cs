namespace WinOpt;

internal sealed class CommonSoftwareDialog : Form
{
    private readonly CheckBox _askBeforeInstall = new();
    private readonly Panel _listHost = new();
    private readonly Label _subtitle = new();
    private readonly Label _wingetHint = new();
    private readonly Button _installWingetBtn = new();
    private readonly ListBox _categoryMenu = new();
    private readonly Dictionary<string, CommonSoftwareRow> _rows = new(StringComparer.OrdinalIgnoreCase);
    private string _selectedCategory = "全部";
    private int _categoryHover = -1;

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
        Font = new Font("Microsoft YaHei UI", 9F);
        BackColor = AppTheme.Surface;
        ForeColor = AppTheme.TextMain;

        var header = BuildHeader();
        header.Dock = DockStyle.Top;

        var bottom = BuildBottomBar();
        bottom.Dock = DockStyle.Bottom;

        var body = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Surface,
        };

        var sidebar = BuildSidebar();
        sidebar.Dock = DockStyle.Left;

        var main = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Surface,
            Padding = new Padding(12, 8, 12, 8),
        };

        var toolStrip = BuildToolStrip();
        toolStrip.Dock = DockStyle.Top;

        var tableWrap = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SurfaceCard,
            Padding = new Padding(0),
        };
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

        body.Controls.Add(main);
        body.Controls.Add(sidebar);

        Controls.Add(body);
        Controls.Add(bottom);
        Controls.Add(header);

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

    private Panel BuildHeader()
    {
        var header = new Panel { Height = 52, BackColor = AppTheme.PrimaryDeep };
        header.Paint += (_, e) =>
        {
            var r = header.ClientRectangle;
            using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                r, AppTheme.HeaderBarTop, AppTheme.HeaderBarBottom, 90f);
            e.Graphics.FillRectangle(brush, r);
        };

        var logo = new PictureBox
        {
            Size = new Size(36, 36),
            Location = new Point(14, 8),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Transparent,
        };
        var logoImg = AppBrand.LoadLogoImage();
        if (logoImg is not null) logo.Image = logoImg;

        var title = new Label
        {
            Text = "常用软件",
            AutoSize = false,
            Location = new Point(54, 6),
            Size = new Size(240, 28),
            ForeColor = AppTheme.TextOnPrimary,
            Font = new Font("Microsoft YaHei UI", 12.5F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = Color.Transparent,
        };

        _subtitle.AutoSize = false;
        _subtitle.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _subtitle.Location = new Point(54, 32);
        _subtitle.Height = 18;
        _subtitle.ForeColor = AppTheme.TextOnPrimarySoft;
        _subtitle.Font = new Font("Microsoft YaHei UI", 8.5F);
        _subtitle.TextAlign = ContentAlignment.MiddleLeft;
        _subtitle.BackColor = Color.Transparent;
        _subtitle.Text = "官方源下载与安装 · 优先 winget";

        header.Controls.Add(_subtitle);
        header.Controls.Add(title);
        header.Controls.Add(logo);
        header.Resize += (_, _) => _subtitle.Width = Math.Max(200, header.Width - 68);
        return header;
    }

    private Panel BuildSidebar()
    {
        var sidebar = new Panel { Width = 160, BackColor = AppTheme.NavBg };

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
        _categoryMenu.ItemHeight = 44;
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

        _wingetHint.AutoSize = false;
        _wingetHint.Location = new Point(0, 10);
        _wingetHint.Size = new Size(360, 22);
        _wingetHint.ForeColor = AppTheme.TextMute;

        _installWingetBtn.Text = "一键安装 winget";
        _installWingetBtn.Location = new Point(368, 6);
        _installWingetBtn.Size = new Size(120, 28);
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
        _askBeforeInstall.Location = new Point(500, 10);
        _askBeforeInstall.ForeColor = AppTheme.TextMain;

        strip.Controls.Add(_wingetHint);
        strip.Controls.Add(_installWingetBtn);
        strip.Controls.Add(_askBeforeInstall);
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
        header.Controls.Add(MakeHeaderCell("软件名称", 16, 320));
        header.Controls.Add(MakeHeaderCell("安装", 344, 96, ContentAlignment.MiddleCenter));
        header.Controls.Add(MakeHeaderCell("卸载", 448, 72, ContentAlignment.MiddleCenter));
        header.Controls.Add(MakeHeaderCell("状态", 528, 320));
        header.Resize += (_, _) => header.Invalidate();
        return header;
    }

    private Panel BuildBottomBar()
    {
        var bar = new Panel
        {
            Height = 52,
            BackColor = AppTheme.SurfaceCard,
            Padding = new Padding(12, 8, 12, 8),
        };
        bar.Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.BorderLight);
            e.Graphics.DrawLine(pen, 0, 0, bar.Width, 0);
        };

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = AppTheme.SurfaceCard,
        };

        var close = ActionButton("关闭", () => Close(), primary: true);
        close.DialogResult = DialogResult.Cancel;
        CancelButton = close;

        flow.Controls.Add(close);
        flow.Controls.Add(ActionButton("安装系统必备", InstallEssentials, primary: false));
        flow.Controls.Add(ActionButton("检查软件更新", () =>
        {
            var msg = CommonSoftwareHelper.CheckUpdates(CommonSoftwareCatalog.All);
            MessageBox.Show(this, msg, "检查更新", MessageBoxButtons.OK, MessageBoxIcon.Information);
            RefreshAll();
        }, primary: false));
        flow.Controls.Add(ActionButton("清理下载临时文件", () =>
        {
            CommonSoftwareHelper.ClearDownloadCache();
            MessageBox.Show(this, "已清理下载临时目录。", "常用软件", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }, primary: false));

        bar.Controls.Add(flow);
        return bar;
    }

    private void BuildList()
    {
        _listHost.SuspendLayout();
        _listHost.Controls.Clear();
        _rows.Clear();

        var items = _selectedCategory == "全部"
            ? CommonSoftwareCatalog.All
            : CommonSoftwareCatalog.All.Where(x => x.Category == _selectedCategory).ToList();

        const int rowH = 44;
        var y = 0;
        var alt = false;
        foreach (var item in items)
        {
            var row = new CommonSoftwareRow(item, rowH, alt ? AppTheme.RowAlt : AppTheme.SurfaceCard, OnInstall, OnUninstall);
            row.Location = new Point(0, y);
            row.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            row.Width = Math.Max(680, _listHost.ClientSize.Width - 4);
            _listHost.Controls.Add(row);
            _rows[item.Id] = row;
            y += rowH;
            alt = !alt;
        }

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
        var wingetOk = CommonSoftwareHelper.IsWingetAvailable();
        _installWingetBtn.Visible = !wingetOk;
        _wingetHint.Text = wingetOk
            ? "已检测到 winget，可一键静默安装其它软件。"
            : "未检测到 winget，请先安装后再使用一键安装其它软件。";
        _subtitle.Text = wingetOk
            ? "官方源下载与安装 · 已启用 winget"
            : "官方源下载与安装 · 需先安装 winget";

        if (_categoryMenu.SelectedIndex < 0) _categoryMenu.SelectedIndex = 0;
        if (_rows.Count == 0) BuildList();
        foreach (var row in _rows.Values)
            row.RefreshStatus();
    }

    private void InstallWingetNow()
    {
        var winget = CommonSoftwareCatalog.Find("winget");
        if (winget is null) return;

        if (_askBeforeInstall.Checked)
        {
            var answer = MessageBox.Show(this,
                "将下载并安装「应用安装程序」(winget) 及其依赖。\r\n\r\n" +
                "依次尝试：PowerShell 修复模块 → 官方离线包 → 注册别名。\r\n" +
                "Server 环境可能需要数分钟，是否继续？",
                "安装 winget", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer != DialogResult.Yes) return;
        }

        UseWaitCursor = true;
        _installWingetBtn.Enabled = false;
        try
        {
            var msg = CommonSoftwareHelper.InstallWinget();
            RefreshAll();
            MessageBox.Show(this,
                string.IsNullOrWhiteSpace(msg) ? "winget 安装完成。" : msg,
                "安装 winget",
                MessageBoxButtons.OK,
                CommonSoftwareHelper.IsWingetAvailable() ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "安装 winget 失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            UseWaitCursor = false;
            _installWingetBtn.Enabled = true;
        }
    }

    private void OnInstall(CommonSoftwareItem item)
    {
        if (item.IsWingetBootstrap)
        {
            InstallWingetNow();
            return;
        }

        var status = CommonSoftwareHelper.Query(item);
        var action = status.Installed ? "修复安装" : "一键安装";
        if (_askBeforeInstall.Checked)
        {
            var answer = MessageBox.Show(this,
                $"即将对「{item.Title}」执行{action}。\r\n\r\n优先使用 winget；失败则打开官方下载页。\r\n是否继续？",
                "常用软件", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer != DialogResult.Yes) return;
        }

        UseWaitCursor = true;
        try
        {
            var msg = CommonSoftwareHelper.Install(item);
            RefreshAll();
            if (msg.Length > 0)
                MessageBox.Show(this, msg, item.Title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "安装失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private void OnUninstall(CommonSoftwareItem item)
    {
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

        UseWaitCursor = true;
        try
        {
            var msg = CommonSoftwareHelper.Uninstall(item);
            RefreshAll();
            if (msg.Length > 0)
                MessageBox.Show(this, msg, item.Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            else
                MessageBox.Show(this, "卸载命令已执行，请稍候刷新状态。", item.Title,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "卸载失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private void InstallEssentials()
    {
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
                $"将依次安装以下 {missing.Count} 款必备软件：\r\n\r\n{names}\r\n\r\n是否继续？",
                "安装系统必备软件", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer != DialogResult.Yes) return;
        }

        UseWaitCursor = true;
        var notes = new List<string>();
        try
        {
            foreach (var item in missing)
            {
                var msg = CommonSoftwareHelper.Install(item);
                if (msg.Length > 0) notes.Add(item.Title + "：" + msg);
            }
            RefreshAll();
            MessageBox.Show(this,
                notes.Count == 0 ? "必备软件安装流程已完成，请查看列表状态。" : string.Join("\r\n\r\n", notes),
                "安装系统必备软件", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        finally
        {
            UseWaitCursor = false;
        }
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

    private static Button ActionButton(string text, Action click, bool primary)
    {
        var b = new Button
        {
            Text = text,
            AutoSize = true,
            Height = 32,
            MinimumSize = new Size(72, 32),
            Padding = new Padding(10, 0, 10, 0),
            Margin = new Padding(6, 0, 0, 0),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            BackColor = primary ? AppTheme.Primary : AppTheme.SurfaceCard,
            ForeColor = primary ? AppTheme.TextOnPrimary : AppTheme.TextMain,
        };
        if (primary) b.FlatAppearance.BorderSize = 0;
        else
        {
            b.FlatAppearance.BorderColor = AppTheme.Border;
            b.MouseEnter += (_, _) => b.BackColor = AppTheme.PrimaryPale;
            b.MouseLeave += (_, _) => b.BackColor = AppTheme.SurfaceCard;
        }
        b.Click += (_, _) => click();
        return b;
    }

    private sealed class CommonSoftwareRow : Panel
    {
        private readonly CommonSoftwareItem _item;
        private readonly Button _install;
        private readonly Button _uninstall;
        private readonly Label _status;
        private readonly Action<CommonSoftwareItem> _onInstall;
        private readonly Action<CommonSoftwareItem> _onUninstall;

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

            var name = new Label
            {
                Text = item.Title,
                Location = new Point(16, 0),
                Size = new Size(316, height),
                ForeColor = AppTheme.TextMain,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
            };

            _install = RowButton("一键安装", 344);
            _install.Click += (_, _) => _onInstall(_item);

            _uninstall = RowButton("卸载", 448);
            _uninstall.Click += (_, _) => _onUninstall(_item);
            if (item.IsWingetBootstrap)
            {
                _uninstall.Enabled = false;
                _uninstall.ForeColor = AppTheme.TextMute;
            }

            _status = new Label
            {
                Location = new Point(528, 10),
                Size = new Size(300, 24),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Microsoft YaHei UI", 8.75F),
                Padding = new Padding(8, 0, 8, 0),
            };

            Controls.AddRange([name, _install, _uninstall, _status]);
            Paint += (_, e) =>
            {
                using var pen = new Pen(AppTheme.BorderLight);
                e.Graphics.DrawLine(pen, 0, Height - 1, Width, Height - 1);
            };

            RefreshStatus();
        }

        public void RefreshStatus()
        {
            var s = CommonSoftwareHelper.Query(_item);
            if (_item.IsWingetBootstrap)
                _install.Text = CommonSoftwareHelper.IsWingetAvailable() ? "修复安装" : "一键安装";

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
