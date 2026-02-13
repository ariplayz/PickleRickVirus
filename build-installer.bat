@echo off
REM Build PickleRick Installer
cd /d "%~dp0"

echo Cleaning build artifacts...
dotnet clean PickleRick.csproj >nul 2>&1
dotnet clean PickleRick.Player\PickleRick.Player.csproj >nul 2>&1
dotnet clean PickleRick.Installer\PickleRick.Installer.csproj >nul 2>&1

if exist "obj" rmdir /s /q "obj"
if exist "bin" rmdir /s /q "bin"
if exist "PickleRick.Player\obj" rmdir /s /q "PickleRick.Player\obj"
if exist "PickleRick.Player\bin" rmdir /s /q "PickleRick.Player\bin"
if exist "PickleRick.Installer\obj" rmdir /s /q "PickleRick.Installer\obj"
if exist "PickleRick.Installer\bin" rmdir /s /q "PickleRick.Installer\bin"

echo Building PickleRick Service...
dotnet publish PickleRick.csproj -c Release -o "PickleRick.Installer\obj\ServicePublish"
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo Building PickleRick Player...
dotnet publish PickleRick.Player\PickleRick.Player.csproj -c Release -o "PickleRick.Installer\obj\PlayerPublish"
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo Creating payload staging directory...
if exist "PickleRick.Installer\obj\PayloadStaging" rmdir /s /q "PickleRick.Installer\obj\PayloadStaging"
mkdir "PickleRick.Installer\obj\PayloadStaging"

echo Copying service files...
xcopy "PickleRick.Installer\obj\ServicePublish\*" "PickleRick.Installer\obj\PayloadStaging\" /E /I /Y >nul

echo Copying player files...
copy "PickleRick.Installer\obj\PlayerPublish\PickleRick.Player.exe" "PickleRick.Installer\obj\PayloadStaging\" >nul
copy "PickleRick.Installer\obj\PlayerPublish\PickleRick.Player.dll" "PickleRick.Installer\obj\PayloadStaging\" >nul

echo Creating ServicePayload.zip...
powershell -NoProfile -Command "Compress-Archive -Path 'PickleRick.Installer\obj\PayloadStaging\*' -DestinationPath 'PickleRick.Installer\ServicePayload.zip' -Force"

echo Building installer...
dotnet publish PickleRick.Installer\PickleRick.Installer.csproj -c Release
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo.
echo Build complete!
echo Installer: PickleRick.Installer\bin\Release\net10.0\win-x64\publish\PickleRickInstaller.exe
pause

