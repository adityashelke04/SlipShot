# Visual and interaction specification

Reading this as a compact Windows utility for focused work and gaming, with a calm native language. Mode: Operate. No dashboard, onboarding, marketing or animated decoration. User requirements override web-specific skill suggestions.

Tokens: Segoe UI 13 body, 12 secondary, 16 section, 24 title; spacing 4/8/12/16/24/32 DIP; control corners 8, floating surfaces 12. Dark background #17191D, panel #22252B, border #42464F, text #F3F4F6, muted #B8BEC8. Light background #F3F4F6, panel #FFFFFF, border #CCD1D8, text #20242A, muted #555E6C. Accent #257A68 light / #82D8BE dark. System high-contrast colors take priority. System theme default, explicit light/dark overrides.

Editor (0.2 redesign): native window chrome, compact identity/undo/zoom/export header, a narrow two-column labeled tool rail, contextual color/stroke/text controls, and an image-first canvas. The annotations panel is closed by default. No wall of outlined buttons. Only the primary action is accent-filled; other controls use quiet hover surfaces. Selected tools retain a tinted background and clear accent edge. Original vector icons complement labels. Standard focus rectangles plus explicit focus borders; disabled opacity .45. No custom font downloads. 100% means one source pixel per physical screen pixel; DPI changes preserve that zoom.

Preview (0.2 redesign): 328 DIP wide, bottom-left of capture monitor work area, 16 DIP inset, adaptive width on small displays. Compact crop mark + Capture ready header, image on a recessed canvas, dimensions and drag hint, segmented Edit / Copy / Save / Pin. Close uses a small accessible vector button. 180ms opacity/translation entrance, 120ms dismissal; reduced motion disables transitions. Preview never activates on appearance. Eight-second timeout pauses on hover, held pointer and OLE drag, then refreshes after a gesture. Click edits; crossing the system drag threshold hands off a standard Windows payload without opening the editor. Footer/tooltip disclose temporary PNG creation on explicit drag. One visible and up to three pending, drop oldest pending on overflow; most recent retained for tray reopen.

Current palette: dark Surface #191C20, Panel #22262B, Canvas #101316, Border #363D43, Ink #F0F3F5, Muted #A3ADB5, Accent #9DE4C7, OnAccent #142C23, Selected #2A403B. Light Surface #F7F8FA, Panel white, Canvas #E9EDF0, Border #DDE3E7, Ink #1E272C, Muted #5F6C75, Accent #197456, OnAccent white, Selected #E2F2EB. These replace the initial palette above; high contrast still overrides them.

Editor entrance: 180ms opacity only. No capture/save/copy waits for animation. Interactions use explicit hit targets, accessible names, keyboard focus. Text editing suppresses document shortcuts. Every surfaced action must work; unfinished optional actions stay absent.

Synthetic offscreen render review is allowed; actual windows, focus behavior, mixed-DPI native capture and accessibility remain NOT RUN until user returns.
