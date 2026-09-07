namespace SrvDesk;

internal sealed class CleanupDialog : Form
{
    private readonly ListView _list = new();
    private readonly ProgressBar _bar = new();
    private readonly Label _status = new();
    private readonly Button _run;
    private readonly Button _cancelRun;
    private CancellationTokenSource? _cts;
    private bool _busy;

    public CleanupDialog()
    {
        Text = "垃圾清理";
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(720, 600);
        MinimumSize = new Size(640, 520);

        var body = ThemedSettingsChrome.CreateBodyPanel();
        body.AutoScroll = false;

        _list.View = View.Details;
        _list.FullRowSelect = true;
        _list.CheckBoxes = true;
        _list.ShowGroups = true;
        _list.HideSelection = false;
        _list.BorderStyle = BorderStyle.FixedSingle;
        _list.Dock = DockStyle.Fill;
        _list.BackColor = AppTheme.SurfaceCard;
        _list.HeaderStyle = ColumnHeaderStyle.Nonclickable;
        UiBuffer.Enable(_list);
        _list.Columns.Add("项目", 200);
        _list.Columns.Add("说明", 200);
        FillItems();

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = false,
            Padding = new Padding(0, 8, 0, 0),
        };
        UiBuffer.ConfigureNoScrollRow(actions);
        _run = ThemedSettingsChrome.CreateButton("开始清理", true);
        _run.Click += (_, _) => StartCleanup();
        _cancelRun = ThemedSettingsChrome.CreateButton("停止", false);
        _cancelRun.Enabled = false;
        _cancelRun.Margin = new Padding(8, 0, 0, 0);
        _cancelRun.Click += (_, _) => _cts?.Cancel();
        var allOn = ThemedSettingsChrome.CreateButton("全选", false);
        allOn.Margin = new Padding(16, 0, 0, 0);
        allOn.Click += (_, _) => SetAll(true);
        var allOff = ThemedSettingsChrome.CreateButton("全不选", false);
        allOff.Margin = new Padding(8, 0, 0, 0);
        allOff.Click += (_, _) => SetAll(false);
        var safe = ThemedSettingsChrome.CreateButton("仅安全项", false);
        safe.Margin = new Padding(8, 0, 0, 0);
        safe.Click += (_, _) => ResetDefaults();
        var repair = ThemedSettingsChrome.CreateButton("修复被锁组件", false);
        repair.Margin = new Padding(16, 0, 0, 0);
        repair.Click += (_, _) =>
        {
            CompetitorTweaks.RepairLockedComponents();
            MessageBox.Show(this, "已尝试恢复任务管理器、CMD、注册表编辑器、控制面板等。", "策略修复",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        };
        actions.Controls.AddRange([_run, _cancelRun, allOn, allOff, safe, repair]);

        var progress = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 52,
            Padding = new Padding(0, 8, 0, 0),
        };
        _bar.Dock = DockStyle.Top;
        _bar.Height = 16;
        _status.Dock = DockStyle.Fill;
        _status.ForeColor = AppTheme.TextMute;
        _status.TextAlign = ContentAlignment.MiddleLeft;
        _status.Text = "默认勾选安全项。Cookies、WinSxS、.NET 镜像、系统日志默认不勾。";
        progress.Controls.Add(_status);
        progress.Controls.Add(_bar);

        body.Controls.Add(_list);
        body.Controls.Add(progress);
        body.Controls.Add(actions);
        actions.BringToFront();
        progress.BringToFront();
        _list.SendToBack();

