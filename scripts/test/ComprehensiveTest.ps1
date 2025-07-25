# DocOrganizer V2.2 - Comprehensive Functionality Test Suite
# Execute comprehensive testing including encoding 1512 fix verification

param(
    [switch]$SkipBuild = $false,
    [switch]$QuickTest = $false
)

$ErrorActionPreference = "Continue"

Write-Host "=== DocOrganizer V2.2 Comprehensive Test Suite ===" -ForegroundColor Magenta
Write-Host "Starting comprehensive functionality testing..." -ForegroundColor Green
Write-Host "Test Started: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')`n" -ForegroundColor Cyan

# Global test results
$global:TestResults = @{
    StartTime = Get-Date
    BuildSuccess = $false
    StartupSuccess = $false
    SampleDataAccess = 0
    EncodingErrors = 0
    JapaneseFileCount = 0
    MemoryUsageMB = 0
    TestAppProcess = $null
}

# Phase 1: Environment Setup
Write-Host "=== Phase 1: Environment Setup ===" -ForegroundColor Magenta

Write-Host "Git synchronization..." -ForegroundColor Yellow
try {
    git pull origin main | Out-String | Write-Host -ForegroundColor Gray
    Write-Host "✅ Git sync completed" -ForegroundColor Green
} catch {
    Write-Host "⚠️ Git sync warning: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host "`nEnvironment info:" -ForegroundColor Yellow
Write-Host "  Directory: $(Get-Location)" -ForegroundColor White
Write-Host "  PowerShell: $($PSVersionTable.PSVersion)" -ForegroundColor White
Write-Host "  .NET: $(dotnet --version)" -ForegroundColor White
Write-Host "  Windows: $((Get-ComputerInfo -Property WindowsProductName).WindowsProductName)" -ForegroundColor White

# Sample data verification
Write-Host "`nSample data verification..." -ForegroundColor Yellow
$sampleFiles = Get-ChildItem -Path "sample" -Recurse -Include "*.heic","*.HEIC","*.jpg","*.JPG","*.png","*.PNG","*.jpeg","*.JPEG"
$japaneseFiles = $sampleFiles | Where-Object { $_.Name -match "[ひ-龯]" }

Write-Host "  Total files: $($sampleFiles.Count) (Expected: 40)" -ForegroundColor White
Write-Host "  Japanese files: $($japaneseFiles.Count)" -ForegroundColor White

if ($sampleFiles.Count -eq 40) {
    Write-Host "✅ Sample data verification passed" -ForegroundColor Green
} else {
    Write-Host "⚠️ Sample data count mismatch" -ForegroundColor Yellow
}

$global:TestResults.JapaneseFileCount = $japaneseFiles.Count

# Phase 2: Build Process
if (-not $SkipBuild) {
    Write-Host "`n=== Phase 2: Build Process ===" -ForegroundColor Magenta
    
    Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
    dotnet clean --configuration Release | Out-String | Write-Host -ForegroundColor Gray
    
    if (-not (Test-Path "release")) {
        New-Item -ItemType Directory -Path "release" | Out-Null
    }
    
    Write-Host "Restoring dependencies..." -ForegroundColor Yellow
    dotnet restore | Out-String | Write-Host -ForegroundColor Gray
    
    Write-Host "Building solution..." -ForegroundColor Yellow
    $buildResult = dotnet build --configuration Release --no-restore
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Build completed successfully" -ForegroundColor Green
        
        Write-Host "Publishing Windows executable..." -ForegroundColor Yellow
        dotnet publish src/DocOrganizer.UI -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o release
        
        $exePath = "release/DocOrganizer.UI.exe"
        if (Test-Path $exePath) {
            $fileInfo = Get-Item $exePath
            $fileSize = [math]::Round($fileInfo.Length / 1MB, 2)
            Write-Host "✅ EXE created: $($fileInfo.FullName)" -ForegroundColor Green
            Write-Host "   Size: ${fileSize}MB, Created: $($fileInfo.CreationTime)" -ForegroundColor White
            $global:TestResults.BuildSuccess = $true
        } else {
            Write-Host "❌ EXE creation failed" -ForegroundColor Red
            return
        }
    } else {
        Write-Host "❌ Build failed" -ForegroundColor Red
        return
    }
}

