# SlipShot brand assets

The canonical palette is orange **#D97757** and dark **#141413**. The SVG files in `src/SlipShot/Assets` contain exact color values. Antialiased raster edges blend those colors with their background; the flat interior colors remain exact.

| Concept | Master | Dark variant | Use |
| --- | --- | --- | --- |
| Frame S | `frame-s.svg` | `frame-s-dark.svg` | Default app, window and tray identity |
| Lifted Slip | `lifted-slip.svg` | `lifted-slip-dark.svg` | Alternative: capture frame with an outgoing slip |
| Open Shutter | `open-shutter.svg` | `open-shutter-dark.svg` | Alternative: open aperture and capture motion |

Each master has a 64 × 64 view box and only filled vector paths. There are no embedded images, external references or font dependencies. Dark variants replace orange with the exact dark color for monochrome use on light backgrounds. Lifted Slip's primary version uses both palette colors.

The three PNG boards under `docs/branding/concepts` were generated as visual concept studies. They are preserved unchanged and are not production color references. These SVG masters are original, simplified geometric interpretations of those studies, authored for reliable small-size rendering rather than pixel traces. Frame S uses two opposing crop-bracket shapes, with broad open channels. None uses an Anthropic asterisk or existing third-party symbol. No font or generated wordmark is packaged into the app icon.

## Windows icon generation

`scripts/BuildBrandAssets.cs` reads the Frame S master and renders it in software on an STA thread without opening a window, capturing the desktop or accessing the clipboard. It creates `src/SlipShot/Assets/slipshot.ico` with PNG frames at **16, 20, 24, 32, 40, 48, 64, 128 and 256 pixels**. The file stores256first, but the window loader explicitly selects the largest decoded frame: a test caught WPF's convenience decoder selecting16px instead. The orange mark occupies70% of a dark rounded tile; outer corners remain transparent.

Run the existing fallback build from the repository root:

```powershell
./scripts/build-fallback.ps1 -Test
```

The fallback build compiles and runs the asset helper before compiling the app. `-CoreOnly` skips it. The generated ICO is also committed as the source asset used by ordinary SDK builds. The application embeds that same ICO both as the executable's Windows icon and as `SlipShot.Assets.slipshot.ico` for window/tray use. Window images are cached, frozen and fully loaded before their stream closes. Tray callers receive independent icons they own and dispose.

Synthetic previews are emitted under `artifacts/brand-audit`:

- `vector-masters.png`: Frame S, Lifted Slip and Open Shutter from left to right, primary colors above dark variants.
- `icon-sizes.png`: actual-size raster icons from 16px through 256px, left to right.
- `frame-s-<size>.png`: each individual icon frame for inspection.
- `concept-small-sizes.png`: all three original vector marks at16/24/32/64px on a light background.

`BrandAssetsTests` validates the embedded ICO directory, complete size set, PNG payloads, transparency and exact interior palette. It also exercises resource lifetime and independent tray-icon ownership. These checks do not assert that Windows Explorer has refreshed its icon cache or that a live notification area has displayed the icon.
