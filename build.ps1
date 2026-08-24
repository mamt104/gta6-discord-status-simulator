param(
    [switch]$Clean
)

$ErrorActionPreference = 'Stop'
$ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$SourcePath = Join-Path $ProjectRoot 'src\GtaViPresence.cs'
$ToolSourcePath = Join-Path $ProjectRoot 'tools\IconMaker.cs'
$BuildDirectory = Join-Path $ProjectRoot 'build'
$DistDirectory = Join-Path $ProjectRoot 'dist'
$ExecutablePath = Join-Path $DistDirectory 'GTA6.exe'

if ($Clean) {
    foreach ($Target in @($BuildDirectory, $DistDirectory)) {
        $ResolvedRoot = [System.IO.Path]::GetFullPath($ProjectRoot)
        $ResolvedTarget = [System.IO.Path]::GetFullPath($Target)
        if (-not $ResolvedTarget.StartsWith($ResolvedRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to clean a path outside the project: $ResolvedTarget"
        }
        if (Test-Path -LiteralPath $ResolvedTarget) {
            Remove-Item -LiteralPath $ResolvedTarget -Recurse -Force
        }
    }
}

New-Item -ItemType Directory -Path $BuildDirectory, $DistDirectory -Force | Out-Null

$CompilerCandidates = @(
    "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe",
    "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)
$Compiler = $CompilerCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $Compiler) {
    throw 'Microsoft .NET Framework C# compiler was not found. Enable/install .NET Framework 4.x.'
}

$CompilerArguments = @(
    '/nologo',
    '/target:winexe',
    '/platform:anycpu',
    '/optimize+',
    '/r:System.dll',
    '/r:System.Core.dll',
    '/r:System.Drawing.dll',
    '/r:System.Windows.Forms.dll'
)

$IconPng = Join-Path $ProjectRoot 'assets\app-icon.png'
if (Test-Path -LiteralPath $IconPng) {
    $IconMakerPath = Join-Path $BuildDirectory 'IconMaker.exe'
    $IconPath = Join-Path $BuildDirectory 'app.ico'
    & $Compiler /nologo /target:exe /optimize+ /r:System.Drawing.dll "/out:$IconMakerPath" $ToolSourcePath
    if ($LASTEXITCODE -ne 0) { throw 'IconMaker compilation failed.' }
    & $IconMakerPath $IconPng $IconPath
    if ($LASTEXITCODE -ne 0) { throw 'Icon generation failed.' }
    $CompilerArguments += "/win32icon:$IconPath"
}

$CompilerArguments += "/out:$ExecutablePath"
$CompilerArguments += $SourcePath
& $Compiler @CompilerArguments
if ($LASTEXITCODE -ne 0) { throw 'Application compilation failed.' }

$ImageExample = Join-Path $ProjectRoot 'config\gta6-image.example.txt'
$ImageDestination = Join-Path $DistDirectory 'gta6-image.txt'
if (-not (Test-Path -LiteralPath $ImageDestination)) {
    Copy-Item -LiteralPath $ImageExample -Destination $ImageDestination
}

$ProfileExample = Join-Path $ProjectRoot 'config\gta6-profile.example.ini'
$ProfileDestination = Join-Path $DistDirectory 'gta6-profile.ini'
if (-not (Test-Path -LiteralPath $ProfileDestination)) {
    Copy-Item -LiteralPath $ProfileExample -Destination $ProfileDestination
}

$Version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($ExecutablePath).FileVersion
Write-Host "Build completed: $ExecutablePath"
Write-Host "Version: $Version"
Write-Host 'Next: run .\configure.ps1 and enter your Discord Application ID.'

