# Quick Start Guide

## To Build the Installer

**Option 1 - Double-click:**
```
build-installer.bat
```

**Option 2 - Command line:**
```cmd
cd "C:\Users\{{Your User}}\RiderProjects\PickleRick"
build-installer.bat
```

**Option 3 - PowerShell:**
```powershell
.\build-installer.ps1
```

The installer will be created at:
```
PickleRick.Installer\bin\Release\net10.0\win-x64\publish\PickleRickInstaller.exe
```

## To Install the Service

1. Navigate to the publish folder
2. Right-click `PickleRickInstaller.exe`
3. Select "Run as Administrator"
4. The service will install and start automatically (no UI shown)

## To Verify It's Running

Open PowerShell as Administrator:

```powershell
# Check service status
sc.exe query PickleRick

# Check service logs
Get-EventLog -LogName Application -Source ".NET Runtime" -Newest 20 | Where-Object {$_.Message -like "*PickleRick*"}
```

## To Uninstall

Open PowerShell as Administrator:

```powershell
sc.exe stop PickleRick
sc.exe delete PickleRick
Remove-Item "C:\Program Files\PickleRick" -Recurse -Force
```

## Troubleshooting

**Service won't start:**
- Check Event Viewer > Windows Logs > Application
- Look for errors from ".NET Runtime" or "PickleRick"

**Video doesn't play:**
- Ensure a user is logged in to the console
- Check that I'mPickeRick.mp4 exists in `C:\Program Files\PickleRick\`
- Check that Windows Media Player is installed

**Need to change schedule:**
- Edit `C:\Program Files\PickleRick\appsettings.json`
- Change `MinPlaysPerHour` or `MaxPlaysPerHour`
- Restart service: `sc.exe stop PickleRick; sc.exe start PickleRick`

## What Happens

- Service schedules 1-4 random times each hour
- At each scheduled time, the video plays fullscreen
- Video uses Windows Media Player
- Plays in the active user's session (not as SYSTEM)
- Logs all activities to Application Event Log

