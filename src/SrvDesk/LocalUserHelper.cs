using System.DirectoryServices;
using System.Text.RegularExpressions;

namespace SrvDesk;

internal sealed class LocalUserCreateRequest
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public bool MustChangePasswordAtNextLogon { get; set; }
    public bool UserCannotChangePassword { get; set; } = true;
    public bool PasswordNeverExpires { get; set; } = true;
    public bool AddToAdministrators { get; set; } = true;
}

/// <summary>本地用户快速创建（WinNT / SAM）。</summary>
internal static class LocalUserHelper
{
    // ADS_UF_* / UF_*
    private const int UfScript = 0x0001;
    private const int UfNormalAccount = 0x0200;
    private const int UfPasswdCantChange = 0x0040;
    private const int UfDontExpirePasswd = 0x10000;

    private static readonly Regex InvalidNameChars = new(
        @"[""/\\\[\]:;|=,+*?<>@]",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static string? Validate(LocalUserCreateRequest req)
    {
        var name = (req.Username ?? "").Trim();
        if (name.Length == 0)
            return "请填写用户名。";
        if (name.Length > 20)
            return "用户名最长 20 个字符。";
        if (name.EndsWith(".", StringComparison.Ordinal) || name.EndsWith(" ", StringComparison.Ordinal))
            return "用户名不能以点或空格结尾。";
        if (InvalidNameChars.IsMatch(name))
            return "用户名含非法字符。";
        if (string.IsNullOrEmpty(req.Password))
            return "请填写密码。";
        if (req.MustChangePasswordAtNextLogon && req.UserCannotChangePassword)
            return "「下次登录须更改密码」与「用户不能修改密码」不能同时启用。";
        if (req.MustChangePasswordAtNextLogon && req.PasswordNeverExpires)
            return "「下次登录须更改密码」与「密码永远不过期」不能同时启用。";
        return null;
    }

    public static bool UserExists(string username)
    {
        username = username.Trim();
        try
        {
            using var machine = OpenMachine();
            using var _ = machine.Children.Find(username, "User");
            return true;
        }
        catch (DirectoryServicesCOMException)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>创建本地用户。成功返回 null；失败返回错误说明。</summary>
    public static string? Create(LocalUserCreateRequest req)
    {
        var err = Validate(req);
        if (err is not null) return err;

        var username = req.Username.Trim();
        if (UserExists(username))
            return "用户「" + username + "」已存在。";

        try
        {
            using var machine = OpenMachine();
            using var user = machine.Children.Add(username, "User");
            user.Invoke("SetPassword", req.Password);

            var flags = UfScript | UfNormalAccount;
            if (req.UserCannotChangePassword)
                flags |= UfPasswdCantChange;
            if (req.PasswordNeverExpires)
                flags |= UfDontExpirePasswd;

            user.Invoke("Put", "UserFlags", flags);
            // 0 = 不要求下次登录改密；1 = 要求改密
            user.Invoke("Put", "PasswordExpired", req.MustChangePasswordAtNextLogon ? 1 : 0);
            user.CommitChanges();

            if (req.AddToAdministrators)
                AddToLocalGroup(machine, user, "Administrators");

            ApplyLog.Write("已创建本地用户：" + username
                + (req.AddToAdministrators ? "（Administrators）" : "")
                + "；不能改密=" + req.UserCannotChangePassword
                + "；密码不过期=" + req.PasswordNeverExpires
                + "；下次改密=" + req.MustChangePasswordAtNextLogon);
            return null;
        }
        catch (Exception ex)
        {
            ApplyLog.Write("创建本地用户失败：" + username + " → " + ex.Message);
            return "创建失败：" + TrimMessage(ex);
        }
    }

    private static DirectoryEntry OpenMachine() =>
        new("WinNT://" + Environment.MachineName + ",computer");

    private static void AddToLocalGroup(DirectoryEntry machine, DirectoryEntry user, string groupName)
    {
        using var group = machine.Children.Find(groupName, "Group");
        group.Invoke("Add", user.Path);
        group.CommitChanges();
    }

    private static string TrimMessage(Exception ex)
    {
        var msg = ex.InnerException?.Message ?? ex.Message;
        msg = (msg ?? "").Trim();
        if (msg.Length > 240) msg = msg.Substring(0, 240) + "…";
        return msg.Length > 0 ? msg : ex.GetType().Name;
    }
}
