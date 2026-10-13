# Troubleshooting iBackup

## The iPhone is detected, but photos cannot be scanned

1. Unlock the iPhone and keep its screen awake.
2. Reconnect the USB cable and tap **Trust This Computer** on the iPhone.
3. Confirm that the Apple Devices app or iTunes is installed on Windows.
4. Open the Windows Photos app once and confirm that it can see the phone's photos.
5. Return to iBackup and start the backup again.

If iCloud Photos uses **Optimize iPhone Storage**, some full-resolution originals may not exist locally on the phone. iBackup can only copy media exposed by the iPhone through Windows USB access.

## The backup folder cannot be used

OneDrive can sometimes report a Pictures path that is not currently available. Use **Browse** to choose an existing local folder or connected external drive. iBackup validates the folder before scanning the phone.

## Understanding the status

- **Ready**: An Apple mobile device is detected.
- **Scanning**: iBackup is discovering supported photos and videos. The number found updates live.
- **Copying**: Files are transferring to the selected folder.
- **Complete**: The summary lists copied, skipped, and failed files.
- **Needs attention**: The message explains the access, destination, or transfer problem.

## Diagnostic logs

Open **Having trouble accessing iPhone photos?** on the Home screen and choose **Open diagnostic logs**. Logs are stored under `%LOCALAPPDATA%\iBackup\logs` and contain technical errors and transfer results. They do not contain photo or video contents.

When reporting a problem, include:

- the newest log file;
- the iPhone and Windows versions;
- whether Windows Photos can import from the phone;
- whether the phone was unlocked and trusted.
