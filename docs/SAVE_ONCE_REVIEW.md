# Save-once review

Reviewed: 2026-10-31. Verdict: Ship. No remaining blocking finding in the reviewed save-once changes.

## Scope and behavior

Reviewed `EditDocument`, `ImageFiles`, `AppController`, `EditorWindow`, `PreviewWindow`, and the save-deduplication and preview tests. This is a source review for the single-user WPF application; application save operations run on its UI thread.

An unchanged document snapshot reuses its existing saved file. The controller checks that receipt before the rendering memory budget, so a repeated save does not render, write another PNG, or open an error dialog because of that budget. Editing creates a separate snapshot and a subsequent ordinary Save creates a new file. Undo and redo return to the receipts belonging to their respective snapshots.

The retained capture and editor forks share save receipts, frozen source pixels, and immutable initial snapshots. They have separate edit histories. Discarding an editor's changes therefore does not change the retained capture or a reopened editor. Explicit Save a copy as remains available, including its existing overwrite confirmation.

## Findings resolved

1. **Receipts for overwritten files:** `MarkSaved(path)` invalidates other snapshot receipts for that path. An invalidated receipt remains distinguishable from the original clean, never-exported state, so undoing to the older version correctly makes it dirty. `RecordSavedFile` also invalidates matching receipts in other live capture groups before recording a successful explicit export. The controller supplies open editor documents and the retained latest capture.
2. **Deleted exports:** `RefreshSavedState` invalidates a receipt when its file is missing. Refresh calls cover preview creation/reopening, editor opening and activation, editor closing, preview mouse entry, and ordinary save. Invalidations notify the UI, allowing another save and preserving the close warning for a missing saved version.
3. **Stale sibling controls:** receipt owners are weak references, and receipt changes notify all live document owners. Preview and editor controls can therefore update when another fork saves their shared snapshot. Closed windows unsubscribe their document handlers; the receipt owner list does not strongly retain abandoned documents.
4. **Repeated-save error prompts:** the controller's saved-file return precedes the allocation check and saving guard. Repeated ordinary saves of an existing version complete without either rendering or an additional notification/dialog.

File writes still complete before their successful receipt is recorded. A failed initial export leaves the document unsaved and available for retry. Save As cancellation and rejected overwrite confirmation return before file writes and receipt changes.

## Evidence

The coordinating implementer reports 114 passing tests after the fixes, including reproductions of the earlier overwrite and shared-notification failures. This reviewer inspected the tests but did not rerun them.

The inspected regression tests cover three calls to the actual controller save method producing one PNG; shared receipts with independent edits; saved undo/redo states; failed writes and retry; deleted-file restoration; same-capture and cross-capture overwrite invalidation; and sibling change notification. Preview tests invoke actual, unshown button controls and check that success disables repeated Save while failure permits retry.

## Limits

This review launched no application windows, builds, clipboard operations, or screenshots, and made no commit. Native Save As dialogs and visible interaction were traced in source, not exercised interactively.

Missing-file detection happens at the listed interaction points, not through a filesystem watcher. Receipts establish snapshot identity and file existence; they do not hash or monitor files for outside content replacement, path aliases, or changes made by another process. Separately recreated but pixel-identical edit snapshots are not content-deduplicated. These limits do not block the requested repeated-save behavior.
