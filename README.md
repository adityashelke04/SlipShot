# LeftShot

A small, local-only Windows screenshot editor. Capture a region, open the bottom-left preview, annotate, and copy or save a flattened image.

**0.1.0-preview.1 — implemented and synthetically tested; native desktop acceptance is pending.** This unattended development run does not establish production readiness. LeftShot is a provisional name, without a trademark availability claim. MIT license for original code. No BridgeShot source, binaries, branding or artwork is included.

## Run the portable preview

Windows 10 version 2004 or newer / Windows 11, **x64**, with the **.NET 8 Windows Desktop Runtime**. Keep all files in the portable folder together and run `LeftShot.exe`. It starts in the notification area, without an editor or capture overlay appearing automatically. Default capture shortcut: **Ctrl+Shift+F8**. The tray also provides capture, recent-image access, settings and exit. Windows may hide the tray icon in its overflow menu.

Build output: `artifacts/portable/LeftShot.exe`. Organized release: `artifacts/releases/0.1.0-preview.1/win-x64/LeftShot.exe`. It is unsigned; Windows reputation warnings are possible. Do not disable Windows security. No installer, service, startup registration or updater is supplied.

All preferences live in `data/settings.json` beside the executable. Put the portable folder in a location you can write. There is no screenshot history or automatic saving. Choosing Save opens a dialog; no cloud location is selected automatically. An explicitly chosen folder is the user's responsibility.

## Everyday flow

1. Place the pointer on the desired monitor. Press Ctrl+Shift+F8 or choose **Capture region** from the tray.
2. Drag in any direction within that monitor. Release to capture; Escape cancels. Cross-monitor selection is deliberately constrained. Keyboard alternative: Space sets the center anchor, arrows adjust the end point, Shift moves 10 pixels, Enter captures.
3. The preview appears at the **bottom-left of that monitor's work area**. Choose Edit, Copy, Save or Pin. It expires after eight seconds, pausing while hovered. Appearance does not activate the app. The tray gives keyboard access to the latest capture.
4. In the editor, use Crop, Arrow, Line, Rectangle, Ellipse, Pen, Highlight, Text, numbered Step or opaque Redact. Select objects from the canvas or Annotations panel, move them, drag a resize handle, or delete them. Text controls support content, font size and opaque color; rectangles and ellipses support fill.
5. Ctrl+Z undoes; Ctrl+Y / Ctrl+Shift+Z redoes. Ctrl+C copies the flattened image; Ctrl+S saves. These shortcuts yield to text/combo editing. Ctrl+wheel zooms, Fit fits, middle-drag or Space+drag pans. Arrow keys nudge a selected object; Shift nudges 10 source pixels. Editing closes through Save / Discard / Cancel.

One preview is visible, with at most three pending. Overflow drops the oldest pending capture. The most recent capture remains available from the tray until replaced, forgotten or exit. Dismissal never deletes a saved file. Three editors and three pins are allowed, subject to the image-memory budget. Use **Forget in-memory previews** to release recent captures; it leaves editors and pins intact.

## Build and test with an SDK

This machine had the .NET 8.0.11 desktop runtime but **no SDK**. No SDK was installed. A supported .NET 8 SDK is the normal prerequisite for these commands; the SDK build itself has not been run here. Use a currently serviced runtime patch on your machine; 8.0.11 is not current. .NET 8 support ends November 10, 2026; migrating the target is a follow-up before a long-lived public release. See [Microsoft's support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core).

From PowerShell in this repository:

```powershell
. ./scripts/environment.ps1
dotnet restore LeftShot.sln --disable-parallel
dotnet build LeftShot.sln -c Release --no-restore -m:1 -p:UseSharedCompilation=false
New-Item -ItemType Directory -Force artifacts/tests | Out-Null
dotnet run --project tests/LeftShot.Tests -c Release --no-build -- --workers=0 --work=artifacts/tests --result=TestResult.xml
dotnet publish src/LeftShot -c Release --no-restore --self-contained false -o artifacts/sdk-portable -m:1 -p:UseSharedCompilation=false
# Only when you are ready for desktop interaction:
./artifacts/sdk-portable/LeftShot.exe
```

The tests use NUnitLite's maintained lightweight runner, so the test command is `dotnet run` above, not an empty `dotnet test` invocation. Test fixtures are synthetic and clipboard sinks are fake. The environment script redirects NuGet, CLI-home and temporary data into this repository, disables development CLI telemetry and lowers only its own shell/child-process priority. It does not change your global environment or other processes.

## Verified SDK-less build on this machine

The fallback uses the installed desktop runtime, a small Microsoft Roslyn compiler package, NUnit, Microsoft's apphost template and a build-only host-writing library. It compiles optimized x64 code with nullable warnings treated as errors; it is a useful local validation path, not a replacement for SDK validation on CI.

```powershell
. ./scripts/environment.ps1
python scripts/bootstrap-compiler.py
./scripts/build-fallback.ps1 -Test -VisualCheck
# Only when you are ready:
./artifacts/portable/LeftShot.exe
```

`-VisualCheck` renders synthetic editor content offscreen in software. It does not show windows, capture the desktop, register hotkeys or touch the real clipboard. Downloads and generated files remain inside this repository. The installed app has no networking code or development tool dependencies.

## Limits and privacy

- GDI still-image capture is SDR, without cursor inclusion. HDR colors may differ. Protected surfaces and exclusive fullscreen may return black/unavailable images. An all-black frame is rejected, including a legitimately all-black desktop. No hooks, injection, elevation, anti-cheat interaction or bypass is attempted.
- Capture exclusion is best effort; own visible windows are also hidden before capture and DWM is flushed. This is not a confidentiality guarantee against other tools. Those native behaviors require the checks in TESTING.md.
- Selection coordinates are physical pixels; editor coordinates are source pixels. Strokes scale with image zoom; resizing object geometry retains its stroke width. Resizing text changes font size. Exports use crop pixel dimensions at 96 DPI, without source metadata/layers/thumbnails.
- Redaction is opaque black and rendered last. Original pixels remain in the in-memory source and undo history until references are released. No forensic RAM erasure or OS clipboard-history deletion is promised. Pin copies are independently retained until closed.
- No telemetry, accounts, OCR, AI models, uploading, updater or background network requests. No screenshot/clipboard/window-title logging. Preferences contain no image data.
- CPU, working-set and gameplay impact have not been measured. Timers run only while a preview is alive. Idle targets are intentions pending measurement, not zero-impact claims.
- Full-monitor/foreground-window capture, OCR, persistent history and presentation export are intentionally absent. Pin is implemented. There are no controls for absent tools.

See [TESTING.md](TESTING.md), [ARCHITECTURE.md](ARCHITECTURE.md), [REFERENCE_NOTES.md](REFERENCE_NOTES.md), [DEPENDENCIES.md](DEPENDENCIES.md), and the three audit reports under `docs/`.
