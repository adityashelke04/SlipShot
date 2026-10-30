# Progress

## Current: 0.2.0-preview.1

User requested drag-and-drop plus full UI redesign, then authorized creating private GitHub repo. Created https://github.com/adityashelke04/LeftShot (PRIVATE), origin/master tracks correctly; ordinary pushes verified. Source change3ea28fc pushed. Publishing preflight in scripts/check-publish.ps1.

Implemented threshold drag gesture, standard FileDrop/Bitmap/PNG payloads, explicit-drag temporary file lifetime with ownership/bounds, hover/hold/drag timeout suspension and safe nested-close handling. Redesigned preview and editor with compact header/tool rail/context controls/optional inspector, physical-pixel zoom and corrected primary-button contrast. Independent source review found no blocking defect; actual target drag interoperability still NOT RUN.

Latest serial fallback build:80passed/0failed/0skipped;379assertions;16synthetic UI fixtures rendered, representative full/compact/context/preview states inspected. No production capture/clipboard/native input tests performed. Existing preview1 process remains running; do not kill it or discard unsaved work. User must Exit old tray instance normally before launching new release. Packaged immutable0.2.0-preview.1: folder359,397bytes, ZIP158,937bytes; all payload checksums verified. docs/VERIFICATION_0.2.md records evidence. All outputs F:/SlipShot; new source and documentation pushed privately. Next check is real target-app drag interoperability.

## Previous delivery (historical)

Implemented capture/selection/left preview, complete annotation editor, clipboard/export, settings/tray and pins. Local milestone commits recorded. Three audits completed; root integrated cancellation, memory-accounting, overwrite, failure-reporting, timeout, close and theme fixes.

SDK unavailable; official Roslyn + installed .NET8 runtime fallback compiles optimized x64 apphost/executable. Final observed suite:62 passing,0 failures,223 assertions,1.296s. Ten synthetic offscreen editor/preview/settings images rendered and inspected; no actual windows shown. Native desktop acceptance and SDK/MSBuild remain NOT RUN. All generated output/cache/temp remains in repository on F:.

Release0.1.0-preview.1 packaged immutably under artifacts/releases with per-file and ZIP checksums. Release folder308,372bytes; ZIP137,471bytes. docs/VERIFICATION.md records exact evidence and limitations. Next person should run the five-minute checklist and record native observations; obtain a supported SDK for the conventional build. Do not run app or desktop tests until user returns. No remote publication performed.
