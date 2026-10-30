# Preview lifetime / repeated-capture verification

Version0.2.3-preview.1,2026-10-30.

- Observed installed preview still visible with pointeroutside and notforeground using native window metadata only. Exact stale WPF flag could not be inspected; no personal pixels read.
- Confirmed source mismatch: a second capture updated latest but FIFO retained the first card/callbacks. Confirmed missing capture-active publication guard and animation-only close dependency. Review and timing rationale: PREVIEW_REVIEW.md.
- First regression run:2new passive-expiry tests failed against original logic. Fixed shared hover grace, native input qualification, missed-release recovery and timer-based closure.
- Final `./scripts/build-fallback.ps1 -Test -VisualCheck`: **102passed,0failed,0skipped,0warnings**; test duration2.262seconds. Optimized x64 compilation with nullable warnings treated as errors. No SDK installed; conventional SDK path remains unverified.
- Real DispatcherTimer/unshown PreviewWindow test asserted zero HWND before dismissal, then observed Closed without a rendering-driven completion event. No test window was shown.
- Added newest-capture pixel regression, bitmap+PNG privacy/alpha checks, replacement and capture-busy state tests. Existing retry/cancellation/drag-lifetime tests pass. Auto-copy remains opt-in; capture-only is not a paste command.
- Sixteen synthetic layouts rendered. Updated preview demo/footer inspected; only synthetic PNG copied into documentation. Static PE/source and repository privacy checks passed.
- Packaged ZIP SHA256: `d8d0c792ea4a0f8c782a9d45fb7599d5dca7dd72dc57c173973510fa4eac3fb3`.
- Installed20verified payload files under `F:/Apps/SlipShot/versions/0.2.3-preview.1`, preserved existing preferences with timeout10, and redirected Start menu shortcut. The existing process is intentionally not terminated; activate via normal tray Exit then Start. No screenshot or drag-out files copied.

Manual acceptance still needed:10second no-interaction closure, parked hover around30seconds, stale focus after Copy, long active drag, two rapid captures then Copy/drag into the user's target, capture while prior card is fading and display changes. No real clipboard write, desktop capture, OLE target drop, mouse movement or gameplay benchmark was performed. Synthetic/source evidence does not establish universal target compatibility or production readiness.
