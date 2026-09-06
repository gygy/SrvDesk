param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$dotnet = "$env:LOCALAPPDATA\dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) { $dotnet = "dotnet" }
$repo = Split-Path $PSScriptRoot -Parent
$proj = Join-Path $repo "src\WinServerOPT\WinServerOPT.csproj"
$dist = Join-Path $repo "dist"

New-Item -ItemType Directory -Force -Path $dist | Out-Null

# 每次发布前删除 dist 内全部旧产物（含历史时间戳 exe）
function Clear-DistArtifacts {
    param([string]$Dir)
    $removed = 0
    $locked = @()
    Get-ChildItem -LiteralPath $Dir -Force -ErrorAction SilentlyContinue |
        Where-Object { -not $_.PSIsContainer } |
        ForEach-Object {
            try {
                Remove-Item -LiteralPath $_.FullName -Force -ErrorAction Stop
                $removed++
            }
            catch {
                $locked += $_.Name
            }
        }
    return @{ Removed = $removed; Locked = $locked }
}

$clear = Clear-DistArtifacts -Dir $dist
Write-Host "已清理 dist 旧文件: $($clear.Removed) 个"
if ($clear.Locked.Count -gt 0) {
    Write-Warning ("以下文件被占用未能删除: " + ($clear.Locked -join ", "))
}

& $dotnet publish $proj -c $Configuration -o $dist
if ($LASTEXITCODE -ne 0) {
    # 主文件被占用时，发布到临时目录再拷贝为带时间戳的新 exe
    $stamp = Get-Date -Format "HHmmss"
    $tmp = Join-Path $repo ("dist-tmp-" + $stamp)
    New-Item -ItemType Directory -Force -Path $tmp | Out-Null
    try {
        & $dotnet publish $proj -c $Configuration -o $tmp
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
        $built = Get-ChildItem -LiteralPath $tmp -Filter "*.exe" | Select-Object -First 1
        if ($null -eq $built) {
            Write-Error "发布目录中未找到 exe"
            exit 1
        }
        $target = Join-Path $dist ("SrvDesk-" + $stamp + ".exe")
        Copy-Item -LiteralPath $built.FullName -Destination $target -Force
        Write-Host "dist\SrvDesk.exe 被占用，已输出: $target ($($built.Length) bytes)"
        exit 0
    }
    finally {
        Remove-Item -LiteralPath $tmp -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Get-ChildItem -LiteralPath $dist -Filter "*.config" -ErrorAction SilentlyContinue | Remove-Item -Force
Get-ChildItem -LiteralPath $dist -Filter "*.pdb" -ErrorAction SilentlyContinue | Remove-Item -Force

# 再次清理：只保留本次 SrvDesk.exe（去掉误留的时间戳副本）
Get-ChildItem -LiteralPath $dist -Filter "SrvDesk-*.exe" -ErrorAction SilentlyContinue | Remove-Item -Force -ErrorAction SilentlyContinue

$exe = Get-ChildItem -LiteralPath $dist -Filter "SrvDesk.exe" -ErrorAction SilentlyContinue | Select-Object -First 1
if ($null -eq $exe) {
    $exe = Get-ChildItem -LiteralPath $dist -Filter "*.exe" | Select-Object -First 1
}
if ($null -eq $exe) {
    Write-Error "dist 目录中未找到 exe 文件"
    exit 1
}

Write-Host "已发布: $($exe.FullName) ($($exe.Length) bytes)"
