# Solar Trail Diagnostic Script
Write-Host "`n==== SOLAR TRAIL IMPLEMENTATION CHECK ====" -ForegroundColor Cyan

# 1. Check texture file
$noiseTexture = "Assets\Textures\Noise\SolarFlameNoise.png"
if (Test-Path $noiseTexture) {
    $fileInfo = Get-Item $noiseTexture
    Write-Host "✅ Noise Texture: EXISTS ($($fileInfo.Length) bytes)" -ForegroundColor Green
} else {
    Write-Host "❌ Noise Texture: MISSING at $noiseTexture" -ForegroundColor Red
}

# 2. Check compiled shader
$shaderXnb = "Effects\BulletTrailSolar.xnb"
if (Test-Path $shaderXnb) {
    $shaderInfo = Get-Item $shaderXnb
    Write-Host "✅ Compiled Shader: EXISTS ($($shaderInfo.Length) bytes, Last: $($shaderInfo.LastWriteTime))" -ForegroundColor Green
} else {
    Write-Host "❌ Compiled Shader: MISSING at $shaderXnb" -ForegroundColor Red
}

# 3. Check shader source has uImage1
$shaderFx = "Effects\BulletTrailSolar.fx"
if (Test-Path $shaderFx) {
    $hasUImage1 = Select-String -Path $shaderFx -Pattern "sampler uImage1" -Quiet
    if ($hasUImage1) {
        Write-Host "✅ Shader Source: Contains 'sampler uImage1'" -ForegroundColor Green
    } else {
        Write-Host "❌ Shader Source: Missing 'sampler uImage1'" -ForegroundColor Red
    }
} else {
    Write-Host "❌ Shader Source: FILE MISSING" -ForegroundColor Red
}

# 4. Check C# renderer has texture binding
$renderer = "Content\Graphics\Renderers\ElementalBulletRenderer.cs"
if (Test-Path $renderer) {
    $hasBinding = Select-String -Path $renderer -Pattern 'uImage1.*SetValue' -Quiet
    if ($hasBinding) {
        Write-Host "✅ C# Renderer: Contains texture binding code" -ForegroundColor Green
    } else {
        Write-Host "❌ C# Renderer: Missing texture binding" -ForegroundColor Red
    }
} else {
    Write-Host "❌ C# Renderer: FILE MISSING" -ForegroundColor Red
}

# 5. Check if tModLoader is running (blocks build)
$tmlProcess = Get-Process | Where-Object { $_.ProcessName -like "*tModLoader*" -or $_.ProcessName -like "*Terraria*" }
if ($tmlProcess) {
    Write-Host "⚠️  tModLoader/Terraria IS RUNNING - Must close to build!" -ForegroundColor Yellow
} else {
    Write-Host "✅ tModLoader/Terraria: Not running (safe to build)" -ForegroundColor Green
}

Write-Host "`n==== NEXT STEPS ====" -ForegroundColor Cyan
if ($tmlProcess) {
    Write-Host "1. Close  tModLoader/Terraria" -ForegroundColor Yellow
    Write-Host "2. Run: dotnet build" -ForegroundColor White
    Write-Host "3. Launch tModLoader and test Solar weapon" -ForegroundColor White
} else {
    Write-Host "Ready to build!" -ForegroundColor Green
    Write-Host "Run: dotnet build" -ForegroundColor White
}
