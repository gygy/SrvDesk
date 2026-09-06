using System.Text;

namespace WinOpt;

/// <summary>优化项对应的一键开启/关闭脚本（.reg / CMD / PowerShell）。</summary>
internal enum SettingActionKind
{
    Reg,
    Cmd,
    PowerShell,
    Mixed,
}

internal sealed class SettingActionRecipe
{
    public SettingActionKind Kind { get; }
    public string EnableContent { get; }
    public string DisableContent { get; }
    public string Note { get; }
    public string FileExtension { get; }

    public SettingActionRecipe(
        SettingActionKind kind,
        string enableContent,
        string disableContent,
        string? note = null,
        string? fileExtension = null)
    {
        Kind = kind;
        EnableContent = enableContent.TrimEnd() + "\r\n";
        DisableContent = disableContent.TrimEnd() + "\r\n";
        Note = note ?? "";
        FileExtension = fileExtension ?? DefaultExt(kind);
    }

    public string KindLabel => Kind switch
    {
        SettingActionKind.Reg => "注册表 (.reg)",
        SettingActionKind.Cmd => "批处理 (.cmd)",
        SettingActionKind.PowerShell => "PowerShell (.ps1)",
        _ => "混合脚本",
    };

    public string ContentFor(bool enable) => enable ? EnableContent : DisableContent;

    public string SuggestedFileName(string itemTitle, bool enable)
    {
        var safe = SanitizeFileName(itemTitle);
        var side = enable ? "开启" : "关闭";
        return $"{safe}-{side}{FileExtension}";
    }

    static string DefaultExt(SettingActionKind kind) => kind switch
    {
        SettingActionKind.Reg => ".reg",
        SettingActionKind.Cmd => ".cmd",
        SettingActionKind.PowerShell => ".ps1",
        _ => ".txt",
    };

    static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(name.Length);
        foreach (var c in name)
            sb.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
        var s = sb.ToString().Trim();
        return s.Length == 0 ? "优化项" : (s.Length > 40 ? s.Substring(0, 40) : s);
    }
}

/// <summary>生成与本软件 Apply 逻辑一致的 .reg / CMD 片段。</summary>
internal static class ActionScript
{
    public static SettingActionRecipe Reg(string enableBody, string disableBody, string? note = null) =>
        new(SettingActionKind.Reg, WrapReg(enableBody), WrapReg(disableBody), note);

    public static SettingActionRecipe Cmd(string enableBody, string disableBody, string? note = null) =>
        new(SettingActionKind.Cmd, WrapCmd(enableBody), WrapCmd(disableBody), note);

    public static SettingActionRecipe Ps(string enableBody, string disableBody, string? note = null) =>
        new(SettingActionKind.PowerShell, WrapPs(enableBody), WrapPs(disableBody), note);

    public static SettingActionRecipe Mixed(string enableBody, string disableBody, string? note = null) =>
        new(SettingActionKind.Mixed, enableBody, disableBody, note, ".txt");

    public static string WrapReg(string body) =>
        "Windows Registry Editor Version 5.00\r\n\r\n" + body.Trim() + "\r\n";

    public static string WrapCmd(string body) =>
        "@echo off\r\nchcp 65001 >nul\r\n" + body.Trim() + "\r\necho.\r\necho 完成。\r\npause\r\n";

    public static string WrapPs(string body) =>
        "#Requires -RunAsAdministrator\r\n$ErrorActionPreference = 'Stop'\r\n" + body.Trim() + "\r\nWrite-Host '完成。'\r\n";

    public static string HkLm(string subKey) => @"[HKEY_LOCAL_MACHINE\" + subKey + "]";
    public static string HkCu(string subKey) => @"[HKEY_CURRENT_USER\" + subKey + "]";

    public static string Dword(string name, int value) =>
        "\"" + EscapeRegName(name) + "\"=dword:" + ((uint)value).ToString("x8");

    public static string Sz(string name, string value) =>
        "\"" + EscapeRegName(name) + "\"=\"" + EscapeRegSz(value) + "\"";

    public static string DeleteValue(string name) =>
        "\"" + EscapeRegName(name) + "\"=-";

    public static string Hex(string name, params byte[] bytes)
    {
        var parts = new string[bytes.Length];
        for (var i = 0; i < bytes.Length; i++)
            parts[i] = bytes[i].ToString("x2");
        return "\"" + EscapeRegName(name) + "\"=hex:" + string.Join(",", parts);
    }

    public static string Block(string header, params string[] lines)
    {
        var sb = new StringBuilder();
        sb.AppendLine(header);
        foreach (var line in lines)
            sb.AppendLine(line);
        sb.AppendLine();
        return sb.ToString();
    }

    public static SettingActionRecipe DwordToggle(
        bool hkcu,
        string subKey,
        string name,
        int onValue,
        int offValue,
        string? note = null)
    {
        var header = hkcu ? HkCu(subKey) : HkLm(subKey);
        return Reg(
            Block(header, Dword(name, onValue)),
            Block(header, Dword(name, offValue)),
            note);
    }

    public static SettingActionRecipe DwordOnDeleteOff(
        bool hkcu,
        string subKey,
        string name,
        int onValue,
        string? note = null)
    {
        var header = hkcu ? HkCu(subKey) : HkLm(subKey);
        return Reg(
            Block(header, Dword(name, onValue)),
            Block(header, DeleteValue(name)),
            note);
    }

    public static SettingActionRecipe Service(
        string serviceName,
        bool enableMeansStart,
        bool disableWhenOff = true,
        string? note = null)
    {
        // enableMeansStart=true：开启优化 = 启动服务；false：开启优化 = 禁用服务
        string On()
        {
            if (enableMeansStart)
                return $"sc config \"{serviceName}\" start= auto\r\nsc start \"{serviceName}\"";
            return disableWhenOff
                ? $"sc stop \"{serviceName}\"\r\nsc config \"{serviceName}\" start= disabled"
                : $"sc config \"{serviceName}\" start= demand";
        }

        string Off()
        {
            if (enableMeansStart)
                return disableWhenOff
                    ? $"sc stop \"{serviceName}\"\r\nsc config \"{serviceName}\" start= disabled"
                    : $"sc config \"{serviceName}\" start= demand\r\nsc stop \"{serviceName}\"";
            return $"sc config \"{serviceName}\" start= auto\r\nsc start \"{serviceName}\"";
        }

        return Cmd(On(), Off(), note ?? $"服务：{serviceName}");
    }

    static string EscapeRegName(string name) => name.Replace("\\", "\\\\").Replace("\"", "\\\"");
    static string EscapeRegSz(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
