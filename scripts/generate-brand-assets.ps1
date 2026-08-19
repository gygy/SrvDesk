param(
    [string]$OutDir = (Join-Path $PSScriptRoot "..\src\WinServerOPT")
)

$dotnet = "$env:LOCALAPPDATA\dotnet\dotnet.exe"
$proj = Join-Path $PSScriptRoot "IconGenerator\IconGenerator.csproj"

& $dotnet run --project $proj -c Release -- $OutDir
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Brand assets updated."
