# Service audit — 2026-10-31

What this part does: SlipShot captures a Windows monitor, exports flattened images, copies images, and prepares temporary files for deliberate drag and drop. Preferences stay beside the portable app. The expected load is one local user and one app instance, with serial UI actions.

## Confirmed bugs

1. **Equivalent hotkeys conflict with the app itself** (`src/SlipShot/Services/Hotkeys.cs`).
   - What this is: Settings lets the user choose a capture shortcut.
   - Before: Changing only the order or case of the same shortcut, such as `Ctrl+Alt+Shift+F24` to `shift+ctrl+alt+f24`, asks Windows to register the same combination again under another ID. Windows rejects it as already in use, and Settings cannot be saved.
   - Fix: Compare the parsed key and modifiers to the current successful registration. Keep that registration when the effective shortcut is unchanged.
   - Effect: Equivalent spellings can be saved. Actual replacement still registers the new shortcut before releasing the old one, preserving the old shortcut if a real conflict occurs.
   - Evidence: A native duplicate-registration probe returned Windows error 1409. `EquivalentHotkeySpellingKeepsTheExistingRegistration` failed before the fix and passed after it in the shared 09:17:48 UTC run. That prior native verification conflicted with the hotkey restriction in `PRODUCT.md`. The test is now marked NUnit `Explicit` for attended acceptance only and is excluded from the default automatic suite. No native verification was rerun after identifying the restriction.

2. **Invalid function-key input selects a different key** (`src/SlipShot/Services/Hotkeys.cs`).
   - What this is: The same parser validates Settings and stored preferences before registering a Windows shortcut.
   - Before: Values such as `Ctrl+F1,F2` and `Ctrl+F1, F1` were accepted. Enum parsing combines comma-separated names, so an invalid shortcut silently became a single key.
   - Fix: Require the function-key text to match exactly one supported key name, ignoring case.
   - Effect: Malformed combinations are rejected; ordinary modified letters and F1–F24 shortcuts retain their existing behavior.
   - Evidence: Both `CombinedFunctionKeyNamesAreRejected` cases failed before the fix and passed after it in the shared 09:17:48 UTC run. All three service audit cases passed in that earlier run; the native case is now explicitly excluded from automatic verification by repository policy. That combined run still had one unrelated editor failure under investigation. The coordinator owns final complete-suite verification.

## Scope reviewed

Read every service source file and traced its callers in the controller, settings, editor, preview and selection code. Reviewed native capture handles and cancellation; clipboard payload metadata and retries; PNG/JPEG export and overwrite handling; save receipts; folder launch arguments; settings validation and atomic replacement; drag-file ownership, expiry, capacity and late reads; and hotkey parsing and ownership. Read the existing service, save, clipboard, drag, folder and integration tests, build/test configuration and architecture notes. No `AGENTS.md` files were present in the repository.

Only confirmed bugs are changed. No dependency, export format, retention policy, feature or visual redesign was added.

## Not checked

Actual desktop capture, HDR/protected surfaces, native window placement across real monitors, Windows clipboard interoperability and real application drag-and-drop targets were not exercised. The added native hotkey check is NUnit `Explicit` and requires attended acceptance: `PRODUCT.md` prohibits registering hotkeys during automatic verification. When deliberately selected for attended acceptance, it uses a temporary message-only window, releases its shortcut immediately, and skips if every candidate test shortcut is already occupied. It does not show a window or capture/copy desktop content. Public release readiness still requires the remaining desktop acceptance checks.
