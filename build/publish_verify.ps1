param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$publishExe = Join-Path $repoRoot "artifacts\MergeTool-portable-win-x64\MergeTool.App.Wpf.exe"

Push-Location $repoRoot
try {
    Write-Host "=== POL-07 publish verification ($Configuration) ===" -ForegroundColor Cyan

    & "$PSScriptRoot\run_tests_bounded.ps1" -Configuration $Configuration
    & "$PSScriptRoot\publish-portable.ps1" -Configuration $Configuration

    if (-not (Test-Path $publishExe)) {
        throw "Published executable missing: $publishExe"
    }

    $required = @(
        "MergeTool.App.Wpf.exe",
        "MergeTool.App.Wpf.dll",
        "ICSharpCode.AvalonEdit.dll",
        "THIRD_PARTY_NOTICES.txt",
        "README.txt"
    )
    foreach ($name in $required) {
        $path = Join-Path (Split-Path $publishExe) $name
        if (-not (Test-Path $path)) {
            throw "Publish output missing required file: $name"
        }
    }

    Write-Host ""
    Write-Host "=== Phase-1 checkpoint against published build ===" -ForegroundColor Cyan
    & "$PSScriptRoot\phase1_checkpoint.ps1" -ExePath $publishExe

    Write-Host ""
    Write-Host "=== POL-07 publish verification PASSED ===" -ForegroundColor Green
}
finally {
    Pop-Location
}
