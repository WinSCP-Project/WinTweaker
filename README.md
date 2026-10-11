# WinTweaker

Desktop tweaker for Windows 7, 10, and 11. You pick the changes first. Disk writes start on Apply.

![System Tweaks](images/preview.png)

The yellow bar in the screenshot appears when the window is opened without administrator rights. The release build requests them on its own.

## What's inside

Four steps.

1. **Welcome.** Restore point and Explorer restart.
2. **Apps.** Remove preinstalled Store apps. On Windows 7 this page is empty.
3. **System Tweaks.** Three columns of cards, search, the default preset, the last selection, and clear. Hover a setting to see what it writes.
4. **Review.** The selected list, Apply, and a log.

Settings the running system does not have are greyed out. On Windows 10 that covers Copilot, Recall, Drag Tray, and the rest that exists only on Windows 11.

## Run

Administrator rights and .NET Framework 4.8. Windows 10 and 11 already include the runtime. Windows 7 needs it installed separately.

Download [WinTweaker-0.1.0.zip](https://github.com/WinSCP-Project/WinTweaker/releases/latest), unzip, and run `WinTweaker.exe`. `WinTweaker.exe.config` has to sit next to it.

## What Apply does

- Writes registry settings for privacy, the shell, updates, File Explorer, the Start menu, and the taskbar.
- Changes the start type of telemetry and AI services when that service exists on the machine.
- Removes the Store apps you checked.
- Can replace the pinned Start layout with an empty template.
- Creates a restore point before writing, when System Protection is on for the system drive.
- Restarts Explorer when that box is checked on the first page.

The undo snapshot is stored in `%LocalAppData%\WinTweaker\backups`. Undo from the window menu: **Undo last apply**. Removed apps stay removed.

## Build

```
dotnet build src/WinTweaker/WinTweaker.csproj -c Release
```

Output: `src/WinTweaker/bin/Release/WinTweaker.exe`.

## Empty Start layout

`src/WinTweaker/Assets/start2.bin` is a Windows 11 Start layout with no pinned apps. It comes from [Raphire/Win11Debloat](https://github.com/Raphire/Win11Debloat) (MIT). The license text sits beside it in `src/WinTweaker/Assets/start2-NOTICE.txt`.
