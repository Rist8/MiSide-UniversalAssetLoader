@echo off
setlocal enabledelayedexpansion
dotnet build Plugin -c Release -o Compiled
if not exist ".\Compiled\Dependencies" mkdir ".\Compiled\Dependencies"


for /f "tokens=3" %%a in ('REG QUERY "HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam" /v InstallPath ^|findstr /ri "REG_SZ"') do set SteamPath=%%a

REM Define input file

set "inputFile=%SteamPath%\steamapps\libraryfolders.vdf"

REM Initialize variables for paths
set "path1="
set "path2="
set "nextIsPath="

REM Loop through each line in the file
for /f "usebackq tokens=1,* delims=  " %%A in ("%inputFile%") do (
    REM Trim the first token
    set "key=%%A"
    set "value=%%B"

    REM Remove quotes for key and value
    set "key=!key:"=!"
    set "value=!value:"=!"

    REM Detect the start of library entry "1"
    if "!key!"=="1" (
        set "nextIsPath=1"
    )

    REM Detect the start of library entry "2"
    if "!key!"=="2" (
        set "nextIsPath=2"
    )

    REM If the "path" key is found and a flag is set, extract the path
    if "!nextIsPath!" NEQ "" if "!key!"=="path" (
        set "path!nextIsPath!=!value!"
        set "nextIsPath="
    )
)

REM Display extracted paths
if defined path1 (
    echo Path 1: !path1!
) else (
    echo Path 1 not found.
)

if defined path2 (
    echo Path 2: !path2!
) else (
    echo Path 2 not found.
)

REM Check for "MiSide" folder under each path
if defined path1 (
    set "target1=!path1!\steamapps\common\MiSide"
    if exist "!target1!" (
        set "GameFolder=!target1!"
    echo Chose !target1! as game directory
    ) else (
        echo Folder does not exist: !target1!
    )
)

if defined path2 (
    set "target2=!path2!\steamapps\common\MiSide"
    if exist "!target2!" (
        set "GameFolder=!target2!"
    echo Chose !target2! as game directory
    ) else (
        echo Folder does not exist: !target2!
    )
)

copy ".\Dependencies\AssimpNet.dll" ".\Compiled\Dependencies"
copy ".\Dependencies\AssimpNet.pdb" ".\Compiled\Dependencies"
copy ".\Dependencies\Newtonsoft.Json.dll" ".\Compiled\Dependencies"
copy ".\Dependencies\Newtonsoft.Json.pdb" ".\Compiled\Dependencies"
copy ".\Dependencies\assimp.dll" ".\Compiled\Dependencies"
copy ".\Dependencies\K4os.Compression.LZ4.dll" ".\Compiled\Dependencies"
copy ".\Dependencies\K4os.Hash.xxHash.dll" ".\Compiled\Dependencies"
copy ".\Dependencies\K4os.Hash.xxHash.dll" ".\Compiled\Dependencies"
copy ".\Dependencies\System.IO.Pipelines.dll" ".\Compiled\Dependencies"


robocopy ".\Compiled" "%GameFolder%\BepInEx\plugins\UniversalAssetLoader" /E /np /nfl /njh /njs /ndl /nc /ns

if not exist %GameFolder%\assimp.dll (
    copy ".\Dependencies\assimp.dll" "%GameFolder%"
)

pause