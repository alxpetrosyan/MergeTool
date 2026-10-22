#Requires -Version 5.1
<#
.SYNOPSIS
Restores and builds the MergeTool solution in Debug and Release.
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release', 'All')]
    [string]$Configuration = 'All'
)

$ErrorActionPreference = 'Stop'
Set-Location -Path $PSScriptRoot

dotnet restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$configurations = if ($Configuration -eq 'All') { @('Debug', 'Release') } else { @($Configuration) }
foreach ($config in $configurations) {
    Write-Host "Building configuration '$config'..." -ForegroundColor Cyan
    dotnet build -c $config --no-restore
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Write-Host 'Build succeeded.' -ForegroundColor Green
