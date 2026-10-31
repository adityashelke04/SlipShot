# SlipShot visual and interaction specification

Current identity: 0.2.8-preview.1. Mode: Operate. A compact native Windows utility for focused work and gaming; the screenshot stays central. Historical specifications remain in Git history.

## Identity

Frame-S is the user-selected original mark: opposing capture brackets form an S. Exact-color vector masters are under `src/SlipShot/Assets`; generated concept boards are references, not production icons. UI headers use the dark variant on light surfaces and orange on dark surfaces. Windows executable, title and tray icons use orange on a charcoal tile, with nine size-specific frames. No trademark clearance or Anthropic affiliation is claimed.

Orange #D97757 and charcoal #141413 follow the user-requested palette. Provenance: [Anthropic's published palette](https://github.com/anthropics/skills/blob/main/skills/brand-guidelines/SKILL.md). All geometry and UI implementation are original.

## Tokens

| Role | Light | Dark |
|---|---|---|
| Surface | #FAF9F5 | #141413 |
| Panel | #FFFFFF | #20201E |
| Canvas | #E8E6DC | #10100F |
| Ink | #141413 | #FAF9F5 |
| Muted | #625F57 | #B0AEA5 |
| Decorative divider | #D8D5CC | #3A3A36 |
| Input outline / scroll thumb | #868279 | #79776F |
| Accent | #D97757 | #D97757 |
| On accent | #141413 | #141413 |
| Primary hover / pressed | #E38B6D / #CB7053 | #E38B6D / #CB7053 |
| Focus | #A04F35 | #D97757 |
| Selected | #F4E4DC | #35241F |
| Hover / pressed | #EFEDE6 / #EAD7CC | #30302C / #453027 |
| Error | #A33225 | #FFB4A5 |

Typography: installed Segoe UI; 13 body, 11–12 secondary, 15–16 sections, 24 settings title. Spacing: 4/8/12/16/24/32 DIP. Controls use 8-pixel corners; preview uses 12. Native WPF behavior and automation peers are retained. No downloaded fonts or styling dependencies.

Primary fills use dark text and an inner dark keyboard-focus outline. Secondary controls have quiet hover/press surfaces and visible focus edges. Disabled opacity is 0.45. Shared templates cover inputs, checkbox, dropdown, tooltip, flat export menu, annotation selection and both scrollbars. Settings validation is announced and targets the invalid field.

High contrast replaces brand colors with Windows system colors, uses the system tray-menu renderer and disables animation. System appearance is default; explicit Light and Dark are available. Existing windows use dynamic resources, including retained drawing icons. Windows 11 title bars request the documented dark preference; Windows 10 and native OS file/message dialogs retain system chrome. See [Microsoft DWM attributes](https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/ne-dwmapi-dwmwindowattribute).

## Surfaces

- Preview: 328 DIP, adaptive to small work areas, bottom-left with 16 DIP inset. Frame-S + Capture ready, aspect-correct thumbnail, drag hint/dimensions and one Edit/Copy/Save/Pin row. A 28×28 folder box immediately follows Latest in tray.
- Editor: compact identity/undo/zoom/export header, two-column labeled tool rail, contextual controls, optional annotations panel and image-first canvas. 100% means one source pixel per physical pixel. Document annotation colors never change with the theme.
- Settings: three flat sections—Appearance, Capture, Saving & export—with scrolling body and fixed Save/Cancel/error footer. Fields remain accessible at 360×400. Preferences persist only on Save settings.
- Pin: native resizable frame, theme-aware background, unchanged image pixels and native close/move controls.
- Capture: dimmed frozen desktop image, white selection and near-black instruction plate. It remains legible over arbitrary image colors, independently of appearance.
- Tray: native menu behavior with shared warm-neutral colors and system rendering in high contrast.

## Motion and lifetime

Preview entrance: 180 ms opacity/translation, dismissal: 120 ms. Editor entrance: 180 ms opacity. Hover/press feedback: 80–100 ms, with sustained state colors even under reduced motion. No capture/copy/save waits for animation. Reduced motion eliminates movement.

Preview default: 10 seconds, with up to 20 additional seconds of aggregate passive hover/focus grace. Active holds/drags/copy/save protect the operation. One card shows the latest capture; previous cards do not resurface. Latest remains available in the tray. Dismissal never deletes a saved file.

## Evidence and limits

See `docs/THEME_REVIEW.md` for three audit passes, regression results and remaining desktop checks. Synthetic renders do not establish native HWND appearance, screen-reader usability, physical DPI correctness or production readiness.
