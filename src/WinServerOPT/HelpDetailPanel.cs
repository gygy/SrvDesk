namespace WinOpt;

/// <summary>右侧上下文帮助面板：结构化展示选中项说明。</summary>
internal sealed class HelpDetailPanel : Panel
{
    private readonly Label _caption = new();
    private readonly Label _title = new();
    private readonly Label _summary = new();
    private readonly Panel _sections = new();
    private readonly Label _footer = new();

    public HelpDetailPanel()
    {
        Width = 300;
        BackColor = AppTheme.PrimaryPale;
        Padding = new Padding(0, 0, 0, 8);
        AutoScroll = true;

        Paint += (_, e) =>
        {
            using var accent = new SolidBrush(AppTheme.Primary);
            e.Graphics.FillRectangle(accent, 0, 0, 4, Height);
            using var top = new Pen(AppTheme.Border);
            e.Graphics.DrawLine(top, 0, 0, Width, 0);
        };

        _caption.Text = "帮助";
        _caption.SetBounds(16, 10, 260, 18);
        _caption.ForeColor = AppTheme.TextMute;
        _caption.Font = new Font("Microsoft YaHei UI", 8F);
        _caption.BackColor = Color.Transparent;

        _title.SetBounds(16, 30, 260, 44);
        _title.ForeColor = AppTheme.PrimaryDeep;
        _title.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
        _title.BackColor = Color.Transparent;
        _title.AutoEllipsis = false;

        _summary.SetBounds(16, 76, 260, 48);
        _summary.ForeColor = AppTheme.TextMute;
        _summary.Font = new Font("Microsoft YaHei UI", 8.75F);
        _summary.BackColor = Color.Transparent;
        _summary.AutoEllipsis = false;

        _sections.SetBounds(16, 128, 260, 10);
        _sections.BackColor = Color.Transparent;
        _sections.AutoSize = true;
        _sections.AutoSizeMode = AutoSizeMode.GrowAndShrink;

        _footer.SetBounds(16, 140, 260, 36);
        _footer.ForeColor = AppTheme.PrimaryDark;
        _footer.Font = new Font("Microsoft YaHei UI", 8F);
        _footer.BackColor = Color.Transparent;

        Controls.Add(_footer);
        Controls.Add(_sections);
        Controls.Add(_summary);
        Controls.Add(_title);
        Controls.Add(_caption);

        Resize += (_, _) => LayoutInner();
        ShowPlaceholder();
    }

    public void ShowEmbeddedGuide(string pageTitle)
    {
        _caption.Text = "帮助 · 即时设置";
        _title.Text = pageTitle;
        _summary.Text = "本页开关修改后立即写入系统，无需点击底部「应用推荐」。";
        BuildSections([
            ("与分组页的关系", "同一设置若在「性能及安全」等分组中也有，本页用于逐项微调；分组页适合配合预设批量应用。"),
            ("同步状态", "在其他地方修改系统后，可点本页底部「刷新」读取当前值。"),
        ]);
        _footer.Text = "hosts、事件查看器等系统工具请从顶部「工具」菜单打开";
    }

    public void ShowPlaceholder(string? groupTitle = null)
    {
        _caption.Text = "帮助 · 使用指引";
        _title.Text = groupTitle is null ? "选择左侧设置项" : $"{groupTitle}";
        _summary.Text = "点击列表中的项目名称或 ⓘ 图标，此处显示完整说明。";
        BuildSections([
            ("操作", "开关=采用推荐设置；关闭=恢复右侧「系统默认」列所示值。修改后点击底部「应用推荐」写入系统。"),
            ("搜索", "顶部命令栏可搜索项目名称或摘要；「视图」菜单可隐藏当前系统不适用的项。"),
            ("标识", "Server 专属 = 仅 Windows Server；需桌面体验 = Server Core 无效；版本标签 = 最低系统要求。"),
        ]);
        _footer.Text = "F1 打开完整使用说明";
    }

