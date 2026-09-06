namespace WinOpt;

/// <summary>配置脚本面板：说明 + 可查看/编辑的开启与关闭脚本（复制、导出）。</summary>
internal sealed class HelpDetailPanel : BufferedPanel
{
    private readonly Label _caption = new();
    private readonly Label _title = new();
    private readonly Label _summary = new();
    private readonly Panel _sections = new();
    private readonly Label _footer = new();
    private readonly Button _dockRight = new();
    private readonly Button _dockBottom = new();
    private readonly Button _dockClose = new();
    private readonly ToolTip _tip = new();

    private readonly Panel _recipeHost = new();
    private readonly Label _recipeCaption = new();
    private readonly Label _recipeKind = new();
    private readonly Label _recipeNote = new();
    private readonly Button _tabEnable = new();
    private readonly Button _tabDisable = new();
    private readonly ScriptSyntaxEditor _recipeBox = new();
    private readonly Button _btnCopy = new();
    private readonly Button _btnSave = new();
    private readonly Button _btnReset = new();
    private readonly Label _emptyRecipe = new();

    private SettingActionRecipe? _recipe;
    private string _itemTitle = "";
    private bool _showEnable = true;
    private ConfigScriptDock _activeDock = ConfigScriptDock.Right;
    private readonly System.Windows.Forms.Timer _persistTimer = new() { Interval = 600 };
    private const int PadX = 12;

    /// <summary>用户点击面板顶部停靠图标时触发。</summary>
    public event Action<ConfigScriptDock>? DockRequested;

