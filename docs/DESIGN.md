# Interface conventions

iBackup borrows the restraint of Apple interfaces without imitating macOS controls. It remains a native Windows app.

- Use system typography, with one page title and short supporting text.
- Keep content within an 800-pixel reading column; constrain the scroll viewport so narrow windows cannot clip the controls.
- Use 16-pixel card radii, 24-pixel inner spacing, and theme-aware neutral surfaces.
- Reserve the Windows accent for the primary action and selected navigation. Status must also have a text label.
- Show Start Backup consistently. Show pause and cancellation controls only during an active backup.
- Keep progress, copied bytes, and file counts visible together. Do not use percentages to imply a scan has a known total.
- Use native controls and focus indicators. Give path selection and progress explicit accessible names.
- Adapt navigation to a compact rail when space is limited. Long descriptions and status messages must wrap.
- Explain unavailable features rather than displaying settings that silently do nothing.

Release checks should cover disconnected and connected states, waiting/scanning/copying/error states, narrow windows, light/dark themes, keyboard navigation, long paths, and populated history. A successful XAML build alone does not establish visual correctness.
