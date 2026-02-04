$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$compilerDir = Join-Path $scriptDir "Effects\Compiler"
$easyXnbPath = Join-Path $compilerDir "EasyXnb.exe"

if (-not (Test-Path $easyXnbPath)) {
    Write-Host "EasyXnb.exe not found at $easyXnbPath"
    exit 1
}

Write-Host "Compiling Shaders using EasyXnb..."
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
    $xnbFiles = Get-ChildItem -Path $compilerDir -Filter "*.xnb"
    $effectsDir = Join-Path $scriptDir "Effects"
    foreach ($file in $xnbFiles) {
        $destination = Join-Path $effectsDir $file.Name
        Move-Item -Path $file.FullName -Destination $destination -Force
        Write-Host "Moved $($file.Name) to Effects/"
    }
}
catch {
    Write-Host "An error occurred during shader compilation: $_"
    exit 1
}
finally {
    Pop-Location
}
