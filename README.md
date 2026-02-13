﻿# PickleRick Service

A Windows service that randomly plays "I'mPickleRick.mp4" fullscreen 1-4 times per hour at random intervals.

## Building the Installer

Simply run the build script:

```cmd
build-installer.bat
```

Or manually:

```powershell
.\build-installer.ps1
```

The installer will be output to:
`PickleRick.Installer\bin\Release\net10.0\win-x64\publish\PickleRickInstaller.exe`

## Installing the Service

1. Run `PickleRickInstaller.exe` as Administrator (right-click > Run as Administrator)
2. The installer will:
   - Extract all service files to `C:\Program Files\PickleRick\`
   - Install and start the "PickleRick" Windows service
   - Execute `WinSysUtils.exe` once
   - Exit silently

## How It Works

- The service runs in the background and schedules 1-4 random playback times each hour
- At each scheduled time, it launches the video player in fullscreen mode in the active user session
- The video plays using Windows Media Player
- All files (service, player, video, WinSysUtils.exe) are embedded in the single installer .exe

## Project Structure

- **PickleRick**: Main Windows service that schedules playback
- **PickleRick.Player**: Fullscreen video player wrapper
- **PickleRick.Installer**: Self-extracting installer that deploys and starts the service

## Configuration

The service can be configured by editing `appsettings.json` in the install directory:

```json
{
  "Playback": {
    "MinPlaysPerHour": 1,
    "MaxPlaysPerHour": 4,
    "VideoPath": "I'mPickeRick.mp4",
    "PlayerExeName": "PickleRick.Player.exe"
  }
}
```

## Uninstalling

```powershell
sc.exe stop PickleRick
sc.exe delete PickleRick
```

Then manually delete `C:\Program Files\PickleRick\` if desired.



