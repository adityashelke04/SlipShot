# Screenshot drag-and-drop RCA — 2026-11-01

## Plain English

SlipShot could hand another app a screenshot file, then delete it before that app opened it. The cleanup code assumed that Windows returning “no drop” meant nobody had taken the data. That assumption is unsafe for a receiver that queues work. The fix keeps files and PNG data once a receiver has read the payload, even when the final drag effect is None. Unused canceled drags still clean up normally.

This defect was reproduced in the installed build and fixed under controlled native Windows tests. The user reported failures in Discord and ChatGPT; their actual attachment/upload code was not exercised. The controlled test establishes a real source-side failure mechanism, rather than proving that every reported upload failure had this cause.

## Investigation and evidence

Two subagents independently reviewed native drag formats/UI gestures and file/stream ownership. The coordinator reviewed integration, added diagnostics, implemented the fix and ran combined verification.

- The installed artifact was `F:\Apps\SlipShot\versions\0.2.8-preview.1\SlipShot.dll`, SHA-256 `A7FAA429F82843DE2CE7E92BF04C86C702B031D26BF7CA1F23A85D7C18572398`. It differs from the working-tree build, so the installed DLL was also tested in isolation.
- Installed drag storage contained six ordinary PNG files totaling 1,656,778 bytes. No image contents were inspected. The old 32-file/256-MiB capacity limit was not reached in this incident.
- `ImageFiles.Save` completes synchronously: encode, flush, close, decode verification, then atomic rename. An unfinished file was not the cause found here.
- Repeated native COM PNG requests returned complete, identical bytes even after managed stream reads. Native Unicode CF_HDROP paths remained correct after accepted drops. The PNG cursor hypothesis was excluded. .NET 8 WPF resets the stream before native HGLOBAL serialization: [WPF dataobject.cs](https://github.com/dotnet/wpf/blob/v8.0.11/src/Microsoft.DotNet.Wpf/src/PresentationCore/System/Windows/dataobject.cs).

## Confirmed failure chain

1. A receiving target calls native `IDataObject.GetData(CF_HDROP)` and retains the screenshot path for a later read.
2. Its Drop callback returns None. The OLE effect describes the drag operation; it is not an upload-completion receipt. See [Microsoft DoDragDrop](https://learn.microsoft.com/en-us/windows/win32/api/ole2/nf-ole2-dodragdrop).
3. `AppController.DragImage` previously called `transfer.Complete(false)` for every non-Copy result.
4. `DragTransferStore.Complete` immediately deleted the PNG; `DragTransferLease.Complete` disposed the PNG stream.
5. The delayed file read found a missing file; a later PNG data request raised `ObjectDisposedException`. A receiver cannot finish an attachment from missing/disposed source data.

The old test suite covered accepted late reads and unconsumed cancellation, but not **consumed data followed by a None result**. Direct managed GetData assertions also missed the actual native boundary.

## Changes

- `DragPayload` sits inside WPF's concrete `DataObject`, so managed and native OLE reads pass through the same tracking point. Actual successful payload reads mark the transfer as exposed. Format enumeration and availability queries do not.
- Completion preserves the file and PNG stream when Copy was accepted **or** data was read. Disposal remains idempotent. A target can request another format later while its data object/source process remains available. Retained files also survive source-process exit.
- Unconsumed cancellations and preparation failures still remove their owned file. Exact-name ownership, reparse-point safeguards, active-transfer protection and 24-hour opportunistic expiry remain intact.
- Raised the file-count cap from 32 to 1024. A normal screenshot workflow could hit 32 retained drags in a day; exposed canceled drags also now count. The 256-MiB cap remains, and recent handoffs are never silently evicted. This was a separate deterministic limit, not the current incident's demonstrated cause.
- Updated the preview tooltip and documentation: a receiver may read data during hover, so a subsequent canceled drag may retain an exported PNG.
- Added opt-in local diagnostics without image contents, filenames, paths, window titles or exception messages.

## Verification

`./scripts/build-fallback.ps1 -Test -VisualCheck` compiles with warnings as errors and reports **165 passed, zero failed, one explicit hotkey test unrun** (166 discovered). All 36 synthetic UI renders complete; the live theme-switch pixel comparison passes. No SDK is installed, so this establishes the fallback compiler/runtime path, not an SDK/MSBuild run.

Regression coverage includes managed and native CF_HDROP late reads after None, late/repeated PNG requests, Unicode paths, query-only/unconsumed cancellation cleanup, accepted late reads, metadata/redaction preservation, 33 healthy handoffs, the 1024-file boundary, byte capacity, expiry/no eviction, bounded diagnostic requests/log rotation and unwritable diagnostic storage.

Evidence retained locally under ignored `artifacts/`:

| Run | Outcome | Evidence |
| --- | --- | --- |
| Initial installed-DLL offscreen regressions | 4 fail, 5 pass; lost consumed paths/PNG reproduced | `installed-regression/InstalledDragRegression.xml` |
| Initial new lifetime tests against old code | Missing file and closed-stream assertions fail | `tests/DragLifetimeRed.xml` |
| Capacity regressions before raising cap | Three fixtures fail on the 32-file refusal | `tests/DragCapacityRed.xml` |
| Installed DLL, controlled native OLE | 10 Copy cases pass; 10 consumed-None cases fail; 20 actual Drop callbacks, zero invalid attempts | `drag-probe-installed/native-result.txt` |
| Fixed DLL, controlled native OLE | All 20 actual drops pass, including 10 None cases; zero invalid attempts | `drag-probe-fixed/native-result.txt` |
| Final automated suite | 165 pass; one explicit hotkey test unrun | `tests/TestResult.xml` |

The native probe uses generated pixels, two owned windows and a real OLE drag loop with a custom native IDropTarget. It is a controlled same-process source/target test, not a Discord/ChatGPT or cross-process upload test. The 10/10 split was deliberately selected; it does not measure the user's failure rate. An early probe had missed Drop callbacks; those invalid attempts were excluded and superseded by the guarded, event-synchronized runs above.

The user explicitly authorized the desktop probe after reviewing its scope. It performs no desktop capture, real clipboard write, hotkey registration or network upload. An initial run had already completed when the PRODUCT.md restriction was noticed; further desktop tests were paused until permission arrived. The controlled comparison above was then run with authorization. All test windows closed and the original pointer was conditionally restored. A later optional exact-controller run stopped on independent pointer movement after seven valid Copy drops, before its None cases; those partial results are excluded from the comparison.

Reproduce the no-window COM lifetime test with `./scripts/drag-drop-probe.ps1`. It reports 20/20 passes against assembly 0.2.9.0. The same runner accepts `-AssemblyPath` for an isolated old-DLL comparison. Desktop mode requires explicit `-Desktop` and user authorization; it sends input only between its owned windows, synchronizes on native DragEnter, aborts on independent pointer motion and has a 45-second watchdog. It is deliberately separate from the normal automatic suite.

## Diagnostics for any remaining Discord/ChatGPT failure

Exit the older SlipShot through its tray menu first, resolving any unsaved editor normally. Start the fixed build in a PowerShell session with:

```powershell
$env:SLIPSHOT_DRAG_DIAGNOSTICS = '1'
& 'F:\SlipShot\artifacts\portable\SlipShot.exe'
```

Only this launch and its children inherit the flag; this does not set a global environment variable. Logs appear beside that executable at `data/diagnostics/drag.jsonl` and rotate to one `drag.jsonl.1` at approximately 1 MiB each. They are ignored by Git. Restart without the flag to disable writing. Existing local logs remain until explicitly removed.

Events correlate a drag by ID and record prepared dimensions/encoded bytes, elapsed milliseconds, version/PID, format query/read category, `native_start` button state, `native_return` effect, errors by type/HRESULT, and `completed` acceptance/retention. Target format chatter is capped at 128 records per drag; terminal events remain. Disk/permission failures in logging do not fail the drag.

- `data_read` → `native_return` effect 0 → `completed` retained true shows the fixed lifetime path.
- `native_start` leftButtonDown false with no data reads identifies a release before Windows entered the drag loop.
- `prepare_failed` distinguishes file/capacity/memory preparation errors from target behavior.
- A retained source with an actual receiver upload error needs investigation in that receiver; a Copy effect alone does not prove upload success.

## Remaining limits

PNG preparation still happens synchronously before OLE begins. Offscreen synthetic timing measured Prepare alone at roughly 94–514 ms for 1080p and 327–1463 ms warm for 4K; one noisy 4K cold run took 3060 ms. These artificial images are not user screenshots and the timings are not a performance guarantee. A very fast release during that work can still produce no drag. Removing this delay safely would require a separate lazy-materialization/active-drag design; arbitrary sleeps or retries would not solve it.

Real Discord/ChatGPT attachment success, cross-process behavior, privilege mismatches and receiver/network errors remain unverified. No user screenshot or message was sent anywhere. Accepted/exposed PNGs stay on disk until a later sweep after at least 24 hours; this is not secure erasure. At 1024 files or 256 MiB the app still refuses another file drag safely rather than deleting recent handoffs.
