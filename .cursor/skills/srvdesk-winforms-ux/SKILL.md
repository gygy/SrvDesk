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
4. 本地验证后按 `compile-dist` 规则 `publish.ps1`（需交付时）

## 不要做

- 引入第二套色板或默认 3D 按钮混排
- 把状态 Label 做成实心主色「假按钮」
- 为桌面工具堆 Web 风大英雄区/紫渐变
- **画蛇添足**：重复用途说明、「提示：」科普、同一页反复解析、优化顾问流程条

## 优化顾问

- 按侧栏标签页归类展示「未达推荐」项；分类可折叠
- 行勾选 + 组全选/本组设为推荐 + 底栏「设为推荐值 / 应用到系统」
- 开关：设推荐只改主界面勾选；应用到系统走变更计划
- 服务：设推荐即改启动类型（与服务页一致）
- 不要再加「①环境识别 → … → ⑥持续巡检」流程条
- 仍禁止：用途横幅、洞察说教、堆无关入口按钮
