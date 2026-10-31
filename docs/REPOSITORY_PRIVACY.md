# Repository privacy review

Reviewed2026-10-30 for the requested README screenshots and repository handoff.

## What was added

Three generated previews under `docs/images`: actual WPF editor in light/dark and capture preview. Their document is created from literal drawing instructions and invented text in `scripts/VisualCheck.cs`. No desktop, user screenshots, clipboard, drag-out files, user preferences or personal documents were read to produce these images. All three output images were visually inspected before staging; text/EXIF metadata checks found none. See `docs/images/README.md` for provenance and regeneration instructions.

## Checks performed

- Inspected tracked-file inventory and all reachable Git history objects. Before adding these files, the bounded scan covered68tracked paths and120historical blobs; it was rerun after staging/commit.
- No prohibited runtime/generated paths, unapproved screenshot/image paths, binaries, credential-file paths or selected credential signatures found. The script checks private-key headers, GitHub tokens, AWS access-key IDs and Slack tokens, and reports rule names/paths without printing candidate secret values.
- Verified that artifacts, temporary research data, runtime `data/` folders, `.env` files, signing keys, binaries and unrelated image formats are ignored. Only the three named synthetic PNG paths are exceptions.
- Source/render build succeeded. No application behavior changed in this documentation pass; the preceding functional suite passed87tests. Native acceptance limitations remain in TESTING.md.
- GitHub remains PRIVATE. Only explicitly staged source/docs/synthetic images are pushed. Local personal captures and builds are not uploaded. No Git history was rewritten and no user files were deleted.

## Boundaries

This is a focused accidental-publication check, not a complete secret-scanner or a guarantee that arbitrary future files are safe. Public research references and normal Git authorship/repository metadata remain. Review image pixels and provenance whenever replacing demos; never substitute a real screenshot and rely only on redaction or `.gitignore`. The scripts inspect this repository only, not personal folders or unrelated applications.

0.2.7 adds explicitly approved brand assets: three ChatGPT-generated concept PNGs (text prompts only), six authored path-only SVG masters/variants and their generated app ICO. These exact paths are reviewed; unrelated images remain blocked. The generation tool's originals remain in its managed cache, with project copies under F:/SlipShot. No user screenshot was submitted as an image-generation reference. Canonical vector and ICO palette checks are separate from privacy checks.

Before a future push:

```powershell
python scripts/repo-privacy-check.py
git diff --cached --stat
./scripts/check-publish.ps1 -CheckRemote
```
