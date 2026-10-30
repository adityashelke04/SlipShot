# Verification — 0.2.0-preview.1

2026-10-30. Optimized x64 WPF build using the documented official-compiler fallback and installed .NET8.0.11 runtime. Normal SDK/MSBuild verification remains blocked by the missing SDK. No SDK installation or runtime upgrade performed.

## Executed checks

- `./scripts/build-fallback.ps1 -Test -VisualCheck`: successful application, apphost and test compilation, nullable warnings treated as errors. **80tests passed,0failed,0skipped,0warnings;379assertions**. Final NUnit run15:32:37–15:32:38UTC,1.779190seconds test-suite wall time. Not an application performance measurement.
- Sixteen software offscreen layouts rendered: light/dark editor at1240×820,850×650,640×480, plus compact Text/Rectangle/Redaction contexts, preview and settings. Representative full/compact/context/preview states inspected. Fixed primary-action text contrast and preview crop-mark color; Fit avoids enlarging a capture above100%; fractional enlargement uses smooth sampling, integral zoom uses crisp pixel sampling. No native windows shown by this harness.
- Added transfer tests verify file/bitmap/PNG representations, redaction pixels, omitted source metadata/thumbnail, late file/stream reads after accepted drop, cancel cleanup, owned-file expiry, active leases, retention caps and blocked destinations. Gesture tests cover click versus drag, one drag per threshold crossing, cancellation and timer pause outside the preview. Physical-pixel zoom tested across100/125/150/200% DPI.
- Independent connected-source review in DRAG_REVIEW.md found no blocking defect; native behavior is explicitly outside that verdict.
- Static scan of25 C# application files found none of the selected network/logging/placeholder/forced-foreground APIs. PE/XML checks passed; native GUI host is x64 with PerMonitorV2 manifest.
- Private GitHub setup confirmed: origin=https://github.com/adityashelke04/LeftShot.git, master tracks origin/master, repository visibility PRIVATE. Authenticated GitHub CLI already existed. Existing commits and the feature commit3ea28fc pushed successfully. Publishing preflight remote-read check passed. No public conversion or binary upload.

## Release inventory

`artifacts/releases/0.2.0-preview.1/win-x64/LeftShot.exe`

- App payload292,969bytes: DLL153,600 + EXE139,264 + runtimeconfig105.
- Portable release folder359,397bytes across16files, including docs/licenses/checksums.
- ZIP158,937bytes.
- ZIP SHA256: `eb2b7e83ba65e8a01a7add4c3e20e0582a2c0c61df04b179fba1bbc850220e53`.
- All15manifest payload hashes verified. Previous0.1.0-preview.1 release retained unchanged.

Sizes measured by summing file lengths, not installed runtime/SDK/cache size. All development/package/test/cache output remains under F:/SlipShot. Runtime CPU/working-set/game impact was not measured.

## Unrun and acceptance boundaries

Real drag into the user's terminal/IDE, Explorer, Paint or an image composer: **NOT RUN**. A terminal may accept only a path; some destinations reject OLE drops, and Windows may block a non-elevated app dropping into an elevated destination. We implement standard formats without claiming universal compatibility.

Actual long drag hold, Escape, monitor disconnect during OLE, keyboard/native focus and preview nonactivation need manual confirmation. Existing capture/mixed-DPI/clipboard/accessibility/fullscreen matrix still applies. No capture or actual clipboard manipulation performed during this change.

The earlier preview1 process remains untouched, preserving any unsaved work. Exit it normally from the tray before launching0.2. Tests do not certify production readiness or BridgeShot parity. Research used public footage/narration and21st.dev preview patterns; no proprietary code/assets or licensed binaries copied.
