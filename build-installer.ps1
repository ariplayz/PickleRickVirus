#!/usr/bin/env pwsh
# Build script for PickleRick Installer

$ErrorActionPreference = "Stop"

Write-Host "Building PickleRick Service..." -ForegroundColor Cyan
dotnet publish PickleRick.csproj -c Release -o "PickleRick.Installer\obj\ServicePublish"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Building PickleRick Player..." -ForegroundColor Cyan
dotnet publish PickleRick.Player\PickleRick.Player.csproj -c Release -o "PickleRick.Installer\obj\PlayerPublish"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Creating payload staging directory..." -ForegroundColor Cyan
$stagingPath = "PickleRick.Installer\obj\PayloadStaging"
if (Test-Path $stagingPath) {
    Remove-Item -Path $stagingPath -Recurse -Force
}
New-Item -ItemType Directory -Path $stagingPath -Force | Out-Null

Write-Host "Copying service files..." -ForegroundColor Cyan
Copy-Item -Path "PickleRick.Installer\obj\ServicePublish\*" -Destination $stagingPath -Recurse -Force

Write-Host "Copying player files..." -ForegroundColor Cyan
Copy-Item -Path "PickleRick.Installer\obj\PlayerPublish\PickleRick.Player.exe" -Destination $stagingPath -Force
Copy-Item -Path "PickleRick.Installer\obj\PlayerPublish\PickleRick.Player.dll" -Destination $stagingPath -Force

Write-Host "Creating ServicePayload.zip..." -ForegroundColor Cyan
$zipPath = "PickleRick.Installer\ServicePayload.zip"
if (Test-Path $zipPath) {
    Remove-Item -Path $zipPath -Force
}
Compress-Archive -Path "$stagingPath\*" -DestinationPath $zipPath -Force

Write-Host "Building installer..." -ForegroundColor Cyan
dotnet publish PickleRick.Installer\PickleRick.Installer.csproj -c Release
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "`nBuild complete!" -ForegroundColor Green
Write-Host "Installer: PickleRick.Installer\bin\Release\net10.0\win-x64\publish\PickleRickInstaller.exe" -ForegroundColor Yellow

