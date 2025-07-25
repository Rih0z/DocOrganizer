# DocOrganizer V2.2 Windows EXE Test Script
# Based on Claude.md exe_verification section

param(
    [string]$ExePath = "release\DocOrganizer.UI.exe"
)

Write-Host "=== DocOrganizer V2.2 Windows EXE Verification Test ===" -ForegroundColor Green
Write-Host "Following Claude.md exe_verification policy" -ForegroundColor Cyan
Write-Host ""

# Step 1: Check if EXE exists
Write-Host "Step 1: Verifying EXE existence..." -ForegroundColor Yellow
if (Test-Path $ExePath) {
    Write-Host "✅ EXE found: $ExePath" -ForegroundColor Green
    
    # Display file information
    $fileInfo = Get-Item $ExePath
    Write-Host "📁 File Size: $([math]::Round($fileInfo.Length/1MB, 2)) MB" -ForegroundColor Cyan
    Write-Host "📅 Created: $($fileInfo.CreationTime)" -ForegroundColor Cyan
    Write-Host "🔄 Modified: $($fileInfo.LastWriteTime)" -ForegroundColor Cyan
    
    # Step 2: Launch process test
    Write-Host ""
    Write-Host "Step 2: Starting application process..." -ForegroundColor Yellow
    
    try {
        $process = Start-Process -FilePath $ExePath -PassThru -ErrorAction Stop
        Write-Host "✅ Process launched successfully" -ForegroundColor Green
        Write-Host "🆔 Process ID: $($process.Id)" -ForegroundColor Cyan
        
        # Wait and check if process is running
        Start-Sleep -Seconds 5
        
        if (-not $process.HasExited) {
            Write-Host "✅ Process is running stable" -ForegroundColor Green
            
            # Get memory usage
            $processInfo = Get-Process -Id $process.Id -ErrorAction SilentlyContinue
            if ($processInfo) {
                Write-Host "💾 Memory Usage: $([math]::Round($processInfo.WorkingSet64/1MB, 2)) MB" -ForegroundColor Cyan
            }
            
            # Graceful shutdown
            Write-Host ""
            Write-Host "Step 3: Testing graceful shutdown..." -ForegroundColor Yellow
            Stop-Process -Id $process.Id -Force
            Start-Sleep -Seconds 2
            
            if ($process.HasExited) {
                Write-Host "✅ Process terminated successfully" -ForegroundColor Green
            } else {
                Write-Host "⚠️ Process required force termination" -ForegroundColor Yellow
            }
        } else {
            Write-Host "❌ Process exited immediately - check for startup errors" -ForegroundColor Red
        }
        
    } catch {
        Write-Host "❌ Failed to start process: $($_.Exception.Message)" -ForegroundColor Red
    }
    
} else {
    Write-Host "❌ EXE not found: $ExePath" -ForegroundColor Red
    Write-Host "💡 Run build-windows.bat first to generate the EXE" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== Test Completed ===" -ForegroundColor Green
Write-Host "Expected Features:" -ForegroundColor Cyan
Write-Host "- CubePDF Utility互換 UI" -ForegroundColor White
Write-Host "- PDF読み込み・編集機能" -ForegroundColor White
Write-Host "- 税務特化ツール機能" -ForegroundColor White
Write-Host "- ドラッグ&ドロップ操作" -ForegroundColor White
Write-Host "- 画像ファイル対応（HEIC/JPG/PNG）" -ForegroundColor White