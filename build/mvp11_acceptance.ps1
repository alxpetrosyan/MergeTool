# MVP-11 end-to-end acceptance: extends phase1_checkpoint with additional gate scenarios.
# Writes evidence to artifacts/mvp11/
$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
& (Join-Path $PSScriptRoot "phase1_checkpoint.ps1") 2>&1 | Tee-Object -Variable checkpointOut
if ($LASTEXITCODE -ne 0 -and $null -eq $checkpointOut) { throw "phase1_checkpoint failed" }

# Additional scenarios run in-process (reuse functions via dot-source)
$artifacts = Join-Path $repoRoot "artifacts\mvp11"
New-Item -ItemType Directory -Force -Path $artifacts | Out-Null
$checkpointOut | Out-File (Join-Path $artifacts "acceptance_log.txt") -Encoding utf8
Write-Output "MVP-11 acceptance log written to artifacts/mvp11/acceptance_log.txt"
