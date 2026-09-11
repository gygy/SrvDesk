# SrvDesk — Windows Server Optimization Assistant

A local optimizer for **Windows Server 2019 / 2022 / 2025** (also works on Windows 10/11). It shows what can change, why, and the risk — then writes to the system only after you confirm.

**Version:** 1.0.33 · [Download Releases](https://github.com/gygy/SrvDesk/releases) → `SrvDesk.exe`

Full guide (中文): [README.md](README.md)

## Requirements

Windows Server 2016+ (2022/2025 recommended, Desktop Experience). Windows 10/11 works too; Server-only items are hidden or marked N/A. Needs **.NET Framework 4.8**. **Run as Administrator.**

This public repo has **docs + license only** (no source). Get the binary from Releases.

## Quick start

1. Run `SrvDesk.exe` as Administrator.
2. Open **Tools → Optimization advisor** (also on the main toolbar). It lists **Strong / Recommended** items that are not met yet; you can apply selected ones.
3. Optionally set server role (RDP, file share, Docker, …) and optimization level.
4. Use the side panels for switches/services; most changes need **Apply to system** (optional change plan / restore point).
5. **Tools → Rollback optimization** for service snapshots or System Restore.

Presets such as Home Server / Docker host / NAS are available if you want a starting template.

## Design rule

Change what you don’t need — not “disable more = faster”.

Each suggestion tries to state: reason, benefit, risk, impact, and whether it can be rolled back.

## Disclaimer (short)

Changing system settings is risky. Test first; back up production machines. The author is not liable for data loss or downtime. See in-app disclaimer and license for full terms.