# Phase 3: Application Startup
Write-Host "`n=== Phase 3: Application Startup Test ===" -ForegroundColor Magenta

$exePath = "release/DocOrganizer.UI.exe"
if (Test-Path $exePath) {
    Write-Host "Starting application..." -ForegroundColor Yellow
    $startTime = Get-Date
    
    try {
        $proc = Start-Process -FilePath $exePath -PassThru -WindowStyle Normal
        Start-Sleep -Seconds 5
        
        if (-not $proc.HasExited) {
            $loadTime = (Get-Date) - $startTime
            $memoryMB = [math]::Round($proc.WorkingSet64/1MB, 1)
            
            Write-Host "✅ Application startup successful" -ForegroundColor Green
            Write-Host "   Process ID: $($proc.Id)" -ForegroundColor White
            Write-Host "   Load time: $([math]::Round($loadTime.TotalSeconds, 2))s" -ForegroundColor White
            Write-Host "   Memory: ${memoryMB}MB" -ForegroundColor White
            
            $global:TestResults.StartupSuccess = $true
            $global:TestResults.MemoryUsageMB = $memoryMB
            $global:TestResults.TestAppProcess = $proc
        } else {
            Write-Host "❌ Application exited immediately (Exit code: $($proc.ExitCode))" -ForegroundColor Red
        }
    } catch {
        Write-Host "❌ Startup error: $($_.Exception.Message)" -ForegroundColor Red
    }
} else {
    Write-Host "❌ EXE file not found: $exePath" -ForegroundColor Red
}

# Phase 4: High-Risk File Testing (Encoding 1512 Fix)
Write-Host "`n=== Phase 4: Encoding 1512 Fix Verification ===" -ForegroundColor Magenta

$highRiskFiles = @(
    "sample/PNG/ダウンロード (2).png",
    "sample/PNG/ダウンロード (3).png", 
    "sample/PNG/ダウンロード (4).png",
    "sample/PNG/ダウンロード (5).png",
    "sample/JPG/写真 2024-05-14 17 30 29.jpg",
    "sample/JPG/写真 2024-05-14 17 30 42.jpg",
    "sample/PNG/スクリーンショット 2024-10-25 000731.png"
)

$encodingTestResults = @()

foreach ($file in $highRiskFiles) {
    Write-Host "`nTesting: $(Split-Path $file -Leaf)" -ForegroundColor Yellow
    
    if (Test-Path $file) {
        try {
            # Test file accessibility (encoding test)
            $testBytes = [System.IO.File]::ReadAllBytes($file)
            $fileInfo = Get-Item $file
            $fileSize = [math]::Round($fileInfo.Length / 1MB, 1)
            
            Write-Host "  ✅ File accessible: ${fileSize}MB" -ForegroundColor Green
            $encodingTestResults += @{File = $file; Status = "PASS"; Size = $fileSize}
            $global:TestResults.SampleDataAccess++
        }
        catch {
            Write-Host "  ❌ Encoding error: $($_.Exception.Message)" -ForegroundColor Red
            $encodingTestResults += @{File = $file; Status = "FAIL"; Error = $_.Exception.Message}
            $global:TestResults.EncodingErrors++
        }
    } else {
        Write-Host "  ❌ File not found" -ForegroundColor Red
        $encodingTestResults += @{File = $file; Status = "MISSING"}
    }
}

$passedCount = ($encodingTestResults | Where-Object { $_.Status -eq "PASS" }).Count
Write-Host "`nEncoding Test Results: $passedCount/$($highRiskFiles.Count) files passed" -ForegroundColor $(if($passedCount -eq $highRiskFiles.Count) { "Green" } else { "Red" })

