# TaxDocOrganizer V2.2 - Encoding 1512 Comprehensive Test Plan

## Executive Summary

This document provides a comprehensive test plan for verifying the encoding 1512 fix implementation in TaxDocOrganizer V2.2, based on detailed analysis of 40 sample images across 5 formats, with specific focus on 17 high-risk files containing Japanese characters, spaces, and special symbols.

## Sample Data Analysis Results

### Total Sample Inventory
- **Total Files**: 40 image files
- **High-Risk Files**: 17 files (42.5% of total)
- **Formats Covered**: HEIC, JPG, PNG, JPEG
- **Distribution**: 
  - HEIC: 16 files (2.3-3.6 MB each)
  - JPG: 9 files (368KB-4.3MB each) 
  - PNG: 10 files (110KB-11MB each)
  - JPEG: 5 files (4.0-4.5MB each)

### High-Risk Files for Encoding Testing

#### Category A: Japanese Characters + Spaces + Parentheses (Highest Risk)
```
ダウンロード (2).png - 10.6MB - Japanese + Spaces + Parentheses
ダウンロード (3).png - 10.1MB - Japanese + Spaces + Parentheses  
ダウンロード (4).png - 9.1MB - Japanese + Spaces + Parentheses
ダウンロード (5).png - 11.0MB - Japanese + Spaces + Parentheses
```

#### Category B: Japanese Characters + Spaces (High Risk)
```
写真 2024-05-14 17 30 29.jpg - 4.3MB - iPhone EXIF data
写真 2024-05-14 17 30 42.jpg - 3.6MB - iPhone EXIF data
写真 2024-05-14 17 31 00.jpg - 3.6MB - iPhone EXIF data  
写真 2024-05-14 17 31 08.jpg - 3.5MB - iPhone EXIF data
スクリーンショット 2024-10-25 000731.png - 379KB - Screenshot
```

#### Category C: Japanese Characters Only (Medium Risk)
```
ダウンロード.png - 10.9MB - Single Japanese word
```

#### Category D: Parentheses in English (Medium Risk)
```
IMG_2949(1).jpeg - 4.1MB - Duplicate indicator
IMG_2950(1).jpeg - 4.1MB - Duplicate indicator
Image_20250210_235730_150 (1).jpeg - 4.5MB - Timestamp + parentheses
```

#### Category E: Spaces in English (Low Risk)
```
1- hidari-ping.png - 115KB - Orientation test image
1- migi-png.png - 115KB - Orientation test image
1- tate-png.png - 109KB - Orientation test image
1- ura-png.png - 109KB - Orientation test image
```

### Low-Risk Control Files (Standard ASCII)
```
HEIC/IMG_5331.HEIC to IMG_5417.HEIC - 16 files (2.3-3.6MB)
JPG/IMG_7347.JPG to IMG_7351.JPG - 5 files (368-414KB)
jpeg/IMG_2949.jpeg, IMG_2950.jpeg - 2 files (4.1MB each)
```

## Windows MCP Test Execution Plan

### Phase 1: Pre-Test Environment Verification

#### 1.1 Repository Synchronization
```powershell
cd "C:\builds\Standard-image-repo\v2.2-taxdoc-organizer"
git pull origin main
git status
```
**Expected**: Clean working directory, up-to-date with main branch

#### 1.2 Sample Data Verification
```powershell
# Count total sample files
Get-ChildItem -Path "sample" -Recurse -Include "*.heic","*.HEIC","*.jpg","*.JPG","*.png","*.PNG","*.jpeg","*.JPEG" | Measure-Object
```
**Expected**: Count = 40 files

#### 1.3 Build Status Check
```powershell
dotnet --version
dotnet restore
dotnet build --configuration Release
```
**Expected**: Successful build with encoding fix packages

### Phase 2: Application Launch and Basic Functionality

#### 2.1 EXE Startup Test
```powershell
cd release
Start-Process -FilePath "TaxDocOrganizer.UI.exe" -PassThru
```
**Expected**: Application window opens without encoding errors

#### 2.2 UI Responsiveness Test
- **Action**: Wait 10 seconds after launch
- **Expected**: Main window fully loaded, no crash dialogs
- **Check**: Window title displays correctly (no encoding garbled text)

