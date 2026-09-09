namespace SrvDesk;

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

        _caption.Text = AppLang.L("配置脚本", "Config script");
        _caption.SetBounds(PadX, 10, 200, 18);
        _caption.ForeColor = AppTheme.TextMute;
        _caption.Font = UiFit.UiFontScope;
        _caption.BackColor = Color.Transparent;

        StyleDockButton(_dockRight, MenuIcons.DockRight, AppLang.L("靠右停靠", "Dock right"));
        StyleDockButton(_dockBottom, MenuIcons.DockBottom, AppLang.L("靠底停靠", "Dock bottom"));
        StyleDockButton(_dockClose, MenuIcons.PanelClose, AppLang.L("关闭面板", "Close panel"));
        _dockRight.Click += (_, _) => DockRequested?.Invoke(ConfigScriptDock.Right);
        _dockBottom.Click += (_, _) => DockRequested?.Invoke(ConfigScriptDock.Bottom);
        _dockClose.Click += (_, _) => CloseRequested?.Invoke();

        _title.SetBounds(PadX, 30, 280, 40);
        _title.ForeColor = AppTheme.PrimaryDeep;
        _title.Font = UiFit.UiFontBold(10F);
        _title.BackColor = Color.Transparent;
        _title.AutoEllipsis = false;

        _summary.SetBounds(PadX, 72, 280, 52);
        _summary.ForeColor = AppTheme.TextMute;
        _summary.Font = UiFit.UiFontSmall;
        _summary.BackColor = Color.Transparent;
        _summary.AutoEllipsis = false;

        _sections.SetBounds(PadX, 128, 280, 10);
        _sections.BackColor = Color.Transparent;
        _sections.AutoSize = false;

        BuildRecipeHost();

        _footer.SetBounds(PadX, 140, 280, 36);
        _footer.ForeColor = AppTheme.PrimaryDark;
        _footer.Font = UiFit.UiFontScope;
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

        _recipeCaption.Text = AppLang.L("配置脚本", "Config script");
        _recipeCaption.Font = UiFit.UiFontBold(8.75F);
        _recipeCaption.ForeColor = AppTheme.PrimaryDeep;
        _recipeCaption.BackColor = Color.Transparent;
        _recipeCaption.AutoSize = true;
        _recipeCaption.Location = new Point(8, 6);

        _recipeKind.Font = UiFit.UiFontScope;
        _recipeKind.ForeColor = AppTheme.TextMute;
        _recipeKind.BackColor = Color.Transparent;
        _recipeKind.AutoSize = true;
        _recipeKind.Location = new Point(80, 8);

        StyleTab(_tabEnable, AppLang.L("开启", "On"), true);
        StyleTab(_tabDisable, AppLang.L("关闭", "Off"), false);
        _tabEnable.Location = new Point(8, 30);
        _tabDisable.Location = new Point(8 + _tabEnable.Width + 8, 30);
        _tabEnable.Click += (_, _) => SetRecipeSide(true);
        _tabDisable.Click += (_, _) => SetRecipeSide(false);

        // 初始位置仅占位；真正顶边由 LayoutRecipe 按 Tab 底边计算，避免压住首行
        _recipeBox.Location = new Point(8, 68);
        _recipeBox.Height = 220;
        _recipeBox.Width = 240;
        _recipeBox.ReadOnly = false;
        _recipeBox.DetectUrls = false;
        _tip.SetToolTip(_recipeBox, AppLang.L(
            "可直接编辑；改完会自动记住。可用复制/导出/恢复默认。",
            "Editable; changes are remembered. Copy / export / reset available."));

        StyleAction(_btnCopy, AppLang.L("复制", "Copy"));
        StyleAction(_btnSave, AppLang.L("导出", "Export"));
        StyleAction(_btnReset, AppLang.L("恢复默认", "Reset"));
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

        // 提示改走面板 _footer，避免塞在脚本宿主底边被裁切
        _recipeNote.Visible = false;
        _recipeNote.Font = UiFit.UiFontScope;
        _recipeNote.ForeColor = AppTheme.PrimaryDark;
        _recipeNote.BackColor = Color.Transparent;
        _recipeNote.AutoSize = false;
        _recipeNote.MaximumSize = new Size(260, 0);

        _emptyRecipe.Text = AppLang.L(
            "此项为组合操作（DISM/多服务等），未单独收录脚本；请用左侧开关 +「应用到系统」。",
            "This item is a combined action (DISM/multi-service); use the left toggle + Apply.");
        _emptyRecipe.Font = UiFit.UiFontSmall;
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
        b.Font = UiFit.UiFontSmall;
        b.Size = UiFit.ButtonSize(text, UiFit.ControlHeight(b.Font), b.Font, minWidth: 56, padding: 20);
        b.FlatStyle = FlatStyle.Flat;
        b.Cursor = Cursors.Hand;
        b.FlatAppearance.BorderSize = 1;
        ApplyTabVisual(b, selected: primaryLook);
        UiFit.EnableCenteredFlatText(b);
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
        b.Font = UiFit.UiFontSmall;
        b.Size = UiFit.ButtonSize(text, UiFit.ControlHeight(b.Font), b.Font, minWidth: 56, padding: 20);
        b.FlatStyle = FlatStyle.Flat;
        b.BackColor = Color.White;
        b.ForeColor = AppTheme.PrimaryDeep;
        b.FlatAppearance.BorderColor = AppTheme.Primary;
        b.Cursor = Cursors.Hand;
        UiFit.EnableCenteredFlatText(b);
    }

    public void ShowEmbeddedGuide(string pageTitle)
    {
        HideRecipe();
        _caption.Text = AppLang.L("配置脚本", "Config script");
        _title.Text = pageTitle;
        _footer.Text = "";
        if (pageTitle == AppLang.L("服务优化", "Service optimize"))
        {
            _summary.Text = AppLang.L(
                "选中服务后此处显示说明。改动立即生效；改前会自动备份。",
                "Select a service to see notes. Changes apply immediately; auto-backup before edits.");
            BuildSections([
                (AppLang.L("备份/还原", "Backup/Restore"), AppLang.L(
                    "「备份」保存本机全部服务启动类型；「还原」可回退。批量修改前也会自动备份。",
                    "Backup saves all start types; Restore rolls back. Auto-backup also runs before batch changes.")),
                (AppLang.L("操作", "Actions"), AppLang.L("可按建议改启动类型；「保持」不会被批量改动。", "Change start type by advice; Keep items are skipped in batch.")),
            ]);
        }
        else
        {
            _summary.Text = AppLang.L("本页改动会立即写入系统。", "Changes on this page write to the system immediately.");
            BuildSections([]);
        }
    }

    /// <summary>服务优化页：展示当前选中服务的说明。</summary>
    public void ShowServiceOptimize(ServiceOptimizeRow? row)
    {
        HideRecipe();
        _caption.Text = AppLang.L("服务说明", "Service notes");
        if (row is null)
        {
            _title.Text = AppLang.L("服务优化", "Service optimize");
            _summary.Text = AppLang.L(
                "列表来自本机实时服务。点一项查看说明；有建议的可按建议调整。",
                "List is live from this PC. Select a row for notes; apply advice when suggested.");
            _footer.Text = "";
            BuildSections([
                (AppLang.L("建议", "Advice"), AppLang.L("禁用 / 手动 / 自动 / 保持。", "Disable / Manual / Auto / Keep.")),
            ]);
            return;
        }

        _title.Text = row.DisplayName;
        _summary.Text = row.AdviceNote;
        _footer.Text = "";

        _sections.Controls.Clear();
        _sections.AutoSize = false;
        _sections.Controls.Add(new RecommendStarsRow
        {
            Level = row.OptimizeLevel,
            TrailingText = RecommendLevelUi.Title(row.OptimizeLevel) + " · " + row.AdviceTag,
            Tag = "stars",
        });

        void AddPair(string head, string body)
        {
            _sections.Controls.Add(new Label
            {
                Text = head,
                AutoSize = false,
                ForeColor = AppTheme.PrimaryDeep,
                Font = UiFit.UiFontBold(8.75F),
                BackColor = Color.Transparent,
                Tag = "h",
            });
            _sections.Controls.Add(new Label
            {
                Text = body,
                AutoSize = false,
                ForeColor = AppTheme.TextMain,
                Font = UiFit.UiFontSmall,
                BackColor = Color.Transparent,
                Tag = "b",
            });
        }

        AddPair(AppLang.L("服务名", "Service"), row.ActualServiceName);
        AddPair(AppLang.L("说明", "Note"), row.AdviceNote);
        LayoutInner();
    }

    public void ShowPlaceholder(string? groupTitle = null)
    {
        HideRecipe();
        _caption.Text = AppLang.L("配置脚本", "Config script");
        _title.Text = groupTitle is null ? AppLang.L("选择左侧配置项", "Select an item on the left") : groupTitle;
        _summary.Text = AppLang.L(
            "点一项后，可查看并编辑开启/关闭脚本。",
            "Select an item to view and edit its on/off scripts.");
        _footer.Text = "";
        BuildSections([]);
    }

    public void ShowUsageGuide()
    {
        HideRecipe();
        _caption.Text = AppLang.L("使用说明", "How to use");
        _title.Text = AppBrand.ProductName;
        _summary.Text = AppLang.L(
            "勾选后点「应用到系统」。需管理员权限。",
            "Check items, then Apply. Administrator required.");
        _footer.Text = "";
        BuildSections([
            (AppLang.L("配置脚本", "Config script"), AppLang.L("点左侧项可查看对应脚本。", "Select an item to view its scripts.")),
            (AppLang.L("备份", "Backup"), AppLang.L("文件菜单可导入、导出配置。", "Import/export profiles from the File menu.")),
        ]);
    }

    public void ShowScopeLegend()
    {
        HideRecipe();
        _caption.Text = AppLang.L("标识图例", "Legend");
        _title.Text = AppLang.L("适用范围", "Scope");
        _summary.Text = AppLang.L(
            "名称下方标签表示该项适用的系统范围。",
            "Badges under the name show where the item applies.");
        _footer.Text = "";
        BuildSections([
            (AppLang.L("Server 专属", "Server only"), AppLang.L("仅 Windows Server。", "Windows Server only.")),
            (AppLang.L("需桌面体验", "Needs Desktop Experience"), AppLang.L("Server Core 不可用。", "Not available on Server Core.")),
        ]);
    }

    public void ShowSetting(string itemTitle, SettingHelpInfo help)
    {
        _caption.Text = AppLang.L("配置脚本 · 当前项", "Config script · current");
        _title.Text = itemTitle;
        _summary.Text = help.Summary;
        _footer.Text = "";
        BuildSettingBrief(help);

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
                Font = UiFit.UiFontSmall,
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
                Font = UiFit.UiFontSmall,
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
            _footer.Text = "";
            return;
        }

        // 提示放在面板 footer，单独量高排布，避免贴在脚本宿主底边被裁
        _recipeNote.Visible = false;
        var customized = SettingScriptStore.HasOverride(_itemTitle, _showEnable);
        if (customized)
        {
            _footer.Text = AppLang.L(
                "已保存你的修改（下次打开仍有效）。「导出」写出文件；「恢复默认」还原内置脚本。",
                "Your edits are saved. Export writes a file; Reset restores the built-in script.");
            _btnReset.Enabled = true;
        }
        else if (_recipe.Note.Length > 0)
        {
            _footer.Text = _recipe.Note + AppLang.L(" · 修改会自动记住。", " · Edits are remembered.");
            _btnReset.Enabled = false;
        }
        else
        {
            _footer.Text = AppLang.L(
                "可直接改字，修改会自动记住；「导出」写出文件。",
                "Edit freely; changes are remembered. Export writes a file.");
            _btnReset.Enabled = false;
        }
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
            _btnCopy.Text = AppLang.L("已复制", "Copied");
            var t = new System.Windows.Forms.Timer { Interval = 1200 };
            t.Tick += (_, _) =>
            {
                _btnCopy.Text = AppLang.L("复制", "Copy");
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
            SettingActionKind.Reg => AppLang.L("注册表 (*.reg)|*.reg|所有文件 (*.*)|*.*", "Registry (*.reg)|*.reg|All files (*.*)|*.*"),
            SettingActionKind.Cmd => AppLang.L("批处理 (*.cmd)|*.cmd|所有文件 (*.*)|*.*", "Batch (*.cmd)|*.cmd|All files (*.*)|*.*"),
            SettingActionKind.PowerShell => "PowerShell (*.ps1)|*.ps1|" + AppLang.L("所有文件 (*.*)|*.*", "All files (*.*)|*.*"),
            _ => AppLang.L("文本 (*.txt)|*.txt|所有文件 (*.*)|*.*", "Text (*.txt)|*.txt|All files (*.*)|*.*"),
        };
        var suggested = _recipe?.SuggestedFileName(_itemTitle, _showEnable)
                        ?? (AppLang.L("配置脚本", "config-script") + (_showEnable ? AppLang.L("-开启", "-on") : AppLang.L("-关闭", "-off")) + ext);

        using var dlg = new SaveFileDialog
        {
            Title = AppLang.L("导出配置脚本", "Export config script"),
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
            _btnSave.Text = AppLang.L("已导出", "Exported");
            var t = new System.Windows.Forms.Timer { Interval = 1200 };
            t.Tick += (_, _) =>
            {
                _btnSave.Text = AppLang.L("导出", "Export");
                t.Stop();
                t.Dispose();
            };
            t.Start();
        }
        catch (Exception ex)
        {
            MessageBox.Show(AppLang.L("导出失败：\n", "Export failed:\n") + ex.Message, AppBrand.ProductName,
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
                    Font = UiFit.UiFontBold(8.75F),
                    BackColor = Color.Transparent,
                    Tag = "h",
                });
            }
            _sections.Controls.Add(new Label
            {
                Text = body,
                AutoSize = false,
                ForeColor = AppTheme.TextMain,
                Font = UiFit.UiFontSmall,
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
            // 先排简要说明；footer 提示预留高度后再把剩余高度留给脚本区
            _sections.Top = y;
            RelayoutSectionLabels(w);
            y = _sections.Bottom + 8;

            var footerReserve = 0;
            if (!string.IsNullOrEmpty(_footer.Text))
            {
                footerReserve = TextRenderer.MeasureText(
                    _footer.Text, _footer.Font, new Size(w, int.MaxValue),
                    TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPrefix).Height
                    + UiFit.LineHeight(_footer.Font) + 20;
            }

            _recipeHost.Top = y;
            var avail = ClientSize.Height - y - footerReserve;
            if (avail < 180) avail = 180;
            LayoutRecipe(w, avail);
            y = _recipeHost.Bottom + 10;
        }
        else
        {
            _sections.Top = y;
            RelayoutSectionLabels(w);
            y = _sections.Bottom + 8;
        }

        _footer.Top = y;
        if (string.IsNullOrEmpty(_footer.Text))
        {
            _footer.Height = 0;
            _footer.Visible = false;
        }
        else
        {
            _footer.Visible = true;
            var footerH = TextRenderer.MeasureText(
                _footer.Text, _footer.Font, new Size(w, int.MaxValue),
                TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPrefix).Height + 10;
            // 至少一行高，避免字号/测量偏差时只露出字顶
            _footer.Height = Math.Max(footerH, UiFit.LineHeight(_footer.Font) + 10);
        }
        AutoScrollMinSize = new Size(0, Math.Max(_footer.Bottom + 20, ClientSize.Height));
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
        _recipeNote.Visible = false;

        if (_emptyRecipe.Visible)
        {
            _recipeHost.Height = Math.Max(100, hostHeight > 0 ? Math.Min(hostHeight, 140) : 100);
            return;
        }

        // 类型标签跟在标题后，避免与长标题重叠
        _recipeKind.Location = new Point(_recipeCaption.Right + 8, 8);

        _tabEnable.Location = new Point(8, 30);
        _tabDisable.Location = new Point(8 + _tabEnable.Width + 8, 30);
        // 按实际 Tab 底边留空，避免压住脚本首行
        var afterTabs = Math.Max(_tabEnable.Bottom, _tabDisable.Bottom) + 10;

        const int btnGap = 8;
        var btnH = Math.Max(_btnCopy.Height, 28);
        var chrome = afterTabs + btnGap + btnH + 14;
        var boxH = hostHeight > 0
            ? Math.Max(100, hostHeight - chrome)
            : Math.Max(160, _recipeBox.Height);
        _recipeBox.Height = boxH;
        _recipeBox.Location = new Point(8, afterTabs);
        // 编辑框在 Tab 之下，避免 z-order 盖住按钮文字；Tab 保持可点
        _recipeBox.SendToBack();
        _tabEnable.BringToFront();
        _tabDisable.BringToFront();

        var x = 8;
        var by = _recipeBox.Bottom + btnGap;
        _btnCopy.Location = new Point(x, by);
        x += _btnCopy.Width + 8;
        _btnSave.Location = new Point(x, by);
        x += _btnSave.Width + 8;
        _btnReset.Location = new Point(x, by);

        var contentH = _btnCopy.Bottom + 12;
        _recipeHost.Height = hostHeight > 0 ? Math.Max(hostHeight, contentH) : contentH;
        // 若强制撑满导致按钮贴底，仍以内容为准多留一点
        if (_btnCopy.Bottom + 12 > _recipeHost.Height)
            _recipeHost.Height = _btnCopy.Bottom + 12;
    }
}
