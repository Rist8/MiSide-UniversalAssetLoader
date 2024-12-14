dotnet build Plugin -c Release -o Compiled
@echo off

set "GameFolder=E:\Program Files (x86)\Steam\SteamApps\common\MiSide Demo"

set "pluginInfoFile=.\Plugin\PluginLoader.cs"

for /f "tokens=2 delims==" %%a in ('findstr /c:"public const string PLUGIN_GUID" "%pluginInfoFile%"') do (
  set "PluginName=%%a"
)

set "PluginName=%PluginName: =%"
set "PluginName=%PluginName:"=%"
set "PluginName=%PluginName:;=%"

copy ".\Dependencies\AssimpNet.dll" ".\Compiled"
copy ".\Dependencies\AssimpNet.pdb" ".\Compiled"

set readme_file=".\Compiled\README.txt"
del %readme_file%
echo WARNING: assimp.dll in game folder right beside MiSide.exe is required for mod to work. You can find it in 'Dependencies/assimp.dll'. >> %readme_file%
echo If you want to load specific assets, edit the 'assets_config.txt' file. >> %readme_file%
echo. - .png, .jpg, .jpeg as texture files >> %readme_file%
echo. - .fbx as mesh files >> %readme_file%
echo. - .ogg as audio files >> %readme_file%
echo. - .mp4 as video files >> %readme_file%


robocopy "./Assets" "./Compiled/Assets" /E /np /nfl /njh /njs /ndl /nc /ns
robocopy ".\Compiled" "%GameFolder%\BepInEx\plugins\%PluginName%" /E /np /nfl /njh /njs /ndl /nc /ns

if not exist %GameFolder%\assimp.dll (
    copy ".\Dependencies\assimp.dll" "%GameFolder%"
)

"%GameFolder%\MiSide.exe" 