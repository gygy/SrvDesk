# 查看本机密码策略：哪一项还在拦「添加用户」
# 用法：右键 PowerShell「以管理员运行」后执行：
#   powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\check-password-policy.ps1

$ErrorActionPreference = "Continue"
Write-Host "=== net accounts ===" -ForegroundColor Cyan
net accounts
Write-Host ""

$cfg = Join-Path $env:TEMP ("SrvDesk-pwcheck-{0}.inf" -f [guid]::NewGuid().ToString("N"))
secedit /export /cfg $cfg /areas SECURITYPOLICY | Out-Null
if (-not (Test-Path $cfg)) {
    Write-Host "secedit 导出失败（请用管理员运行）。" -ForegroundColor Red
    exit 1
}

Write-Host "=== secedit [System Access] ===" -ForegroundColor Cyan
Get-Content $cfg -Encoding Unicode |
    Select-String -Pattern "PasswordComplexity|PasswordHistorySize|MinimumPasswordLength|MaximumPasswordAge|MinimumPasswordAge" |
    ForEach-Object { $_.Line.Trim() }

$inf = Get-Content $cfg -Encoding Unicode -Raw
function Get-InfVal([string]$key) {
    if ($inf -match "(?m)^$([regex]::Escape($key))\s*=\s*(.+)$") { return $Matches[1].Trim() }
    return $null
}

$comp = Get-InfVal "PasswordComplexity"
$hist = Get-InfVal "PasswordHistorySize"
$minL = Get-InfVal "MinimumPasswordLength"
$maxA = Get-InfVal "MaximumPasswordAge"

Write-Host ""
Write-Host "=== 哪条还在拦 ===" -ForegroundColor Cyan
$blockers = @()

if ($comp -ne "0") {
    $blockers += "密码复杂性仍开启 (PasswordComplexity=$comp) → 需大小写/数字/符号等"
    Write-Host "[不满足] 密码复杂性仍开启" -ForegroundColor Yellow
} else {
    Write-Host "[OK] 密码复杂性已关闭" -ForegroundColor Green
}

if ($null -ne $hist -and [int]$hist -gt 0) {
    $blockers += "强制密码历史仍开启 (PasswordHistorySize=$hist) → 不能与最近 $hist 次相同"
    Write-Host "[不满足] 强制密码历史 = $hist 次" -ForegroundColor Yellow
} else {
    Write-Host "[OK] 强制密码历史已关闭" -ForegroundColor Green
}

$minN = 0
[void][int]::TryParse($minL, [ref]$minN)
if ($minN -gt 0) {
    $blockers += "最小密码长度 = $minN → 密码至少 $minN 位"
    Write-Host "[不满足] 最小密码长度 = $minN" -ForegroundColor Yellow
} else {
    Write-Host "[OK] 最小密码长度无限制 (0)" -ForegroundColor Green
}

Write-Host ("最长使用期限 MaximumPasswordAge = {0}" -f $(if ($maxA) { $maxA } else { "?" }))

Write-Host ""
if ($blockers.Count -eq 0) {
    Write-Host "策略侧已放开。若添加用户仍失败：用户可能已存在，或密码含非法字符；也可用更长密码再试。" -ForegroundColor Green
} else {
    Write-Host "仍不满足：" -ForegroundColor Red
    $blockers | ForEach-Object { Write-Host ("  - " + $_) }
    Write-Host "处理：SrvDesk → 账户策略 → 关闭对应项 → 应用到系统 → 再添加用户。"
}

Remove-Item $cfg -Force -ErrorAction SilentlyContinue
