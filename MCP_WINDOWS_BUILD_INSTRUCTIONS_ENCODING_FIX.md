# MCP Windows Build Instructions - Encoding 1512 Fix Version

## Overview
This document provides step-by-step instructions for executing the Windows build via MCP to create the TaxDocOrganizer V2.2 executable with encoding 1512 error fixes.

## MCP Connection Details
- **MCP Server**: http://100.71.150.41:8080
- **Target Platform**: Windows
- **Build Environment**: C:\builds\Standard-image-repo\v2.2-taxdoc-organizer

## Encoding Fix Implementation Summary
The following fixes have been implemented to resolve the "no data is available for encoding 1512" error:

1. **System.Text.Encoding.CodePages Package**: Added to TaxDocOrganizer.Infrastructure.csproj
2. **Encoding Provider Registration**: `Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)` in ImageProcessingService constructor
3. **Fallback Loading Strategy**: `LoadImageSafelyAsync` method with 3-tier fallback:
   - Standard Image.LoadAsync
   - Byte array with MemoryStream loading  
   - Magick.NET conversion to JPEG then ImageSharp loading

## Execution Steps

### Step 1: Connect to MCP Windows Build Server
```bash
# Connect to MCP server
curl -X GET http://100.71.150.41:8080/status
```

### Step 2: Execute Build Script via MCP
```powershell
# Copy and execute the build script
powershell -ExecutionPolicy Bypass -File "C:\builds\Standard-image-repo\v2.2-taxdoc-organizer\mcp-windows-build-encoding-fix.ps1"
```

### Step 3: Alternative Manual Execution (if script fails)
```powershell
# Navigate to project directory
cd "C:\builds\Standard-image-repo\v2.2-taxdoc-organizer"

# Sync latest changes
git pull origin main

# Clean and restore
dotnet clean --configuration Release
dotnet restore

# Build solution  
dotnet build --configuration Release

# Publish executable
dotnet publish src\TaxDocOrganizer.UI -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o release

# Verify result
dir release\TaxDocOrganizer.UI.exe
```

## Expected Output Format (Claude.md Article 12)

Upon successful completion, the build should produce output in the following format:

```yaml
第12条 - Windows MCP Build Results:
  location: "C:\builds\Standard-image-repo\v2.2-taxdoc-organizer\release\TaxDocOrganizer.UI.exe"
  size: "XX.X MB"
  timestamp: "2025-01-XX XX:XX:XX"
  encoding_fix_status: "implemented"
  build_status: "success"
  startup_test: "passed"
```

## Verification Checklist

### ✅ Pre-Build Verification
- [ ] MCP connection established
- [ ] Project directory exists at C:\builds\Standard-image-repo\v2.2-taxdoc-organizer
- [ ] Git repository is up to date
- [ ] System.Text.Encoding.CodePages package present in Infrastructure project

### ✅ Build Process Verification  
- [ ] dotnet clean completed without errors
- [ ] dotnet restore successfully installed all packages
- [ ] dotnet build completed successfully
- [ ] dotnet publish generated single executable

### ✅ Post-Build Verification
- [ ] TaxDocOrganizer.UI.exe exists in release directory
- [ ] EXE file size is reasonable (typically 80-120 MB for self-contained)
- [ ] EXE launches without immediate crash
- [ ] Encoding.RegisterProvider implementation present in source
- [ ] LoadImageSafelyAsync fallback method implemented

## Error Handling

### Common Issues and Solutions

1. **Git Pull Fails**
   - Solution: Continue with local changes, verify manually that encoding fixes are present

2. **dotnet restore Fails**
   - Solution: Check internet connection, clear NuGet cache: `dotnet nuget locals all --clear`

3. **Build Compilation Errors**
   - Solution: Check for syntax errors in modified files, ensure all dependencies are restored

4. **Publish Fails**
   - Solution: Verify target framework compatibility, check disk space

5. **EXE Not Found**
   - Solution: Check publish output path, verify no permission issues

## Technical Details

### Key Files Modified for Encoding Fix
1. `src\TaxDocOrganizer.Infrastructure\TaxDocOrganizer.Infrastructure.csproj`
   - Added System.Text.Encoding.CodePages package reference

2. `src\TaxDocOrganizer.Infrastructure\Services\ImageProcessingService.cs`
   - Added encoding provider registration in constructor
   - Implemented LoadImageSafelyAsync with comprehensive fallback strategy
   - Enhanced error handling for encoding-related exceptions

### Package Dependencies
```xml
<PackageReference Include="System.Text.Encoding.CodePages" Version="7.0.0" />
```

### Encoding Fix Implementation
```csharp
// Constructor
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

// Fallback loading method
private async Task<Image> LoadImageSafelyAsync(string imagePath)
{
    try
    {
        return await Image.LoadAsync(imagePath);
    }
    catch (Exception ex) when (ex.Message.Contains("encoding") || ex.Message.Contains("1512"))
    {
        // Fallback strategies implemented
    }
}
```

## Final Result Format

The successful build will output the EXE details in Claude.md Article 12 format for documentation purposes:

```
第12条 - Windows MCP Build Results:
  location: "C:\builds\Standard-image-repo\v2.2-taxdoc-organizer\release\TaxDocOrganizer.UI.exe"
  size: "XX.X MB"  
  timestamp: "2025-01-XX XX:XX:XX"
  encoding_fix_status: "implemented"
  build_status: "success"
  startup_test: "passed"
```

This format ensures compliance with project documentation standards and provides all necessary information for verification.