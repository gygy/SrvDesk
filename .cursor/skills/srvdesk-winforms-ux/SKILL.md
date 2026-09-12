---
name: srvdesk-winforms-ux
description: >-
  审计或修改 SrvDesk WinForms 界面时，按仓库 UX 规范检查颜色、按钮、裁字、状态条与对话框壳。
  触发：WinForms UX、界面规范、对话框不好看、按钮不统一、裁字、SrvDesk UI 审计。
---

# SrvDesk WinForms UX

## 何时用

- 新建/改 `*Dialog.cs`、`MainForm` 相关 UI
- 用户抱怨按钮、状态条、裁字、风格不统一
- 要求「按规范改界面」

## 必读

1. `.cursor/rules/winforms-ux.mdc`
2. `docs/winforms-ux-checklist.md`
3. 代码：`AppTheme` / `UiFit` / `ThemedSettingsChrome`

## 流程

1. 确认是 WinForms（不要套 Web frontend-design）
2. 对照检查清单审计目标窗体
3. 最小 diff 改为使用 `CreateButton`、`ControlHeight`、`AppTheme`
4. 给用户试或交付时：按 `compile-dist` 跑 `.\scripts\publish.ps1`（**升版本 + 轻度混淆**，勿加 `-NoBumpVersion` / `-SkipObfuscate`）

## 不要做

- 引入第二套色板或默认 3D 按钮混排
- 把状态 Label 做成实心主色「假按钮」
- 为桌面工具堆 Web 风大英雄区/紫渐变
- **画蛇添足**：重复用途说明、「提示：」科普、同一页反复解析、优化顾问流程条
- **底栏左侧默认说明**：勾选/按钮已能表达时 `MountModal(..., footerHint: "")`，勿写「默认：不强制下次改密 · …」类注脚
- **实现方式 / 技术路径横幅**：如 Sysinternals、LSA 机密、注册表路径、对齐全套工具名；填表页只留字段与短操作提示
- **术语堆砌进主界面**：LSA / secedit / .reg 细节放帮助面板，不放页顶长文
- 常态占位的科普；仅缺依赖时给一句（如未装 wt.exe）

## 优化顾问

- 按本机真实状态 + **OS 识别**（Win10/11、Server 2016–2025、Server Core、虚拟机）解析有效推荐强度
- 四级：★★★★★ 强烈推荐 · ★★★★ 推荐 · ★★ 高级 · 不推荐
- 仅列出**未达推荐**且 **强烈推荐 / 推荐** 的项；高级与不推荐不进顾问核心
- 安全缓解关闭（VBS/HVCI/Spectre 等）、激进性能项默认「不推荐/高级」，勿当一键优化
- 按侧栏标签页归类；分类可折叠；行勾选 + 组全选 + 底栏「设为推荐值 / 应用到系统」
- **不要**「本组设为推荐」
- 「应用到系统」：无还原点/变更计划弹窗，勾选（或未勾则全部）立即写入并刷新
- 服务：设推荐即改启动类型；开关随「应用到系统」静默写入
- Server Core 自动隐藏桌面体验类项
- 不要再加「①环境识别 → … → ⑥持续巡检」流程条
- 仍禁止：用途横幅、洞察说教、堆无关入口按钮
