#!/usr/bin/env pwsh
# Build script for PickleRick Installer

$ErrorActionPreference = "Stop"

Write-Host "`n=== PickleRick Installer Build Script ===`n" -ForegroundColor Cyan

# Clean function
function Clean-Directory {
    param($path)
    if (Test-Path $path) {
        Remove-Item $path -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Write-Host "[1/6] Cleaning build artifacts..." -ForegroundColor Yellow
Clean-Directory "obj"
Clean-Directory "bin"
Clean-Directory "PickleRick.Player\obj"
Clean-Directory "PickleRick.Player\bin"
Clean-Directory "PickleRick.Installer\obj"
Clean-Directory "PickleRick.Installer\bin"
Clean-Directory "PickleRick.Installer\ServicePayload.zip"
Write-Host "      Done.`n" -ForegroundColor Green

Write-Host "[2/6] Building PickleRick Service..." -ForegroundColor Yellow
$result = dotnet publish PickleRick.csproj -c Release -o "PickleRick.Installer\obj\ServicePublish" 2>&1
if ($LASTEXITCODE -ne 0) { 
    Write-Host "ERROR: Service build failed!" -ForegroundColor Red
    Write-Host $result
    exit $LASTEXITCODE 
}
Write-Host "      Done.`n" -ForegroundColor Green

Write-Host "[3/6] Building PickleRick Player..." -ForegroundColor Yellow
$result = dotnet publish PickleRick.Player\PickleRick.Player.csproj -c Release -o "PickleRick.Installer\obj\PlayerPublish" 2>&1
if ($LASTEXITCODE -ne 0) { 
    Write-Host "ERROR: Player build failed!" -ForegroundColor Red
    Write-Host $result
    exit $LASTEXITCODE 
}
Write-Host "      Done.`n" -ForegroundColor Green

Write-Host "[4/6] Creating payload package..." -ForegroundColor Yellow
$stagingPath = "PickleRick.Installer\obj\PayloadStaging"
Clean-Directory $stagingPath
New-Item -ItemType Directory -Path $stagingPath -Force | Out-Null

Copy-Item -Path "PickleRick.Installer\obj\ServicePublish\*" -Destination $stagingPath -Recurse -Force
Copy-Item -Path "PickleRick.Installer\obj\PlayerPublish\PickleRick.Player.exe" -Destination $stagingPath -Force
Copy-Item -Path "PickleRick.Installer\obj\PlayerPublish\PickleRick.Player.dll" -Destination $stagingPath -Force

$zipPath = "PickleRick.Installer\ServicePayload.zip"
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path "$stagingPath\*" -DestinationPath $zipPath -Force
Write-Host "      Done.`n" -ForegroundColor Green

Write-Host "[5/6] Building installer..." -ForegroundColor Yellow
$result = dotnet publish PickleRick.Installer\PickleRick.Installer.csproj -c Release 2>&1
if ($LASTEXITCODE -ne 0) { 
    Write-Host "ERROR: Installer build failed!" -ForegroundColor Red
    Write-Host $result
    exit $LASTEXITCODE 
}
Write-Host "      Done.`n" -ForegroundColor Green

Write-Host "[6/6] Verifying output..." -ForegroundColor Yellow
$installerPath = "PickleRick.Installer\bin\Release\net10.0\win-x64\publish\PickleRickInstaller.exe"
if (Test-Path $installerPath) {
    $fileSize = (Get-Item $installerPath).Length / 1MB
    Write-Host "      Done.`n" -ForegroundColor Green
    Write-Host "=== BUILD SUCCESSFUL ===`n" -ForegroundColor Green
    Write-Host "Installer created:" -ForegroundColor Cyan
    Write-Host "  Path: $installerPath" -ForegroundColor White
    Write-Host "  Size: $([math]::Round($fileSize, 2)) MB`n" -ForegroundColor White
} else {
    Write-Host "ERROR: Installer exe not found!" -ForegroundColor Red
    exit 1
}


