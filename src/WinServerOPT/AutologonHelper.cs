using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace WinOpt;

internal sealed class AutologonSettings
{
    public string Domain { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    /// <summary>为 true 时写入 Password；为 false 且启用时保留 LSA 中已有密码。</summary>
    public bool UpdatePassword { get; set; } = true;
}

internal sealed class AutologonStatus
{
    public bool Enabled { get; set; }
    public string Username { get; set; } = "";
    public string Domain { get; set; } = "";
    public bool HasStoredPassword { get; set; }

    public string DisplayDefault()
    {
        if (!Enabled) return "未启用";
        var who = string.IsNullOrEmpty(Domain) ? Username : $"{Domain}\\{Username}";
        var pwd = HasStoredPassword ? " · 密码已存 LSA" : " · 无密码";
        return $"已启用 · {who}{pwd}";
    }
}

/// <summary>
/// 自动登录，实现方式对齐微软 Sysinternals Autologon：
/// Winlogon 注册表 + LsaStorePrivateData("DefaultPassword")。
/// </summary>
internal static class AutologonHelper
{
    private const string WinlogonKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon";
    private const string LsaPasswordKey = "DefaultPassword";

    public static AutologonStatus Read()
    {
        using var key = Registry.LocalMachine.OpenSubKey(WinlogonKey);
        var auto = key?.GetValue("AutoAdminLogon") as string;
        var enabled = auto == "1";
        var user = key?.GetValue("DefaultUserName") as string ?? "";
        var domain = key?.GetValue("DefaultDomainName") as string ?? "";
        var hasPwd = HasLsaPassword() || key?.GetValue("DefaultPassword") is string rp && !string.IsNullOrEmpty(rp);
        return new AutologonStatus
        {
            Enabled = enabled,
            Username = user,
            Domain = domain,
            HasStoredPassword = hasPwd,
        };
    }

    public static void Enable(AutologonSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Username))
            throw new InvalidOperationException("请填写自动登录的用户名。");

        var domain = NormalizeDomain(settings.Domain);
        using var key = Registry.LocalMachine.OpenSubKey(WinlogonKey, writable: true)
            ?? throw new InvalidOperationException("无法打开 Winlogon 注册表项。");

        key.SetValue("AutoAdminLogon", "1", RegistryValueKind.String);
        key.SetValue("DefaultUserName", settings.Username.Trim(), RegistryValueKind.String);
        key.SetValue("DefaultDomainName", domain, RegistryValueKind.String);
        key.DeleteValue("DefaultPassword", throwOnMissingValue: false);

