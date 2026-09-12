# SrvDesk

**Windows 系统优化与桌面体验助手**

[English](README_en.md) · [中文](README.md)

SrvDesk 是一款面向 **Windows 10、Windows 11 及 Windows Server 2019/2022/2025** 的系统优化工具。

通过系统检测、优化建议、服务管理、组件管理、性能调优等能力，让 Windows Server 更适合个人桌面使用，同时也为 Windows 10/11 提供安全、稳定、可恢复的系统优化能力。

当前版本：**1.0.66** · [Releases 下载](https://github.com/gygy/SrvDesk/releases)

---

## 下载与运行

1. 打开 [Releases](https://github.com/gygy/SrvDesk/releases)，下载 **`SrvDesk.exe`**（单文件，轻度混淆）。
2. 右键 → **以管理员身份运行**。
3. 免责声明、隐私说明、许可证在程序 **帮助** 菜单。

**系统要求：** Windows 10 / 11；Windows Server 2019 / 2022 / 2025（建议带桌面体验）。需要 **.NET Framework 4.8**。

> 本公开仓库仅含说明与许可证，**不含源码**。程序请从 Releases 下载。

---

## ✨ 核心功能

### 1. 系统优化

提供常用系统设置及性能优化：

* 电源计划
* 视觉效果
* CPU 性能
* GPU 设置
* 内存管理
* NTFS 优化
* 网络优化
* 系统后台优化
* Windows Update 设置

### 2. 服务管理

对 Windows 服务进行分类和风险识别：

* 推荐保留
* 建议优化
* 按需启用
* 可安全禁用
* 不建议修改

支持查看：

* 服务用途
* 当前状态
* 启动类型
* 优化建议
* 影响范围
* 恢复方式

### 3. 系统组件管理

管理 Windows Features、Server Roles 等系统组件：

* Hyper-V
* Containers
* IIS
* NFS
* SNMP
* Print Services
* Remote Desktop Services
* Windows Sandbox
* SMB 1.0
* 其他可选功能

根据实际使用场景进行按需启用或关闭。

### 4. 启动与后台管理

管理系统启动项及后台任务：

* 开机启动项
* 后台程序
* 计划任务
* 无用 Updater
* 软件助手
* 第三方后台服务

减少不必要的资源占用。

### 5. Windows Server 桌面化

针对 Windows Server 作为个人电脑使用的场景进行专门优化：

* Desktop Experience
* Server Manager
* IE Enhanced Security Configuration
* Windows Search
* 桌面视觉效果
* 多媒体相关组件
* 常用桌面体验设置

让 Windows Server 更接近普通 Windows 桌面使用体验。

### 6. 安全优化

在保证系统安全的前提下进行优化：

* Microsoft Defender
* Windows Firewall
* UAC
* Windows Update
* SmartScreen
* 安全策略

**不以关闭安全功能换取性能。**

### 7. 系统检测

自动检测当前系统状态：

* 操作系统版本
* 硬件配置
* CPU / GPU
* 内存
* 磁盘
* 网络
* 电源计划
* 服务状态
* 启动项
* 系统组件
* 安全配置

根据检测结果生成优化建议。

### 8. 一键优化

支持按照优化等级批量执行：

| 等级 | 说明 |
| --- | --- |
| 🟢 推荐 | 安全、稳定，建议执行 |
| 🔵 性能 | 优先改善系统性能 |
| 🟡 按需 | 根据使用场景选择 |
| 🔴 高级 | 面向高级用户的深度调优 |

---

## 🛡️ 优化原则

SrvDesk 不追求“关闭得越多越快”，而是坚持：

> **安全优先 · 稳定优先 · 性能适度 · 按需优化 · 可恢复**

每个优化项尽可能明确：

* 当前状态
* 推荐状态
* 优化作用
* 潜在影响
* 风险等级
* 是否需要重启
* 是否支持恢复

避免无依据的：

* 极限注册表优化
* 大量禁用系统服务
* 关闭 Defender
* 关闭 Windows Firewall
* 永久关闭 Windows Update
* 无依据的 TCP 参数修改

---

## 💻 支持系统

### Windows Desktop

* Windows 10
* Windows 11

### Windows Server

* Windows Server 2019
* Windows Server 2022
* Windows Server 2025

SrvDesk 会根据不同 Windows 版本的系统能力和配置差异，自动匹配对应的优化项。

---

## 🎯 适用场景

### 普通个人电脑

优化：

* 系统响应速度
* 开机启动
* 后台程序
* 电源
* 视觉效果
* 存储
* 网络

### Windows Server 个人桌面

将 Windows Server 用作：

* 个人电脑
* 工作站
* 开发环境
* NAS
* 下载服务器
* Docker 主机
* 虚拟化主机
* 家庭服务器

### 高级用户

提供：

* 服务级优化
* 系统组件管理
* 网络高级调优
* CPU / GPU 调优
* 内存及存储调优
* 高级系统参数配置

---

## 🔄 可恢复

所有重要优化操作应尽可能提供恢复能力。

优化前：

1. 检测当前配置
2. 保存原始状态
3. 执行优化
4. 验证结果
5. 出现问题时恢复

**不建议无法解释、无法恢复的“一键极限优化”。**

---

## 📋 产品定位

SrvDesk 不是简单的“系统清理工具”，而是：

> **Windows 系统检测 + 优化建议 + 配置管理 + 性能调优 + 桌面体验优化助手**

目标是让用户：

**看得懂 → 选得明白 → 优化安全 → 出问题可恢复。**

---

## ✅ 干净承诺

**保证干净：无任何广告；无任何病毒；无任何私活。包括后续任何更新。**

SrvDesk 仅做本机系统检测与配置优化，不捆绑推广、不植入恶意代码、不以软件名义夹带其他用途。

---

## 📌 版本规划

后续持续完善：

* 更多 Windows 优化项
* Windows 版本差异识别
* 服务智能推荐
* 优化前后对比
* 配置备份与恢复
* 性能基准测试
* 网络优化
* 存储优化
* 游戏优化
* 开发环境优化
* Server 桌面化优化
* 自动化优化策略

---

## 免责声明（摘要）

改系统设置有风险。请先在测试机验证；生产环境务必备份。因使用本软件造成的数据丢失或业务中断，作者不承担责任。完整条款见程序内免责声明与许可证。

---

## License

本项目仅用于系统管理、配置优化和个人电脑/服务器维护。

执行高级系统优化前，请确认了解相关配置可能产生的影响，并建议提前做好系统及重要数据备份。
