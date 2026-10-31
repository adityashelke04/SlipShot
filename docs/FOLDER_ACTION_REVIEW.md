# Open file location — review and verification

0.2.6-preview.1,2026-10-31. User requested a captures-folder action beside Edit/Open on the left preview; separately asked to confirm image-generation ability before any app-logo/icon replacement.

Implemented two compact action rows: Edit + Open file location, followed by Copy/Save/Pin. The new button remains available before and after saving. It resolves the current configured quick-save directory on each click (default F:/SlipShot Captures here), validates an absolute path and creates an empty directory if needed. Existing files are untouched. It then launches the fixed Windows Explorer executable with one directory argument through ArgumentList, without a command shell. It neither saves nor reads an image. Folder/Explorer failures stay inline; explicit interaction refreshes preview time.

Source review checked action wiring, non-activation on appearance, saved-button availability, parameter handling, no executable-file launch target, no implicit image save, existing-file preservation and failure retry. The static process-launch check has one explicit exception for this reviewed folder service; other process launches remain flagged.

Full serial build/test: **120passed,0failed,0skipped,0warnings**. New service tests build the Explorer request without launching it, using controlled temporary directories. Unshown WPF button tests verify callback invocation before/after save, zero save/edit callbacks, zero HWND and inline error recovery. Twenty synthetic layouts render; dark preview and light Saved preview inspected. No personal captures, clipboard, visible app input or live Explorer window were used in tests. Actual Explorer opening remains a manual check after restart.

Icon review: current code creates a tray mark at runtime; no ApplicationIcon, Window.Icon or tracked ICO asset is configured. No icon identity/asset change or image-generation call was made. ChatGPT image generation is available; logo creation/replacement is deferred pending the user's approval, as requested.
