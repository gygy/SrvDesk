# Push a minimal public tree to GitHub: README + LICENSE only.
# Binary is distributed via GitHub Releases (SrvDesk.exe). Full source stays on Gitea main.
param(
    [string]$RepoRoot = (Join-Path $PSScriptRoot "..")
)

$ErrorActionPreference = "Stop"
$Root = (Resolve-Path $RepoRoot).Path
Set-Location $Root

$PublicFiles = @(
    "README.md",
    "README_cn.md",
    "LICENSE"
)

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

foreach ($dir in @("src", "scripts")) {
    if (Test-Path (Join-Path $Root $dir)) {
        Invoke-Git rm -r --cached --ignore-unmatch -- $dir
    }
}
foreach ($extra in @(".gitignore", "CHANGELOG.md", "RELEASE_NOTES.md")) {
    Invoke-Git rm --cached --ignore-unmatch -- $extra
}

$tracked = & $git @GitConfig ls-files
foreach ($path in $tracked) {
    if ($PublicFiles -notcontains $path) {
        Invoke-Git rm -r --cached --ignore-unmatch -- $path
    }
}

foreach ($file in $PublicFiles) {
    $full = Join-Path $Root $file
    if (-not (Test-Path $full)) {
        throw "Missing public file: $file"
    }
    Invoke-Git add -- $file
}

$status = & $git @GitConfig status --porcelain
if ($status) {
    Invoke-Git commit -m "chore: GitHub public tree — README and LICENSE only"
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
Write-Host "Pushed public branch (README + LICENSE) to github/main"

Invoke-Git checkout -f main
Write-Host "Back on main (full source for Gitea)"
