# TaxDocOrganizer.UI.exe Startup Crash Investigation Report

**Investigation Date**: 2025-07-22  
**Target Application**: TaxDocOrganizer V2.2  
**Investigator**: Claude AI Assistant  
**Status**: Critical Startup Issues Identified

---

## Executive Summary

The TaxDocOrganizer.UI.exe application is experiencing immediate startup crashes. Based on comprehensive code analysis, I have identified **7 critical issues** that would prevent the application from starting successfully. These range from dependency injection configuration problems to missing image resources.

## Critical Issues Identified

### 1. **CRITICAL: Dependency Injection Circular Dependency**
**Severity**: 🔴 Critical - Application Cannot Start  
**Location**: `App.xaml.cs`, lines 52-61  
**Issue**: The `App.xaml` declares `StartupUri="Views/MainWindow.xaml"`, but `App.xaml.cs` manually creates and shows MainWindow in `OnStartup()`. This creates a circular initialization.

**Evidence**:
```csharp
// App.xaml declares automatic startup
StartupUri="Views/MainWindow.xaml"

// But App.xaml.cs manually creates window
protected override async void OnStartup(StartupEventArgs e)
{
    await _host.StartAsync();
    var mainWindow = _host.Services.GetRequiredService<MainWindow>();
    mainWindow.DataContext = _host.Services.GetRequiredService<MainViewModel>();
    mainWindow.Show(); // This conflicts with automatic startup
    base.OnStartup(e);
}
```

### 2. **CRITICAL: Missing Images Directory**
**Severity**: 🔴 Critical - UI Binding Failures  
**Location**: `MainWindow.xaml`, lines 38-155  
**Issue**: XAML references image files in `/Images/` directory that don't exist in the project structure.

**Missing Image References**:
- `/Images/open.png`
- `/Images/save.png` 
- `/Images/rotate_left.png`
- `/Images/rotate_right.png`
- `/Images/delete.png`
- `/Images/merge.png`
- `/Images/split.png`
- `/Images/zoom_in.png`
- `/Images/zoom_out.png`

### 3. **HIGH: Serilog Directory Creation Issue**
**Severity**: 🟠 High - Logging Initialization Failure  
**Location**: `App.xaml.cs`, lines 27-32  
**Issue**: Serilog configured to write to `"logs/taxdoc-organizer-.txt"` but directory doesn't exist and isn't created.

**Evidence**:
```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.File("logs/taxdoc-organizer-.txt", // Directory "logs/" must exist
        rollingInterval: RollingInterval.Day,
        // ... configuration
```

### 4. **HIGH: MainViewModel Binding Commands Don't Exist**
**Severity**: 🟠 High - UI Command Binding Failures  
**Location**: `MainWindow.xaml`, lines 13-22, 54-55  
**Issue**: XAML binds to commands that don't exist in `MainViewModel.cs`.

**Missing Commands**:
- `UndoCommand` / `RedoCommand`
- `ShowHelpCommand`
- `ZoomInCommand` / `ZoomOutCommand` / `FitToWindowCommand`
- `ThumbnailSmallCommand` / `ThumbnailMediumCommand` / `ThumbnailLargeCommand`
- `SecurityCommand`

### 5. **MEDIUM: PDF Service Dependencies Missing Implementation**
**Severity**: 🟡 Medium - Runtime Failures  
**Location**: `PdfService.cs`, lines 183-226  
**Issue**: `ExtractPageThumbnailAsync()` and `ExtractPagePreviewAsync()` create placeholder bitmaps but don't actually extract from PDF content.

### 6. **MEDIUM: PageViewModel Thumbnail Loading Issues**
**Severity**: 🟡 Medium - UI Display Problems  
**Location**: `PageViewModel.cs`, lines 38-43  
**Issue**: `LoadThumbnail()` method has placeholder implementation that sets `ThumbnailImage = null`.

**Evidence**:
```csharp
private void LoadThumbnail()
{
    // プレースホルダー実装
    // 実際にはPDFページのサムネイルを生成
    ThumbnailImage = null; // This will cause empty thumbnails
}
```

### 7. **LOW: Infrastructure Package Version Conflicts**
**Severity**: 🟢 Low - Potential Runtime Issues  
**Location**: `TaxDocOrganizer.Infrastructure.csproj`  
**Issue**: Multiple imaging libraries may have conflicting dependencies.

**Packages**:
- PdfSharp v1.50.5147
- SkiaSharp v2.88.6  
- SixLabors.ImageSharp v3.1.10
- Magick.NET-Q16-AnyCPU v13.5.0

---

## Startup Failure Analysis

