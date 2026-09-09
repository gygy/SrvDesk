using System.Runtime.InteropServices;

namespace SrvDesk;

internal enum ShutdownPowerAction
{
    Shutdown,
    Restart,
    LogOff,
    Suspend,
    Hibernate,
    Lock,
}

/// <summary>执行关机/重启/注销/睡眠/休眠/锁定（对齐 Shutdown Agent 能力）。</summary>
internal static class ShutdownPowerActions
{
    private const uint EWX_LOGOFF = 0x00000000;
    private const uint EWX_SHUTDOWN = 0x00000001;
    private const uint EWX_REBOOT = 0x00000002;
    private const uint EWX_FORCE = 0x00000004;
    private const uint EWX_POWEROFF = 0x00000008;

    private const uint TOKEN_QUERY = 0x0008;
    private const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
    private const uint SE_PRIVILEGE_ENABLED = 0x00000002;

    public static string DisplayName(ShutdownPowerAction action) => action switch
    {
        ShutdownPowerAction.Shutdown => AppLang.L("关机", "Shutdown"),
        ShutdownPowerAction.Restart => AppLang.L("重启", "Restart"),
        ShutdownPowerAction.LogOff => AppLang.L("注销", "Log Off"),
        ShutdownPowerAction.Suspend => AppLang.L("睡眠", "Suspend"),
        ShutdownPowerAction.Hibernate => AppLang.L("休眠", "Hibernate"),
        ShutdownPowerAction.Lock => AppLang.L("锁定计算机", "Lock Computer"),
        _ => action.ToString(),
    };

    public static void Execute(ShutdownPowerAction action, bool force)
    {
        switch (action)
        {
            case ShutdownPowerAction.Shutdown:
                ExitWindows(EWX_POWEROFF, force);
                break;
            case ShutdownPowerAction.Restart:
                ExitWindows(EWX_REBOOT, force);
                break;
            case ShutdownPowerAction.LogOff:
                ExitWindows(EWX_LOGOFF, force);
                break;
            case ShutdownPowerAction.Suspend:
                if (!SetSuspendState(hibernate: false, forceCritical: force, disableWakeEvent: false))
                    throw new InvalidOperationException(AppLang.L("无法进入睡眠。", "Unable to suspend."));
                break;
            case ShutdownPowerAction.Hibernate:
                if (!SetSuspendState(hibernate: true, forceCritical: force, disableWakeEvent: false))
                    throw new InvalidOperationException(AppLang.L("无法休眠（可能未启用休眠）。", "Unable to hibernate (may be disabled)."));
                break;
            case ShutdownPowerAction.Lock:
                if (!LockWorkStation())
                    throw new InvalidOperationException(AppLang.L("无法锁定工作站。", "Unable to lock the workstation."));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action));
        }
    }

    private static void ExitWindows(uint action, bool force)
    {
        var flags = force ? action | EWX_FORCE : action;
        EnableShutdownPrivilege(true);
        try
        {
            if (!ExitWindowsEx(flags, 0))
            {
                var err = Marshal.GetLastWin32Error();
                throw new InvalidOperationException(
                    AppLang.L($"关机操作失败（Win32 {err}）。", $"Power action failed (Win32 {err})."));
            }
        }
        finally
        {
            EnableShutdownPrivilege(false);
        }
    }

    private static void EnableShutdownPrivilege(bool enable)
    {
        if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out var token))
            return;
        try
        {
            if (!LookupPrivilegeValue(null, "SeShutdownPrivilege", out var luid))
                return;
            var tp = new TOKEN_PRIVILEGES
            {
                PrivilegeCount = 1,
                Privileges = new LUID_AND_ATTRIBUTES
                {
                    Luid = luid,
                    Attributes = enable ? SE_PRIVILEGE_ENABLED : 0,
                },
            };
            AdjustTokenPrivileges(token, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
        }
        finally
        {
            CloseHandle(token);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LUID
    {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LUID_AND_ATTRIBUTES
    {
        public LUID Luid;
        public uint Attributes;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TOKEN_PRIVILEGES
    {
        public uint PrivilegeCount;
        public LUID_AND_ATTRIBUTES Privileges;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ExitWindowsEx(uint uFlags, uint dwReason);

    [DllImport("user32.dll")]
    private static extern bool LockWorkStation();

    [DllImport("powrprof.dll", SetLastError = true)]
    private static extern bool SetSuspendState(bool hibernate, bool forceCritical, bool disableWakeEvent);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool LookupPrivilegeValue(string? lpSystemName, string lpName, out LUID lpLuid);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool AdjustTokenPrivileges(
        IntPtr tokenHandle, bool disableAllPrivileges, ref TOKEN_PRIVILEGES newState,
        uint bufferLength, IntPtr previousState, IntPtr returnLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);
}
