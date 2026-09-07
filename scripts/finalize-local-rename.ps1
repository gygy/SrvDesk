<#
.SYNOPSIS
  在 Cursor/终端不再占用「win一键优化」目录后，把物理文件夹改名为 SrvDesk，并去掉临时 junction。
#>
$ErrorActionPreference = "Stop"
$gitRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
if (-not (Test-Path (Join-Path $gitRoot "_common"))) {
    $gitRoot = "G:\gitea"
}
$old = Join-Path $gitRoot "win一键优化"
$junction = Join-Path $gitRoot "SrvDesk"

Write-Host "gitRoot=$gitRoot"
if (-not (Test-Path -LiteralPath $old)) {
    Write-Host "旧目录已不存在，无需处理。"
    exit 0
}

$item = Get-Item -LiteralPath $junction -Force -ErrorAction SilentlyContinue
if ($item -and ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
    Write-Host "删除临时 junction: $junction"
    cmd /c rmdir "$junction"
}

Write-Host "重命名: win一键优化 -> SrvDesk"
Rename-Item -LiteralPath $old -NewName "SrvDesk"

$cfg = Join-Path $gitRoot "_common\gitea-auto-sync.json"
if (Test-Path $cfg) {
    $utf8 = New-Object System.Text.UTF8Encoding $false
    $raw = [IO.File]::ReadAllText($cfg, [Text.Encoding]::UTF8)
    # 去掉 win一键优化 禁用块（简单整段替换）
    $pattern = '(?ms),\s*"win一键优化"\s*:\s*\{[^}]*\}'
    $raw2 = [regex]::Replace($raw, $pattern, '')
    if ($raw2 -ne $raw) {
        [IO.File]::WriteAllText($cfg, $raw2, $utf8)
        Write-Host "已从 gitea-auto-sync.json 移除 win一键优化 条目"
    }
}

Write-Host "完成。请用 Cursor 打开: $(Join-Path $gitRoot 'SrvDesk')"
