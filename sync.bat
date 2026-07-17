@echo off
set source_dir="F:\Coding\Mods\pesky_flight_stuff"
set target_dir="D:\SteamLibrary\steamapps\common\RimWorld\Mods\pesky_flight_stuff"

echo Syncing from %source_dir% to %target_dir%
robocopy %source_dir% %target_dir% /S /E /DCOPY:DA /COPY:DAT /PURGE /MIR /XD Source .git bin obj /XF *.cs *.csproj *.sln .gitignore sync.bat sync.ps1 /R:1000000 /W:30
if %errorlevel% geq 8 (
    echo Robocopy failed with exit code %errorlevel%
    exit /b %errorlevel%
) else (
    echo Sync complete!
    exit /b 0
)
