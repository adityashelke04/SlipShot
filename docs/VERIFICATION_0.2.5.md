# Save-once verification — 0.2.5-preview.2

2026-10-31 local date. Full optimized fallback build and synthetic suite passed: **114tests,0failures,0skips,0warnings**; final test wall time2.252seconds. Native desktop runtime8.0.11, x64; SDK absent and conventional SDK build still unverified.

Reproduced before fix: calling the real controller's ordinary Save three times for one unchanged document wrote3PNGs. The regression now observes1PNG. Two review-driven regressions also failed before correction: stale receipt after explicit overwrite, and absent sibling notification. All are now passing.

Tests execute real document/render/file-save paths in controlled test directories. UI tests construct actual unshown WPF controls, invoke Save, assert disabled Saved/single invocation, and verify failure allows retry. No HWND shown, production hotkeys, live clipboard or desktop capture. Source review found no remaining blocker; limits are in SAVE_ONCE_REVIEW.md.

Save receipts follow snapshot identity, not global pixel hashing. Separate captures or genuinely edited snapshots may have separate files. Explicit Save a copy as intentionally permits another export. Existing user files are not deleted. Missing files are checked on relevant interactions, not polled; external content replacement/path aliasing is not monitored.

Save, preview Save and Ctrl+S are dialog-free; only explicit Save a copy as/Ctrl+Shift+S invokes the picker. Original source/readme screenshots remain synthetic, reviewed and privacy-checked. Installed update is staged for a normal restart so the running session's unsaved work is preserved. User requested main: source history will be pushed to main and GitHub default branch set to main, preserving old master history.
