namespace SrvDesk;

/// <summary>工具：将默认软件安装路径改到非系统盘，或恢复到系统盘默认。</summary>
internal sealed class DefaultInstallPathDialog : Form
{
    private readonly Label _current64 = new();
    private readonly Label _current86 = new();
    private readonly ComboBox _drive = new();
    private readonly Label _preview64 = new();
    private readonly Label _preview86 = new();
    private readonly CheckBox _createDirs = new();
    private readonly Label _status = new();
    private readonly Button _apply;
    private readonly Button _restore;

    public DefaultInstallPathDialog()
    {
        Text = AppLang.L("默认安装路径", "Default install path");
        AppBrand.ApplyWindowIcon(this);
        AutoScaleMode = AutoScaleMode.None;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Font = UiFit.UiFont;
        BackColor = AppTheme.Surface;
        ClientSize = UiScale.Size(580, 380);
        MinimumSize = UiScale.Size(540, 360);

        var body = ThemedSettingsChrome.CreateBodyPanel();
        body.AutoScroll = false;

        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = AppTheme.SurfaceCard,
            Padding = new Padding(UiScale.S(16)),
        };
        card.Paint += (_, e) =>
        {
            using var pen = new Pen(AppTheme.BorderLight);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var form = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
        };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, UiScale.S(108)));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        var rowH = Math.Max(UiScale.S(36), UiFit.ControlHeight() + UiScale.S(8));
        for (var i = 0; i < 6; i++)
            form.RowStyles.Add(new RowStyle(SizeType.Absolute, rowH));

        AddRow(form, 0, AppLang.L("当前 64 位", "Current 64-bit"), StyleValue(_current64));
        AddRow(form, 1, AppLang.L("当前 32 位", "Current 32-bit"), StyleValue(_current86));

        _drive.DropDownStyle = ComboBoxStyle.DropDownList;
        _drive.Dock = DockStyle.Fill;
        UiFit.FitCombo(_drive);
        _drive.SelectedIndexChanged += (_, _) => UpdatePreview();
        AddRow(form, 2, AppLang.L("目标磁盘", "Target drive"), _drive);

        AddRow(form, 3, AppLang.L("将设为", "Will set"), StyleValue(_preview64));
        AddRow(form, 4, "", StyleValue(_preview86));

        _createDirs.Text = AppLang.L("若目录不存在则创建", "Create folders if missing");
        _createDirs.Checked = true;
        _createDirs.AutoSize = true;
        _createDirs.ForeColor = AppTheme.TextMain;
        _createDirs.Anchor = AnchorStyles.Left;
        AddRow(form, 5, "", _createDirs);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, UiScale.S(8), 0, 0),
        };
        UiBuffer.ConfigureNoScrollRow(actions);

        _apply = ThemedSettingsChrome.CreateButton(AppLang.L("应用到所选盘", "Apply to selected drive"), true);
        UiFit.FitButton(_apply, padding: 24);
        _apply.Click += (_, _) => ApplySelected();
        actions.Controls.Add(_apply);

        _restore = ThemedSettingsChrome.CreateButton(AppLang.L("恢复系统默认", "Restore system default"), false);
        UiFit.FitButton(_restore, padding: 24);
        _restore.Margin = new Padding(UiScale.S(8), 0, 0, 0);
        _restore.Click += (_, _) => RestoreDefault();
        actions.Controls.Add(_restore);

        _status.Dock = DockStyle.Fill;
        _status.ForeColor = AppTheme.TextMute;
        _status.Font = UiFit.UiFontSmall;
        _status.TextAlign = ContentAlignment.TopLeft;
        _status.Padding = new Padding(0, UiScale.S(10), 0, 0);

        root.Controls.Add(form, 0, 0);
        root.Controls.Add(actions, 0, 1);
        root.Controls.Add(_status, 0, 2);
        card.Controls.Add(root);
        body.Controls.Add(card);

        ThemedSettingsChrome.MountModal(
            this,
            Text,
            "",
            body,
            footerHint: "",
            onRefresh: Reload);

        Reload();
    }

    private static void AddRow(TableLayoutPanel stack, int row, string label, Control value)
    {
        var lbl = new Label
        {
            Text = label,
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = AppTheme.TextHeader,
        };
        stack.Controls.Add(lbl, 0, row);
        stack.Controls.Add(value, 1, row);
    }

    private static Label StyleValue(Label label)
    {
        label.AutoSize = false;
        label.Dock = DockStyle.Fill;
        label.TextAlign = ContentAlignment.MiddleLeft;
        label.ForeColor = AppTheme.TextMain;
        label.AutoEllipsis = true;
        return label;
    }

    private void Reload()
    {
        var snap = DefaultInstallPathHelper.Read();
        _current64.Text = snap.ProgramFiles;
        _current86.Text = snap.ProgramFilesX86;

        var drives = DefaultInstallPathHelper.ListCandidateDrives();
        _drive.Items.Clear();
        foreach (var d in drives)
        {
            string free;
            try { free = FormatBytes(d.AvailableFreeSpace); }
            catch { free = "—"; }
            _drive.Items.Add(new DriveItem(
                d.RootDirectory.FullName,
                $"{d.Name.TrimEnd('\\')}  ({free} {AppLang.L("可用", "free")})"));
        }

        _apply.Enabled = _drive.Items.Count > 0;
        if (_drive.Items.Count > 0)
            _drive.SelectedIndex = 0;
        else
        {
            _preview64.Text = AppLang.L("没有可用的非系统本地磁盘", "No non-system fixed drives available");
            _preview86.Text = "";
        }

        _restore.Enabled = !snap.IsAtSystemDefault;
        _status.Text = snap.IsAtSystemDefault
            ? AppLang.L("当前为系统盘默认位置。", "Currently using system-drive defaults.")
            : AppLang.L("当前已改到非系统盘（或自定义路径）。", "Currently using a custom / non-system path.");
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        if (_drive.SelectedItem is not DriveItem item)
            return;
        var next = DefaultInstallPathHelper.BuildForDrive(item.Root);
        _preview64.Text = next.ProgramFiles;
        _preview86.Text = next.ProgramFilesX86;
    }

    private void ApplySelected()
    {
        if (_drive.SelectedItem is not DriveItem item)
        {
            MessageBox.Show(
                this,
                AppLang.L("请先选择目标磁盘。", "Select a target drive first."),
                Text,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        var next = DefaultInstallPathHelper.BuildForDrive(item.Root);
        var confirm = MessageBox.Show(
            this,
            AppLang.L(
                "将默认安装路径改为：\r\n" + next.ProgramFiles + "\r\n" + next.ProgramFilesX86 +
                "\r\n\r\n已安装的软件不会移动；仅影响之后遵守该设置的安装程序。",
                "Change default install path to:\r\n" + next.ProgramFiles + "\r\n" + next.ProgramFilesX86 +
                "\r\n\r\nInstalled apps will not move; only future installers that honor this setting."),
            Text,
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Question);
        if (confirm != DialogResult.OK) return;

        try
        {
            UseWaitCursor = true;
            using (ApplyLog.PushContext(AppLang.L("默认安装路径", "Default install path")))
                DefaultInstallPathHelper.Apply(next, _createDirs.Checked);
            Reload();
            _status.Text = AppLang.L("已应用到所选盘。", "Applied to the selected drive.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _status.Text = AppLang.L("应用失败。", "Apply failed.");
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private void RestoreDefault()
    {
        var def = DefaultInstallPathHelper.BuildSystemDefault();
        var confirm = MessageBox.Show(
            this,
            AppLang.L(
                "恢复系统默认：\r\n" + def.ProgramFiles + "\r\n" + def.ProgramFilesX86,
                "Restore system defaults:\r\n" + def.ProgramFiles + "\r\n" + def.ProgramFilesX86),
            Text,
            MessageBoxButtons.OKCancel,
            MessageBoxIcon.Question);
        if (confirm != DialogResult.OK) return;

        try
        {
            UseWaitCursor = true;
            using (ApplyLog.PushContext(AppLang.L("默认安装路径", "Default install path")))
                DefaultInstallPathHelper.RestoreSystemDefault(_createDirs.Checked);
            Reload();
            _status.Text = AppLang.L("已恢复系统默认。", "System defaults restored.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _status.Text = AppLang.L("恢复失败。", "Restore failed.");
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private static string FormatBytes(long bytes)
    {
        const double kb = 1024, mb = kb * 1024, gb = mb * 1024, tb = gb * 1024;
        if (bytes >= tb) return (bytes / tb).ToString("0.#") + " TB";
        if (bytes >= gb) return (bytes / gb).ToString("0.#") + " GB";
        if (bytes >= mb) return (bytes / mb).ToString("0.#") + " MB";
        return bytes + " B";
    }

    private sealed class DriveItem
    {
        public string Root { get; }
        public string Display { get; }
        public DriveItem(string root, string display)
        {
            Root = root;
            Display = display;
        }

        public override string ToString() => Display;
    }
}
