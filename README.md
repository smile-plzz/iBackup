# 📱 iBackup

**A lightweight, native Windows desktop app that automatically backs up photos and videos from your iPhone/iPad over USB — incrementally, reliably, and without duplicates.**

Plug in your cable. Your media is backed up. That's it.

---

## ✨ Features

- **🔌 Plug & Play** — Auto-detects iOS devices via USB
- **⚡ Incremental Backup** — Only copies new files, never duplicates
- **📁 Transparent Storage** — Files land as normal JPG/HEIC/MOV/MP4 in browsable folders
- **🗂️ Smart Organization** — Automatically organizes by year and month
- **🔒 Privacy-First** — No cloud, no accounts, no telemetry — everything stays local
- **🪶 Lightweight** — Small footprint, fast startup, minimal resource usage
- **🔔 System Tray** — Runs quietly in the background with Windows notifications

## 🛠️ Tech Stack

| Layer | Technology |
|---|---|
| Language | C# / .NET 8 |
| UI Framework | WinUI 3 (Windows App SDK) |
| iOS Communication | MediaDevices (WPD/PTP) + libimobiledevice (AFC fallback) |
| Database | SQLite (backup manifest tracking) |
| Platform | Windows 10/11 (x64) |

## 📋 Prerequisites

- **Windows 10 (1809+)** or **Windows 11**
- **iTunes** or **Apple Devices** app installed (provides USB drivers)
- iPhone/iPad with USB cable

## 🚀 Getting Started

> 🚧 Project is in early development. Build instructions coming soon.

```bash
git clone https://github.com/smile-plzz/iBackup.git
cd iBackup
```

## 📖 Documentation

- [Product Requirements Document (PRD)](docs/PRD.md)

## 📄 License

TBD

---

*Built with focus. One job, done right.*
