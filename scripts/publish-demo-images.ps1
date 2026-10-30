$ErrorActionPreference='Stop'
. "$PSScriptRoot/environment.ps1"
$repo=Split-Path $PSScriptRoot -Parent
$source=Join-Path $repo 'artifacts/visual'
$destination=Join-Path $repo 'docs/images'
# The harness creates its own document from literal drawing commands. Never copy
# data/drag-out, user captures, desktop screenshots or arbitrary input directories.
& "$PSScriptRoot/build-fallback.ps1" -VisualCheck
if($LASTEXITCODE -ne 0){throw 'Synthetic UI rendering failed.'}
New-Item -ItemType Directory -Path $destination -Force | Out-Null
$images=@{
    'editor-dark-1240x820.png'='editor-dark.png'
    'editor-light-1240x820.png'='editor-light.png'
    'preview-dark.png'='preview-dark.png'
}
foreach($name in $images.Keys){
    Copy-Item -LiteralPath (Join-Path $source $name) -Destination (Join-Path $destination $images[$name])
}
Get-ChildItem -LiteralPath $destination -Filter *.png | ForEach-Object {Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256}
Write-Output 'Only synthetic UI images copied. Visually review these exact files before committing.'
