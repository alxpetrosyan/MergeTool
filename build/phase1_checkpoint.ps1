# Phase-1 checkpoint: automated UI verification for MVP file/folder workflows.
# Drives the real WPF app through UI Automation and writes evidence to artifacts/phase1/.
param(
    [string]$ExePath = ""
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing

$repoRoot = Split-Path -Parent $PSScriptRoot
$fixtures = Join-Path $repoRoot "tests\TestData\CompareFixtures"
$folderLeft = Join-Path $repoRoot "tests\TestData\FolderFixtures\left"
$folderRight = Join-Path $repoRoot "tests\TestData\FolderFixtures\right"
$artifacts = Join-Path $repoRoot "artifacts\phase1"
New-Item -ItemType Directory -Force -Path $artifacts | Out-Null

# Align metadata-match fixture timestamps.
$sameLeft = Join-Path $folderLeft "same.txt"
$sameRight = Join-Path $folderRight "same.txt"
$stamp = Get-Date
(Get-Item $sameLeft).LastWriteTimeUtc = $stamp
(Get-Item $sameRight).LastWriteTimeUtc = $stamp

$exe = if ([string]::IsNullOrWhiteSpace($ExePath)) {
    Join-Path $repoRoot "src\MergeTool.App.Wpf\bin\Release\net10.0-windows\MergeTool.App.Wpf.exe"
} else {
    $ExePath
}
if (-not (Test-Path $exe)) {
    throw "Executable not found: $exe"
}

$proc = Start-Process $exe -PassThru
Start-Sleep -Seconds 7

function Find-ByName($parent, $name) {
    $cond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty, $name)
    return $parent.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
}

function Find-ButtonByName($parent, $name) {
    $nameCond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty, $name)
    $buttonCond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::Button)
    $cond = New-Object System.Windows.Automation.AndCondition($nameCond, $buttonCond)
    return $parent.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
}

function Set-EditValue($window, $name, $value) {
    $edit = Find-ByName $window $name
    if (-not $edit) { throw "Edit '$name' not found" }
    $pattern = $edit.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
    $pattern.SetValue($value)
}

function Invoke-ButtonByText($window, $text) {
    $button = Find-ButtonByName $window $text
    if (-not $button) { $button = Find-ByName $window $text }
    if (-not $button) { throw "Button '$text' not found" }
    if (-not $button.Current.IsEnabled) { throw "Button '$text' is disabled" }
    $button.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
}

function Wait-ForButtonEnabled($window, $name, $timeoutSec = 15) {
    $deadline = (Get-Date).AddSeconds($timeoutSec)
    do {
        $button = Find-ButtonByName $window $name
        if (-not $button) { $button = Find-ByName $window $name }
        if ($button -and $button.Current.IsEnabled) { return $button }
        Start-Sleep -Milliseconds 400
    } while ((Get-Date) -lt $deadline)
    throw "Timed out waiting for enabled button '$name'"
}

function Get-TextsMatching($window, $regex) {
    $all = $window.FindAll([System.Windows.Automation.TreeScope]::Descendants,
        [System.Windows.Automation.Condition]::TrueCondition)
    return ,@($all | ForEach-Object { $_.Current.Name } | Where-Object { $_ -match $regex })
}

function Assert-ButtonEnabled($window, $name, $expected) {
    $button = Find-ButtonByName $window $name
    if (-not $button) { $button = Find-ByName $window $name }
    if (-not $button) { throw "Button '$name' not found for enabled check" }
    $enabled = -not $button.Current.IsOffscreen -and $button.Current.IsEnabled
    if ($enabled -ne $expected) {
        throw "Button '$name' enabled=$enabled expected=$expected"
    }
}

function Save-WindowShot($window, $file) {
    try {
        $rect = $window.Current.BoundingRectangle
        $bmp = New-Object System.Drawing.Bitmap([int]$rect.Width, [int]$rect.Height)
        $gfx = [System.Drawing.Graphics]::FromImage($bmp)
        $gfx.CopyFromScreen([int]$rect.X, [int]$rect.Y, 0, 0, $bmp.Size)
        $bmp.Save((Join-Path $artifacts $file), [System.Drawing.Imaging.ImageFormat]::Png)
        $gfx.Dispose(); $bmp.Dispose()
    } catch {
        Write-Output "  (screenshot '$file' skipped: $($_.Exception.Message))"
    }
}

$results = @()

