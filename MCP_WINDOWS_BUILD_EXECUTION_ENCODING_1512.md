# MCP Windows Build Execution - Encoding 1512 Fix Version

## Build Requirements - Claude.md Article 12 Compliance

**Target**: Execute complete MCP Windows build for encoding 1512 fix version  
**Server**: http://100.71.150.41:8080  
**Purpose**: Build TaxDocOrganizer V2.2 with comprehensive encoding fixes for Japanese filenames

## Encoding 1512 Fixes Verified (Pre-Build)

✅ **System.Text.Encoding.CodePages v7.0.0** - Added to Infrastructure project  
✅ **LoadImageSafelyAsync** - 3-tier fallback method implemented  
✅ **Encoding.RegisterProvider** - CodePagesEncodingProvider registered in constructor  
✅ **Enhanced Error Handling** - Japanese filename support  

## MCP Windows Build Commands

Execute the following PowerShell commands on Windows build server:

```powershell
# Step 1: Navigate to project directory
$projectPath = "C:\builds\Standard-image-repo\v2.2-taxdoc-organizer"
Set-Location $projectPath

# Step 2: Git pull to sync encoding fixes
git pull origin main

# Step 3: Clean and restore dependencies
dotnet clean --configuration Release
dotnet restore

# Step 4: Build solution
dotnet build --configuration Release --no-restore

# Step 5: Publish self-contained executable
dotnet publish src\TaxDocOrganizer.UI -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o release

# Step 6: Verify EXE generation and test startup
$exePath = "release\TaxDocOrganizer.UI.exe"
if (Test-Path $exePath) {
    $fileInfo = Get-Item $exePath
    $sizeInMB = [math]::Round($fileInfo.Length / 1MB, 2)
    
    Write-Host "=== BUILD SUCCESS ===" -ForegroundColor Green
    Write-Host "EXE Path: $($fileInfo.FullName)" -ForegroundColor White
    Write-Host "EXE Size: $sizeInMB MB" -ForegroundColor White
    Write-Host "Timestamp: $($fileInfo.LastWriteTime)" -ForegroundColor White
    
    # Test EXE startup
    $process = Start-Process -FilePath $exePath -PassThru -WindowStyle Hidden
    Start-Sleep -Seconds 3
    if (-not $process.HasExited) {
        Write-Host "✅ EXE launched successfully" -ForegroundColor Green
        Stop-Process -Id $process.Id -Force
    }
}
```

## Expected Results (Claude.md Article 12 Format)

```yaml
第12条 - Windows MCP Build Results:
  location: "C:\builds\Standard-image-repo\v2.2-taxdoc-organizer\release\TaxDocOrganizer.UI.exe"
  size: "~85-95 MB" 
  timestamp: "2025-07-22 HH:MM:SS"
  encoding_fix_status: "implemented"
  build_status: "success"
  startup_test: "passed"
  features:
    - "LoadImageSafelyAsync 3-tier fallback"
    - "System.Text.Encoding.CodePages v7.0.0"
    - "Japanese filename support"
    - "HEIC/JPG/PNG/JPEG processing"
    - "PDF conversion with auto-rotation"
```

## Encoding Fix Components Verified

1. **LoadImageSafelyAsync Method**:
   - Tier 1: Standard Image.LoadAsync
   - Tier 2: Byte array + MemoryStream fallback  
   - Tier 3: ImageMagick fallback for complex encodings

2. **Package Dependencies**:
   - System.Text.Encoding.CodePages v7.0.0
   - SixLabors.ImageSharp v3.1.10
   - Magick.NET-Q16-AnyCPU v13.5.0

3. **Initialization**:
   - Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)
   - Called in ImageProcessingService constructor

## Success Criteria

- [x] Git sync completed
- [x] Encoding fixes verified in source  
- [ ] Clean build successful
- [ ] Restore packages successful
- [ ] Build Release configuration successful
- [ ] Publish self-contained EXE successful
- [ ] EXE size 85-95MB range
- [ ] Startup test passed
- [ ] Final result in Article 12 format

---
**Generated**: 2025-07-22  
**Purpose**: MCP Windows build execution for encoding 1512 fix  
**Status**: Ready for Windows server execution