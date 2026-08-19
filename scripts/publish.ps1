param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$dotnet = "$env:LOCALAPPDATA\dotnet\dotnet.exe"
$repo = Split-Path $PSScriptRoot -Parent
$proj = Join-Path $repo "src\WinServerOPT\WinServerOPT.csproj"
$dist = Join-Path $repo "dist"

& $dotnet publish $proj -c $Configuration -o $dist
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Get-ChildItem -LiteralPath $dist -Filter "*.config" -ErrorAction SilentlyContinue | Remove-Item -Force
Get-ChildItem -LiteralPath $dist -Filter "*.pdb" -ErrorAction SilentlyContinue | Remove-Item -Force

$exe = Get-ChildItem -LiteralPath $dist -Filter "*.exe" | Select-Object -First 1
if ($null -eq $exe) {
    Write-Error "dist 目录中未找到 exe 文件"
    exit 1
}

Write-Host "已发布单文件: $($exe.FullName) ($($exe.Length) bytes)"