### Most Likely Crash Sequence:
1. **Application starts** with WPF initialization
2. **App.xaml.cs constructor** runs, creates DI host
3. **WPF tries to auto-create MainWindow** due to `StartupUri`
4. **MainWindow XAML parsing fails** due to missing image resources
5. **OR** Serilog initialization fails due to missing logs directory  
6. **OR** DI circular dependency causes initialization deadlock
7. **Application crashes** before showing UI

### Error Types Expected:
- `System.IO.DirectoryNotFoundException` - logs directory
- `System.IO.FileNotFoundException` - missing image files
- `System.InvalidOperationException` - DI circular reference
- `XamlParseException` - missing image resources in XAML
- `NullReferenceException` - command binding failures

---

## Recommended Fixes (Priority Order)

### 🔴 **IMMEDIATE FIXES REQUIRED**

#### Fix 1: Resolve DI Circular Dependency
```xml
<!-- App.xaml - REMOVE StartupUri -->
<Application x:Class="TaxDocOrganizer.UI.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <!-- Remove: StartupUri="Views/MainWindow.xaml" -->
</Application>
```

#### Fix 2: Create Missing Directories and Placeholder Images
```powershell
# Windows MCP Commands to create structure
New-Item -ItemType Directory -Path "logs" -Force
New-Item -ItemType Directory -Path "src/TaxDocOrganizer.UI/Images" -Force

# Create minimal placeholder PNG files or remove image references
```

#### Fix 3: Fix Serilog Directory Creation
```csharp
// App.xaml.cs - Create logs directory before Serilog initialization
var logsDir = "logs";
if (!Directory.Exists(logsDir))
{
    Directory.CreateDirectory(logsDir);
}

Log.Logger = new LoggerConfiguration()
    .WriteTo.File(Path.Combine(logsDir, "taxdoc-organizer-.txt"), 
        // ... rest of configuration
```

### 🟠 **HIGH PRIORITY FIXES**

#### Fix 4: Add Missing RelayCommand Properties
```csharp
// MainViewModel.cs - Add missing commands
[RelayCommand] private void Undo() { /* Implementation */ }
[RelayCommand] private void Redo() { /* Implementation */ }
[RelayCommand] private void ShowHelp() { /* Implementation */ }
[RelayCommand] private void ZoomIn() { /* Implementation */ }
[RelayCommand] private void ZoomOut() { /* Implementation */ }
// ... etc for all missing commands
```

#### Fix 5: Implement Basic Thumbnail Loading
```csharp
// PageViewModel.cs - Basic thumbnail implementation
private void LoadThumbnail()
{
    // Create a basic placeholder bitmap instead of null
    var placeholder = new BitmapImage(new Uri("pack://application:,,,/Images/page_placeholder.png"));
    ThumbnailImage = placeholder;
}
```

---

## Windows MCP Execution Plan

### Step 1: Execute Diagnostic Script
```powershell
# Connect to Windows MCP: http://100.71.150.41:8080
cd C:\builds\Standard-image-repo\v2.2-taxdoc-organizer
.\windows-crash-investigation.ps1
```

### Step 2: Create Required Directories
```powershell
New-Item -ItemType Directory -Path "logs" -Force
New-Item -ItemType Directory -Path "src\TaxDocOrganizer.UI\Images" -Force
```

### Step 3: Test Startup After Fixes
```powershell
# After applying fixes, test startup:
dotnet build src\TaxDocOrganizer.UI -c Release
dotnet publish src\TaxDocOrganizer.UI -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o release-fixed
.\release-fixed\TaxDocOrganizer.UI.exe
```

---

## Success Criteria

### ✅ **Startup Success Indicators**:
- Application window appears without crash
- No immediate exceptions in Windows Event Log  
- MainWindow shows with empty state message
- Status bar shows "準備完了" (Ready) message
- Menu and toolbar buttons are visible (even if not functional)

### ⚠️ **Acceptable Warnings**:
- Missing image icons (can be placeholder boxes)
- Non-functional commands (until implementation)
- Empty PDF preview area

---

## Investigation Tools Provided

1. **`windows-crash-investigation.ps1`** - Comprehensive diagnostic script
2. **This report** - Detailed analysis and fix recommendations
3. **Windows MCP execution plan** - Step-by-step investigation process

---

## Conclusion

The TaxDocOrganizer V2.2 application has **fundamental architectural issues** preventing startup. The primary causes are:

1. **DI configuration conflicts** (circular dependency)
2. **Missing file resources** (images directory, logs directory)  
3. **Incomplete command implementations** (XAML binding failures)

**Estimated Fix Time**: 2-4 hours for basic startup functionality  
**Risk Level**: 🔴 Critical - Application completely non-functional  
**Recommended Action**: Apply immediate fixes before any further development

The provided diagnostic script and fix recommendations should resolve the startup crash and allow the application to launch successfully, even if some features remain incomplete.

---

**Report Status**: ✅ COMPLETED  
**Next Action**: Execute Windows MCP investigation script for confirmation