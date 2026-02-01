# Compiles .fx shaders to .mgfxo for Destiny2 mod (embedded in build)
# Run after editing shaders, before building. Requires: dotnet tool install -g dotnet-mgfxc
param(
    [string]$EffectsDir = (Join-Path $PSScriptRoot "Content\Shaders")
)

$fxFiles = Get-ChildItem -Path $EffectsDir -Filter "*.fx" -File -ErrorAction SilentlyContinue
if ($fxFiles.Count -eq 0) {
    Write-Host "No .fx files in $EffectsDir"
    exit 0
}

foreach ($fx in $fxFiles) {
    $base = [System.IO.Path]::GetFileNameWithoutExtension($fx.FullName)
    $outPath = Join-Path $EffectsDir "$base.mgfxo"
    Write-Host "Compiling $($fx.Name) -> $base.mgfxo"
    & mgfxc $fx.FullName $outPath /Profile:OpenGL
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  OK" -ForegroundColor Green
    } else {
        Write-Host "  FAILED" -ForegroundColor Red
        exit 1
    }
}
