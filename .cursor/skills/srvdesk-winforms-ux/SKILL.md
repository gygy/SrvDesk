---
name: srvdesk-winforms-ux
description: >-
  审计或修改 SrvDesk WinForms 界面时，按仓库 UX 规范检查颜色、按钮、裁字（含多显示器 DPI）、
  顶/底栏、状态条、对话框壳、布局贴边/溢出与反馈。触发：WinForms UX、界面规范、按钮裁字、
  副屏、SrvDesk UI 审计、窗口太小、贴左、勾选框竖线。
---

# SrvDesk WinForms UX

本技能对接仓库强制约定；**不要**套用 Web landing / frontend-design。  
可借鉴的通用 UX（来自 ui-ux-pro-max 原则，已改写为 WinForms）：对比度、可见焦点、加载反馈、防溢出裁切、稳定布局、标签可见、危险操作确认。

## 何时用

- 新建/改 `*Dialog.cs`、`MainForm` 相关 UI
- 用户抱怨：裁字、贴左、窗口太小、勾选框只剩竖线、按钮压扁、风格不统一
- 要求「按规范改界面」或对照检查清单审计

## 必读

1. `.cursor/rules/winforms-ux.mdc`
2. `docs/winforms-ux-checklist.md`
3. 代码：`AppTheme` / `UiFit` / `UiScale` / `ThemedSettingsChrome`

## 流程

1. 确认是 **WinForms**（禁止 Web 英雄区/紫渐变/大圆角卡片堆）
2. 对照检查清单审计目标窗体
3. 最小 diff：`CreateButton`（`FlatChromeButton`）、`ControlHeight`/`FitButton`、`AppTheme`、`UiScale.S`；底栏高度随按钮伸缩
4. **顶栏与底栏一起查**；有 Padding 的容器优先 **Dock / TableLayout / CreateToggleStack**，勿 `Location(0,y)` 贴左缘
5. 可缩放窗：`ClientSize` / `MinimumSize` 按 **FitButton 后实测** 留足宽度（多钮一行至少约 `UiScale.Size(780+, 520+)`）
6. 交付：`.\scripts\publish.ps1`（升版本 + Obfuscar；勿默认 `-NoBumpVersion` / `-SkipObfuscate`）

## 从通用 UX 借鉴（WinForms 落地）

| 原则 | SrvDesk 做法 |
|------|----------------|
| 对比度可读 | 正文 `TextMain`、提示 `TextMute`、主钮 `TextOnPrimary`；禁止灰字压灰底 |
| 焦点可见 | 保留 Tab 序；模态设 `AcceptButton`/`CancelButton`；勿让 Dock 底栏盖住聚焦控件 |
| 加载反馈 | 长操作 `UseWaitCursor` / 禁用主钮 / 进度或状态文案；禁止静默卡住 |
| 错误可恢复 | `MessageBox` + 短原因；状态栏一句失败；危险项二次确认 |
| 防溢出裁切 | 内容超界用滚动或加大窗；**禁止**靠裁切/竖压文字「塞进」矮行 |
| 稳定布局 | 异步改状态前预留行高/状态区，避免列表跳动（如检测中…→已安装） |
| 标签可见 | 字段用明确 Label，勿仅靠 Placeholder；图标钮要有 `ToolTip` |
| 截断策略 | **标签/列表名**可用 `SingleLineLabel`+省略；**按钮文案禁止**用省略号糊弄 |
| 间距与命中 | 同排钮间距约 `UiScale.S(8)`；行高 ≥ `ControlHeight + 边距`（勾选框勿 16×16 硬塞进矮行） |
| 导航当前态 | 侧栏选中用现有 `NavMenuStyle`；勿另起一套高亮 |

**明确不借鉴**：移动端 44pt 触摸规范整套照搬、Web `overflow-hidden`、Landing 首屏、暗色 OLED 炫光、GSAP/动画堆砌。

## 布局铁律（近年翻车点）

1. **Padding 只作用于 Dock/布局引擎子控件**；`Location = Point(0,y)` **忽略** Padding → 内容贴左。改法：内层 `Dock=Fill`，或 `CreateToggleStack` + `StretchStackChildren`。
2. **窗口宽度 ≥ 一行 FitButton 之和 + 边距**；配置脚本/安全中心等曾因 640 宽裁掉「关闭」「批处理」。
3. **列表行高随 `ControlHeight`**：写死 `44` 会竖直裁切 Flat 钮字与勾选框（只剩竖线）。
4. **表头列位与行控件共用常量**（见 `CommonSoftwareRow.SelectColX` 等），禁止表头写死、行内另算。
5. 浮动 `Anchor=Bottom|Right` 叠在 `Dock.Fill` 列表上 → 改 `Dock.Bottom` 动作条。

## 不要做

- 第二套色板 / 默认 3D 按钮混排；状态条扮主色实心按钮
- Web 风大英雄区、紫渐变、奶油衬线
- **画蛇添足**：用途横幅、「提示：」科普、流程条、footerHint 说教、实现方式/注册表路径长文
- **按钮裁字**：裸 `new Button`+写死高；`Height=40` 条带；`CreateButton` 后再 `Size=(140,34)`；底栏写死 `58`；只修底栏不修顶栏；Paint 盖字却不关默认绘制
- 省略号代替加宽按钮；动作条盖住「共 N 项」

## 优化顾问（产品约束）

- OS 感知推荐；仅未达 + 强烈推荐/推荐；无流程条；静默应用到系统；勿加回「①…⑥」

## 交付前速检

- [ ] AppTheme / CreateButton / ControlHeight·FitButton / 顶底同规则
- [ ] 无贴左（无 Padding+绝对定位）；窗体 MinimumSize 够宽
- [ ] 列表行高够；勾选框完整；按钮字不竖压
- [ ] 加载/错误有反馈；危险有确认
- [ ] footerHint 空（无说教）；`publish.ps1` 出包