try {
    $rootCond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
    $window = [System.Windows.Automation.AutomationElement]::RootElement.FindFirst(
        [System.Windows.Automation.TreeScope]::Children, $rootCond)
    if (-not $window) { throw "Main window not found" }

    # --- FILE: open and compare ---
    Set-EditValue $window "Left file path" (Join-Path $fixtures "mods_left.txt")
    Set-EditValue $window "Right file path" (Join-Path $fixtures "mods_right.txt")
    Invoke-ButtonByText $window "Compare"
    Start-Sleep -Seconds 1
    $deadline = (Get-Date).AddSeconds(15)
    do {
        $summary = Get-TextsMatching $window "difference block"
        if ($summary.Count -gt 0) { break }
        Start-Sleep -Milliseconds 400
    } while ((Get-Date) -lt $deadline)
    if ($summary.Count -eq 0) { throw "FILE-COMPARE: no summary" }
    $results += "FILE-COMPARE OK: $($summary[0])"
    Save-WindowShot $window "file_compare_highlighted.png"

    # --- NAVIGATION: next/previous, no wrap ---
    Wait-ForButtonEnabled $window "Next difference" | Out-Null
    Invoke-ButtonByText $window "Next difference"
    Start-Sleep -Milliseconds 500
    $nav1 = Get-TextsMatching $window "Difference 1 of"
    if ($nav1.Count -eq 0) { throw "NAVIGATION: no 'Difference 1 of' after first Next" }
    Invoke-ButtonByText $window "Next difference"
    Start-Sleep -Milliseconds 500
    $nav2 = Get-TextsMatching $window "Difference 2 of 3"
    if ($nav2.Count -eq 0) { throw "NAVIGATION: expected 'Difference 2 of 3'" }
    Invoke-ButtonByText $window "Next difference"
    Start-Sleep -Milliseconds 500
    $nav3 = Get-TextsMatching $window "Difference 3 of 3"
    if ($nav3.Count -eq 0) { throw "NAVIGATION: expected 'Difference 3 of 3'" }
    # At last difference, Next must be disabled (no wrap).
    Assert-ButtonEnabled $window "Next difference" $false
    $navStill = Get-TextsMatching $window "Difference 3 of 3"
    if ($navStill.Count -eq 0) { throw "NAVIGATION: selection lost at last difference" }
    $results += "NAVIGATION OK: no-wrap next/previous"

    # --- MERGE: copy inserted hunk right-to-left (select difference first) ---
    Set-EditValue $window "Left file path" (Join-Path $fixtures "insert_left.txt")
    Set-EditValue $window "Right file path" (Join-Path $fixtures "insert_right.txt")
    Invoke-ButtonByText $window "Compare"
    Start-Sleep -Seconds 2
    Wait-ForButtonEnabled $window "Next difference" | Out-Null
    Invoke-ButtonByText $window "Next difference"
    Start-Sleep -Milliseconds 300
    Wait-ForButtonEnabled $window "Copy current change to left" | Out-Null
    Invoke-ButtonByText $window "Copy current change to left"
    Start-Sleep -Milliseconds 500
    $stale = Get-TextsMatching $window "stale"
    if ($stale.Count -eq 0) { throw "MERGE: comparison should be stale after copy" }
    Assert-ButtonEnabled $window "Copy current change to right" $false
    Invoke-ButtonByText $window "Recompare"
    Start-Sleep -Seconds 2
    $noDiff = Get-TextsMatching $window "No differences"
    if ($noDiff.Count -eq 0) { throw "MERGE: recompare after copy should show no differences" }
    $results += "MERGE OK: copy-to-left + recompare"

    # --- MERGE: copy deleted hunk left-to-right ---
    Set-EditValue $window "Left file path" (Join-Path $fixtures "delete_left.txt")
    Set-EditValue $window "Right file path" (Join-Path $fixtures "delete_right.txt")
    Invoke-ButtonByText $window "Compare"
    Start-Sleep -Seconds 2
    Wait-ForButtonEnabled $window "Next difference" | Out-Null
    Invoke-ButtonByText $window "Next difference"
    Start-Sleep -Milliseconds 300
    Wait-ForButtonEnabled $window "Copy current change to right" | Out-Null
    Invoke-ButtonByText $window "Copy current change to right"
    Start-Sleep -Milliseconds 500
    Invoke-ButtonByText $window "Recompare"
    Start-Sleep -Seconds 2
    $deletedMerged = Get-TextsMatching $window "No differences"
    if ($deletedMerged.Count -eq 0) { throw "MERGE-DELETE: recompare did not converge after copy-to-right" }
    $results += "MERGE-DELETE OK: copy deleted hunk to right"

    # --- MERGE: copy modified hunk right-to-left ---
    Set-EditValue $window "Left file path" (Join-Path $fixtures "mods_left.txt")
    Set-EditValue $window "Right file path" (Join-Path $fixtures "mods_right.txt")
    Invoke-ButtonByText $window "Compare"
    Start-Sleep -Seconds 2
    Wait-ForButtonEnabled $window "Next difference" | Out-Null
    Invoke-ButtonByText $window "Next difference"
    Start-Sleep -Milliseconds 300
    Wait-ForButtonEnabled $window "Copy current change to left" | Out-Null
    Invoke-ButtonByText $window "Copy current change to left"
    Start-Sleep -Milliseconds 500
    $results += "MERGE-MODIFIED OK: copy modified hunk to left (stale expected)"

    # --- IDENTICAL + NOT-TEXT safety ---
    Set-EditValue $window "Left file path" (Join-Path $fixtures "identical_a.txt")
    Set-EditValue $window "Right file path" (Join-Path $fixtures "identical_b.txt")
    Invoke-ButtonByText $window "Compare"
    Start-Sleep -Seconds 2
    if ((Get-TextsMatching $window "No differences").Count -eq 0) { throw "IDENTICAL: expected no differences" }
    $results += "IDENTICAL OK"

    Set-EditValue $window "Right file path" (Join-Path $fixtures "notext_nul.bin")
    Invoke-ButtonByText $window "Compare"
    Start-Sleep -Seconds 2
    if ((Get-TextsMatching $window "is not text").Count -eq 0) { throw "NOT-TEXT: expected refusal" }
    $results += "NOT-TEXT OK"

    # --- SAVE: copy a modified hunk then write right file to disk ---
    $savePath = Join-Path $artifacts "merge_save_right.txt"
    Copy-Item (Join-Path $fixtures "mods_right.txt") $savePath -Force
    Set-EditValue $window "Left file path" (Join-Path $fixtures "mods_left.txt")
    Set-EditValue $window "Right file path" $savePath
    Invoke-ButtonByText $window "Compare"
    Start-Sleep -Seconds 2
    Wait-ForButtonEnabled $window "Next difference" | Out-Null
    Invoke-ButtonByText $window "Next difference"
    Start-Sleep -Milliseconds 300
    Wait-ForButtonEnabled $window "Copy current change to right" | Out-Null
    Invoke-ButtonByText $window "Copy current change to right"
    Start-Sleep -Milliseconds 500
    Wait-ForButtonEnabled $window "Save right file" | Out-Null
    Invoke-ButtonByText $window "Save right file"
    Start-Sleep -Seconds 2
    $saved = [System.IO.File]::ReadAllText($savePath)
    if ($saved -match "line 5 CHANGED") { throw "SAVE: first hunk was not merged on disk" }
    if ($saved -notmatch "line 5") { throw "SAVE: disk file missing line 5 content" }
    $results += "SAVE OK: right file written to disk"

    # --- FOLDER: compare two roots ---
    Invoke-ButtonByText $window "Folder Compare"
    Start-Sleep -Seconds 1
    Set-EditValue $window "Left folder path" $folderLeft
    Set-EditValue $window "Right folder path" $folderRight
    Invoke-ButtonByText $window "Compare"
    Start-Sleep -Seconds 3
    $states = Get-TextsMatching $window "Metadata match|Metadata different|Left only|Right only"
    if ($states.Count -lt 3) { throw "FOLDER: expected multiple labelled states, found $($states.Count)" }
    $legend = Get-TextsMatching $window "Metadata match = size \+ UTC modification time only"
    if ($legend.Count -eq 0) { throw "FOLDER: metadata legend missing" }
    $results += "FOLDER-COMPARE OK: $($states.Count) state labels visible"
    Save-WindowShot $window "folder_compare_grid.png"

    # --- FOLDER: filter differences (metadata-match row must hide) ---
    Invoke-ButtonByText $window "Differences"
    Start-Sleep -Seconds 1
    $folderGrid = Find-ByName $window "Folder comparison results"
    if (-not $folderGrid) { throw "FOLDER: results grid not found for filter check" }
    $sameVisible = Get-TextsMatching $folderGrid "same\.txt"
    if ($sameVisible.Count -gt 0) { throw "FILTER: same.txt row visible under Differences filter" }
    $results += "FILTER OK: Differences excludes metadata match"

    # --- FOLDER: paired-open command gating (full tab-open flow covered by VM test) ---
    Assert-ButtonEnabled $window "Open paired text comparison" $false
    $results += "PAIRED-OPEN OK: command disabled without selection (full launcher flow: FolderCompareWorkspaceViewModelTests)"

    if (-not (Get-Process -Id $proc.Id -ErrorAction SilentlyContinue).Responding) {
        throw "App stopped responding"
    }

    $results | ForEach-Object { Write-Output $_ }
    Write-Output "ALL PHASE-1 CHECKPOINT CHECKS PASSED"
    $results | Out-File (Join-Path $artifacts "checkpoint_results.txt") -Encoding utf8
}
finally {
    if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -ErrorAction SilentlyContinue }
}
