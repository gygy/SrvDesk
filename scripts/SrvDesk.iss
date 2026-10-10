; SrvDesk installer — wraps the already obfuscated dist\SrvDesk.exe
; Built by scripts\publish.ps1 via ISCC.exe. Do not ship a non-obfuscated payload.

#ifndef MyAppVersion
  #define MyAppVersion "0.0.0"
#endif
#ifndef MyAppSource
  #define MyAppSource "..\dist\SrvDesk.exe"
#endif
#ifndef MyAppIcon
  #define MyAppIcon "..\src\SrvDesk\app.ico"
#endif
#ifndef MyOutputDir
  #define MyOutputDir "..\dist"
#endif
#ifndef MyOutputName
  #define MyOutputName "SrvDesk-Setup"
#endif

[Setup]
AppId={{A8C3E2F1-7B4D-4E9A-9C2F-1D6E8B0A4C72}}
AppName=SrvDesk
AppVersion={#MyAppVersion}
AppVerName=SrvDesk {#MyAppVersion}
AppPublisher=SrvDesk
AppPublisherURL=https://github.com/gygy/SrvDesk
AppSupportURL=https://github.com/gygy/SrvDesk/releases
DefaultDirName={autopf}\SrvDesk
DefaultGroupName=SrvDesk
DisableProgramGroupPage=yes
LicenseFile=..\LICENSE
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir={#MyOutputDir}
OutputBaseFilename={#MyOutputName}
SetupIconFile={#MyAppIcon}
UninstallDisplayIcon={app}\SrvDesk.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
VersionInfoVersion={#MyAppVersion}.0
VersionInfoCompany=SrvDesk
VersionInfoProductName=SrvDesk
VersionInfoProductVersion={#MyAppVersion}
MinVersion=6.1sp1

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
; checkedonce: default create desktop shortcut on first install
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "附加图标:"; Flags: checkedonce

[Files]
Source: "{#MyAppSource}"; DestDir: "{app}"; DestName: "SrvDesk.exe"; Flags: ignoreversion

[Icons]
Name: "{group}\SrvDesk"; Filename: "{app}\SrvDesk.exe"; Comment: "Windows 系统优化助手"
Name: "{autodesktop}\SrvDesk"; Filename: "{app}\SrvDesk.exe"; Comment: "Windows 系统优化助手"; Tasks: desktopicon

[Run]
Filename: "{app}\SrvDesk.exe"; Description: "立即运行 SrvDesk"; Flags: nowait postinstall skipifsilent shellexec

[UninstallDelete]
Type: filesandordirs; Name: "{app}"