    /// <summary>用户点击面板顶部关闭图标时触发。</summary>
    public event Action? CloseRequested;

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
            if (_activeDock == ConfigScriptDock.Bottom)
                e.Graphics.DrawLine(edge, 0, 0, Width, 0);
            else
                e.Graphics.DrawLine(edge, 0, 0, 0, Height);
        };

        _caption.Text = "配置脚本";
        _caption.SetBounds(PadX, 10, 200, 18);
        _caption.ForeColor = AppTheme.TextMute;
        _caption.Font = new Font("Microsoft YaHei UI", 8F);
        _caption.BackColor = Color.Transparent;

        StyleDockButton(_dockRight, MenuIcons.DockRight, "靠右停靠");
        StyleDockButton(_dockBottom, MenuIcons.DockBottom, "靠底停靠");
        StyleDockButton(_dockClose, MenuIcons.PanelClose, "关闭配置脚本面板");
        _dockRight.Click += (_, _) => DockRequested?.Invoke(ConfigScriptDock.Right);
        _dockBottom.Click += (_, _) => DockRequested?.Invoke(ConfigScriptDock.Bottom);
        _dockClose.Click += (_, _) => CloseRequested?.Invoke();

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
        _sections.AutoSize = false;

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
        Controls.Add(_dockRight);
        Controls.Add(_dockBottom);
        Controls.Add(_dockClose);

        Resize += (_, _) => LayoutInner();
        SetActiveDock(ConfigScriptDock.Right);
        ShowPlaceholder();
    }

    /// <summary>同步面板顶部停靠图标的选中态。</summary>
    public void SetActiveDock(ConfigScriptDock dock)
    {
        _activeDock = dock;
        ApplyDockButtonVisual(_dockRight, dock == ConfigScriptDock.Right);
        ApplyDockButtonVisual(_dockBottom, dock == ConfigScriptDock.Bottom);
        Invalidate();
    }

    private void StyleDockButton(Button b, Image image, string tip)
    {
        b.Size = new Size(24, 24);
        b.FlatStyle = FlatStyle.Flat;
        b.FlatAppearance.BorderSize = 1;
        b.BackColor = Color.White;
        b.Cursor = Cursors.Hand;
        b.TabStop = false;
        b.Image = image;
        b.ImageAlign = ContentAlignment.MiddleCenter;
        b.Text = "";
        _tip.SetToolTip(b, tip);
    }

    private static void ApplyDockButtonVisual(Button b, bool selected)
    {
        if (selected)
        {
            b.BackColor = AppTheme.PrimaryPale;
            b.FlatAppearance.BorderColor = AppTheme.Primary;
        }
        else
        {
            b.BackColor = Color.White;
            b.FlatAppearance.BorderColor = AppTheme.BorderLight;
        }
    }

    private void BuildRecipeHost()
    {
        _recipeHost.BackColor = Color.FromArgb(248, 250, 252);
        _recipeHost.Padding = new Padding(8);
        _recipeHost.Visible = false;

        _recipeCaption.Text = "配置脚本";
        _recipeCaption.Font = new Font("Microsoft YaHei UI", 8.75F, FontStyle.Bold);
        _recipeCaption.ForeColor = AppTheme.PrimaryDeep;
        _recipeCaption.BackColor = Color.Transparent;
        _recipeCaption.AutoSize = true;
        _recipeCaption.Location = new Point(8, 6);

        _recipeKind.Font = new Font("Microsoft YaHei UI", 8F);
        _recipeKind.ForeColor = AppTheme.TextMute;
        _recipeKind.BackColor = Color.Transparent;
        _recipeKind.AutoSize = true;
        _recipeKind.Location = new Point(80, 8);

        StyleTab(_tabEnable, "开启", true);
        StyleTab(_tabDisable, "关闭", false);
        _tabEnable.Location = new Point(8, 28);
        _tabDisable.Location = new Point(8 + _tabEnable.Width + 8, 28);
        _tabEnable.Click += (_, _) => SetRecipeSide(true);
        _tabDisable.Click += (_, _) => SetRecipeSide(false);

        _recipeBox.Location = new Point(8, 58);
        _recipeBox.Height = 220;
        _recipeBox.Width = 240;
        _recipeBox.ReadOnly = false;
        _recipeBox.DetectUrls = false;
        _tip.SetToolTip(_recipeBox, "可直接编辑；改完会自动记住。可用复制/导出/恢复默认。");

        StyleAction(_btnCopy, "复制");
        StyleAction(_btnSave, "导出");
        StyleAction(_btnReset, "恢复默认");
        _btnCopy.Click += (_, _) => CopyRecipe();
        _btnSave.Click += (_, _) => ExportRecipe();
        _btnReset.Click += (_, _) => ResetRecipeToBuiltin();
        _recipeBox.UserScriptChanged += (_, _) =>
        {
            _persistTimer.Stop();
            _persistTimer.Start();
        };
        _persistTimer.Tick += (_, _) =>
        {
            _persistTimer.Stop();
            PersistCurrentScript();
            UpdateOverrideHint();
        };

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
        _recipeHost.Controls.Add(_btnReset);
        _recipeHost.Controls.Add(_recipeNote);
        _recipeHost.Controls.Add(_emptyRecipe);
    }

    private static void StyleTab(Button b, string text, bool primaryLook)
    {
        b.Text = text;
        b.Font = new Font("Microsoft YaHei UI", 8.5F);
        b.Size = UiFit.ButtonSize(text, 26, b.Font, minWidth: 56, padding: 20);
        b.FlatStyle = FlatStyle.Flat;
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
        b.Font = new Font("Microsoft YaHei UI", 8.5F);
        b.Size = UiFit.ButtonSize(text, 26, b.Font, minWidth: 56, padding: 20);
        b.FlatStyle = FlatStyle.Flat;
        b.BackColor = Color.White;
        b.ForeColor = AppTheme.PrimaryDeep;
        b.FlatAppearance.BorderColor = AppTheme.Primary;
        b.Cursor = Cursors.Hand;
    }

    public void ShowEmbeddedGuide(string pageTitle)
    {
        HideRecipe();
        _caption.Text = "配置脚本";
        _title.Text = pageTitle;
        _summary.Text = "本页开关会直接写入系统。";
        BuildSections([]);
        _footer.Text = "";
    }

    public void ShowPlaceholder(string? groupTitle = null)
    {
        HideRecipe();
        _caption.Text = "配置脚本";
        _title.Text = groupTitle is null ? "选择左侧配置项" : groupTitle;
        _summary.Text = "点选一项后，下方可查看并编辑开启/关闭脚本。";
        BuildSections([]);
        _footer.Text = "";
    }

    public void ShowUsageGuide()
    {
        HideRecipe();
        _caption.Text = "使用说明";
        _title.Text = AppBrand.ProductName;
        _summary.Text = "勾选优化项 →「应用到系统」。需管理员运行。";
        BuildSections([
            ("配置脚本", "点选左侧项可查看/编辑对应脚本。"),
            ("备份", "文件菜单可导入、导出配置。"),
        ]);
        _footer.Text = "";
    }

    public void ShowScopeLegend()
    {
        HideRecipe();
        _caption.Text = "标识图例";
        _title.Text = "适用范围";
        _summary.Text = "名称下方标签表示该项适用的系统范围。";
        BuildSections([
            ("Server 专属", "仅 Windows Server。"),
            ("需桌面体验", "Server Core 不可用。"),
        ]);
        _footer.Text = "";
    }

    public void ShowSetting(string itemTitle, SettingHelpInfo help)
    {
        _caption.Text = "配置脚本 · 当前项";
        _title.Text = itemTitle;
        _summary.Text = help.Summary;
        BuildSettingBrief(help);
        _footer.Text = "";

        _itemTitle = itemTitle;
        _recipe = SettingRecipeCatalog.Get(help);
        _showEnable = true;
        ShowRecipeUi();
    }

    /// <summary>精简说明：第1行彩色五星+推荐/范围/生效；其后各占一行，最多约 3 行。</summary>
    private void BuildSettingBrief(SettingHelpInfo help)
    {
        _sections.Controls.Clear();
        _sections.AutoSize = false;

        var metaParts = new List<string> { RecommendLevelUi.Title(help.Recommend) };
        if (help.Scope.HasBadge)
            metaParts.Add(help.Scope.FormatBadges());
        if (help.Effect.Length > 0)
            metaParts.Add(TrimOneLine(help.Effect, 20));

        _sections.Controls.Add(new RecommendStarsRow
        {
            Level = help.Recommend,
            TrailingText = string.Join(" · ", metaParts),
            Tag = "stars",
        });

        var line2 = TrimOneLine(help.Purpose, 56);
        if (line2.Length > 0)
        {
            _sections.Controls.Add(new Label
            {
                Text = line2,
                AutoSize = false,
                ForeColor = AppTheme.TextMain,
                Font = new Font("Microsoft YaHei UI", 8.5F),
                BackColor = Color.Transparent,
                Tag = "b",
            });
        }

        var line3 = help.Guide.Length > 0
            ? TrimOneLine(help.Guide, 56)
            : (help.Benefit.Length > 0 ? TrimOneLine(help.Benefit, 56) : "");
        if (line3.Length == 0 && help.UiPlace.Length > 0)
            line3 = TrimOneLine(help.UiPlace, 56);
        else if (line3.Length == 0 && help.WhenHint.Length > 0)
            line3 = TrimOneLine(help.WhenHint, 56);

        if (line3.Length > 0)
        {
            _sections.Controls.Add(new Label
            {
                Text = line3,
                AutoSize = false,
                ForeColor = AppTheme.TextMute,
                Font = new Font("Microsoft YaHei UI", 8.25F),
                BackColor = Color.Transparent,
                Tag = "b",
            });
        }

        LayoutInner();
    }

    private static string TrimOneLine(string text, int max)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        var t = text.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ').Trim();
        while (t.Contains("  ")) t = t.Replace("  ", " ");
        var cut = t.IndexOfAny(['。', '；', ';']);
        if (cut > 0 && cut < max) t = t.Substring(0, cut);
        if (t.Length > max) t = t.Substring(0, max - 1) + "…";
        return t;
    }

    private void HideRecipe()
    {
        _persistTimer.Stop();
        PersistCurrentScript();
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
        _btnReset.Visible = has;
        _recipeKind.Visible = has;
        _emptyRecipe.Visible = !has;

        if (has)
        {
            _recipeKind.Text = _recipe!.KindLabel;
            SetRecipeSide(_showEnable, flushPrevious: false);
        }
        else
        {
            _recipeKind.Text = "";
            _recipeNote.Visible = false;
            LayoutInner();
        }
    }

    private void SetRecipeSide(bool enable) => SetRecipeSide(enable, flushPrevious: true);

    /// <summary>导入配置后刷新当前正在显示的脚本内容。</summary>
    public void ReloadScriptsIfShowing()
    {
        if (_recipe is null || string.IsNullOrEmpty(_itemTitle) || !_recipeBox.Visible)
            return;
        _persistTimer.Stop();
        SetRecipeSide(_showEnable, flushPrevious: false);
    }

    private void SetRecipeSide(bool enable, bool flushPrevious)
    {
        if (flushPrevious)
        {
            _persistTimer.Stop();
            PersistCurrentScript();
        }

        _showEnable = enable;
        ApplyTabVisual(_tabEnable, enable);
        ApplyTabVisual(_tabDisable, !enable);
        if (_recipe is not null)
        {
            var text = SettingScriptStore.TryGet(_itemTitle, enable, out var custom)
                ? custom
                : _recipe.ContentFor(enable);
            _recipeBox.SetScript(text, _recipe.Kind);
            UpdateOverrideHint();
        }
        LayoutInner();
    }

    private void PersistCurrentScript()
    {
        if (_recipe is null || string.IsNullOrEmpty(_itemTitle) || !_recipeBox.Visible)
            return;
        var current = CurrentScriptText();
        var builtin = _recipe.ContentFor(_showEnable);
        if (ScriptsEqual(current, builtin))
            SettingScriptStore.Remove(_itemTitle, _showEnable);
        else
            SettingScriptStore.Set(_itemTitle, _showEnable, current);
    }

    private void ResetRecipeToBuiltin()
    {
        if (_recipe is null) return;
        _persistTimer.Stop();
        SettingScriptStore.Remove(_itemTitle, _showEnable);
        _recipeBox.SetScript(_recipe.ContentFor(_showEnable), _recipe.Kind);
        UpdateOverrideHint();
        LayoutInner();
    }

    private void UpdateOverrideHint()
    {
        if (_recipe is null)
        {
            _recipeNote.Visible = false;
            return;
        }

        var customized = SettingScriptStore.HasOverride(_itemTitle, _showEnable);
        if (customized)
        {
            _recipeNote.Text = "已保存你的修改（下次打开仍有效）。「导出」写出文件；「恢复默认」还原内置脚本。";
            _btnReset.Enabled = true;
        }
        else if (_recipe.Note.Length > 0)
        {
            _recipeNote.Text = _recipe.Note + " · 修改会自动记住。";
            _btnReset.Enabled = false;
        }
        else
        {
            _recipeNote.Text = "可直接改字，修改会自动记住；「导出」写出文件。";
            _btnReset.Enabled = false;
        }
        _recipeNote.Visible = true;
    }

    private static bool ScriptsEqual(string a, string b)
    {
        static string Norm(string s) =>
            (s ?? "").Replace("\r\n", "\n").Replace('\r', '\n').TrimEnd();
        return string.Equals(Norm(a), Norm(b), StringComparison.Ordinal);
    }

    private string CurrentScriptText() => _recipeBox.PlainText;

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

    private void ExportRecipe()
    {
        if (!_recipeBox.Visible) return;
        _persistTimer.Stop();
        PersistCurrentScript();
        UpdateOverrideHint();
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
            Title = "导出配置脚本",
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
            _btnSave.Text = "已导出";
            var t = new System.Windows.Forms.Timer { Interval = 1200 };
            t.Tick += (_, _) =>
            {
                _btnSave.Text = "导出";
                t.Stop();
                t.Dispose();
            };
            t.Start();
        }
        catch (Exception ex)
        {
            MessageBox.Show("导出失败：\n" + ex.Message, AppBrand.ProductName,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BuildSections(IReadOnlyList<(string Head, string Body)> items)
    {
        _sections.Controls.Clear();
        _sections.AutoSize = false;
        foreach (var (head, body) in items)
        {
            if (!string.IsNullOrEmpty(head))
            {
                _sections.Controls.Add(new Label
                {
                    Text = head,
                    AutoSize = false,
                    ForeColor = AppTheme.PrimaryDeep,
                    Font = new Font("Microsoft YaHei UI", 8.75F, FontStyle.Bold),
                    BackColor = Color.Transparent,
                    Tag = "h",
                });
            }
            _sections.Controls.Add(new Label
            {
                Text = body,
                AutoSize = false,
                ForeColor = AppTheme.TextMain,
                Font = new Font("Microsoft YaHei UI", 8.5F),
                BackColor = Color.Transparent,
                Tag = "b",
            });
        }
        LayoutInner();
    }

    private void LayoutInner()
    {
        var w = Math.Max(240, ClientSize.Width - PadX * 2);
        const int dockBtn = 24;
        const int dockGap = 4;
        var dockTop = 8;
        // 从右到左：关闭 | 靠底 | 靠右
        _dockClose.SetBounds(ClientSize.Width - PadX - dockBtn, dockTop, dockBtn, dockBtn);
        _dockBottom.SetBounds(_dockClose.Left - dockGap - dockBtn, dockTop, dockBtn, dockBtn);
        _dockRight.SetBounds(_dockBottom.Left - dockGap - dockBtn, dockTop, dockBtn, dockBtn);
        _dockRight.BringToFront();
        _dockBottom.BringToFront();
        _dockClose.BringToFront();

        var captionW = Math.Max(80, _dockRight.Left - PadX - 8);
        _caption.Left = PadX;
        _caption.Width = captionW;
        _title.Left = PadX;
        _summary.Left = PadX;
        _sections.Left = PadX;
        _footer.Left = PadX;
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
            // 先排简要说明，再把剩余高度留给可编辑脚本区
            _sections.Top = y;
            RelayoutSectionLabels(w);
            y = _sections.Bottom + 8;

            _recipeHost.Top = y;
            var avail = ClientSize.Height - y - 8;
            if (avail < 200) avail = 200;
            LayoutRecipe(w, avail);
            y = _recipeHost.Bottom + 8;
        }
        else
        {
            _sections.Top = y;
            RelayoutSectionLabels(w);
            y = _sections.Bottom + 8;
        }

        _footer.Top = y;
        var footerH = string.IsNullOrEmpty(_footer.Text)
            ? 0
            : TextRenderer.MeasureText(
                _footer.Text, _footer.Font, new Size(w, int.MaxValue),
                TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPrefix).Height + 4;
        _footer.Height = Math.Max(footerH, 8);
        AutoScrollMinSize = new Size(0, Math.Max(_footer.Bottom + 12, ClientSize.Height));
    }

    /// <summary>按当前宽度重新测量并纵向排布说明块，避免换行高度变化后文字重叠。</summary>
    private void RelayoutSectionLabels(int w)
    {
        _sections.SuspendLayout();
        var y = 0;
        foreach (Control c in _sections.Controls)
        {
            c.Left = 0;
            c.Width = w;
            if (c is RecommendStarsRow stars)
            {
                stars.Height = 22;
                stars.Top = y;
                y += stars.Height + 4;
                continue;
            }

            if (c is not Label lbl) continue;
            var isHead = Equals(lbl.Tag, "h");
            var h = TextRenderer.MeasureText(
                lbl.Text, lbl.Font, new Size(w, int.MaxValue),
                TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPrefix).Height + 2;
            if (isHead)
                h = Math.Max(18, h);
            else
                h = Math.Max(16, h);
            lbl.Top = y;
            lbl.Height = h;
            y += h + (isHead ? 4 : 6);
        }
        _sections.Height = Math.Max(y, 8);
        _sections.ResumeLayout(true);
    }

    public void FocusRecipe()
    {
        if (!_recipeHost.Visible) return;
        AutoScrollPosition = new Point(0, Math.Max(0, _recipeHost.Top - 8));
        if (_recipeBox.Visible)
            _recipeBox.Focus();
    }

    private void LayoutRecipe(int w, int hostHeight = 0)
    {
        var inner = Math.Max(220, w - 16);
        _recipeBox.Width = inner;
        _recipeBox.ReadOnly = false;
        _emptyRecipe.SetBounds(8, 28, inner, 60);
        _recipeNote.Width = inner;
        _recipeNote.MaximumSize = new Size(inner, 0);

        if (_recipeNote.Visible && _recipeNote.Text.Length > 0)
        {
            var noteH = TextRenderer.MeasureText(
                _recipeNote.Text, _recipeNote.Font, new Size(inner, int.MaxValue),
                TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl).Height + 4;
            _recipeNote.Height = Math.Min(noteH, 72);
        }

        if (_emptyRecipe.Visible)
        {
            _recipeHost.Height = Math.Max(100, hostHeight > 0 ? Math.Min(hostHeight, 140) : 100);
            return;
        }

        // 类型标签跟在标题后，避免与长标题重叠
        _recipeKind.Location = new Point(_recipeCaption.Right + 8, 8);

        const int tabsBottom = 58;
        const int btnH = 26;
        const int btnGap = 6;
        var noteBlock = (_recipeNote.Visible && _recipeNote.Text.Length > 0) ? (_recipeNote.Height + 6) : 0;
        var chrome = tabsBottom + btnGap + btnH + noteBlock + 10;
        var boxH = hostHeight > 0
            ? Math.Max(160, hostHeight - chrome)
            : Math.Max(160, _recipeBox.Height);
        _recipeBox.Height = boxH;
        _recipeBox.Location = new Point(8, tabsBottom);

        var x = 8;
        _btnCopy.Location = new Point(x, _recipeBox.Bottom + btnGap);
        x += _btnCopy.Width + 8;
        _btnSave.Location = new Point(x, _recipeBox.Bottom + btnGap);
        x += _btnSave.Width + 8;
        _btnReset.Location = new Point(x, _recipeBox.Bottom + btnGap);
        if (_recipeNote.Visible)
        {
            _recipeNote.Location = new Point(8, _btnCopy.Bottom + 6);
            _recipeHost.Height = hostHeight > 0 ? hostHeight : _recipeNote.Bottom + 10;
        }
        else
        {
            _recipeHost.Height = hostHeight > 0 ? hostHeight : _btnCopy.Bottom + 10;
        }
    }
}
