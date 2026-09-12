# SrvDesk

**Windows system optimization & desktop experience assistant**

[中文](README.md) · [English](README_en.md)

SrvDesk is a system optimization tool for **Windows 10, Windows 11, and Windows Server 2019/2022/2025**.

Through system inspection, optimization advice, service management, component management, and performance tuning, it helps make Windows Server more suitable for personal desktop use, while also providing safe, stable, and recoverable optimization for Windows 10/11.

**Version:** 1.0.75 · [Download Releases](https://github.com/gygy/SrvDesk/releases)

---

## Download & run

1. Open [Releases](https://github.com/gygy/SrvDesk/releases) and download **`SrvDesk.exe`** (single file, light obfuscation).
2. Right-click → **Run as administrator**.
3. Disclaimer, privacy, and license are under the in-app **Help** menu.

**Requirements:** Windows 10 / 11; Windows Server 2019 / 2022 / 2025 (Desktop Experience recommended). Needs **.NET Framework 4.8**.

> This public repository contains **docs and license only** (no source). Get the binary from Releases.

---

## ✨ Core features

### 1. System optimization

Common settings and performance tweaks:

* Power plans
* Visual effects
* CPU performance
* GPU settings
* Memory management
* NTFS optimization
* Network optimization
* Background optimization
* Windows Update settings

### 2. Service management

Classify Windows services and identify risk:

* Keep recommended
* Suggested to optimize
* Enable on demand
* Safe to disable
* Do not modify

You can review:

* Service purpose
* Current status
* Startup type
* Optimization advice
* Impact scope
* Recovery options

### 3. System component management

Manage Windows Features, Server Roles, and related components:

* Hyper-V
* Containers
* IIS
* NFS
* SNMP
* Print Services
* Remote Desktop Services
* Windows Sandbox
* SMB 1.0
* Other optional features

Enable or disable based on real usage.

### 4. Startup & background management

Manage startup items and background tasks:

* Startup apps
* Background programs
* Scheduled tasks
* Unnecessary updaters
* Software assistants
* Third-party background services

Reduce unnecessary resource use.

### 5. Windows Server desktop experience

Specialized for using Windows Server as a personal PC:

* Desktop Experience
* Server Manager
* IE Enhanced Security Configuration
* Windows Search
* Desktop visual effects
* Multimedia-related components
* Common desktop experience settings

Make Windows Server feel closer to a normal Windows desktop.

### 6. Security-aware optimization

Optimize without trading away security:

* Microsoft Defender
* Windows Firewall
* UAC
* Windows Update
* SmartScreen
* Security policies

**We do not chase performance by turning off security features.**

### 7. System inspection

Automatically inspect the current system:

* OS version
* Hardware profile
* CPU / GPU
* Memory
* Disk
* Network
* Power plan
* Service status
* Startup items
* System components
* Security configuration

Generate optimization advice from the results.

### 8. One-click optimization

Batch apply by optimization level:

| Level | Meaning |
| --- | --- |
| 🟢 Recommended | Safe and stable; suggested |
| 🔵 Performance | Prefer system responsiveness |
| 🟡 On demand | Choose by scenario |
| 🔴 Advanced | Deep tuning for advanced users |

---

## 🛡️ Optimization principles

SrvDesk does not chase “disable more = faster”. It sticks to:

> **Security first · Stability first · Moderate performance · Optimize on demand · Recoverable**

Each item should make clear, where possible:

* Current state
* Recommended state
* What it does
* Potential impact
* Risk level
* Whether reboot is required
* Whether recovery is supported

Avoid unjustified:

* Extreme registry “tweaks”
* Mass-disabling system services
* Turning off Defender
* Turning off Windows Firewall
* Permanently disabling Windows Update
* Unjustified TCP parameter changes

---

## 💻 Supported systems

### Windows Desktop

* Windows 10
* Windows 11

### Windows Server

* Windows Server 2019
* Windows Server 2022
* Windows Server 2025

SrvDesk matches available optimizations to each Windows edition’s capabilities and differences.

---

## 🎯 Use cases

### Personal PCs

Optimize:

* Responsiveness
* Startup
* Background apps
* Power
* Visual effects
* Storage
* Network

### Windows Server as a personal desktop

Use Windows Server as:

* Personal PC
* Workstation
* Dev environment
* NAS
* Download server
* Docker host
* Virtualization host
* Home server

### Advanced users

Provides:

* Service-level tuning
* Component management
* Advanced network tuning
* CPU / GPU tuning
* Memory and storage tuning
* Advanced system parameters

---

## 🔄 Recoverability

Important optimizations should be recoverable whenever possible.

Before optimizing:

1. Inspect current configuration
2. Save original state
3. Apply changes
4. Verify results
5. Restore if something goes wrong

**Avoid unexplained, unrecoverable “extreme one-click” optimizations.**

---

## 📋 Product positioning

SrvDesk is not a simple “system cleaner”. It is:

> **Windows inspection + optimization advice + configuration management + performance tuning + desktop experience assistant**

The goal is:

**Understand → Choose clearly → Optimize safely → Recover when needed.**

---

## ✅ Cleanliness pledge

**Guaranteed clean: no ads; no malware; no hidden side agendas — including every future update.**

SrvDesk only inspects and tunes the local system. It does not bundle promotions, embed malicious code, or use the app as cover for unrelated activities.

---

## 📌 Roadmap

Continuing improvements:

* More Windows optimization items
* Better edition/version awareness
* Smarter service recommendations
* Before/after comparison
* Config backup and restore
* Performance benchmarks
* Network optimization
* Storage optimization
* Gaming optimization
* Dev-environment optimization
* Server desktop experience improvements
* Automated optimization strategies

---

## Disclaimer (short)

Changing system settings is risky. Validate on a test machine first; always back up production environments. The author is not liable for data loss or business interruption caused by using this software. See the in-app disclaimer and license for full terms.

---

## License

This project is intended for system administration, configuration optimization, and personal PC/server maintenance.

Before advanced system changes, make sure you understand the possible impact, and back up the system and important data first.
