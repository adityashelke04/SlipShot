param([string]$Version='0.2.7-preview.1')
$ErrorActionPreference='Stop'
. "$PSScriptRoot/environment.ps1"
if ($Version -notmatch '^\d+\.\d+\.\d+(?:-[a-z0-9.]+)?$') { throw 'Use a simple semantic version.' }
$repo=Split-Path $PSScriptRoot -Parent
$source=Join-Path $repo 'artifacts/portable'
$release=Join-Path $repo "artifacts/releases/$Version"
if(Test-Path -LiteralPath $release) { throw 'This release already exists. Preserve it and choose a new version.' }
foreach($name in 'SlipShot.exe','SlipShot.dll','SlipShot.runtimeconfig.json') { if(!(Test-Path -LiteralPath (Join-Path $source $name))) {throw "Missing build output: $name"} }
$destination=Join-Path $release 'win-x64'
New-Item -ItemType Directory -Path $destination -Force | Out-Null
foreach($name in 'SlipShot.exe','SlipShot.dll','SlipShot.runtimeconfig.json') {Copy-Item -LiteralPath (Join-Path $source $name) -Destination $destination}
foreach($name in 'README.md','TESTING.md','LICENSE','DEPENDENCIES.md','CHANGELOG.md','ARCHITECTURE.md','REFERENCE_NOTES.md') {Copy-Item -LiteralPath (Join-Path $repo $name) -Destination $destination}
Copy-Item -LiteralPath (Join-Path $repo '.cache/tooling/microsoft.netcore.app.host.win-x64.8.0.11/LICENSE.TXT') -Destination (Join-Path $destination 'DOTNET-LICENSE.txt')
$releaseDocs=Join-Path $destination 'docs'
New-Item -ItemType Directory -Path $releaseDocs | Out-Null
foreach($name in 'PUBLISHING.md','DRAG_REVIEW.md','REFERENCE_REFRESH.md','EDITOR_REDESIGN.md','PREVIEW_REVIEW.md','QUICK_SAVE_REVIEW.md','SAVE_ONCE_REVIEW.md','FOLDER_ACTION_REVIEW.md') {Copy-Item -LiteralPath (Join-Path $repo "docs/$name") -Destination $releaseDocs}
$imageDocs=Join-Path $releaseDocs 'images'
New-Item -ItemType Directory -Path $imageDocs | Out-Null
foreach($name in 'editor-dark.png','editor-light.png','preview-dark.png','README.md') {Copy-Item -LiteralPath (Join-Path $repo "docs/images/$name") -Destination $imageDocs}
$brandingDocs=Join-Path $releaseDocs 'branding'
New-Item -ItemType Directory -Path (Join-Path $brandingDocs 'concepts') -Force | Out-Null
foreach($name in 'README.md','ASSETS.md','LOGO_AUDIT.md','FINAL_AUDIT.md') {Copy-Item -LiteralPath (Join-Path $repo "docs/branding/$name") -Destination $brandingDocs}
foreach($name in 'frame-s.png','lifted-slip.png','open-shutter.png') {Copy-Item -LiteralPath (Join-Path $repo "docs/branding/concepts/$name") -Destination (Join-Path $brandingDocs 'concepts')}
$brandSources=Join-Path $destination 'src/SlipShot/Assets'
New-Item -ItemType Directory -Path $brandSources -Force | Out-Null
foreach($name in 'frame-s.svg','frame-s-dark.svg','lifted-slip.svg','lifted-slip-dark.svg','open-shutter.svg','open-shutter-dark.svg','slipshot.ico') {Copy-Item -LiteralPath (Join-Path $repo "src/SlipShot/Assets/$name") -Destination $brandSources}
$hashes=Get-ChildItem -LiteralPath $destination -File -Recurse | Sort-Object FullName | ForEach-Object { (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()+'  '+$_.FullName.Substring($destination.Length+1).Replace('\','/') }
[IO.File]::WriteAllLines((Join-Path $destination 'SHA256SUMS.txt'),$hashes)
$archive=Join-Path $release "SlipShot-$Version-win-x64.zip"
Compress-Archive -Path "$destination/*" -DestinationPath $archive -CompressionLevel Optimal
$archiveHash=(Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant()
[IO.File]::WriteAllText("$archive.sha256",$archiveHash+'  '+[IO.Path]::GetFileName($archive)+[Environment]::NewLine)
Get-ChildItem -LiteralPath $destination -File | Select-Object Name,Length
Write-Output "ZIP: $archive"
Write-Output "SHA256: $archiveHash"
