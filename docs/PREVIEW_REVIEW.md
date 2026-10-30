# Preview lifetime and transfer review

The reported symptoms were a capture card staying indefinitely, confusing behavior after a second capture, and unsuccessful paste/drag into another application.

## Evidence

At 2026-10-30 17:40:42 UTC, the coordinator observed only native window metadata: the running preview was visible, the pointer was outside it, and it was not the foreground window. No screenshot or captured-image content was read. The exact WPF pause flag was not observable, so a stale hover/focus flag remains a hypothesis rather than a demonstrated root cause.

Source review found that every timer tick paused the lifetime whenever WPF reported hover, keyboard focus, a held gesture, an active drag, or a hidden window. Passive hover/focus could therefore extend lifetime indefinitely. Dismissal also stopped the timer and relied entirely on an opacity-animation completion callback to close the window.

After capture B, the FIFO queue retained capture A as its current item. The controller restored A's hidden card and refused to create another card while A existed. Its Copy and Drag callbacks still referenced A, while the latest capture and optional automatic clipboard copy referenced B. A stuck A prevented B from appearing. A pending dismissal could also publish another queued card during asynchronous capture because publication had no capture-active guard.

Clipboard copy previously supplied only the platform bitmap representation. The retry loop already marshalled retries to its original STA dispatcher and bounded busy-clipboard retries. Drag supplied file, bitmap, and PNG representations, retaining accepted temporary files for late reads. These source observations do not establish that a particular receiving application accepts image paste or file/image drops.

## Chosen behavior

### Why ten seconds

The evidence does not establish one universally optimal screenshot timeout. [Microsoft Fluent](https://fluent2.microsoft.design/components/web/react/core/toast/usage) uses seven seconds for non-action confirmation toasts, permits hovering to pause, and says dismissible content should remain available elsewhere. SlipShot has optional actions and a latest-capture tray entry, so its card needs more opportunity to act than a bare confirmation. [W3C timing guidance](https://www.w3.org/WAI/WCAG22/Understanding/timing-adjustable.html) emphasizes enough time and equivalent access when temporary information disappears; this is useful design guidance, not a claim that a native app has been WCAG-certified.

Five to seven seconds is fast but can rush locating a destination. Thirty seconds on every capture obscures content too long, especially for gaming. Ten seconds is the chosen compromise, with a configurable2–60second duration, extra hover grace and protected active gestures. It is a product judgment, not a measured user-study result. Hover allowance is shared across entries and does not refill on every pointer movement. Manual actions and completed drags get a fresh timeout; passive presence does not.

The default preview duration is 10 seconds, with at most 20 additional seconds of aggregate passive hover/focus grace: 30 seconds total while visible without an active held gesture or drag. Genuine held gestures and active drags pause indefinitely. The timeout must finish independently of animation completion.

A successful new capture replaces the old card. Dismissal must not reveal older captures again. A cancelled capture retains the existing card. Preview publication is deferred while capture is active, and retired cards are not restored from the hidden-window list.

Explicit Copy publishes metadata-free bitmap and PNG representations. It does not publish text or filesystem paths as substitutes for image paste. Automatic copy remains governed by the user's setting, and optional copy failure must not misreport a successful capture as failed.

## Verification scope

Regression coverage should include passive grace exhaustion, real held/drag pauses, recovery after release, dismissal with motion disabled or interrupted, capture replacement/cancellation, deferred publication during selection, and stale close callbacks. Clipboard payload tests use only synthetic pixels and in-memory data objects on an STA thread: cropped opaque redaction, PNG dimensions and alpha, removed metadata/thumbnail, and unchanged source pixels. Existing busy/retry tests remain relevant.

The coordinator owns compilation and test execution. This review does not claim live clipboard interoperability, successful OLE drops into a particular target, or physical mouse/keyboard acceptance testing. No personal screenshot, clipboard contents, or drag-out images were inspected.

### Executed regression evidence

Two new lifetime tests first failed against the original implementation, reproducing indefinite passive pause. After fixes the full suite passes102tests. It covers passive expiry, active hold/drag protection, stale release recovery, timer-driven closure, replacement of captureA byB, PNG/bitmap identity and privacy. A real DispatcherTimer test closes an unshown PreviewWindow whose fade cannot render; its HWND is asserted zero, so this exercises the close fallback without a desktop window, capture or clipboard operation. Real target paste/drag remains NOT RUN.
