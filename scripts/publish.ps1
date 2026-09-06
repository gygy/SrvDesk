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

# Stop processes running from dist so old EXEs can be deleted
Get-Process -ErrorAction SilentlyContinue | ForEach-Object {
    $proc = $_
    try {
        $p = $proc.Path
        if ($p -and $p.StartsWith($dist, [StringComparison]::OrdinalIgnoreCase)) {
            Stop-Process -Id $proc.Id -Force -ErrorAction Stop
            Write-Host ("Stopped process: {0} ({1})" -f $proc.ProcessName, $proc.Id)
        }
    }
    catch { }
}
Start-Sleep -Milliseconds 400

# Always clear previous dist artifacts before publish
$removed = 0
$locked = New-Object System.Collections.Generic.List[string]
Get-ChildItem -LiteralPath $dist -Force -ErrorAction SilentlyContinue |
    Where-Object { -not $_.PSIsContainer } |
    ForEach-Object {
        try {
            Remove-Item -LiteralPath $_.FullName -Force -ErrorAction Stop
            $removed++
        }
        catch {
            [void]$locked.Add($_.Name)
        }
    }
Write-Host ("Cleared old dist files: {0}" -f $removed)
if ($locked.Count -gt 0) {
    Write-Warning ("Locked (not deleted): " + ($locked -join ", "))
}

& $dotnet publish $proj -c $Configuration -o $dist
if ($LASTEXITCODE -ne 0) {
    $stamp = Get-Date -Format "HHmmss"
    $tmp = Join-Path $repo ("dist-tmp-" + $stamp)
    New-Item -ItemType Directory -Force -Path $tmp | Out-Null
    try {
        & $dotnet publish $proj -c $Configuration -o $tmp
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
        $built = Get-ChildItem -LiteralPath $tmp -Filter "*.exe" | Select-Object -First 1
        if ($null -eq $built) {
            Write-Error "No exe found in publish output"
            exit 1
        }
        Get-ChildItem -LiteralPath $dist -Filter "SrvDesk-*.exe" -ErrorAction SilentlyContinue |
            Remove-Item -Force -ErrorAction SilentlyContinue
        $target = Join-Path $dist ("SrvDesk-" + $stamp + ".exe")
        Copy-Item -LiteralPath $built.FullName -Destination $target -Force
        Write-Host ("SrvDesk.exe locked; wrote: {0} ({1} bytes)" -f $target, $built.Length)
        exit 0
    }
    finally {
        Remove-Item -LiteralPath $tmp -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Get-ChildItem -LiteralPath $dist -Filter "*.config" -ErrorAction SilentlyContinue | Remove-Item -Force
Get-ChildItem -LiteralPath $dist -Filter "*.pdb" -ErrorAction SilentlyContinue | Remove-Item -Force
Get-ChildItem -LiteralPath $dist -Filter "SrvDesk-*.exe" -ErrorAction SilentlyContinue |
    Remove-Item -Force -ErrorAction SilentlyContinue

$exe = Get-ChildItem -LiteralPath $dist -Filter "SrvDesk.exe" -ErrorAction SilentlyContinue | Select-Object -First 1
if ($null -eq $exe) {
    $exe = Get-ChildItem -LiteralPath $dist -Filter "*.exe" | Select-Object -First 1
}
if ($null -eq $exe) {
    Write-Error "No exe found in dist"
    exit 1
}

Write-Host ("Published: {0} ({1} bytes)" -f $exe.FullName, $exe.Length)