namespace SrvDesk;

/// <summary>单独窗口展示某优化项的开启/关闭配置脚本（可编辑、自动记住、导出）。可自由缩放。</summary>
internal sealed class SettingRecipeDialog : Form
{
    private readonly SettingActionRecipe? _recipe;
    private readonly string _itemTitle;
    private readonly ScriptSyntaxEditor _box = new();
    private readonly Button _tabOn;
    private readonly Button _tabOff;
    private readonly Label _head = new();
    private readonly Label _hint = new();
    private readonly Label _kind = new();
    private readonly Label _note = new();
    private readonly Button _copy;
    private readonly Button _export;
    private readonly Button _reset;
    private readonly Button _close;
    private readonly System.Windows.Forms.Timer _persistTimer = new() { Interval = 600 };
    private bool _showEnable = true;

    public static void ShowFor(IWin32Window? owner, string itemTitle, SettingHelpInfo help)
    {
        using var dlg = new SettingRecipeDialog(itemTitle, help);
        dlg.ShowDialog(owner);
    }

    private SettingRecipeDialog(string itemTitle, SettingHelpInfo help)
    {
        _itemTitle = itemTitle;
        _recipe = SettingRecipeCatalog.Get(help);

        Text = "配置脚本 · " + itemTitle;
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        AutoScaleMode = AutoScaleMode.None;
        ClientSize = UiScale.Size(920, 640);
        MinimumSize = UiScale.Size(780, 520);
        BackColor = AppTheme.SurfaceCard;
        Font = UiFit.UiFont;

        var body = ThemedSettingsChrome.CreateBodyPanel();
        body.AutoScroll = false;
        body.Padding = new Padding(UiScale.S(20), UiScale.S(14), UiScale.S(20), UiScale.S(10));

        var top = ThemedSettingsChrome.CreateToggleStack();
        top.Dock = DockStyle.Top;
        top.AutoSize = true;
        top.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        top.Padding = Padding.Empty;

        _head.Text = itemTitle;
        _head.Font = UiFit.UiFontSection;
        _head.ForeColor = AppTheme.PrimaryDeep;
        _head.AutoEllipsis = true;
        _head.AutoSize = false;
        _head.Height = Math.Max(UiScale.S(28), UiFit.LineHeight(UiFit.UiFontSection) + UiScale.S(8));
        _head.TextAlign = ContentAlignment.MiddleLeft;
        _head.Margin = new Padding(0, 0, 0, UiScale.S(4));

        _hint.Text = "";
        _hint.Visible = false;
        _hint.Height = 0;
        _hint.Margin = Padding.Empty;

        _tabOn = StyleTab("开启（优化）");
        _tabOff = StyleTab("关闭（恢复）");
        _tabOn.Click += (_, _) => SetSide(true, flush: true);
        _tabOff.Click += (_, _) => SetSide(false, flush: true);

        _kind.AutoSize = false;
        _kind.ForeColor = AppTheme.TextMute;
        _kind.Font = UiFit.UiFontSmall;
        _kind.TextAlign = ContentAlignment.MiddleLeft;
        _kind.Height = Math.Max(UiScale.S(22), UiFit.ControlHeight(UiFit.UiFontSmall));

        var tabs = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Margin = new Padding(0, 0, 0, UiScale.S(8)),
            Padding = Padding.Empty,
        };
        _tabOn.Margin = new Padding(0, 0, UiScale.S(8), UiScale.S(4));
        _tabOff.Margin = new Padding(0, 0, UiScale.S(12), UiScale.S(4));
        _kind.Margin = new Padding(0, UiScale.S(6), 0, UiScale.S(4));
        _kind.AutoSize = true;
        tabs.Controls.AddRange([_tabOn, _tabOff, _kind]);

        top.Controls.AddRange([_head, _hint, tabs]);

        _box.Dock = DockStyle.Fill;
        _box.UserScriptChanged += (_, _) =>
        {
            _persistTimer.Stop();
            _persistTimer.Start();
        };
        _persistTimer.Tick += (_, _) =>
        {
            _persistTimer.Stop();
            Persist();
            RefreshNote();
        };

