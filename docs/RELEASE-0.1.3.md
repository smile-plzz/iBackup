# v0.1.3 preview

Fixes slow folder discovery on the connected iPhone. Its Windows media tree exposes dated folders directly below Internal Storage, without a DCIM folder. The scanner now uses the actual Internal Storage root before probing guessed paths.

Real-device validation on 2026-10-14: production scanner found two sample media files; the production backup engine and transfer service copied both (5,427,993 bytes), with zero failures and destination hashing enabled. A repeated backup copied zero files and skipped both successfully. The test used an isolated destination and SQLite manifest, leaving the normal backup history and phone originals unchanged.

This confirms a small real-device backup and incremental repeat. A complete library run, interruption recovery, and large-video transfers remain unverified. The scan uses blocking Windows device calls on a background thread; cancellation can still wait for a device call to return.
