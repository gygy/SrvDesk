param(
    [Parameter(Mandatory = $true)]
    [string]$SourceExe
)

$ErrorActionPreference = "Stop"
$repo = Split-Path $PSScriptRoot -Parent
$dist = Join-Path $repo "dist"
New-Item -ItemType Directory -Force -Path $dist | Out-Null

if (-not (Test-Path -LiteralPath $SourceExe)) {
    Write-Host "copy-to-dist: source missing: $SourceExe"
    exit 0
}

$dest = Join-Path $dist "SrvDesk.exe"
try {
    Copy-Item -LiteralPath $SourceExe -Destination $dest -Force
    Write-Host "Copied to dist\SrvDesk.exe"
}
catch {
    $stamp = Get-Date -Format "HHmmss"
    $alt = Join-Path $dist ("SrvDesk-" + $stamp + ".exe")
    Copy-Item -LiteralPath $SourceExe -Destination $alt -Force
    Write-Host "dist\SrvDesk.exe is locked, copied to dist\$(Split-Path $alt -Leaf)"
}
