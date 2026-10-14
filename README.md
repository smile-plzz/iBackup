# iBackup

Your memories, on your own drive.

A native Windows app for copying iPhone and iPad photos and videos over USB. Files are saved in ordinary folders, with incremental tracking so a repeat backup skips previously recorded files.

## Download

[Download v0.1.4 for Windows x64](https://github.com/smile-plzz/iBackup/releases/tag/v0.1.4)

This is an **unsigned preview**, not a full iPhone restore-backup tool. Extract the entire ZIP before opening `iBackup.exe`. The portable folder includes the .NET and Windows App SDK runtimes.

## First backup

1. Use Windows 10 (1809+) or Windows 11, with Apple Devices or iTunes installed for USB drivers.
2. Connect your iPhone or iPad, unlock it, and tap **Trust This Computer** if prompted.
3. Open iBackup and choose a backup folder under **Save to**.
4. Press **Start Backup**. Keep the device unlocked during scanning and copying.
5. Check the completion message and your destination files.

Windows may show a SmartScreen warning because this preview is not signed. Only use a release you trust.

## What it does

- Detects Apple devices through Windows Portable Devices.
- Shows scanning, copying, progress, and actionable error messages.
- Copies locally available supported photos, videos, RAW files, and screenshots.
- Organizes files by year and month.
- Skips files already recorded in the local backup manifest.
- Never overwrites existing destination files; collisions get numbered names.
- Checks copied file sizes and records destination SHA-256 hashes.
- Keeps phone originals untouched. No account, upload, or telemetry.
- Shows previous sessions in History and provides local diagnostic logs.

The v0.1.4 interface uses softer cards, clearer typography, adaptive navigation, and simpler backup controls. It follows the Windows theme and keeps native keyboard and accessibility behavior.

## Preview boundaries

- Backup preferences currently use fixed defaults: all supported media, year/month folders, and copy checks. Folder selection and automatic backup are on the Backup page.
- Custom media filters, alternative folder layouts, Windows startup, and minimize-to-tray preferences are not wired into the application flow. Settings now explains this instead of showing ineffective controls.
- SHA-256 is calculated on the copied file, not compared with an independently hashed source.
- USB access depends on the driver and trust/lock state. iCloud-only originals may not be available.
- Cancellation may wait for a blocking Windows device operation to return.
- The connected-device check copied two sample files and skipped them on a repeat run. It is not a full-library, large-video, or interruption certification.
- A successful historical manifest entry does not re-check whether a destination file has since been moved or deleted.

## Documentation

- [Release notes and verification](docs/RELEASE-0.1.4.md)
- [Troubleshooting](docs/TROUBLESHOOTING.md)
- [Design conventions](docs/DESIGN.md)
- [Product requirements](docs/PRD.md), a planning document, not a list of shipped capabilities

## Development

Windows, .NET 8 SDK, and the Windows build tools are required. No API keys are needed.

```powershell
dotnet restore iBackup.sln
dotnet test src/iBackup.Core.Tests/iBackup.Core.Tests.csproj -c Release
dotnet build src/iBackup.App/iBackup.App.csproj -c Release -p:Platform=x64
dotnet publish src/iBackup.App/iBackup.App.csproj -c Release -r win-x64 --self-contained true -p:Platform=x64 -p:WindowsAppSDKSelfContained=true -o artifacts/win-x64
```

The app is WinUI 3 / C#, with MediaDevices for USB access and SQLite for its manifest. See [CLAUDE.md](CLAUDE.md) for architecture and invariants. GitHub Actions tests and packages Windows x64 builds.

## License

Not yet specified.
