# Dependencies and asset provenance

Application runtime: Microsoft.NETCore.App and Microsoft.WindowsDesktop.App 8.x (WPF, WinForms tray, System.Drawing), Windows user32/gdi32/dwmapi/shcore. No application NuGet packages, embedded browser, network SDK or service. .NET is MIT; Windows components and Segoe UI are supplied by the OS and not redistributed as assets.

Development only, downloaded from `https://api.nuget.org/v3-flatcontainer/` into `.cache/tooling`:

| Package | Version | Use | License |
|---|---|---|---|
| Microsoft.Net.Compilers.Toolset | 4.11.0 | SDK-less C# compiler | MIT |
| NUnit / NUnitLite | 4.4.0 | Synthetic STA test runner | MIT |
| Microsoft.NET.HostModel | 3.1.16 | Build-only apphost writer; last stable standalone package, not app runtime | MIT |
| Microsoft.NETCore.App.Host.win-x64 | 8.0.11 | Microsoft native .NET 8 apphost template | MIT |

HostModel's netstandard assembly uses framework-provided System.Reflection.Metadata in the fallback; the old standalone package is deliberately confined to build tooling. Prefer the normal supported SDK publish path for future releases. The apphost template is .NET 8, not a .NET 3.1 runtime. All exact archive SHA256 values are pinned in the bootstrap script. Runtime servicing is the user's responsibility; no updater is installed.

Sources/licenses: [Roslyn](https://github.com/dotnet/roslyn/blob/main/License.txt), [NUnit](https://github.com/nunit/nunit/blob/main/LICENSE.txt), [NUnitLite package](https://www.nuget.org/packages/NUnitLite/4.4.0), [.NET runtime](https://github.com/dotnet/runtime/blob/main/LICENSE.TXT), [HostModel package](https://www.nuget.org/packages/Microsoft.NET.HostModel/3.1.16). Package archives retain their license files in the local development cache. The portable package includes the .NET apphost license.

Tray mark and editor tool icons are original geometric drawings authored for LeftShot under this repository's MIT license. No competitor assets or external icon packs. Synthetic visual fixtures contain invented checklist text and `example.test`, never user content. Public reference images were examined only for research and are not shipped or committed.

No telemetry, paid API or runtime network dependency is present. Package restore is development-time network activity only.
