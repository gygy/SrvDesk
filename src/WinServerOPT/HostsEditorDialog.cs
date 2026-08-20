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
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(760, 520);
        MinimumSize = new Size(640, 420);
        Font = new Font("Microsoft YaHei UI", 9F);
        BackColor = AppTheme.SurfaceCard;

        var tip = new Label
        {
            Text = "修改本机 DNS 覆盖（%SystemRoot%\\System32\\drivers\\etc\\hosts）。勾选「启用」生效；取消勾选会以 # 注释该行。",
            Location = new Point(16, 12),
            Size = new Size(728, 36),
            ForeColor = AppTheme.TextMute,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };

        _path.SetBounds(16, 48, 728, 20);
        _path.ForeColor = AppTheme.TextHeader;
        _path.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        _grid.Location = new Point(16, 74);
        _grid.Size = new Size(728, 340);
        _grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
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

        _grid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            Name = "Enabled",
            HeaderText = "启用",
            FillWeight = 12,
            Width = 56,
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Address",
            HeaderText = "IP 地址",
            FillWeight = 28,
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Hosts",
            HeaderText = "主机名（多个用空格分隔）",
            FillWeight = 40,
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Comment",
            HeaderText = "备注",
            FillWeight = 20,
        });

        _backup.Text = "保存前备份";
        _backup.Checked = true;
        _backup.AutoSize = true;
        _backup.Location = new Point(16, 428);
        _backup.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _backup.ForeColor = AppTheme.TextMain;

        _flush.Text = "保存后刷新 DNS 缓存";
        _flush.Checked = true;
        _flush.AutoSize = true;
        _flush.Location = new Point(130, 428);
        _flush.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        _flush.ForeColor = AppTheme.TextMain;

        var bar = new FlowLayoutPanel
        {
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Location = new Point(200, 464),
            BackColor = AppTheme.SurfaceCard,
        };

        bar.Controls.Add(ActionButton("添加", AddRow, false));
        bar.Controls.Add(ActionButton("删除", DeleteSelected, false));
        bar.Controls.Add(ActionButton("重新加载", Reload, false));
        bar.Controls.Add(ActionButton("打开备份目录", OpenBackupDir, false));
        bar.Controls.Add(ActionButton("保存", Save, true));

        var close = ActionButton("关闭", () => Close(), false);
        close.DialogResult = DialogResult.Cancel;
        bar.Controls.Add(close);
        CancelButton = close;

        Controls.AddRange([tip, _path, _grid, _backup, _flush, bar]);
        Load += (_, _) => Reload();
        Resize += (_, _) => bar.Location = new Point(Math.Max(16, ClientSize.Width - bar.Width - 16), ClientSize.Height - 48);
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

    private static Button ActionButton(string text, Action click, bool primary)
    {
        var b = new Button
        {
            Text = text,
            AutoSize = true,
            Height = 32,
            MinimumSize = new Size(72, 32),
            Padding = new Padding(8, 0, 8, 0),
            Margin = new Padding(6, 0, 0, 0),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            BackColor = primary ? AppTheme.Primary : AppTheme.SurfaceCard,
            ForeColor = primary ? AppTheme.TextOnPrimary : AppTheme.TextMain,
        };
        if (primary) b.FlatAppearance.BorderSize = 0;
        else b.FlatAppearance.BorderColor = AppTheme.Border;
        b.Click += (_, _) => click();
        return b;
    }
}
