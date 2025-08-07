# DocOrganizer V2.2 Build Script
# Article 12 compliant - Complete build to EXE generation

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "DocOrganizer V2.2 Build Starting" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan

# 1. Git sync
Write-Host "`n[1/6] Git sync..." -ForegroundColor Yellow
Set-Location "C:\Users\koki\ezark\standard-image\Standard-image\v2.2-doc-organizer"
git pull origin main
if ($LASTEXITCODE -ne 0) {
    Write-Host "Git sync failed" -ForegroundColor Red
    exit 1
}

# 2. Clean build
Write-Host "`n[2/6] Clean build preparation..." -ForegroundColor Yellow
dotnet clean
if ($LASTEXITCODE -ne 0) {
    Write-Host "Clean failed" -ForegroundColor Red
    exit 1
}

# 3. Restore packages
Write-Host "`n[3/6] Restoring packages..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "Package restore failed" -ForegroundColor Red
    exit 1
}

# 4. Release build
Write-Host "`n[4/6] Building Release..." -ForegroundColor Yellow
dotnet build --configuration Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed" -ForegroundColor Red
    exit 1
}

# 5. Backup existing EXE
Write-Host "`n[5/6] Backing up existing EXE..." -ForegroundColor Yellow
if (Test-Path "release\DocOrganizer.UI.exe") {
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    Copy-Item "release\DocOrganizer.UI.exe" "release\DocOrganizer.UI.exe.$timestamp.bak"
    Write-Host "Backup created: DocOrganizer.UI.exe.$timestamp.bak" -ForegroundColor Green
}

# 6. Generate EXE
Write-Host "`n[6/6] Generating self-contained EXE..." -ForegroundColor Yellow
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o release
if ($LASTEXITCODE -ne 0) {
    Write-Host "EXE generation failed" -ForegroundColor Red
    exit 1
}

# Result confirmation
Write-Host "`n=====================================" -ForegroundColor Green
Write-Host "Build Complete!" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Green

# EXE information display
if (Test-Path "release\DocOrganizer.UI.exe") {
    $exeInfo = Get-Item "release\DocOrganizer.UI.exe"
    $sizeMB = [math]::Round($exeInfo.Length / 1MB, 1)
    $creationTime = $exeInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
    $fullPath = $exeInfo.FullName
    
    Write-Host "`nDocOrganizer V2.2 Complete:" -ForegroundColor Green
    Write-Host "   Full path: $fullPath" -ForegroundColor White
    Write-Host "   Size: ${sizeMB}MB" -ForegroundColor White
    Write-Host "   Created: $creationTime" -ForegroundColor White
    
    # Article 13 compliant startup instructions
    Write-Host "`n[How to Start] (Article 13 compliant)" -ForegroundColor Yellow
    Write-Host "Open the following path in Explorer and" -ForegroundColor White
    Write-Host "double-click DocOrganizer.UI.exe:" -ForegroundColor White
    Write-Host "$fullPath" -ForegroundColor Cyan
    Write-Host "`nDO NOT run as Administrator!" -ForegroundColor Red
    Write-Host "   Drag & Drop will be disabled." -ForegroundColor Red
} else {
    Write-Host "`nError: EXE file was not generated" -ForegroundColor Red
    exit 1
}