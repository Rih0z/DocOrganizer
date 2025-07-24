# Manual Drag and Drop Test for DocOrganizer V2.2
# Test orientation correction functionality

Write-Host "=== Manual Drag and Drop Test ===" -ForegroundColor Green

# Check if application is running
$process = Get-Process -Name "DocOrganizer.UI" -ErrorAction SilentlyContinue
if (-not $process) {
    Write-Host "Starting DocOrganizer..." -ForegroundColor Yellow
    $exePath = "C:\Users\koki\ezark\standard-image\Standard-image\v2.2-doc-organizer\release\DocOrganizer.UI.exe"
    $process = Start-Process -FilePath $exePath -PassThru -WindowStyle Normal
    Start-Sleep -Seconds 3
}

Write-Host "DocOrganizer is running (PID: $($process.Id))" -ForegroundColor Green

# Open both folders
$samplePath = "C:\Users\koki\ezark\standard-image\Standard-image\v2.2-doc-organizer\sample"
Write-Host "Opening sample folder: $samplePath" -ForegroundColor Yellow
Start-Process explorer $samplePath

# Instructions
Write-Host "`n=== TEST INSTRUCTIONS ===" -ForegroundColor Cyan
Write-Host "1. Drag '1- tate-png.png' (vertical image) to DocOrganizer" -ForegroundColor White
Write-Host "2. Check orientation correction in preview" -ForegroundColor White
Write-Host "3. Look for rotation in thumbnail" -ForegroundColor White
Write-Host ""
Write-Host "Expected: Vertical image should be rotated 270 degrees (right 90 degrees)" -ForegroundColor Green
Write-Host ""
Write-Host "Press any key to check logs after testing..."
$null = $Host.UI.RawUI.ReadKey()

# Check logs
$logPath = "C:\Users\koki\ezark\standard-image\Standard-image\v2.2-doc-organizer\release\logs"
$currentDate = Get-Date -Format "yyyyMMdd"
$logFile = Get-ChildItem -Path $logPath -Filter "doc-organizer-${currentDate}*.txt" | Sort-Object LastWriteTime -Descending | Select-Object -First 1

if ($logFile) {
    Write-Host "`nChecking log: $($logFile.Name)" -ForegroundColor Yellow
    $recentLogs = Get-Content $logFile.FullName -Tail 20
    
    # Look for orientation logs
    $orientationLogs = $recentLogs | Where-Object { 
        $_ -match "Auto-corrected" -or 
        $_ -match "rotation" -or 
        $_ -match "aspect ratio" -or
        $_ -match "EXIF"
    }
    
    if ($orientationLogs) {
        Write-Host "`nOrientation logs found:" -ForegroundColor Green
        foreach ($log in $orientationLogs) {
            Write-Host "  $log" -ForegroundColor White
        }
    } else {
        Write-Host "`nNo orientation correction logs found in recent entries" -ForegroundColor Yellow
        Write-Host "Recent log entries:" -ForegroundColor Gray
        foreach ($log in $recentLogs[-5..-1]) {
            Write-Host "  $log" -ForegroundColor Gray
        }
    }
} else {
    Write-Host "No log file found" -ForegroundColor Red
}

Write-Host "`nTest completed at $(Get-Date)" -ForegroundColor Green