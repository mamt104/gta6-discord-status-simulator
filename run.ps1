$ErrorActionPreference = 'Stop'
$ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$ExecutablePath = Join-Path $ProjectRoot 'dist\GTA6.exe'
if (-not (Test-Path -LiteralPath $ExecutablePath)) {
    throw 'Build not found. Run .\build.ps1 first.'
}
Start-Process -FilePath $ExecutablePath

