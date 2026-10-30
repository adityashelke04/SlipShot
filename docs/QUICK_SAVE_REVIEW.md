# Quick Save review — 0.2.4-preview.1

User request: Save should write to a dedicated SlipShot folder on the PC without repeatedly asking for location.

Implemented Save/preview Save/Ctrl+S/close-guard Save as PNG quick-save. Default follows the local app drive: F:/SlipShot Captures for this installation. The folder is created on explicit Save; existing custom SaveDirectory remains respected. A network-hosted app defaults to local application data rather than silently saving to the share. Users can change Quick-save folder in Settings. Save As remains reachable by the editor arrow menu and Ctrl+Shift+S, including JPEG/quality/background and explicit overwrite confirmation.

No implicit overwrite: timestamp/suffix generation skips existing files/directories; atomic non-overwriting move handles races. Up to16collision retries then a friendly failure. No silent fallback folder, no saved-state marking before success, no changes to capture-only or drag temporary-file behavior. Screenshot files are never included in commits.

Independent read-only review found no blocking issue across ImageFiles, controller save paths, editor/close guard, Settings and tests. Reviewer did not independently run tests or desktop UI. Main-agent serial build/test: **104passed,0failed,0skipped,0warnings**. New fixtures verify missing-directory creation, repeated same-timestamp saves retaining original bytes, decoded PNG dimensions, blocked-folder preservation and relative-path rejection. Existing atomic-write/overwrite/PNG/JPEG/redaction tests remain passing.

Sixteen synthetic UI layouts rendered; compact dark editor and Settings inspected. No real screenshot or desktop capture used. Standard SDK path remains unverified because the SDK is absent. Manual checks still needed after normal restart: preview Save, editor Ctrl+S, Save As menu/shortcut, Settings custom path, and unavailable-folder failure. Existing installed process is preserved so unsaved work is not discarded.
