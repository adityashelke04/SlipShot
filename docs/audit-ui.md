# UI and preview audit

What this part does: SlipShot shows a temporary screenshot preview, lets the user select a capture region, and supplies settings and pinned reference windows. The review assumes one Windows desktop user, multiple open editors or pins, and asynchronous copy/save work.

## Confirmed bugs

| Before | After | Cause and check |
| --- | --- | --- |
| Dismissing a preview while Copy or Save was pending, or clicking the folder action during its fade, could leave its invisible window alive for another full timeout. | The preview keeps closing when pending work finishes or the folder action runs during its fade. | The action replaced the dismissal timer state. Copy, Save and folder regression cases failed before their guards; the fix preserves an active dismissal. |
| JPEG colors containing spaces could pass the settings form, then produce a misleading preferences-write error. | Invalid colors remain in the form for correction before settings are applied. | Hexadecimal number parsing allowed leading/trailing whitespace although the color converter rejects it. Regression cases cover both positions. |

## Reviewed

Read PreviewWindow, SelectionWindow, PinWindow, SettingsWindow, all Theme parts, Branding, PreviewQueue, PreviewLifetime, and PreviewGesture. Traced callers in AppController, coordinate placement in Native and ResourcePolicy, settings storage validation, and existing preview, gesture, lifetime, theme, and integration tests.

The changes keep existing appearance, actions, capture limits, and retention rules. No redesign or additional dependency is involved.

## Verification

The coordinator runs builds serially because all reviewers share the same output directory. Initial red run: 136 tests, original 134 passed, both pending-operation dismissal regressions failed as expected. The next run confirmed both dismissal cases pass after their fix and both malformed-color cases fail against the original validator because settings are applied. Further verification is recorded by the coordinator after the fixes.

Not checked interactively: mixed-DPI physical monitors, actual capture/clipboard/drop targets, screen-reader announcements, and native focus behavior. Synthetic tests and source review do not certify every Windows configuration for public release.
