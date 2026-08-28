# Changelog

All notable changes to SrvDesk are documented here.

## [Unreleased]

## [1.0.1] - 2026-08-28

### Added

- Initial public release: **Windows server优化助手 SrvDesk**
- Instant settings pages: Explorer, power & services, startup, DNS
- Batch optimization groups with search and contextual help panel
- Four presets: Server Desktop, Security, Remote Work, Minimal
- JSON profile import/export and CLI (`--apply-preset`, `--load-profile`, `--export-profile`)
- Tools: hosts editor, group policy, Windows features, context menu tweaks, cleanup, quick tools
- Operation log at `%LocalAppData%\WinOpt\apply.log`

### Fixed

- Startup crash when loading embedded page before form handle is created

### Changed

- Product rebrand from Win一键优化 to SrvDesk (`SrvDesk.exe`)
