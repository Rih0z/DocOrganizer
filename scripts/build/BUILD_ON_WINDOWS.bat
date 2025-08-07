@echo off
echo ===================================
echo DocOrganizer V2.2 Windows Build
echo ===================================

cd /d C:\builds\Standard-image-repo\v2.2-doc-organizer

echo Step 1: Git Sync
git pull origin main

echo Step 2: Clean Build
dotnet clean
dotnet restore

echo Step 3: Release Build
dotnet build --configuration Release

echo Step 4: Publish EXE
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o release

echo Step 5: Verify EXE
if exist "release\DocOrganizer.UI.exe" (
    echo ✅ SUCCESS: EXE generated
    dir release\DocOrganizer.UI.exe
) else (
    echo ❌ FAILED: EXE not found
)

echo Step 6: Test Launch
start /wait release\DocOrganizer.UI.exe

pause