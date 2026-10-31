# Screenshot provenance

These are the application's actual WPF controls, rendered offscreen in software by `scripts/VisualCheck.cs`. They are **synthetic UI previews**, not screenshots of a user's desktop or captured documents. Native window chrome is not shown.

The harness constructs the image entirely from literal shapes and invented text in `SyntheticDocument()`. The sample email uses the reserved `example.test` domain. It never reads a user's image, clipboard, screen, saved preferences, capture history or drag-out directory; save/copy/pin callbacks are inert. Redaction in the example is a demonstration over invented pixels, not sanitization of a real screenshot.

Reproduce from repository root:

```powershell
./scripts/publish-demo-images.ps1
```

The script copies only these fixed outputs:

- `editor-dark.png`: editor, dark appearance,1240×820.
- `editor-light.png`: editor, light appearance,1240×820.
- `preview-dark.png`: capture preview, dark appearance,328×400, including Open file location.

Review every image before staging it. The ignore file permits only these three PNG paths; other screenshot formats, private runtime data, generated artifacts and credentials remain ignored. Ignore rules are a guardrail, not a privacy guarantee. Never use `git add -f` to publish a personal capture. Newly authored interface geometry and synthetic fixture content use the repository MIT license.
