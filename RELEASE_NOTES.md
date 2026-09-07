# SrvDesk v1.0.21

## 中文

- 启动时「设置操作」同步读取本机实际状态，不再先显示推荐值
- 「系统当前值」与「设置操作」一致（开关：开启/关闭；下拉：当前选项）
- 修复任务栏自动隐藏开关无效（按系统标志位写入并立即通知外壳）
- RDP 改为下拉；「启用远程桌面」归入「远程与网络」

下载：单文件 `SrvDesk.exe`，请以管理员身份运行。

## English

- Setting controls now load live system state on startup (not recommended defaults)
- Current-value column matches the control (on/off or selected option)
- Fix taskbar auto-hide not applying (APPBARDATA + StuckRects bits)
- RDP items are dropdowns; Enable RDP lives under Remote & Network

Download `SrvDesk.exe` and run as Administrator.
