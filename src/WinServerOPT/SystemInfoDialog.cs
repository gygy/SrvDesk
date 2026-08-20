using System.Diagnostics;

namespace WinOpt;

internal sealed class SystemInfoDialog : Form
{
    private readonly ListView _list = new();
    private List<SystemInfoRow> _rows = [];

    public SystemInfoDialog()
    {
        Text = "系统信息";
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(640, 560);
        MinimumSize = new Size(520, 420);
        Font = new Font("Microsoft YaHei UI", 9F);
        BackColor = AppTheme.SurfaceCard;

        var tip = new Label
        {
            Text = "本机操作系统、硬件与网络摘要。可复制为文本，或打开系统自带「系统信息」(msinfo32)。",
            Location = new Point(16, 12),
            Size = new Size(608, 32),
            ForeColor = AppTheme.TextMute,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };

        _list.Location = new Point(16, 48);
        _list.Size = new Size(608, 452);
        _list.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _list.View = View.Details;
        _list.FullRowSelect = true;
        _list.GridLines = true;
        _list.ShowGroups = true;
        _list.HeaderStyle = ColumnHeaderStyle.Nonclickable;
        _list.Columns.Add("项目", 160);
        _list.Columns.Add("值", 420);

        var bar = new FlowLayoutPanel
        {
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = AppTheme.SurfaceCard,
        };
        bar.Controls.Add(ActionButton("刷新", LoadInfo, false));
        bar.Controls.Add(ActionButton("复制全部", CopyAll, false));
        bar.Controls.Add(ActionButton("打开 msinfo32", OpenMsinfo, false));
        var close = ActionButton("关闭", () => Close(), true);
        close.DialogResult = DialogResult.Cancel;
        bar.Controls.Add(close);
        CancelButton = close;

        Controls.AddRange([tip, _list, bar]);
        Load += (_, _) => LoadInfo();
        Resize += (_, _) =>
        {
            bar.Location = new Point(Math.Max(16, ClientSize.Width - bar.Width - 16), ClientSize.Height - 48);
            if (_list.Columns.Count >= 2)
                _list.Columns[1].Width = Math.Max(200, _list.ClientSize.Width - _list.Columns[0].Width - 24);
        };
        Shown += (_, _) => bar.Location = new Point(Math.Max(16, ClientSize.Width - bar.Width - 16), ClientSize.Height - 48);
    }

    private void LoadInfo()
    {
        _list.BeginUpdate();
        _list.Items.Clear();
        _list.Groups.Clear();
        try
        {
            Cursor = Cursors.WaitCursor;
            _rows = SystemInfoSnapshot.Collect();
            var groups = new Dictionary<string, ListViewGroup>(StringComparer.Ordinal);
            foreach (var r in _rows)
            {
                if (!groups.TryGetValue(r.Group, out var grp))
                {
                    grp = new ListViewGroup(r.Group, r.Group);
                    groups[r.Group] = grp;
                    _list.Groups.Add(grp);
                }

                var item = new ListViewItem(r.Name) { Group = grp };
                item.SubItems.Add(r.Value);
                _list.Items.Add(item);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "读取系统信息失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Cursor = Cursors.Default;
            _list.EndUpdate();
        }
    }

    private void CopyAll()
    {
        var text = SystemInfoSnapshot.ToText(_rows);
        Clipboard.SetText(text);
        MessageBox.Show(this, "已复制到剪贴板。", "系统信息", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void OpenMsinfo()
    {
        try
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "msinfo32.exe");
            Process.Start(new ProcessStartInfo
            {
                FileName = File.Exists(path) ? path : "msinfo32.exe",
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "无法打开 msinfo32。\r\n\r\n" + ex.Message,
                "系统信息", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
