# iBackup

This file provides guidance to Claude Code and other coding agents working in this repository.

## What this is

A WinUI 3/.NET 8 Windows desktop preview for incremental USB copies of iPhone/iPad media. It is not a full device restore backup.

## Commands

See README.md for restore, test, build, and self-contained publish commands. Build on Windows. The test project is src/iBackup.Core.Tests. No secrets are required. There is no automated visual UI suite.

## Architecture

- src/iBackup.App/App.xaml.cs initializes logging, SQLite, singleton monitoring/backup services, and MainWindow.
- MainWindow.xaml hosts adaptive navigation and the frame. App.xaml owns shared theme-aware presentation styles.
- Views/MainPage.xaml and ViewModels/MainViewModel.cs own device state, destination selection, and backup commands. The view model is singleton and the page cached to avoid duplicate listeners.
- Views/HistoryPage.xaml displays manifest sessions. SettingsPage explains current defaults; legacy SettingsViewModel preference persistence is not connected to MainViewModel's backup construction.
- src/iBackup.Core/Services/MediaScannerService.cs discovers the device's media root. Modern devices can expose dated folders directly below Internal Storage, without DCIM.
- BackupEngine.cs orchestrates scan/copy/manifest recording. FileTransferService.cs handles collision-safe copying and destination checks. Data/ManifestRepository.cs persists state in SQLite.
- .github/workflows/release.yml tests, publishes, validates compiled XAML resources, packages, and releases tagged builds.

## Conventions to preserve

- Run blocking WPD work off the UI thread. Avoid reconnecting an already monitored device or adding duplicate event subscriptions.
- Never replace inaccessible/failed scans with a false successful empty backup.
- Never overwrite existing destination files or delete originals. Cleanup may remove only a partial file created by the current transfer.
- Keep the custom publish resource-copy target. Missing PRI/XBF files previously caused a process with no usable window.
- Do not claim destination SHA-256 is a source comparison. Do not equate unit tests or a small device sample with full-library verification.
- Keep unavailable preferences out of active controls until wired and tested. Read docs/DESIGN.md before changing presentation.
