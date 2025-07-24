# DocOrganizer V2.2 Drag & Drop Test Script
# Article 13 Compliance: Normal privilege execution from Explorer

Write-Host "=== DocOrganizer V2.2 Drag & Drop Test ===" -ForegroundColor Green
Write-Host "Article 13 Compliance: Normal privilege application startup" -ForegroundColor Yellow

# Set working directory
Set-Location "C:\Users\koki\ezark\standard-image\Standard-image\v2.2-doc-organizer\release"

$exePath = ".\DocOrganizer.UI.exe"

if (Test-Path $exePath) {
    $fileInfo = Get-Item $exePath
    Write-Host ""
    Write-Host "EXE File Information:" -ForegroundColor Cyan
    Write-Host "   Path: $($fileInfo.FullName)" -ForegroundColor White
    Write-Host "   Size: $([math]::Round($fileInfo.Length / 1MB, 1)) MB" -ForegroundColor White
    Write-Host "   Created: $($fileInfo.CreationTime)" -ForegroundColor White
    Write-Host "   Modified: $($fileInfo.LastWriteTime)" -ForegroundColor White
    
    # Check test files
    Write-Host ""
    Write-Host "Test Files Check:" -ForegroundColor Cyan
    
    $pngFile = "PNG\screenshot*.png"
    $heicFile = "HEIC\IMG_5331.HEIC"
    
    $pngFiles = Get-ChildItem -Path "PNG" -Name "*.png" | Select-Object -First 1
    if ($pngFiles) {
        $pngPath = "PNG\$pngFiles"
        if (Test-Path $pngPath) {
            $info = Get-Item $pngPath
            Write-Host "   PNG File: $pngPath ($([Math]::Round($info.Length / 1KB, 2)) KB)" -ForegroundColor Green
        }
    }
    
    if (Test-Path $heicFile) {
        $info = Get-Item $heicFile
        Write-Host "   HEIC File: $heicFile ($([Math]::Round($info.Length / 1KB, 2)) KB)" -ForegroundColor Green
    } else {
        Write-Host "   HEIC File: Not found" -ForegroundColor Red
    }
    
    Write-Host ""
    Write-Host "Application Startup (Article 13 Compliance - Normal Privilege):" -ForegroundColor Cyan
    Write-Host "   Important: Administrator privilege disables drag and drop" -ForegroundColor Yellow
    
    try {
        # Start process with normal privilege (WindowStyle Normal for visibility)
        $proc = Start-Process -FilePath $exePath -PassThru -WindowStyle Normal
        Write-Host "   Process started successfully - PID: $($proc.Id)" -ForegroundColor Green
        
        # Wait 5 seconds to check process status
        Start-Sleep -Seconds 5
        
        if (-not $proc.HasExited) {
            Write-Host "   Application is running normally" -ForegroundColor Green
            
            Write-Host ""
            Write-Host "Manual Drag and Drop Test Instructions:" -ForegroundColor Yellow
            Write-Host "=========================================" -ForegroundColor Yellow
            
            Write-Host ""
            Write-Host "Test 1 - PNG File:" -ForegroundColor Magenta
            Write-Host "   1. Open Explorer folder: PNG\" -ForegroundColor White
            if ($pngFiles) {
                Write-Host "   2. Drag file: $pngFiles" -ForegroundColor White
            } else {
                Write-Host "   2. Drag any PNG file" -ForegroundColor White
            }
            Write-Host "   3. Drop on DocOrganizer window" -ForegroundColor White
            Write-Host "   Success indicators:" -ForegroundColor Green
            Write-Host "      - Mouse cursor changes to copy icon (+)" -ForegroundColor Green
            Write-Host "      - Drop overlay appears in blue" -ForegroundColor Green
            Write-Host "      - Processing message displays" -ForegroundColor Green
            Write-Host "      - Page thumbnail generates" -ForegroundColor Green
            Write-Host "   Failure indicators:" -ForegroundColor Red
            Write-Host "      - Mouse cursor remains as prohibition mark" -ForegroundColor Red
            Write-Host "      - No response on application" -ForegroundColor Red
            
            Write-Host ""
            Write-Host "Test 2 - HEIC File:" -ForegroundColor Magenta
            Write-Host "   1. Open Explorer folder: HEIC\" -ForegroundColor White
            Write-Host "   2. Drag file: IMG_5331.HEIC" -ForegroundColor White
            Write-Host "   3. Drop on DocOrganizer window" -ForegroundColor White
            Write-Host "   Check for errors:" -ForegroundColor Yellow
            Write-Host "      - Record exact error message" -ForegroundColor Yellow
            Write-Host "      - Check if 'attempt to access a missing method' appears" -ForegroundColor Yellow
            Write-Host "      - Identify which processing stage fails" -ForegroundColor Yellow
            
            Write-Host ""
            Write-Host "PDF Conversion Test (if successful):" -ForegroundColor Magenta
            Write-Host "   1. File -> Save As for PDF output" -ForegroundColor White
            Write-Host "   2. Check output destination and file size" -ForegroundColor White
            Write-Host "   3. Record conversion time" -ForegroundColor White
            
            Write-Host ""
            Write-Host "=========================================" -ForegroundColor Yellow
            Write-Host "Press Enter after completing manual tests..." -ForegroundColor Cyan
            Read-Host
            
            # Terminate process
            if (-not $proc.HasExited) {
                Write-Host ""
                Write-Host "Terminating process..." -ForegroundColor Cyan
                Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
                Write-Host "   Process terminated successfully" -ForegroundColor Green
            }
        } else {
            Write-Host "   Process exited unexpectedly" -ForegroundColor Red
            Write-Host "   Exit code: $($proc.ExitCode)" -ForegroundColor Red
            
            # Check log file
            $logPath = "logs\doc-organizer-$(Get-Date -Format 'yyyyMMdd').txt"
            if (Test-Path $logPath) {
                Write-Host ""
                Write-Host "Latest Log File Content (last 20 lines):" -ForegroundColor Yellow
                Get-Content $logPath -Tail 20 | ForEach-Object { Write-Host "   $_" -ForegroundColor White }
            }
        }
    } catch {
        Write-Host "   Application startup error: $($_.Exception.Message)" -ForegroundColor Red
    }
} else {
    Write-Host "EXE file not found: $exePath" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== Test Execution Complete ===" -ForegroundColor Green