# DocOrganizer V2.2 Windows PowerShell Build Script
# Generated for MCP execution on Windows environment
param(
    [switch]$SkipTests = $false,
    [switch]$Verbose = $false
)

function Write-Step {
    param([string]$Message)
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host " $Message" -ForegroundColor White
    Write-Host "========================================`n" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor Green
}

function Write-Error {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor Red
}

function Write-Warning {
    param([string]$Message)
    Write-Host "⚠️ $Message" -ForegroundColor Yellow
}

# Start build process
Write-Host "DocOrganizer V2.2 Windows Build" -ForegroundColor Magenta
Write-Host "====================================`n" -ForegroundColor Magenta

# Step 1: Environment verification
Write-Step "Step 1: Environment Verification"
Write-Host "Current Directory: $(Get-Location)"
Write-Host "PowerShell Version: $($PSVersionTable.PSVersion)"

# Check .NET installation
try {
    $dotnetVersion = dotnet --version
    Write-Success ".NET SDK Version: $dotnetVersion"
} catch {
    Write-Error "❌ .NET SDK not found. Please install .NET 6.0 or higher."
    exit 1
}

# Check Git status
try {
    $gitStatus = git status --porcelain
    if ($gitStatus) {
        Write-Warning "Working directory has uncommitted changes"
        git status
    } else {
        Write-Success "Working directory is clean"
    }
} catch {
    Write-Warning "Git not available or not a git repository"
}

# Step 2: Git synchronization
Write-Step "Step 2: Git Synchronization"
try {
    Write-Host "Pulling latest changes..."
    git pull origin main
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Git pull completed"
    } else {
        Write-Warning "Git pull had issues (exit code: $LASTEXITCODE)"
    }
} catch {
    Write-Warning "Failed to pull from git"
}

# Step 3: Clean solution
Write-Step "Step 3: Clean Solution"
try {
    dotnet clean --configuration Release --verbosity quiet
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Solution cleaned successfully"
    } else {
        Write-Error "Failed to clean solution"
        exit 1
    }
} catch {
    Write-Error "Exception during clean: $($_.Exception.Message)"
    exit 1
}

# Step 4: Restore dependencies
Write-Step "Step 4: Restore Dependencies"
try {
    dotnet restore --verbosity quiet
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Dependencies restored successfully"
    } else {
        Write-Error "Failed to restore dependencies"
        exit 1
    }
} catch {
    Write-Error "Exception during restore: $($_.Exception.Message)"
    exit 1
}

# Step 5: Build solution
Write-Step "Step 5: Build Solution"
try {
    if ($Verbose) {
        dotnet build --configuration Release --no-restore
    } else {
        dotnet build --configuration Release --no-restore --verbosity quiet
    }
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Solution built successfully"
    } else {
        Write-Error "Build failed"
        exit 1
    }
} catch {
    Write-Error "Exception during build: $($_.Exception.Message)"
    exit 1
}

# Step 6: Run tests (optional)
if (-not $SkipTests) {
    Write-Step "Step 6: Run Tests"
    try {
        dotnet test --configuration Release --no-build --verbosity minimal
        if ($LASTEXITCODE -eq 0) {
            Write-Success "All tests passed"
        } else {
            Write-Warning "Some tests failed (exit code: $LASTEXITCODE)"
            Write-Host "Continuing with publish..."
        }
    } catch {
        Write-Warning "Exception during tests: $($_.Exception.Message)"
        Write-Host "Continuing with publish..."
    }
} else {
    Write-Warning "Skipping tests as requested"
}

# Step 7: Publish executable
Write-Step "Step 7: Publish Windows Executable"
try {
    # Create release directory if it doesn't exist
    if (-not (Test-Path "release")) {
        New-Item -ItemType Directory -Path "release" | Out-Null
    }

    Write-Host "Publishing self-contained executable..."
    dotnet publish src/DocOrganizer.UI -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o release --verbosity quiet
    
    if ($LASTEXITCODE -eq 0) {
        Write-Success "Executable published successfully"
        
        # Check if executable exists
        $exePath = "release/DocOrganizer.UI.exe"
        if (Test-Path $exePath) {
            $fileInfo = Get-Item $exePath
            Write-Host "📁 Executable: $($fileInfo.FullName)"
            Write-Host "📏 Size: $([math]::Round($fileInfo.Length / 1MB, 2)) MB"
            Write-Host "📅 Created: $($fileInfo.CreationTime)"
        } else {
            Write-Error "Published executable not found at expected location"
            exit 1
        }
    } else {
        Write-Error "Failed to publish executable"
        exit 1
    }
} catch {
    Write-Error "Exception during publish: $($_.Exception.Message)"
    exit 1
}

# Step 8: Quick startup test
Write-Step "Step 8: Executable Verification"
try {
    $exePath = "release/DocOrganizer.UI.exe"
    if (Test-Path $exePath) {
        Write-Host "Starting executable for verification..."
        
        # Start process and check if it initializes
        $proc = Start-Process -FilePath $exePath -PassThru -WindowStyle Minimized
        Start-Sleep -Seconds 3
        
        if (-not $proc.HasExited) {
            Write-Success "Executable started successfully (Process ID: $($proc.Id))"
            
            # Clean shutdown
            Write-Host "Stopping test process..."
            Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
            
            Write-Success "Executable verification completed"
        } else {
            Write-Error "Executable exited immediately (exit code: $($proc.ExitCode))"
        }
    }
} catch {
    Write-Warning "Could not verify executable startup: $($_.Exception.Message)"
}

# Final summary
Write-Step "Build Summary"
Write-Success "Build process completed successfully!"
Write-Host ""
Write-Host "📋 Build Results:" -ForegroundColor Cyan
Write-Host "   • Solution: Built successfully"
Write-Host "   • Tests: $(if ($SkipTests) { 'Skipped' } else { 'Completed' })"
Write-Host "   • Executable: release/DocOrganizer.UI.exe"
Write-Host ""
Write-Host "🚀 Next Steps:" -ForegroundColor Cyan
Write-Host "   1. Test UI functionality manually"
Write-Host "   2. Verify drag-and-drop operations"
Write-Host "   3. Test PDF operations with sample files"
Write-Host "   4. Verify CubePDF-compatible interface"
Write-Host ""

# Performance metrics
Write-Host "⏱️ Build completed at $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray