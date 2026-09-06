param(
    [string]$Configuration = "Release",
    [switch]$SkipObfuscate
)

$ErrorActionPreference = "Stop"
$dotnet = "$env:LOCALAPPDATA\dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) { $dotnet = "dotnet" }
$repo = Split-Path $PSScriptRoot -Parent
$proj = Join-Path $repo "src\WinServerOPT\WinServerOPT.csproj"
$dist = Join-Path $repo "dist"
$obfuscarXml = Join-Path $PSScriptRoot "obfuscar.xml"
$toolsDir = Join-Path $repo "tools\obfuscar"
$publishRaw = Join-Path $repo "artifacts\publish-raw"
$obfIn = Join-Path $repo "artifacts\obfuscar-in"
$obfOut = Join-Path $repo "artifacts\obfuscar-out"

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

function Publish-ToDir([string]$OutDir) {
    if (Test-Path $OutDir) {
        Remove-Item -LiteralPath $OutDir -Recurse -Force -ErrorAction SilentlyContinue
    }
    New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
    # Out-Host: keep tool logs off the success pipeline so return value stays numeric
    & $dotnet publish $proj -c $Configuration -o $OutDir 2>&1 | ForEach-Object { Write-Host $_ }
    return [int]$LASTEXITCODE
}

function Ensure-Obfuscar {
    $exe = Join-Path $toolsDir "obfuscar.console.exe"
    if (Test-Path $exe) { return $exe }

    New-Item -ItemType Directory -Force -Path $toolsDir | Out-Null
    Write-Host "Installing Obfuscar.GlobalTool (light obfuscation)..."
    & $dotnet tool install Obfuscar.GlobalTool --tool-path $toolsDir --version 2.2.50 2>&1 | ForEach-Object { Write-Host $_ }
    if ($LASTEXITCODE -ne 0) {
        & $dotnet tool update Obfuscar.GlobalTool --tool-path $toolsDir --version 2.2.50 2>&1 | ForEach-Object { Write-Host $_ }
    }
    if (-not (Test-Path $exe)) {
        throw "Obfuscar not found after install: $exe"
    }
    return $exe
}

function Invoke-LightObfuscate([string]$SourceExe, [string]$DestExe) {
    $obfuscar = Ensure-Obfuscar

    foreach ($d in @($obfIn, $obfOut)) {
        if (Test-Path $d) { Remove-Item -LiteralPath $d -Recurse -Force -ErrorAction SilentlyContinue }
        New-Item -ItemType Directory -Force -Path $d | Out-Null
    }

    Copy-Item -LiteralPath $SourceExe -Destination (Join-Path $obfIn "SrvDesk.exe") -Force

    # Fill absolute paths into a temp config (Obfuscar resolves relative to cwd)
    $cfg = Join-Path $repo "artifacts\obfuscar.generated.xml"
    $xml = Get-Content -LiteralPath $obfuscarXml -Raw -Encoding UTF8
    $xml = $xml.Replace('value="./in"', ('value="' + $obfIn + '"'))
    $xml = $xml.Replace('value="./out"', ('value="' + $obfOut + '"'))

    $fxSearch = @(
        "${env:ProgramFiles(x86)}\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8",
        "${env:ProgramFiles}\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8",
        "C:\Windows\Microsoft.NET\Framework64\v4.0.30319",
        "C:\Windows\Microsoft.NET\Framework\v4.0.30319"
    ) | Where-Object { Test-Path (Join-Path $_ "System.Windows.Forms.dll") } | Select-Object -First 1
    if (-not $fxSearch) {
        throw "Cannot find System.Windows.Forms.dll for Obfuscar AssemblySearchPath"
    }
    $xml = $xml.Replace('path="./framework"', ('path="' + $fxSearch + '"'))

    $utf8Bom = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText($cfg, $xml, $utf8Bom)

    # Local SDK under %LOCALAPPDATA%\dotnet needs DOTNET_ROOT for apphost tools
    $dotnetRoot = Split-Path $dotnet -Parent
    if ($dotnet -eq "dotnet") {
        $dotnetRoot = $env:DOTNET_ROOT
    }
    $prevDotnetRoot = $env:DOTNET_ROOT
    if ($dotnetRoot) { $env:DOTNET_ROOT = $dotnetRoot }

    Push-Location $repo
    try {
        & $obfuscar $cfg 2>&1 | ForEach-Object { Write-Host $_ }
        if ($LASTEXITCODE -ne 0) {
            throw "Obfuscar failed with exit code $LASTEXITCODE"
        }
    }
    finally {
        Pop-Location
        if ($null -eq $prevDotnetRoot) { Remove-Item Env:DOTNET_ROOT -ErrorAction SilentlyContinue }
        else { $env:DOTNET_ROOT = $prevDotnetRoot }
    }

    $outExe = Join-Path $obfOut "SrvDesk.exe"
    if (-not (Test-Path $outExe)) {
        throw "Obfuscar output missing: $outExe"
    }
    Copy-Item -LiteralPath $outExe -Destination $DestExe -Force
    Write-Host ("Obfuscated (light): {0}" -f $DestExe)
}

$code = Publish-ToDir $publishRaw
if ($code -ne 0) {
    Write-Error "dotnet publish failed"
    exit $code
}

$built = Get-ChildItem -LiteralPath $publishRaw -Filter "SrvDesk.exe" | Select-Object -First 1
if ($null -eq $built) {
    Write-Error "No SrvDesk.exe in publish output"
    exit 1
}

$destExe = Join-Path $dist "SrvDesk.exe"
try {
    if ($SkipObfuscate) {
        Copy-Item -LiteralPath $built.FullName -Destination $destExe -Force
        Write-Host "SkipObfuscate: copied plain Release build"
    }
    else {
        Invoke-LightObfuscate -SourceExe $built.FullName -DestExe $destExe
    }
}
catch {
    # dist locked: write stamped obfuscated/plain copy
    $stamp = Get-Date -Format "HHmmss"
    $fallback = Join-Path $dist ("SrvDesk-" + $stamp + ".exe")
    Write-Warning $_.Exception.Message
    if ($SkipObfuscate) {
        Copy-Item -LiteralPath $built.FullName -Destination $fallback -Force
    }
    else {
        try {
            Invoke-LightObfuscate -SourceExe $built.FullName -DestExe $fallback
        }
        catch {
            Copy-Item -LiteralPath $built.FullName -Destination $fallback -Force
            Write-Warning "Obfuscation failed; wrote plain build as fallback"
        }
    }
    Write-Host ("Wrote: {0} ({1} bytes)" -f $fallback, (Get-Item $fallback).Length)
    exit 0
}

Get-ChildItem -LiteralPath $dist -Filter "*.config" -ErrorAction SilentlyContinue | Remove-Item -Force
Get-ChildItem -LiteralPath $dist -Filter "*.pdb" -ErrorAction SilentlyContinue | Remove-Item -Force
Get-ChildItem -LiteralPath $dist -Filter "SrvDesk-*.exe" -ErrorAction SilentlyContinue |
    Remove-Item -Force -ErrorAction SilentlyContinue

# Clean temp artifacts (keep tools\obfuscar cache)
Remove-Item -LiteralPath $publishRaw -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $obfIn -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $obfOut -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath (Join-Path $repo "artifacts\obfuscar.generated.xml") -Force -ErrorAction SilentlyContinue

$exe = Get-Item -LiteralPath $destExe
Write-Host ("Published: {0} ({1} bytes) [light obfuscation]" -f $exe.FullName, $exe.Length)
