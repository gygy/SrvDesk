# SrvDesk WinForms UX 检查清单

对照 `.cursor/rules/winforms-ux.mdc`。审计某个窗体时逐项打勾。

## 文案

- [ ] 无流程条 / 步骤口号
- [ ] 无与按钮重复的摘要横幅
- [ ] 无「提示：」科普注脚（列表「建议」列可保留短句）
- [ ] 删掉后仍能完成当前页任务

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

## 按钮与底栏

- [ ] `ThemedSettingsChrome.CreateButton`
- [ ] 主按钮不超过一条主路径抢视线
- [ ] 同排按钮同高，间距统一
- [ ] 状态条 ≠ 第二主按钮（浅底边框 / Pale，非 PrimaryDeep 实心块）

## 列表与工具条

- [ ] ListView `SurfaceCard` + 双缓冲（`UiBuffer`）
- [ ] 列宽 fit，无多余横/竖滚动条
- [ ] 单行工具条 `ConfigureNoScrollRow`

## 文案与风险

- [ ] 用户串双语
- [ ] 危险操作有确认 / 等级拦截
- [ ] 建议含原因或可点到说明（服务页等）

## 参考实现

- 壳与按钮：`ThemedSettingsChrome.cs`
- 定时关机底栏（状态 vs 按钮）：`ShutdownTimerDialog.cs`
- 优化顾问：`HealthOverviewDialog.cs`
