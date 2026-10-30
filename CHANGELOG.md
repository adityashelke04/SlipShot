# Changelog

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
