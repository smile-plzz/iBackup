# iBackup 0.1.4 preview

## What changed

- A calmer, Apple-inspired visual direction while retaining native Windows controls: rounded neutral cards, clearer typography, consistent spacing, and branded adaptive navigation.
- A full-width primary backup action; pause and cancel appear only during a backup.
- Corrected horizontal clipping on the backup page, wrapping descriptions, and non-overlapping History headers. History entries now separate identity/status from transfer details.
- A larger initial window, explicit accessible names for folder selection/progress, and a polite status live-region hint.
- Settings now documents actual defaults instead of presenting controls not connected to the backup flow. About reads the compiled version rather than a hard-coded 1.0.0 label.
- Rewritten README, design conventions, and architecture notes with explicit preview boundaries.

The USB scanner and copy engine are unchanged from 0.1.3. Existing user settings and manifest data are not migrated or deleted.

## Verification

- 40 core regression tests passed locally.
- Windows x64 self-contained publish succeeded, including the required PRI/XBF resources.
- The published app launched. Native UI inspection covered the disconnected Backup page, History empty state, Settings, and navigation in the current dark Windows theme. The normal-size Backup view was checked after the width fix; expanded views were also inspected during refinement.
- The earlier connected-device test copied two files totaling 5,427,993 bytes and skipped both on repetition using an isolated manifest and destination. The user subsequently reported the latest backup build working as expected.

Not certified: every light/high-contrast/DPI combination, populated-history visual states, a complete photo library, large videos, or interrupted USB recovery. These remain acceptance checks, not implied successes.

## Install

Download iBackup-win-x64.zip from this release, extract the complete folder, and launch iBackup.exe. Close an older running copy first. Apple Devices or iTunes is still required for USB drivers. Keep the iPhone unlocked and trusted.

This is an unsigned preview. It backs up media, not the full phone. See README.md and docs/TROUBLESHOOTING.md for current limitations.
