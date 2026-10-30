# LeftShot reference research

Research date: 2026-10-30. LeftShot is an original Windows/WPF implementation. This document records a bounded public-reference investigation of BridgeShot; it does not establish implementation or feature parity.

## Scope and method

The investigation used public web search, read-only HTTP requests, creator page metadata, and publicly served preview images from a creator's video. It did not download or extract a proprietary application, run an untrusted executable, inspect private accounts, bypass authentication, open an interactive browser, operate the desktop, or capture the user's screen. Research files were written only beneath `F:/SlipShot`.

The `reverse-engineer-anything` skill and its connection/recovery guide were read. REA's connected `binary_session({})` reported `open: false`, no selected artifact, and server package `rea-agents` version `6.7.0`. Thus REA is present; absence of REA itself is **not** the limitation. No BridgeShot artifact or selected public-page CDP target was supplied. Native application behavior and source reconstruction therefore remain outside the evidence gathered. No engine installation, registration change, or configuration repair was attempted. The session-status response is diagnostic information, not an application-analysis Evidence record. Public HTTP and image observations below are ordinary research evidence, not REA-authenticated runtime evidence.

## Source access ledger

| Source | Access result | What it establishes |
| --- | --- | --- |
| [Official BridgeShot product URL](https://www.bridgemind.ai/products/bridgeshot) | Direct web retrieval returned HTTP 404. Search exposed cached generic BridgeMind marketing at the same path. | The direct product page was unavailable during this investigation. Cached generic content is not accepted as BridgeShot documentation. |
| [Creator LinkedIn post supplied for the task](https://www.linkedin.com/posts/bridgemindai_vibecoding-agenticcoding-aicoding-activity-7482045667939622912-4595) | Public post text accessible without sign-in; page includes a sign-in/join affordance. | The creator discusses plan-mode investigation of an AWS bug report and mentions BridgeShot and the BridgeMind API as input context. It does not describe BridgeShot controls or substantiate its architecture. |
| [Creator video: My Vibe Coding Workflow (Step by Step Workflow)](https://www.youtube.com/watch?v=vBndrDoB_X4) | Web-reader fetch throttled. A direct public HTTP GET returned 200 and exposed normal player metadata naming BridgeMind as author. Public preview-image URLs from that metadata were accessible. | Creator description identifies BridgeShot as a macOS screenshot/annotation tool. Limited visual samples of a capture-related sequence were inspectable. |
| Creator video's public English caption track, obtained from the same page metadata | HTTP 200 with an empty body. | No primary-source transcript was recovered; no narration is quoted or treated as observed. |

Search discovery also surfaced a mirrored transcript at [ShadowingEnglish](https://shadowingenglish.com/nl/practice/my-vibe-coding-workflow-step-by-step-workflow-vBndrDoB_X4), a [PacketBench competitive brief](https://github.com/packetloss404/PacketBench/blob/main/dev/bridgemind/bridgespace-competitive-brief.md), and a [third-party review](https://starsearn.com/guides/bridgemind-review-2026). These were discovery leads only. Their technology, OCR, subscription, and availability assertions are not adopted as verified product facts.

## Observed, claimed, and unknown

### Directly observed in public media

The creator video's publicly supplied storyboard sheets were inspected as existing media assets, not generated screenshots. The highest supplied preview format was 320 by 180 pixels per frame, sampled at ten-second intervals. Sheets 8 through 13 were examined. In sheet 11, the sequence around 16:30–17:10 shows an ecosystem page with a screenshot-tool tile, a browser page, a centered image-editing surface with a compact toolbar, then an agent workspace containing a small image preview. These coarse samples suggest a capture/edit-to-agent-context workflow; they do not reveal the exact input actions, full toolbar labels, clipboard contents, or file format. The video's chapter list places its screenshot discussion at 12:30, which did not align with the clearest capture-related samples; sample position is used here instead of silently trusting the chapter label. [Creator video](https://www.youtube.com/watch?v=vBndrDoB_X4&t=990s).

Local inspection aids are `.tmp/bridgeshot-public-storyboard-8.jpg` through `.tmp/bridgeshot-public-storyboard-13.jpg`. These are temporary research references, not LeftShot assets, source code, or branding for redistribution.

### Creator claims

The video's description calls BridgeShot a screenshot and annotation application for macOS and includes it in the creator's coding workflow. This establishes the creator's stated positioning, not independently tested functionality. The supplied LinkedIn post establishes a separate claim that BridgeShot was included in a bug-investigation context. Neither statement establishes a Windows implementation, a specific annotation inventory, or an OCR implementation.

### Unresolved

- Exact capture activation shortcuts, region/window/full-screen behavior, multi-monitor and mixed-DPI handling.
- Toolbar labels, editing tools, shortcut semantics, undo/redo model, crop behavior, and annotation persistence.
- Whether screenshots are copied, saved, dragged, uploaded, or attached through a specific integration in the sampled sequence.
- OCR availability, language support, local versus remote processing, redaction behavior, and privacy guarantees.
- History storage, retention, image formats, current distribution status, current pricing, licensing, internal framework, source code, and implementation algorithms.
- Runtime performance, accessibility, error recovery, and source-code parity.

These are evidence gaps, not assertions that BridgeShot lacks those features.

## Original LeftShot direction

The reference supports a broad product idea: capture visual context, make the relevant part clear, then hand it to the next task. LeftShot's implementation and interface should be designed independently around the user's Windows requirements. No BridgeShot icons, logo, proprietary text, source, or pixel-for-pixel layout should be imported.

Proposed original improvements are explicit capture-mode choices with keyboard cancellation; a clear preview before export; annotation undo/redo; opaque redaction that survives flattened export; visible save/copy results and useful error messages; predictable local storage; and reliable coordinate handling across displays with differing DPI. A concise screenshot-plus-note handoff could help bug reports without requiring a particular AI service. These are design proposals, not claims of superiority over verified BridgeShot behavior, and not a declaration that every item has shipped. The implementation and test report determine LeftShot's delivered scope.

## Coverage conclusion

Public identity and creator positioning: partially answered. Public workflow: partially answered through low-resolution visual samples. Exact behavior, proprietary internals, and parity: unresolved. The available evidence is sufficient for broad inspiration while keeping LeftShot's design original; it is insufficient to describe BridgeShot as fully reverse engineered.
