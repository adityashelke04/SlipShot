# Contributing to SlipShot

SlipShot is an MIT-licensed native Windows utility in preview. Start with [README](README.md), [architecture](ARCHITECTURE.md) and [desktop test matrix](TESTING.md). Frame-S is the selected identity; [DESIGN.md](DESIGN.md) defines its light, dark and high-contrast treatment. Brand sources and provenance are in [the brand kit](docs/branding/README.md).

Use Windows x64 and a supported .NET 8 SDK. Follow the serial restore/build/NUnitLite commands in README. Tests must use synthetic images and fake clipboard/capture services. Never add a personal screenshot, local settings, credentials, capture/drag files, caches or build output to Git. Check `git diff --cached` and run `python scripts/repo-privacy-check.py` before submitting.

Keep capture, coordinates, document editing, rendering/export and UI separate. Preserve source-pixel editing coordinates, opaque flattened redaction, bounded memory and nonactivating previews. Runtime network features, telemetry, startup registration and silent screenshot saving do not fit the current product.

For a bug report, include the version, Windows version, display scaling and reproducible steps using invented content. Include a synthetic image only if needed. Mark actual desktop checks as run only when observed. For a pull request, explain the user-visible change, relevant tests and outstanding native checks. Keep unrelated work separate.

Report suspected security/privacy vulnerabilities privately using [SECURITY.md](SECURITY.md).