    public void ShowUsageGuide()
    {
        _caption.Text = "帮助 · 使用说明";
        _title.Text = $"{AppBrand.ProductName} 使用说明";
        _summary.Text = "面向 Windows Server 桌面化场景的一键注册表/服务/DISM 优化工具。";
        BuildSections([
            ("工作流程", "1. 选择左侧分类 → 2. 勾选推荐项或载入预设 → 3. 点击「应用推荐」写入系统。"),
            ("预设方案", "「预设」菜单提供 Server 桌面、安全加固、远程办公、最小改动四套方案；载入后仍可微调。"),
            ("配置备份", "「文件」菜单可导入/导出 JSON 配置，便于多台机器复用或回滚界面状态。"),
            ("管理员", "必须以管理员身份运行，否则注册表、服务、DISM 操作可能失败。"),
            ("生效", "多数项立即生效；DISM、大系统缓存、自动登录等需重启。详见各项「生效方式」。"),
        ]);
        _footer.Text = "日志路径见「帮助 → 打开操作日志」";
    }

    public void ShowScopeLegend()
    {
        _caption.Text = "帮助 · 标识图例";
        _title.Text = "适用范围标识";
        _summary.Text = "每项名称下方彩色标签表示该优化在不同系统上的有效性。";
        BuildSections([
            ("Server 专属", "仅在 Windows Server 安装类型下有意义；客户端 Windows 上可能无效或不存在对应策略。"),
            ("需桌面体验", "Server Core（无桌面体验）无法应用；GUI Server 或 Win10/11 桌面可用。"),
            ("版本标签", "如 Server 2016+、Win10+ 表示该注册表/功能在更低版本上不存在或行为不同。"),
            ("过滤", "勾选「视图 → 隐藏不适用项」可自动隐藏当前环境不可用的开关。"),
        ]);
        _footer.Text = "";
    }

    public void ShowSetting(string itemTitle, SettingHelpInfo help)
    {
        _caption.Text = "帮助 · 当前项";
        _title.Text = itemTitle;
        _summary.Text = help.Summary;
        var sections = new List<(string Head, string Body)>();
        if (help.Scope.HasBadge)
            sections.Add(("适用范围", help.Scope.FormatHelpSection().Trim()));
        sections.Add(("作用", help.Purpose));
        sections.Add(("好处", help.Benefit));
        sections.Add(("指引", help.Guide));
        sections.Add(("生效", help.Effect));
        BuildSections(sections);
        _footer.Text = help.Scope.HasBadge ? help.Scope.FormatBadges() : "";
    }

    private void BuildSections(IReadOnlyList<(string Head, string Body)> items)
    {
        _sections.Controls.Clear();
        _sections.SuspendLayout();
        var y = 0;
        foreach (var (head, body) in items)
        {
            var headLbl = new Label
            {
                Text = head,
                Location = new Point(0, y),
                Size = new Size(260, 20),
                ForeColor = AppTheme.PrimaryDeep,
                Font = new Font("Microsoft YaHei UI", 8.75F, FontStyle.Bold),
                BackColor = Color.Transparent,
            };
            y += 22;
            var bodyLbl = new Label
            {
                Text = body,
                Location = new Point(0, y),
                MaximumSize = new Size(260, 0),
                AutoSize = true,
                ForeColor = AppTheme.TextMain,
                Font = new Font("Microsoft YaHei UI", 8.75F),
                BackColor = Color.Transparent,
            };
            y += bodyLbl.Height + 14;
            _sections.Controls.Add(headLbl);
            _sections.Controls.Add(bodyLbl);
        }
        _sections.Height = Math.Max(y, 20);
        _sections.ResumeLayout(true);
        LayoutInner();
    }

    private void LayoutInner()
    {
        var w = Math.Max(240, ClientSize.Width - 24);
        _caption.Width = w;
        _title.Width = w;
        _summary.Width = w;
        _sections.Width = w;
        _footer.Width = w;

        var titleH = Math.Max(28, TextRenderer.MeasureText(
            _title.Text, _title.Font, new Size(w, int.MaxValue),
            TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPrefix).Height + 4);
        _title.Height = Math.Min(titleH, 72);
        _summary.Top = _title.Bottom + 4;
        var summaryH = Math.Max(24, TextRenderer.MeasureText(
            _summary.Text, _summary.Font, new Size(w, int.MaxValue),
            TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPrefix).Height + 4);
        _summary.Height = Math.Min(summaryH, 96);
        _sections.Top = _summary.Bottom + 8;

        foreach (Control c in _sections.Controls)
        {
            if (c is Label lbl && (lbl.Font.Style & FontStyle.Bold) != 0)
                lbl.Width = w;
            else if (c is Label body)
                body.MaximumSize = new Size(w, 0);
        }

        _footer.Top = _sections.Bottom + 8;
        var contentH = _footer.Bottom + 12;
        AutoScrollMinSize = new Size(0, contentH);
    }
}
