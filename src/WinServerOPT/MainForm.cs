namespace WinOpt;

internal sealed class MainForm : Form
{
    private readonly CheckBox _cpu = NewCheck("CPU资源分配程序优先");
    private readonly CheckBox _dep = NewCheck("数据执行保护DEP（T）");
    private readonly CheckBox _uac = NewCheck("禁用用户账户控制UAC");
    private readonly CheckBox _ie = NewCheck("关闭IE增强安全配置");
    private readonly CheckBox _thisPc = NewCheck("桌面此电脑图标");
    private readonly CheckBox _taskbar = NewCheck("使用小按钮任务栏");
    private readonly CheckBox _confirmDel = NewCheck("显示删除确认对话框");
    private readonly CheckBox _audio = NewCheck("启动音频服务");
    private readonly CheckBox _svrMgr = NewCheck("登录不启动服务管理器");
    private readonly CheckBox _azure = NewCheck("禁止启动Azure Arc");
    private readonly CheckBox _pwd = NewCheck("禁用密码符合复杂性要求");
    private readonly CheckBox _shutdownLogon = NewCheck("允许未登录时关机");
    private readonly CheckBox _shutdownReason = NewCheck("关闭显示事件跟踪程序");
    private readonly CheckBox _noCad = NewCheck("无需Ctrl+Alt+Del登录");
    private readonly Label _status = new();
    private readonly Button _apply = new();

    private CheckBox[] AllChecks =>
    [
        _cpu, _dep, _uac, _ie, _thisPc, _taskbar, _confirmDel, _audio,
        _svrMgr, _azure, _pwd, _shutdownLogon, _shutdownReason, _noCad
    ];

    public MainForm()
    {
        Text = "Win一键优化";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(740, 470);
        Font = new Font("Microsoft YaHei UI", 9F);

        var hint = new Label
        {
            AutoSize = true,
            Location = new Point(16, 12),
            Text = "先勾选或取消配置项，再运行",
        };

        var perf = NewGroup("性能及安全", 16, 40, 348, 150, _cpu, _dep, _uac, _ie);
        var personal = NewGroup("个性化设置", 376, 40, 348, 150, _thisPc, _taskbar, _confirmDel, _audio);
        var startup = NewGroup("启动项", 16, 202, 348, 110, _svrMgr, _azure);
        var account = NewGroup("账户策略", 376, 202, 348, 150, _pwd, _shutdownLogon, _shutdownReason, _noCad);

        var selectAll = NewButton("全部选择", 16, 370, 100, () => SetAll(true));
        var selectNone = NewButton("全部取消", 124, 370, 100, () => SetAll(false));
        var about = NewButton("关于", 624, 370, 100, ShowAbout);

        _apply.Text = "一键优化";
        _apply.Location = new Point(376, 366);
        _apply.Size = new Size(140, 36);
        _apply.Click += (_, _) => Apply();

        _status.AutoSize = false;
        _status.Location = new Point(16, 418);
        _status.Size = new Size(708, 40);
        _status.Text = Optimizer.IsWindowsServer()
            ? "就绪。勾选表示采用优化项，取消勾选表示恢复该项。"
            : "当前系统可能不是 Windows Server。本工具面向 Server 日常使用优化。";

        Controls.AddRange([hint, perf, personal, startup, account, selectAll, selectNone, _apply, about, _status]);
        Load += (_, _) => LoadState();
    }

    private void LoadState()
    {
        try
        {
            Bind(Optimizer.Read());
        }
        catch (Exception ex)
        {
            _status.Text = "读取当前配置失败：" + ex.Message;
        }
    }

    private void Bind(Optimizer.State s)
    {
        _cpu.Checked = s.CpuProgramPriority;
        _dep.Checked = s.Dep;
        _uac.Checked = s.DisableUac;
        _ie.Checked = s.DisableIeEsc;
        _thisPc.Checked = s.ShowThisPcIcon;
        _taskbar.Checked = s.SmallTaskbar;
        _confirmDel.Checked = s.ConfirmDelete;
        _audio.Checked = s.EnableAudio;
        _svrMgr.Checked = s.SkipServerManager;
        _azure.Checked = s.DisableAzureArc;
        _pwd.Checked = s.DisablePasswordComplexity;
        _shutdownLogon.Checked = s.ShutdownWithoutLogon;
        _shutdownReason.Checked = s.DisableShutdownReason;
        _noCad.Checked = s.DisableCad;
    }

    private Optimizer.State CaptureState() => new()
    {
        CpuProgramPriority = _cpu.Checked,
        Dep = _dep.Checked,
        DisableUac = _uac.Checked,
        DisableIeEsc = _ie.Checked,
        ShowThisPcIcon = _thisPc.Checked,
        SmallTaskbar = _taskbar.Checked,
        ConfirmDelete = _confirmDel.Checked,
        EnableAudio = _audio.Checked,
        SkipServerManager = _svrMgr.Checked,
        DisableAzureArc = _azure.Checked,
        DisablePasswordComplexity = _pwd.Checked,
        ShutdownWithoutLogon = _shutdownLogon.Checked,
        DisableShutdownReason = _shutdownReason.Checked,
        DisableCad = _noCad.Checked,
    };

    private void SetAll(bool value)
    {
        foreach (var box in AllChecks) box.Checked = value;
    }

    private void Apply()
    {
        _apply.Enabled = false;
        UseWaitCursor = true;
        _status.Text = "正在应用…";
        Application.DoEvents();
        try
        {
            var errors = Optimizer.Apply(CaptureState());
            LoadState();
            if (errors.Count == 0)
                _status.Text = "已应用。部分项目（UAC、IE 增强安全等）可能需要注销或重启后生效。";
            else
                _status.Text = "部分失败：\r\n" + string.Join("\r\n", errors);
        }
        catch (Exception ex)
        {
            _status.Text = "应用失败：" + ex.Message;
        }
        finally
        {
            UseWaitCursor = false;
            _apply.Enabled = true;
        }
    }

    private static void ShowAbout()
    {
        MessageBox.Show(
            "Windows Server 日常使用优化工具。",
            "关于",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private static CheckBox NewCheck(string text) => new()
    {
        AutoSize = true,
        Text = text,
        UseVisualStyleBackColor = true,
    };

    private static Button NewButton(string text, int x, int y, int w, Action click)
    {
        var b = new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(w, 28),
            UseVisualStyleBackColor = true,
        };
        b.Click += (_, _) => click();
        return b;
    }

    private static GroupBox NewGroup(string title, int x, int y, int w, int h, params CheckBox[] boxes)
    {
        var g = new GroupBox
        {
            Text = title,
            Location = new Point(x, y),
            Size = new Size(w, h),
        };
        for (var i = 0; i < boxes.Length; i++)
        {
            boxes[i].Location = new Point(16, 28 + i * 28);
            g.Controls.Add(boxes[i]);
        }
        return g;
    }
}
