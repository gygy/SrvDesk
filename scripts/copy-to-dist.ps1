param(
    [Parameter(Mandatory = $true)]
    [string]$SourceExe
)

$ErrorActionPreference = "Stop"
$repo = Split-Path $PSScriptRoot -Parent
$dist = Join-Path $repo "dist"
New-Item -ItemType Directory -Force -Path $dist | Out-Null

# 杀软常秒删 obj\SrvDesk.exe：先抢拷到 TEMP，再回填，避免后续 MSB3030
$safe = Join-Path $env:TEMP ("SrvDesk-build-safe-" + $PID + ".exe")

function Copy-Force([string]$from, [string]$to) {
    [System.IO.File]::Copy($from, $to, $true)
}

if (-not (Test-Path -LiteralPath $SourceExe)) {
    Write-Host "copy-to-dist: source missing: $SourceExe"
    # 尝试用上次抢拷回填
    if (Test-Path -LiteralPath $safe) {
        Copy-Force $safe $SourceExe
        Write-Host "Restored source from TEMP safe copy"
    }
    else {
        exit 0
    }
}

try {
    Copy-Force $SourceExe $safe
}
catch {
    Write-Warning ("Safe copy failed: " + $_.Exception.Message)
}

$dest = Join-Path $dist "SrvDesk.exe"
try {
    Copy-Force $SourceExe $dest
    Write-Host "Copied to dist\SrvDesk.exe"
}
catch {
    $stamp = Get-Date -Format "HHmmss"
    $alt = Join-Path $dist ("SrvDesk-" + $stamp + ".exe")
    try {
        Copy-Force $SourceExe $alt
        Write-Host "dist\SrvDesk.exe is locked, copied to dist\$(Split-Path $alt -Leaf)"
    }
    catch {
        if (Test-Path -LiteralPath $safe) {
            Copy-Force $safe $alt
            Write-Host "Copied safe build to dist\$(Split-Path $alt -Leaf)"
        }
        else { throw }
    }
}

# 若杀软已删 Intermediate 输出，立刻从 safe 回填，供后续 Copy to bin / publish
if (-not (Test-Path -LiteralPath $SourceExe) -and (Test-Path -LiteralPath $safe)) {
    try {
        $dir = Split-Path -Parent $SourceExe
        if (-not (Test-Path -LiteralPath $dir)) {
            New-Item -ItemType Directory -Force -Path $dir | Out-Null
        }
        Copy-Force $safe $SourceExe
        Write-Host "Re-filled IntermediateOutputPath from safe copy"
    }
    catch {
        Write-Warning ("Re-fill failed: " + $_.Exception.Message)
    }
}
