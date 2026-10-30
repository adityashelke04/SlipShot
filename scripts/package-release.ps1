param([string]$Version='0.1.0-preview.1')
$ErrorActionPreference='Stop'
. "$PSScriptRoot/environment.ps1"
if ($Version -notmatch '^\d+\.\d+\.\d+(?:-[a-z0-9.]+)?$') { throw 'Use a simple semantic version.' }
$repo=Split-Path $PSScriptRoot -Parent
$source=Join-Path $repo 'artifacts/portable'
$release=Join-Path $repo "artifacts/releases/$Version"
if(Test-Path -LiteralPath $release) { throw 'This release already exists. Preserve it and choose a new version.' }
foreach($name in 'LeftShot.exe','LeftShot.dll','LeftShot.runtimeconfig.json') { if(!(Test-Path -LiteralPath (Join-Path $source $name))) {throw "Missing build output: $name"} }
$destination=Join-Path $release 'win-x64'
New-Item -ItemType Directory -Path $destination -Force | Out-Null
foreach($name in 'LeftShot.exe','LeftShot.dll','LeftShot.runtimeconfig.json') {Copy-Item -LiteralPath (Join-Path $source $name) -Destination $destination}
foreach($name in 'README.md','TESTING.md','LICENSE','DEPENDENCIES.md','CHANGELOG.md','ARCHITECTURE.md','REFERENCE_NOTES.md') {Copy-Item -LiteralPath (Join-Path $repo $name) -Destination $destination}
Copy-Item -LiteralPath (Join-Path $repo '.cache/tooling/microsoft.netcore.app.host.win-x64.8.0.11/LICENSE.TXT') -Destination (Join-Path $destination 'DOTNET-LICENSE.txt')
$hashes=Get-ChildItem -LiteralPath $destination -File | Sort-Object Name | ForEach-Object { (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()+'  '+$_.Name }
[IO.File]::WriteAllLines((Join-Path $destination 'SHA256SUMS.txt'),$hashes)
$archive=Join-Path $release "LeftShot-$Version-win-x64.zip"
Compress-Archive -Path "$destination/*" -DestinationPath $archive -CompressionLevel Optimal
$archiveHash=(Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText("$archive.sha256",$archiveHash+'  '+[IO.Path]::GetFileName($archive)+[Environment]::NewLine)
Get-ChildItem -LiteralPath $destination -File | Select-Object Name,Length
Write-Output "ZIP: $archive"
Write-Output "SHA256: $archiveHash"
