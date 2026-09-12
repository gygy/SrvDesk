# SrvDesk v1.0.75

## 中文

- 账户策略：修正密码复杂性读写（以 secpol 为准）；secedit 导出改键回写，避免退出码 1 误判
- 应用到系统：修复长时间卡在「正在写入」（进程超时、应用后快速刷新）
- 右键菜单等即时页：点「应用到系统」会提示已立即生效，不再静默无响应
- 关闭强制密码历史；关复杂性时一并放开最小密码长度
- 单文件 `SrvDesk.exe`，轻度混淆；请以管理员身份运行

## English

- Account policy: fix password-complexity read/write (trust secpol); export-edit-configure for secedit (exit code 1 no longer treated as hard fail)
- Apply to system: fix long hang on “Writing…” (process timeout + fast refresh after apply)
- Instant pages (e.g. context menu): Apply shows a clear tip instead of doing nothing
- Disable password history; relaxing complexity also sets minimum length to 0
- Single-file `SrvDesk.exe`, lightly obfuscated; run as Administrator
