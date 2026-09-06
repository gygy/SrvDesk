using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace WinOpt;

/// <summary>配置脚本编辑器：.reg / .cmd / .ps1 语法高亮，可读可编辑。</summary>
internal sealed class ScriptSyntaxEditor : RichTextBox
{
    private SettingActionKind _kind = SettingActionKind.Mixed;
    private bool _suppress;
    private readonly System.Windows.Forms.Timer _debounce = new() { Interval = 180 };

    // 浅色主题：对比清晰、不刺眼
    private static readonly Color ColDefault = Color.FromArgb(36, 41, 47);
    private static readonly Color ColComment = Color.FromArgb(106, 153, 85);
    private static readonly Color ColKeyword = Color.FromArgb(0, 92, 197);
    private static readonly Color ColKeyPath = Color.FromArgb(111, 66, 193);
    private static readonly Color ColString = Color.FromArgb(163, 21, 21);
    private static readonly Color ColNumber = Color.FromArgb(9, 134, 88);
    private static readonly Color ColType = Color.FromArgb(0, 128, 128);
    private static readonly Color ColCommand = Color.FromArgb(0, 55, 130);
    private static readonly Color ColVariable = Color.FromArgb(180, 80, 0);
    private static readonly Color ColHeader = Color.FromArgb(55, 65, 81);
    private static readonly Color ColSection = Color.FromArgb(120, 80, 40);

    public ScriptSyntaxEditor()
    {
        DetectUrls = false;
        Multiline = true;
        WordWrap = false;
        AcceptsTab = true;
        BorderStyle = BorderStyle.FixedSingle;
        Font = new Font("Consolas", 9F);
        BackColor = Color.FromArgb(252, 253, 255);
        ForeColor = ColDefault;
        ScrollBars = RichTextBoxScrollBars.Both;
        HideSelection = false;
        _debounce.Tick += (_, _) =>
        {
            _debounce.Stop();
            HighlightAll();
        };
        TextChanged += OnTextChangedHighlight;
    }

    /// <summary>用户编辑（非程序 SetScript）后触发，用于自动持久化。</summary>
    public event EventHandler? UserScriptChanged;

    public bool IsUpdating => _suppress;

    public SettingActionKind Kind
    {
        get => _kind;
        set
        {
            if (_kind == value) return;
            _kind = value;
            HighlightAll();
        }
    }

    /// <summary>设置脚本内容并按类型高亮（会重置光标到开头）。</summary>
    public void SetScript(string text, SettingActionKind kind)
    {
        _kind = kind;
        _suppress = true;
        try
        {
            SuspendDrawing();
            Text = text ?? "";
            HighlightAllCore();
            Select(0, 0);
        }
        finally
        {
            ResumeDrawing();
            _suppress = false;
        }
    }

