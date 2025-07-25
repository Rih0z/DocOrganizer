# Windows Application Startup Crash Investigation Script
# Target: DocOrganizer.UI.exe startup crash diagnosis
# Generated: 2025-07-22
# Purpose: Comprehensive diagnostic information gathering

param(
    [string]$ProjectPath = "C:\builds\Standard-image-repo\v2.2-doc-organizer",
    [string]$McpServer = "http://100.71.150.41:8080"
)

Write-Host "=== DocOrganizer.UI.exe Startup Crash Investigation ===" -ForegroundColor Yellow
Write-Host "Project Path: $ProjectPath" -ForegroundColor White
Write-Host "MCP Server: $McpServer" -ForegroundColor White
Write-Host ""

# Set error action preference for detailed error information
$ErrorActionPreference = "Continue"

try {
    # Step 1: Verify project structure and EXE location
    Write-Host "=== Step 1: File System Investigation ===" -ForegroundColor Green
    
    if (Test-Path $ProjectPath) {
        Write-Host "✅ Project directory exists: $ProjectPath" -ForegroundColor Green
        Set-Location $ProjectPath
        
        # List key directories
        Write-Host "`nProject structure:" -ForegroundColor White
        Get-ChildItem -Directory | ForEach-Object { 
            Write-Host "  📁 $($_.Name)" -ForegroundColor Cyan
        }
        
        # Check release directory
        $releaseDir = Join-Path $ProjectPath "release"
        if (Test-Path $releaseDir) {
            Write-Host "`n✅ Release directory exists" -ForegroundColor Green
            Write-Host "Release contents:" -ForegroundColor White
            Get-ChildItem $releaseDir | ForEach-Object {
                $sizeInfo = if ($_.PSIsContainer) { "[DIR]" } else { "$([math]::Round($_.Length/1MB, 2))MB" }
                Write-Host "  📄 $($_.Name) - $sizeInfo" -ForegroundColor White
            }
        } else {
            Write-Host "❌ Release directory not found: $releaseDir" -ForegroundColor Red
        }
        
        # Look for EXE files in the project
        Write-Host "`nSearching for EXE files in project..." -ForegroundColor White
        Get-ChildItem -Recurse -Filter "*.exe" | ForEach-Object {
            $relPath = $_.FullName.Replace($ProjectPath, ".")
            $sizeInfo = "$([math]::Round($_.Length/1MB, 2))MB"
            Write-Host "  🔍 Found: $relPath - $sizeInfo - $($_.LastWriteTime)" -ForegroundColor Cyan
        }
        
    } else {
        Write-Host "❌ Project directory not found: $ProjectPath" -ForegroundColor Red
        return
    }
    
    # Step 2: Locate the target EXE
    Write-Host "`n=== Step 2: Target EXE Analysis ===" -ForegroundColor Green
    
    $exePaths = @(
        "release\DocOrganizer.UI.exe",
        "src\DocOrganizer.UI\bin\Release\net6.0-windows\DocOrganizer.UI.exe",
        "src\DocOrganizer.UI\bin\Release\net6.0-windows\win-x64\DocOrganizer.UI.exe",
        "src\DocOrganizer.UI\bin\Release\net6.0-windows\publish\DocOrganizer.UI.exe"
    )
    
    $targetExe = $null
    foreach ($exePath in $exePaths) {
        $fullPath = Join-Path $ProjectPath $exePath
        if (Test-Path $fullPath) {
            $targetExe = $fullPath
            $fileInfo = Get-Item $fullPath
            Write-Host "✅ Found target EXE: $exePath" -ForegroundColor Green
            Write-Host "   Path: $fullPath" -ForegroundColor White
            Write-Host "   Size: $([math]::Round($fileInfo.Length/1MB, 2))MB" -ForegroundColor White
            Write-Host "   Created: $($fileInfo.CreationTime)" -ForegroundColor White
            Write-Host "   Modified: $($fileInfo.LastWriteTime)" -ForegroundColor White
            break
        }
    }
    
    if (-not $targetExe) {
        Write-Host "❌ No target EXE found in expected locations" -ForegroundColor Red
        Write-Host "Expected locations checked:" -ForegroundColor White
        foreach ($path in $exePaths) {
            Write-Host "  ❌ $path" -ForegroundColor Red
        }
        return
    }
    
    # Step 3: .NET Runtime Environment Check
    Write-Host "`n=== Step 3: .NET Runtime Environment ===" -ForegroundColor Green
    
    try {
        $dotnetInfo = dotnet --info 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ .NET CLI available" -ForegroundColor Green
            $dotnetInfo -split "`n" | Select-Object -First 10 | ForEach-Object {
                Write-Host "  $($_)" -ForegroundColor White
            }
        } else {
            Write-Host "❌ .NET CLI not available or error occurred" -ForegroundColor Red
        }
    } catch {
        Write-Host "❌ Error checking .NET info: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    # Check for .NET 6.0 runtime
    try {
        $runtimes = dotnet --list-runtimes 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "`n.NET Runtimes available:" -ForegroundColor White
            $runtimes -split "`n" | Where-Object { $_ -like "*6.0*" } | ForEach-Object {
                Write-Host "  ✅ $_" -ForegroundColor Green
            }
        }
    } catch {
        Write-Host "❌ Error checking .NET runtimes: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    # Step 4: Dependency Analysis
    Write-Host "`n=== Step 4: Dependency Analysis ===" -ForegroundColor Green
    
    # Check for companion files (should be minimal for self-contained)
    $exeDir = Split-Path $targetExe -Parent
    $companionFiles = Get-ChildItem $exeDir | Where-Object { $_.Name -ne (Split-Path $targetExe -Leaf) }
    
    if ($companionFiles.Count -gt 0) {
        Write-Host "Companion files in EXE directory:" -ForegroundColor White
        $companionFiles | ForEach-Object {
            $sizeInfo = if ($_.PSIsContainer) { "[DIR]" } else { "$([math]::Round($_.Length/1MB, 2))MB" }
            Write-Host "  📄 $($_.Name) - $sizeInfo" -ForegroundColor White
        }
    } else {
        Write-Host "✅ Clean EXE directory (self-contained)" -ForegroundColor Green
    }
    
    # Step 5: Application Startup Test with Detailed Error Capture
    Write-Host "`n=== Step 5: Application Startup Investigation ===" -ForegroundColor Green
    
    # Test 1: Basic startup with immediate exit
    Write-Host "`nTest 1: Basic startup test..." -ForegroundColor Yellow
    try {
        $startInfo = New-Object System.Diagnostics.ProcessStartInfo
        $startInfo.FileName = $targetExe
        $startInfo.UseShellExecute = $false
        $startInfo.RedirectStandardOutput = $true
        $startInfo.RedirectStandardError = $true
        $startInfo.CreateNoWindow = $true
        $startInfo.WindowStyle = [System.Diagnostics.ProcessWindowStyle]::Hidden
        
        $process = New-Object System.Diagnostics.Process
        $process.StartInfo = $startInfo
        
        # Set up event handlers for output
        $stdOut = New-Object System.Text.StringBuilder
        $stdErr = New-Object System.Text.StringBuilder
        
        $process.add_OutputDataReceived({
            param($sender, $e)
            if ($e.Data -ne $null) {
                [void]$stdOut.AppendLine($e.Data)
            }
        })
        
        $process.add_ErrorDataReceived({
            param($sender, $e)
            if ($e.Data -ne $null) {
                [void]$stdErr.AppendLine($e.Data)
            }
        })
        
        Write-Host "  🚀 Starting process..." -ForegroundColor White
        $process.Start()
        $process.BeginOutputReadLine()
        $process.BeginErrorReadLine()
        
        # Wait for a few seconds
        $waitResult = $process.WaitForExit(5000)  # 5 second timeout
        
        if ($process.HasExited) {
            Write-Host "  ⚠️  Process exited with code: $($process.ExitCode)" -ForegroundColor Yellow
            Write-Host "  📊 Exit time: $(Get-Date)" -ForegroundColor White
            Write-Host "  ⏱️  Runtime: ~$($process.ExitTime - $process.StartTime)" -ForegroundColor White
            
            # Check for output
            $stdOutText = $stdOut.ToString().Trim()
            $stdErrText = $stdErr.ToString().Trim()
            
            if ($stdOutText) {
                Write-Host "`n  📤 Standard Output:" -ForegroundColor Cyan
                Write-Host $stdOutText -ForegroundColor White
            }
            
            if ($stdErrText) {
                Write-Host "`n  ❌ Standard Error:" -ForegroundColor Red
                Write-Host $stdErrText -ForegroundColor White
            }
            
            if (-not $stdOutText -and -not $stdErrText) {
                Write-Host "  ℹ️  No console output captured" -ForegroundColor Gray
            }
            
        } else {
            Write-Host "  ✅ Process is running (PID: $($process.Id))" -ForegroundColor Green
            Write-Host "  ⏸️  Terminating for investigation..." -ForegroundColor Yellow
            try {
                $process.Kill()
                $process.WaitForExit(2000)
                Write-Host "  ✅ Process terminated successfully" -ForegroundColor Green
            } catch {
                Write-Host "  ⚠️  Error terminating process: $($_.Exception.Message)" -ForegroundColor Yellow
            }
        }
        
    } catch {
        Write-Host "  ❌ Error starting process: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "  📋 Exception details:" -ForegroundColor Red
        Write-Host "     Type: $($_.Exception.GetType().FullName)" -ForegroundColor Red
        Write-Host "     Message: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.Exception.InnerException) {
            Write-Host "     Inner: $($_.Exception.InnerException.Message)" -ForegroundColor Red
        }
    }
    
    # Step 6: Windows Event Log Check
    Write-Host "`n=== Step 6: Windows Event Log Analysis ===" -ForegroundColor Green
    
    try {
        # Get recent application errors
        $cutoffTime = (Get-Date).AddMinutes(-10)
        Write-Host "Checking for application errors in last 10 minutes..." -ForegroundColor White
        
        $appErrors = Get-WinEvent -FilterHashtable @{
            LogName = 'Application'
            Level = 1,2  # Critical and Error
            StartTime = $cutoffTime
        } -MaxEvents 20 -ErrorAction SilentlyContinue | Where-Object {
            $_.LevelDisplayName -eq 'Error' -or $_.LevelDisplayName -eq 'Critical'
        }
        
        if ($appErrors) {
            Write-Host "Recent application errors found:" -ForegroundColor Yellow
            $appErrors | Select-Object -First 5 | ForEach-Object {
                Write-Host "  📅 $($_.TimeCreated) - $($_.LevelDisplayName)" -ForegroundColor White
                Write-Host "     Source: $($_.ProviderName)" -ForegroundColor Gray
                Write-Host "     Message: $($_.Message -replace "`n", " " -replace "`r", "")" -ForegroundColor Gray
                Write-Host ""
            }
        } else {
            Write-Host "✅ No recent critical application errors in Event Log" -ForegroundColor Green
        }
        
    } catch {
        Write-Host "⚠️ Could not access Windows Event Log: $($_.Exception.Message)" -ForegroundColor Yellow
    }
    
    # Step 7: Environment Analysis
    Write-Host "`n=== Step 7: Environment Analysis ===" -ForegroundColor Green
    
    # OS Information
    $osInfo = Get-ComputerInfo | Select-Object WindowsProductName, WindowsVersion, TotalPhysicalMemory
    Write-Host "Operating System:" -ForegroundColor White
    Write-Host "  Name: $($osInfo.WindowsProductName)" -ForegroundColor White
    Write-Host "  Version: $($osInfo.WindowsVersion)" -ForegroundColor White
    Write-Host "  RAM: $([math]::Round($osInfo.TotalPhysicalMemory/1GB, 1))GB" -ForegroundColor White
    
    # User context
    Write-Host "`nUser Context:" -ForegroundColor White
    Write-Host "  User: $env:USERNAME" -ForegroundColor White
    Write-Host "  Domain: $env:USERDOMAIN" -ForegroundColor White
    Write-Host "  Admin: $(([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator"))" -ForegroundColor White
    
    # Temp directory permissions
    Write-Host "`nTemp Directory Access:" -ForegroundColor White
    try {
        $testFile = Join-Path $env:TEMP "taxdoc-test-$(Get-Random).txt"
        "test" | Out-File $testFile -ErrorAction Stop
        Remove-Item $testFile -ErrorAction SilentlyContinue
        Write-Host "  ✅ Can write to temp directory: $env:TEMP" -ForegroundColor Green
    } catch {
        Write-Host "  ❌ Cannot write to temp directory: $($_.Exception.Message)" -ForegroundColor Red
    }
    
    # Step 8: Generate Crash Analysis Report
    Write-Host "`n=== Step 8: Crash Analysis Summary ===" -ForegroundColor Green
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    
    Write-Host "`n" + "="*80 -ForegroundColor Yellow
    Write-Host "TAXDOC ORGANIZER V2.2 STARTUP CRASH INVESTIGATION REPORT" -ForegroundColor Yellow
    Write-Host "Generated: $timestamp" -ForegroundColor Yellow
    Write-Host "="*80 -ForegroundColor Yellow
    
    Write-Host "`n🔍 INVESTIGATION SUMMARY:" -ForegroundColor White
    Write-Host "   Target EXE: $targetExe" -ForegroundColor White
    Write-Host "   Project Path: $ProjectPath" -ForegroundColor White
    Write-Host "   Investigation Status: COMPLETED" -ForegroundColor Green
    
    Write-Host "`n📋 NEXT STEPS REQUIRED:" -ForegroundColor White
    Write-Host "   1. Review process exit code and error output above" -ForegroundColor White
    Write-Host "   2. Check for missing .NET 6.0 runtime dependencies" -ForegroundColor White
    Write-Host "   3. Examine App.xaml.cs DI configuration issues" -ForegroundColor White
    Write-Host "   4. Verify WPF initialization and logging setup" -ForegroundColor White
    Write-Host "   5. Test with verbose logging enabled if supported" -ForegroundColor White
    
    Write-Host "`n✅ INVESTIGATION COMPLETED SUCCESSFULLY" -ForegroundColor Green
    Write-Host "="*80 -ForegroundColor Yellow
    
} catch {
    Write-Host "`n❌ CRITICAL ERROR DURING INVESTIGATION" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Stack: $($_.ScriptStackTrace)" -ForegroundColor Red
    
} finally {
    Write-Host "`nInvestigation completed at $(Get-Date)" -ForegroundColor Gray
}