namespace WinOpt;

/// <summary>右侧配置脚本面板：说明 + 可查看/编辑的开启与关闭脚本（复制、保存）。</summary>
internal sealed class HelpDetailPanel : BufferedPanel
{
    private readonly Label _caption = new();
    private readonly Label _title = new();
    private readonly Label _summary = new();
    private readonly Panel _sections = new();
    private readonly Label _footer = new();

    private readonly Panel _recipeHost = new();
    private readonly Label _recipeCaption = new();
    private readonly Label _recipeKind = new();
    private readonly Label _recipeNote = new();
    private readonly Button _tabEnable = new();
    private readonly Button _tabDisable = new();
    private readonly TextBox _recipeBox = new();
    private readonly Button _btnCopy = new();
    private readonly Button _btnSave = new();
    private readonly Label _emptyRecipe = new();

    private SettingActionRecipe? _recipe;
    private string _itemTitle = "";
    private bool _showEnable = true;
    private const int PadX = 12;

    public HelpDetailPanel() : base(composited: true)
    {
        Width = UiPrefs.DefaultHelpPanelWidth;
        BackColor = AppTheme.SurfaceCard;
        Padding = new Padding(0, 0, 0, 8);
        AutoScroll = true;

        // 仅 1px 细分隔线，不要粗色条
        Paint += (_, e) =>
        {
            using var edge = new Pen(AppTheme.BorderLight);
            e.Graphics.DrawLine(edge, 0, 0, 0, Height);
        };

        _caption.Text = "配置脚本";
        _caption.SetBounds(PadX, 10, 280, 18);
        _caption.ForeColor = AppTheme.TextMute;
        _caption.Font = new Font("Microsoft YaHei UI", 8F);
        _caption.BackColor = Color.Transparent;

        _title.SetBounds(PadX, 30, 280, 44);
        _title.ForeColor = AppTheme.PrimaryDeep;
        _title.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
        _title.BackColor = Color.Transparent;
        _title.AutoEllipsis = false;

        _summary.SetBounds(PadX, 76, 280, 48);
        _summary.ForeColor = AppTheme.TextMute;
        _summary.Font = new Font("Microsoft YaHei UI", 8.75F);
        _summary.BackColor = Color.Transparent;
        _summary.AutoEllipsis = false;

        _sections.SetBounds(PadX, 128, 280, 10);
        _sections.BackColor = Color.Transparent;
        _sections.AutoSize = true;
        _sections.AutoSizeMode = AutoSizeMode.GrowAndShrink;

        BuildRecipeHost();

        _footer.SetBounds(PadX, 140, 280, 36);
        _footer.ForeColor = AppTheme.PrimaryDark;
        _footer.Font = new Font("Microsoft YaHei UI", 8F);
        _footer.BackColor = Color.Transparent;

        Controls.Add(_footer);
        Controls.Add(_recipeHost);
        Controls.Add(_sections);
        Controls.Add(_summary);
        Controls.Add(_title);
        Controls.Add(_caption);

        Resize += (_, _) => LayoutInner();
        ShowPlaceholder();
    }

