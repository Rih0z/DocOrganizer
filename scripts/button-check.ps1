# DocOrganizer Button Check Script
Write-Host "==================================" -ForegroundColor Cyan
Write-Host "DocOrganizer V2.2 Button Check" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan

# Check EXE
$exePath = Join-Path $PSScriptRoot "..\release\DocOrganizer.exe"
if (-not (Test-Path $exePath)) {
    Write-Host "ERROR: EXE not found: $exePath" -ForegroundColor Red
    exit 1
}

Write-Host "EXE found: $exePath" -ForegroundColor Green

# List of all implemented commands
Write-Host "`nImplemented Commands:" -ForegroundColor Yellow

$commandList = @"
[File Operations]
- OpenCommand (Open PDF/Images)
- SaveCommand (Save)
- SaveAsCommand (Save As)
- CloseCommand (Close)
- ExitCommand (Exit)

[Edit]
- UndoCommand (Undo)
- RedoCommand (Redo)
- SelectAllCommand (Select All)
- DeselectAllCommand (Deselect All)

[Page Operations]
- RotateLeftCommand (Rotate Left - 270 degrees)
- RotateRightCommand (Rotate Right - 90 degrees)
- DeleteCommand (Delete)

[Document Operations]
- MergeCommand (Merge PDFs)
- SplitCommand (Split PDF)
- SecurityCommand (Security Settings)

[View]
- ZoomInCommand (Zoom In)
- ZoomOutCommand (Zoom Out)
- FitToWindowCommand (Fit to Window)
- ThumbnailSmallCommand (Small Thumbnails)
- ThumbnailMediumCommand (Medium Thumbnails)
- ThumbnailLargeCommand (Large Thumbnails)

[Help]
- ShowHelpCommand (Show Help)
- CheckForUpdatesCommand (Check Updates)
- AboutCommand (About)
"@

Write-Host $commandList

# Check ViewModel implementation
Write-Host "`nChecking ViewModel implementation..." -ForegroundColor Yellow
$viewModelPath = Join-Path $PSScriptRoot "..\src\DocOrganizer.UI\ViewModels\V3\MainCompositeViewModel.cs"
# V3アーキテクチャ: MainViewModelは廃止済み"
if (Test-Path $viewModelPath) {
    $content = Get-Content $viewModelPath -Raw
    $relayCommands = ($content | Select-String -Pattern '\[RelayCommand' -AllMatches).Matches.Count
    Write-Host "Found $relayCommands RelayCommand implementations" -ForegroundColor Green
}

# Start application for manual testing
Write-Host "`nStarting DocOrganizer for manual testing..." -ForegroundColor Yellow
Write-Host "Please test each button manually." -ForegroundColor Cyan
Write-Host "IMPORTANT: Do NOT run as administrator!" -ForegroundColor Red

try {
    $proc = Start-Process -FilePath $exePath -PassThru
    Write-Host "Application started (PID: $($proc.Id))" -ForegroundColor Green
    Write-Host "`nPress any key when testing is complete..." -ForegroundColor Yellow
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    
    if (-not $proc.HasExited) {
        $proc.CloseMainWindow()
        Start-Sleep -Seconds 1
        if (-not $proc.HasExited) {
            Stop-Process -Id $proc.Id -Force
        }
    }
} catch {
    Write-Host "ERROR: $_" -ForegroundColor Red
}

Write-Host "`nTest completed!" -ForegroundColor Green