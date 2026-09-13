namespace SrvDesk;

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
        ClientSize = UiScale.Size(780, 580);
        MinimumSize = UiScale.Size(660, 480);

        var body = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(UiScale.S(12), UiScale.S(8), UiScale.S(12), UiScale.S(8)),
            BackColor = AppTheme.Surface,
        };

        _gpeditHint.Dock = DockStyle.Top;
        _gpeditHint.Height = Math.Max(UiScale.S(28), UiFit.ControlHeight(UiFit.UiFontSmall));
        _gpeditHint.ForeColor = AppTheme.TextHeader;
        _gpeditHint.Font = UiFit.UiFont;
        _gpeditHint.TextAlign = ContentAlignment.MiddleLeft;

        _output.Dock = DockStyle.Fill;
        _output.Multiline = true;
        _output.ReadOnly = true;
        _output.ScrollBars = ScrollBars.Vertical;
        _output.BorderStyle = BorderStyle.FixedSingle;
        _output.BackColor = AppTheme.SurfaceCard;
        _output.ForeColor = AppTheme.TextMain;
        _output.Font = new Font("Consolas", 9F);
        _output.Text = "点击下方「强制更新组策略」执行 gpupdate /force，输出将显示在此处。";

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
        var gpupdate = ThemedSettingsChrome.CreateButton("强制更新组策略", true);
        UiFit.FitButton(gpupdate, padding: 28);
        gpupdate.Click += (_, _) => RunGpUpdate();
        var gpedit = ThemedSettingsChrome.CreateButton("打开组策略编辑器", false);
        UiFit.FitButton(gpedit, padding: 28);
        gpedit.Margin = new Padding(UiScale.S(8), 0, 0, 0);
        gpedit.Click += (_, _) => OpenGpedit();
        actions.Controls.AddRange([gpupdate, gpedit]);

        body.Controls.Add(_output);
        body.Controls.Add(actions);
        body.Controls.Add(_gpeditHint);

        ThemedSettingsChrome.MountModal(
            this,
            "组策略",
            "",
            body,
            "",
            showHeader: false);

        Load += (_, _) => UpdateGpeditHint();
    }

    private void UpdateGpeditHint()
    {
        _gpeditHint.Text = GroupPolicyHelper.IsGpeditAvailable()
            ? "本机已安装组策略编辑器 (gpedit.msc)。"
            : "本机未检测到 gpedit.msc（Server Core 等环境可能不可用），仍可使用强制更新。";
    }

    private void OpenGpedit()
    {
        try { GroupPolicyHelper.OpenEditor(); }
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
}
