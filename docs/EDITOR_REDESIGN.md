# Editor redesign

The editor remains a native WPF window with standard resizing, system menus, keyboard focus, and document history. Its visual direction is an image-first graphite and mint workspace, with semantic theme resources supplying light, dark, and high-contrast variants.

## Layout

- The compact header carries the SlipShot wordmark and current pixel dimensions, grouped Undo/Redo, centered zoom controls, and Copy/Save at the right.
- A two-column tool rail keeps all twelve tools labeled and reachable without a full-width wall of buttons. Existing tool shortcuts and authored geometric icons remain.
- A contextual strip shows the controls that apply to the current tool or selected annotation. Color defaults remain editable before drawing. Stroke appears for stroked shapes, Fill for boxes and ellipses, and font/text controls for text. Delete appears when an annotation is selected; Reset crop appears when applicable.
- The image sits on the neutral `Canvas` surface with a single thin frame and a 20-DIP inset. The annotation list starts closed and can be toggled from the rail footer, alongside Pin.
- At narrow widths, contextual groups wrap together. The tool rail can scroll vertically on unusually short windows. The footer keeps contextual guidance available and exposes truncated messages in a tooltip.

## Behavior preserved

Selection, moving, eight resize handles, cropping/reset, arrow, line, rectangle, ellipse, pen, highlighter, text, steps, opaque redaction, panning, deletion, Undo/Redo, export callbacks, Pin, unsaved-close confirmation, limits, and document validation retain the existing implementation. Text input continues to suppress document shortcuts. No capture, clipboard, process, or save-service behavior was changed.

## Pixel inspection

Zoom is shown as image pixels relative to physical screen pixels. `100%` and Ctrl+1 use the current monitor's DPI so one source pixel occupies one screen pixel. Moving between DPI scales preserves that physical zoom, except in Fit mode, which recalculates for the available viewport. The renderer uses nearest-neighbor bitmap scaling at 100% and above, and high-quality downsampling below 100%.

`DipZoom(physicalZoom, dpiScale)` is the small arithmetic boundary for testing the conversion and 2–800% clamp. Mixed-DPI window movement still requires native hardware verification.

## Review status

Source review and `git diff --check` completed for the editor. The coordinating agent owns serial compilation, automated tests, and synthetic offscreen renders, including dark/light and compact-width review. No visible editor window was launched by this work. Actual keyboard focus, mixed-DPI movement, screen-reader behavior, capture, and export flows require a native interactive pass.