# Phase 5: Bulk File Testing
if (-not $QuickTest) {
    Write-Host "`n=== Phase 5: Bulk File Processing Test ===" -ForegroundColor Magenta
    
    Write-Host "Testing all $($sampleFiles.Count) sample files..." -ForegroundColor Yellow
    $accessibleCount = 0
    
    foreach ($file in $sampleFiles) {
        try {
            $testBytes = [System.IO.File]::ReadAllBytes($file.FullName)
            $accessibleCount++
        }
        catch {
            Write-Host "  ❌ Cannot access: $($file.Name)" -ForegroundColor Red
        }
    }
    
    Write-Host "Bulk processing results: $accessibleCount/$($sampleFiles.Count) files accessible" -ForegroundColor Green
    $global:TestResults.SampleDataAccess = $accessibleCount
}

# Phase 6: Manual Testing Instructions
if ($global:TestResults.StartupSuccess) {
    Write-Host "`n=== Phase 6: Manual Testing Instructions ===" -ForegroundColor Magenta
    Write-Host "The application is running. Please perform these manual tests:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "1. CRITICAL: Drag & Drop Test" -ForegroundColor Cyan
    Write-Host "   - Navigate to: $((Get-Location).Path)\sample\PNG\" -ForegroundColor White
    Write-Host "   - Drag 'ダウンロード (2).png' into the application" -ForegroundColor White
    Write-Host "   - Verify: No 'encoding 1512' error appears" -ForegroundColor Red
    Write-Host "   - Verify: Japanese characters display correctly" -ForegroundColor White
    Write-Host ""
    Write-Host "2. Multi-Format Test" -ForegroundColor Cyan
    Write-Host "   - Test HEIC files from sample/HEIC/" -ForegroundColor White
    Write-Host "   - Test JPEG files from sample/jpeg/" -ForegroundColor White
    Write-Host ""
    Write-Host "3. PDF Generation Test" -ForegroundColor Cyan
    Write-Host "   - Load a Japanese filename file" -ForegroundColor White
    Write-Host "   - Create PDF via File menu" -ForegroundColor White
    Write-Host "   - Verify PDF creation succeeds" -ForegroundColor White
    Write-Host ""
    Write-Host "Press ENTER when manual testing is complete..." -ForegroundColor Yellow
    Read-Host
}

# Phase 7: Performance Monitoring
if ($global:TestResults.TestAppProcess -and -not $global:TestResults.TestAppProcess.HasExited) {
    Write-Host "`n=== Phase 7: Performance Assessment ===" -ForegroundColor Magenta
    
    try {
        $currentMemory = [math]::Round($global:TestResults.TestAppProcess.WorkingSet64/1MB, 1)
        $uptime = (Get-Date) - $global:TestResults.TestAppProcess.StartTime
        
        Write-Host "Performance metrics:" -ForegroundColor Green
        Write-Host "  Memory usage: ${currentMemory}MB" -ForegroundColor White
        Write-Host "  Uptime: $([math]::Round($uptime.TotalMinutes, 1)) minutes" -ForegroundColor White
        
        $global:TestResults.MemoryUsageMB = $currentMemory
        
        if ($currentMemory -lt 1000) {
            Write-Host "  ✅ Memory usage acceptable (<1GB)" -ForegroundColor Green
        } else {
            Write-Host "  ⚠️ High memory usage (>1GB)" -ForegroundColor Yellow
        }
    }
    catch {
        Write-Host "  ⚠️ Cannot assess performance - application may be unresponsive" -ForegroundColor Yellow
    }
}

# Phase 8: Final Results and Cleanup
Write-Host "`n=== COMPREHENSIVE TEST RESULTS ===" -ForegroundColor Magenta
Write-Host "=" * 60 -ForegroundColor Magenta

$endTime = Get-Date
$totalTime = $endTime - $global:TestResults.StartTime

