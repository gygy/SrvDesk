using System.Diagnostics;

namespace SrvDesk;

internal sealed class SystemInfoDialog : Form
{
    private readonly ListView _list = new();
    private readonly Label _summary = new();
    private List<SystemInfoRow> _rows = [];

    public SystemInfoDialog()
    {
        Text = "系统信息";
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = UiScale.Size(860, 640);
        MinimumSize = UiScale.Size(720, 520);

        var body = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(UiScale.S(12), UiScale.S(8), UiScale.S(12), UiScale.S(8)),
            BackColor = AppTheme.Surface,
        };

        _list.Dock = DockStyle.Fill;
        _list.View = View.Details;
        UiBuffer.Enable(_list);
        _list.FullRowSelect = true;
        _list.GridLines = true;
        _list.ShowGroups = true;
        _list.HeaderStyle = ColumnHeaderStyle.Nonclickable;
        _list.BackColor = AppTheme.SurfaceCard;
        _list.BorderStyle = BorderStyle.FixedSingle;
        _list.Columns.Add("项目", 180);
        _list.Columns.Add("值", 480);

        _summary.Dock = DockStyle.Top;
        _summary.Height = Math.Max(UiScale.S(28), UiFit.ControlHeight(UiFit.UiFontSmall));
        _summary.ForeColor = AppTheme.TextMute;
        _summary.Font = UiFit.UiFont;
        _summary.TextAlign = ContentAlignment.MiddleLeft;

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = UiFit.ControlHeight() + UiScale.S(16),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = false,
            Padding = new Padding(0, UiScale.S(8), 0, 0),
        };
        UiBuffer.ConfigureNoScrollRow(actions);
        var copy = ThemedSettingsChrome.CreateButton("复制全部", false);
        UiFit.FitButton(copy, padding: 28);
        copy.Click += (_, _) => CopyAll();
        var msinfo = ThemedSettingsChrome.CreateButton("msinfo32", false);
        UiFit.FitButton(msinfo, padding: 28);
        msinfo.Margin = new Padding(UiScale.S(8), 0, 0, 0);
        msinfo.Click += (_, _) => OpenMsinfo();
        actions.Controls.AddRange([copy, msinfo]);

        body.Controls.Add(_list);
        body.Controls.Add(actions);
        body.Controls.Add(_summary);

        ThemedSettingsChrome.MountModal(
            this,
            "系统信息",
            "",
            body,
            "",
            showHeader: false,
            onRefresh: LoadInfo);

        UiBuffer.BindListViewColumnFit(_list, 1, 200);
        body.Resize += (_, _) => UiBuffer.FitListViewColumn(_list, 1, 200);
        Load += (_, _) => LoadInfo();
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
            _summary.Text = $"共 {_rows.Count} 项，分 {groups.Count} 组。";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "读取系统信息失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            _summary.Text = "读取失败。";
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