### Phase 3: Systematic Encoding Test by Risk Category

#### 3.1 Category A Tests (Highest Risk - Japanese + Spaces + Parentheses)

**Test A1: ダウンロード (2).png**
1. **Drag & Drop**: Drag from `sample/PNG/ダウンロード (2).png` to application
2. **Expected Behavior**: 
   - File loads without "encoding 1512" error
   - Filename displays correctly in UI (Japanese characters preserved)
   - Thumbnail generates successfully
   - File size shows ~10.6MB

**Test A2: ダウンロード (3).png**
1. **Drag & Drop**: Drag from `sample/PNG/ダウンロード (3).png`
2. **Expected**: Same as A1, size ~10.1MB

**Test A3: ダウンロード (4).png**
1. **Drag & Drop**: Drag from `sample/PNG/ダウンロード (4).png`
2. **Expected**: Same as A1, size ~9.1MB

**Test A4: ダウンロード (5).png**
1. **Drag & Drop**: Drag from `sample/PNG/ダウンロード (5).png`
2. **Expected**: Same as A1, size ~11.0MB

#### 3.2 Category B Tests (High Risk - Japanese + Spaces)

**Test B1: 写真 2024-05-14 17 30 29.jpg**
1. **Drag & Drop**: Drag from `sample/JPG/写真 2024-05-14 17 30 29.jpg`
2. **Expected**: 
   - JPG loads successfully
   - Filename with spaces and Japanese chars displayed correctly
   - EXIF data handled properly (iPhone 12 metadata)

**Test B2-B4**: Repeat for other 写真 files
**Test B5**: スクリーンショット PNG file

#### 3.3 Category C Test (Japanese Only)

**Test C1: ダウンロード.png**
1. **Drag & Drop**: Single Japanese word filename
2. **Expected**: Loads without parentheses-related issues

#### 3.4 Category D Tests (English + Parentheses)

**Test D1-D3**: Test IMG_2949(1).jpeg, IMG_2950(1).jpeg, Image_20250210_235730_150 (1).jpeg
1. **Expected**: Parentheses in filenames handled correctly

#### 3.5 Category E Tests (English + Spaces)

**Test E1-E4**: Test orientation PNG files (1- hidari-ping.png, etc.)
1. **Expected**: Space-containing English filenames work normally

### Phase 4: Bulk Load Stress Test

#### 4.1 Multi-File Drag & Drop
1. **Action**: Select all 17 high-risk files simultaneously
2. **Drag & Drop**: All files to application at once
3. **Expected**: 
   - All files load without encoding errors
   - UI remains responsive
   - Memory usage reasonable (<1GB)
   - No application crashes

#### 4.2 Mixed Format Test
1. **Action**: Select 1 file from each format and risk category (5 files total)
2. **Expected**: All formats handled consistently

### Phase 5: PDF Generation Test

#### 5.1 High-Risk File PDF Creation
1. **Load**: ダウンロード (2).png (highest risk)
2. **Action**: Create PDF via UI
3. **Expected**:
   - PDF generates successfully
   - Output filename preserves Japanese characters or converts safely
   - PDF opens correctly in PDF viewer

#### 5.2 Batch PDF Creation
1. **Load**: All Category A files (4 files)
2. **Action**: Create combined PDF
3. **Expected**: Single PDF with all pages, proper ordering

### Phase 6: Error Recovery Testing

#### 6.1 Simulate Encoding Error Conditions
1. **Create**: Temporary file with invalid encoding name
2. **Test**: Application's fallback mechanisms
3. **Expected**: Graceful error handling, no application crash

#### 6.2 Fallback Strategy Verification
1. **Monitor**: Application logs during high-risk file loading
2. **Check**: Which encoding strategy (Primary/Secondary/Tertiary) is used
3. **Expected**: Successful loading through one of the three strategies

## Success Criteria Definition

### Critical Success Factors (Must Pass)
1. **Zero Encoding Errors**: No "encoding 1512" errors during any test
2. **Japanese Character Support**: All Japanese filenames display correctly
3. **File Loading**: All 40 sample files load successfully
4. **UI Stability**: No application crashes during testing
5. **PDF Generation**: High-risk files can be converted to PDF

