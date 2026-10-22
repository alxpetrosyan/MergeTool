# Bounded test runner for MVP-11: one configuration at a time, test projects sequential.
# Avoids uncontrolled parallel assembly load that previously correlated with OOM on this machine.
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repoRoot

$projects = @(
    "tests/MergeTool.Core.Tests/MergeTool.Core.Tests.csproj",
    "tests/MergeTool.Text.Tests/MergeTool.Text.Tests.csproj",
    "tests/MergeTool.Folders.Tests/MergeTool.Folders.Tests.csproj",
    "tests/MergeTool.Application.Tests/MergeTool.Application.Tests.csproj",
    "tests/MergeTool.App.Wpf.Tests/MergeTool.App.Wpf.Tests.csproj"
)

Write-Output "=== dotnet restore ==="
dotnet restore | Out-Host
if ($LASTEXITCODE -ne 0) { Pop-Location; exit $LASTEXITCODE }

Write-Output "=== dotnet build -c $Configuration ==="
dotnet build -c $Configuration --no-restore | Out-Host
if ($LASTEXITCODE -ne 0) { Pop-Location; exit $LASTEXITCODE }

$totalPassed = 0
$totalFailed = 0
foreach ($proj in $projects) {
    Write-Output "=== dotnet test $proj -c $Configuration (sequential project) ==="
    dotnet test $proj -c $Configuration --no-build -- RunConfiguration.MaxCpuCount=1 | Out-Host
    if ($LASTEXITCODE -ne 0) {
        Pop-Location
        exit $LASTEXITCODE
    }
}

Write-Output "=== ALL BOUNDED TESTS PASSED ($Configuration) ==="
Pop-Location
exit 0
