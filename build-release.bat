@echo off
setlocal

if "%~1"=="" (
    echo.
    echo [ERROR] Version argument is missing!
    echo.
    echo Usage:
    echo   build-release.bat [VERSION]
    echo.
    echo Example:
    echo   build-release.bat 1.5.0
    echo.
    exit /b 1
)

set VERSION=%~1

echo [1/3] Publishing TrayShot v%VERSION% (Release, win-x64)...
dotnet publish src\TrayShot\TrayShot.csproj -c Release -r win-x64 --self-contained true /p:Version=%VERSION% -o .\publish
if errorlevel 1 (
 echo [ERROR] Dotnet publish failed!
 exit /b 1
)

echo [2/3] Packing with Velopack...
vpk pack -u TrayShot -v %VERSION% -p .\publish -e TrayShot.exe --icon src\TrayShot\assets\app.ico -o .\Releases
if errorlevel 1 (
 echo [ERROR] Velopack packaging failed!
 exit /b 1
)

echo [3/3] Creating versioned copy of setup and portable files...
if exist .\Releases\TrayShot-win-Setup.exe (
 copy /y .\Releases\TrayShot-win-Setup.exe .\Releases\TrayShot-%VERSION%-win-Setup.exe > nul
)
if exist .\Releases\TrayShot-win-Portable.zip (
 copy /y .\Releases\TrayShot-win-Portable.zip .\Releases\TrayShot-%VERSION%-win-Portable.zip > nul
)

echo.
echo ========================================================
echo TrayShot v%VERSION% Release build completed!
echo Output folder: .\Releases
echo ========================================================
endlocal
