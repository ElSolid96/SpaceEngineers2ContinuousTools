[CmdletBinding()]
param(
    [string]$GameDir = 'C:\Program Files (x86)\Steam\steamapps\common\SpaceEngineers2\Game2'
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$ctRoot = $PSScriptRoot
$ctProject = Join-Path $ctRoot 'ContinuousTools\ContinuousTools.csproj'
$ctTests = Join-Path $ctRoot 'ContinuousTools.Tests\ContinuousTools.Tests.csproj'
$ctOutput = Join-Path $ctRoot 'ContinuousTools\bin'
$ctStage = Join-Path $ctRoot 'stage'

Push-Location $ctRoot
try {
    & dotnet restore 'ContinuousTools.slnx'
    if ($LASTEXITCODE -ne 0) { throw 'Restore failed.' }
    & dotnet build 'ContinuousTools.slnx' -c Release --no-restore "-p:GameDir=$GameDir"
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    & dotnet test $ctTests -c Release --no-build --no-restore "-p:GameDir=$GameDir"
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
} finally { Pop-Location }

if (Test-Path -LiteralPath $ctStage) {
    Get-ChildItem -LiteralPath $ctStage -Force | Remove-Item -Recurse -Force
} else {
    New-Item -ItemType Directory -Path $ctStage | Out-Null
}

foreach ($ctFile in Get-ChildItem -LiteralPath $ctOutput -File) {
    if ($ctFile.Extension -eq '.pdb') { continue }
    Copy-Item -LiteralPath $ctFile.FullName -Destination $ctStage
}

$ctDll = Join-Path $ctStage 'ContinuousTools.dll'
Write-Host "Build and tests passed."
Write-Host "Staged DLL: $ctDll"
Write-Host "SHA256: $((Get-FileHash -LiteralPath $ctDll -Algorithm SHA256).Hash)"
