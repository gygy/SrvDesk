namespace WinOpt;

internal sealed class CommonSoftwareDialog : Form
{
    private readonly CheckBox _askBeforeInstall = new();
    private readonly Panel _listHost = new();
    private readonly Label _wingetHint = new();
    private readonly Dictionary<string, CommonSoftwareRow> _rows = new(StringComparer.OrdinalIgnoreCase);

    public CommonSoftwareDialog()
    {
        Text = "常用软件";
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(920, 620);
        MinimumSize = new Size(780, 520);
        Font = new Font("Microsoft YaHei UI", 9F);
        BackColor = AppTheme.Surface;

        var sidebar = BuildSidebar();
        sidebar.Dock = DockStyle.Left;

        var main = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.Surface,
            Padding = new Padding(8, 8, 12, 8),
        };

        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 34,
            BackColor = AppTheme.PrimaryLight,
        };
        header.Controls.Add(MakeHeaderCell("软件名称", 0, 300, ContentAlignment.MiddleLeft));
        header.Controls.Add(MakeHeaderCell("安装", 304, 100, ContentAlignment.MiddleCenter));
        header.Controls.Add(MakeHeaderCell("卸载", 408, 80, ContentAlignment.MiddleCenter));
        header.Controls.Add(MakeHeaderCell("状态", 492, 380, ContentAlignment.MiddleLeft));

        _listHost.Dock = DockStyle.Fill;
        _listHost.AutoScroll = true;
        _listHost.BackColor = AppTheme.SurfaceCard;

        main.Controls.Add(_listHost);
        main.Controls.Add(header);

        Controls.Add(main);
        Controls.Add(sidebar);

        Load += (_, _) => RefreshAll();
    }

    private Panel BuildSidebar()
    {
        var panel = new Panel
        {
            Width = 220,
            BackColor = AppTheme.SurfaceCard,
            Padding = new Padding(12, 12, 12, 12),
        };
        panel.Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.BorderLight);
            e.Graphics.DrawLine(pen, panel.Width - 1, 0, panel.Width - 1, panel.Height);
        };

        var title = new Label
        {
            Text = "常用软件",
            Location = new Point(12, 8),
            AutoSize = true,
            Font = new Font("Microsoft YaHei UI", 14F, FontStyle.Bold),
            ForeColor = AppTheme.PrimaryDeep,
        };

        var tip = new Label
        {
            Text = "✓ 提供常用软件一键下载与安装\r\n" +
                   "✓ 软件来自官方源，优先 winget\r\n" +
                   "✓ 支持检测安装状态与卸载",
            Location = new Point(12, 44),
            Size = new Size(196, 72),
            ForeColor = AppTheme.TextMute,
        };

        _askBeforeInstall.Text = "一键安装前询问";
        _askBeforeInstall.Checked = true;
        _askBeforeInstall.Location = new Point(12, 124);
        _askBeforeInstall.AutoSize = true;
        _askBeforeInstall.ForeColor = AppTheme.TextMain;

        _wingetHint.Location = new Point(12, 152);
        _wingetHint.Size = new Size(196, 36);
        _wingetHint.ForeColor = AppTheme.TextMute;
        _wingetHint.Font = new Font("Microsoft YaHei UI", 8F);

        var clearBtn = SidebarButton("删除下载临时文件", 196, () =>
        {
            CommonSoftwareHelper.ClearDownloadCache();
            MessageBox.Show(this, "已清理下载临时目录。", "常用软件", MessageBoxButtons.OK, MessageBoxIcon.Information);
        });
        clearBtn.Location = new Point(12, 196);

        var updateBtn = SidebarButton("检查软件更新", 196, () =>
        {
            var msg = CommonSoftwareHelper.CheckUpdates(CommonSoftwareCatalog.All);
            MessageBox.Show(this, msg, "检查更新", MessageBoxButtons.OK, MessageBoxIcon.Information);
            RefreshAll();
        });
        updateBtn.Location = new Point(12, 236);

        var essentialBtn = SidebarButton("安装系统必备软件", 196, InstallEssentials, primary: true);
        essentialBtn.Location = new Point(12, 480);
        essentialBtn.Height = 40;
        essentialBtn.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        panel.Controls.AddRange([title, tip, _askBeforeInstall, _wingetHint, clearBtn, updateBtn, essentialBtn]);
        return panel;
    }

    private void BuildList()
    {
        _listHost.SuspendLayout();
        _listHost.Controls.Clear();
        _rows.Clear();

        var y = 0;
        const int rowH = 40;
        string? category = null;
        foreach (var item in CommonSoftwareCatalog.All)
        {
            if (item.Category != category)
            {
                category = item.Category;
                var cap = new Label
                {
                    Text = category,
                    Location = new Point(8, y + 6),
                    Size = new Size(860, 22),
                    ForeColor = AppTheme.PrimaryDeep,
                    Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
                    BackColor = AppTheme.GroupBg,
                };
                var capWrap = new Panel
                {
                    Location = new Point(0, y),
                    Height = 34,
                    Width = 880,
                    BackColor = AppTheme.GroupBg,
                };
                capWrap.Controls.Add(cap);
                _listHost.Controls.Add(capWrap);
                y += 34;
            }

            var row = new CommonSoftwareRow(item, rowH, OnInstall, OnUninstall);
            row.Location = new Point(0, y);
            row.Width = 880;
            _listHost.Controls.Add(row);
            _rows[item.Id] = row;
            y += rowH;
        }

        _listHost.ResumeLayout(true);
    }

    private void RefreshAll()
    {
        _wingetHint.Text = CommonSoftwareHelper.IsWingetAvailable()
            ? "已检测到 winget，可一键安装。"
            : "未检测到 winget，将打开官方下载页。";
        if (_rows.Count == 0) BuildList();
        foreach (var row in _rows.Values)
            row.RefreshStatus();
    }

    private void OnInstall(CommonSoftwareItem item)
    {
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
        var missing = essentials.Where(x => !CommonSoftwareHelper.Query(x).Installed).ToList();
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
                notes.Count == 0 ? "必备软件安装流程已完成，请查看右侧状态。" : string.Join("\r\n\r\n", notes),
                "安装系统必备软件", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private static Label MakeHeaderCell(string text, int x, int w, ContentAlignment align) => new()
    {
        Text = text,
        Location = new Point(x, 0),
        Size = new Size(w, 34),
        ForeColor = AppTheme.TextHeader,
        Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold),
        TextAlign = align,
        BackColor = Color.Transparent,
    };

    private static Button SidebarButton(string text, int width, Action click, bool primary = false)
    {
        var b = new Button
        {
            Text = text,
            Width = width,
            Height = 32,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            BackColor = primary ? AppTheme.Primary : AppTheme.SurfaceCard,
            ForeColor = primary ? AppTheme.TextOnPrimary : AppTheme.TextMain,
        };
        if (primary) b.FlatAppearance.BorderSize = 0;
        else b.FlatAppearance.BorderColor = AppTheme.Border;
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
            Action<CommonSoftwareItem> onInstall,
            Action<CommonSoftwareItem> onUninstall)
        {
            _item = item;
            _onInstall = onInstall;
            _onUninstall = onUninstall;
            Height = height;
            BackColor = AppTheme.SurfaceCard;

            var name = new Label
            {
                Text = item.Title,
                Location = new Point(12, 0),
                Size = new Size(284, height),
                ForeColor = AppTheme.TextOnPrimary,
                BackColor = AppTheme.PrimaryDeep,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(8, 0, 0, 0),
            };

            _install = new Button
            {
                Text = "一键安装",
                Location = new Point(304, 6),
                Size = new Size(96, 28),
                FlatStyle = FlatStyle.Flat,
                ForeColor = AppTheme.PrimaryDeep,
                BackColor = AppTheme.SurfaceCard,
                Cursor = Cursors.Hand,
            };
            _install.FlatAppearance.BorderColor = AppTheme.Border;
            _install.Click += (_, _) => _onInstall(_item);

            _uninstall = new Button
            {
                Text = "卸载",
                Location = new Point(408, 6),
                Size = new Size(72, 28),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.FromArgb(120, 70, 150),
                BackColor = AppTheme.SurfaceCard,
                Cursor = Cursors.Hand,
            };
            _uninstall.FlatAppearance.BorderColor = AppTheme.Border;
            _uninstall.Click += (_, _) => _onUninstall(_item);

            _status = new Label
            {
                Location = new Point(492, 6),
                Size = new Size(360, 28),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = AppTheme.TextOnPrimary,
                Font = new Font("Microsoft YaHei UI", 8.75F, FontStyle.Bold),
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
            if (s.Installed)
            {
                _install.Text = "修复安装";
                _install.ForeColor = Color.FromArgb(0, 130, 60);
                _status.Text = string.IsNullOrWhiteSpace(s.Version)
                    ? "已安装"
                    : "已安装  版本: " + s.Version;
                _status.BackColor = Color.FromArgb(0, 150, 70);
            }
            else
            {
                _install.Text = "一键安装";
                _install.ForeColor = AppTheme.PrimaryDeep;
                _status.Text = "未安装";
                _status.BackColor = Color.FromArgb(220, 60, 60);
            }
        }
    }
}