        if (settings.UpdatePassword)
        {
            if (string.IsNullOrEmpty(settings.Password))
                throw new InvalidOperationException("首次启用自动登录必须填写密码。");
            StoreLsaPassword(settings.Password);
        }
        else if (!HasLsaPassword() && key.GetValue("DefaultPassword") is not string)
        {
            throw new InvalidOperationException("未检测到已保存的密码，请填写密码。");
        }
    }

    public static void Disable()
    {
        using var key = Registry.LocalMachine.OpenSubKey(WinlogonKey, writable: true);
        if (key is not null)
        {
            key.SetValue("AutoAdminLogon", "0", RegistryValueKind.String);
            key.DeleteValue("DefaultPassword", throwOnMissingValue: false);
        }
        ClearLsaPassword();
    }

    public static AutologonSettings FromStatus(AutologonStatus status) => new()
    {
        Domain = status.Domain,
        Username = status.Username,
        UpdatePassword = false,
    };

    private static string NormalizeDomain(string domain)
    {
        var d = domain.Trim();
        if (d is "" or "." or ".\")
            return Environment.UserDomainName;
        return d;
    }

    private static bool HasLsaPassword()
    {
        try
        {
            var pwd = RetrieveLsaPassword();
            return !string.IsNullOrEmpty(pwd);
        }
        catch
        {
            return false;
        }
    }

    private static void StoreLsaPassword(string password)
    {
        using var policy = LsaConnection.Open();
        policy.StoreSecret(LsaPasswordKey, password);
    }

    private static void ClearLsaPassword()
    {
        try
        {
            using var policy = LsaConnection.Open();
            policy.StoreSecret(LsaPasswordKey, null);
        }
        catch { /* ignore */ }
    }

    private static string? RetrieveLsaPassword()
    {
        using var policy = LsaConnection.Open();
        return policy.RetrieveSecret(LsaPasswordKey);
    }

    private sealed class LsaConnection : IDisposable
    {
        private IntPtr _handle;

        public static LsaConnection Open()
        {
            var objAttrs = new LSA_OBJECT_ATTRIBUTES();
            var conn = new LsaConnection();
            var status = LsaOpenPolicy(IntPtr.Zero, ref objAttrs, PolicyAllAccess, out conn._handle);
            if (status != 0)
                throw new Win32Exception(LsaNtStatusToWinError(status));
            return conn;
        }

        public void StoreSecret(string key, string? value)
        {
            var keyUs = LsaUnicode.From(key);
            IntPtr dataPtr = IntPtr.Zero;
            LSA_UNICODE_STRING? dataUs = null;
            if (value is not null)
            {
                dataUs = LsaUnicode.From(value);
                dataPtr = Marshal.AllocHGlobal(Marshal.SizeOf<LSA_UNICODE_STRING>());
                Marshal.StructureToPtr(dataUs.Value, dataPtr, false);
            }

            try
            {
                var status = LsaStorePrivateData(_handle, ref keyUs, dataPtr);
                if (status != 0)
                    throw new Win32Exception(LsaNtStatusToWinError(status));
            }
            finally
            {
                if (dataPtr != IntPtr.Zero)
                {
                    Marshal.DestroyStructure<LSA_UNICODE_STRING>(dataPtr);
                    Marshal.FreeHGlobal(dataPtr);
                }
            }
        }

        public string? RetrieveSecret(string key)
        {
            var keyUs = LsaUnicode.From(key);
            var status = LsaRetrievePrivateData(_handle, ref keyUs, out var buffer);
            if (status != 0)
            {
                var err = LsaNtStatusToWinError(status);
                if (err == 2) return null;
                throw new Win32Exception(err);
            }
            if (buffer == IntPtr.Zero) return null;
            try
            {
                var us = Marshal.PtrToStructure<LSA_UNICODE_STRING>(buffer);
                if (us.Length == 0) return null;
                return Marshal.PtrToStringUni(us.Buffer, us.Length / 2);
            }
            finally
            {
                LsaFreeMemory(buffer);
            }
        }

        public void Dispose()
        {
            if (_handle != IntPtr.Zero)
            {
                LsaClose(_handle);
                _handle = IntPtr.Zero;
            }
        }

        private const uint PolicyAllAccess = 0x000F0FFF;

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern uint LsaOpenPolicy(
            IntPtr SystemName,
            ref LSA_OBJECT_ATTRIBUTES ObjectAttributes,
            uint DesiredAccess,
            out IntPtr PolicyHandle);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint LsaStorePrivateData(
            IntPtr PolicyHandle,
            ref LSA_UNICODE_STRING KeyName,
            IntPtr PrivateData);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint LsaRetrievePrivateData(
            IntPtr PolicyHandle,
            ref LSA_UNICODE_STRING KeyName,
            out IntPtr PrivateData);

        [DllImport("advapi32.dll")]
        private static extern uint LsaClose(IntPtr ObjectHandle);

        [DllImport("advapi32.dll")]
        private static extern int LsaNtStatusToWinError(uint status);

        [DllImport("advapi32.dll")]
        private static extern int LsaFreeMemory(IntPtr buffer);

        [StructLayout(LayoutKind.Sequential)]
        private struct LSA_OBJECT_ATTRIBUTES
        {
            public uint Length;
            public IntPtr RootDirectory;
            public IntPtr ObjectName;
            public uint Attributes;
            public IntPtr SecurityDescriptor;
            public IntPtr SecurityQualityOfService;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct LSA_UNICODE_STRING
        {
            public ushort Length;
            public ushort MaximumLength;
            public IntPtr Buffer;
        }

        private static class LsaUnicode
        {
            public static LSA_UNICODE_STRING From(string s)
            {
                var bytes = Encoding.Unicode.GetBytes(s);
                var buffer = Marshal.AllocHGlobal(bytes.Length + 2);
                Marshal.Copy(bytes, 0, buffer, bytes.Length);
                Marshal.WriteInt16(buffer, bytes.Length, 0);
                return new LSA_UNICODE_STRING
                {
                    Length = (ushort)bytes.Length,
                    MaximumLength = (ushort)(bytes.Length + 2),
                    Buffer = buffer,
                };
            }
        }
    }
}