    private void BuildRecipeHost()
    {
        _recipeHost.BackColor = Color.FromArgb(248, 250, 252);
        _recipeHost.Padding = new Padding(8);
        _recipeHost.Visible = false;

        _recipeCaption.Text = "配置脚本（可编辑）";
        _recipeCaption.Font = new Font("Microsoft YaHei UI", 8.75F, FontStyle.Bold);
        _recipeCaption.ForeColor = AppTheme.PrimaryDeep;
        _recipeCaption.BackColor = Color.Transparent;
        _recipeCaption.AutoSize = true;
        _recipeCaption.Location = new Point(8, 6);

        _recipeKind.Font = new Font("Microsoft YaHei UI", 8F);
        _recipeKind.ForeColor = AppTheme.TextMute;
        _recipeKind.BackColor = Color.Transparent;
        _recipeKind.AutoSize = true;
        _recipeKind.Location = new Point(128, 8);

        StyleTab(_tabEnable, "开启", true);
        StyleTab(_tabDisable, "关闭", false);
        _tabEnable.Location = new Point(8, 28);
        _tabDisable.Location = new Point(88, 28);
        _tabEnable.Click += (_, _) => SetRecipeSide(true);
        _tabDisable.Click += (_, _) => SetRecipeSide(false);

        _recipeBox.Multiline = true;
        _recipeBox.ReadOnly = false; // 允许用户改完再复制/保存
        _recipeBox.ScrollBars = ScrollBars.Vertical;
        _recipeBox.Font = new Font("Consolas", 8.25F);
        _recipeBox.BackColor = Color.White;
        _recipeBox.ForeColor = AppTheme.TextMain;
        _recipeBox.BorderStyle = BorderStyle.FixedSingle;
        _recipeBox.Location = new Point(8, 58);
        _recipeBox.Height = 150;
        _recipeBox.WordWrap = false;
        _recipeBox.ShortcutsEnabled = true;
        _recipeBox.AcceptsReturn = true;
        _recipeBox.AcceptsTab = true;

        StyleAction(_btnCopy, "复制");
        StyleAction(_btnSave, "保存…");
        _btnCopy.Click += (_, _) => CopyRecipe();
        _btnSave.Click += (_, _) => SaveRecipe();

        _recipeNote.Font = new Font("Microsoft YaHei UI", 8F);
        _recipeNote.ForeColor = AppTheme.PrimaryDark;
        _recipeNote.BackColor = Color.Transparent;
        _recipeNote.AutoSize = false;
        _recipeNote.MaximumSize = new Size(260, 0);

        _emptyRecipe.Text = "此项为组合操作（DISM/多服务等），未单独收录脚本；请用左侧开关 +「应用到系统」。";
        _emptyRecipe.Font = new Font("Microsoft YaHei UI", 8.5F);
        _emptyRecipe.ForeColor = AppTheme.TextMute;
        _emptyRecipe.BackColor = Color.Transparent;
        _emptyRecipe.AutoSize = false;
        _emptyRecipe.Visible = false;

        _recipeHost.Controls.Add(_recipeCaption);
        _recipeHost.Controls.Add(_recipeKind);
        _recipeHost.Controls.Add(_tabEnable);
        _recipeHost.Controls.Add(_tabDisable);
        _recipeHost.Controls.Add(_recipeBox);
        _recipeHost.Controls.Add(_btnCopy);
        _recipeHost.Controls.Add(_btnSave);
        _recipeHost.Controls.Add(_recipeNote);
        _recipeHost.Controls.Add(_emptyRecipe);
    }

    private static void StyleTab(Button b, string text, bool primaryLook)
    {
        b.Text = text;
        b.Size = new Size(72, 26);
        b.FlatStyle = FlatStyle.Flat;
        b.Font = new Font("Microsoft YaHei UI", 8.5F);
        b.Cursor = Cursors.Hand;
        b.FlatAppearance.BorderSize = 1;
        ApplyTabVisual(b, selected: primaryLook && text == "开启");
    }

    private static void ApplyTabVisual(Button b, bool selected)
    {
        if (selected)
        {
            b.BackColor = AppTheme.Primary;
            b.ForeColor = AppTheme.TextOnPrimary;
            b.FlatAppearance.BorderColor = AppTheme.PrimaryDark;
        }
        else
        {
            b.BackColor = Color.White;
            b.ForeColor = AppTheme.TextMain;
            b.FlatAppearance.BorderColor = AppTheme.Border;
        }
    }

    private static void StyleAction(Button b, string text)
    {
        b.Text = text;
        b.Size = new Size(72, 26);
        b.FlatStyle = FlatStyle.Flat;
        b.Font = new Font("Microsoft YaHei UI", 8.5F);
        b.BackColor = Color.White;
        b.ForeColor = AppTheme.PrimaryDeep;
        b.FlatAppearance.BorderColor = AppTheme.Primary;
        b.Cursor = Cursors.Hand;
    }