        _note.Dock = DockStyle.Bottom;
        _note.ForeColor = AppTheme.PrimaryDark;
        _note.Font = UiFit.UiFontScope;
        _note.AutoSize = false;
        _note.Padding = new Padding(0, UiScale.S(8), 0, UiScale.S(4));

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = UiFit.ControlHeight() + UiScale.S(20),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoScroll = false,
            Padding = new Padding(0, UiScale.S(8), 0, 0),
        };
        UiBuffer.ConfigureNoScrollRow(actions);

        _copy = StyleAction("复制到剪贴板", false);
        _copy.Click += (_, _) => CopyClipboard();
        _export = StyleAction("导出", false);
        _export.Click += (_, _) => ExportAs();
        _reset = StyleAction("恢复默认", false);
        _reset.Margin = new Padding(UiScale.S(8), 0, 0, UiScale.S(4));
        _reset.Click += (_, _) =>
        {
            if (_recipe is null) return;
            _persistTimer.Stop();
            SettingScriptStore.Remove(_itemTitle, _showEnable);
            _box.SetScript(_recipe.ContentFor(_showEnable), _recipe.Kind);
            RefreshNote();
        };
        _close = StyleAction("关闭", true);
        _close.Margin = new Padding(UiScale.S(16), 0, 0, UiScale.S(4));
        _close.Click += (_, _) => Close();

        _copy.Margin = new Padding(0, 0, 0, UiScale.S(4));
        _export.Margin = new Padding(UiScale.S(8), 0, 0, UiScale.S(4));
        actions.Controls.AddRange([_copy, _export, _reset, _close]);

        body.Controls.Add(_box);
        body.Controls.Add(_note);
        body.Controls.Add(actions);
        body.Controls.Add(top);
        body.Resize += (_, _) =>
        {
            ThemedSettingsChrome.StretchStackChildren(top);
            FitNoteHeight();
        };

        Controls.Add(body);

        FormClosing += (_, _) =>
        {
            _persistTimer.Stop();
            Persist();
        };

        if (_recipe is null)
        {
            _tabOn.Enabled = false;
            _tabOff.Enabled = false;
            _copy.Enabled = false;
            _export.Enabled = false;
            _reset.Enabled = false;
            _kind.Text = "";
            _box.SetScript(
                "此项为组合操作（DISM / 多服务 / 右键菜单集成等），未单独收录可复制脚本。\r\n\r\n请直接在列表中切换开关，再点「应用到系统」。",
                SettingActionKind.Mixed);
            _note.Text = "";
        }
        else
        {
            _kind.Text = _recipe.KindLabel;
            SetSide(true, flush: false);
        }

        Load += (_, _) =>
        {
            ThemedSettingsChrome.StretchStackChildren(top);
            FitNoteHeight();
            PaintTab(_tabOn, _showEnable);
            PaintTab(_tabOff, !_showEnable);
        };
        AcceptButton = _close;
    }

    private void FitNoteHeight()
    {
        if (string.IsNullOrEmpty(_note.Text))
        {
            _note.Visible = false;
            _note.Height = 0;
            return;
        }

        _note.Visible = true;
        var w = Math.Max(200, _note.ClientSize.Width > 0 ? _note.ClientSize.Width : ClientSize.Width - UiScale.S(48));
        var h = TextRenderer.MeasureText(
            _note.Text,
            _note.Font,
            new Size(w, int.MaxValue),
            TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height;
        _note.Height = Math.Max(UiScale.S(28), h + UiScale.S(16));
    }

    private void CopyClipboard()
    {
        try
        {
            Clipboard.SetText(_box.PlainText);
            _copy.Text = "已复制";
            UiFit.FitButton(_copy, padding: 22);
            var t = new System.Windows.Forms.Timer { Interval = 1200 };
            t.Tick += (_, _) =>
            {
                _copy.Text = "复制到剪贴板";
                UiFit.FitButton(_copy, padding: 22);
                t.Stop();
                t.Dispose();
            };
            t.Start();
        }
        catch
        {
            MessageBox.Show(this, "无法写入剪贴板。", AppBrand.ProductName,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void SetSide(bool enable, bool flush)
    {
        if (flush)
        {
            _persistTimer.Stop();
            Persist();
        }
        _showEnable = enable;
        PaintTab(_tabOn, enable);
        PaintTab(_tabOff, !enable);
        if (_recipe is not null)
        {
            var text = SettingScriptStore.TryGet(_itemTitle, enable, out var custom)
                ? custom
                : _recipe.ContentFor(enable);
            _box.SetScript(text, _recipe.Kind);
            RefreshNote();
        }
        FitNoteHeight();
    }

    private void Persist()
    {
        if (_recipe is null) return;
        var current = _box.PlainText;
        var builtin = _recipe.ContentFor(_showEnable);
        if (ScriptsEqual(current, builtin))
            SettingScriptStore.Remove(_itemTitle, _showEnable);
        else
            SettingScriptStore.Set(_itemTitle, _showEnable, current);
    }

    private void RefreshNote()
    {
        if (_recipe is null)
        {
            _note.Text = "";
            FitNoteHeight();
            return;
        }
        var customized = SettingScriptStore.HasOverride(_itemTitle, _showEnable);
        _reset.Enabled = customized;
        _note.Text = customized
            ? "已记住你的修改。可导出文件，或点「恢复默认」还原内置脚本。"
            : (_recipe.Note.Length > 0 ? _recipe.Note + " · 修改会自动记住。" : "修改会自动记住。");
        FitNoteHeight();
    }

    private static bool ScriptsEqual(string a, string b)
    {
        static string Norm(string s) =>
            (s ?? "").Replace("\r\n", "\n").Replace('\r', '\n').TrimEnd();
        return string.Equals(Norm(a), Norm(b), StringComparison.Ordinal);
    }

    private void ExportAs()
    {
        Persist();
        var ext = _recipe?.FileExtension ?? ".txt";
        var filter = _recipe?.Kind switch
        {
            SettingActionKind.Reg => "注册表 (*.reg)|*.reg|所有文件 (*.*)|*.*",
            SettingActionKind.Cmd => "批处理 (*.cmd)|*.cmd|所有文件 (*.*)|*.*",
            SettingActionKind.PowerShell => "PowerShell (*.ps1)|*.ps1|所有文件 (*.*)|*.*",
            _ => "文本 (*.txt)|*.txt|所有文件 (*.*)|*.*",
        };
        using var dlg = new SaveFileDialog
        {
            Title = "导出配置脚本",
            Filter = filter,
            FileName = _recipe?.SuggestedFileName(_itemTitle, _showEnable)
                       ?? ("配置脚本" + (_showEnable ? "-开启" : "-关闭") + ext),
            OverwritePrompt = true,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            File.WriteAllText(dlg.FileName, _box.PlainText,
                new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "导出失败：\n" + ex.Message, AppBrand.ProductName,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static Button StyleTab(string text)
    {
        var b = ThemedSettingsChrome.CreateButton(text, false);
        UiFit.FitButton(b, padding: 22);
        PaintTab(b, selected: text.StartsWith("开启", StringComparison.Ordinal));
        return b;
    }

    private static void PaintTab(Button b, bool selected)
    {
        if (selected)
        {
            b.BackColor = AppTheme.Primary;
            b.ForeColor = AppTheme.TextOnPrimary;
            b.FlatAppearance.BorderSize = 0;
        }
        else
        {
            b.BackColor = AppTheme.SurfaceCard;
            b.ForeColor = AppTheme.TextMain;
            b.FlatAppearance.BorderColor = AppTheme.Border;
            b.FlatAppearance.BorderSize = 1;
        }
        b.Invalidate();
    }

    private static Button StyleAction(string text, bool primary)
    {
        var b = ThemedSettingsChrome.CreateButton(text, primary);
        if (!primary)
            b.ForeColor = AppTheme.PrimaryDeep;
        UiFit.FitButton(b, padding: 22);
        return b;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _persistTimer.Dispose();
        base.Dispose(disposing);
    }
}
