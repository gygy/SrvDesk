# SrvDesk — Windows Server Optimization & Health Assistant

Local **optimization & health assistant** for Windows Server 2019/2022/2025 (also works on desktop Windows):

**Profile → health check → explainable advice → dry-run confirm → apply → verify/rollback → inspection.**

Don’t disable services blindly — balance performance, stability, security, and compatibility.

**Version:** 1.0.31 · [Download Releases](https://github.com/gygy/SrvDesk/releases) → `SrvDesk.exe`

Full guide (中文, default): [README.md](README.md)

## Requirements

Windows Server 2016+ (2022/2025 recommended, Desktop Experience). Also works on Windows 10/11 (Server-only items hide). .NET Framework 4.8. **Run as Administrator.**

This public repo has **docs + license only** (no source). Get the binary from Releases.

## Quick start

1. Run as Administrator  
2. **Tools → Server profile** — roles + optimization level (Detect / Safe / Standard / Deep)  
3. **Tools → Health overview** — score & issues  
4. Review advice; **Apply** shows a change plan first  
5. **Tools → Rollback optimization** for service snapshots or `rstrui`

## Tools (health assistant)

Health overview · Server profile · Recommendations · Port exposure · Rollback optimization · plus existing cleanup, features, security center, shutdown timer, etc.

## Design rule

> Not “disable more = faster”, but “change only what you are sure you don’t need”.
