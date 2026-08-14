Attribute VB_Name = "modHelper"
Option Explicit

Public Const HKEY_CURRENT_USER As Long = &H80000001
Public Const HKEY_LOCAL_MACHINE As Long = &H80000002
Public Const KEY_QUERY_VALUE As Long = &H1
Public Const KEY_WOW64_64KEY As Long = &H100
Public Const REG_SZ As Long = 1
Public Const REG_DWORD As Long = 4
Public Const ERROR_SUCCESS As Long = 0

Public Declare Function RegOpenKeyEx Lib "advapi32.dll" Alias "RegOpenKeyExA" ( _
    ByVal hKey As Long, ByVal lpSubKey As String, ByVal ulOptions As Long, _
    ByVal samDesired As Long, phkResult As Long) As Long
Public Declare Function RegQueryValueEx Lib "advapi32.dll" Alias "RegQueryValueExA" ( _
    ByVal hKey As Long, ByVal lpValueName As String, ByVal lpReserved As Long, _
    lpType As Long, lpData As Any, lpcbData As Long) As Long
Public Declare Function RegCloseKey Lib "advapi32.dll" (ByVal hKey As Long) As Long

Public Function ShellWait(ByVal cmd As String) As Long
    Dim sh As Object
    Set sh = CreateObject("WScript.Shell")
    ShellWait = sh.Run(cmd, 0, True)
End Function

Public Function Cmd(ByVal line As String) As Long
    Cmd = ShellWait("cmd.exe /c " & line)
End Function

