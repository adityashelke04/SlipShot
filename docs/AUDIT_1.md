# Audit 1: correctness and lifecycle

Scope: Core, Services/Native.cs, Services/DesktopCapture.cs, UI/SelectionWindow.cs, UI/PreviewWindow.cs, and capture integration in AppController.cs. Source inspection and synthetic test additions only; no native windows, desktop capture, hotkeys, clipboard, or application launch.

## Findings requiring integration fixes

1. **High: canceled capture generation can still be displayed.** `DisplaysChanged` marks CaptureGate canceled but the current capture flow only calls `TryComplete` in finally and ignores its result. A frame finishing after display change can create an overlay with stale geometry, and selected output can be delivered despite canceled generation. Check generation validity after each await and before delivery, or cancel a stored per-capture cancellation token on display changes. Keep the native-worker gate occupied until the worker finishes. Root notified immediately.

2. **Medium: retained image estimate omits current preview.** `RetainedBytes` counts latest, editor sources, pins, and pending queue reservations, but does not count `queue.Current` when it differs from latest. It can undercount a full 32-megapixel image (128 MB). Include current and use distinct references to avoid accidental double-counting. Runtime transient capture/export buffers also need headroom; the limit is a retention bound, not a process RSS promise.

3. **Medium: preview countdown uses adjustable wall clock and resets on interaction.** `DateTime.UtcNow` jumps affect timeout; hover/key entry resets eight seconds rather than pausing remaining time. A pure `PreviewLifetime` helper now accepts monotonic elapsed duration and interaction state. Root will wire it with Stopwatch and use its reduced-motion dismissal duration.

4. **Medium: all-black frame is indistinguishable from legitimate black content.** DesktopCapture rejects every all-black image as an error. This prevents capture of valid black screens. Protected-content limitations should be explained without claiming all black pixels prove failure; consider allowing the frame with a warning.

5. **Low: stale monitor placement on tray reopen.** Display changes clear queued previews, but `latest` retains original monitor geometry and scale. Reopening the latest preview after unplugging/rearranging a display can use stale placement. Resolve its placement to a currently connected monitor when reopening.

## Core fixes made in this audit

- Reject nonfinite coordinates, unsupported coordinates/sizes, invalid enum values, malformed collections, and zero-area crop requests before changing document state.
- Bound each state to 1,000 annotations, 100,000 points, and 100,000 text characters. Trim history by combined data cost as well as the 80-state ceiling, so long paths cannot multiply without a memory bound. Weak saved-state tracking does not retain an evicted snapshot.
- Added pure CaptureGate that rejects simultaneous sessions, rejects canceled/stale completions, and retains occupancy after cancellation until that generation terminates.
- Added pure PreviewLifetime that pauses without resetting, saturates countdown at zero, and supplies a zero-duration dismissal when motion is disabled.
- Strengthened pixel evidence: specific shape interiors, descending line direction, translucent highlighter, localized text glyphs, resized redaction edges, and PNG round-trip opacity. Existing every-tool smoke tests remain supplementary evidence.

## Source observations

- Native DC/HBITMAP handles have matching release functions; selected object is restored before disposal. Cancellation checks bracket the expensive copy. Frames are explicitly opaque and frozen before crossing threads.
- Overlay coordinates use actual rendered dimensions to map to source pixels, avoiding assumptions about DPI scaling. Selection is clamped to one monitor, rounded outward, and native placement uses physical screen coordinates including negative origins. Real mixed-DPI placement is still unverified.
- Redaction rendering ignores supplied color/alpha, expands fractional bounds outward to pixels, clips to crop, and runs after other annotations. Export creates a new flattened crop bitmap rather than attaching source metadata.
- Preview thumbnail reference is cleared on close; queue overflow drops oldest pending. Application-level latest/editor/pin references intentionally retain captures independently.
- Source inspection cannot establish foreground/nonactivation behavior, compositor timing, display-affinity behavior, focus accessibility, native failure cleanup under GPU stalls, or memory RSS on real captures.

## Verification status

Parent reported 18 original synthetic tests passing through the official Roslyn/runtime fallback before this audit. Added audit tests and fixes are ready for the parent's serialized compilation/test run; this agent did not execute builds. New tests were written before production changes, but a RED execution was not observed because builds are serialized by the parent. Native acceptance remains NOT RUN.

## Root integration verification

The later full suite passed 62 tests. Generation validity and active cancellation are checked before overlay/delivery; projected buffer accounting now includes distinct queue images. Preview uses Stopwatch + PreviewLifetime. Tray reopen refreshes the original monitor or falls back to a current monitor after disconnect; latest image is retained while pending previews are dismissed on display change. All-black frames remain deliberately rejected with a documented limitation, not a claim that black proves protected content. Native assertions remain NOT RUN.