### Performance Success Factors (Should Pass)
1. **Load Time**: Individual files load within 5 seconds
2. **Memory Usage**: Application uses <1GB RAM with all files loaded
3. **Response Time**: UI remains responsive during file operations

### Usability Success Factors (Nice to Have)
1. **Filename Display**: Special characters render correctly in UI
2. **Error Messages**: User-friendly error messages if issues occur
3. **Progress Indicators**: Loading progress shown for large files

## Test Report Template

### Pre-Test Environment
```yaml
Test Date: [YYYY-MM-DD HH:MM:SS]
Windows Version: [Version]
.NET Version: [Version]
Application Version: TaxDocOrganizer V2.2
Repository Status: [git status output]
Sample Files Count: [40 expected]
```

### Test Execution Results

#### Category A Results (Japanese + Spaces + Parentheses)
```yaml
ダウンロード (2).png:
  status: [PASS/FAIL]
  load_time: [X seconds]
  encoding_strategy: [Primary/Secondary/Tertiary]
  error_details: [if any]

ダウンロード (3).png:
  status: [PASS/FAIL]
  # ... etc for all Category A files
```

#### Summary Results
```yaml
Overall Test Results:
  total_files_tested: 40
  successful_loads: [X/40]
  encoding_errors: [X count]
  application_crashes: [X count]
  pdf_generation_success: [X/tested]
  
Critical Success Factors:
  zero_encoding_errors: [PASS/FAIL]
  japanese_character_support: [PASS/FAIL]
  file_loading_complete: [PASS/FAIL]
  ui_stability: [PASS/FAIL]
  pdf_generation: [PASS/FAIL]

Performance Metrics:
  average_load_time: [X seconds]
  peak_memory_usage: [X MB]
  ui_responsiveness: [Good/Fair/Poor]

Encoding Fix Effectiveness:
  primary_strategy_success_rate: [X%]
  secondary_strategy_usage: [X files]
  tertiary_strategy_usage: [X files]
  
Final Assessment: [PASS/FAIL]
Encoding 1512 Fix Status: [Verified/Needs Improvement]
```

## Risk Mitigation and Troubleshooting

### Common Issues and Solutions

#### Issue 1: "Encoding 1512" Error Still Occurs
**Diagnosis**: Check if System.Text.Encoding.CodePages package is properly loaded
**Solution**: Verify package references and restart application

#### Issue 2: Japanese Characters Display as Squares/Garbled
**Diagnosis**: Font rendering issue, not encoding issue
**Solution**: Verify system has appropriate fonts installed

#### Issue 3: Large Files (>10MB) Cause Memory Issues
**Diagnosis**: Image processing memory pressure
**Solution**: Monitor memory usage, implement streaming if needed

#### Issue 4: Application Crashes on Specific Files
**Diagnosis**: File corruption or unsupported sub-format
**Solution**: Check file integrity with external tools

### Post-Test Actions Required

#### If Tests Pass (Expected Outcome)
1. **Document Results**: Complete test report with all PASS statuses
2. **Archive Test Data**: Save test results for future reference
3. **Update Status**: Mark encoding 1512 fix as verified
4. **Prepare for Production**: Application ready for client deployment

#### If Tests Fail
1. **Error Analysis**: Identify which risk categories failed
2. **Log Collection**: Gather detailed error logs and stack traces
3. **Fallback Strategy**: Determine if additional encoding support needed
4. **Implementation Review**: Review encoding fix implementation for gaps

## Conclusion

This comprehensive test plan provides systematic verification of the encoding 1512 fix across all identified risk factors in the sample data. The 40 sample files represent a realistic cross-section of files that tax professionals would encounter, with particular emphasis on the 17 high-risk files containing Japanese characters and special symbols.

The test plan ensures that the multi-layered encoding fix (Package + Provider + Fallback Strategy) is thoroughly validated before production deployment. Success in these tests will confirm that the TaxDocOrganizer V2.2 application can handle complex filename encodings that previously caused the "encoding 1512" error.

**Next Action**: Execute this test plan immediately after successful Windows MCP build completion.