    public void ShowEmbeddedGuide(string pageTitle)
    {
        HideRecipe();
        _caption.Text = "配置脚本 · 即时设置";
        _title.Text = pageTitle;
        _summary.Text = "本页开关切换后直接写入系统。";
        BuildSections([
            ("与分组页的关系", "登录启动项、DNS 等即时页适合单项微调；左侧其它分类为分组列表，改完后点「应用到系统」。"),
            ("同步状态", "在其他地方改过系统后，可点本页底部「刷新」，或菜单「工具 → 刷新当前状态」。"),
            ("配置脚本", "分组列表中点选优化项，可在本栏查看并编辑「开启/关闭」对应的注册表或脚本。"),
        ]);
        _footer.Text = "RDP 端口 / 预取 / Search 等请从「工具 → 高级设置」打开";
    }

    public void ShowPlaceholder(string? groupTitle = null)
    {
        HideRecipe();
        _caption.Text = "配置脚本 · 使用指引";
        _title.Text = groupTitle is null ? "选择左侧配置项" : $"{groupTitle}";
        _summary.Text = "点选配置项后，可在此查看、编辑开启/关闭脚本，并复制或保存为文件。";
        BuildSections([
            ("操作", "开=采用优化建议；关=恢复「系统默认值」。改完后点「应用到系统」。"),
            ("配置脚本", "脚本可直接改字；「复制」到剪贴板，「保存…」另存为 .reg/.cmd/.ps1 后手工执行。"),
            ("面板宽度", "拖动左右分隔条可调整本栏宽度，下次启动会记住。"),
            ("说明列", "写明对应系统哪里、何时建议开，悬停可看全文。"),
            ("搜索", "可搜项目名、说明或摘要；「视图」可隐藏当前系统不适用的项。"),
        ]);
        _footer.Text = "F1 打开完整使用说明 · 视图 → 显示配置脚本";
    }

    public void ShowUsageGuide()
    {
        HideRecipe();
        _caption.Text = "配置脚本 · 使用说明";
        _title.Text = $"{AppBrand.ProductName} 使用说明";
        _summary.Text = "用于 Windows Server 桌面化：改注册表、服务与 DISM。";
        BuildSections([
            ("工作流程", "1. 选择左侧分类 → 2. 勾选开关 → 3. 点击「应用到系统」。"),
            ("配置脚本", "点选配置项后，右侧可查看/编辑与软件一致的开启、关闭脚本，便于审计或离线应用。"),
            ("列含义", "说明=对应哪里·何时建议；推荐值=五星；设置操作=开关。"),
            ("配置备份", "「文件」菜单可导入/导出 JSON 配置，便于多台机器复用或回滚界面状态。"),
            ("管理员", "必须以管理员身份运行，否则注册表、服务、DISM 操作可能失败。"),
            ("生效", "多数项写入后即可用；DISM、大系统缓存、自动登录等需重启。"),
            ("操作日志", "一般事件：%LocalAppData%\\WinOpt\\apply.log"),
            ("变更日志", "优化改动专用：%LocalAppData%\\WinOpt\\变更日志.log"),
        ]);
        _footer.Text = "帮助 → 打开变更日志 / 打开操作日志";
    }

