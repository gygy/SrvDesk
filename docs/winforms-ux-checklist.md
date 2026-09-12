# SrvDesk WinForms UX 检查清单

对照 `.cursor/rules/winforms-ux.mdc`。审计某个窗体时逐项打勾。

## 文案

- [ ] 无流程条 / 步骤口号
- [ ] 无与按钮重复的摘要横幅
- [ ] 无「提示：」科普注脚（列表「建议」列可保留短句）
- [ ] 无底栏左侧「默认：…」等复述勾选项的说明（`footerHint` 可空）
- [ ] 无「危险组件会二次确认 / SMBv1 建议卸载 / 完成后建议重启」类操作脚注
- [ ] 无「实现方式 / 仅建议 / 对齐某某」页顶长说明（如 Autologon·LSA 科普）
- [ ] 无常态注册表/路径说教；缺依赖时才给短提示
- [ ] 删掉后仍能完成当前页任务；语气短、可执行

## 窗体

- [ ] `AppBrand.ApplyWindowIcon(this)`
- [ ] 标题 `AppLang.L`
- [ ] 底色 `AppTheme.Surface` 或 `SurfaceCard`
- [ ] 复杂布局 `AutoScaleMode.None`
- [ ] 可缩放窗有合理 `MinimumSize`

## 颜色 / 字体

- [ ] 无随机硬编码主色（品牌色只在 `BrandPalette`）
- [ ] 正文 `UiFit.UiFont`，提示 `TextMute`
- [ ] 固定像素经 `UiScale.S`

## 按钮与顶/底栏（防裁字）

- [ ] `ThemedSettingsChrome.CreateButton`（禁止裸 `new Button` + 手写宽高）
- [ ] 高度用 `UiFit.ControlHeight` / `FitButton`，**禁止写死 30/36/40 条带塞按钮**
- [ ] **禁止** `CreateButton` 后再 `_btn.Size = new Size(140, 34)` 压窄压矮
- [ ] 按钮为 `FlatChromeButton`（`CreateButton`）；完整显示文案（不用省略号糊弄）
- [ ] 底栏高度随 `FitButton` 伸缩（禁止写死 `Height = 58`）
- [ ] **顶栏与底栏同一套规则**（不只修底栏）
- [ ] 工具条容器够高，不裁按钮顶/字脚；动作条不盖住下方状态行
- [ ] 换屏：Mount 壳或自有 `DpiChanged`/`Layout` 会重算
- [ ] 主按钮不超过一条主路径抢视线
- [ ] 同排按钮同高，间距统一
- [ ] 状态条 ≠ 第二主按钮（浅底边框 / Pale，非 PrimaryDeep 实心块）
- [ ] `footerHint: ""`（无说教脚注）
## 列表与工具条

- [ ] ListView `SurfaceCard` + 双缓冲（`UiBuffer`）
- [ ] 列宽 fit，无多余横/竖滚动条
- [ ] 单行工具条 `ConfigureNoScrollRow`

## 文案与风险

- [ ] 用户串双语
- [ ] 危险操作有确认 / 等级拦截
- [ ] 建议含原因或可点到说明（服务页等）

## 参考实现

- 壳与按钮：`ThemedSettingsChrome.cs`（`CreateButton` / `CreateFooter` / DPI）
- 尺寸与居中绘制：`UiFit.cs`、`UiScale.OnHostDpiChanged`
- 主窗顶/底栏：`MainForm.FitTopCommandBar` / `FitBottomActionButtons`
- 定时关机底栏（状态 vs 按钮）：`ShutdownTimerDialog.cs`
- 优化顾问：`HealthOverviewDialog.cs`
