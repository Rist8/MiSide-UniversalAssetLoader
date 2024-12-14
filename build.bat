dotnet build Plugin -c Release -o Compiled
@echo off

for /f "tokens=3" %%a in ('REG QUERY "HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam" /v InstallPath ^|findstr /ri "REG_SZ"') do set SteamPath=%%a
set "GameFolder=%SteamPath%\SteamApps\common\MiSide Demo"

set "pluginInfoFile=.\Plugin\PluginLoader.cs"

for /f "tokens=2 delims==" %%a in ('findstr /c:"public const string PLUGIN_GUID" "%pluginInfoFile%"') do (
  set "PluginName=%%a"
)

set "PluginName=%PluginName: =%"
set "PluginName=%PluginName:"=%"
set "PluginName=%PluginName:;=%"

robocopy ".\Compiled" "%GameFolder%\BepInEx\plugins\%PluginName%" Plugin.* /E /np /nfl /njh /njs /ndl /nc /ns

"%GameFolder%\MiSide.exe" 