# Windows server优化助手 SrvDesk

> 原名 **Win一键优化**。面向 **Windows Server 桌面化** 的一键注册表 / 服务 / DISM 优化工具。

当前版本：**1.0.1**

[English](README.md)

---

## SrvDesk 是什么？

SrvDesk 帮助把 **Windows Server 2022/2025** 配置成适合日常桌面使用的环境：资源管理器、RDP、DNS、启动项、隐私与性能等，支持 **即时开关** 与 **批量预设** 两种方式。

**适用场景**

- 新装 Server 后快速「桌面化」
- 远程办公 RDP 调优
- 多台机器用 JSON 配置复用同一套设置

**系统要求**

- Windows Server 2016+（推荐 2022/2025，带桌面体验）
- 也支持 Windows 10/11（部分「Server 专属」项会自动隐藏）
- **.NET Framework 4.8**（Server 2022/2025 通常已内置）
- **必须以管理员身份运行**

---

## 下载与安装

1. 打开 [Releases](https://github.com/gygy/SrvDesk/releases) 下载最新 **`SrvDesk.exe`**（单文件，无需安装）。
2. 放到任意目录（建议路径不含特殊字符）。
3. **右键 → 以管理员身份运行**。
4. 若 SmartScreen 提示，选「仍要运行」（开源未签名 exe 常见现象）。

### 从源码编译

完整源码在私有 Gitea 仓库维护。GitHub 公开仓库仅含文档与 Release，不含 `src/`。

若你有源码访问权限：

```powershell
git clone ssh://git@你的Gitea地址/sheng/win-yijian-youhua.git
cd win-yijian-youhua
.\scripts\publish.ps1
# 输出：dist\SrvDesk.exe
```

需要本机已安装 [.NET SDK](https://dotnet.microsoft.com/download)（用于编译 net48 项目）。

---

## 快速上手（5 分钟）

### 第一次使用推荐流程

1. **以管理员身份** 启动 `SrvDesk.exe`。
2. 顶部菜单 **预设 → Server 桌面（推荐）**，再点 **载入当前所选预设**。
3. 浏览左侧列表，确认开关状态（右侧帮助面板可看每项说明）。
4. 点击底部 **「应用推荐」** 写入系统。
5. 按提示 **重启**（DISM、部分服务项需重启后完全生效）。

### 界面说明

| 区域 | 说明 |
|------|------|
| 左侧菜单 | 分类：即时页 + 批量分组 |
| 中间列表 | 开关=推荐值；关=恢复系统默认 |
| 右侧帮助 | 点击项目或 ⓘ 查看详细说明；**F1** 打开完整使用说明 |
| 底部 | 应用 / 恢复 / 状态栏 |

---

## 左侧菜单说明

### 即时页（改完即生效，无需点「应用推荐」）

| 菜单 | 功能 |
|------|------|
| **资源管理器** | 显示隐藏文件、快速访问、任务栏、Win11 资源管理器等 |
| **电源与服务** | 远程桌面、休眠、关键服务、右键菜单工具等 |
| **登录启动项** | 启动项管理、Autologon 相关 |
| **DNS 设置** | 按网卡切换 DNS（如 223.5.5.5 / 8.8.8.8） |

### 批量分组（需点「应用推荐」）

| 菜单 | 典型内容 |
|------|----------|
| **性能及安全** | 遥测、更新、TCP/BBR、UAC、Defender 相关等 |
| **桌面外观** | 主题、任务栏、搜索、SmartScreen 等 |
| **远程与网络** | RDP 帧率/GPU/NLA、网络发现等 |
| **隐私与体验** | Copilot、活动历史、广告 ID 等 |
| **系统组件** | DISM 可选功能 / Capabilities |
| **账户策略** | 密码策略、自动登录、Ctrl+Alt+Del 等 |

---

## 预设方案

菜单 **预设** 提供四套方案（对标 WinUtil / 社区 Server 桌面帖）：

| 预设 | 适用 |
|------|------|
| **Server 桌面（推荐）** | 新装 Server 当日常桌面，默认首选 |
| **安全加固** | 保留 UAC/NLA，关闭 SMB1、遥测等 |
| **远程办公** | RDP 高帧率 + GPU，高性能电源 |
| **最小改动** | 只动 Server 专属与账户便利项 |

载入预设后仍可逐项微调，再点 **应用推荐**。

---

## 配置导入 / 导出

- **文件 → 导出配置**：保存当前界面状态为 `SrvDesk-配置.json`
- **文件 → 导入配置**：在另一台机器复用同一套开关组合

> 导入的是「要应用哪些项」，不是完整系统备份；应用前请确认目标机器环境相近。

---

## 常用工具（顶部「工具」菜单）

- 计算机名 / 工作组、Autologon、系统信息
- 编辑 hosts、组策略、事件查看器
- 垃圾清理、桌面维护、Windows 可选功能
- 右键菜单扩展（取得所有权、在此处打开 Terminal 等）
- **快速工具**：计算机管理、计划任务、PowerShell 等
- **帮助 → 打开操作日志**：查看 `%LocalAppData%\WinOpt\apply.log`

---

## 命令行（CLI）

需 **管理员** 命令提示符或 PowerShell：

```text
SrvDesk.exe --help
SrvDesk.exe --apply-preset server-desktop
SrvDesk.exe --apply-preset security
SrvDesk.exe --load-profile D:\SrvDesk-配置.json
SrvDesk.exe --export-profile D:\current.json
```

预设 ID：`server-desktop` | `security` | `remote-work` | `minimal`

---

## 注意事项

1. **务必管理员运行**，否则注册表 / 服务 / DISM 会失败。
2. **Server Core**（无桌面体验）下，勾选 **视图 → 隐藏不适用项**。
3. 修改前建议在虚拟机或测试机验证；重要生产环境请先导出配置。
4. 本工具直接修改系统设置，**无自动完整回滚**；可用 **恢复** 按钮或导入旧 JSON 尽量还原。
5. 操作日志：`%LocalAppData%\WinOpt\apply.log`

---

## 常见问题

**Q：双击 exe 没反应？**  
A：请右键「以管理员身份运行」；若仍失败，在管理员 CMD 中运行 `SrvDesk.exe` 查看错误。

**Q：和 SophiApp / WinUtil 有什么区别？**  
A：SrvDesk 专注 **Windows Server 桌面化**，含即时页（资源管理器、DNS、启动项）和 Server DISM 项，单 exe、无安装。

**Q：Win10/11 能用吗？**  
A：可以，但「Server 专属」项会自动标记或隐藏。

---

## 许可证

[MIT License](LICENSE)

## 反馈

在 [GitHub Issues](https://github.com/gygy/SrvDesk/issues) 提交问题或建议。
