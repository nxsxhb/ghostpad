param(
    [ValidateSet("portable", "standalone")]
    [string]$Target = "portable"
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host " Building GhostPad ($Target)..." -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Terminate existing process if running
Get-Process -Name "GhostPad" -ErrorAction SilentlyContinue | Stop-Process -Force

if ($Target -eq "portable") {
    $outDir = ".\publish"
    Write-Host "Publishing framework-dependent release to $outDir..." -ForegroundColor Yellow
    & "C:\Program Files\dotnet\dotnet.exe" publish -c Release -r win-x64 --self-contained false -o $outDir
} else {
    $outDir = ".\publish-package"
    Write-Host "Publishing single-file self-contained release to $outDir..." -ForegroundColor Yellow
    & "C:\Program Files\dotnet\dotnet.exe" publish -c Release -r win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -o $outDir
}

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n[OK] GhostPad successfully built in $outDir!" -ForegroundColor Green
} else {
    Write-Host "`n[ERROR] Build failed with exit code $LASTEXITCODE" -ForegroundColor Red
}
