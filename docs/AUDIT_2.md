# Audit 2 — privacy, resource ownership, and lifecycle

Date: 2026-10-30. Method: read-only source review of all `src/LeftShot` files, with focus on `AppController`, native capture, export, clipboard, settings, and retained images. This review did not build or run the application, open a window, register a hotkey, read the desktop, or access the OS clipboard. Findings below describe the source snapshot reviewed; line numbers may move during fixes. This is not a production-readiness or native-behavior certification.

## Findings

### P1 — Save can overwrite a newly appeared file without consent

Location: `src/LeftShot/AppController.cs:170`, `:172`, `:175`; `src/LeftShot/Services/ImageFiles.cs:53`.

After the save dialog returns, the controller derives `overwrite` from a fresh `File.Exists`. If another process creates the selected path after the dialog checked it, that fresh existence result becomes permission to replace the new file. `ImageFiles.Save` correctly honors the permission supplied by its caller, so the defect is the controller's inference of consent. The dialog's prior confirmation cannot cover a file it never saw.

Fix direction: keep permission independent of existence. Attempt a non-overwriting move first, and explicitly ask before retrying replacement when an existing destination blocks it. Parent implementer already identified this race. A confirmation does not provide filesystem compare-and-swap semantics; do not promise protection against a destination continuously modified by another process.

### P1 — Display-change cancellation does not reject an in-flight capture's delivery

Location: `src/LeftShot/AppController.cs:85`, `:86`, `:87`, `:94`, `:99`, `:208`; `src/LeftShot/Core/CaptureGate.cs:23`, `:28`.

`DisplaysChanged` calls `captureGate.Cancel()`, but `CaptureRegionAsync` checks only `disposed` and its separate local token before showing the selection window and publishing the cropped image. The gate's canceled result is returned only from `TryComplete`, whose return value is ignored. A monitor change while the native task is pending can therefore produce a selection overlay tied to the old monitor snapshot; a subsequent selection can enter the queue or automatic clipboard path despite cancellation.

Fix direction: cancel the active per-capture token on display changes and check that cancellation before constructing/showing the overlay and before publishing its result, or expose and enforce the generation's delivery validity. Preserve the occupied gate until an already-running native call exits.

### P2 — Compositor wait is outside the capture timeout and runs on the UI thread

Location: `src/LeftShot/AppController.cs:83`, `:84`, `:85`; `src/LeftShot/Services/Native.cs:28`.

`DwmFlush()` runs synchronously before the task observed by the three-second `WaitAsync`. That timeout cannot bound a stalled compositor call, so the UI can become unavailable before its recovery path begins. This is an unbounded wait visible in source, not a reproduced DWM hang.

Fix direction: include compositor synchronization in the background operation covered by the existing timeout/gate. A timeout should restore the UI while keeping the native worker accounted for; do not claim managed cancellation forcibly terminates a native call.

### P2 — Clipboard failure is reported as editor success

Location: `src/LeftShot/AppController.cs:155`, `:159`, `:160`; `src/LeftShot/UI/EditorWindow.cs:843`.

The controller catches clipboard failure or cancellation and completes its `Task` successfully. The editor then reports that the flattened image was copied. The early return when another copy is already in progress has the same effect. A user may close a document believing its latest pixels reached the clipboard even though the clipboard still holds older content.

Fix direction: propagate failure or return an explicit success result. Only successful clipboard completion should cause a success message. Keep cancellation neutral rather than turning it into a successful copy.

### P2 — The retained-memory estimate is not a 384 MiB peak-memory bound

Location: `src/LeftShot/AppController.cs:75`, `:95`, `:96`, `:152`, `:207`; `src/LeftShot/Core/PreviewQueue.cs:9`; `src/LeftShot/Services/DesktopCapture.cs:34`, `:44`; `src/LeftShot/Services/ImageFiles.cs:32`.

The estimator counts editor sources, `latest`, pins, and a conservative per-pending-item allowance. It does not count `queue.Current` when that image differs from `latest`, or outstanding native/cropped/render/export buffers. Admission checks compare the already-retained estimate with the threshold instead of accounting for the next allocation. Each allowed 32-megapixel capture can temporarily require a DIB, a managed pixel array, a WPF bitmap, and later a detached crop. Export adds another rendered image and codec buffers.

The queue/editor/pin counts and per-capture pixel cap do impose bounds on object counts. They do not establish a 384 MiB process-memory ceiling. Count unique current/pending/editor/pin images and use a conservative allocation allowance before new capture/pin work. Document transient codec/WPF overhead and confirm actual peaks in a permitted native memory test.

### P2 — Deferred editor closing loses an Exit request

Location: `src/LeftShot/AppController.cs:146`, `:195`, `:202`; `src/LeftShot/UI/EditorWindow.cs:856`, `:871`.

The editor cancels the initial closing event to offer Save/Discard/Cancel, then schedules a later `Close` after Save or Discard. Immediately after the initial `editor.Close()` returns, `TryExit` sees that the editor remains in the list and resets `exitRequested` to false. The later `Closed` event no longer continues shutdown. The app can remain resident after the user chose Exit and completed the save/discard flow.

Fix direction: distinguish a rejected close from a deferred approved close through an explicit close result/task, or coordinate the complete exit sequence asynchronously. Keep Exit pending while an approved close completes, and clear it only on actual cancellation or failed save.

