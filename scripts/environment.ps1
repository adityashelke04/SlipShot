$repo = Split-Path $PSScriptRoot -Parent
$env:DOTNET_CLI_HOME = Join-Path $repo '.cache/dotnet-home'
$env:NUGET_PACKAGES = Join-Path $repo '.cache/nuget'
$env:NUGET_HTTP_CACHE_PATH = Join-Path $repo '.cache/nuget-http'
$env:NUGET_PLUGINS_CACHE_PATH = Join-Path $repo '.cache/nuget-plugins'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:TEMP = Join-Path $repo '.tmp'
$env:TMP = $env:TEMP
$env:TMPDIR = $env:TEMP
New-Item -ItemType Directory -Force $env:TEMP,$env:DOTNET_CLI_HOME,$env:NUGET_PACKAGES | Out-Null
# This shell and its own children only. Never touches unrelated processes.
[System.Diagnostics.Process]::GetCurrentProcess().PriorityClass = 'BelowNormal'
