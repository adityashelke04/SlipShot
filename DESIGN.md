# Visual and interaction specification

Reading this as a compact Windows utility for focused work and gaming, with a calm native language. Mode: Operate. No dashboard, onboarding, marketing or animated decoration. User requirements override web-specific skill suggestions.

Tokens: Segoe UI 13 body, 12 secondary, 16 section, 24 title; spacing 4/8/12/16/24/32 DIP; control corners 8, floating surfaces 12. Dark background #17191D, panel #22252B, border #42464F, text #F3F4F6, muted #B8BEC8. Light background #F3F4F6, panel #FFFFFF, border #CCD1D8, text #20242A, muted #555E6C. Accent #257A68 light / #82D8BE dark. System high-contrast colors take priority. System theme default, explicit light/dark overrides.

Editor: normal native window chrome for reliable resizing and system menus. Compact wrapping tool strips, a large centered image canvas, status at bottom. Tool labels remain visible; original geometric icons complement labels. Stroke/fill/font controls grouped together. Standard focus rectangles plus accent selected state; disabled opacity .45. Borders and background changes supply hover/press feedback. No custom font downloads.

Preview: 312 DIP wide, bottom-left of capture monitor work area, 16 DIP inset, adaptive width on small displays. Aspect-correct thumbnail, Edit / Copy / Save / Pin, close. 180ms opacity/translation entrance, 120ms dismissal; reduced motion disables transitions. Preview never activates on appearance. Eight-second timeout pauses on interaction. One visible and up to three pending, drop oldest pending on overflow; most recent retained for tray reopen. UI explains memory-only retention.

Editor entrance: 180ms opacity only. No capture/save/copy waits for animation. Interactions use explicit hit targets, accessible names, keyboard focus. Text editing suppresses document shortcuts. Every surfaced action must work; unfinished optional actions stay absent.

Synthetic offscreen render review is allowed; actual windows, focus behavior, mixed-DPI native capture and accessibility remain NOT RUN until user returns.