    public void ShowScopeLegend()
    {
        HideRecipe();
        _caption.Text = "配置脚本 · 标识图例";
        _title.Text = "适用范围标识";
        _summary.Text = "每项名称下方的彩色标签，标明该项在不同系统上是否有效。";
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
        _caption.Text = "配置脚本 · 当前项";
        _title.Text = itemTitle;
        _summary.Text = help.Summary;
        var sections = new List<(string Head, string Body)>();
        if (help.Scope.HasBadge)
            sections.Add(("适用范围", help.Scope.FormatHelpSection().Trim()));
        sections.Add(("推荐强度", RecommendLevelUi.Tip(help.Recommend)));
        if (help.UiPlace.Length > 0)
            sections.Add(("对应哪里", help.UiPlace));
        if (help.WhenHint.Length > 0)
            sections.Add(("何时建议", help.WhenHint));
        sections.Add(("作用", help.Purpose));
        sections.Add(("好处", help.Benefit));
        sections.Add(("指引", help.Guide));
        sections.Add(("生效", help.Effect));
        BuildSections(sections);
        _footer.Text = help.Scope.HasBadge ? help.Scope.FormatBadges() : "";

        _itemTitle = itemTitle;
        _recipe = SettingRecipeCatalog.Get(help);
        _showEnable = true;
        ShowRecipeUi();
    }

    private void HideRecipe()
    {
        _recipe = null;
        _itemTitle = "";
        _recipeHost.Visible = false;
        LayoutInner();
    }

    private void ShowRecipeUi()
    {
        _recipeHost.Visible = true;
        var has = _recipe is not null;
        _recipeBox.Visible = has;
        _tabEnable.Visible = has;
        _tabDisable.Visible = has;
        _btnCopy.Visible = has;
        _btnSave.Visible = has;
        _recipeKind.Visible = has;
        _recipeNote.Visible = has && _recipe!.Note.Length > 0;
        _emptyRecipe.Visible = !has;

        if (has)
        {
            _recipeKind.Text = _recipe!.KindLabel;
            _recipeNote.Text = _recipe.Note.Length > 0
                ? _recipe.Note
                : "可直接修改下方内容，再复制或保存为文件。";
            _recipeNote.Visible = true;
            SetRecipeSide(_showEnable);
        }
        else
        {
            _recipeKind.Text = "";
            LayoutInner();
        }
    }

    private void SetRecipeSide(bool enable)
    {
        _showEnable = enable;
        ApplyTabVisual(_tabEnable, enable);
        ApplyTabVisual(_tabDisable, !enable);
        if (_recipe is not null)
            _recipeBox.Text = _recipe.ContentFor(enable);
        LayoutInner();
    }

    private string CurrentScriptText() => _recipeBox.Text;

