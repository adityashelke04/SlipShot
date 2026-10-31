# Changelog

## 0.2.7-preview.1 — 2026-10-31

Moved folder access to a compact footer box beside Latest in tray; restored one primary Edit/Copy/Save/Pin row. Added three ChatGPT-generated identity concepts, exact-color original SVG masters and an audited Frame-S Windows icon at9sizes. Executable/window/tray icon resources now present; UI orange/charcoal palette uses verified brand reference colors with contrast-safe text/focus.125tests pass. Existing screenshots and user data remain untouched.

## 0.2.6-preview.1 — 2026-10-31

Added Open file location beside Edit in the left preview. It opens the configured captures folder in Windows Explorer, creating an empty folder if needed; it never saves an image. Errors stay inline. Preview actions use two compact rows. Existing app/tray icon is unchanged; image/logo generation awaits explicit user approval.

## 0.2.5-preview.2 — 2026-10-31

Save now remembers successful exports per capture snapshot. Unchanged repeat saves are no-ops; buttons become disabled Saved. Capture previews and reopened editors share save receipts but keep independent edits. Undo/redo recognizes already-exported snapshots. Failed/deleted exports remain recoverable, and confirmed Save As overwrites invalidate stale receipts. Separate copies require the explicitly named Save a copy as action. Default GitHub branch becomes main; older master history remains.

## 0.2.4-preview.1 — 2026-10-30

Save and Ctrl+S now write a uniquely named PNG directly to the configured SlipShot folder, without a location dialog. Default for this F-drive install is F:/SlipShot Captures; missing folders are created on explicit Save. Save As remains in the editor arrow menu and Ctrl+Shift+S for other paths/JPEG. Folder errors do not silently redirect or mark the document saved. No automatic saving on capture added.

## 0.2.3-preview.1 — 2026-10-30

- Default preview timeout10seconds; passive hover/focus grace capped at twice the configured duration. Real hold/drag/copy remains protected. Native pointer/foreground validation and missed-release recovery prevent stale WPF flags pinning the card indefinitely.
- Timer-driven closure remains effective if the fade never completes. New capture replaces the old card; no FIFO resurfacing or preview publication during selection. Retired cards cannot clear newer ownership.
- Copy provides persistent PNG and bitmap formats with metadata stripped, shows success/error feedback, and supersedes older pending copy retries. Optional auto-copy errors no longer misreport capture failure. Auto-copy remains opt-in.

## 0.2.2-preview.1 — 2026-10-30

Renamed the application to **SlipShot**: UI/tray labels, generated image filenames, executable/assembly/project/namespace names, build scripts, current docs and synthetic demo images. Private GitHub repository renamed to adityashelke04/SlipShot. Shortcut remains Ctrl+Alt+S. Historical releases/audit records retain their original identity. Kept the legacy single-instance mutex and drag-file prefix to coexist safely with earlier builds and retained transfers.

## 0.2.1-preview.1 — 2026-10-30

Default capture shortcut is now **Ctrl+Alt+S**, with no function key. Settings accept modified letter keys as well as function keys, reject numeric enum aliases and unmodified keys, and retain native conflict detection. Existing custom settings remain user-controlled. Includes new parser/mapping regression tests.

## 0.2.0-preview.1 — 2026-10-30

- Real preview drag-and-drop with PNG file, bitmap and PNG-stream payloads, click/drag threshold distinction and dismissal pause through the entire gesture.
- Explicit-drag temporary files retained for late consumers, canceled-file cleanup and bounded ownership-aware retention. No automatic files on capture.
- Redesigned preview/editor: compact header, labeled tool rail, contextual properties, optional annotation inspector, filled primary actions and crisp physical-pixel zoom.
- Private GitHub repository connected with origin/upstream, plus a publishing preflight. No public conversion or binary upload.
- Additional synthetic gesture, transfer and DPI tests; native IDE/terminal drop acceptance still requires manual verification.

## 0.1.0-preview.1 — 2026-10-30

Initial independent native WPF implementation: region selection, left preview, tray/hotkey/settings, all core annotation tools, bounded undo/redo, opaque redaction, PNG/JPEG export, clipboard retry handling and pinned references.

Includes synthetic NUnit tests, six offscreen editor layouts, three source audits and a documented manual acceptance matrix. Built using a small official compiler fallback because the SDK was absent. Unsigned, x64, framework-dependent. Public-release readiness, native desktop interaction and SDK publish remain unverified.
