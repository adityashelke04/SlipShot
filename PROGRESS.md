# Progress

## Latest: 0.2.5-preview.2 save once

User reported repeatSave stilloffered andduplicatePNGs; reproduced actualcontroller3saves→3files, fixedto1. Captured.Document retainsbaseline; independenteditorFork shares weakper-snapshot save receipts. Savedbuttonsdisabled; explicitSaveacopyas separate; no pickerfromquickSave. Failed/deletedfile recovery, undo/redo receipts, overwriteinvalidation acrosslivecaptures andweakownerUIupdates addedafterindependentreview.114tests passed; reviewverdictShip. Version0.2.5-preview.2 packaged/installed for normalrestart; existingruntime unsavedwork preserved. Userrequested main; finalcommit/push main anddefaultbranch switch complete asrecordedinhandoff. No existinguserimagesdeleted.

## Latest: 0.2.4-preview.1 quick save

Save now uses configured SaveDirectory or same-local-drive SlipShot Captures (F:/SlipShot Captures here), creating it on explicit Save and choosing unique PNG names without overwriting. Settings quick-save path may be new/absolute; Save As preserved via editorarrow/CtrlShiftS for JPEG/otherpaths. No automatic capture saving. Full suite104passed; compactUI reviewed; independentreview found no blocking issue. Latest package/install version0.2.4-preview.1; activate by normal Exit/reopen from Start. Current installed0.2.3process preserved; no unsavedwork discarded. Evidence docs/QUICK_SAVE_REVIEW.md. Commit/push authorized.

## Latest fix: 0.2.3-preview.1

Investigated installed stuck preview using only window metadata (pointeroutside/notforeground); no personal image read. Confirmed FIFOcardA-versus-latestB mismatch and publicationduringcapture path. Fixed latest-only replacement with identity-guarded Closed, capture-active suppression, async selection completion; optional copy errors remain distinct from capture success. Default10second idle+20second aggregate hover grace; physical pointer/foreground check, missed-release recovery, and timer-driven close independent of fade. Genuine hold/drag/copy protected. Persistent clipboardBitmap+PNG added, old pending copy requests superseded. Suite102passed including realtimer/unshownWindow regression, no native clipboard/capture test. Packaged0.2.3; installed fixed payload at F:/Apps/SlipShot/versions/0.2.3-preview.1 and updated Start menu. Existing10448process deliberately untouched; normal Exit then Start activates fix. Settings preserved with PreviewSeconds10, no capture files copied. Userrequestedcommit+push aftertests.

## Latest: SlipShot 0.2.2-preview.1

Renamed product, projects, namespaces, executable/apphost, generated filenames, current docs and safe synthetic README images to SlipShot. Source directories now src/SlipShot and tests/SlipShot.Tests; solution SlipShot.sln. Private GitHub repository renamed to adityashelke04/SlipShot, origin updated. Ctrl+Alt+S unchanged. Historical records/releases untouched; legacy mutex and .leftshot-drag prefix deliberately retained for compatibility. Serial build87tests passed,16offscreen UI fixtures regenerated; static host/source checks passed. New release path artifacts/releases/0.2.2-preview.1/win-x64/SlipShot.exe. No desktop capture or personal screenshot used; app launch not performed in this rename.

## Repository handoff

Added3reviewed synthetic actual-WPF UI images to docs/images and README, with reproducible generator/provenance. No desktop or personal screenshots used. Tightened ignore rules for private runtime data, credentials, binaries and images; only3exact demoPNG paths allowed. Added bounded tracked/history privacy checker and docs/REPOSITORY_PRIVACY.md. Source compiled during freshoffscreen rendering; app behavior unchanged, preceding87tests passed. Future release packages include the same reviewed demo images so README links work offline. No old release archive altered. Repository remains PRIVATE; staged-file/privacy checks and push complete as recorded in final handoff.

## Latest shortcut update: 0.2.1-preview.1

User rejected function keys. Default is now Ctrl+Alt+S; modified letters supported in parser/settings UI alongside legacy function keys. Parser rejects numeric aliases, unmodified letters and reserved unsupported bindings. Observed test-first failure of four letter cases, then full suite87passed/0failed, native PE/static scan passed. New immutable local release artifacts/releases/0.2.1-preview.1/win-x64/LeftShot.exe; use this for the next launch. No app was running during this change, and actual native registration remains untested until launch. Earlier releases remain unchanged; preview0.2.0 local settings still contain the prior Ctrl+F8 preference. Current source/default and new release use Ctrl+Alt+S. No automatic startup or launch performed.

## Current: 0.2.0-preview.1

User requested drag-and-drop plus full UI redesign, then authorized creating private GitHub repo. Created https://github.com/adityashelke04/LeftShot (PRIVATE), origin/master tracks correctly; ordinary pushes verified. Source change3ea28fc pushed. Publishing preflight in scripts/check-publish.ps1.

Implemented threshold drag gesture, standard FileDrop/Bitmap/PNG payloads, explicit-drag temporary file lifetime with ownership/bounds, hover/hold/drag timeout suspension and safe nested-close handling. Redesigned preview and editor with compact header/tool rail/context controls/optional inspector, physical-pixel zoom and corrected primary-button contrast. Independent source review found no blocking defect; actual target drag interoperability still NOT RUN.

Latest serial fallback build:80passed/0failed/0skipped;379assertions;16synthetic UI fixtures rendered, representative full/compact/context/preview states inspected. No production capture/clipboard/native input tests performed. Existing preview1 process remains running; do not kill it or discard unsaved work. User must Exit old tray instance normally before launching new release. Packaged immutable0.2.0-preview.1: folder359,397bytes, ZIP158,937bytes; all payload checksums verified. docs/VERIFICATION_0.2.md records evidence. All outputs F:/SlipShot; new source and documentation pushed privately. Next check is real target-app drag interoperability.

## Previous delivery (historical)

Implemented capture/selection/left preview, complete annotation editor, clipboard/export, settings/tray and pins. Local milestone commits recorded. Three audits completed; root integrated cancellation, memory-accounting, overwrite, failure-reporting, timeout, close and theme fixes.

SDK unavailable; official Roslyn + installed .NET8 runtime fallback compiles optimized x64 apphost/executable. Final observed suite:62 passing,0 failures,223 assertions,1.296s. Ten synthetic offscreen editor/preview/settings images rendered and inspected; no actual windows shown. Native desktop acceptance and SDK/MSBuild remain NOT RUN. All generated output/cache/temp remains in repository on F:.

Release0.1.0-preview.1 packaged immutably under artifacts/releases with per-file and ZIP checksums. Release folder308,372bytes; ZIP137,471bytes. docs/VERIFICATION.md records exact evidence and limitations. Next person should run the five-minute checklist and record native observations; obtain a supported SDK for the conventional build. Do not run app or desktop tests until user returns. No remote publication performed.
