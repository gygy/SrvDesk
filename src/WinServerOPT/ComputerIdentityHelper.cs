using System.Management;
using System.Text.RegularExpressions;

namespace WinOpt;

internal sealed class ComputerIdentityInfo
{
    public string ComputerName { get; set; } = "";
    public string Workgroup { get; set; } = "";
    public bool PartOfDomain { get; set; }
    public string Domain { get; set; } = "";

    public string Summary =>
        PartOfDomain
            ? $"{ComputerName} · 域 {Domain}"
            : $"{ComputerName} · 工作组 {Workgroup}";
}

internal static class ComputerIdentityHelper
{
    private const uint JoinWorkgroup = 4096;

    public static ComputerIdentityInfo Read()
    {
        using var obj = GetComputerSystem();
        return new ComputerIdentityInfo
        {
            ComputerName = obj["Name"]?.ToString() ?? Environment.MachineName,
            Workgroup = obj["Workgroup"]?.ToString() ?? "WORKGROUP",
            PartOfDomain = obj["PartOfDomain"] is bool p && p,
            Domain = obj["Domain"]?.ToString() ?? "",
        };
    }

    public static bool ValidateNetbiosName(string name, out string error)
    {
        error = "";
        if (string.IsNullOrWhiteSpace(name))
        {
            error = "名称不能为空。";
            return false;
        }

        name = name.Trim();
        if (name.Length > 15)
        {
            error = "名称最长 15 个字符（NetBIOS 限制）。";
            return false;
        }

        if (!Regex.IsMatch(name, @"^[A-Za-z0-9](?:[A-Za-z0-9-]{0,13}[A-Za-z0-9])?$"))
        {
            error = "仅允许字母、数字、连字符，且不能以连字符开头或结尾。";
            return false;
        }

        return true;
    }

    public static void RenameComputer(string newName)
    {
        if (!ValidateNetbiosName(newName, out var err))
            throw new InvalidOperationException(err);

        newName = newName.Trim().ToUpperInvariant();
        var current = Read().ComputerName;
        if (newName.Equals(current, StringComparison.OrdinalIgnoreCase))
            return;

        using var cs = GetComputerSystem();
        var inParams = cs.GetMethodParameters("Rename");
        inParams["Name"] = newName;
        using var outParams = cs.InvokeMethod("Rename", inParams, null);
        var code = Convert.ToUInt32(outParams["ReturnValue"]);
        if (code != 0)
            throw new InvalidOperationException($"重命名计算机失败，WMI 返回码 {code}。");
    }

    public static void SetWorkgroup(string workgroup)
    {
        if (!ValidateNetbiosName(workgroup, out var err))
            throw new InvalidOperationException(err);

        workgroup = workgroup.Trim().ToUpperInvariant();
        var info = Read();
        if (info.PartOfDomain)
            throw new InvalidOperationException($"当前已加入域「{info.Domain}」，请先退域后再修改工作组。");

        if (workgroup.Equals(info.Workgroup, StringComparison.OrdinalIgnoreCase))
            return;

        using var cs = GetComputerSystem();
        var inParams = cs.GetMethodParameters("JoinDomainOrWorkgroup");
        inParams["Name"] = workgroup;
        inParams["Options"] = JoinWorkgroup;
        inParams["UserName"] = null;
        inParams["Password"] = null;
        using var outParams = cs.InvokeMethod("JoinDomainOrWorkgroup", inParams, null);
        var code = Convert.ToUInt32(outParams["ReturnValue"]);
        if (code != 0)
            throw new InvalidOperationException($"修改工作组失败，WMI 返回码 {code}。");
    }

    public static void ScheduleRestart(int seconds = 60)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "shutdown.exe",
            Arguments = $"/r /t {seconds} /c \"{AppBrand.ProductName}：计算机名/工作组已更改，系统即将重启\"",
            UseShellExecute = false,
            CreateNoWindow = true,
        });
    }

    private static ManagementObject GetComputerSystem()
    {
        using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem");
        foreach (ManagementObject obj in searcher.Get())
            return obj;

        throw new InvalidOperationException("无法读取 Win32_ComputerSystem。");
    }
}
