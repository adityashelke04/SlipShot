# Final verification — 0.1.0-preview.1

Date: 2026-10-30. Source is an independent implementation. This is an unsigned preview, not a production-readiness certification.

## Observed automated results

- `./scripts/build-fallback.ps1 -Test`: exit 0; optimized x64 app and tests compile with nullable enabled and warnings treated as errors. Native GUI apphost generated through Microsoft's host tooling.
- NUnitLite 4.4.0: **62 total, 62 passed, 0 failed, 0 skipped, 0 warnings**, 223 assertions. Final test run 14:29:49–14:29:51 UTC, reported duration **1.296492 seconds**, one worker path (`--workers=0`) on installed .NET8.0.11 x64. This is test-suite wall time, not application performance.
- Latest offscreen visual pass rendered **10 synthetic fixtures**: six editor sizes/themes plus light/dark preview/settings. The resulting images were inspected; dark dropdown styling and settings footer were corrected. No shown window, desktop capture, clipboard write or hotkey registration.
- `python scripts/source-audit.py`: exit 0. Bounded scan of23 application C# files found no selected network clients, logging, placeholders, process launching or forced-foreground APIs. Project/manifest XML parsed. Portable EXE is x64 PE, GUI subsystem, targets LeftShot.dll and includes PerMonitorV2 manifest. This does not prove app launch or every possible security property.
- Three audit reports: `AUDIT_1.md` correctness/lifecycle, `AUDIT_2.md` privacy/security/resources, `AUDIT_3.md` editor/accessibility/UI. Integration findings addressed, with native proof explicitly deferred. All-black-frame rejection remains a documented limitation. Shutdown additionally suppresses pending preview creation.
- Historical full test failure:57/60 passed, one genuine corrupt/unsupported-settings recovery bug plus two OS exception-type assumptions. Fixed and rerun successfully; no failures were silently skipped.

## Output inventory

Measured by summing file lengths with PowerShell `Get-ChildItem -File | Measure-Object Length -Sum`:

| Output | Bytes | Contents |
|---|---:|---|
| `artifacts/portable` | 276,585 | EXE139,264 + DLL137,216 + runtimeconfig105 |
| `artifacts/releases/0.1.0-preview.1/win-x64` | 308,372 | App plus README, license, .NET apphost license, architecture, reference, dependency, change and testing notes; SHA256SUMS |
| Release ZIP | 137,471 | Same12 release files |
| `.cache` development files | 109,809,075 | Compiler/NUnit/host packages and extracted tooling; **not part of installed app** |

ZIP: `artifacts/releases/0.1.0-preview.1/LeftShot-0.1.0-preview.1-win-x64.zip`

SHA256: `e9fa78b1cc0b88f55dbf56ebc6072f67c5c56a6cadcfb41ffda54b09fc59712f`

All generated project, cache, test, temporary and release output was directed under F:/SlipShot. SDK/runtime/package-cache sizes must not be conflated with the portable payload. No CPU/working-set/idle/gameplay benchmark was run. No self-contained build was made; that would include a substantially larger runtime and remains a separate option.

## Explicitly unrun / blocked

- **Application launch and every native desktop acceptance row in TESTING.md: NOT RUN**, because the user is gaming and prohibited desktop capture, hotkey registration, clipboard manipulation and interactive test windows.
- **Normal SDK restore/build/publish: NOT RUN**, because no SDK is installed. The fallback uses installed runtime assemblies rather than SDK reference packs; it cannot establish MSBuild/SDK compatibility on another machine.
- No native focus, mixed-DPI pixels, high-contrast interaction, screen-reader, monitor disconnect, exclusive-fullscreen, real clipboard, tray cleanup or actual repeated-capture resource measurements claimed.
- BridgeShot's product page was404; public creator evidence is incomplete and describes a macOS tool. REA was connected but no proprietary app artifact was supplied or extracted. Complete feature parity is unverified.
- No GitHub publication, remote push, account creation, installer/system modifications or global identity changes. Local commits and the local preview tag preserve milestones.

The installed8.0.11 runtime is an old patch. Update servicing and move to a longer-supported target before public release; [.NET8 support ends November10,2026](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core). No runtime/SDK installation was performed by this task.
