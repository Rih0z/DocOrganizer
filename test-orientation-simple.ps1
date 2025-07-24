# DocOrganizer V2.2 Orientation Correction Test
# Simple Test Script - 2025-07-24

Write-Host "=== DocOrganizer V2.2 Orientation Test ===" -ForegroundColor Green

# Paths
$exePath = "C:\Users\koki\ezark\standard-image\Standard-image\v2.2-doc-organizer\release\DocOrganizer.UI.exe"
$samplePath = "C:\Users\koki\ezark\standard-image\Standard-image\v2.2-doc-organizer\sample"
$logPath = "C:\Users\koki\ezark\standard-image\Standard-image\v2.2-doc-organizer\release\logs"

# 1. Check EXE file
Write-Host "1. Checking EXE file..." -ForegroundColor Yellow
if (Test-Path $exePath) {
    $exeInfo = Get-Item $exePath
    $sizeInMB = [math]::Round($exeInfo.Length / 1MB, 1)
    Write-Host "EXE Found: ${sizeInMB}MB - $($exeInfo.LastWriteTime)" -ForegroundColor Green
} else {
    Write-Host "EXE not found: $exePath" -ForegroundColor Red
    exit 1
}

# 2. Check test images
Write-Host "`n2. Checking test images..." -ForegroundColor Yellow
$testImages = @(
    "1- tate-png.png",      # Vertical image - main test
    "1- hidari-ping.png",   # Left-oriented
    "1- migi-png.png",      # Right-oriented
    "JPG/IMG_7347.JPG",     # JPEG with potential EXIF
    "HEIC/IMG_5331.HEIC"    # HEIC with potential EXIF
)

foreach ($imageName in $testImages) {
    $fullPath = Join-Path $samplePath $imageName
    if (Test-Path $fullPath) {
        $imageInfo = Get-Item $fullPath
        $sizeInKB = [math]::Round($imageInfo.Length / 1KB, 1)
        Write-Host "Found: $imageName (${sizeInKB}KB)" -ForegroundColor Green
    } else {
        Write-Host "Missing: $imageName" -ForegroundColor Yellow
    }
}

# 3. Start application
Write-Host "`n3. Starting DocOrganizer..." -ForegroundColor Yellow
try {
    $existingProcess = Get-Process -Name "DocOrganizer.UI" -ErrorAction SilentlyContinue
    if ($existingProcess) {
        Write-Host "Stopping existing process: PID $($existingProcess.Id)"
        $existingProcess | Stop-Process -Force
        Start-Sleep -Seconds 2
    }
    
    $process = Start-Process -FilePath $exePath -PassThru -WindowStyle Normal
    Write-Host "Application started: PID $($process.Id)" -ForegroundColor Green
    Start-Sleep -Seconds 3
} catch {
    Write-Host "Failed to start application: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# 4. Open sample folder
Write-Host "`n4. Opening sample folder..."
Start-Process explorer $samplePath

# 5. Check latest log file
Write-Host "`n5. Monitoring logs..." -ForegroundColor Yellow
$currentDate = Get-Date -Format "yyyyMMdd"
$logFiles = Get-ChildItem -Path $logPath -Filter "doc-organizer-${currentDate}*.txt" -ErrorAction SilentlyContinue
if ($logFiles) {
    $latestLogFile = $logFiles | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    Write-Host "Latest log file: $($latestLogFile.Name)" -ForegroundColor Green
    $initialLogSize = $latestLogFile.Length
} else {
    Write-Host "No log file found for today" -ForegroundColor Yellow
    $latestLogFile = $null
}

# 6. Instructions
Write-Host "`n=== MANUAL TEST INSTRUCTIONS ===" -ForegroundColor Cyan
Write-Host "Please perform the following test steps:" -ForegroundColor White
Write-Host ""
Write-Host "1. Drag and drop '1- tate-png.png' into DocOrganizer" -ForegroundColor Yellow
Write-Host "2. Check if thumbnail appears in left panel" -ForegroundColor Yellow
Write-Host "3. Verify image orientation is corrected in preview" -ForegroundColor Yellow
Write-Host "4. Test other images (JPG, HEIC) similarly" -ForegroundColor Yellow
Write-Host ""
Write-Host "Expected behavior:" -ForegroundColor Green
Write-Host "- Vertical images should be auto-rotated 270 degrees (right 90 degrees)" -ForegroundColor Gray
Write-Host "- EXIF orientation data should be applied correctly" -ForegroundColor Gray
Write-Host "- Log should show 'Auto-corrected image orientation' messages" -ForegroundColor Gray

# 7. Wait and monitor
Write-Host "`n=== Waiting 30 seconds for testing ===" -ForegroundColor Yellow
for ($i = 30; $i -gt 0; $i--) {
    Write-Progress -Activity "Waiting for drag and drop test" -Status "Time remaining: $i seconds" -PercentComplete ((30-$i)/30*100)
    Start-Sleep -Seconds 1
}
Write-Progress -Activity "Waiting for drag and drop test" -Completed

# 8. Check logs
Write-Host "`n8. Analyzing logs..." -ForegroundColor Yellow
if ($latestLogFile) {
    $latestLogFile.Refresh()
    if ($latestLogFile.Length -gt $initialLogSize) {
        Write-Host "Log file updated - reading recent entries..." -ForegroundColor Green
        
        $logContent = Get-Content $latestLogFile.FullName -Tail 50
        
        # Look for orientation correction logs
        $orientationLogs = $logContent | Where-Object { 
            $_ -match "Auto-corrected image orientation" -or 
            $_ -match "rotation.*:" -or 
            $_ -match "aspect ratio" -or
            $_ -match "EXIF rotation"
        }
        
        if ($orientationLogs) {
            Write-Host "`nOrientation correction logs found:" -ForegroundColor Green
            foreach ($log in $orientationLogs) {
                Write-Host "  $log" -ForegroundColor White
            }
        } else {
            Write-Host "No orientation correction logs found" -ForegroundColor Yellow
        }
        
        # Check for errors
        $errorLogs = $logContent | Where-Object { $_ -match "\[ERR\]" -or $_ -match "ERROR" -or $_ -match "Exception" }
        if ($errorLogs) {
            Write-Host "`nErrors found:" -ForegroundColor Red
            foreach ($error in $errorLogs) {
                Write-Host "  $error" -ForegroundColor Red
            }
        } else {
            Write-Host "No errors detected" -ForegroundColor Green
        }
    } else {
        Write-Host "Log file not updated - no drag and drop activity detected" -ForegroundColor Yellow
    }
} else {
    Write-Host "No log file available for monitoring" -ForegroundColor Yellow
}

Write-Host "`n=== Test Complete ===" -ForegroundColor Green
Write-Host "Timestamp: $(Get-Date)" -ForegroundColor Gray