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
- **🔔 System Tray** — Keeps the app available from the Windows notification area
- **✅ Integrity Verification** — Optional SHA-256 verification after each transfer
- **🛡️ Collision Safe** — Existing files are never overwritten; numbered copies are created instead

## 🛠️ Tech Stack

| Layer | Technology |
|---|---|
| Language | C# / .NET 8 |
| UI Framework | WinUI 3 (Windows App SDK) |
| iOS Communication | MediaDevices 2.x (Windows WPD/PTP) |
| Database | SQLite (backup manifest tracking) |
| Platform | Windows 10/11 (x64) |

## 📋 Prerequisites

- **Windows 10 (1809+)** or **Windows 11**
- **iTunes** or **Apple Devices** app installed (provides USB drivers)
- iPhone/iPad with USB cable

## 🚀 Getting Started

Download the latest Windows x64 ZIP from [Releases](https://github.com/smile-plzz/iBackup/releases), extract it, and run `iBackup.exe`.

The app is currently an unsigned preview, so Windows may show a SmartScreen warning. You must also have Apple Devices or iTunes installed and tap **Trust** on the unlocked iPhone/iPad when prompted.

### Build from source

```bash
git clone https://github.com/smile-plzz/iBackup.git
cd iBackup
dotnet restore iBackup.sln
dotnet test src/iBackup.Core.Tests/iBackup.Core.Tests.csproj -c Release
dotnet build src/iBackup.App/iBackup.App.csproj -c Release -p:Platform=x64
```

### Create a portable release

```powershell
dotnet publish src/iBackup.App/iBackup.App.csproj -c Release -r win-x64 --self-contained true -p:Platform=x64 -p:WindowsAppSDKSelfContained=true -o artifacts/win-x64
```

## Current preview limitations

- USB discovery and transfers require a real iPhone/iPad for end-to-end hardware verification.
- The preview is unsigned and ships as a portable folder rather than an MSIX installer.
- Keep the iOS device unlocked during large transfers because WPD availability can change when the screen locks.

## 📖 Documentation

- [Product Requirements Document (PRD)](docs/PRD.md)

## 📄 License

TBD

---

*Built with focus. One job, done right.*
