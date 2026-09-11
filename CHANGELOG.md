# Changelog

All notable changes to SrvDesk are documented here.

## [Unreleased]

## [1.0.41] - 2026-09-11

### Added

- Common software: UniGetUI (Devolutions.UniGetUI)

### Fixed

- Config script panel forced hidden at startup when dock default is Closed
- Optimization advisor blank gap after Apply to system (scroll reset)

## [1.0.36] - 2026-09-11

### Changed

- Raised many toggles to Strongly recommended (list + advisor): IE ESC, This PC, RDP/GPU/FPS, taskbar clock & tray icons, shortcut/context-menu items, Disable System Restore, account-policy items
- Config script dock: add Closed; default off for new installs (merged former “show at startup”)

## [1.0.33] - 2026-09-11

### Added

- Command-bar shortcut for Optimization advisor (next to Common software)

### Changed

- Recommend tiers: Strongly recommended / Recommended / Advanced / Not recommended; advisor OS-aware filter
- First-run AV false-positive tip + VirusTotal link; cleaner update-notes formatting

## [1.0.32] - 2026-09-11

### Changed

- Optimization advisor: only Strong/Must unset items; silent apply (no change-plan/restore prompts); removed group “Set recommended”; shorter header

## [1.0.31] - 2026-09-09

### Added

- Tools → Shutdown timer (Shutdown Agent–style layout, SrvDesk theme): countdown or clock time; shutdown / restart / log off / suspend / hibernate / lock; force terminate; tray blink near execution

### Changed

- Shutdown timer menu icon: self-drawn power + countdown arc

## [1.0.30] - 2026-09-09

### Fixed

- Flat buttons/combos no longer clip Chinese text (custom config toolbar, main command bar, script panel, common software rows, etc.)
- Config-script dialog layout: tabs not overlapped by editor; window is freely resizable

### Changed

- Config-script entry icon: document + pencil
- Help/detail panel reserves footer tip space so text is not clipped

## [1.0.29] - 2026-09-09

### Changed

- Service Optimize LTSC hints: richer notes from Win10 LTSC 2021 service remarks (privacy, Bluetooth, update orchestrator, SgrmBroker, Search, vendor drivers, etc.)

### Added

- Common software: Inkscape

## [1.0.28] - 2026-09-08

### Added

- Service Optimize: live Win32 services list, advice tags, collapsible categories, recommend stars, start-type backup/restore snapshots (auto before batch changes)
- File menu → Exit

### Fixed

- Header no longer shows workgroup; IP/CPU/memory meter stays fully visible
- Common software toolbar labels/buttons no longer clipped on the right
- Embedded pages (Service Optimize, etc.) no longer leave an empty top command bar
- Service list column order and wider wrapping display names

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
