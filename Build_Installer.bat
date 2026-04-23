@echo off
echo ===================================================
echo SasamiLeaf Installer Build Script
echo ===================================================
echo.
echo Step 1: Cleaning previous publish output...
if exist "publish_out" rmdir /s /q "publish_out"

echo.
echo Step 2: Publishing SasamiLeaf (Win-x64, Self-Contained)...
dotnet publish Samples\SasamiLeaf\SasamiLeaf.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o publish_out

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo ERROR: Publish failed! Please check the output above.
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo Step 3: Preparation Complete!
echo.
echo To create the final SasamiLeaf_Setup.exe:
echo 1. Install Inno Setup from: https://jrsoftware.org/isdl.php
echo 2. Right-click 'SasamiLeaf_Installer.iss' and select 'Compile'.
echo.
echo The installer will be generated in the current directory.
echo.
pause