### P3 — Preview expiry uses adjustable wall-clock time despite a monotonic helper

Location: `src/LeftShot/UI/PreviewWindow.cs:38`, `:51`, `:52`; `src/LeftShot/Core/PreviewLifetime.cs:16`.

The actual window subtracts `DateTime.UtcNow` differences. Clock correction can expire it immediately or extend its retention unexpectedly. The existing `PreviewLifetime` helper documents monotonic elapsed time but is not used by this window in the reviewed snapshot. Wire a `Stopwatch` into that helper and verify hover/keyboard pause, hidden-window pause, and reduced-motion dismissal against the same lifecycle logic.

## Source-supported safeguards

- No application HTTP clients, upload endpoints, telemetry calls, subprocess launch, updater, or startup-registration write were found in the application source. Settings persist beside the portable app; screenshot writes occur in the explicit save path. This is bounded source evidence, not a network trace of Windows or its clipboard services.
- `DesktopCapture` limits one frame to 32 million pixels, checks cancellation before and after native work, freezes returned pixels, and distinguishes screen DC, memory DC, and GDI-object disposal. Its `finally` restores the selected object before handle disposal. The controller intentionally keeps a timed-out native task behind its capture gate and observes late task exceptions.
- The selected region is copied into a detached bitmap instead of retaining a `CroppedBitmap` dependency on the entire monitor. Original editor sources remain in memory for undo/editing; redaction does not erase those source pixels from process memory.
- `DocumentRenderer` draws opaque redactions after all other annotations. Copy/save/pin callers pass flattened renders. `ImageFiles` rerenders supplied pixels, creates a fresh encoder frame without original metadata, requires an opaque JPEG background, validates decoded dimensions, and only overwrites when its caller explicitly permits it.
- Export/settings temporary files use same-directory unpredictable names and `CreateNew`. Cleanup tracks whether this call created the file. Cleanup is best effort: a crash, power loss, or deletion failure may leave a temporary file containing the explicitly saved flattened image. There is no secure-erasure guarantee.
- Clipboard retries are bounded to four attempts for `CLIPBRD_E_CANT_OPEN`, retain the original STA dispatcher, and honor cancellation. Tests use an injected sink rather than the OS clipboard. Windows clipboard history/sync behavior is outside this source review.
- Settings loading caps input size at 64 KiB, rejects unsupported versions/invalid values, and falls back to defaults with a warning. Annotation history has entry and aggregate complexity limits; editors/pins and pending previews have finite count limits. These safeguards still need realistic memory measurements.
- Shutdown unsubscribes static system events, unregisters hotkeys, disposes tray/menu/icon resources, closes preview/overlay/pins, and cancels the lifetime token. Process termination is ultimately what ends a native worker that never returns; disposal does not forcibly abort `BitBlt`.

## Verification still required

No code changes or tests were executed for this audit. In a later explicitly permitted native session, verify mixed-DPI/negative-origin coordinates, display changes during capture, DWM timeout recovery, black/protected/exclusive-fullscreen behavior, focus/nonactivation, display-affinity limitations, GDI counts over repeated capture/cancel cycles, actual peak memory, clipboard contention, save races, and deferred exit confirmation. Existing source-level tests cannot establish those native results. Findings remain open until separately fixed and verified.

## Remediation follow-up

The parent implementer subsequently changed the source. A focused source re-read confirms these revisions; this section supersedes the open-source status above, without asserting native runtime proof:

| Finding | Source remediation status |
| --- | --- |
| Overwrite consent | Addressed: controller now asks an explicit Yes/No replacement question when the destination exists. A new-file save keeps `overwrite=false`, so a later-created destination causes a failure instead of silent replacement. |
| Stale capture delivery | Addressed: display changes cancel the active per-capture token; `CaptureGate.IsCurrent(generation)` is checked before showing selection and before publication. Display changes also clear the retained latest preview. |
| Unbounded UI compositor wait | Addressed: `DwmFlush` moved into the background capture worker covered by the timeout and occupied gate. Native cancellation still cannot forcibly interrupt it. |
| Clipboard false success | Addressed: the controller's editor-copy path propagates failures and rejects a concurrent copy. Preview-specific error handling is separate. |
| Retained-memory estimate | Mitigated and clarified: current and pending queue images are included; public `ResourcePolicy.CanAllocate` applies a projected 512 MiB pixel-buffer budget with three transient buffers. This is explicitly an estimate, not a measured process-memory cap; native/WPF/codec peak profiling remains deferred. |
| Deferred Exit | Addressed in source: editor `ClosePending` distinguishes the deferred accepted close, preserving `exitRequested` for the later close callback. |
| Wall-clock expiry | Addressed: the preview uses `Stopwatch` elapsed time and `PreviewLifetime`, including hidden/hover/focus pausing. |

The parent reported a successful full compile followed by 60 tests with 57 passing and three failures. Those failures exposed one actual service defect and two platform-specific test assumptions. `SettingsStore.Load` now explicitly catches `InvalidDataException` (unsupported settings previously escaped the fallback). The two locked-destination tests now accept either `IOException` or `UnauthorizedAccessException` while still requiring original bytes to remain unchanged and temporary files to be removed. Production file-write behavior was not changed to satisfy an exception-type assertion. These last adjustments require the parent's next test run; this reviewer did not build or run tests.
