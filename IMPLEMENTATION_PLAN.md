# LeftShot implementation plan

Goal: a complete independent native capture/edit/export source delivery with synthetic verification, and runnable output if available tooling permits.

Architecture: one small WPF app, plain immutable document records with source-pixel coordinates, shared WPF drawing/flattening, isolated Win32 capture/hotkey layer, injectable clipboard and storage. UI uses a document view model and event-driven pointer gestures. No runtime third-party dependencies. NUnit synthetic tests run serially on STA.

Environment: empty F:/SlipShot, no inherited AGENTS.md observed, Git identity exists, ~84 GiB free. .NET 8.0.11 x64 desktop runtime installed; SDK absent. Do not install an SDK. Investigate a small official Roslyn compiler package fallback using existing runtime, keep all packages on F:. Normal SDK build remains an explicit prerequisite.

- [x] A: Inspect, research references/REA availability, read requested skills, define visual spec and contracts, initialize Git.
- [x] B: Write geometry/document/render tests; implement immutable source, annotation model, bounded undo and shared flattening. Commit foundations with accurate verification status.
- [x] C: Implement disposable GDI still capture, single-monitor overlay, timeout/cancellation, hide-before-capture, native nonactivation and bounded left preview. Commit capture and preview milestones.
- [x] D: Implement crop, drawing tools, selection/move/resize, text, pan/zoom, shortcuts, copy/save and close guard. Commit editor and export milestones.
- [x] E: Settings with safe recovery, tray, hotkey conflicts, cleanup, original vector identity. Commit integration/polish.
- [x] F: Three audits: correctness/lifecycle; privacy/security/resource bounds; UI/accessibility/acceptance. Fix findings, run all available synthetic tests serially, no interactive app launch.
- [x] H: Release output, architecture/license/test/usage documentation and honest deferred desktop matrix. Actual native and normal SDK acceptance remain blocked/deferred as documented.

Review focus: monitor origins and scale; captures racing cancellation; source leakage through redaction/export; failed writes/clipboard busy; text focus/unsaved-close loss. Test synthetic behavior and defer native claims.

User explicitly waived optional questions and chose autonomous implementation; skill approval gates are superseded. No remote push/publication. All commits are local and stage exact project paths.
