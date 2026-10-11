# Self-test:「我的方案」UserPlanStore / UserPlanState（不写系统优化）
$ErrorActionPreference = "Stop"
$dotnet = "$env:LOCALAPPDATA\dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) { $dotnet = "dotnet" }
$repo = Split-Path $PSScriptRoot -Parent
$proj = Join-Path $repo "src\SrvDesk\SrvDesk.csproj"
$dll = Join-Path $repo "src\SrvDesk\bin\Release\net48\SrvDesk.exe"

Write-Host "=== Build ==="
& $dotnet build $proj -c Release --nologo
if ($LASTEXITCODE -ne 0) { throw "build failed" }

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
$asm = [System.Reflection.Assembly]::LoadFrom($dll)

$failed = New-Object System.Collections.Generic.List[string]
function FailMsg([string]$msg) {
    Write-Host "FAIL  $msg"
    [void]$failed.Add($msg)
}
function Assert([bool]$cond, [string]$msg) {
    if ($cond) { Write-Host "PASS  $msg" }
    else { FailMsg $msg }
}
function Resolve-Type([string]$name) {
    $t = $asm.GetType($name, $false, $false)
    if (-not $t) { throw "type not found: $name" }
    return $t
}

$allStat = [Reflection.BindingFlags]"Public,NonPublic,Static"
$allInst = [Reflection.BindingFlags]"Public,NonPublic,Instance"
$pubInst = [Reflection.BindingFlags]"Public,Instance"

$store = Resolve-Type "SrvDesk.UserPlanStore"
$stateHelper = Resolve-Type "SrvDesk.UserPlanState"
$detailType = Resolve-Type "SrvDesk.UserPlanDetail"
$itemType = Resolve-Type "SrvDesk.UserPlanItem"
$optType = Resolve-Type "SrvDesk.Optimizer"
$stateType = $optType.GetNestedType("State", [Reflection.BindingFlags]"Public,NonPublic")
$mainType = Resolve-Type "SrvDesk.MainForm"
$dialogType = Resolve-Type "SrvDesk.UserPlanDialog"

Assert ($null -ne $store) "UserPlanStore type"
Assert ($null -ne $dialogType) "UserPlanDialog type"
Assert ($null -ne $mainType.GetMethod("ApplyUserPlanItems", $allInst)) "MainForm.ApplyUserPlanItems"
Assert ($null -ne $mainType.GetMethod("RefreshFavoriteStars", $allInst)) "MainForm.RefreshFavoriteStars"

$testRoot = Join-Path $env:TEMP ("SrvDesk-UserPlan-Test-" + [Guid]::NewGuid().ToString("N"))
$origRoot = $store.GetProperty("RootDir", $allStat).GetValue($null)
# RootDir is computed from AppData — isolate by redirecting LocalAppData via env is hard.
# Instead exercise APIs against real RootDir but unique plan names, then delete.

function New-PlanItem([string]$key, [int]$value, [int]$optIdx = 1) {
    $it = [Activator]::CreateInstance($itemType)
    $it.Key = $key
    $it.Value = $value
    $it.OptimizedIndex = $optIdx
    $it.Title = "Test-$key"
    $it.Module = "UnitTest"
    $it.DesiredLabel = "On"
    $it.Enabled = $true
    return $it
}

Write-Host "`n=== Create / upsert / contains ==="
$planName = "自测方案-" + [Guid]::NewGuid().ToString("N").Substring(0, 8)
$create = $store.GetMethod("CreatePlan", $allStat)
$plan = $create.Invoke($null, @($planName))
Assert ($null -ne $plan) "CreatePlan"
Assert (-not [string]::IsNullOrWhiteSpace($plan.Id)) "plan Id"
Assert ($plan.Name -eq $planName) "plan Name"

$item = New-PlanItem "DisableTelemetry" 1 1
$upsert = $store.GetMethod("UpsertItem", $allStat)
$added = [bool]$upsert.Invoke($null, @($plan, $item))
Assert ($added) "UpsertItem new"

$plan2 = $store.GetMethod("LoadPlan", $allStat).Invoke($null, @($plan.Id))
Assert ($plan2.Items.Count -eq 1) "LoadPlan item count=1"
Assert ($plan2.Items[0].Key -eq "DisableTelemetry") "item key"

$contains = [bool]$store.GetMethod("ContainsKey", $allStat).Invoke($null, @($plan.Id, "DisableTelemetry"))
Assert ($contains) "ContainsKey"

$item2 = New-PlanItem "DisableTelemetry" 0 1
$item2.DesiredLabel = "Off"
$updated = -not [bool]$upsert.Invoke($null, @($plan2, $item2))
# Upsert returns false when updating existing
$plan3 = $store.GetMethod("LoadPlan", $allStat).Invoke($null, @($plan.Id))
Assert ($plan3.Items.Count -eq 1) "still one item after update"
Assert ($plan3.Items[0].Value -eq 0) "value updated to 0"

