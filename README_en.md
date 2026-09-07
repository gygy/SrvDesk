# SrvDesk — Windows Server Desktop Optimizer

Local optimizer for **Windows Server as a daily desktop**: registry / services / policy / DISM. Instant pages + batch toggles.

**Version:** 1.0.26 · [Download Releases](https://github.com/gygy/SrvDesk/releases) → `SrvDesk.exe`

Full guide (中文, default): [README.md](README.md)

## Requirements

Windows Server 2016+ (2022/2025 recommended, Desktop Experience). Also works on Windows 10/11 (Server-only items hide). .NET Framework 4.8. **Run as Administrator.**

This public repo has **docs + license only** (no source). Get the binary from Releases.

## Quick start

1. Run `SrvDesk.exe` as Administrator  
2. **Preset → Server Desktop (recommended)** → load  
3. Review left-side groups; click **Apply recommended**  
4. Reboot if prompted  

First launch shows a one-time notice. Details: **Help → Disclaimer / Privacy / License**.

Header shows **IPv4, CPU, memory**.

## Sidebar

**Instant (apply immediately):** Startup items · DNS · Custom packs  

**Batch (need Apply):** Server-only · Account policy · Explorer · Desktop · Remote/network · Privacy/experience · Performance/security · Power/services  

Privacy and performance groups are split into foldable sections by scenario.

## Tools (highlights)

Common software (includes **AI**: Codex CLI/Desktop, Pi Agent) · hosts · junk cleanup (grouped, with progress) · Windows features · Edge · context menu · security center · restore defaults

**Help → Check for updates** reads GitHub Releases, downloads `SrvDesk.exe`, replaces the running file and restarts. Startup check can be turned off in **File → Settings**.

## CLI (admin)

```text
SrvDesk.exe --apply-preset server-desktop
SrvDesk.exe --load-profile D:\profile.json
SrvDesk.exe --export-profile D:\current.json
```

## License

[MIT](LICENSE) · [Disclaimer](DISCLAIMER.md) · [Privacy](PRIVACY.md)  
Issues: https://github.com/gygy/SrvDesk/issues
