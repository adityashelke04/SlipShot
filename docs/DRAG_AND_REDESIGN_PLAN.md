# Preview handoff and visual redesign

User-reported fault: pressing/dragging the preview could only trigger its Click handler; no Windows drag source or payload existed. Timeout paused on hover but knew nothing about a held pointer or an OLE drag leaving the window. This is an implementation gap, not a target-app permission diagnosis.

Chosen fix: threshold-based pointer gesture, click-to-edit retained, dismissal paused while held/dragging, fresh timeout on return, and FileDrop + Bitmap + PNG payload on explicit drag. Flattened PNGs are created only on deliberate drag, in portable data/drag-out. Accepted files survive late target reads/app exit. Cancelled files are removed. Owned older-than24h files are swept on subsequent drags; at32files/256MiB refuse safely rather than delete young handoffs. No persistent screenshot history is added.

Native nested-drag review: block capture, reopen/forget preview and tray exit while dragging; display-change close is deferred until OLE cancellation completes. UI must not open editor at drag-end. Receiving applications decide whether to accept an image attachment, file path or no drop. Elevated destinations may reject normal-integrity drops; the app never elevates to bypass this.

Design direction using Taste/Impeccable Operate guidance and UI UX Pro Max WPF checks: image-first graphite/silver surfaces, restrained jade accent, one filled primary action, compact original vector icons, stable toolbar groups, contextual properties and no default-open empty sidebar. Preserve native window chrome, tool names, keyboard navigation, reduced motion and high contrast. Reference sources are in REFERENCE_REFRESH.md; no proprietary components/assets copied.

Verification: synthetic payload+pixel+retention tests, gesture regression tests, physical-pixel zoom tests, complete existing suite, software offscreen light/dark rendering at normal and compact widths and Text/Rectangle/Redaction contexts. Real OLE drop into the user's terminal/IDE remains a manual acceptance check. Old running preview1 is not forcibly terminated because it may contain unsaved captures. New release uses a new immutable version folder.
