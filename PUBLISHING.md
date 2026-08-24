param(
    [string]$ApplicationId,
    [string]$AssetKey = 'gtavi_cover'
)

$ErrorActionPreference = 'Stop'
$ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$DistDirectory = Join-Path $ProjectRoot 'dist'
$ExecutablePath = Join-Path $DistDirectory 'GTA6.exe'

if (-not (Test-Path -LiteralPath $ExecutablePath)) {
    throw 'Build not found. Run .\build.ps1 first.'
}

if (-not $ApplicationId) {
    $ApplicationId = Read-Host 'Paste your Discord Application ID (not your user ID)'
}
$ApplicationId = $ApplicationId.Trim()
if ($ApplicationId -notmatch '^\d{15,25}$') {
    throw 'Invalid Application ID: expected 15-25 digits.'
}

$AssetKey = $AssetKey.Trim()
if ($AssetKey -notmatch '^[A-Za-z0-9_-]{2,256}$') {
    throw 'Invalid asset key. Use letters, digits, underscores, or hyphens.'
}

[System.IO.File]::WriteAllText((Join-Path $DistDirectory 'gta6-presence.txt'), $ApplicationId)
[System.IO.File]::WriteAllText((Join-Path $DistDirectory 'gta6-image.txt'), $AssetKey)

Write-Host 'Configuration saved.'
Write-Host "Application ID: $ApplicationId"
Write-Host "Large image asset key: $AssetKey"
Write-Host "Run: $ExecutablePath"

