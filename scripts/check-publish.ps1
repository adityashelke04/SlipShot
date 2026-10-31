param([switch]$CheckRemote)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
Set-Location -LiteralPath $repo
git rev-parse --show-toplevel
if($LASTEXITCODE -ne 0){throw 'This folder is not a Git repository.'}
$origin=git remote get-url origin 2>$null
if($LASTEXITCODE -ne 0){throw 'No origin remote. Create a GitHub repository, then run: git remote add origin <repository-url>'}
Write-Output "Origin: $origin"
$branch=git branch --show-current
if(!$branch){throw 'Detached HEAD. Switch to a branch before publishing.'}
$tracking=git rev-parse --abbrev-ref '@{upstream}' 2>$null
if($LASTEXITCODE -ne 0){throw "No upstream for $branch. First push: git push -u origin $branch"}
Write-Output "Tracking: $tracking"
if($CheckRemote){
    git ls-remote --exit-code origin "refs/heads/$branch"
    if($LASTEXITCODE -ne 0){throw 'Remote branch cannot be read. Check URL, GitHub authentication and repository permissions.'}
}
python (Join-Path $PSScriptRoot 'repo-privacy-check.py')
if($LASTEXITCODE -ne 0){throw 'Repository privacy check failed. Resolve private/generated files before publishing.'}
Write-Output 'Publish setup and repository privacy checks passed. Use git push for subsequent commits. This check changes no Git or account settings.'