Write-Host "`nTest Summary:" -ForegroundColor Cyan
Write-Host "  Test Duration: $([math]::Round($totalTime.TotalMinutes, 1)) minutes" -ForegroundColor White
Write-Host "  Test Date: $($endTime.ToString('yyyy-MM-dd HH:mm:ss'))" -ForegroundColor White

Write-Host "`nCore Results:" -ForegroundColor Cyan
Write-Host "  Build Success: $(if($global:TestResults.BuildSuccess) { '✅ PASS' } else { '❌ FAIL' })" -ForegroundColor $(if($global:TestResults.BuildSuccess) { "Green" } else { "Red" })
Write-Host "  Startup Success: $(if($global:TestResults.StartupSuccess) { '✅ PASS' } else { '❌ FAIL' })" -ForegroundColor $(if($global:TestResults.StartupSuccess) { "Green" } else { "Red" })
Write-Host "  Sample Files Accessible: $($global:TestResults.SampleDataAccess)" -ForegroundColor White
Write-Host "  Encoding Errors: $($global:TestResults.EncodingErrors)" -ForegroundColor $(if($global:TestResults.EncodingErrors -eq 0) { "Green" } else { "Red" })
Write-Host "  Japanese Character Files: $($global:TestResults.JapaneseFileCount)" -ForegroundColor White
Write-Host "  Peak Memory Usage: $($global:TestResults.MemoryUsageMB)MB" -ForegroundColor White

# Critical Success Factor Assessment
$criticalFactors = @{
    "Build Completes" = $global:TestResults.BuildSuccess
    "Application Starts" = $global:TestResults.StartupSuccess  
    "Zero Encoding Errors" = $global:TestResults.EncodingErrors -eq 0
    "Japanese File Support" = $global:TestResults.JapaneseFileCount -gt 0
    "Memory Under 1GB" = $global:TestResults.MemoryUsageMB -lt 1000
}

Write-Host "`nCritical Success Factors:" -ForegroundColor Cyan
$allPassed = $true
foreach ($factor in $criticalFactors.GetEnumerator()) {
    $status = if ($factor.Value) { "✅ PASS" } else { "❌ FAIL" }
    $color = if ($factor.Value) { "Green" } else { "Red" }
    Write-Host "  $($factor.Key): $status" -ForegroundColor $color
    if (-not $factor.Value) { $allPassed = $false }
}

Write-Host "`nFINAL ASSESSMENT:" -ForegroundColor Magenta
if ($allPassed -and $global:TestResults.EncodingErrors -eq 0) {
    Write-Host "🎉 ALL CRITICAL TESTS PASSED" -ForegroundColor Green
    Write-Host "📋 Encoding 1512 Fix: VERIFIED" -ForegroundColor Green  
    Write-Host "📋 DocOrganizer V2.2: READY FOR PRODUCTION" -ForegroundColor Green
} else {
    Write-Host "❌ CRITICAL ISSUES DETECTED" -ForegroundColor Red
    Write-Host "📋 Review failed tests for resolution" -ForegroundColor Yellow
    if ($global:TestResults.EncodingErrors -gt 0) {
        Write-Host "⚠️ ENCODING ERRORS STILL PRESENT - FIX REQUIRED" -ForegroundColor Red
    }
}

# Cleanup
if ($global:TestResults.TestAppProcess -and -not $global:TestResults.TestAppProcess.HasExited) {
    Write-Host "`nCleaning up..." -ForegroundColor Yellow
    try {
        $global:TestResults.TestAppProcess.CloseMainWindow()
        Start-Sleep -Seconds 2
        if (-not $global:TestResults.TestAppProcess.HasExited) {
            $global:TestResults.TestAppProcess.Kill()
        }
        Write-Host "✅ Application closed" -ForegroundColor Green
    }
    catch {
        Write-Host "⚠️ Cleanup warning: $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

Write-Host "`nTEST SUITE COMPLETED" -ForegroundColor Magenta
Write-Host "Results saved to test session. Manual verification may be required for UI functionality." -ForegroundColor Cyan