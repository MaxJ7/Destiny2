# Compiles .fx shaders using local EasyXnb and deploys to Assets
# Source: Effects/
# Compiler: Effects/Compiler/EasyXnb.exe
# Destination: Assets/AutoloadedEffects/Shaders/Primitives/

$ErrorActionPreference = "Stop"
$ScriptRoot = $PSScriptRoot

# 1. Run EasyXnb from Effects dir (it scans current dir)
Write-Host "Running EasyXnb..."
Push-Location "$ScriptRoot\Effects"
try {
    & ".\Compiler\EasyXnb.exe"
}
finally {
    Pop-Location
}

# 2. Results
Write-Host "Shader compilation complete. XNB files are in $ScriptRoot\Effects"

