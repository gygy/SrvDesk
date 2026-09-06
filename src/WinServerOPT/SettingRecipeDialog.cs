namespace WinOpt;

/// <summary>单独窗口展示某优化项的开启/关闭一键脚本，便于复制与另存为。</summary>
internal sealed class SettingRecipeDialog : Form
{
    private readonly SettingActionRecipe? _recipe;
    private readonly string _itemTitle;
    private readonly ScriptSyntaxEditor _box = new();
    private readonly Button _tabOn = new();
    private readonly Button _tabOff = new();
    private readonly Label _kind = new();
    private readonly Label _note = new();
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
            Text = "语法高亮显示。可直接编辑，再复制或保存为文件手工执行。",
            Location = new Point(16, 38),
            Size = new Size(520, 20),
            ForeColor = AppTheme.TextMute,
            Font = new Font("Microsoft YaHei UI", 8.5F),
        };

        StyleTab(_tabOn, "开启（优化）");
        StyleTab(_tabOff, "关闭（恢复）");
        _tabOn.Location = new Point(16, 66);
        _tabOff.Location = new Point(130, 66);
        _tabOn.Click += (_, _) => SetSide(true);
        _tabOff.Click += (_, _) => SetSide(false);

        _kind.Location = new Point(250, 70);
        _kind.AutoSize = true;
        _kind.ForeColor = AppTheme.TextMute;
        _kind.Font = new Font("Microsoft YaHei UI", 8.5F);

        _box.Location = new Point(16, 100);
        _box.Size = new Size(528, 230);

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
                var t = new System.Windows.Forms.Timer { Interval = 1200 };
                t.Tick += (_, _) => { copy.Text = "复制到剪贴板"; t.Stop(); t.Dispose(); };
                t.Start();
            }
            catch
            {
                MessageBox.Show(this, "无法写入剪贴板。", AppBrand.ProductName,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };

        var save = ActionButton("保存…", 140);
        save.Click += (_, _) => SaveAs();

        var close = ActionButton("关闭", 456);
        close.Click += (_, _) => Close();

        Controls.Add(head);
        Controls.Add(hint);
        Controls.Add(_tabOn);
        Controls.Add(_tabOff);
        Controls.Add(_kind);
        Controls.Add(_box);
        Controls.Add(_note);
        Controls.Add(copy);
        Controls.Add(save);
        Controls.Add(close);

        if (_recipe is null)
        {
            _tabOn.Enabled = false;
            _tabOff.Enabled = false;
            copy.Enabled = false;
            save.Enabled = false;
            _kind.Text = "";
            _box.SetScript(
                "此项为组合操作（DISM / 多服务 / 右键菜单集成等），未单独收录可复制脚本。\r\n\r\n请直接在列表中切换开关，再点「应用到系统」。",
                SettingActionKind.Mixed);
            _note.Text = "";
        }
        else
        {
            _kind.Text = _recipe.KindLabel;
            _note.Text = _recipe.Note;
            SetSide(true);
        }
    }

    private void SetSide(bool enable)
    {
        _showEnable = enable;
        PaintTab(_tabOn, enable);
        PaintTab(_tabOff, !enable);
        if (_recipe is not null)
            _box.SetScript(_recipe.ContentFor(enable), _recipe.Kind);
    }

    private void SaveAs()
    {
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
            Title = "保存配置脚本",
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
            MessageBox.Show(this, "保存失败：\n" + ex.Message, AppBrand.ProductName,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void StyleTab(Button b, string text)
    {
        b.Text = text;
        b.Size = new Size(108, 28);
        b.FlatStyle = FlatStyle.Flat;
        b.Font = new Font("Microsoft YaHei UI", 9F);
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
            Size = new Size(112, 30),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = AppTheme.PrimaryDeep,
            Cursor = Cursors.Hand,
        };
        b.FlatAppearance.BorderColor = AppTheme.Primary;
        return b;
    }
}
