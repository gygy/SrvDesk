# Push a minimal public tree to GitHub: README / LICENSE only (no source, no docs/).
# Binary is distributed via GitHub Releases (SrvDesk.exe). Full source stays on Gitea main.
param(
    [string]$RepoRoot = (Join-Path $PSScriptRoot "..")
)

$ErrorActionPreference = "Stop"
$Root = (Resolve-Path $RepoRoot).Path
Set-Location $Root

$PublicFiles = @(
    "README.md",
    "README_en.md",
    "README_cn.md",
    "LICENSE",
    "DISCLAIMER.md",
    "PRIVACY.md"
)

# 这些目录永不进 GitHub（docs/ 为内部资料，仅留 Gitea）
$BlockedDirs = @("src", "scripts", "docs", ".cursor", "tools", "artifacts", "dist")

function Get-GitExe {
    foreach ($c in @("git", "$env:ProgramFiles\Git\cmd\git.exe", "$env:LOCALAPPDATA\Programs\Git\cmd\git.exe")) {
        if ($c -eq "git") {
            $cmd = Get-Command git -ErrorAction SilentlyContinue
            if ($cmd) { return $cmd.Source }
        } elseif (Test-Path $c) { return $c }
    }
    throw "git not found"
}

$git = Get-GitExe
$safeDir = (Resolve-Path $Root).Path
$GitConfig = @("-c", "safe.directory=$safeDir")
$GithubRemoteUrl = "https://github.com/gygy/SrvDesk.git"

function Invoke-Git {
    param([Parameter(ValueFromRemainingArguments = $true)][string[]]$GitArgs)
    & $git @GitConfig @GitArgs
    if ($LASTEXITCODE -ne 0) { throw "git failed: $($GitArgs -join ' ')" }
}

$current = (& $git @GitConfig branch --show-current).Trim()
if ($current -ne "main") {
    Invoke-Git checkout main
}

Invoke-Git checkout -B public main

foreach ($dir in $BlockedDirs) {
    # 目录级移除，避免中文文件名在逐文件 rm 时被 PowerShell 弄坏
    & $git @GitConfig rm -r --cached --ignore-unmatch -- $dir 2>$null
}

foreach ($extra in @(".gitignore", "CHANGELOG.md", "RELEASE_NOTES.md")) {
    Invoke-Git rm --cached --ignore-unmatch -- $extra
}

$trackedList = @(& $git @GitConfig -c core.quotepath=false ls-files)
foreach ($path in $trackedList) {
    if ([string]::IsNullOrWhiteSpace($path)) { continue }
    if ($PublicFiles -contains $path) { continue }
    & $git @GitConfig -c core.quotepath=false rm -r --cached --ignore-unmatch -- $path
}

foreach ($file in $PublicFiles) {
    $full = Join-Path $Root $file
    if (-not (Test-Path -LiteralPath $full)) {
        throw "Missing public file: $file"
    }
    Invoke-Git add -- $file
}

# 确认索引里没有 docs/
$leak = @(& $git @GitConfig -c core.quotepath=false ls-files) |
    Where-Object { $_ -like "docs/*" -or $_ -eq "docs" -or $_ -like "src/*" -or $_ -like "scripts/*" }
if ($leak.Count -gt 0) {
    throw ("Public branch still contains blocked paths:`n" + ($leak -join "`n"))
}

$status = & $git @GitConfig status --porcelain
if ($status) {
    Invoke-Git commit -m "chore: GitHub public files only (no src/, no docs/)"
}

$prevEa = $ErrorActionPreference
$ErrorActionPreference = 'SilentlyContinue'
$githubUrl = & $git @GitConfig remote get-url github 2>$null
$ErrorActionPreference = $prevEa
if (-not $githubUrl) {
    Invoke-Git remote add github $GithubRemoteUrl
} else {
    Invoke-Git remote set-url github $GithubRemoteUrl
}

Invoke-Git push --force github public:main
Write-Host "Pushed public files to github/main (no source, no docs/)"

Invoke-Git checkout -f main
Write-Host "Back on main (full source for Gitea)"
