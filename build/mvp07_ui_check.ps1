# MVP-07 manual-verification helper: drives the real UI through UI Automation.
# Verifies compare of fixtures, same-path rejection, and the not-text diagnostic,
# and captures window screenshots as evidence for the testing report.
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing

$repoRoot = Split-Path -Parent $PSScriptRoot
$fixtures = Join-Path $repoRoot "tests\TestData\CompareFixtures"
$artifacts = Join-Path $repoRoot "artifacts\mvp07"
New-Item -ItemType Directory -Force -Path $artifacts | Out-Null

$exe = Join-Path $repoRoot "src\MergeTool.App.Wpf\bin\Release\net10.0-windows\MergeTool.App.Wpf.exe"
$proc = Start-Process $exe -PassThru
Start-Sleep -Seconds 7

function Find-ByName($parent, $name) {
    $cond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty, $name)
    return $parent.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $cond)
}

function Set-EditValue($window, $name, $value) {
    $edit = Find-ByName $window $name
    if (-not $edit) { throw "Edit '$name' not found" }
    $pattern = $edit.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
    $pattern.SetValue($value)
}

function Invoke-ButtonByText($window, $text) {
    $button = Find-ByName $window $text
    if (-not $button) { throw "Button '$text' not found" }
    $button.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
}

function Get-TextsMatching($window, $regex) {
    $all = $window.FindAll([System.Windows.Automation.TreeScope]::Descendants,
        [System.Windows.Automation.Condition]::TrueCondition)
    $matched = @($all | ForEach-Object { $_.Current.Name } | Where-Object { $_ -match $regex })
    return ,$matched
}

function Save-WindowShot($window, $file) {
    # Best effort: screen capture is unavailable in some shell sessions.
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

try {
    $rootCond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
    $window = [System.Windows.Automation.AutomationElement]::RootElement.FindFirst(
        [System.Windows.Automation.TreeScope]::Children, $rootCond)
    if (-not $window) { throw "Main window not found" }

    # --- Check 1: several separated modifications -> colored blocks + summary ---
    Set-EditValue $window "Left file path" (Join-Path $fixtures "mods_left.txt")
    Set-EditValue $window "Right file path" (Join-Path $fixtures "mods_right.txt")
    Invoke-ButtonByText $window "Compare"
    Start-Sleep -Seconds 3
    $summary = Get-TextsMatching $window "difference block"
    if ($summary.Count -eq 0) { throw "CHECK1 FAIL: no comparison summary shown" }
    Write-Output "CHECK1 OK: $($summary[0])"
    Save-WindowShot $window "check1_modifications.png"

    # --- Check 2: significantly different lengths ---
    Set-EditValue $window "Left file path" (Join-Path $fixtures "length_short.txt")
    Set-EditValue $window "Right file path" (Join-Path $fixtures "length_long.txt")
    Invoke-ButtonByText $window "Compare"
    Start-Sleep -Seconds 3
    $summary2 = Get-TextsMatching $window "difference block"
    if ($summary2.Count -eq 0) { throw "CHECK2 FAIL: no summary for different-length compare" }
    Write-Output "CHECK2 OK: $($summary2[0])"
    Save-WindowShot $window "check2_different_lengths.png"

    # --- Check 3: identical files ---
    Set-EditValue $window "Left file path" (Join-Path $fixtures "identical_a.txt")
    Set-EditValue $window "Right file path" (Join-Path $fixtures "identical_b.txt")
    Invoke-ButtonByText $window "Compare"
    Start-Sleep -Seconds 3
    $identical = Get-TextsMatching $window "No differences"
    if ($identical.Count -eq 0) { throw "CHECK3 FAIL: identical files did not report 'No differences'" }
    Write-Output "CHECK3 OK: $($identical[0])"
    Save-WindowShot $window "check3_identical.png"

    # --- Check 3b: pure insertion on the right ---
    Set-EditValue $window "Left file path" (Join-Path $fixtures "insert_left.txt")
    Set-EditValue $window "Right file path" (Join-Path $fixtures "insert_right.txt")
    Invoke-ButtonByText $window "Compare"
    Start-Sleep -Seconds 3
    $inserted = Get-TextsMatching $window "1 inserted"
    if ($inserted.Count -eq 0) { throw "CHECK3b FAIL: insertion not reported" }
    Write-Output "CHECK3b OK: $($inserted[0])"
    Save-WindowShot $window "check3b_insertion.png"

    # --- Check 4: same normalized path on both sides is rejected ---
    Set-EditValue $window "Left file path" (Join-Path $fixtures "identical_a.txt")
    Set-EditValue $window "Right file path" ((Join-Path $fixtures "identical_a.txt").ToUpperInvariant())
    Invoke-ButtonByText $window "Compare"
    Start-Sleep -Seconds 2
    $samePath = Get-TextsMatching $window "cannot be compared with itself"
    if ($samePath.Count -eq 0) { throw "CHECK4 FAIL: same-path comparison was not rejected" }
    Write-Output "CHECK4 OK: $($samePath[0])"
    Save-WindowShot $window "check4_same_path.png"

    # --- Check 5: NUL-heavy file is refused as not-text, no crash ---
    Set-EditValue $window "Left file path" (Join-Path $fixtures "identical_a.txt")
    Set-EditValue $window "Right file path" (Join-Path $fixtures "notext_nul.bin")
    Invoke-ButtonByText $window "Compare"
    Start-Sleep -Seconds 3
    $notText = Get-TextsMatching $window "is not text"
    if ($notText.Count -eq 0) { throw "CHECK5 FAIL: NUL file was not refused" }
    Write-Output "CHECK5 OK: $($notText[0])"
    Save-WindowShot $window "check5_not_text.png"

    if (-not (Get-Process -Id $proc.Id -ErrorAction SilentlyContinue).Responding) {
        throw "App stopped responding"
    }
    Write-Output "ALL UI CHECKS PASSED"
}
finally {
    if (-not $proc.HasExited) { Stop-Process -Id $proc.Id }
}
