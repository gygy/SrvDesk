namespace SrvDesk;

/// <summary>单独窗口展示某优化项的开启/关闭配置脚本（可编辑、自动记住、导出）。可自由缩放。</summary>
internal sealed class SettingRecipeDialog : Form
{
    private readonly SettingActionRecipe? _recipe;
    private readonly string _itemTitle;
    private readonly ScriptSyntaxEditor _box = new();
    private readonly Button _tabOn = new();
    private readonly Button _tabOff = new();
    private readonly Label _head = new();
    private readonly Label _hint = new();
    private readonly Label _kind = new();
    private readonly Label _note = new();
    private readonly Button _copy = new();
    private readonly Button _export = new();
    private readonly Button _reset = new();
    private readonly Button _close = new();
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
        ClientSize = new Size(640, 480);
        MinimumSize = new Size(480, 360);
        BackColor = AppTheme.SurfaceCard;
        Font = UiFit.UiFont;
        Padding = new Padding(16);

        _head.Text = itemTitle;
        _head.Font = UiFit.UiFontBold(10F);
        _head.ForeColor = AppTheme.PrimaryDeep;
        _head.AutoEllipsis = true;
        _head.AutoSize = false;

        _hint.Text = "可直接编辑；修改会自动记住。可复制或导出为文件。";
        _hint.ForeColor = AppTheme.TextMute;
        _hint.Font = UiFit.UiFontSmall;
        _hint.AutoSize = false;

        StyleTab(_tabOn, "开启（优化）");
        StyleTab(_tabOff, "关闭（恢复）");
        _tabOn.Click += (_, _) => SetSide(true, flush: true);
        _tabOff.Click += (_, _) => SetSide(false, flush: true);

        _kind.AutoSize = true;
        _kind.ForeColor = AppTheme.TextMute;
        _kind.Font = UiFit.UiFontSmall;

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

        _note.ForeColor = AppTheme.PrimaryDark;
        _note.Font = UiFit.UiFontScope;
        _note.AutoSize = false;

        StyleAction(_copy, "复制到剪贴板");
        _copy.Click += (_, _) => CopyClipboard();

        StyleAction(_export, "导出");
        _export.Click += (_, _) => ExportAs();

        StyleAction(_reset, "恢复默认");
        _reset.Click += (_, _) =>
        {
            if (_recipe is null) return;
            _persistTimer.Stop();
            SettingScriptStore.Remove(_itemTitle, _showEnable);
            _box.SetScript(_recipe.ContentFor(_showEnable), _recipe.Kind);
            RefreshNote();
        };

        StyleAction(_close, "关闭");
        _close.Click += (_, _) => Close();

        FormClosing += (_, _) =>
        {
            _persistTimer.Stop();
            Persist();
        };
        Resize += (_, _) => LayoutContent();

        Controls.Add(_head);
        Controls.Add(_hint);
        Controls.Add(_tabOn);
        Controls.Add(_tabOff);
        Controls.Add(_kind);
        Controls.Add(_box);
        Controls.Add(_note);
        Controls.Add(_copy);
        Controls.Add(_export);
        Controls.Add(_reset);
        Controls.Add(_close);

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

        LayoutContent();
    }

    private void LayoutContent()
    {
        const int pad = 16;
        var w = Math.Max(320, ClientSize.Width - pad * 2);
        var y = pad;

        _head.SetBounds(pad, y, w, Math.Max(22, UiFit.LineHeight(_head.Font) + 4));
        y = _head.Bottom + 4;

        _hint.SetBounds(pad, y, w, Math.Max(18, UiFit.LineHeight(_hint.Font) + 2));
        y = _hint.Bottom + 10;

        _tabOn.Location = new Point(pad, y);
        _tabOff.Location = new Point(pad + _tabOn.Width + 8, y);
        _kind.Location = new Point(_tabOff.Right + 12, y + Math.Max(0, (_tabOn.Height - _kind.PreferredHeight) / 2));
        // 编辑框必须在 Tab 实际底边之下，避免盖住文字
        y = Math.Max(_tabOn.Bottom, _tabOff.Bottom) + 10;

        var btnH = Math.Max(_copy.Height, UiFit.ControlHeight());
        var noteH = string.IsNullOrEmpty(_note.Text)
            ? 0
            : Math.Max(UiFit.LineHeight(_note.Font) + 8,
                TextRenderer.MeasureText(_note.Text, _note.Font, new Size(w, int.MaxValue),
                    TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height + 6);
        var bottomChrome = (noteH > 0 ? noteH + 8 : 0) + btnH + pad + 8;
        var boxH = Math.Max(120, ClientSize.Height - y - bottomChrome);
        _box.SetBounds(pad, y, w, boxH);
        _box.BringToFront();
        _tabOn.BringToFront();
        _tabOff.BringToFront();
        _kind.BringToFront();

        y = _box.Bottom + 8;
        if (noteH > 0)
        {
            _note.Visible = true;
            _note.SetBounds(pad, y, w, noteH);
            y = _note.Bottom + 8;
        }
        else
        {
            _note.Visible = false;
            _note.Height = 0;
        }

        _copy.Location = new Point(pad, y);
        _export.Location = new Point(_copy.Right + 8, y);
        _reset.Location = new Point(_export.Right + 8, y);
        _close.Location = new Point(Math.Max(_reset.Right + 8, pad + w - _close.Width), y);
    }

    private void CopyClipboard()
    {
        try
        {
            Clipboard.SetText(_box.PlainText);
            _copy.Text = "已复制";
            UiFit.FitButton(_copy);
            LayoutContent();
            var t = new System.Windows.Forms.Timer { Interval = 1200 };
            t.Tick += (_, _) =>
            {
                _copy.Text = "复制到剪贴板";
                UiFit.FitButton(_copy);
                LayoutContent();
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
        LayoutContent();
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
            return;
        }
        var customized = SettingScriptStore.HasOverride(_itemTitle, _showEnable);
        _reset.Enabled = customized;
        _note.Text = customized
            ? "已记住你的修改。可导出文件，或点「恢复默认」还原内置脚本。"
            : (_recipe.Note.Length > 0 ? _recipe.Note + " · 修改会自动记住。" : "修改会自动记住。");
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

    private static void StyleTab(Button b, string text)
    {
        b.Text = text;
        b.Font = UiFit.UiFont;
        b.Size = UiFit.ButtonSize(text, UiFit.ControlHeight(b.Font), b.Font, minWidth: 88, padding: 22);
        b.FlatStyle = FlatStyle.Flat;
        b.Cursor = Cursors.Hand;
        b.FlatAppearance.BorderSize = 1;
        PaintTab(b, selected: text.StartsWith("开启", StringComparison.Ordinal));
        UiFit.EnableCenteredFlatText(b);
    }

    private static void PaintTab(Button b, bool selected)
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
        b.Invalidate();
    }

    private static void StyleAction(Button b, string text)
    {
        b.Text = text;
        b.Font = UiFit.UiFont;
        b.Size = UiFit.ButtonSize(text, UiFit.ControlHeight(b.Font), b.Font, padding: 22);
        b.FlatStyle = FlatStyle.Flat;
        b.BackColor = Color.White;
        b.ForeColor = AppTheme.PrimaryDeep;
        b.Cursor = Cursors.Hand;
        b.FlatAppearance.BorderColor = AppTheme.Primary;
        UiFit.EnableCenteredFlatText(b);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _persistTimer.Dispose();
        base.Dispose(disposing);
    }
}
