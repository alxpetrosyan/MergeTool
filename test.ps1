#Requires -Version 5.1
<#
.SYNOPSIS
Runs all MergeTool tests in Debug and Release.
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release', 'All')]
    [string]$Configuration = 'All'
)

$ErrorActionPreference = 'Stop'
Set-Location -Path $PSScriptRoot

$configurations = if ($Configuration -eq 'All') { @('Debug', 'Release') } else { @($Configuration) }
foreach ($config in $configurations) {
    Write-Host "Testing configuration '$config'..." -ForegroundColor Cyan
    # -m:1 runs test projects sequentially; parallel test hosts can exhaust
    # the Windows commit limit on memory-constrained machines.
    dotnet test -c $config -m:1
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Write-Host 'All tests passed.' -ForegroundColor Green
