# SrvDesk v1.0.24

## 中文

- 常用软件：winget 不可用或失败时，自动从官网解析最新安装包并静默安装；仍失败才打开下载页
- 海康互联、天翼云盘走官方接口取最新包（下载页是动态页面，HTML 里没有直链）
- 短英文检测不再误报（例如 `pi` 不会命中无关软件）
- 7-Zip 优先解析官网同域安装包

下载：单文件 `SrvDesk.exe`，请以管理员身份运行。

## English

- Common software: if winget is missing or fails, resolve the latest official installer and silent-install; the download page opens only as a last resort
- HikConnect and Tianyi Cloud Drive use official APIs (their download sites are SPAs with no static `.exe` links)
- Short ASCII detect patterns no longer false-match unrelated apps
- 7-Zip scoring prefers the official `7-zip.org` package

Download `SrvDesk.exe` and run as Administrator.
