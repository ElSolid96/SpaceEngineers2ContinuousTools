[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$BackupDll,
    [string]$InstallDirectory = 'C:\SE2Mods\ContinuousTools'
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if (Get-Process -Name 'SpaceEngineers2' -ErrorAction SilentlyContinue) {
    throw 'Close Space Engineers 2 before rollback.'
}
$ctSource = (Resolve-Path -LiteralPath $BackupDll).Path
$ctDestination = Join-Path $InstallDirectory 'ContinuousTools.dll'
Write-Host "Rollback source: $ctSource"
Write-Host "Destination: $ctDestination"
if ((Read-Host 'Type ROLLBACK to approve replacing ContinuousTools.dll') -cne 'ROLLBACK') {
    throw 'Rollback cancelled.'
}
Copy-Item -LiteralPath $ctSource -Destination $ctDestination
if ((Get-FileHash -LiteralPath $ctSource).Hash -ne (Get-FileHash -LiteralPath $ctDestination).Hash) {
    throw 'Rollback hash verification failed.'
}
Write-Host 'Rollback installed and verified. Build Planner was not touched.'
