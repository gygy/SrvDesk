using System.Diagnostics;

namespace SrvDesk;

internal sealed class AppUpdateDialog : Form
{
    private readonly Label _current = new();
    private readonly Label _latest = new();
    private readonly TextBox _notes = new();
    private readonly ProgressBar _bar = new();
    private readonly Label _status = new();
    private readonly Button _check;
    private readonly Button _apply;
    private AppReleaseInfo? _release;
    private CancellationTokenSource? _cts;
    private bool _busy;

    public AppUpdateDialog(AppReleaseInfo? known = null)
    {
        Text = "检查更新";
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(520, 420);

        var body = ThemedSettingsChrome.CreateBodyPanel();
        body.AutoScroll = false;
        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            Padding = new Padding(4),
        };
        stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        stack.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _current.AutoSize = true;
        _current.ForeColor = AppTheme.TextMain;
        _current.Margin = new Padding(0, 0, 0, 6);
        _current.Text = "当前版本  v" + AppBrand.VersionText;

        _latest.AutoSize = true;
        _latest.ForeColor = AppTheme.TextMute;
        _latest.Margin = new Padding(0, 0, 0, 8);
        _latest.Text = "尚未检查。";

        _notes.Multiline = true;
        _notes.ReadOnly = true;
        _notes.ScrollBars = ScrollBars.Vertical;
        _notes.Dock = DockStyle.Fill;
        _notes.BorderStyle = BorderStyle.FixedSingle;
        _notes.BackColor = AppTheme.SurfaceCard;
        _notes.ForeColor = AppTheme.TextMain;
        _notes.Text = "点击「检查更新」查询 GitHub Releases。";

        _bar.Dock = DockStyle.Top;
        _bar.Height = 16;
        _bar.Margin = new Padding(0, 8, 0, 4);

        _status.AutoSize = true;
        _status.ForeColor = AppTheme.TextMute;
        _status.Margin = new Padding(0, 0, 0, 8);
        _status.Text = AppUpdate.ReleasesPage;

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoScroll = false,
            Margin = new Padding(0),
        };
        UiBuffer.ConfigureNoScrollRow(buttons);
        _check = ThemedSettingsChrome.CreateButton("检查更新", true);
        _check.Click += async (_, _) => await CheckAsync(autoApply: false);
        _apply = ThemedSettingsChrome.CreateButton("下载并更新", false);
        _apply.Enabled = false;
        _apply.Margin = new Padding(8, 0, 0, 0);
        _apply.Click += async (_, _) => await DownloadAndApplyAsync(confirm: true);
        var open = ThemedSettingsChrome.CreateButton("打开发布页", false);
        open.Margin = new Padding(8, 0, 0, 0);
        open.Click += (_, _) =>
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = _release?.HtmlUrl ?? AppUpdate.ReleasesPage,
                    UseShellExecute = true,
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };
        buttons.Controls.AddRange([_check, _apply, open]);

        stack.Controls.Add(_current, 0, 0);
        stack.Controls.Add(_latest, 0, 1);
        stack.Controls.Add(_notes, 0, 2);
        stack.Controls.Add(_bar, 0, 3);
        stack.Controls.Add(_status, 0, 4);
        stack.Controls.Add(buttons, 0, 5);
        body.Controls.Add(stack);

        ThemedSettingsChrome.MountModal(
            this,
            "检查更新",
            "从 GitHub Releases 获取最新 SrvDesk.exe，下载后自动替换并重启",
            body,
            "更新需要能访问 GitHub。替换时会退出当前进程。");
        if (known is not null)
            Bind(known);
        FormClosing += (_, e) =>
        {
            if (!_busy) return;
            e.Cancel = true;
            _cts?.Cancel();
        };
    }

    public Task StartDownloadAsync(bool confirm) => DownloadAndApplyAsync(confirm);

    public async Task CheckAsync(bool autoApply)
    {
        if (_busy) return;
        SetBusy(true, "正在检查…");
        try
        {
            var info = await Task.Run(AppUpdate.CheckLatest);
            _release = info;
            Bind(info);
            if (!info.IsNewer)
            {
                _status.Text = "已是最新版本。";
                return;
            }

            if (autoApply)
                await DownloadAndApplyAsync(confirm: false);
        }
        catch (Exception ex)
        {
            _status.Text = ex.Message;
            MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            SetBusy(false, _status.Text);
        }
    }

    private void Bind(AppReleaseInfo info)
    {
        _latest.Text = info.IsNewer
            ? $"发现新版本  v{info.Version}（{info.Tag}）"
            : $"最新版本  v{info.Version}（已是最新）";
        _latest.ForeColor = info.IsNewer ? AppTheme.PrimaryDeep : AppTheme.TextMute;
        _notes.Text = string.IsNullOrWhiteSpace(info.Notes) ? "（无发布说明）" : info.Notes;
        _apply.Enabled = info.IsNewer && !string.IsNullOrWhiteSpace(info.DownloadUrl);
        _status.Text = info.IsNewer
            ? (info.Size > 0 ? $"附件 {info.AssetName}（{info.Size / 1024} KB）" : info.AssetName)
            : "无需更新。";
    }

    private async Task DownloadAndApplyAsync(bool confirm)
    {
        if (_release is null || !_release.IsNewer) return;
        if (string.IsNullOrWhiteSpace(_release.DownloadUrl))
        {
            MessageBox.Show(this, "该版本没有可下载的 exe。", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (confirm && MessageBox.Show(this,
                $"下载 v{_release.Version} 并替换当前程序？\r\n完成后会自动重启。",
                Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        SetBusy(true, "准备下载…");
        _cts = new CancellationTokenSource();
        try
        {
            var dest = await Task.Run(() => AppUpdate.DownloadAsset(_release, (pct, msg) =>
            {
                if (!IsHandleCreated) return;
                BeginInvoke(new Action(() =>
                {
                    _bar.Value = Math.Max(0, Math.Min(100, pct));
                    _status.Text = msg;
                }));
            }, _cts.Token));
            _status.Text = "正在替换并重启…";
            AppUpdate.ReplaceAndRestart(dest);
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
            _cts?.Dispose();
            _cts = null;
            SetBusy(false, _status.Text);
        }
    }

    private void SetBusy(bool busy, string status)
    {
        _busy = busy;
        _check.Enabled = !busy;
        _apply.Enabled = !busy && _release is { IsNewer: true } && !string.IsNullOrWhiteSpace(_release.DownloadUrl);
        _status.Text = status;
        UseWaitCursor = busy;
    }
}
