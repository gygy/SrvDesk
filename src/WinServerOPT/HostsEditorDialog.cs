using System.Diagnostics;

namespace WinOpt;

internal sealed class HostsEditorDialog : Form
{
    private readonly DataGridView _grid = new();
    private readonly Label _path = new();
    private readonly CheckBox _backup = new();
    private readonly CheckBox _flush = new();
    private HostsDocument _doc = new();

    public HostsEditorDialog()
    {
        Text = "编辑 hosts";
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        KeyPreview = true;
        ClientSize = new Size(780, 560);
        MinimumSize = new Size(640, 420);

        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 8, 12, 48), BackColor = AppTheme.Surface };

        _path.Dock = DockStyle.Top;
        _path.Height = 22;
        _path.ForeColor = AppTheme.TextHeader;

        _grid.Dock = DockStyle.Fill;
        _grid.AllowUserToAddRows = true;
        _grid.AllowUserToDeleteRows = true;
        _grid.BackgroundColor = AppTheme.SurfaceCard;
        _grid.BorderStyle = BorderStyle.FixedSingle;
        _grid.RowHeadersVisible = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.ColumnHeadersHeight = 32;
        _grid.RowTemplate.Height = 28;
        _grid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Enabled", HeaderText = "启用", FillWeight = 12 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Address", HeaderText = "IP 地址", FillWeight = 28 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Hosts", HeaderText = "主机名（多个用空格分隔）", FillWeight = 40 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Comment", HeaderText = "备注", FillWeight = 20 });

        var options = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 32,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
        };
        _backup.Text = "保存前备份";
        _backup.Checked = true;
        _backup.AutoSize = true;
        _backup.ForeColor = AppTheme.TextMain;
        _flush.Text = "保存后刷新 DNS 缓存";
        _flush.Checked = true;
        _flush.AutoSize = true;
        _flush.Margin = new Padding(16, 0, 0, 0);
        _flush.ForeColor = AppTheme.TextMain;
        options.Controls.Add(_backup);
        options.Controls.Add(_flush);

        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 40,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 4, 0, 0),
        };
        bar.Controls.Add(MkBtn("添加", AddRow, false));
        bar.Controls.Add(MkBtn("粘贴", PasteFromClipboard, false));
        bar.Controls.Add(MkBtn("删除", DeleteSelected, false));
        bar.Controls.Add(MkBtn("重新加载", Reload, false));
        bar.Controls.Add(MkBtn("备份目录", OpenBackupDir, false));
        var save = MkBtn("保存", Save, true);
        bar.Controls.Add(save);

        body.Controls.Add(_grid);
        body.Controls.Add(bar);
        body.Controls.Add(options);
        body.Controls.Add(_path);

        ThemedSettingsChrome.MountModal(
            this,
            "编辑 hosts",
            "本机 DNS 覆盖 · 支持 Ctrl+V 批量粘贴",
            body,
            "取消勾选「启用」将以 # 注释该行。",
            Reload);

        Load += (_, _) => Reload();
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.V) && !_grid.IsCurrentCellInEditMode)
        {
            try
            {
                if (Clipboard.ContainsText())
                {
                    var text = Clipboard.GetText();
                    if (text.IndexOf('\n') >= 0 || text.IndexOf('\r') >= 0)
                    {
                        PasteFromClipboard();
                        return true;
                    }
                }
            }
            catch { /* 使用默认粘贴 */ }
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void PasteFromClipboard()
    {
        try
        {
            if (!Clipboard.ContainsText()) return;
            var text = Clipboard.GetText();
            if (string.IsNullOrWhiteSpace(text)) return;

            if (!text.Contains("\n") && !text.Contains("\r") && _grid.IsCurrentCellInEditMode)
                return;

            var parsed = HostsFileHelper.ParseText(text);
            if (parsed.Entries.Count == 0)
            {
                MessageBox.Show(this,
                    "剪贴板中未识别到有效的 hosts 映射行。\r\n\r\n" +
                    "支持格式示例：\r\n127.0.0.1 example.com\r\n# 192.168.1.1 test.local",
                    "粘贴 hosts", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var insertAt = _grid.Rows.Count - 1;
            if (_grid.CurrentRow is not null && !_grid.CurrentRow.IsNewRow)
                insertAt = _grid.CurrentRow.Index;

            foreach (var entry in parsed.Entries)
                _grid.Rows.Insert(insertAt++, entry.Enabled, entry.Address, entry.Hosts, entry.Comment);

            var msg = $"已粘贴 {parsed.Entries.Count} 条映射。";
            if (parsed.SkippedLines > 0)
                msg += $"\r\n已跳过 {parsed.SkippedLines} 行（空行或非映射注释）。";
            MessageBox.Show(this, msg, "粘贴 hosts", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "粘贴失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void Reload()
    {
        try
        {
            _doc = HostsFileHelper.Read();
            _path.Text = "文件：" + HostsFileHelper.FilePath + $"（{_doc.Entries.Count} 条）";
            _grid.Rows.Clear();
            foreach (var e in _doc.Entries)
                _grid.Rows.Add(e.Enabled, e.Address, e.Hosts, e.Comment);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "读取 hosts 失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void AddRow()
    {
        var i = _grid.Rows.Add(true, "127.0.0.1", "", "");
        _grid.CurrentCell = _grid.Rows[i].Cells["Address"];
        _grid.BeginEdit(true);
    }

    private void DeleteSelected()
    {
        if (_grid.CurrentRow is null || _grid.CurrentRow.IsNewRow) return;
        _grid.Rows.Remove(_grid.CurrentRow);
    }

    private void Save()
    {
        _grid.EndEdit();
        var entries = new List<HostsEntry>();
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.IsNewRow) continue;
            var entry = new HostsEntry
            {
                Enabled = row.Cells["Enabled"].Value is true,
                Address = Convert.ToString(row.Cells["Address"].Value) ?? "",
                Hosts = Convert.ToString(row.Cells["Hosts"].Value) ?? "",
                Comment = Convert.ToString(row.Cells["Comment"].Value) ?? "",
            };
            if (string.IsNullOrWhiteSpace(entry.Address) && string.IsNullOrWhiteSpace(entry.Hosts))
                continue;
            var err = HostsFileHelper.Validate(entry);
            if (err.Length > 0)
            {
                MessageBox.Show(this, $"第 {row.Index + 1} 行：{err}", "无法保存", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _grid.CurrentCell = row.Cells["Address"];
                return;
            }
            entries.Add(entry);
        }

        try
        {
            _doc.Entries.Clear();
            _doc.Entries.AddRange(entries);
            HostsFileHelper.Save(_doc, _backup.Checked, _flush.Checked);
            MessageBox.Show(this, "hosts 已保存。" + (_flush.Checked ? "\r\n已刷新 DNS 缓存。" : ""),
                "保存成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Reload();
        }
        catch (UnauthorizedAccessException)
        {
            MessageBox.Show(this, "无法写入 hosts。请以管理员身份运行本工具。", "权限不足",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "保存失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OpenBackupDir()
    {
        try
        {
            Directory.CreateDirectory(HostsFileHelper.BackupDir);
            Process.Start(new ProcessStartInfo
            {
                FileName = HostsFileHelper.BackupDir,
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "无法打开备份目录", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static Button MkBtn(string text, Action click, bool primary)
    {
        var b = ThemedSettingsChrome.CreateButton(text, primary);
        b.AutoSize = true;
        b.Height = 32;
        b.Margin = new Padding(0, 0, 8, 0);
        b.Click += (_, _) => click();
        return b;
    }
}
