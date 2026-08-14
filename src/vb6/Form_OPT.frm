VERSION 5.00
Begin VB.Form Form_OPT
   BorderStyle     =   1  'Fixed Single
   Caption         =   "Win一键优化"
   ClientHeight    =   8385
   ClientWidth     =   9720
   LinkTopic       =   "Form1"
   MaxButton       =   0   'False
   ScaleHeight     =   8385
   ScaleWidth      =   9720
   StartUpPosition =   2  '屏幕中心
   Begin VB.Timer Timer1
      Interval        =   200
      Left            =   120
      Top             =   7920
   End
   Begin VB.CommandButton Command_WSOPT
      Caption         =   "一键优化"
      Height          =   495
      Left            =   3960
      TabIndex        =   0
      Top             =   7680
      Width           =   1815
   End
   Begin VB.CommandButton Command1
      Caption         =   "全部选择"
      Height          =   375
      Left            =   1560
      TabIndex        =   1
      Top             =   7740
      Width           =   1215
   End
   Begin VB.CommandButton Command2
      Caption         =   "全部取消"
      Height          =   375
      Left            =   2880
      TabIndex        =   2
      Top             =   7740
      Width           =   1215
   End
   Begin VB.CommandButton cmdAbout
      Caption         =   "关于"
      Height          =   375
      Left            =   8280
      TabIndex        =   3
      Top             =   7740
      Width           =   1215
   End
   Begin VB.Label lblHint
      Caption         =   "先勾选或取消配置项，再运行"
      Height          =   255
      Left            =   240
      TabIndex        =   4
      Top             =   120
      Width           =   4455
   End
   Begin VB.Frame Frame4
      Caption         =   "性能及安全"
      Height          =   1815
      Left            =   240
      TabIndex        =   5
      Top             =   480
      Width           =   4575
      Begin VB.CheckBox Check_CPU
         Caption         =   "CPU资源分配程序优先"
         Height          =   255
         Left            =   240
         TabIndex        =   6
         Top             =   360
         Width           =   4095
      End
      Begin VB.CheckBox Check_DEP
         Caption         =   "数据执行保护DEP（T）"
         Height          =   255
         Left            =   240
         TabIndex        =   7
         Top             =   680
         Width           =   4095
      End
      Begin VB.CheckBox Check_LUA
         Caption         =   "禁用用户账户控制UAC"
         Height          =   255
         Left            =   240
         TabIndex        =   8
         Top             =   1000
         Width           =   4095
      End
      Begin VB.CheckBox Check_IE
         Caption         =   "关闭IE增强安全配置"
         Height          =   255
         Left            =   240
         TabIndex        =   9
         Top             =   1320
         Width           =   4095
      End
   End
   Begin VB.Frame Frame3
      Caption         =   "个性化设置"
      Height          =   1815
      Left            =   4920
      TabIndex        =   10
      Top             =   480
      Width           =   4575
      Begin VB.CheckBox Check_MyCptr
         Caption         =   "桌面此电脑图标"
         Height          =   255
         Left            =   240
         TabIndex        =   11
         Top             =   360
         Width           =   4095
      End
      Begin VB.CheckBox Check_TskBarSml
         Caption         =   "使用小按钮任务栏"
         Height          =   255
         Left            =   240
         TabIndex        =   12
         Top             =   680
         Width           =   4095
      End
      Begin VB.CheckBox Check_CnfrmDel
         Caption         =   "显示删除确认对话框"
         Height          =   255
         Left            =   240
         TabIndex        =   13
         Top             =   1000
         Width           =   4095
      End
      Begin VB.CheckBox Check_AudioSrv
         Caption         =   "启动音频服务"
         Height          =   255
         Left            =   240
         TabIndex        =   14
         Top             =   1320
         Width           =   4095
      End
   End
   Begin VB.Frame Frame2
      Caption         =   "启动项"
      Height          =   1215
      Left            =   240
      TabIndex        =   15
      Top             =   2400
      Width           =   4575
      Begin VB.CheckBox Check_SvrMng
         Caption         =   "登录不启动服务管理器"
         Height          =   255
         Left            =   240
         TabIndex        =   16
         Top             =   360
         Width           =   4095
      End
      Begin VB.CheckBox Check_Azure
         Caption         =   "禁止启动Azure Arc"
         Height          =   255
         Left            =   240
         TabIndex        =   17
         Top             =   720
         Width           =   4095
      End
   End
   Begin VB.Frame Frame1
      Caption         =   "账户策略"
      Height          =   1815
      Left            =   4920
      TabIndex        =   18
      Top             =   2400
      Width           =   4575
      Begin VB.CheckBox Check_PswdCmplx
         Caption         =   "禁用密码符合复杂性要求"
         Height          =   255
         Left            =   240
         TabIndex        =   19
         Top             =   360
         Width           =   4095
      End
      Begin VB.CheckBox Check_ShtdwnLogon
         Caption         =   "允许未登录时关机"
         Height          =   255
         Left            =   240
         TabIndex        =   20
         Top             =   680
         Width           =   4095
      End
      Begin VB.CheckBox Check_ShtdwnRsn
         Caption         =   "关闭显示事件跟踪程序"
         Height          =   255
         Left            =   240
         TabIndex        =   21
         Top             =   1000
         Width           =   4095
      End
      Begin VB.CheckBox Check_NOCAD
         Caption         =   "无需Ctrl+Alt+Del登录"
         Height          =   255
         Left            =   240
         TabIndex        =   22
         Top             =   1320
         Width           =   4095
      End
   End
   Begin VB.Frame Frame5
      Caption         =   "参数"
      Height          =   3015
      Left            =   240
      TabIndex        =   23
      Top             =   4320
      Width           =   9255
      Begin VB.Label Label8
         Caption         =   "注册表查询"
         Height          =   255
         Left            =   240
         TabIndex        =   24
         Top             =   360
         Width           =   1455
      End
      Begin VB.Label Label7
         Caption         =   "版本"
         Height          =   255
         Left            =   6240
         TabIndex        =   25
         Top             =   360
         Width           =   615
      End
      Begin VB.Label lblVer
         Caption         =   "Win一键优化"
         Height          =   255
         Left            =   6840
         TabIndex        =   26
         Top             =   360
         Width           =   2175
      End
      Begin VB.ComboBox Combo1
         Height          =   315
         Left            =   1680
         Style           =   2  'Dropdown List
         TabIndex        =   27
         Top             =   720
         Width           =   1215
      End
      Begin VB.ComboBox Combo2
         Height          =   315
         Left            =   3000
         TabIndex        =   28
         Top             =   720
         Width           =   6015
      End
      Begin VB.TextBox Text1
         Height          =   315
         Left            =   1680
         TabIndex        =   29
         Top             =   1140
         Width           =   7335
      End
      Begin VB.Label Label1
         Caption         =   "查询结果"
         Height          =   1215
         Left            =   240
         TabIndex        =   30
         Top             =   1620
         Width           =   8775
      End
   End
