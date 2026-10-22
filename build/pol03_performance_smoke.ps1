param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot

try {
    Write-Host "=== POL-03 performance smoke ($Configuration) ===" -ForegroundColor Cyan
    Write-Host "See docs/performance/POL-03_budgets.md for environment notes."
    Write-Host ""

    dotnet restore | Out-Null
    dotnet build -c $Configuration --no-restore

    $filter = "FullyQualifiedName~PerformanceSmokeTests|FullyQualifiedName~ThrottledProgressTests"
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    dotnet test tests/MergeTool.Application.Tests/MergeTool.Application.Tests.csproj `
        -c $Configuration `
        --no-build `
        --filter $filter `
        --logger "console;verbosity=normal"
    $sw.Stop()

    if ($LASTEXITCODE -ne 0) {
        throw "Performance smoke tests failed."
    }

    Write-Host ""
    Write-Host "=== POL-03 smoke completed in $($sw.Elapsed) (host wall clock) ===" -ForegroundColor Green
}
finally {
    Pop-Location
}
