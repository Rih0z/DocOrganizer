@echo off
REM DocOrganizer V2.2 Windows Build Script
REM Generated for MCP execution on Windows environment

echo =====================================
echo DocOrganizer V2.2 Windows Build
echo =====================================

echo.
echo Step 1: Sync with Git
git pull origin main
if errorlevel 1 (
    echo ERROR: Failed to pull from Git
    pause
    exit /b 1
)

echo.
echo Step 2: Clean Solution
dotnet clean --configuration Release
if errorlevel 1 (
    echo ERROR: Failed to clean solution
    pause
    exit /b 1
)

echo.
echo Step 3: Restore Dependencies  
dotnet restore
if errorlevel 1 (
    echo ERROR: Failed to restore dependencies
    pause
    exit /b 1
)

echo.
echo Step 4: Build Solution
dotnet build --configuration Release
if errorlevel 1 (
    echo ERROR: Failed to build solution
    pause
    exit /b 1
)

echo.
echo Step 5: Run Tests
dotnet test --configuration Release --verbosity minimal
if errorlevel 1 (
    echo ERROR: Tests failed
    pause
    exit /b 1
)

echo.
echo Step 6: Publish Windows Executable
dotnet publish src/DocOrganizer.UI -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o release
if errorlevel 1 (
    echo ERROR: Failed to publish executable
    pause
    exit /b 1
)

echo.
echo =====================================
echo BUILD COMPLETED SUCCESSFULLY!
echo =====================================
echo.
echo Executable Location: release\DocOrganizer.UI.exe
echo File Size: 
for %%I in ("release\DocOrganizer.UI.exe") do echo %%~zI bytes

echo.
echo Next steps:
echo 1. Test the executable: release\DocOrganizer.UI.exe
echo 2. Verify UI functionality
echo 3. Test drag-and-drop operations
echo.

echo Press any key to continue...
pause >nul