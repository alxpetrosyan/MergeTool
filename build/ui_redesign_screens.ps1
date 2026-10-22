# UI-REDESIGN-01 evidence: nested folder tree + comparison settings dialog screenshots.
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing

$repoRoot = Split-Path -Parent $PSScriptRoot
$artifacts = Join-Path $repoRoot "artifacts\ui-redesign"
New-Item -ItemType Directory -Force -Path $artifacts | Out-Null

# Nested fixture trees
$left = Join-Path $env:TEMP "mt_uird_left"
$right = Join-Path $env:TEMP "mt_uird_right"
foreach ($root in @($left, $right)) {
    if (Test-Path $root) { Remove-Item $root -Recurse -Force }
    New-Item -ItemType Directory -Path (Join-Path $root "src\app") -Force | Out-Null
    New-Item -ItemType Directory -Path (Join-Path $root "docs") -Force | Out-Null
}
Set-Content (Join-Path $left "src\app\main.cs") "class A { int x = 1; }"
Set-Content (Join-Path $right "src\app\main.cs") "class A { int x = 2; } // changed"
Set-Content (Join-Path $left "src\app\left-only.cs") "left only"
Set-Content (Join-Path $right "src\app\right-only.cs") "right only"
Set-Content (Join-Path $left "docs\readme.md") "same text"
Set-Content (Join-Path $right "docs\readme.md") "same text"
$stamp = Get-Date
(Get-Item (Join-Path $left "docs\readme.md")).LastWriteTimeUtc = $stamp
(Get-Item (Join-Path $right "docs\readme.md")).LastWriteTimeUtc = $stamp

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
    $edit.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue($value)
}
function Invoke-ButtonByText($window, $text) {
    $button = Find-ByName $window $text
    if (-not $button) { throw "Button '$text' not found" }
    $button.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
}
function Save-Shot($element, $file) {
    $rect = $element.Current.BoundingRectangle
    $bmp = New-Object System.Drawing.Bitmap([int]$rect.Width, [int]$rect.Height)
    $gfx = [System.Drawing.Graphics]::FromImage($bmp)
    $gfx.CopyFromScreen([int]$rect.X, [int]$rect.Y, 0, 0, $bmp.Size)
    $bmp.Save((Join-Path $artifacts $file), [System.Drawing.Imaging.ImageFormat]::Png)
    $gfx.Dispose(); $bmp.Dispose()
}

try {
    $rootCond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
    $window = [System.Windows.Automation.AutomationElement]::RootElement.FindFirst(
        [System.Windows.Automation.TreeScope]::Children, $rootCond)
    if (-not $window) { throw "Main window not found" }

    # Nested folder comparison tree
    Invoke-ButtonByText $window "Folder Compare"
    Start-Sleep -Seconds 1
    Set-EditValue $window "Left folder path" $left
    Set-EditValue $window "Right folder path" $right
    Invoke-ButtonByText $window "Compare"
    Start-Sleep -Seconds 3
    Save-Shot $window "folder_tree_nested.png"

    # Settings dialog on a file compare tab (modal: invoke from a background job-safe pattern)
    Invoke-ButtonByText $window "File Compare"
    Start-Sleep -Seconds 1
    $optionsButton = Find-ByName $window "Comparison settings"
    $optionsButton.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    Start-Sleep -Seconds 2
    $procCond = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ProcessIdProperty, $proc.Id)
    $windows = [System.Windows.Automation.AutomationElement]::RootElement.FindAll(
        [System.Windows.Automation.TreeScope]::Children, $procCond)
    $dialog = $windows | Where-Object { $_.Current.Name -match "Comparison settings" } | Select-Object -First 1
    if ($dialog) { Save-Shot $dialog "settings_dialog.png" } else { Save-Shot $window "settings_dialog.png" }
    Write-Output "Screenshots written to $artifacts"
}
finally {
    if (-not $proc.HasExited) { Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue }
    Remove-Item $left, $right -Recurse -Force -ErrorAction SilentlyContinue
}
