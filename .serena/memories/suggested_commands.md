# Suggested Commands for DocOrganizer Development

## Core Development Commands

### Building the Project
```bash
# Clean and restore
dotnet clean
dotnet restore

# Development build
dotnet build --configuration Debug

# Release build  
dotnet build --configuration Release

# Publish single-file executable
dotnet publish src/DocOrganizer.UI/DocOrganizer.UI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o release
```

### Testing
```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test tests/DocOrganizer.Core.Tests/
```

### Useful PowerShell Test Scripts
```powershell
# Quick automated test
.\scripts\test\QuickAutoTest.ps1

# Comprehensive functionality test
.\scripts\test\ComprehensiveTest.ps1

# Test drag & drop functionality
.\scripts\test\drag-drop-test.ps1

# Test orientation correction
.\scripts\test\test-orientation-correction.ps1

# Test all sample files
.\scripts\test\TestAllSampleFiles.ps1
```

### Git Operations
```bash
# Standard workflow
git add .
git commit -m "Description of changes"
git push origin main

# Pull latest changes (always do before starting work)
git pull origin main

# Check status
git status
```

### Windows System Commands
```cmd
# List files
dir
ls  # PowerShell alias

# Change directory
cd path\to\directory

# Find files
Get-ChildItem -Recurse -Name "*.cs" | Select-String "pattern"

# Process management
Get-Process DocOrganizer
Stop-Process -Name DocOrganizer
```

### Debug and Logging
```bash
# View debug log
type release\DEBUG_LOG.txt
# or
Get-Content release\DEBUG_LOG.txt -Wait  # Real-time monitoring
```

### Release Management
```powershell
# Build and create GitHub release
.\scripts\build\build-and-release.ps1 -Version "2.2.0" -GitHubToken $env:GITHUB_TOKEN

# Upload existing build to GitHub
.\scripts\utils\upload-release.ps1 -Version "2.2.0" -GitHubToken $env:GITHUB_TOKEN
```

## Development Workflow
1. `git pull origin main` - Always sync before starting
2. Make changes
3. Run tests: `dotnet test`
4. Build: `dotnet build --configuration Release`
5. Test manually: `.\scripts\test\QuickAutoTest.ps1`
6. Commit and push changes
7. For releases: Use build-and-release.ps1 script