' strFullRegPath 形如 HKLM\SOFTWARE\...\ValueName 或 HKCU\...
' bln64Bit=True 时走 KEY_WOW64_64KEY（对应原程序 REG ADD /reg:64）
Public Function ReadRegistryValue(ByVal strFullRegPath As String, Optional ByVal bln64Bit As Boolean = True) As String
    Dim hRoot As Long
    Dim strSub As String
    Dim strName As String
    Dim p As Long
    Dim p2 As Long
    Dim hKey As Long
    Dim sam As Long
    Dim lType As Long
    Dim lSize As Long
    Dim lRet As Long
    Dim lDword As Long
    Dim buf As String

    On Error GoTo EH

    strFullRegPath = Trim$(strFullRegPath)
    If Len(strFullRegPath) < 5 Then
        ReadRegistryValue = "路径格式错误"
        Exit Function
    End If

    p = InStr(strFullRegPath, "\")
    If p = 0 Then
        ReadRegistryValue = "路径格式错误"
        Exit Function
    End If

    Select Case UCase$(Left$(strFullRegPath, p - 1))
        Case "HKLM", "HKEY_LOCAL_MACHINE"
            hRoot = HKEY_LOCAL_MACHINE
        Case "HKCU", "HKEY_CURRENT_USER"
            hRoot = HKEY_CURRENT_USER
        Case Else
            ReadRegistryValue = "不支持的根键"
            Exit Function
    End Select

    strSub = Mid$(strFullRegPath, p + 1)
    p2 = InStrRev(strSub, "\")
    If p2 = 0 Then
        ReadRegistryValue = "路径格式错误"
        Exit Function
    End If
    strName = Mid$(strSub, p2 + 1)
    strSub = Left$(strSub, p2 - 1)

    sam = KEY_QUERY_VALUE
    If bln64Bit Then sam = sam Or KEY_WOW64_64KEY

    lRet = RegOpenKeyEx(hRoot, strSub, 0, sam, hKey)
    If lRet <> ERROR_SUCCESS Then
        ReadRegistryValue = "项不存在/权限不足（错误码：" & CStr(lRet) & "）"
        Exit Function
    End If

    lSize = 4
    lRet = RegQueryValueEx(hKey, strName, 0, lType, lDword, lSize)
    If lType = REG_DWORD And lRet = ERROR_SUCCESS Then
        Call RegCloseKey(hKey)
        ReadRegistryValue = CStr(lDword)
        Exit Function
    End If

    lSize = 0
    lRet = RegQueryValueEx(hKey, strName, 0, lType, ByVal 0, lSize)
    If lRet <> ERROR_SUCCESS And lSize = 0 Then
        Call RegCloseKey(hKey)
        ReadRegistryValue = "值不存在"
        Exit Function
    End If

    If lType = REG_SZ Then
        If lSize <= 1 Then
            Call RegCloseKey(hKey)
            ReadRegistryValue = "值为空"
            Exit Function
        End If
        buf = String$(lSize, vbNullChar)
        lRet = RegQueryValueEx(hKey, strName, 0, lType, ByVal buf, lSize)
        Call RegCloseKey(hKey)
        If lRet <> ERROR_SUCCESS Then
            ReadRegistryValue = "读取失败"
        Else
            ReadRegistryValue = Left$(buf, InStr(buf & vbNullChar, vbNullChar) - 1)
        End If
        Exit Function
    End If

    Call RegCloseKey(hKey)
    ReadRegistryValue = "不支持的类型(" & CStr(lType) & ")"
    Exit Function

EH:
    On Error Resume Next
    If hKey <> 0 Then Call RegCloseKey(hKey)
    ReadRegistryValue = "异常：" & Err.Description
End Function

' 返回 "状态|启动类型"，供勾选框回读 AudioSrv
Public Function GetAudioSrvInfo(ByRef strState As String, ByRef strStartMode As String) As String
    Dim loc As Object
    Dim col As Object
    Dim itm As Object
    Dim n As Long

    On Error GoTo EH
    strState = ""
    strStartMode = ""

    Set loc = GetObject("winmgmts:{impersonationLevel=impersonate}!\\.\root\cimv2")
    Set col = loc.ExecQuery("SELECT State, StartMode FROM Win32_Service WHERE Name = 'AudioSrv'")
    n = col.Count
    If n = 0 Then
        GetAudioSrvInfo = "未找到AudioSrv服务"
        Exit Function
    End If

    For Each itm In col
        Select Case UCase$(CStr(itm.State))
            Case "RUNNING": strState = "运行中"
            Case "STOPPED": strState = "已停止"
            Case "START PENDING", "STARTING": strState = "启动中"
            Case "STOP PENDING", "STOPPING": strState = "停止中"
            Case "PAUSED": strState = "已暂停"
            Case Else: strState = "未知状态：" & CStr(itm.State)
        End Select

        Select Case UCase$(CStr(itm.StartMode))
            Case "AUTO": strStartMode = "自动"
            Case "MANUAL": strStartMode = "手动"
            Case "DISABLED": strStartMode = "禁用"
            Case "BOOT": strStartMode = "引导启动"
            Case "SYSTEM": strStartMode = "系统启动"
            Case Else: strStartMode = "未知类型：" & CStr(itm.StartMode)
        End Select
        Exit For
    Next

    GetAudioSrvInfo = strState & "|" & strStartMode
    Exit Function
EH:
    GetAudioSrvInfo = "查询失败：" & Err.Description
End Function

Public Function IsWindowsServer() As Boolean
    Dim loc As Object
    Dim col As Object
    Dim itm As Object
    On Error GoTo EH
    Set loc = GetObject("winmgmts:\\.\root\cimv2")
    Set col = loc.ExecQuery("SELECT Caption FROM Win32_OperatingSystem")
    For Each itm In col
        IsWindowsServer = (InStr(1, CStr(itm.Caption), "Windows Server", vbTextCompare) > 0)
        Exit Function
    Next
EH:
End Function

Public Function ReadPasswordComplexityDisabled() As Boolean
    Dim f As String
    Dim s As String
    f = "C:\Pswd.cfg"
    On Error Resume Next
    Call Cmd("secedit /export /cfg  " & f)
    If Dir$(f) = "" Then Exit Function
    Open f For Input As #1
    Do While Not EOF(1)
        Line Input #1, s
        If InStr(s, "PasswordComplexity = 0") > 0 Then
            ReadPasswordComplexityDisabled = True
            Exit Do
        End If
    Loop
    Close #1
    On Error Resume Next
    Kill f
End Function
