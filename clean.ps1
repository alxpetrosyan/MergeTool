#Requires -Version 5.1
<#
.SYNOPSIS
Removes build output (bin/obj) and test results for the MergeTool solution.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Set-Location -Path $PSScriptRoot

foreach ($dir in @('src', 'tests')) {
    if (Test-Path $dir) {
        Get-ChildItem -Path $dir -Directory -Recurse -Include 'bin', 'obj', 'TestResults' |
            ForEach-Object {
                Write-Host "Removing $($_.FullName)"
                Remove-Item -Path $_.FullName -Recurse -Force
            }
    }
}

Write-Host 'Clean complete.' -ForegroundColor Green
