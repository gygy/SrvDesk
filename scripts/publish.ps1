param(
    [string]$Configuration = "Release",
    [switch]$SkipObfuscate,
    [switch]$NoBumpVersion,
    [switch]$SkipInstaller
)

$ErrorActionPreference = "Stop"
$dotnet = "$env:LOCALAPPDATA\dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) { $dotnet = "dotnet" }
$repo = Split-Path $PSScriptRoot -Parent
$proj = Join-Path $repo "src\SrvDesk\SrvDesk.csproj"
$dist = Join-Path $repo "dist"
$obfuscarXml = Join-Path $PSScriptRoot "obfuscar.xml"
$issScript = Join-Path $PSScriptRoot "SrvDesk.iss"
$toolsDir = Join-Path $repo "tools\obfuscar"
$innoToolsDir = Join-Path $repo "tools\innosetup"
$publishRaw = Join-Path $repo "artifacts\publish-raw"
$obfIn = Join-Path $repo "artifacts\obfuscar-in"
$obfOut = Join-Path $repo "artifacts\obfuscar-out"
$appIcon = Join-Path $repo "src\SrvDesk\app.ico"

function Get-ProjectVersion([string]$CsprojPath) {
    $raw = Get-Content -LiteralPath $CsprojPath -Raw -Encoding UTF8
    $m = [regex]::Match($raw, '<Version>\s*([^<]+?)\s*</Version>')
    if (-not $m.Success) {
        throw "Cannot find <Version> in $CsprojPath"
    }
    return $m.Groups[1].Value.Trim()
}

function Set-ProjectVersion([string]$CsprojPath, [string]$NewVersion) {
    $raw = Get-Content -LiteralPath $CsprojPath -Raw -Encoding UTF8
    $fileVer = if ($NewVersion -match '^\d+\.\d+\.\d+$') { "$NewVersion.0" } else { $NewVersion }
    $raw = [regex]::Replace($raw, '(?m)(<Version>)[^<]*(</Version>)', "`${1}$NewVersion`${2}")
    $raw = [regex]::Replace($raw, '(?m)(<FileVersion>)[^<]*(</FileVersion>)', "`${1}$fileVer`${2}")
    $raw = [regex]::Replace($raw, '(?m)(<InformationalVersion>)[^<]*(</InformationalVersion>)', "`${1}$NewVersion`${2}")
    $utf8 = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText($CsprojPath, $raw, $utf8)
}

function Bump-PatchVersion([string]$Version) {
    if ($Version -notmatch '^(\d+)\.(\d+)\.(\d+)(.*)$') {
        throw "Unsupported version format: $Version (expect major.minor.patch)"
    }
    $major = [int]$Matches[1]
    $minor = [int]$Matches[2]
    $patch = [int]$Matches[3] + 1
    $suffix = $Matches[4]
    return "{0}.{1}.{2}{3}" -f $major, $minor, $patch, $suffix
}

if (-not $NoBumpVersion) {
    $oldVer = Get-ProjectVersion $proj
    $newVer = Bump-PatchVersion $oldVer
    Set-ProjectVersion -CsprojPath $proj -NewVersion $newVer
    Write-Host ("Version bumped: {0} → {1}" -f $oldVer, $newVer)
}
else {
    Write-Host ("Version unchanged: {0}" -f (Get-ProjectVersion $proj))
}

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

function Get-InnoCompiler {
    foreach ($c in @(
            (Join-Path $innoToolsDir "ISCC.exe"),
            "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
            "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
        )) {
        if ($c -and (Test-Path -LiteralPath $c)) { return $c }
    }
    return $null
}

