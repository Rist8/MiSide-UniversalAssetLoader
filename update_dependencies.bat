@echo off
setlocal enabledelayedexpansion

set "dllFolder=\Dependencies"
set "csProjFile=.\Plugin\Plugin.csproj"

IF EXIST "%csProjFile%.new" del "%csProjFile%.new"
for /f "usebackq delims=" %%a in ("!csprojFile!") do (
    echo "%%a" | findstr /i /c:"</Project>" /c:"</ItemGroup>" > nul
    if errorlevel 1 (
        echo %%a >> "%csProjFile%.new"
    )
)

for %%i in ("%cd%\%dllFolder%\*.dll") do (
    set "dependencyName=%%~ni"
	
	type !csProjFile! | findstr /i /c:^"\^"!dependencyName!\^"^" > nul
    if errorlevel 1 (
        echo.    ^<Reference Include^="!dependencyName!^"^> >> "%csProjFile%.new"
        echo.      ^<HintPath^>..%dllFolder%\!dependencyName!.dll^</HintPath^> >> "%csProjFile%.new"
        echo.      ^<Private^>false^</Private^> >> "%csProjFile%.new"
        echo.    ^</Reference^> >> "%csProjFile%.new"
    )
)

echo.  ^</ItemGroup^> >> "%csProjFile%.new"
echo ^</Project^> >> "%csProjFile%.new"
move /y "%csProjFile%.new" "%csProjFile%"

echo Done.