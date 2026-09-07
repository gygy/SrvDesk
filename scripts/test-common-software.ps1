# Functional checks for 常用软件 (no silent install of random apps).
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
$asm = [Reflection.Assembly]::LoadFrom($dll)
$failed = New-Object System.Collections.Generic.List[string]
$passed = 0
function Pass([string]$id, [string]$msg) { $script:passed++; Write-Host "PASS  $id  $msg" }
function Fail([string]$id, [string]$msg) { [void]$script:failed.Add("$id  $msg"); Write-Host "FAIL  $id  $msg" }
function Assert([string]$id, [bool]$ok, [string]$msg) { if ($ok) { Pass $id $msg } else { Fail $id $msg } }
function T([string]$name) { $t = $asm.GetType($name, $false, $false); if (-not $t) { throw "missing $name" }; $t }

$cat = T "SrvDesk.CommonSoftwareCatalog"
$helper = T "SrvDesk.CommonSoftwareHelper"
$resolver = T "SrvDesk.OfficialInstallerResolver"
$items = @($cat.GetMethod("GetAll").Invoke($null, @()))
Write-Host "catalog count=$($items.Count)"

$ids = @($items | ForEach-Object { $_.Id })
$dup = $ids | Group-Object | Where-Object { $_.Count -gt 1 }
Assert "CS-01" ($items.Count -ge 40 -and -not $dup) "count=$($items.Count) dups=$($dup.Name -join ',')"

$emptyTitle = @($items | Where-Object { [string]::IsNullOrWhiteSpace($_.Title) })
Assert "CS-02" ($emptyTitle.Count -eq 0) "empty titles"

$noPath = @()
foreach ($it in $items) {
    $hasWinget = -not [string]::IsNullOrWhiteSpace($it.WingetId)
    $hasOff = -not [string]::IsNullOrWhiteSpace($it.OfflineInstallerUrl)
    $hasPage = -not [string]::IsNullOrWhiteSpace($it.DownloadUrl)
    $hasStore = -not [string]::IsNullOrWhiteSpace($it.StoreProductId)
    $hasGh = -not [string]::IsNullOrWhiteSpace($it.GitHubRepo)
    $hasApi = -not [string]::IsNullOrWhiteSpace($it.LatestApiUrl)
    if (-not ($hasWinget -or $hasOff -or $hasPage -or $hasStore -or $hasGh -or $hasApi -or $it.IsWingetBootstrap)) {
        $noPath += $it.Id
    }
}
Assert "CS-03" ($noPath.Count -eq 0) "no install path: $($noPath -join ',')"

$noWinget = @($items | Where-Object { [string]::IsNullOrWhiteSpace($_.WingetId) -and -not $_.IsWingetBootstrap })
Write-Host "INFO  no-winget: $((@($noWinget | ForEach-Object Id) -join ', '))"
Assert "CS-04" ($noWinget.Count -ge 1) "hikconnect/tianyiyun remain without winget"

$query = $helper.GetMethod("Query")
$qFail = @()
$installed = @()
foreach ($it in $items) {
    try {
        $st = $query.Invoke($null, @($it))
        if ($st.Installed) { $installed += "$($it.Id)=$($st.Version)" }
    } catch {
        $qFail += "$($it.Id): $($_.Exception.Message)"
    }
}
Assert "CS-05" ($qFail.Count -eq 0) "query throws: $($qFail -join ' | ')"
Write-Host "INFO  installed: $($installed -join '; ')"

$find = $cat.GetMethod("Find")
$pi = $find.Invoke($null, @([string]"pi-agent"))
Assert "CS-06" (@($pi.DetectPatterns) -notcontains "pi") "pi-agent patterns=$($pi.DetectPatterns -join ',')"

$isDirect = $resolver.GetMethod("IsDirectInstallerUrl")
$isFresh = $resolver.GetMethod("IsFreshLatestUrl")
$tryRes = $resolver.GetMethod("TryResolve")
$chrome = $find.Invoke($null, @([string]"chrome"))
Assert "CS-07" ([bool]$isDirect.Invoke($null, @([string]$chrome.OfflineInstallerUrl))) "chrome msi is direct"
Assert "CS-08" ([bool]$isFresh.Invoke($null, @([string]$chrome.OfflineInstallerUrl))) "chrome url treated as latest"

$hik = $find.Invoke($null, @([string]"hikconnect"))
$tianyi = $find.Invoke($null, @([string]"tianyiyun"))
Assert "CS-09" ($null -ne $hik -and $null -ne $tianyi -and [string]::IsNullOrWhiteSpace($hik.WingetId) -and [string]::IsNullOrWhiteSpace($tianyi.WingetId)) "no-winget items exist"

function Try-ResolveId([string]$id) {
    $item = $find.Invoke($null, @([string]$id))
    try {
        $url = [string]$tryRes.Invoke($null, @($item))
        if ([string]::IsNullOrWhiteSpace($url)) { Fail "CS-R-$id" "empty resolve"; return }
        $ok = [bool]$isDirect.Invoke($null, @($url)) -or ($url -match '\.(exe|msi)(\?|$)') -or ($url -match '/latest/') -or ($url -match 'downloadFile\.action')
        if ($id -eq "7zip") {
            $ok = $ok -and ($url -match '7-zip\.org')
        }
        Assert "CS-R-$id" $ok "resolved=$url"
    } catch {
        Fail "CS-R-$id" $_.Exception.Message
    }
}

Write-Host "`n=== Resolve official latest (network) ==="
Try-ResolveId "git"
Try-ResolveId "7zip"
Try-ResolveId "wechat"
Try-ResolveId "chrome"
Try-ResolveId "hikconnect"
Try-ResolveId "tianyiyun"

$dlgType = T "SrvDesk.CommonSoftwareDialog"
try {
    $d = [Activator]::CreateInstance($dlgType)
    Assert "CS-10" ($d.Text -match "常用软件") "dialog title=$($d.Text)"
    $d.Dispose()
} catch {
    Fail "CS-10" $_.Exception.Message
}

Write-Host ""
Write-Host ("Passed {0}  Failed {1}" -f $passed, $failed.Count)
if ($failed.Count -gt 0) {
    $failed | ForEach-Object { Write-Host " - $_" }
    exit 1
}
Write-Host "Common software checks passed."
exit 0
