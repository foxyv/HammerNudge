# Packages Dizzy Nudge for GitHub Releases.
# Usage: .\scripts\package-release.ps1 [-Version 0.1.0]

param(
    [string]$Version = "0.1.2"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $root

$dll = "src\Dizzy.Nudge\bin\Release\Dizzy.Nudge.dll"
if (-not (Test-Path $dll)) {
    Write-Host "Building Dizzy.Nudge Release..."
    dotnet build src\Dizzy.Nudge\Dizzy.Nudge.csproj -c Release
}

if (-not (Test-Path $dll)) {
    throw "Missing $dll - build failed."
}

$staging = "dist\Dizzy.Nudge-$Version"
$pluginDir = "$staging\Dizzy.Nudge"
$zipPath = "dist\Dizzy.Nudge-$Version.zip"
$notesPath = "dist\GITHUB_RELEASE_NOTES-v$Version.md"

if (Test-Path $staging) {
    Remove-Item $staging -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $pluginDir | Out-Null
Copy-Item $dll $pluginDir -Force

if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}
Compress-Archive -Path $pluginDir -DestinationPath $zipPath -Force

$notes = @"
## Summary

- Hold a hammer, look at locked (nailed) furniture, and tap Q/T/E + click to nudge it in place
- Q away/closer, T up/down, E left/right (Shift for a finer step)
- Vanilla hammer lock/unlock is unchanged when those keys are not held

## Install

Requires Sailwind + [BepInEx 5](https://thunderstore.io/c/sailwind/p/BepInEx/BepInExPack/) (Thunderstore BepInExPack recommended).

1. Download ``Dizzy.Nudge-$Version.zip`` below.
2. Extract the ``Dizzy.Nudge`` folder into ``BepInEx\plugins\`` (next to ``Sailwind.exe``: ``Sailwind\BepInEx\plugins\Dizzy.Nudge\``).
3. Launch the game.

## Contents

``````
Dizzy.Nudge/
  Dizzy.Nudge.dll
``````

Config: ``BepInEx\config\com.dizzy.sailwind.nudge.cfg``

## Source

Built from tag ``v$Version`` on this repository.
"@

New-Item -ItemType Directory -Force -Path dist | Out-Null
Set-Content -Path $notesPath -Value $notes -Encoding UTF8

Write-Host "Created: $zipPath"
Write-Host "Release notes: $notesPath"
Write-Host ""
Write-Host "Next: GitHub -> Releases -> Draft new release -> tag v$Version -> attach zip -> paste notes."
