using System.Diagnostics;

namespace WinOpt;

internal sealed class SystemInfoDialog : Form
{
    private readonly ListView _list = new();
    private List<SystemInfoRow> _rows = [];

    public SystemInfoDialog()
    {
        Text = "系统信息";
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(640, 560);
        MinimumSize = new Size(520, 420);

        var body = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 8, 12, 48), BackColor = AppTheme.Surface };

        _list.Dock = DockStyle.Fill;
        _list.View = View.Details;
        _list.FullRowSelect = true;
        _list.GridLines = true;
        _list.ShowGroups = true;
        _list.HeaderStyle = ColumnHeaderStyle.Nonclickable;
        _list.BackColor = AppTheme.SurfaceCard;
        _list.BorderStyle = BorderStyle.FixedSingle;
        _list.Columns.Add("项目", 160);
        _list.Columns.Add("值", 420);

        var refresh = ThemedSettingsChrome.CreateButton("刷新", false);
        refresh.Size = new Size(80, 34);
        refresh.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        refresh.Click += (_, _) => LoadInfo();

        var copy = ThemedSettingsChrome.CreateButton("复制全部", false);
        copy.Size = new Size(96, 34);
        copy.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        copy.Click += (_, _) => CopyAll();

        var msinfo = ThemedSettingsChrome.CreateButton("msinfo32", false);
        msinfo.Size = new Size(96, 34);
        msinfo.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        msinfo.Click += (_, _) => OpenMsinfo();

        body.Controls.Add(_list);
        body.Controls.AddRange([refresh, copy, msinfo]);

        ThemedSettingsChrome.MountModal(
            this,
            "系统信息",
            "操作系统 · 硬件 · 网络摘要",
            body,
            "可复制为文本，或打开系统自带 msinfo32。",
            LoadInfo);

        void LayoutButtons()
        {
            msinfo.Location = new Point(body.ClientSize.Width - msinfo.Width, body.ClientSize.Height - msinfo.Height);
            copy.Location = new Point(msinfo.Left - copy.Width - 8, msinfo.Top);
            refresh.Location = new Point(copy.Left - refresh.Width - 8, msinfo.Top);
            if (_list.Columns.Count >= 2)
                _list.Columns[1].Width = Math.Max(200, _list.ClientSize.Width - _list.Columns[0].Width - 24);
        }

        body.Resize += (_, _) => LayoutButtons();
        Load += (_, _) => LoadInfo();
        Shown += (_, _) => LayoutButtons();
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
        Clipboard.SetText(SystemInfoSnapshot.ToText(_rows));
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
}
