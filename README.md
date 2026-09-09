# Windows server优化助手 SrvDesk

面向 **Windows Server 桌面化** 的本机优化工具：注册表 / 服务 / 策略 / DISM，支持即时页与批量开关。

当前版本：**1.0.30** · [Releases 下载](https://github.com/gygy/SrvDesk/releases)

[English](README_en.md)

---

## 下载与运行

1. 打开 [Releases](https://github.com/gygy/SrvDesk/releases) 下载 **`SrvDesk.exe`**（单文件）。
2. **右键 → 以管理员身份运行**。
3. 首次打开会有一次简短说明；完整条款见 **帮助 → 免责声明 / 隐私说明 / 许可证**。

**系统要求：** Windows Server 2016+（推荐 2022/2025，带桌面体验）；也可用在 Windows 10/11（「Server专属」项会隐藏或不适用）。需 **.NET Framework 4.8**。

> 本仓库只放说明与许可证，**不含源码**。程序只从 Releases 获取。

---

## 快速上手

1. 管理员启动 `SrvDesk.exe`。
2. 顶部 **预设 → Server 桌面（推荐）** → 载入。
3. 左侧切换分类，核对开关（开=推荐值，关=尽量恢复默认）。
4. 底部点 **「应用推荐」**（即时页改完即生效，不必点应用）。
5. 按提示重启（部分 DISM / 服务项需要）。

顶栏可看本机 **IPv4** 以及 **CPU**、**内存**。可用搜索框与「分类」筛选；勾选「隐藏不适用项」可收起当前系统不支持的开关。

---

## 左侧菜单

### 即时页（改完即生效）

| 菜单 | 说明 |
|------|------|
| **登录启动项** | 启动项管理、与自动登录相关的入口 |
| **DNS 设置** | 按网卡切换 DNS |
| **自定义配置** | 自建 .reg / .cmd / .ps1 方案（保存在本机） |

### 批量分组（需「应用推荐」）

组内可再折叠分区，例如「性能及安全」「隐私与体验」已按场景拆开。

| 菜单 | 内容概要 |
|------|----------|
| **Server专属** | Server Manager / WAC / Azure 相关提示、媒体功能、精简组件等 |
| **账户策略** | 自动登录、密码策略、关机界面、键盘过滤等 |
| **资源管理器** | 显示扩展名/隐藏文件、快速访问、Win11 布局、任务栏相关 |
| **桌面外观** | 桌面图标、任务栏、主题与搜索、SmartScreen、右键项 |
| **远程与网络** | RDP GPU/帧率/NLA、网络发现等 |
| **隐私与体验** | 广告与推荐、搜索与助手、隐私数据、微软拼音、界面体验等 |
| **性能及安全** | 常用开关、性能加速、Windows 更新、网络、遥测、安全服务等 |
| **电源与服务** | 远程桌面、休眠/快速启动、后台服务与内存相关 |

右侧可查看每项说明，并可复制/保存开启与关闭脚本（**视图 → 显示配置脚本**）。

---

## 预设

| 预设 | 适用 |
|------|------|
| **Server 桌面（推荐）** | 新装 Server 当日常桌面 |
| **安全加固** | 保留更多安全项，关掉 SMB1、遥测等 |
| **远程办公** | 偏 RDP 与性能 |
| **最小改动** | 只动少量便利项 |

载入后仍可逐项改，再点应用。

---

## 配置导入 / 导出

- **文件 → 导出配置**：当前勾选状态 → JSON  
- **文件 → 导入配置**：在另一台机器复用  

导入的是「要改哪些项」，不是整机备份。

---

## 工具菜单（常用）

- 计算机名 / 工作组、Autologon、系统信息  
- 编辑 hosts、刷新 DNS、组策略、事件查看器  
- **常用软件**（含 **AI**：Codex CLI / Codex Desktop、Pi Agent 等，走 winget 或离线方式）  
- **垃圾清理**（缓存 / 系统残留 / 临时文件，可勾选、带进度；对照 ZyperWin++ 项）  
- 桌面维护、高级设置  
- 可选功能 / Capabilities、安全中心、MSEdge 管理、右键菜单  
- 快速工具、刷新状态（F5）、恢复出厂默认  

---

## 帮助菜单

- **检查更新**（读 GitHub Releases，下载 `SrvDesk.exe` 后自动替换并重启；启动时也会检查，可在 **文件 → 程序设置** 关闭）  
- 变更日志、操作日志（`%LocalAppData%\SrvDesk\`）  
- 免责声明、隐私说明、许可证  
- 支持（作者与反馈地址）  

---

## 命令行（需管理员）

```text
SrvDesk.exe --help
SrvDesk.exe --apply-preset server-desktop
SrvDesk.exe --apply-preset security
SrvDesk.exe --load-profile D:\SrvDesk-配置.json
SrvDesk.exe --export-profile D:\current.json
```

预设 ID：`server-desktop` | `security` | `remote-work` | `minimal`

---

## 注意

1. 必须管理员运行，否则多数写入会失败。  
2. Server Core 建议勾选 **视图 → 隐藏不适用项**。  
3. 重要环境先备份 / 还原点 / 虚拟机快照；可先导出 JSON。  
4. 没有完整自动回滚；可用「恢复」或旧配置尽量还原。  
5. 暂停更新、关闭安全相关项等请自行评估风险。  

---

## 常见问题

**双击没反应？** 右键以管理员运行；仍不行就在管理员 CMD 里运行看报错。  

**和 WinUtil / SophiApp 区别？** 更偏向 Server 桌面化：即时页（启动项、DNS 与自定义配置）+ Server 相关项，单 exe。  

**Win10/11？** 可用；Server 专属项会隐藏或不适用。  

---

## 许可证 · 免责 · 隐私

- [MIT License](LICENSE)  
- [免责声明](DISCLAIMER.md)  
- [隐私说明](PRIVACY.md)  

问题反馈：[GitHub Issues](https://github.com/gygy/SrvDesk/issues)
