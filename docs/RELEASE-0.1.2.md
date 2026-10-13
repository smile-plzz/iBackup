# v0.1.2 preview

This update repairs backup feedback and several scan lifecycle defects reported after v0.1.1.

- Runs blocking Windows device operations off the UI thread.
- Keeps one backup dashboard model and avoids duplicate Home navigation/listeners.
- Stops the device monitor from reopening and disconnecting an active phone every two seconds.
- Filters non-Apple portable drives out of phone discovery.
- Supports deeper DCIM paths and storage trees with dated folders exposed without DCIM.
- Reports inaccessible scans and zero supported files as errors rather than successful backups.
- Shows scan counts, transfer progress, destination failures, and recovery guidance.
- Adds local log and backup folder shortcuts, a product icon, and troubleshooting documentation.
- Defaults automatic backup to off for new settings. Existing saved preferences remain respected.

Validation: 40 core tests, Release x64 publish, and responsive packaged-window startup. No iPhone was connected during final verification, so a real photo/video copy remains a required hardware acceptance check. The deeper-path fallback is not yet validated against the reporting user's phone.

Known limits: Windows WPD calls can block while the device is unresponsive; cancellation is observed when the call returns. SHA-256 currently records a destination digest, not a source-versus-destination comparison. Logs contain local paths and device identifiers; review before sharing.

## Icon source

Created with the built-in image-generation tool. Source: `src/iBackup.App/Assets/iBackup-icon.png`; Windows icon: `src/iBackup.App/Assets/iBackup.ico`.

Prompt: Original minimal Windows desktop icon combining an upright smartphone and a downward backup arrow into a storage tray; rounded geometry, blue/cyan palette, transparent background, strong small-size silhouette, no text, Apple logo, trademarks, or watermark.