End
Attribute VB_Name = "Form_OPT"
Attribute VB_GlobalNameSpace = False
Attribute VB_Creatable = False
Attribute VB_PredeclaredId = True
Attribute VB_Exposed = False
Option Explicit

Private Const IE_ESC_ADMIN As String = "{A509B1A7-37EF-4b3f-8CFC-4F3A74704073}"
Private Const IE_ESC_USER As String = "{A509B1A8-37EF-4b3f-8CFC-4F3A74704073}"
Private Const CLSID_MYCOMPUTER As String = "{20D04FE0-3AEA-1069-A2D8-08002B30309D}"

Private Sub SetAllChecks(ByVal v As Integer)
    Check_CPU.Value = v
    Check_DEP.Value = v
    Check_LUA.Value = v
    Check_IE.Value = v
    Check_MyCptr.Value = v
    Check_TskBarSml.Value = v
    Check_CnfrmDel.Value = v
    Check_AudioSrv.Value = v
    Check_SvrMng.Value = v
    Check_Azure.Value = v
    Check_PswdCmplx.Value = v
    Check_ShtdwnLogon.Value = v
    Check_ShtdwnRsn.Value = v
    Check_NOCAD.Value = v
End Sub

Private Function OnOff(ByVal chk As CheckBox) As Boolean
    OnOff = (chk.Value = vbChecked)
End Function

