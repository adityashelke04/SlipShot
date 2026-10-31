# Application bug review — 2026-10-31

SlipShot is a local Windows screenshot capture and annotation app. Three reviewers audited editor/core behavior, preview/window interactions, and services/native boundaries. The coordinator traced controller integration, build/release configuration and existing tests. Expected load: one desktop user within the existing editor, pin and image-memory limits.

Only reproduced bugs were changed. No features, redesign, dependencies or speculative refactors were added.

| Before | After |
| --- | --- |
| Straight lines and arrows resisted diagonal resizing or exported the wrong shape. | Resize handles and exported endpoints follow the new bounds. |
| Clicking a thick arrow's visible head could miss it. | The painted arrowhead can be selected. |
| Clicking empty corners around a numbered circle selected it. | Selection follows the circle. |
| Invalid editor colors containing a space silently changed annotations. | Invalid input preserves the annotation and undo history. |
| Copy/Save completion or the folder action during dismissal could leave an invisible preview alive. | The preview completes its dismissal. |
| Invalid JPEG background colors produced a misleading write-permission error. | The form identifies invalid colors before applying settings. |
| Reordering or changing the case of an unchanged hotkey caused a false conflict. | Equivalent spellings reuse the current registration. |
| Malformed function-key combinations silently selected a key. | Invalid combinations are rejected. |

Detailed coverage and root causes: [editor](audit-editor.md), [preview/UI](audit-ui.md), [services](audit-services.md).

## Verification

- Baseline: 134 tests passed.
- Each defect had a failing regression before its fix. Final default run: 150 discovered, 149 passed, zero failures/warnings, one explicit native hotkey test excluded.
- Final change review traced each modified function and its callers. It found the same dismissal reset in the folder action. Its additional regression failed before the matching one-line guard was applied.
- `scripts/build-fallback.ps1 -Test -VisualCheck` exited successfully with warnings treated as errors. It rebuilt `artifacts/portable/SlipShot.exe`, generated 36 offscreen WPF renders, and verified the live theme switch against a fresh render pixel-for-pixel. Compact light/dark editor, text controls, preview and settings error renders were visually inspected.
- `python scripts/source-audit.py`, `python scripts/repo-privacy-check.py` and `git diff --check` passed. These are bounded checks, not a complete security certification.
- A temporary native hotkey regression passed before the reviewers noticed PRODUCT.md prohibits unattended hotkey registration. Its binding was released immediately. It is now explicitly opt-in and was not run in final default verification. No desktop capture, app launch or real clipboard write occurred.

## Release limits

Public-release readiness is not established by this audit. Real capture, mixed-DPI monitor placement, native focus/keyboard input, clipboard and drag target interoperability, screen readers, and resource measurements still need attended acceptance under TESTING.md. This machine has the .NET desktop runtime but no SDK, so SDK restore/build/publish was not rerun locally. The fallback build is not a replacement for that release gate. Existing desktop/runtime limitations remain documented in README.md.
