param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [switch]$SelfContained
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$artifacts = Join-Path $repoRoot "artifacts"
$publishDir = Join-Path $artifacts "MergeTool-portable-win-x64"

Push-Location $repoRoot
try {
    Write-Host "=== Portable publish ($Configuration) ===" -ForegroundColor Cyan

    if (Test-Path $publishDir) {
        Remove-Item $publishDir -Recurse -Force
    }
    if (-not (Test-Path $artifacts)) {
        New-Item -ItemType Directory -Path $artifacts | Out-Null
    }

    $project = "src/MergeTool.App.Wpf/MergeTool.App.Wpf.csproj"
    $args = @(
        "publish", $project,
        "-c", $Configuration,
        "-r", "win-x64",
        "-o", $publishDir,
        "/p:PublishSingleFile=false",
        "/p:IncludeNativeLibrariesForSelfExtract=false"
    )

    if ($SelfContained) {
        $args += "--self-contained", "true"
    }
    else {
        $args += "--self-contained", "false"
        Write-Host "Framework-dependent publish: requires .NET 10 Desktop Runtime on the target machine."
    }

    dotnet @args

    $noticePath = Join-Path $publishDir "THIRD_PARTY_NOTICES.txt"
    @"
MergeTool third-party notices
=============================

.NET Runtime / SDK — MIT — https://github.com/dotnet/runtime
WPF — MIT — https://github.com/dotnet/wpf
AvalonEdit 6.3.1.120 — MIT — https://github.com/icsharpcode/AvalonEdit
MSTest (tests only) — MIT — https://github.com/microsoft/testfx

See docs/DEPENDENCIES.md for the authoritative dependency record.
"@ | Set-Content -Path $noticePath -Encoding UTF8

    $readmePath = Join-Path $publishDir "README.txt"
    @"
MergeTool (portable Windows x64)
================================

Requirements:
  Windows 10/11 x64
  .NET 10 Desktop Runtime (unless this folder was built with -SelfContained)

Quick start:
  1. Run MergeTool.App.Wpf.exe
  2. Click New File Compare, choose two text files, press Compare
  3. Use F7 / Shift+F7 to move between differences

Full documentation is in the source tree at docs/user/ when available.
See QUICK_START.md, USER_GUIDE.md, TROUBLESHOOTING.md, KNOWN_LIMITATIONS.md.
"@ | Set-Content -Path $readmePath -Encoding UTF8

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed with exit code $LASTEXITCODE"
    }

    Write-Host ""
    Write-Host "Published to: $publishDir" -ForegroundColor Green
    Write-Host "Run: $publishDir\MergeTool.App.Wpf.exe"
}
finally {
    Pop-Location
}
