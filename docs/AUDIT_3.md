# Audit 3 — Editor, accessibility, and UI acceptance

Date: 2026-10-30. Method: source review of the programmatic WPF editor and its model/renderer contracts. Synthetic offscreen render harness authored for root-agent execution. This report does not claim a native interactive test pass.

## Findings fixed in this audit

- **Small-display fit:** the former 850-DIP minimum could exceed a 1024-pixel display at 150% scaling. The editor now uses a 640 × 480 DIP minimum, bounded by the current logical work area, and bounds its initial size to that work area. The annotation sidebar automatically hides below 1000 DIP; the Annotations button can show it explicitly. Tool/action/property groups wrap.
- **Text resize:** changing only text Bounds did not change rendered text size. Text handles now scale font size and recompute natural text bounds. The font selector displays the resulting nonpreset size.
- **Redaction control truth:** exports always paint black, fully opaque redactions on top of every other annotation. Color controls now disable for redactions and display black. Layer order and canvas hit testing prioritize redactions to match the renderer.
- **Layer keyboard/contrast behavior:** native list navigation is preserved. Layer text inherits the container's selected foreground, long labels truncate, and horizontal scrolling is disabled. This avoids unreadable selection colors and oversize annotation rows.
- **Gesture cancellation:** Escape and lost capture discard the preview and refresh selection controls; only completed edits commit to history. A pen click can create a dot. Native window closing cancels a pending drag.
- **Validation failures:** document-limit rejections are caught at the editor boundary and shown in status without replacing the document. The editor caps annotation count at 1000 and caps each in-progress stroke and aggregate point budget before commit.
- **Async close integration:** the editor exposes ClosePending, guards duplicate save/close actions, and defers a completed close until the Closing handler returns. Canceling a save leaves the editor open; successful save alone marks the document saved.

## Architecture and acceptance review

DocumentViewModel owns the observable selected tool, annotation selection, document reference, busy flag, and undo/redo/dirty availability. Window title and Undo/Redo enabled states bind to this model. EditDocument owns immutable source pixels, snapshots, history, and saved-state tracking. Pointer capture, view coordinates, temporary previews, and layout stay in EditorWindow; putting those in the document model would couple image state to WPF interaction mechanics.

Source review confirms:

- Select, crop, arrow, line, rectangle, ellipse, pen, highlight, text, numbered steps, opaque redaction, and pan all have handlers; tool labels stay visible and use authored geometric icons.
- Selection supports move, eight resize handles, layer-list selection, deletion, and keyboard nudging (1 px or Shift+10 px).
- Source-pixel coordinates are independent of canvas zoom. Fit, 100%, bounded zoom, Ctrl+wheel zoom, middle-drag pan, and Space+drag pan share the same document renderer used for export.
- Undo/redo commits once per pointer gesture. Text/color/stroke/fill changes commit only when explicitly applied or chosen.
- Copy does not mark the document saved. Save/Discard/Cancel protects edited documents on close. Failed or canceled save preserves edits.
- TextBox and ComboBox focus suppress document keyboard shortcuts. Controls have visible names/tooltips; the canvas and layer list carry automation names. The UI uses native window chrome and normal controls.
- Reduced-motion entrance behavior is delegated to Theme; no editor interaction waits for animation.
- Core validation and the editor's capped preview point buffers bound edit growth. History memory limits remain the responsibility of EditDocument.

## Authored checks

EditorTests covers all eight resize handles, minimum-size/no-flip behavior, resizing a horizontal line, and observable undo/redo/busy/dirty state. These checks require the root build/test runner; this audit did not execute them.

scripts/VisualCheck.cs renders actual detached editor content at 1240 × 820, 850 × 650, and 640 × 480 in light/dark themes. The image is synthetic and contains no desktop content. The harness does not call Show, EnsureHandle, screen capture, clipboard APIs, or hotkey registration. The root agent must compile/run it and inspect artifacts/visual before recording visual results.

## Native acceptance — NOT RUN

The following remain **NOT RUN**, as unattended desktop interaction is prohibited: keyboard focus traversal in a shown window; screen reader announcements; mouse capture and lost capture on real windows; native close/save dialog sequences; monitor movement and mixed-DPI behavior; real high-contrast switching; clipboard/capture/hotkey integration; pin-window activation and game focus behavior. Synthetic layout rendering cannot establish any of these behaviors.

Known scope limits: resizing is pointer-driven, with keyboard nudging but no keyboard resize shortcut; text is edited in the toolbar rather than in an inline rich-text overlay; the editor does not expose z-order changes, rotation, or rich-text formatting. None of these controls is shown as an inactive feature.
