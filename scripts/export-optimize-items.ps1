# Extract all SrvDesk optimization items into dist TXT
$ErrorActionPreference = "Stop"
$repo = Split-Path $PSScriptRoot -Parent
if (-not (Test-Path (Join-Path $repo "src\SrvDesk\MainForm.cs"))) {
    $repo = "G:\gitea\SrvDesk"
}
Set-Location $repo

$main = [IO.File]::ReadAllText((Join-Path $repo "src\SrvDesk\MainForm.cs"), [Text.UTF8Encoding]::new($false))
$svc  = [IO.File]::ReadAllText((Join-Path $repo "src\SrvDesk\ServiceOptimizeCatalog.cs"), [Text.UTF8Encoding]::new($false))
$csproj = [IO.File]::ReadAllText((Join-Path $repo "src\SrvDesk\SrvDesk.csproj"), [Text.UTF8Encoding]::new($false))
$ver = [regex]::Match($csproj, '<Version>\s*([^<]+)\s*</Version>').Groups[1].Value.Trim()

$fieldTitle = @{}
foreach ($m in [regex]::Matches($main, 'private readonly SettingRow (_\w+)\s*=\s*(?:Row|Choice)\(\s*AppLang\.L\(\s*"([^"]+)"')) {
    $fieldTitle[$m.Groups[1].Value] = $m.Groups[2].Value
}

function Get-ParenBlock([string]$text, [int]$openParenIndex) {
    $depth = 0
    for ($k = $openParenIndex; $k -lt $text.Length; $k++) {
        $ch = $text[$k]
        if ($ch -eq '(') { $depth++ }
        elseif ($ch -eq ')') {
            $depth--
            if ($depth -eq 0) { return $text.Substring($openParenIndex, $k - $openParenIndex + 1) }
        }
    }
    return $null
}

$lines = New-Object System.Collections.Generic.List[string]
[void]$lines.Add("SrvDesk 优化项清单")
[void]$lines.Add("版本: $ver")
[void]$lines.Add("生成时间: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')")
[void]$lines.Add("说明: 主界面开关类优化项（按侧栏分组/分区）+ 服务优化目录。不含 DNS/自定义配置/启动项等即时工具页。")
[void]$lines.Add("")

$listed = New-Object 'System.Collections.Generic.HashSet[string]'
$settingCount = 0
$searchFrom = 0
while ($true) {
    $idx = $main.IndexOf('_groups.Add(', $searchFrom)
    if ($idx -lt 0) { break }
    $open = $main.IndexOf('(', $idx + '_groups.Add'.Length)
    $block = Get-ParenBlock $main $open
    if (-not $block) { break }
    $searchFrom = $open + $block.Length

    $tabM = [regex]::Match($block, '^ \(AppLang\.L\("([^"]+)"')
    if (-not $tabM.Success) { $tabM = [regex]::Match($block, 'AppLang\.L\("([^"]+)"') }
    $tab = $tabM.Groups[1].Value

    [void]$lines.Add(("=" * 60))
    [void]$lines.Add("【$tab】")
    [void]$lines.Add(("=" * 60))

    # Leaf sections: (AppLang.L("sec","en"), [ _a, _b, ])
    foreach ($sec in [regex]::Matches($block, '\(AppLang\.L\("([^"]+)",\s*"[^"]*"\)\s*,\s*\[([^\]]*?)\]')) {
        $section = $sec.Groups[1].Value
        $body = $sec.Groups[2].Value
        if ($body -match 'AppLang') { continue }
        $fields = [regex]::Matches($body, '(_\w+)') | ForEach-Object { $_.Groups[1].Value }
        if ($fields.Count -eq 0) { continue }

        [void]$lines.Add("")
        [void]$lines.Add("-- $section --")
        foreach ($f in $fields) {
            [void]$listed.Add($f)
            $settingCount++
            $title = if ($fieldTitle.ContainsKey($f)) { $fieldTitle[$f] } else { $f }
            [void]$lines.Add(("{0,4}. {1}" -f $settingCount, $title))
        }
    }
}

# AllRows orphans
$allRx = [regex]::Match($main, '(?s)private SettingRow\[\] AllRows =>\s*\[(.*?)\];')
$allList = @()
if ($allRx.Success) {
    $allList = [regex]::Matches($allRx.Groups[1].Value, '(_\w+)') | ForEach-Object { $_.Groups[1].Value }
}
$missing = @($allList | Where-Object { -not $listed.Contains($_) } | Select-Object -Unique)
if ($missing.Count -gt 0) {
    [void]$lines.Add("")
    [void]$lines.Add(("=" * 60))
    [void]$lines.Add("【未归入侧栏分组】")
    [void]$lines.Add(("=" * 60))
    foreach ($f in $missing) {
        $settingCount++
        $title = if ($fieldTitle.ContainsKey($f)) { $fieldTitle[$f] } else { $f }
        [void]$lines.Add(("{0,4}. {1}" -f $settingCount, $title))
    }
}

[void]$lines.Add("")
[void]$lines.Add(("=" * 60))
[void]$lines.Add("【服务优化】")
[void]$lines.Add(("=" * 60))

$byCat = [ordered]@{}
foreach ($em in [regex]::Matches($svc, 'E\(\s*"([^"]+)"\s*,\s*"([^"]+)"\s*,\s*"[^"]*"\s*,\s*"([^"]+)"')) {
    $name = $em.Groups[1].Value
    $title = $em.Groups[2].Value
    $cat = $em.Groups[3].Value
    if (-not $byCat.Contains($cat)) { $byCat[$cat] = New-Object System.Collections.Generic.List[string] }
    [void]$byCat[$cat].Add("$title ($name)")
}

$svcCount = 0
foreach ($cat in $byCat.Keys) {
    [void]$lines.Add("")
    [void]$lines.Add("-- $cat --")
    foreach ($item in $byCat[$cat]) {
        $svcCount++
        [void]$lines.Add(("{0,4}. {1}" -f $svcCount, $item))
    }
}

[void]$lines.Add("")
[void]$lines.Add(("=" * 60))
[void]$lines.Add("合计")
[void]$lines.Add(("=" * 60))
[void]$lines.Add("开关类优化项: $settingCount")
[void]$lines.Add("服务优化目录项: $svcCount")
[void]$lines.Add("总计: $($settingCount + $svcCount)")
[void]$lines.Add("")
[void]$lines.Add("字段声明数(SettingRow): $($fieldTitle.Count)")
[void]$lines.Add("AllRows 条目数: $($allList.Count)")

$dist = Join-Path $repo "dist"
New-Item -ItemType Directory -Force -Path $dist | Out-Null
$outPath = Join-Path $dist "SrvDesk-优化项清单.txt"
$utf8bom = New-Object System.Text.UTF8Encoding $true
[IO.File]::WriteAllLines($outPath, $lines, $utf8bom)

Write-Host "OK $outPath"
Write-Host "settings=$settingCount services=$svcCount fields=$($fieldTitle.Count) missing=$($missing.Count)"