    public string PlainText => Text;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _debounce.Dispose();
        base.Dispose(disposing);
    }

    private void OnTextChangedHighlight(object? sender, EventArgs e)
    {
        if (_suppress) return;
        _debounce.Stop();
        _debounce.Start();
        UserScriptChanged?.Invoke(this, EventArgs.Empty);
    }

    private void HighlightAll()
    {
        if (IsDisposed || !IsHandleCreated) return;
        _suppress = true;
        try
        {
            var selStart = SelectionStart;
            var selLen = SelectionLength;
            var scroll = GetScrollPos();
            SuspendDrawing();
            HighlightAllCore();
            Select(Math.Min(selStart, TextLength), Math.Min(selLen, Math.Max(0, TextLength - selStart)));
            SetScrollPos(scroll);
        }
        finally
        {
            ResumeDrawing();
            _suppress = false;
        }
    }

    private void HighlightAllCore()
    {
        var text = Text;
        if (text.Length == 0) return;

        Select(0, text.Length);
        SelectionColor = ColDefault;
        SelectionFont = Font;

        var kind = _kind == SettingActionKind.Mixed ? DetectKind(text) : _kind;
        switch (kind)
        {
            case SettingActionKind.Reg:
                HighlightReg(text);
                break;
            case SettingActionKind.Cmd:
                HighlightCmd(text);
                break;
            case SettingActionKind.PowerShell:
                HighlightPowerShell(text);
                break;
            default:
                HighlightMixed(text);
                break;
        }
    }

    private static SettingActionKind DetectKind(string text)
    {
        var t = text.TrimStart();
        if (t.StartsWith("Windows Registry Editor", StringComparison.OrdinalIgnoreCase)
            || t.StartsWith("REGEDIT", StringComparison.OrdinalIgnoreCase)
            || t.IndexOf("[HKEY_", StringComparison.OrdinalIgnoreCase) >= 0)
            return SettingActionKind.Reg;
        if (t.StartsWith("#Requires", StringComparison.OrdinalIgnoreCase)
            || Regex.IsMatch(t, @"^\s*(function|param|Disable-|Enable-|Write-Host)\b", RegexOptions.IgnoreCase | RegexOptions.Multiline))
            return SettingActionKind.PowerShell;
        if (t.StartsWith("@echo", StringComparison.OrdinalIgnoreCase)
            || Regex.IsMatch(t, @"^\s*(sc|netsh|powercfg|dism|reg|net|bcdedit|fsutil)\b", RegexOptions.IgnoreCase | RegexOptions.Multiline))
            return SettingActionKind.Cmd;
        return SettingActionKind.Mixed;
    }

    private void HighlightMixed(string text)
    {
        // 分段：.reg 块 / 命令行 / 说明分隔
        ApplyRegex(text, @"(?m)^---.*?$", ColSection, bold: true);
        ApplyRegex(text, @"(?m)^;.*$", ColComment);
        ApplyRegex(text, @"(?m)^#.*$", ColComment);
        ApplyRegex(text, @"(?m)^rem\b.*$", ColComment, ignoreCase: true);
        ApplyRegex(text, @"\[HKEY_[^\]]+\]", ColKeyPath, bold: true);
        ApplyRegex(text, @"\bdword:[0-9a-fA-F]+\b", ColNumber);
        ApplyRegex(text, @"\bhex:[0-9a-fA-F,]+\b", ColNumber);
        ApplyRegex(text, @"""(?:\\.|[^""\\])*""", ColString);
        ApplyRegex(text, @"(?m)^\s*(sc|netsh|powercfg|dism|reg|net|bcdedit|fsutil|schtasks)\b", ColCommand, ignoreCase: true, bold: true);
        ApplyRegex(text, @"\$(?:\w|:)+", ColVariable);
        HighlightReg(text, skipBase: true);
    }

    private void HighlightReg(string text, bool skipBase = false)
    {
        if (!skipBase)
        {
            ApplyRegex(text, @"(?m)^;.*$", ColComment);
            ApplyRegex(text, @"(?i)Windows Registry Editor Version [\d.]+", ColHeader, bold: true);
            ApplyRegex(text, @"(?i)^REGEDIT4\s*$", ColHeader, bold: true);
        }

        ApplyRegex(text, @"\[-?HKEY_[^\]]+\]", ColKeyPath, bold: true);
        // 值名 "Name"=
        ApplyRegex(text, @"""(?:\\.|[^""\\])*""(?=\s*=)", ColKeyword);
        // 类型
        ApplyRegex(text, @"=(dword|hex(?:\([0-7]\))?|hex):", ColType, ignoreCase: true);
        // dword 数值
        ApplyRegex(text, @"dword:([0-9a-fA-F]+)", ColNumber, ignoreCase: true);
        // hex 字节
        ApplyRegex(text, @"hex(?:\([0-7]\))?:([0-9a-fA-F,\\\s]+)", ColNumber, ignoreCase: true);
        // 字符串值 ="..."
        ApplyRegex(text, @"=\s*""(?:\\.|[^""\\])*""", ColString);
        // 删除值 =-
        ApplyRegex(text, @"=-\s*$", ColString, multiline: true);
        // 默认值 @=
        ApplyRegex(text, @"(?m)^@=", ColKeyword, bold: true);
    }

    private void HighlightCmd(string text)
    {
        ApplyRegex(text, @"(?im)^\s*(rem\b.*|::.*)$", ColComment);
        ApplyRegex(text, @"(?im)^\s*@?echo\b.*$", ColComment);
        ApplyRegex(text, @"(?i)\b(if|else|for|goto|call|set|exit|pause|exist|equ|neq|lss|leq|gtr|geq|not|defined)\b", ColKeyword, bold: true);
        ApplyRegex(text, @"(?im)^\s*(sc|netsh|powercfg|dism|reg|net|bcdedit|fsutil|schtasks|powershell|wmic)\b", ColCommand, bold: true);
        ApplyRegex(text, @"%[^%\r\n]+%", ColVariable);
        ApplyRegex(text, @"""[^""\r\n]*""", ColString);
        ApplyRegex(text, @"\b0x[0-9a-fA-F]+\b|\b\d+\b", ColNumber);
    }

    private void HighlightPowerShell(string text)
    {
        ApplyRegex(text, @"(?m)#.*$", ColComment);
        ApplyRegex(text, @"<#[\s\S]*?#>", ColComment);
        ApplyRegex(text, @"(?i)\b(function|param|begin|process|end|if|elseif|else|foreach|for|while|switch|return|break|continue|try|catch|finally|throw|filter|class|enum)\b", ColKeyword, bold: true);
        ApplyRegex(text, @"(?i)#Requires\b.*$", ColHeader, bold: true);
        ApplyRegex(text, @"\$(?:\w+|\{[^}]+\})", ColVariable);
        ApplyRegex(text, @"(?i)\b(Disable|Enable|Set|Get|New|Remove|Write|Start|Stop|Import|Export)-\w+\b", ColCommand, bold: true);
        ApplyRegex(text, @"'(?:''|[^'])*'", ColString);
        ApplyRegex(text, @"""(?:\\.|`""|[^""])*""", ColString);
        ApplyRegex(text, @"\b0x[0-9a-fA-F]+\b|\b\d+\.?\d*\b", ColNumber);
    }

    private void ApplyRegex(
        string text,
        string pattern,
        Color color,
        bool ignoreCase = false,
        bool bold = false,
        bool multiline = false)
    {
        var options = RegexOptions.CultureInvariant;
        if (ignoreCase) options |= RegexOptions.IgnoreCase;
        if (multiline) options |= RegexOptions.Multiline;

        MatchCollection matches;
        try
        {
            matches = Regex.Matches(text, pattern, options);
        }
        catch
        {
            return;
        }

        foreach (Match m in matches)
        {
            if (!m.Success || m.Length == 0) continue;
            // 跳过超出范围（RichTextBox 用 \n 时索引仍与 Text 一致）
            if (m.Index < 0 || m.Index + m.Length > text.Length) continue;
            try
            {
                Select(m.Index, m.Length);
                SelectionColor = color;
                if (bold)
                    SelectionFont = new Font(Font, FontStyle.Bold);
                else
                    SelectionFont = Font;
            }
            catch
            {
                /* 忽略偶发选区异常 */
            }
        }
    }

    #region Native redraw / scroll helpers

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern int GetScrollPos(IntPtr hWnd, int nBar);

    [DllImport("user32.dll")]
    private static extern int SetScrollPos(IntPtr hWnd, int nBar, int nPos, bool bRedraw);

    private const int WM_SETREDRAW = 0x000B;
    private const int SB_VERT = 1;
    private const int WM_VSCROLL = 0x0115;
    private const int SB_THUMBPOSITION = 4;

    private void SuspendDrawing() => SendMessage(Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);

    private void ResumeDrawing()
    {
        SendMessage(Handle, WM_SETREDRAW, new IntPtr(1), IntPtr.Zero);
        Invalidate(true);
        Update();
    }

    private int GetScrollPos()
    {
        try { return GetScrollPos(Handle, SB_VERT); }
        catch { return 0; }
    }

    private void SetScrollPos(int pos)
    {
        try
        {
            SetScrollPos(Handle, SB_VERT, pos, true);
            SendMessage(Handle, WM_VSCROLL, new IntPtr(SB_THUMBPOSITION + (pos << 16)), IntPtr.Zero);
        }
        catch { /* ignore */ }
    }

    #endregion
}
