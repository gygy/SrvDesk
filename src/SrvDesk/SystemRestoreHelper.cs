using System.Management;
using Microsoft.Win32;

namespace SrvDesk;

/// <summary>应用前可选创建系统还原点（对齐 Sophia / WinUtil / Win11Debloat）。</summary>
internal static class SystemRestoreHelper
{
    public static bool IsPolicyDisabled()
    {
        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var k = baseKey.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows NT\SystemRestore");
        return k?.GetValue("DisableSR") is int i && i == 1;
    }

    /// <returns>true=已创建或本来就跳过；false=调用方应中止应用。</returns>
    public static bool TryCreate(string description, out string message)
    {
        if (IsPolicyDisabled())
        {
            message = "系统还原已被策略关闭，已跳过还原点。";
            ApplyLog.Write(message);
            return true;
        }

        try
        {
            using (var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            using (var k = baseKey.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore", writable: true))
                k?.SetValue("SystemRestorePointCreationFrequency", 0, RegistryValueKind.DWord);

            using var mc = new ManagementClass(@"\\.\root\default:SystemRestore");
            var args = mc.GetMethodParameters("CreateRestorePoint");
            args["Description"] = description;
            args["RestorePointType"] = 12; // MODIFY_SETTINGS
            args["EventType"] = 100; // BEGIN_SYSTEM_CHANGE
            using var result = mc.InvokeMethod("CreateRestorePoint", args, null);
            var code = result?["ReturnValue"] is uint u ? (int)u
                : result?["ReturnValue"] is int i ? i
                : 0;
            if (code != 0)
            {
                message = "创建还原点返回 " + code + "（本机可能未启用系统还原）。";
                ApplyLog.Write(message);
                return true;
            }

            message = "已创建系统还原点：" + description;
            ApplyLog.Write(message);
            return true;
        }
        catch (Exception ex)
        {
            message = "创建还原点失败（已跳过）：" + ex.Message;
            ApplyLog.Write(message);
            return true;
        }
    }
}
