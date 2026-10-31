# Verification record and desktop matrix

2026-10-31 bug review: 149 automatic tests pass, with one native hotkey test explicitly opt-in; 36 offscreen UI renders and the theme-switch comparison pass. Eight confirmed bugs were fixed, including the folder-action dismissal path found during final review. See [application audit](docs/AUDIT_4.md) for before/after behavior, regression evidence and remaining release checks.

Automated runs use synthetic pixels, fake clipboard sinks, temporary files under the test output, and serial NUnit STA execution. No capture overlay, production hotkey, real clipboard write or user-desktop screenshot was performed. Native desktop tests are **NOT RUN**.

## Automated evidence

0.2.8: 134 tests pass; 36 synthetic UI renders and a pixel-matched live theme switch. Three audit passes and native limits: [theme review](docs/THEME_REVIEW.md).

0.2.7:125tests passed. Footer tests verify folder action beside retention text, four primary actions and saved/error behavior. Brand tests verify9ICO sizes, transparent corners, exact#D97757/#141413 interiors, cached256px window image and owned32px tray clones. A regression caught16px window decoding and was fixed. Theme tests check normal light/dark palette and text/focus contrast. Native PE resource checks confirm executable icon/icon-group/manifest. Software renders cover20UIstates plus logo size comparisons. Actual Windows shell/tray cache display and high-contrast interaction remain manual checks; no OS cache cleanup performed.

0.2.5 save-once regression:114tests pass. The initial actual-controller test reproduced three PNGs from three unchanged saves, then passed with one. Further tests cover shared preview/editor receipts with independent edits, repeated unshown-preview button clicks becoming disabled Saved, retry after failure, deleted-file recovery, saved undo/redo snapshots, same-capture/cross-capture explicit overwrite invalidation and sibling UI notifications. Twenty synthetic layouts include Saved states. Actual visible dialogs and private capture clicks remain manual acceptance checks; no personal images were used.

0.2.4: quick-save fixtures verify first-use directory creation, PNG dimensions, repeated same-timestamp saves without overwriting, blocked-folder preservation and rejected relative paths. Existing PNG/JPEG, atomic failure and collision tests remain. Manual check: Save from preview/editor creates a file without a picker; Ctrl+Shift+S and Save arrow open Save As; changing the Settings folder takes effect; unavailable folder keeps unsaved edits. Actual private screenshots were not used for these tests.

0.2.3 regression pass:102tests passed. New tests cover10second idle/30second passive limit, aggregate hover allowance, active gestures, missed release recovery, fade-independent closure using a real dispatcher and an unshown zero-HWND preview, latest-capture replacement, capture-busy publication guard, and bitmap/PNG copy privacy and pixel identity. No real desktop or clipboard content used. Manually verify two rapid captures, Copy then app-switch, >30second parked hover, active long drag, cancelled capture and capture during fade. These real-input checks remain NOT RUN.

Run `./scripts/build-fallback.ps1 -Test -VisualCheck` or the SDK commands in README. Machine: Windows x64, .NET desktop runtime 8.0.11. Warnings are errors in fallback compilation. Latest exact totals are in `docs/THEME_REVIEW.md` and `artifacts/tests/TestResult.xml`.

Coverage includes 100/125/150/200% coordinate round trips with negative origins; normalized/tiny/empty/clipped selection; crop pixel dimensions independent of source DPI; drawing pixels for every tool; descending line direction; redaction layering/opacity and PNG reload; text and step contrast; all resize handles; undo/redo/invalidation/saved state; view-model notifications; geometry/input/memory validation; capture cancellation/reentry/stale completion; preview queue bounds/pause/reduced motion and placement; PNG/JPEG encoding/transparency; overwrite/collision/invalid path/write failure; corrupt/unsupported settings; busy/nonbusy/canceled clipboard with thread identity assertions.

An earlier full run failed 3 of 60 tests: unsupported settings threw an uncaught InvalidDataException, and two Windows locked-file paths raised UnauthorizedAccessException instead of the test's expected IOException. The settings bug was fixed; file tests now accept the two valid OS failure classes while still verifying unchanged destination bytes and temp cleanup. These are historical failures, not hidden skipped tests.

Synthetic visual review renders the actual detached editor content at 1240×820, 850×650 and 640×480, plus preview and settings surfaces, in light and dark themes (ten fixtures). These are offscreen software images, not captures of the desktop. Review found and fixed dark dropdown contrast and settings footer visibility. Native title bars, actual focus, input and screen-reader announcements are outside this evidence.

## Desktop checks — all NOT RUN

0.2 update adds18 synthetic tests (80 total): gesture threshold/cancellation/timeout, PNG file/bitmap/stream payloads, redaction and source-metadata stripping, accepted late reads, canceled cleanup, ownership/expiry/capacity/write failures and physical-pixel DPI zoom. Offscreen layout review now includes16 fixtures with Text/Rectangle/Redaction contexts at640DIP. Source/fixture tests do not prove OLE interoperability.