    private void CopyRecipe()
    {
        if (!_recipeBox.Visible) return;
        try
        {
            Clipboard.SetText(CurrentScriptText());
            _btnCopy.Text = "已复制";
            var t = new System.Windows.Forms.Timer { Interval = 1200 };
            t.Tick += (_, _) =>
            {
                _btnCopy.Text = "复制";
                t.Stop();
                t.Dispose();
            };
            t.Start();
        }
        catch
        {
            MessageBox.Show("无法写入剪贴板。", AppBrand.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void SaveRecipe()
    {
        if (!_recipeBox.Visible) return;
        var ext = _recipe?.FileExtension ?? ".txt";
        var filter = _recipe?.Kind switch
        {
            SettingActionKind.Reg => "注册表 (*.reg)|*.reg|所有文件 (*.*)|*.*",
            SettingActionKind.Cmd => "批处理 (*.cmd)|*.cmd|所有文件 (*.*)|*.*",
            SettingActionKind.PowerShell => "PowerShell (*.ps1)|*.ps1|所有文件 (*.*)|*.*",
            _ => "文本 (*.txt)|*.txt|所有文件 (*.*)|*.*",
        };
        var suggested = _recipe?.SuggestedFileName(_itemTitle, _showEnable)
                        ?? ("配置脚本" + (_showEnable ? "-开启" : "-关闭") + ext);

        using var dlg = new SaveFileDialog
        {
            Title = "保存配置脚本",
            Filter = filter,
            FileName = suggested,
            OverwritePrompt = true,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
        };
        if (dlg.ShowDialog(FindForm()) != DialogResult.OK) return;
        try
        {
            File.WriteAllText(dlg.FileName, CurrentScriptText(),
                new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            _btnSave.Text = "已保存";
            var t = new System.Windows.Forms.Timer { Interval = 1200 };
            t.Tick += (_, _) =>
            {
                _btnSave.Text = "保存…";
                t.Stop();
                t.Dispose();
            };
            t.Start();
        }
        catch (Exception ex)
        {
            MessageBox.Show("保存失败：\n" + ex.Message, AppBrand.ProductName,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
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
                Size = new Size(280, 20),
                ForeColor = AppTheme.PrimaryDeep,
                Font = new Font("Microsoft YaHei UI", 8.75F, FontStyle.Bold),
                BackColor = Color.Transparent,
            };
            y += 22;
            var bodyLbl = new Label
            {
                Text = body,
                Location = new Point(0, y),
                MaximumSize = new Size(280, 0),
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
        var w = Math.Max(240, ClientSize.Width - PadX * 2);
        _caption.Left = PadX;
        _title.Left = PadX;
        _summary.Left = PadX;
        _sections.Left = PadX;
        _footer.Left = PadX;
        _caption.Width = w;
        _title.Width = w;
        _summary.Width = w;
        _sections.Width = w;
        _footer.Width = w;
        _recipeHost.Width = w;
        _recipeHost.Left = PadX;

        var titleH = Math.Max(28, TextRenderer.MeasureText(
            _title.Text, _title.Font, new Size(w, int.MaxValue),
            TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPrefix).Height + 4);
        _title.Height = Math.Min(titleH, 72);
        _summary.Top = _title.Bottom + 4;
        var summaryH = Math.Max(24, TextRenderer.MeasureText(
            _summary.Text, _summary.Font, new Size(w, int.MaxValue),
            TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPrefix).Height + 4);
        _summary.Height = Math.Min(summaryH, 96);

        var y = _summary.Bottom + 8;
        if (_recipeHost.Visible)
        {
            _recipeHost.Top = y;
            LayoutRecipe(w);
            y = _recipeHost.Bottom + 10;
        }

        _sections.Top = y;

        foreach (Control c in _sections.Controls)
        {
            if (c is Label lbl && (lbl.Font.Style & FontStyle.Bold) != 0)
                lbl.Width = w;
            else if (c is Label body)
                body.MaximumSize = new Size(w, 0);
        }

        _footer.Top = _sections.Bottom + 8;
        AutoScrollMinSize = new Size(0, _footer.Bottom + 12);
    }

    public void FocusRecipe()
    {
        if (!_recipeHost.Visible) return;
        AutoScrollPosition = new Point(0, Math.Max(0, _recipeHost.Top - 8));
        if (_recipeBox.Visible)
            _recipeBox.Focus();
    }

    private void LayoutRecipe(int w)
    {
        var inner = Math.Max(220, w - 16);
        _recipeBox.Width = inner;
        _emptyRecipe.SetBounds(8, 28, inner, 60);
        _recipeNote.Width = inner;
        _recipeNote.MaximumSize = new Size(inner, 0);

        if (_recipeNote.Visible && _recipeNote.Text.Length > 0)
        {
            var noteH = TextRenderer.MeasureText(
                _recipeNote.Text, _recipeNote.Font, new Size(inner, int.MaxValue),
                TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl).Height + 4;
            _recipeNote.Height = Math.Min(noteH, 48);
        }

        if (_emptyRecipe.Visible)
        {
            _recipeHost.Height = 100;
            return;
        }

        _btnCopy.Location = new Point(8, _recipeBox.Bottom + 6);
        _btnSave.Location = new Point(88, _recipeBox.Bottom + 6);
        if (_recipeNote.Visible)
        {
            _recipeNote.Location = new Point(8, _btnCopy.Bottom + 6);
            _recipeHost.Height = _recipeNote.Bottom + 10;
        }
        else
        {
            _recipeHost.Height = _btnCopy.Bottom + 10;
        }
    }
}
