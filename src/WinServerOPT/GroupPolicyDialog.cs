namespace WinOpt;

internal sealed class GroupPolicyDialog : Form
{
    private readonly TextBox _output = new();
    private readonly Label _gpeditHint = new();

    public GroupPolicyDialog()
    {
        Text = "组策略";
        AppBrand.ApplyWindowIcon(this);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(640, 480);
        MinimumSize = new Size(520, 380);
        Font = new Font("Microsoft YaHei UI", 9F);
        BackColor = AppTheme.SurfaceCard;

        var tip = new Label
        {
            Text = "本地组策略 (GPO) 用于集中管理注册表与系统行为。修改策略后需「强制更新组策略」或重启/注销后才会生效。",
            Location = new Point(16, 12),
            Size = new Size(608, 36),
            ForeColor = AppTheme.TextMute,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };

        _gpeditHint.SetBounds(16, 50, 608, 20);
        _gpeditHint.ForeColor = AppTheme.TextHeader;
        _gpeditHint.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        _output.SetBounds(16, 78, 608, 340);
        _output.Multiline = true;
        _output.ReadOnly = true;
        _output.ScrollBars = ScrollBars.Vertical;
        _output.BorderStyle = BorderStyle.FixedSingle;
        _output.BackColor = AppTheme.Surface;
        _output.ForeColor = AppTheme.TextMain;
        _output.Font = new Font("Consolas", 9F);
        _output.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        _output.Text = "点击下方「强制更新组策略」执行 gpupdate /force，输出将显示在此处。";

        var bar = new FlowLayoutPanel
        {
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = AppTheme.SurfaceCard,
        };

        var close = ActionButton("关闭", () => Close(), primary: true);
        close.DialogResult = DialogResult.Cancel;
        CancelButton = close;

        bar.Controls.Add(close);
        bar.Controls.Add(ActionButton("强制更新组策略", RunGpUpdate, primary: false));
        bar.Controls.Add(ActionButton("打开组策略编辑器", OpenGpedit, primary: false));

        Controls.AddRange([tip, _gpeditHint, _output, bar]);
        Load += (_, _) => UpdateGpeditHint();
        Resize += (_, _) =>
        {
            bar.Location = new Point(Math.Max(16, ClientSize.Width - bar.Width - 16), ClientSize.Height - 48);
            _output.Height = Math.Max(120, ClientSize.Height - 130);
        };
        Shown += (_, _) => bar.Location = new Point(Math.Max(16, ClientSize.Width - bar.Width - 16), ClientSize.Height - 48);
    }

    private void UpdateGpeditHint()
    {
        _gpeditHint.Text = GroupPolicyHelper.IsGpeditAvailable()
            ? "本机已安装组策略编辑器 (gpedit.msc)。"
            : "本机未检测到 gpedit.msc（Server Core 等环境可能不可用），仍可使用强制更新。";
    }

    private void OpenGpedit()
    {
        try
        {
            GroupPolicyHelper.OpenEditor();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "组策略编辑器", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void RunGpUpdate()
    {
        UseWaitCursor = true;
        _output.Text = "正在执行 gpupdate /force …\r\n";
        Application.DoEvents();
        try
        {
            var result = GroupPolicyHelper.ForceUpdate();
            _output.Text = result.Output;
            if (result.RebootRequired)
            {
                _output.AppendText("\r\n\r\n提示：部分策略需注销或重启后生效。");
                MessageBox.Show(this,
                    "组策略已更新，但需要注销或重新启动才能完全生效。\r\n\r\n" + result.Output,
                    "强制更新组策略",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            else if (!result.Success)
            {
                MessageBox.Show(this, result.Output, "强制更新组策略",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            _output.Text = ex.Message;
            MessageBox.Show(this, ex.Message, "强制更新组策略", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            UseWaitCursor = false;
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
            Padding = new Padding(10, 0, 10, 0),
            Margin = new Padding(6, 0, 0, 0),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            BackColor = primary ? AppTheme.Primary : AppTheme.SurfaceCard,
            ForeColor = primary ? AppTheme.TextOnPrimary : AppTheme.TextMain,
        };
        if (primary) b.FlatAppearance.BorderSize = 0;
        else
        {
            b.FlatAppearance.BorderColor = AppTheme.Border;
            b.MouseEnter += (_, _) => b.BackColor = AppTheme.PrimaryPale;
            b.MouseLeave += (_, _) => b.BackColor = AppTheme.SurfaceCard;
        }
        b.Click += (_, _) => click();
        return b;
    }
}
