# Push a public tree (no src/) to GitHub. Full source stays on Gitea main.
param(
    [string]$RepoRoot = (Join-Path $PSScriptRoot "..")
)

$ErrorActionPreference = "Stop"
$Root = (Resolve-Path $RepoRoot).Path
Set-Location $Root

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

$gitignorePath = Join-Path $Root ".gitignore"
$gitignore = Get-Content $gitignorePath -Raw
if ($gitignore -notmatch '(?m)^src/$') {
    Add-Content -Path $gitignorePath -Value "src/"
    Invoke-Git add .gitignore
}

if (Test-Path (Join-Path $Root "src")) {
    Invoke-Git rm -r --cached src 2>$null
    if ($LASTEXITCODE -ne 0) {
        $ErrorActionPreference = 'SilentlyContinue'
        Invoke-Git rm -r --cached src
        $ErrorActionPreference = 'Stop'
    }
}

$status = & $git @GitConfig status --porcelain
if ($status) {
    Invoke-Git commit -m "chore: GitHub public tree without src/"
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

Invoke-Git push github public:main
Write-Host "Pushed public branch (no src/) to github/main"

Invoke-Git checkout main
Write-Host "Back on main (full source for Gitea)"
