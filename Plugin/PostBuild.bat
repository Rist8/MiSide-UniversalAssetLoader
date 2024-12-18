echo off
setlocal enabledelayedexpansion

for /f "tokens=3*" %%a in ('REG QUERY "HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam" /v InstallPath ^| findstr /ri "REG_SZ"') do set "SteamPath=%%a"

set "inputFile=%SteamPath%\steamapps\libraryfolders.vdf"

set "GameFolder="
set "PluginTargetFolder="
set /a index=0

for /f "usebackq tokens=1,* delims=	 " %%A in ("%inputFile%") do (
    set "key=%%A"
    set "value=%%B"

    set "key=!key:"=!"
    set "value=!value:"=!"

    if "!key!"=="path" (
        set /a index+=1
        set "path!index!=!value!"
    )
)

for /L %%i in (1,1,!index!) do (
    set "currentPath=!path%%i!\steamapps\common\MiSide"
    if exist "!currentPath!" (
        echo MiSide finded in: !currentPath!
        set "GameFolder=!currentPath!"
    )
)

if not defined GameFolder (
    echo Can't find MiSide game.
    exit /b 1
)

set "UALPath=%GameFolder%\BepInEx\plugins\UniversalAssetLoader"
if exist "%UALPath%" (
    set "PluginTargetFolder=%UALPath%"
    echo Finded UniversalAssetLoader in: %UALPath%
) else (
    echo UniversalAssetLoader not finded.
    exit /b 1
)

set "sourceFile=%~1"
copy "%sourceFile%" "%PluginTargetFolder%\"

start "" "%GameFolder%\MiSideFull.exe"

endlocal
pause
