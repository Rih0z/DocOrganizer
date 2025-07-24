# Check EXE details
$exePath = "C:\Users\koki\ezark\standard-image\Standard-image\v2.2-doc-organizer\release\DocOrganizer.UI.exe"
if (Test-Path $exePath) {
    $exe = Get-Item $exePath
    $sizeMB = [math]::Round($exe.Length / 1MB, 1)
    $created = $exe.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
    
    Write-Host ""
    Write-Host "=====================================" -ForegroundColor Green
    Write-Host "DocOrganizer V2.2 Build Complete" -ForegroundColor Green
    Write-Host "=====================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Full Path: $($exe.FullName)" -ForegroundColor Cyan
    Write-Host "Size: ${sizeMB}MB" -ForegroundColor Cyan
    Write-Host "Created: $created" -ForegroundColor Cyan
    Write-Host ""
} else {
    Write-Host "EXE not found at: $exePath" -ForegroundColor Red
}