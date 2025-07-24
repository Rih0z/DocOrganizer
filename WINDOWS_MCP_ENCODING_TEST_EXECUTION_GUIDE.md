# Windows MCP Encoding Test Execution Guide
## Quick Reference for Encoding 1512 Fix Verification

### Pre-Test Setup Commands
```powershell
# Navigate to project directory
cd "C:\builds\Standard-image-repo\v2.2-taxdoc-organizer"

# Sync with latest changes
git pull origin main

# Verify sample files count
Get-ChildItem -Path "sample" -Recurse -Include "*.heic","*.HEIC","*.jpg","*.JPG","*.png","*.PNG","*.jpeg","*.JPEG" | Measure-Object
# Expected: Count = 40

# Check build status
dotnet restore
dotnet build --configuration Release
```

### Application Launch Test
```powershell
# Start the application
cd release
Start-Process -FilePath "TaxDocOrganizer.UI.exe" -PassThru

# Monitor for 10 seconds
# Expected: Window opens, no encoding error dialogs
```

### Priority Test Files (High-Risk Encoding)

#### Critical Test 1: Highest Risk File
**File**: `sample/PNG/ダウンロード (2).png` (10.6MB)
- **Risk Factors**: Japanese + Spaces + Parentheses
- **Action**: Drag and drop to application
- **Expected**: Loads without "encoding 1512" error

#### Critical Test 2: Japanese Text with Spaces
**File**: `sample/JPG/写真 2024-05-14 17 30 29.jpg` (4.3MB)
- **Risk Factors**: Japanese + Spaces + EXIF data
- **Action**: Drag and drop to application  
- **Expected**: JPG loads successfully with proper filename display

#### Critical Test 3: Parentheses in English
**File**: `sample/jpeg/IMG_2949(1).jpeg` (4.1MB)
- **Risk Factors**: Parentheses (common in Windows filenames)
- **Action**: Drag and drop to application
- **Expected**: Loads without parentheses-related errors

#### Critical Test 4: Bulk High-Risk Load
**Files**: Select all 4 "ダウンロード" PNG files from `sample/PNG/`
- **Action**: Multi-select and drag all at once
- **Expected**: All 4 files load simultaneously without crashes

### Quick Success Verification
```powershell
# If all critical tests pass:
echo "✅ Encoding 1512 Fix VERIFIED"
echo "✅ Japanese character support WORKING"  
echo "✅ Special character filenames SUPPORTED"
echo "✅ Application stability CONFIRMED"

# If any test fails:
echo "❌ Encoding issues detected - review logs"
```

### Emergency Troubleshooting
```powershell
# Check if encoding package is loaded
dotnet list package | findstr "CodePages"
# Expected: System.Text.Encoding.CodePages

# View recent application logs (if logging implemented)
Get-ChildItem -Path "." -Filter "*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 1

# Memory usage check
Get-Process | Where-Object {$_.ProcessName -like "*TaxDoc*"} | Select-Object ProcessName, WorkingSet
```

### Test Result Summary Template
```yaml
Encoding 1512 Test Results:
Date: [Current Date/Time]
Critical Test 1 (ダウンロード (2).png): [PASS/FAIL]
Critical Test 2 (写真 2024-05-14...): [PASS/FAIL]  
Critical Test 3 (IMG_2949(1).jpeg): [PASS/FAIL]
Critical Test 4 (Bulk load): [PASS/FAIL]
Overall Status: [PASS/FAIL]
```

### Next Steps After Testing
- **If PASS**: Document success and proceed with production deployment
- **If FAIL**: Collect error details and review encoding implementation

**Total Test Time**: ~15 minutes for comprehensive verification