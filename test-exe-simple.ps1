# DocOrganizer V2.2 Simple Test Script
# Article 13 compliant - Run from Explorer

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "DocOrganizer V2.2 Test" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan

# Check EXE existence
$exePath = "C:\Users\koki\ezark\standard-image\Standard-image\v2.2-doc-organizer\release\DocOrganizer.UI.exe"

if (Test-Path $exePath) {
    $exeInfo = Get-Item $exePath
    $sizeMB = [math]::Round($exeInfo.Length / 1MB, 1)
    $creationTime = $exeInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
    
    Write-Host "`nEXE File Confirmed:" -ForegroundColor Green
    Write-Host "   Path: $exePath" -ForegroundColor White
    Write-Host "   Size: ${sizeMB}MB" -ForegroundColor White
    Write-Host "   Updated: $creationTime" -ForegroundColor White
    
    # Quick process test
    Write-Host "`n[Running startup test...]" -ForegroundColor Yellow
    try {
        $proc = Start-Process -FilePath $exePath -PassThru -WindowStyle Normal
        Start-Sleep -Seconds 3
        
        if (-not $proc.HasExited) {
            Write-Host "Application started successfully (PID: $($proc.Id))" -ForegroundColor Green
            Write-Host "`n[Test Items]" -ForegroundColor Yellow
            Write-Host "1. Window is displayed" -ForegroundColor White
            Write-Host "2. Can drag & drop image files" -ForegroundColor White
            Write-Host "3. Can open PDF files" -ForegroundColor White
            
            # Terminate test process
            Stop-Process -Id $proc.Id -Force
            Write-Host "`nTest process terminated" -ForegroundColor Gray
        } else {
            Write-Host "Application exited immediately" -ForegroundColor Red
        }
    } catch {
        Write-Host "Startup error: $_" -ForegroundColor Red
    }
    
    Write-Host "`n=====================================" -ForegroundColor Cyan
    Write-Host "Recommendation: Start directly from Explorer" -ForegroundColor Yellow
    Write-Host "Path: $exePath" -ForegroundColor Cyan
    Write-Host "=====================================" -ForegroundColor Cyan
} else {
    Write-Host "`nEXE file not found" -ForegroundColor Red
    Write-Host "   Expected path: $exePath" -ForegroundColor Red
    Write-Host "`nPlease run build-latest.ps1 first" -ForegroundColor Yellow
}