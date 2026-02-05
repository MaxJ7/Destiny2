$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$compilerDir = Join-Path $scriptDir "Effects\Compiler"
$easyXnbPath = Join-Path $compilerDir "EasyXnb.exe"

if (-not (Test-Path $easyXnbPath)) {
    Write-Host "EasyXnb.exe not found at $easyXnbPath"
    exit 1
}

Write-Host "Compiling Shaders using EasyXnb... (Version 5)"
Push-Location $compilerDir

try {
    # EasyXnb.exe Config should already be set to InputDir=".." OutputDir=".."
    # If not, it defaults to local, so ensuring we are in the directory is key.
    # We run it and wait for exit.
    $process = Start-Process -FilePath $easyXnbPath -Wait -PassThru -NoNewWindow
    
    if ($process.ExitCode -ne 0) {
        Write-Host "EasyXnb failed with exit code $($process.ExitCode)"
        exit $process.ExitCode
    }
    
    Write-Host "Shader compilation successful."
    
    # Move compiled XNBs to Effects folder
    # Move compiled XNBs to Effects folder (Bulk Move)
    $xnbPattern = Join-Path $compilerDir "*.xnb"
    $effectsDir = Join-Path $scriptDir "Effects"
    
    Write-Host "Moving all .xnb files to Effects/..."
    if (Test-Path $xnbPattern) {
        Move-Item -Path $xnbPattern -Destination $effectsDir -Force
    }
}
catch {
    Write-Host "An error occurred during shader compilation: $_"
    exit 1
}
finally {
    Pop-Location
}