function Ensure-InnoSetup {
    $existing = Get-InnoCompiler
    if ($existing) { return $existing }

    New-Item -ItemType Directory -Force -Path $innoToolsDir | Out-Null
    $setup = Join-Path $repo "artifacts\innosetup-install.exe"
    New-Item -ItemType Directory -Force -Path (Split-Path $setup -Parent) | Out-Null

    $mirrors = @(
        "https://jrsoftware.org/download.php/is.exe",
        "https://files.jrsoftware.org/is/6/innosetup-6.4.3.exe",
        "https://ghproxy.net/https://github.com/jrsoftware/issrc/releases/download/is-6_4_3/innosetup-6.4.3.exe"
    )

    Write-Host "Downloading Inno Setup 6 (for SrvDesk-Setup.exe)..."
    $ok = $false
    foreach ($url in $mirrors) {
        try {
            if (Test-Path $setup) { Remove-Item -LiteralPath $setup -Force -ErrorAction SilentlyContinue }
            Write-Host ("  try: " + $url)
            Invoke-WebRequest -Uri $url -OutFile $setup -UseBasicParsing -TimeoutSec 180
            if ((Test-Path $setup) -and ((Get-Item $setup).Length -gt 1000000)) {
                $ok = $true
                break
            }
        }
        catch {
            Write-Warning ("Inno download failed: " + $_.Exception.Message)
        }
    }
    if (-not $ok) {
        throw "Cannot download Inno Setup. Install manually, or place ISCC.exe under tools\innosetup\"
    }

    Write-Host "Installing Inno Setup to tools\innosetup ..."
    $args = "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /DIR=`"$innoToolsDir`""
    $p = Start-Process -FilePath $setup -ArgumentList $args -Wait -PassThru
    if ($p.ExitCode -ne 0 -and $p.ExitCode -ne 3010) {
        throw "Inno Setup silent install failed, exit $($p.ExitCode)"
    }

    $iscc = Get-InnoCompiler
    if (-not $iscc) {
        throw "ISCC.exe not found after Inno Setup install"
    }
    Remove-Item -LiteralPath $setup -Force -ErrorAction SilentlyContinue
    return $iscc
}

function Invoke-BuildInstaller([string]$PayloadExe, [string]$Version) {
    if (-not (Test-Path -LiteralPath $PayloadExe)) {
        throw "Installer payload missing: $PayloadExe"
    }
    if (-not (Test-Path -LiteralPath $issScript)) {
        throw "Inno script missing: $issScript"
    }
    if (-not (Test-Path -LiteralPath $appIcon)) {
        throw "Setup icon missing: $appIcon"
    }

    $iscc = Ensure-InnoSetup
    $outName = "SrvDesk-Setup"
    $setupPath = Join-Path $dist ($outName + ".exe")
    if (Test-Path $setupPath) {
        Remove-Item -LiteralPath $setupPath -Force -ErrorAction SilentlyContinue
    }

    $defines = @(
        "/DMyAppVersion=$Version",
        ('/DMyAppSource="' + $PayloadExe + '"'),
        ('/DMyAppIcon="' + $appIcon + '"'),
        ('/DMyOutputDir="' + $dist + '"'),
        "/DMyOutputName=$outName"
    )

    Write-Host ("Building installer from obfuscated payload: " + $PayloadExe)
    & $iscc $defines $issScript 2>&1 | ForEach-Object { Write-Host $_ }
    if ($LASTEXITCODE -ne 0) {
        throw "ISCC failed with exit code $LASTEXITCODE"
    }
    if (-not (Test-Path -LiteralPath $setupPath)) {
        throw "Installer output missing: $setupPath"
    }
    Write-Host ("Installer: {0} ({1} bytes)" -f $setupPath, (Get-Item $setupPath).Length)
    return $setupPath
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
$obfuscatedPayload = $null
try {
    if ($SkipObfuscate) {
        Copy-Item -LiteralPath $built.FullName -Destination $destExe -Force
        Write-Host "SkipObfuscate: copied plain Release build (installer will NOT be built)"
    }
    else {
        Invoke-LightObfuscate -SourceExe $built.FullName -DestExe $destExe
        $obfuscatedPayload = $destExe
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
            $obfuscatedPayload = $fallback
        }
        catch {
            Copy-Item -LiteralPath $built.FullName -Destination $fallback -Force
            Write-Warning "Obfuscation failed; wrote plain build as fallback"
        }
    }
    Write-Host ("Wrote: {0} ({1} bytes)" -f $fallback, (Get-Item $fallback).Length)
    if (-not $obfuscatedPayload) {
        Write-Warning "No obfuscated payload; skip installer"
        exit 0
    }
    $destExe = $obfuscatedPayload
}

Get-ChildItem -LiteralPath $dist -Filter "*.config" -ErrorAction SilentlyContinue | Remove-Item -Force
Get-ChildItem -LiteralPath $dist -Filter "*.pdb" -ErrorAction SilentlyContinue | Remove-Item -Force
Get-ChildItem -LiteralPath $dist -Filter "SrvDesk-*.exe" -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -notlike "SrvDesk-Setup*" } |
    Remove-Item -Force -ErrorAction SilentlyContinue

$ver = Get-ProjectVersion $proj
$setupBuilt = $null
if ($SkipInstaller) {
    Write-Host "SkipInstaller: not building SrvDesk-Setup.exe"
}
elseif ($SkipObfuscate -or -not $obfuscatedPayload) {
    Write-Warning "Installer requires light-obfuscated payload; skipped (do not ship plain build as Setup)."
}
else {
    try {
        $setupBuilt = Invoke-BuildInstaller -PayloadExe $obfuscatedPayload -Version $ver
    }
    catch {
        Write-Error ("Installer build failed: " + $_.Exception.Message)
        exit 1
    }
}

# Clean temp artifacts (keep tools\obfuscar / tools\innosetup cache)
Remove-Item -LiteralPath $publishRaw -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $obfIn -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $obfOut -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath (Join-Path $repo "artifacts\obfuscar.generated.xml") -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath (Join-Path $repo "artifacts\innosetup-install.exe") -Force -ErrorAction SilentlyContinue

$exe = Get-Item -LiteralPath $destExe
Write-Host ("Published: {0} ({1} bytes) v{2} [light obfuscation]" -f $exe.FullName, $exe.Length, $ver)
if ($setupBuilt) {
    $setupItem = Get-Item -LiteralPath $setupBuilt
    Write-Host ("Published: {0} ({1} bytes) v{2} [setup + desktop shortcut]" -f $setupItem.FullName, $setupItem.Length, $ver)
}
