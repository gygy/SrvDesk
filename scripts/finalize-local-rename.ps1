# Wrapper: physical rename lives in _common so this folder can be renamed safely.
$common = Join-Path (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent) "_common\scripts\finalize-srvdesk-rename.ps1"
if (-not (Test-Path -LiteralPath $common)) {
    $common = "G:\gitea\_common\scripts\finalize-srvdesk-rename.ps1"
}
& $common @args
exit $LASTEXITCODE
