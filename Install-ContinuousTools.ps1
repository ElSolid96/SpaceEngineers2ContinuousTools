[CmdletBinding()]
param(
    [string]$Source = (Join-Path $PSScriptRoot 'stage\ContinuousTools.dll'),
    [string]$InstallDirectory = 'C:\SE2Mods\ContinuousTools'
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if (Get-Process -Name 'SpaceEngineers2' -ErrorAction SilentlyContinue) {
    throw 'Close Space Engineers 2 before installing. The script will not stop it.'
}
if (-not (Test-Path -LiteralPath $Source)) { throw "Staged plugin not found: $Source" }

$ctDestination = Join-Path $InstallDirectory 'ContinuousTools.dll'
$ctStamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$ctBackup = Join-Path $PSScriptRoot "backups\install-$ctStamp"

Write-Host "Source: $Source"
Write-Host "Destination: $ctDestination"
Write-Host "SHA256: $((Get-FileHash -LiteralPath $Source -Algorithm SHA256).Hash)"
if ((Read-Host 'Type INSTALL to approve installing ContinuousTools') -cne 'INSTALL') {
    throw 'Installation cancelled.'
}
if (Get-Process -Name 'SpaceEngineers2' -ErrorAction SilentlyContinue) {
    throw 'Space Engineers 2 started while approval was pending; installation cancelled.'
}

New-Item -ItemType Directory -Path $InstallDirectory -Force | Out-Null
New-Item -ItemType Directory -Path $ctBackup -Force | Out-Null
if (Test-Path -LiteralPath $ctDestination) {
    Copy-Item -LiteralPath $ctDestination -Destination (Join-Path $ctBackup 'ContinuousTools.previous.dll')
}

# Stage contains the plugin and its private MonoMod dependencies. Nothing is copied to the game.
foreach ($ctFile in Get-ChildItem -LiteralPath (Split-Path -Parent $Source) -File) {
    Copy-Item -LiteralPath $ctFile.FullName -Destination $InstallDirectory
}

if ((Get-FileHash -LiteralPath $Source).Hash -ne (Get-FileHash -LiteralPath $ctDestination).Hash) {
    throw 'Installed DLL hash verification failed.'
}

Write-Host "Installed and verified. Backup folder: $ctBackup"
