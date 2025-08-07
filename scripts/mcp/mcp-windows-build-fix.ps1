# DocOrganizer V2.2 Windows MCP Build Fix Script
# 診断と修正を統合したスクリプト

param(
    [switch]$DiagnoseOnly = $false,
    [switch]$Force = $false
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

Write-Host "DocOrganizer V2.2 Build Diagnosis & Fix" -ForegroundColor Magenta
Write-Host "==========================================`n" -ForegroundColor Magenta

# Step 1: Environment diagnosis
Write-Step "Step 1: Environment Diagnosis"

# Check current directory
$currentDir = Get-Location
Write-Host "Current Directory: $currentDir"

# Expected path validation
$expectedPaths = @(
    "C:\builds\Standard-image-repo\v2.2-doc-organizer",
    "src\DocOrganizer.UI\DocOrganizer.UI.csproj",
    "DocOrganizer.V2.2.sln"
)

foreach ($path in $expectedPaths) {
    if (Test-Path $path) {
        Write-Success "Found: $path"
    } else {
        Write-Error "Missing: $path"
    }
}

# Check .NET SDK
try {
    $dotnetVersion = dotnet --version
    Write-Success ".NET SDK Version: $dotnetVersion"
    
    # Check if version is 6.0 or higher
    $versionNumber = [Version]$dotnetVersion
    if ($versionNumber -ge [Version]"6.0.0") {
        Write-Success ".NET version is compatible"
    } else {
        Write-Error ".NET version is too old (need 6.0+)"
    }
} catch {
    Write-Error ".NET SDK not found or not accessible"
    Write-Host "Please install .NET 6.0 SDK from: https://dotnet.microsoft.com/download"
    exit 1
}

# Step 2: Project file validation
Write-Step "Step 2: Project File Validation"

$projectFile = "src\DocOrganizer.UI\DocOrganizer.UI.csproj"
if (Test-Path $projectFile) {
    Write-Success "UI project file found"
    
    # Check project file content
    $projectContent = Get-Content $projectFile -Raw
    
    # Check for required properties
    $requiredProperties = @(
        "OutputType.*WinExe",
        "TargetFramework.*net6.0-windows",
        "UseWPF.*true",
        "PublishSingleFile.*true",
        "SelfContained.*true",
        "RuntimeIdentifier.*win-x64"
    )
    
    foreach ($property in $requiredProperties) {
        if ($projectContent -match $property) {
            Write-Success "Property found: $property"
        } else {
            Write-Warning "Property missing or incorrect: $property"
        }
    }
} else {
    Write-Error "UI project file not found: $projectFile"
    exit 1
}

# Step 3: Build diagnostics
Write-Step "Step 3: Build Diagnostics"

if (-not $DiagnoseOnly) {
    # Clean previous builds
    Write-Host "Cleaning previous builds..."
    try {
        dotnet clean --configuration Release --verbosity minimal
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Clean completed"
        } else {
            Write-Warning "Clean had warnings (exit code: $LASTEXITCODE)"
        }
    } catch {
        Write-Warning "Clean failed: $($_.Exception.Message)"
    }
    
    # Restore dependencies with detailed output
    Write-Host "Restoring dependencies..."
    try {
        dotnet restore --verbosity normal
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Restore completed"
        } else {
            Write-Error "Restore failed (exit code: $LASTEXITCODE)"
            exit 1
        }
    } catch {
        Write-Error "Exception during restore: $($_.Exception.Message)"
        exit 1
    }
    
    # Build with detailed output
    Write-Host "Building solution..."
    try {
        dotnet build --configuration Release --no-restore --verbosity normal
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Build completed"
        } else {
            Write-Error "Build failed (exit code: $LASTEXITCODE)"
            Write-Host "Attempting detailed diagnostic build..."
            dotnet build --configuration Release --no-restore --verbosity detailed
            exit 1
        }
    } catch {
        Write-Error "Exception during build: $($_.Exception.Message)"
        exit 1
    }
}

# Step 4: Publish diagnostics
Write-Step "Step 4: Publish Diagnostics"

