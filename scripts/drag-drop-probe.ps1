param(
    [string]$AssemblyPath = 'artifacts/portable/SlipShot.dll',
    [string]$OutputDirectory = 'artifacts/drag-probe',
    [switch]$Desktop,
    [switch]$Controller
)
$ErrorActionPreference = 'Stop'
if ($Controller -and !$Desktop) { throw '-Controller requires -Desktop and explicit user authorization for synthetic input.' }
. "$PSScriptRoot/environment.ps1"
$repo = Split-Path $PSScriptRoot -Parent
$assembly = [IO.Path]::GetFullPath($(if ([IO.Path]::IsPathRooted($AssemblyPath)) { $AssemblyPath } else { Join-Path $repo $AssemblyPath }))
$output = [IO.Path]::GetFullPath($(if ([IO.Path]::IsPathRooted($OutputDirectory)) { $OutputDirectory } else { Join-Path $repo $OutputDirectory }))
$artifactRoot = [IO.Path]::GetFullPath((Join-Path $repo 'artifacts')) + [IO.Path]::DirectorySeparatorChar
if (!$output.StartsWith($artifactRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'Probe output must stay inside this repository artifacts directory.'
}
if (!(Test-Path -LiteralPath $assembly -PathType Leaf)) { throw 'Build SlipShot first or supply -AssemblyPath.' }
$compiler = Join-Path $repo '.cache/tooling/microsoft.net.compilers.toolset.4.11.0/tasks/netcore/bincore/csc.dll'
if (!(Test-Path -LiteralPath $compiler)) { throw 'Run python scripts/bootstrap-compiler.py first.' }
$dotnetRoot = Split-Path (Get-Command dotnet).Source -Parent
$runtime = Get-ChildItem "$dotnetRoot/shared/Microsoft.NETCore.App/8.*" -Directory | Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
$desktopRuntime = Get-ChildItem "$dotnetRoot/shared/Microsoft.WindowsDesktop.App/8.*" -Directory | Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
if (!$runtime -or !$desktopRuntime) { throw '.NET 8 Windows Desktop Runtime is required.' }
$refs = @(Get-ChildItem $runtime.FullName,$desktopRuntime.FullName -Filter *.dll | Where-Object {
    try { $null = [Reflection.AssemblyName]::GetAssemblyName($_.FullName); $true } catch { $false }
} | Group-Object Name | ForEach-Object { '-r:"' + $_.Group[-1].FullName + '"' })
New-Item -ItemType Directory -Force $output | Out-Null
Copy-Item -LiteralPath $assembly -Destination (Join-Path $output 'SlipShot.dll')
$response = @('-nologo','-nullable:enable','-langversion:12','-platform:x64','-warnaserror+','-target:exe',
    ('-out:"' + "$output/DragDropProbe.dll" + '"'), ('-r:"' + "$output/SlipShot.dll" + '"'),
    ('"' + "$PSScriptRoot/DragDropProbe.cs" + '"')) + $refs
$rsp = Join-Path $output 'compile.rsp'
[IO.File]::WriteAllLines($rsp,$response)
& dotnet $compiler "@$rsp"
if ($LASTEXITCODE -ne 0) { throw 'Drag probe compilation failed.' }
[IO.File]::WriteAllText("$output/DragDropProbe.runtimeconfig.json",'{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.WindowsDesktop.App","version":"8.0.0"}}}')
# Desktop mode is deliberately explicit; use only when the user authorizes synthetic input.
$probeArguments = @(if ($Desktop) { '--run-input' } else { '--headless' })
if ($Controller) { $probeArguments += '--controller' }
& dotnet "$output/DragDropProbe.dll" @probeArguments
if ($LASTEXITCODE -ne 0) { throw "Drag probe reported a failure (exit $LASTEXITCODE)." }
