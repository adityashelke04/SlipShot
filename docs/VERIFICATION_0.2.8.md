# Release evidence — 0.2.8-preview.1

Source implementation commit: `b03a633`. Final serial fallback build/test: 2026-10-31 04:57:43–04:57:45 UTC. NUnit: **134 passed, 0 failed, 0 warnings, 0 skipped**, 2.461 seconds. Warnings are errors in compilation. The software WPF harness generated 36 images and passed the live-switch pixel comparison. Static audit: 29 C# files; expected x64 GUI host, embedded icon/group and PerMonitorV2 manifest. Privacy check passed for 110 tracked paths and 305 pre-commit historical blobs; only approved generated/synthetic images are tracked.

## Build and run

From `F:\SlipShot`:

```powershell
./scripts/build-fallback.ps1 -Test -VisualCheck
python scripts/source-audit.py
python scripts/repo-privacy-check.py
./artifacts/portable/SlipShot.exe
```

Run the executable only after normally exiting any previous tray instance. Conventional SDK restore/build/test commands remain in README; they were **NOT RUN** because no SDK is installed. No SDK installation was attempted. The fallback uses already-cached official compiler/test packages and installed .NET Desktop Runtime 8.0.11.

## Artifact

- Framework-dependent, unsigned, Windows x64; requires .NET 8 Windows Desktop Runtime x64.
- Portable executable: `artifacts/releases/0.2.8-preview.1/win-x64/SlipShot.exe`.
- Release folder: **3,924,248 bytes / 39 files**, including documentation, synthetic screenshots, SVGs and concept boards.
- ZIP: **2,959,396 bytes**, `SlipShot-0.2.8-preview.1-win-x64.zip`.
- ZIP SHA256: `1abbf6367750f3e8c44d0d3f4889f25dbc45051d9d696c3aea689c877ebb20d6`.
- Main executable + assembly + runtime config: **391,785 bytes**. Runtime, SDK, package caches and build tools are excluded from these app-payload numbers.

Sizes measured with PowerShell file lengths; hashes with Get-FileHash. No CPU, GPU, startup-latency or working-set benchmark was performed.

## Installed update

Prepared `F:\Apps\SlipShot\versions\0.2.8-preview.1\SlipShot.exe`; all 38 manifest-listed payload files verified after copying. The Start menu SlipShot shortcut points to this version and its icon. Existing settings only were migrated from 0.2.7; captured images, drag-out data and saved images were not copied or deleted. The running 0.2.7 process was preserved to protect unsaved work. Use its tray Exit and reopen SlipShot to load this update.

## Scope of review

Three audit passes and the five-minute desktop checklist are in [THEME_REVIEW.md](THEME_REVIEW.md). Native focus, system high contrast, title-bar/tray display, actual control input, mixed DPI, capture and clipboard interoperability remain **NOT RUN** where not observed. No personal screen or clipboard content was accessed. This release is a tested preview, not a production-readiness claim.