        ThemedSettingsChrome.MountModal(
            this,
            "垃圾清理",
            "缓存 · 系统残留 · 临时文件 · 对照 ZyperWin++ 项，占用中的文件会跳过",
            body,
            "清理不可恢复。WinSxS / .NET 镜像较重，请按需勾选。");
        UiBuffer.BindListViewColumnFit(_list, 1, 160);
        FormClosing += (_, e) =>
        {
            if (!_busy) return;
            e.Cancel = true;
            _cts?.Cancel();
        };
    }

    private void FillItems()
    {
        _list.BeginUpdate();
        _list.Items.Clear();
        _list.Groups.Clear();
        var groups = new Dictionary<string, ListViewGroup>(StringComparer.Ordinal);
        foreach (var name in CleanupEngine.Groups)
        {
            var g = new ListViewGroup(name, name);
            groups[name] = g;
            _list.Groups.Add(g);
        }

        foreach (var item in CleanupEngine.Items)
        {
            var row = new ListViewItem(item.Title) { Tag = item, Checked = item.DefaultOn };
            row.SubItems.Add(item.Hint);
            if (groups.TryGetValue(item.Group, out var g))
                row.Group = g;
            _list.Items.Add(row);
        }
        _list.EndUpdate();
    }

    private void SetAll(bool on)
    {
        foreach (ListViewItem row in _list.Items)
            row.Checked = on;
    }

    private void ResetDefaults()
    {
        foreach (ListViewItem row in _list.Items)
        {
            if (row.Tag is CleanupItem item)
                row.Checked = item.DefaultOn;
        }
    }

    private List<CleanupItem> Selected()
    {
        var list = new List<CleanupItem>();
        foreach (ListViewItem row in _list.Items)
        {
            if (row.Checked && row.Tag is CleanupItem item)
                list.Add(item);
        }
        return list;
    }

    private async void StartCleanup()
    {
        if (_busy) return;
        var selected = Selected();
        if (selected.Count == 0)
        {
            MessageBox.Show(this, "请先勾选要清理的项目。", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var heavy = selected.Where(x => x.Heavy).Select(x => x.Title).ToList();
        var preview = string.Join("\r\n", selected.Take(8).Select(x => "· " + x.Title));
        if (selected.Count > 8)
            preview += $"\r\n以及另外 {selected.Count - 8} 项";
        var msg = $"即将清理 {selected.Count} 项：\r\n\r\n{preview}";
        if (heavy.Count > 0)
            msg += "\r\n\r\n较重项：" + string.Join("、", heavy) + "（可能需数分钟）";
        msg += "\r\n\r\n确定清理？占用中的文件会跳过。";
        if (MessageBox.Show(this, msg, Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        _busy = true;
        _run.Enabled = false;
        _list.Enabled = false;
        _cancelRun.Enabled = true;
        _bar.Value = 0;
        _bar.Maximum = Math.Max(1, selected.Count);
        _cts = new CancellationTokenSource();
        var ids = selected.Select(x => x.Id).ToList();
        var lastTitle = "";
        var doneTitles = new HashSet<string>(StringComparer.Ordinal);

        try
        {
            var stat = await Task.Run(() =>
            {
                CleanupProgress? last = null;
                CleanupEngine.Run(ids, p =>
                {
                    last = p;
                    if (!IsHandleCreated) return;
                    BeginInvoke(new Action(() =>
                    {
                        if (p.Current != lastTitle)
                        {
                            if (lastTitle.Length > 0)
                                doneTitles.Add(lastTitle);
                            lastTitle = p.Current;
                            _bar.Value = Math.Min(_bar.Maximum, doneTitles.Count);
                        }
                        _status.Text = $"正在清理：{p.Current}  ·  已删 {p.Files} 个文件（{FormatSize(p.Bytes)}）";
                    }));
                }, _cts.Token);
                return last ?? new CleanupProgress();
            }, _cts.Token);

            _bar.Value = _bar.Maximum;
            var text = $"完成：删除 {stat.Files} 个文件（{FormatSize(stat.Bytes)}）";
            if (stat.Failed > 0)
                text += $"，跳过 {stat.Failed} 个占用中的文件";
            if (_cts.IsCancellationRequested)
                text = "已停止。" + text;
            _status.Text = text;
            ApplyLog.Write($"垃圾清理：{stat.Files} 个文件，{stat.Bytes / 1024} KB，跳过 {stat.Failed}");
            MessageBox.Show(this, text, Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (OperationCanceledException)
        {
            _status.Text = "已取消。";
        }
        catch (Exception ex)
        {
            _status.Text = ex.Message;
            MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            _busy = false;
            _run.Enabled = true;
            _list.Enabled = true;
            _cancelRun.Enabled = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private static string FormatSize(long bytes)
    {
        if (bytes < 1024) return bytes + " B";
        if (bytes < 1024 * 1024) return (bytes / 1024) + " KB";
        return $"{bytes / (1024.0 * 1024.0):0.0} MB";
    }
}