Write-Host "`n=== Multi-item / move / enable ==="
$i2 = New-PlanItem "DisableSysMain" 1 1
[void]$upsert.Invoke($null, @($plan3, $i2))
$plan4 = $store.GetMethod("LoadPlan", $allStat).Invoke($null, @($plan.Id))
Assert ($plan4.Items.Count -eq 2) "two items"

$store.GetMethod("MoveItem", $allStat).Invoke($null, @($plan4, "DisableSysMain", -1))
$plan5 = $store.GetMethod("LoadPlan", $allStat).Invoke($null, @($plan.Id))
Assert ($plan5.Items[0].Key -eq "DisableSysMain") "MoveItem up"

$store.GetMethod("SetItemEnabled", $allStat).Invoke($null, @($plan5, "DisableTelemetry", $false))
$plan6 = $store.GetMethod("LoadPlan", $allStat).Invoke($null, @($plan.Id))
$telem = @($plan6.Items | Where-Object { $_.Key -eq "DisableTelemetry" })[0]
Assert ($telem.Enabled -eq $false) "SetItemEnabled false"

Write-Host "`n=== UserPlanState read/write/clone/drift ==="
$st = [Activator]::CreateInstance($stateType)
$st.DisableTelemetry = $true
$st.DisableSysMain = $false
$st.UacNotifyLevel = 1
$st.DisableUac = $false
$st.HighPerfPower = $true
$st.UltimatePerfPower = $false
$st.TaskbarSearchMode = 0

$write = $stateHelper.GetMethod("WriteChoiceIndex", $allStat)
$read = $stateHelper.GetMethod("ReadChoiceIndex", $allStat)
$write.Invoke($null, @($st, "DisableUac", 2, 2))
Assert ($st.UacNotifyLevel -eq 2 -and $st.DisableUac -eq $true) "WriteChoiceIndex UAC never notify"
$uacIdx = [int]$read.Invoke($null, @($st, "DisableUac", 2))
Assert ($uacIdx -eq 2) "ReadChoiceIndex UAC=2"

$write.Invoke($null, @($st, "HighPerfPower", 2, 1))
Assert ($st.UltimatePerfPower -eq $true) "WriteChoiceIndex Ultimate power"
$write.Invoke($null, @($st, "HideTaskbarSearch", 1, 0))
Assert ($st.TaskbarSearchMode -eq 1) "WriteChoiceIndex taskbar search"

$clone = $stateHelper.GetMethod("Clone", $allStat).Invoke($null, @($st))
Assert ($clone.DisableUac -eq $true) "Clone copies fields"
$clone.DisableUac = $false
Assert ($st.DisableUac -eq $true) "Clone is independent"

$applyItems = $stateHelper.GetMethod("ApplyItems", $allStat)
$enabledItem = New-PlanItem "DisableTelemetry" 1 1
$disabledItem = New-PlanItem "DisableSysMain" 1 1
$disabledItem.Enabled = $false
$arr = [Array]::CreateInstance($itemType, 2)
$arr.SetValue($enabledItem, 0)
$arr.SetValue($disabledItem, 1)
$target = $stateHelper.GetMethod("Clone", $allStat).Invoke($null, @($st))
$target.DisableTelemetry = $false
$target.DisableSysMain = $false
$applyItems.Invoke($null, @($target, $arr, $false))
Assert ($target.DisableTelemetry -eq $true) "ApplyItems writes enabled"
Assert ($target.DisableSysMain -eq $false) "ApplyItems skips disabled"

$match = $stateHelper.GetMethod("Matches", $allStat)
$m1 = [bool]$match.Invoke($null, @($target, $enabledItem))
Assert ($m1) "Matches after apply"
$driftItem = New-PlanItem "DisableTelemetry" 1 1
$driftState = $stateHelper.GetMethod("Clone", $allStat).Invoke($null, @($target))
$driftState.DisableTelemetry = $false
$m2 = [bool]$match.Invoke($null, @($driftState, $driftItem))
Assert (-not $m2) "Matches detects drift"

$countDrift = $store.GetMethod("CountDrift", $allStat)
# reload plan with both items enabled for drift count
$planDrift = $store.GetMethod("LoadPlan", $allStat).Invoke($null, @($plan.Id))
foreach ($it in $planDrift.Items) { $it.Enabled = $true }
$store.GetMethod("SavePlan", $allStat).Invoke($null, @($planDrift))
$sys = [Activator]::CreateInstance($stateType)
$sys.DisableTelemetry = $false
$sys.DisableSysMain = $false
$nDrift = [int]$countDrift.Invoke($null, @($planDrift, $sys))
Assert ($nDrift -ge 1) "CountDrift >= 1 ($nDrift)"

