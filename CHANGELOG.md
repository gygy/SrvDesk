# Changelog

All notable changes to SrvDesk are documented here.

## [Unreleased]

## [1.0.26] - 2026-09-08

### Fixed

- Winget install on Server 2019/2022 follows asheroto/winget-install: provision with License1.xml, fix PATH/ACL, then portable fallback
- Desktop / Server 2025 prefer Repair-WinGetPackageManager; clearer install errors and longer dialog text

## [1.0.25] - 2026-09-07

### Fixed

- Setting list title no longer overlaps the scope subtitle
- Single-row toolbars no longer show a leftover vertical scrollbar; preset combo shows full titles such as「Server 桌面（推荐）」
- Common software left nav matches the main window sidebar (width, row height, selected/hover)

## [1.0.24] - 2026-09-07

### Added

- Common software: resolve the latest official Windows installer (GitHub Releases, download pages, vendor JSON APIs) and silent-install when winget is missing or fails
- HikConnect and Tianyi Cloud Drive use their official APIs so SPA download pages still get a fresh package

### Fixed

- Short ASCII detect patterns (e.g. `pi`) no longer match unrelated apps
- 7-Zip latest-package scoring prefers `7-zip.org` over third-party GitHub mirrors

## [1.0.23] - 2026-09-07

### Added

- Apply-time System Restore prompt; Zyper / Sophia / WinUtil-aligned tweaks; Folder Options (group/sort/drive letter)
- Automated four-round test suite (`docs/test-cases.md`, `scripts/full-test.ps1`)

### Fixed

- Profile export/import dropped int/string fields (folder view, taskbar search, autologon user)
- Run-dialog history no longer overwrites `Start_TrackProgs` (app-launch tracking)

## [1.0.1] - 2026-08-28

### Added

- Initial public release: **Windows server优化助手 SrvDesk**
- Instant settings pages: Explorer, power & services, startup, DNS
- Batch optimization groups with search and contextual help panel
- Four presets: Server Desktop, Security, Remote Work, Minimal
- JSON profile import/export and CLI (`--apply-preset`, `--load-profile`, `--export-profile`)
- Tools: hosts editor, group policy, Windows features, context menu tweaks, cleanup, quick tools
- Operation log at `%LocalAppData%\SrvDesk\apply.log`

### Fixed

- Startup crash when loading embedded page before form handle is created

### Changed

- Product rebrand from Win一键优化 to SrvDesk (`SrvDesk.exe`)
