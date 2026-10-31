# Frame-S application theme review — 0.2.8-preview.1

User selected Frame-S and requested application-wide colors, dark mode and the dark logo variant. This is a refinement of the native WPF utility, not a replacement frontend. Three audit passes were completed: initial whole-UI review, independent connected-code review, and automated/visual verification.

## Audit 1: coverage and hierarchy

Reviewed preview, editor, settings, pin, selection overlay, export menu, close confirmation, tray, tooltip and shared controls. Found missing in-content identity, bright default checkbox/scrollbar surfaces in dark settings, OS-colored auxiliary menus, weak primary-button feedback, invisible primary keyboard focus and contradictory historical design tokens.

Implemented Frame-S in preview/editor/settings; dark mark on light, orange on dark, system ink in high contrast. Updated shared WPF controls, flat export menu and WinForms tray rendering. Settings now has Appearance, Capture and Saving & export sections with a fixed Save/Cancel footer and focused inline validation. The small preview folder action remains next to Latest in tray.

## Audit 2: connected code and accessibility

Independent review checked control template parts, native lifetime, trigger precedence and theme changes. Fixed a horizontal scrollbar minimum-size issue. Replaced persistent editor brush assignments with dynamic resource references and added render-affecting resources for custom vector drawings. No static theme subscriptions, image transforms or new runtime dependencies were added.

Primary hover/press colors maintain readable dark text. Focus uses a contrasting inner stroke on primary buttons. Input outlines, error text, brand marks and selection focus are checked in both palettes. High contrast uses system resources. The flat MenuItem template intentionally supports the application's single-level export menu; future submenus/checkable items need an extended template.

Native title bars request DWMWA_USE_IMMERSIVE_DARK_MODE only on Windows 11 builds supporting the documented attribute; no HWND is created just for styling. Windows 10 and OS file/message dialogs retain system chrome. [Microsoft reference](https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/ne-dwmapi-dwmwindowattribute).

## Audit 3: verification

The serial STA suite passes 134 tests with zero failures, warnings or skips. Existing capture geometry, export/redaction, clipboard retry, save-once, preview lifetime and resource tests remain included. New coverage checks checkbox theme updates, readable primary focus/state colors, tray color refresh, scrollbar range/page-command mapping and offscreen auxiliary template creation.

The actual WPF software-render harness generates 36 synthetic images: both themes, three editor sizes, contextual tools, saved states, normal/compact/error settings, preview, pin, selection instructions, menu, tooltip, control sheet and a live theme-switch pair. Representative renders were inspected visually. The same editor switched dark → light with forced frozen-resource replacement matches a fresh light editor pixel-for-pixel. No production app window, screen capture or desktop clipboard is used.

Failures found and resolved during development: missing theme checkbox template; offscreen NUnit resource-owner/dispatcher setup; invalid tooltip TextWrapping attachment; invalid parentage in the menu fixture. Detached visual fixtures now register their resource owner explicitly. These intermediate failures are not reported as passing tests.

## Desktop checks still NOT RUN

- Keyboard tab/focus, Space toggling, dropdown navigation, text selection/caret and both scrollbar gestures in real windows.
- Appearance switching with an editor, preview and pin open; Windows system-theme and high-contrast changes.
- Tray-menu appearance and Windows 11 title chrome; Windows 10 system-chrome fallback.
- Screen reader announcements, physical mixed-DPI sharpness and icon-cache appearance.
- Native capture/drag/clipboard acceptance from TESTING.md remains outstanding where previously marked NOT RUN.

Five-minute review: restart normally; open Settings and cycle Light/Dark/System; capture a harmless image and inspect the preview; open editor, draw/undo/save; inspect folder access, tray menu and tooltips; pin then close; tab through Settings and test an invalid duration. Do not share personal screenshot fixtures.

## Skills and scope

Used Ponytail, Superpowers debugging/verification, Impeccable's native audit principles, Taste's refinement guidance and UI/UX Pro Max's WPF resource/accessibility search. Web/mobile-specific prescriptions were adapted to native WPF; no frontend framework or skill pack was installed. UI/UX Pro Max returned four relevant WPF guidelines. Impeccable's web detector does not validate WPF templates, so native source review and actual WPF software renders supply evidence instead.

The app remains unsigned, local-only and framework-dependent. An unattended pass is not production-readiness proof. SDK/MSBuild verification remains unavailable because this machine has the .NET 8 Desktop Runtime but no SDK.
