namespace WinOpt;

/// <summary>单独窗口展示某优化项的开启/关闭配置脚本（可编辑、自动记住、导出）。</summary>
internal sealed class SettingRecipeDialog : Form
{
    private readonly SettingActionRecipe? _recipe;
    private readonly string _itemTitle;
    private readonly ScriptSyntaxEditor _box = new();
    private readonly Button _tabOn = new();
    private readonly Button _tabOff = new();
    private readonly Label _kind = new();
    private readonly Label _note = new();
    private readonly System.Windows.Forms.Timer _persistTimer = new() { Interval = 600 };
    private bool _showEnable = true;
    private Button? _resetBtn;

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
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        ClientSize = new Size(560, 420);
        BackColor = AppTheme.SurfaceCard;
        Font = new Font("Microsoft YaHei UI", 9F);

        var head = new Label
        {
            Text = itemTitle,
            Location = new Point(16, 12),
            Size = new Size(520, 24),
            Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
            ForeColor = AppTheme.PrimaryDeep,
            AutoEllipsis = true,
        };

        var hint = new Label
        {
            Text = "可直接编辑；修改会自动记住。可复制或导出为文件。",
            Location = new Point(16, 38),
            Size = new Size(520, 20),
            ForeColor = AppTheme.TextMute,
            Font = new Font("Microsoft YaHei UI", 8.5F),
        };

        StyleTab(_tabOn, "开启（优化）");
        StyleTab(_tabOff, "关闭（恢复）");
        _tabOn.Location = new Point(16, 66);
        _tabOff.Location = new Point(16 + _tabOn.Width + 8, 66);
        _tabOn.Click += (_, _) => SetSide(true, flush: true);
        _tabOff.Click += (_, _) => SetSide(false, flush: true);

        _kind.Location = new Point(_tabOff.Right + 12, 70);
        _kind.AutoSize = true;
        _kind.ForeColor = AppTheme.TextMute;
        _kind.Font = new Font("Microsoft YaHei UI", 8.5F);

        _box.Location = new Point(16, 100);
        _box.Size = new Size(528, 230);
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

        _note.Location = new Point(16, 336);
        _note.Size = new Size(528, 36);
        _note.ForeColor = AppTheme.PrimaryDark;
        _note.Font = new Font("Microsoft YaHei UI", 8.25F);

        var copy = ActionButton("复制到剪贴板", 16);
        copy.Click += (_, _) =>
        {
            try
            {
                Clipboard.SetText(_box.PlainText);
                copy.Text = "已复制";
                UiFit.FitButton(copy, 30);
                var t = new System.Windows.Forms.Timer { Interval = 1200 };
                t.Tick += (_, _) =>
                {
                    copy.Text = "复制到剪贴板";
                    UiFit.FitButton(copy, 30);
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
        };

        var export = ActionButton("导出", copy.Right + 8);
        export.Click += (_, _) => ExportAs();

        _resetBtn = ActionButton("恢复默认", export.Right + 8);
        _resetBtn.Click += (_, _) =>
        {
            if (_recipe is null) return;
            _persistTimer.Stop();
            SettingScriptStore.Remove(_itemTitle, _showEnable);
            _box.SetScript(_recipe.ContentFor(_showEnable), _recipe.Kind);
            RefreshNote();
        };

        var close = ActionButton("关闭", Math.Max(_resetBtn.Right + 8, ClientSize.Width - UiFit.ButtonWidth("关闭") - 16));
        close.Click += (_, _) => Close();

        FormClosing += (_, _) =>
        {
            _persistTimer.Stop();
            Persist();
        };

        Controls.Add(head);
        Controls.Add(hint);
        Controls.Add(_tabOn);
        Controls.Add(_tabOff);
        Controls.Add(_kind);
        Controls.Add(_box);
        Controls.Add(_note);
        Controls.Add(copy);
        Controls.Add(export);
        Controls.Add(_resetBtn);
        Controls.Add(close);

        if (_recipe is null)
        {
            _tabOn.Enabled = false;
            _tabOff.Enabled = false;
            copy.Enabled = false;
            export.Enabled = false;
            _resetBtn.Enabled = false;
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
        if (_recipe is null) return;
        var customized = SettingScriptStore.HasOverride(_itemTitle, _showEnable);
        if (_resetBtn is not null)
            _resetBtn.Enabled = customized;
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
        b.Font = new Font("Microsoft YaHei UI", 9F);
        b.Size = UiFit.ButtonSize(text, 28, b.Font, minWidth: 88, padding: 22);
        b.FlatStyle = FlatStyle.Flat;
        b.Cursor = Cursors.Hand;
        b.FlatAppearance.BorderSize = 1;
        PaintTab(b, selected: text.StartsWith("开启", StringComparison.Ordinal));
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
    }

    private Button ActionButton(string text, int x)
    {
        var b = new Button
        {
            Text = text,
            Location = new Point(x, 378),
            Size = UiFit.ButtonSize(text, 30, padding: 22),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = AppTheme.PrimaryDeep,
            Cursor = Cursors.Hand,
        };
        b.FlatAppearance.BorderColor = AppTheme.Primary;
        return b;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _persistTimer.Dispose();
        base.Dispose(disposing);
    }
}