Private Sub ApplyPasswordComplexity(ByVal disableComplexity As Boolean)
    Dim cfg As String
    Dim ps As String
    cfg = "C:\secpol.cfg"
    Call Cmd("secedit /export /cfg " & cfg)
    If disableComplexity Then
        ps = "$secpol = Get-Content '" & cfg & "'; $secpol = $secpol -replace 'PasswordComplexity = 1', 'PasswordComplexity = 0'; $secpol | Set-Content '" & cfg & "'; secedit /configure /db C:\Windows\security\local.sdb /cfg '" & cfg & "' /areas SECURITYPOLICY; Remove-Item '" & cfg & "' -Force"
    Else
        ps = "$secpol = Get-Content '" & cfg & "'; $secpol = $secpol -replace 'PasswordComplexity = 0', 'PasswordComplexity = 1'; $secpol | Set-Content '" & cfg & "'; secedit /configure /db C:\Windows\security\local.sdb /cfg '" & cfg & "' /areas SECURITYPOLICY; Remove-Item '" & cfg & "' -Force"
    End If
    Call ShellWait("powershell -Command ""& { " & ps & " }""")
End Sub

Private Sub RefreshChecks()
    Dim v As String
    Dim st As String
    Dim sm As String

    v = ReadRegistryValue("HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl\Win32PrioritySeparation")
    Check_CPU.Value = IIf(v = "38", vbChecked, vbUnchecked)

    v = ReadRegistryValue("HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management\DataExecutionPrevention_S4UEnable")
    Check_DEP.Value = IIf(v = "1", vbChecked, vbUnchecked)

    v = ReadRegistryValue("HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System\EnableLUA")
    Check_LUA.Value = IIf(v = "0", vbChecked, vbUnchecked)

    v = ReadRegistryValue("HKLM\SOFTWARE\Microsoft\Active Setup\Installed Components\" & IE_ESC_ADMIN & "\IsInstalled")
    Check_IE.Value = IIf(v = "0", vbChecked, vbUnchecked)

    v = ReadRegistryValue("HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel\" & CLSID_MYCOMPUTER, False)
    Check_MyCptr.Value = IIf(v = "0", vbChecked, vbUnchecked)

    v = ReadRegistryValue("HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\TaskbarSmallIcons", False)
    Check_TskBarSml.Value = IIf(v = "1", vbChecked, vbUnchecked)

    v = ReadRegistryValue("HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer\ConfirmFileDelete", False)
    Check_CnfrmDel.Value = IIf(v = "1", vbChecked, vbUnchecked)

    Call GetAudioSrvInfo(st, sm)
    Check_AudioSrv.Value = IIf(sm = "自动", vbChecked, vbUnchecked)

    v = ReadRegistryValue("HKLM\SOFTWARE\Microsoft\ServerManager\DoNotOpenServerManagerAtLogon")
    Check_SvrMng.Value = IIf(v = "1", vbChecked, vbUnchecked)

    v = ReadRegistryValue("HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Run\AzureArcSetup")
    Check_Azure.Value = IIf(v = "值不存在" Or Left$(v, 2) = "项不" Or Left$(v, 2) = "读取", vbChecked, vbUnchecked)

    Check_PswdCmplx.Value = IIf(ReadPasswordComplexityDisabled(), vbChecked, vbUnchecked)

    v = ReadRegistryValue("HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System\ShutdownWithoutLogon")
    Check_ShtdwnLogon.Value = IIf(v = "1", vbChecked, vbUnchecked)

    v = ReadRegistryValue("HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Reliability\ShutdownReasonOn")
    Check_ShtdwnRsn.Value = IIf(v = "0", vbChecked, vbUnchecked)

    v = ReadRegistryValue("HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System\DisableCAD")
    Check_NOCAD.Value = IIf(v = "1", vbChecked, vbUnchecked)
End Sub

Private Sub Form_Load()
    Combo1.Clear
    Combo1.AddItem "HKLM"
    Combo1.AddItem "HKCU"
    Combo1.ListIndex = 0

    Combo2.Clear
    Combo2.AddItem "HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl\Win32PrioritySeparation"
    Combo2.AddItem "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System\EnableLUA"
    Combo2.AddItem "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System\DisableCAD"
    Combo2.AddItem "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System\ShutdownWithoutLogon"
    Combo2.AddItem "HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Reliability\ShutdownReasonOn"
    Combo2.AddItem "HKLM\SOFTWARE\Microsoft\ServerManager\DoNotOpenServerManagerAtLogon"
    Combo2.AddItem "HKLM\SOFTWARE\Microsoft\ServerManager\DoNotPopWACConsoleAtSMLaunch"
    Combo2.AddItem "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Run\AzureArcSetup"
    Combo2.AddItem "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced\TaskbarSmallIcons"
    Combo2.AddItem "HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer\ConfirmFileDelete"
    Combo2.AddItem "HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel\{20D04FE0-3AEA-1069-A2D8-08002B30309D}"
    Combo2.AddItem "HKLM\SOFTWARE\Microsoft\Active Setup\Installed Components\{A509B1A7-37EF-4b3f-8CFC-4F3A74704073}\IsInstalled"
    Combo2.AddItem "HKLM\SOFTWARE\Microsoft\Active Setup\Installed Components\{A509B1A8-37EF-4b3f-8CFC-4F3A74704073}\IsInstalled"

    If Not IsWindowsServer() Then
        MsgBox "本软件面向 Windows Server。当前系统可能不是 Server 版本。", vbExclamation, "Win一键优化"
    End If
    Timer1.Enabled = True
End Sub

Private Sub Timer1_Timer()
    Timer1.Enabled = False
    Call RefreshChecks
End Sub

Private Sub Command1_Click()
    Call SetAllChecks(vbChecked)
End Sub

Private Sub Command2_Click()
    Call SetAllChecks(vbUnchecked)
End Sub

Private Sub cmdAbout_Click()
    MsgBox "Windows Server 日常使用优化工具。", vbInformation, "关于"
End Sub

Private Sub Combo2_Click()
    If Combo2.ListIndex >= 0 Then
        Text1.Text = Combo2.Text
        Call DoQuery
    End If
End Sub

Private Sub Text1_KeyPress(KeyAscii As Integer)
    If KeyAscii = 13 Then
        KeyAscii = 0
        Call DoQuery
    End If
End Sub

Private Sub DoQuery()
    Dim p As String
    p = Trim$(Text1.Text)
    If Len(p) = 0 Then
        Label1.Caption = "查询结果"
        Exit Sub
    End If
    Label1.Caption = "查询结果：" & ReadRegistryValue(p, True)
End Sub

Private Sub Command_WSOPT_Click()
    ' CPU：38=程序优先，2=后台服务（Server 默认倾向）
    If OnOff(Check_CPU) Then
        Call Cmd("REG ADD ""HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl"" /v Win32PrioritySeparation /t REG_DWORD /d 38 /f /reg:64 >nul 2>&1")
    Else
        Call Cmd("REG ADD ""HKLM\SYSTEM\CurrentControlSet\Control\PriorityControl"" /v Win32PrioritySeparation /t REG_DWORD /d 2 /f /reg:64 >nul 2>&1")
    End If

    If OnOff(Check_DEP) Then
        Call Cmd("REG ADD ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"" /v DataExecutionPrevention_S4UEnable /t REG_DWORD /d 1 /f /reg:64 >nul 2>&1")
    Else
        Call Cmd("REG ADD ""HKLM\SYSTEM\CurrentControlSet\Control\Session Manager\Memory Management"" /v DataExecutionPrevention_S4UEnable /t REG_DWORD /d 0 /f /reg:64 >nul 2>&1")
    End If

    If OnOff(Check_LUA) Then
        Call Cmd("reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"" /v ""EnableLUA"" /t REG_DWORD /d 0 /f /reg:64 >nul 2>&1")
    Else
        Call Cmd("reg add ""HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"" /v ""EnableLUA"" /t REG_DWORD /d 1 /f /reg:64 >nul 2>&1")
    End If

    If OnOff(Check_IE) Then
        Call Cmd("reg add ""HKLM\SOFTWARE\Microsoft\Active Setup\Installed Components\" & IE_ESC_ADMIN & """ /v ""IsInstalled"" /t REG_DWORD /d 0 /f /reg:64 >nul 2>&1")
        Call Cmd("reg add ""HKLM\SOFTWARE\Microsoft\Active Setup\Installed Components\" & IE_ESC_USER & """ /v ""IsInstalled"" /t REG_DWORD /d 0 /f /reg:64 >nul 2>&1")
    Else
        Call Cmd("reg add ""HKLM\SOFTWARE\Microsoft\Active Setup\Installed Components\" & IE_ESC_ADMIN & """ /v ""IsInstalled"" /t REG_DWORD /d 1 /f /reg:64 >nul 2>&1")
        Call Cmd("reg add ""HKLM\SOFTWARE\Microsoft\Active Setup\Installed Components\" & IE_ESC_USER & """ /v ""IsInstalled"" /t REG_DWORD /d 1 /f /reg:64 >nul 2>&1")
    End If

    If OnOff(Check_MyCptr) Then
        Call Cmd("REG ADD ""HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel"" /v """ & CLSID_MYCOMPUTER & """ /t REG_DWORD /d 0 /f >nul 2>&1")
    Else
        Call Cmd("REG ADD ""HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel"" /v """ & CLSID_MYCOMPUTER & """ /t REG_DWORD /d 1 /f >nul 2>&1")
    End If

    If OnOff(Check_TskBarSml) Then
        Call Cmd("REG ADD ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v ""TaskbarSmallIcons"" /t REG_DWORD /d 1 /f >nul 2>&1")
    Else
        Call Cmd("REG ADD ""HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"" /v ""TaskbarSmallIcons"" /t REG_DWORD /d 0 /f >nul 2>&1")
    End If

    If OnOff(Check_CnfrmDel) Then
        Call Cmd("REG ADD ""HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer"" /v ""ConfirmFileDelete"" /t REG_DWORD /d 1 /f >nul 2>&1")
    Else
        Call Cmd("REG ADD ""HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Explorer"" /v ""ConfirmFileDelete"" /t REG_DWORD /d 0 /f >nul 2>&1")
    End If

    If OnOff(Check_AudioSrv) Then
        Call Cmd("sc config AudioSrv start= auto")
        Call Cmd("sc config AudioEndpointBuilder start= auto")
        Call Cmd("sc start AudioSrv")
    Else
        Call Cmd("sc stop AudioSrv")
        Call Cmd("sc stop AudioEndpointBuilder")
        Call Cmd("sc config AudioSrv start= Disabled")
        Call Cmd("sc config AudioEndpointBuilder start= Disabled")
    End If

    If OnOff(Check_SvrMng) Then
        Call Cmd("reg add ""HKLM\SOFTWARE\Microsoft\ServerManager"" /v ""DoNotOpenServerManagerAtLogon"" /t REG_DWORD /d 1 /f /reg:64 >nul 2>&1")
        Call Cmd("reg add ""HKLM\SOFTWARE\Microsoft\ServerManager"" /v ""DoNotPopWACConsoleAtSMLaunch"" /t REG_DWORD /d 1 /f /reg:64 >nul 2>&1")
    Else
        Call Cmd("reg add ""HKLM\SOFTWARE\Microsoft\ServerManager"" /v ""DoNotOpenServerManagerAtLogon"" /t REG_DWORD /d 0 /f /reg:64 >nul 2>&1")
        Call Cmd("reg add ""HKLM\SOFTWARE\Microsoft\ServerManager"" /v ""DoNotPopWACConsoleAtSMLaunch"" /t REG_DWORD /d 0 /f /reg:64 >nul 2>&1")
    End If

    If OnOff(Check_Azure) Then
        Call Cmd("reg delete ""HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Run"" /v AzureArcSetup /f /reg:64 >nul 2>&1")
    Else
        Call Cmd("reg ADD ""HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Run"" /v ""AzureArcSetup"" /t REG_SZ /d ""%windir%\AzureArcSetup\Systray\AzureArcSysTray.exe"" /f  /reg:64 >nul 2>&1")
    End If

    Call ApplyPasswordComplexity(OnOff(Check_PswdCmplx))

    If OnOff(Check_ShtdwnLogon) Then
        Call Cmd("REG ADD ""HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"" /v ShutdownWithoutLogon /t REG_DWORD /d 1 /f /reg:64 >nul 2>&1")
    Else
        Call Cmd("REG ADD ""HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"" /v ShutdownWithoutLogon /t REG_DWORD /d 0 /f /reg:64 >nul 2>&1")
    End If

    If OnOff(Check_ShtdwnRsn) Then
        Call Cmd("REG ADD ""HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Reliability"" /v ShutdownReasonOn /t REG_DWORD /d 0 /f /reg:64 >nul 2>&1")
    Else
        Call Cmd("REG ADD ""HKLM\SOFTWARE\Policies\Microsoft\Windows NT\Reliability"" /v ShutdownReasonOn /t REG_DWORD /d 1 /f /reg:64 >nul 2>&1")
    End If

    If OnOff(Check_NOCAD) Then
        Call Cmd("REG ADD ""HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"" /v ""DisableCAD"" /t REG_DWORD /d 1 /f /reg:64 >nul 2>&1")
    Else
        Call Cmd("REG ADD ""HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System"" /v ""DisableCAD"" /t REG_DWORD /d 0 /f /reg:64 >nul 2>&1")
    End If

    Call RefreshChecks
End Sub