if (-not $DiagnoseOnly) {
    # Create release directory if missing
    if (-not (Test-Path "release")) {
        Write-Host "Creating release directory..."
        New-Item -ItemType Directory -Path "release" | Out-Null
        Write-Success "Release directory created"
    }
    
    # Attempt publish with detailed output
    Write-Host "Publishing executable..."
    try {
        $publishArgs = @(
            "publish",
            "src/DocOrganizer.UI",
            "-c", "Release",
            "-r", "win-x64",
            "--self-contained", "true",
            "-p:PublishSingleFile=true",
            "-p:PublishReadyToRun=true",
            "-p:IncludeNativeLibrariesForSelfExtract=true",
            "-o", "release",
            "--verbosity", "normal"
        )
        
        $publishCommand = "dotnet " + ($publishArgs -join " ")
        Write-Host "Publish command: $publishCommand"
        
        & dotnet @publishArgs
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Publish completed"
        } else {
            Write-Error "Publish failed (exit code: $LASTEXITCODE)"
            Write-Host "Attempting publish with detailed verbosity..."
            $publishArgs[-1] = "detailed"
            & dotnet @publishArgs
            exit 1
        }
    } catch {
        Write-Error "Exception during publish: $($_.Exception.Message)"
        exit 1
    }
}

# Step 5: EXE verification
Write-Step "Step 5: EXE Verification"

$exeFiles = @(
    "release\DocOrganizer.UI.exe",
    "release\DocOrganizerV22.exe"
)

$foundExe = $null
foreach ($exeFile in $exeFiles) {
    if (Test-Path $exeFile) {
        $foundExe = $exeFile
        break
    }
}

if ($foundExe) {
    $fileInfo = Get-Item $foundExe
    Write-Success "EXE found: $($fileInfo.FullName)"
    Write-Host "📏 Size: $([math]::Round($fileInfo.Length / 1MB, 2)) MB"
    Write-Host "📅 Created: $($fileInfo.CreationTime)"
    Write-Host "📝 Modified: $($fileInfo.LastWriteTime)"
    
    # Quick startup test if not in diagnose mode
    if (-not $DiagnoseOnly) {
        Write-Host "Testing executable startup..."
        try {
            $proc = Start-Process -FilePath $fileInfo.FullName -PassThru -WindowStyle Minimized
            Start-Sleep -Seconds 3
            
            if (-not $proc.HasExited) {
                Write-Success "Executable started successfully (PID: $($proc.Id))"
                Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
                Write-Success "Startup test completed"
            } else {
                Write-Error "Executable exited immediately (exit code: $($proc.ExitCode))"
            }
        } catch {
            Write-Warning "Could not test startup: $($_.Exception.Message)"
        }
    }
} else {
    Write-Error "No executable found in release directory"
    Write-Host "Contents of release directory:"
    if (Test-Path "release") {
        Get-ChildItem "release" | ForEach-Object {
            Write-Host "  - $($_.Name) ($($_.Length) bytes)"
        }
    } else {
        Write-Host "  Release directory does not exist"
    }
}

# Step 6: Final recommendations
Write-Step "Step 6: Recommendations"

if ($foundExe) {
    Write-Success "Build successful! EXE ready for use."
    Write-Host ""
    Write-Host "🎯 Next Steps:" -ForegroundColor Cyan
    Write-Host "   1. Manual UI testing"
    Write-Host "   2. PDF operation verification" 
    Write-Host "   3. CubePDF compatibility check"
    Write-Host "   4. Performance validation"
} else {
    Write-Error "Build failed. Please check error messages above."
    Write-Host ""
    Write-Host "🔧 Troubleshooting:" -ForegroundColor Yellow
    Write-Host "   1. Ensure .NET 6.0 SDK is installed"
    Write-Host "   2. Check for compilation errors in detailed output"
    Write-Host "   3. Verify all project dependencies"
    Write-Host "   4. Check Windows security/antivirus restrictions"
}

Write-Host ""
Write-Host "⏱️ Diagnosis completed at $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray