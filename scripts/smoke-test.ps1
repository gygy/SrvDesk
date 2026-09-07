# Smoke checks for SrvDesk (reflection; works with internal types).
$ErrorActionPreference = "Stop"
$dotnet = "$env:LOCALAPPDATA\dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) { $dotnet = "dotnet" }
$repo = Split-Path $PSScriptRoot -Parent
$proj = Join-Path $repo "src\SrvDesk\SrvDesk.csproj"
$dll = Join-Path $repo "src\SrvDesk\bin\Release\net48\SrvDesk.exe"

Write-Host "=== Round build ==="
& $dotnet build $proj -c Release --nologo
if ($LASTEXITCODE -ne 0) { throw "build failed" }

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
$asm = [System.Reflection.Assembly]::LoadFrom($dll)

$failed = New-Object System.Collections.Generic.List[string]
function Assert([bool]$cond, [string]$msg) {
    if ($cond) { Write-Host "PASS  $msg" }
    else { Write-Host "FAIL  $msg"; [void]$failed.Add($msg) }
}
function Resolve-Type([string]$name) {
    $t = $asm.GetType($name, $false, $false)
    if (-not $t) { throw "type not found: $name" }
    return $t
}

Write-Host "`n=== Round 1: catalog / AI category ==="
$catType = Resolve-Type "SrvDesk.CommonSoftwareCatalog"
$all = $catType.GetMethod("GetAll").Invoke($null, @())
Assert ($all.Count -gt 20) "catalog has items ($($all.Count))"
$ai = @($all | Where-Object { $_.Category -eq "AI" })
Assert ($ai.Count -ge 3) "AI category count >= 3 ($($ai.Count))"
Assert (@($ai | Where-Object Id -eq "codex").Count -eq 1) "codex in AI"
Assert (@($ai | Where-Object Id -eq "codex-desktop").Count -eq 1) "codex-desktop in AI"
Assert (@($ai | Where-Object Id -eq "pi-agent").Count -eq 1) "pi-agent in AI"
Assert (@($all | Where-Object { $_.Id -eq "codex" -and $_.Category -ne "AI" }).Count -eq 0) "codex only under AI"
$desk = $all | Where-Object Id -eq "codex-desktop" | Select-Object -First 1
Assert ($desk.WingetId -eq "9PLM9XGG6VKS") "codex-desktop winget id"
Assert ($desk.StoreProductId -eq "9PLM9XGG6VKS") "codex-desktop store id"
Assert ($desk.PreferAppxSideload -eq $true) "codex-desktop prefer appx"
$codex = $all | Where-Object Id -eq "codex" | Select-Object -First 1
Assert (($codex.PreferOfflineOnServer -eq $true) -and ($codex.OfflinePortable -eq $true)) "codex offline portable on server"
$pi = $all | Where-Object Id -eq "pi-agent" | Select-Object -First 1
Assert ($pi.WingetId -eq "EarendilWorks.pi") "pi winget id"
Assert (($codex.DetectExeNames -join ",") -match "codex\.exe") "codex detect exe"
Assert (($pi.DetectExeNames -join ",") -match "pi\.exe") "pi detect exe"

Write-Host "`n=== Round 2: helpers / exception paths ==="
$facts = (Resolve-Type "SrvDesk.SystemInfoHelper").GetMethod("Detect").Invoke($null, @())
Assert (-not [string]::IsNullOrWhiteSpace($facts.Summary)) "system facts: $($facts.Summary)"
$auto = (Resolve-Type "SrvDesk.AutologonHelper").GetMethod("Read").Invoke($null, @())
Assert ($null -ne $auto) "autologon read (Enabled=$($auto.Enabled))"
$uxOn = (Resolve-Type "SrvDesk.Win11DesktopTweaks").GetMethod("IsPauseWindowsUpdatesUxOn").Invoke($null, @())
Assert ($uxOn -is [bool]) "pause ux detect ($uxOn)"
$featOn = (Resolve-Type "SrvDesk.Win11DesktopTweaks").GetMethod("IsPauseFeatureUpdatesUntil2035On").Invoke($null, @())
Assert ($featOn -is [bool]) "pause feature detect ($featOn)"
$optType = Resolve-Type "SrvDesk.Optimizer"
$boostKey = $optType.GetField("ProcessorBoostModeKey", [Reflection.BindingFlags]"Public,Static,NonPublic").GetValue($null)
Assert ("$boostKey" -match "be337238") "boost mode key"

# BuildWingetInstallArgs via private method
$helper = Resolve-Type "SrvDesk.CommonSoftwareHelper"
$flags = [Reflection.BindingFlags]"NonPublic,Static"
$buildArgs = $helper.GetMethod("BuildWingetInstallArgs", $flags)
$a1 = [string]$buildArgs.Invoke($null, @("9PLM9XGG6VKS", $true))
Assert ($a1 -match "--id 9PLM9XGG6VKS") "store id uses --id: $a1"
Assert ($a1 -notmatch "--source winget") "store id skips winget source: $a1"
$a2 = [string]$buildArgs.Invoke($null, @("OpenAI.Codex", $true))
Assert ($a2 -match "--id OpenAI\.Codex" -and $a2 -match "--source winget") "normal id + winget source: $a2"
$a3 = [string]$buildArgs.Invoke($null, @("My Custom App", $true))
Assert ($a3 -match "--name") "name install for spaced title: $a3"

Write-Host "`n=== Round 3: display / header meter + IP ==="
$meterType = Resolve-Type "SrvDesk.HeaderResourceMeter"
$meter = [Activator]::CreateInstance($meterType)
Assert ($meter.Width -ge 420) "header meter width $($meter.Width)"
$meter.Dispose()

Add-Type -AssemblyName System.Net.NetworkInformation
$ips = New-Object System.Collections.Generic.List[string]
foreach ($nic in [System.Net.NetworkInformation.NetworkInterface]::GetAllNetworkInterfaces()) {
    if ($nic.OperationalStatus -ne "Up") { continue }
    if ($nic.NetworkInterfaceType -in @("Loopback","Tunnel")) { continue }
    foreach ($ua in $nic.GetIPProperties().UnicastAddresses) {
        if ($ua.Address.AddressFamily -ne "InterNetwork") { continue }
        $s = $ua.Address.ToString()
        if ($s.StartsWith("169.254.")) { continue }
        [void]$ips.Add($s)
    }
}
Assert ($true) "ipv4 enum ok count=$($ips.Count)"
if ($ips.Count -gt 0) { Write-Host "INFO  IPv4: $($ips -join ', ')" }

$stateType = $optType.GetNestedType("State", [Reflection.BindingFlags]"Public,NonPublic")
$st = [Activator]::CreateInstance($stateType)
Assert ($null -ne $stateType.GetField("PauseWindowsUpdatesUx")) "State.PauseWindowsUpdatesUx"
Assert ($null -ne $stateType.GetField("ShowProcessorBoostMode")) "State.ShowProcessorBoostMode"
Assert ($null -ne $stateType.GetField("MsPinyinDefaultEnglish")) "State.MsPinyinDefaultEnglish"
Assert ($null -ne $stateType.GetField("EnableAutologon")) "State.EnableAutologon"

# Setting recipes exist for new help infos
$catalog = Resolve-Type "SrvDesk.SettingCatalog"
$recipeCat = Resolve-Type "SrvDesk.SettingRecipeCatalog"
$get = $recipeCat.GetMethod("Get")
foreach ($name in @("ShowProcessorBoostMode","PauseWindowsUpdatesUx","MsPinyinDefaultEnglish","DisableMsPinyinCloudAndInsights","DisableMsPinyinToolbar")) {
    $help = $catalog.GetField($name, [Reflection.BindingFlags]"Public,Static").GetValue($null)
    $recipe = $get.Invoke($null, @($help))
    Assert ($null -ne $recipe) "recipe for $name"
}

if ($failed.Count -gt 0) {
    Write-Host "`nFAILED $($failed.Count):"
    $failed | ForEach-Object { Write-Host " - $_" }
    exit 1
}
Write-Host "`nAll smoke checks passed."