Write-Host "`n=== Export / import ==="
$exportPath = Join-Path $env:TEMP ("SrvDesk-plan-export-" + [Guid]::NewGuid().ToString("N") + ".json")
$planId = [string]$plan.Id
$exportM = $store.GetMethod("ExportToFile", $allStat, $null, @([string], [string]), $null)
if ($null -eq $exportM) {
    $exportM = @($store.GetMethods($allStat) | Where-Object { $_.Name -eq "ExportToFile" })[0]
}
$exportM.Invoke($null, @([string]$exportPath, [string]$planId))
Assert (Test-Path $exportPath) "ExportToFile exists"
$raw = Get-Content -LiteralPath $exportPath -Raw -Encoding UTF8
Assert ($raw -match "DisableSysMain") "export contains key"

$importM = $store.GetMethod("ImportFromFile", $allStat, $null, @([string], [bool]), $null)
$imported = [int]$importM.Invoke($null, @([string]$exportPath, $true))
Assert ($imported -ge 1) "ImportFromFile merge count=$imported"

Write-Host "`n=== Dialog / MainForm smoke (STA) ==="
function Invoke-Sta([scriptblock]$body) {
    if ([Threading.Thread]::CurrentThread.GetApartmentState() -eq [Threading.ApartmentState]::STA) {
        return & $body
    }
    $state = @{ Result = $null; Error = $null; Body = $body }
    $t = New-Object Threading.Thread ([Threading.ParameterizedThreadStart]{
        param($s)
        [void][System.Windows.Forms.Application]::EnableVisualStyles()
        try { $s.Result = & $s.Body } catch { $s.Error = $_ }
    })
    $t.SetApartmentState([Threading.ApartmentState]::STA)
    $t.IsBackground = $true
    $t.Start($state)
    $t.Join()
    if ($state.Error) { throw $state.Error }
    return $state.Result
}

$ifaceType = Resolve-Type "SrvDesk.IEmbeddedSettingsPage"
try {
    Invoke-Sta {
        $main = [Activator]::CreateInstance($mainType)
        $dlg = [Activator]::CreateInstance($dialogType, @($main))
        Assert ($dlg -is [System.Windows.Forms.Form]) "UserPlanDialog is Form"
        Assert ($ifaceType.IsInstanceOfType($dlg)) "implements IEmbeddedSettingsPage"
        $supp = $ifaceType.GetProperty("SupportsApplyToSystem").GetValue($dlg)
        Assert ($supp -eq $true) "SupportsApplyToSystem"
        $dlg.Dispose()
        $main.Dispose()
    }
} catch {
    FailMsg "STA dialog: $($_.Exception.Message)"
}

Write-Host "`n=== Cleanup test plans ==="
$index = $store.GetMethod("LoadIndex", $allStat).Invoke($null, @())
$toDelete = @($index.Plans | Where-Object { $_.Name -like "自测方案-*" -or $_.Name -like "导入的方案*" -or $_.Name -eq $planName })
# Also delete plans created by import of our export (may have same items)
foreach ($s in @($index.Plans)) {
    if ($s.Id -eq $plan.Id -or $s.Name -eq $planName -or $s.Name -like "自测方案-*") {
        $store.GetMethod("DeletePlan", $allStat).Invoke($null, @($s.Id))
        Write-Host "INFO  deleted plan $($s.Name)"
    }
}
# Delete imported copies that contain only our test keys and UnitTest module
$index2 = $store.GetMethod("LoadIndex", $allStat).Invoke($null, @())
foreach ($s in @($index2.Plans)) {
    $p = $store.GetMethod("LoadPlan", $allStat).Invoke($null, @($s.Id))
    if ($null -eq $p) { continue }
    $allTest = ($p.Items.Count -gt 0) -and (@($p.Items | Where-Object { $_.Module -ne "UnitTest" }).Count -eq 0)
    if ($allTest) {
        $store.GetMethod("DeletePlan", $allStat).Invoke($null, @($s.Id))
        Write-Host "INFO  deleted imported test plan $($s.Name)"
    }
}

Remove-Item -LiteralPath $exportPath -Force -ErrorAction SilentlyContinue

# Dist delivery check
$distExe = Join-Path $repo "dist\SrvDesk.exe"
$distSetup = Join-Path $repo "dist\SrvDesk-Setup.exe"
Assert (Test-Path $distExe) "dist\SrvDesk.exe"
Assert (Test-Path $distSetup) "dist\SrvDesk-Setup.exe"
$vi = [Diagnostics.FileVersionInfo]::GetVersionInfo($distExe)
Assert ($vi.FileVersion -match "^1\.0\.173") "dist file version $($vi.FileVersion)"

if ($failed.Count -gt 0) {
    Write-Host "`nFAILED $($failed.Count):"
    $failed | ForEach-Object { Write-Host " - $_" }
    exit 1
}
Write-Host "`nAll user-plan checks passed."
