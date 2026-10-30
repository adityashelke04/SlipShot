param([switch]$Test, [switch]$CoreOnly, [switch]$VisualCheck)
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/environment.ps1"
$repo = Split-Path $PSScriptRoot -Parent
$tooling = Join-Path $repo '.cache/tooling'
$compiler = Join-Path $tooling 'microsoft.net.compilers.toolset.4.11.0/tasks/netcore/bincore/csc.dll'
if (!(Test-Path $compiler)) { throw 'Run python scripts/bootstrap-compiler.py first. This downloads small official development packages into .cache/tooling.' }
$dotnetRoot = Split-Path (Get-Command dotnet).Source -Parent
$runtime = Get-ChildItem "$dotnetRoot/shared/Microsoft.NETCore.App/8.*" -Directory | Where-Object { $_.Name -match '^8\.\d+\.\d+$' } | Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
$desktop = Get-ChildItem "$dotnetRoot/shared/Microsoft.WindowsDesktop.App/8.*" -Directory | Where-Object { $_.Name -match '^8\.\d+\.\d+$' } | Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
if (!$runtime -or !$desktop) { throw '.NET 8 x64 Windows Desktop Runtime is required.' }
$out = Join-Path $repo 'artifacts/portable'
New-Item -ItemType Directory -Force $out | Out-Null
$refs = @(Get-ChildItem $runtime.FullName,$desktop.FullName -Filter *.dll | Where-Object {
    try { $null = [Reflection.AssemblyName]::GetAssemblyName($_.FullName); $true } catch { $false }
} | Group-Object Name | ForEach-Object { '-r:"' + $_.Group[-1].FullName + '"' })
$sources = @(Get-ChildItem "$repo/src/LeftShot" -Filter *.cs -Recurse | Where-Object { $_.FullName -notmatch '[\\/](obj|bin)[\\/]' })
if ($CoreOnly) { $sources = @($sources | Where-Object { $_.FullName -match '[\\/]Core[\\/]' }) }
$options = @('-nologo','-nullable:enable','-langversion:12','-platform:x64','-optimize+','-deterministic+','-warnaserror+')
$target = if ($CoreOnly) { '-target:library' } else { '-target:winexe' }
$response = $options + $target + ('-out:"' + "$out/LeftShot.dll" + '"') + $refs + @($sources | ForEach-Object { '"' + $_.FullName + '"' })
if (!$CoreOnly) { $response += '-win32manifest:"' + "$repo/src/LeftShot/app.manifest" + '"' }
$rsp = Join-Path $repo '.tmp/app.rsp'
[IO.File]::WriteAllLines($rsp,$response)
& dotnet $compiler "@$rsp"
if ($LASTEXITCODE -ne 0) { throw 'Application compilation failed.' }
$config = '{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.WindowsDesktop.App","version":"8.0.0"}}}'
[IO.File]::WriteAllText("$out/LeftShot.runtimeconfig.json",$config)
if (!$CoreOnly) {
    $buildTools = Join-Path $repo '.tmp/build-tools'
    New-Item -ItemType Directory -Force $buildTools | Out-Null
    $hostModel = Join-Path $tooling 'microsoft.net.hostmodel.3.1.16/lib/netstandard2.0/Microsoft.NET.HostModel.dll'
    if (!(Test-Path $hostModel)) { throw 'Run bootstrap-compiler.py to acquire the official apphost packaging library.' }
    Copy-Item $hostModel $buildTools
    $response = $options + '-target:exe' + ('-out:"' + "$buildTools/CreateHost.dll" + '"') + $refs + ('-r:"' + $hostModel + '"') + ('"' + "$PSScriptRoot/CreateHost.cs" + '"')
    [IO.File]::WriteAllLines($rsp,$response)
    & dotnet $compiler "@$rsp"
    if ($LASTEXITCODE -ne 0) { throw 'Host packaging helper compilation failed.' }
    [IO.File]::WriteAllText("$buildTools/CreateHost.runtimeconfig.json",$config)
    & dotnet "$buildTools/CreateHost.dll" "$tooling/microsoft.netcore.app.host.win-x64.8.0.11/runtimes/win-x64/native/apphost.exe" "$out/LeftShot.exe" "$out/LeftShot.dll"
    if ($LASTEXITCODE -ne 0) { throw 'Host packaging failed.' }
}
if ($Test) {
    $testout = Join-Path $repo 'artifacts/tests'
    New-Item -ItemType Directory -Force $testout | Out-Null
    Copy-Item "$out/LeftShot.dll" "$testout/LeftShot.dll"
    foreach ($package in 'nunit','nunitlite') {
        Get-ChildItem "$tooling/$package.4.4.0/lib/net8.0/*.dll" | Copy-Item -Destination $testout
    }
    $testSources = @(Get-ChildItem "$repo/tests/LeftShot.Tests" -Filter *.cs -Recurse | Where-Object { $_.FullName -notmatch '[\\/](obj|bin)[\\/]' })
    if ($CoreOnly) { $testSources = @($testSources | Where-Object { $_.Name -in 'CoreTests.cs','TestProgram.cs' }) }
    $response = $options + '-target:exe' + ('-out:"' + "$testout/LeftShot.Tests.dll" + '"') + $refs + @('-r:"' + "$testout/LeftShot.dll" + '"','-r:"' + "$testout/nunit.framework.dll" + '"','-r:"' + "$testout/nunitlite.dll" + '"') + @($testSources | ForEach-Object { '"' + $_.FullName + '"' })
    [IO.File]::WriteAllLines($rsp,$response)
    & dotnet $compiler "@$rsp"
    if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
    [IO.File]::WriteAllText("$testout/LeftShot.Tests.runtimeconfig.json",$config)
    & dotnet "$testout/LeftShot.Tests.dll" --workers=0 "--work=$testout" --result=TestResult.xml
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
}
if ($VisualCheck -and !$CoreOnly) {
    $visualTools = Join-Path $repo 'artifacts/visual'
    New-Item -ItemType Directory -Force $visualTools | Out-Null
    Copy-Item "$out/LeftShot.dll" $visualTools
    $response = $options + '-target:exe' + ('-out:"' + "$visualTools/VisualCheck.dll" + '"') + $refs + ('-r:"' + "$out/LeftShot.dll" + '"') + ('"' + "$PSScriptRoot/VisualCheck.cs" + '"')
    [IO.File]::WriteAllLines($rsp,$response)
    & dotnet $compiler "@$rsp"
    if ($LASTEXITCODE -ne 0) { throw 'Synthetic visual harness compilation failed.' }
    [IO.File]::WriteAllText("$visualTools/VisualCheck.runtimeconfig.json",$config)
    & dotnet "$visualTools/VisualCheck.dll" $visualTools
    if ($LASTEXITCODE -ne 0) { throw 'Synthetic offscreen visual check failed.' }
}