New manual checks: drag the thumbnail into Explorer, Paint/an image-aware composer and the user's terminal/IDE; verify attachment vs path semantics, Escape cancellation, >8second drag hold, no accidental editor opening, unsupported target, elevated target rejection and display-change/tray action during drag. **NOT RUN** on those real targets. Preview1 process launch was observed after the user asked to launch; this does not establish capture or preview2 launch correctness.

| Check | Method and expected result | Status |
|---|---|---|
| First run/tray | Launch; tray appears; no editor/overlay; exit removes icon and hotkey | NOT RUN |
| Mixed DPI pixels | Capture a known checkerboard at 100/125/150/200%; compare saved pixel size and edges | NOT RUN |
| Negative/portrait origins | Repeat on left/above-primary and portrait screens | NOT RUN |
| Work area/taskbar | Check left preview with taskbar on each edge, small display, nonprimary monitor | NOT RUN |
| Preview focus | Another app owns focus; preview arrival must not activate/minimize it or intercept keys | NOT RUN |
| Overlay cancellation | Repeated Escape, zero-size drag, focus loss; no copy/file/new preview | NOT RUN |
| Cross-monitor boundary | Drag beyond capture monitor; image must remain correctly clamped | NOT RUN |
| Own-window exclusion | Capture with editor, preview and pins open; none appears in image; hidden windows return without activation | NOT RUN |
| Display disconnect | Disconnect/change resolution during acquisition/overlay/preview; no stale delivery or crash | NOT RUN |
| Clipboard interoperability | Copy redacted crop, paste into Paint/browser/chat; inspect pixels and transparency | NOT RUN |
| Clipboard contention | Another app holds clipboard; bounded friendly error, no false success | NOT RUN |
| Hotkey conflict | Reserve requested combination; settings explains conflict; tray remains functional; old binding preserved | NOT RUN |
| Editor gestures | All tools, every resize handle, pen dot, reverse drags, undo crop, text edit, zoom/pan | NOT RUN |
| Text shortcuts | Ctrl+C/Z, Delete, arrows while typing must edit text normally | NOT RUN |
| Unsaved close/exit | Save, Discard, Cancel on window close and tray Exit; canceled save keeps document | NOT RUN |
| Save failures | Permission-denied/unplugged folder/existing file; no data loss or misleading output | NOT RUN |
| Pin | Nonactivating appearance, move/resize/close, capture exclusion, count cap | NOT RUN |
| Accessibility | Keyboard traversal, visible focus, Narrator names, contrast, high contrast and live theme switching | NOT RUN |
| Motion | Disable system animations/enable reduced motion; no sliding, capture never delayed by animation | NOT RUN |
| Fullscreen/HDR | Borderless vs exclusive games, HDR and protected surfaces; limitations/error recovery | NOT RUN |
| Resource lifecycle | Measure idle CPU and working set after repeated captures/cancels/editor/pin closes | NOT RUN |
| SDK build | Restore/build/publish with supported SDK and runtime; fallback does not prove MSBuild path | NOT RUN (SDK absent) |
| App launch | Execute packaged SlipShot.exe and verify apphost/runtime resolution | NOT RUN (gaming constraint) |

## Five-minute return checklist

1. Launch `artifacts/portable/SlipShot.exe`. Find the tray icon. Open Settings; check hotkey and default privacy preferences.
2. Capture a small region of a harmless document. Verify bottom-left preview on that monitor, no focus theft, hover pauses expiry. Escape a second capture and confirm nothing new appears.
3. Edit: add arrow, text and redaction; crop; move/resize an annotation; undo/redo. Zoom and pan. Verify typing shortcuts stay within text.
4. Copy into Paint and save PNG. Reopen the PNG, inspect redaction and dimensions. Try saving over it and cancel. Close an edited image with Cancel, then Discard.
5. Pin a capture, capture again to check exclusion, close pin. Exit from tray and confirm icon/hotkey cleanup. Record any failure before attempting gaming/fullscreen testing.

Record observed hardware/DPI/app versions next to completed checks. Mocked/native-policy unit tests do not prove native capture correctness or production readiness.
## Theme update: 0.2.8-preview.1

See [theme review](docs/THEME_REVIEW.md) for three audit passes. 134 serial synthetic tests pass; 36 offscreen WPF states generated. A dark-to-light editor switch matches a fresh light editor pixel-for-pixel after forced resource replacement. No real desktop screenshot or clipboard content was used.

| New desktop check | Status |
|---|---|
| Light/Dark/System change with preview, editor and pin open | NOT RUN |
| Windows 11 title bar and themed tray menu | NOT RUN |
| Windows 10 native title-bar fallback | NOT RUN |
| High contrast and screen-reader behavior | NOT RUN |
| Real checkbox/dropdown/textbox/scrollbar keyboard and mouse input | NOT RUN |
