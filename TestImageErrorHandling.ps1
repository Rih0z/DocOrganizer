# ImageProcessingService Error Handling Test
# ファイルが開けない場合の処理を検証

Write-Host "=== ImageProcessingService Error Handling Test ===" -ForegroundColor Green

# テスト用のファイルを準備
$testPath = "C:\Users\koki\ezark\standard-image\Standard-image\v2.2-doc-organizer\test-error-handling"
if (Test-Path $testPath) {
    Remove-Item -Path $testPath -Recurse -Force
}
New-Item -ItemType Directory -Path $testPath -Force | Out-Null

# 1. 存在しないファイル
$nonExistentFile = "$testPath\non_existent.png"

# 2. 空のファイル
$emptyFile = "$testPath\empty.png"
New-Item -ItemType File -Path $emptyFile -Force | Out-Null

# 3. 破損したファイル（不正なデータ）
$corruptedFile = Join-Path $testPath "corrupted.png"
[byte[]]$corruptedData = 0x00, 0x01, 0x02, 0x03, 0x04
[System.IO.File]::WriteAllBytes($corruptedFile, $corruptedData)

# 4. テキストファイル（画像でない）
$textFile = Join-Path $testPath "text.txt"
"This is not an image" | Out-File -FilePath $textFile

# 5. PDFファイルをPNGとして偽装
$fakePng = Join-Path $testPath "fake.png"
[byte[]]$pdfHeader = 0x25, 0x50, 0x44, 0x46  # %PDF
[System.IO.File]::WriteAllBytes($fakePng, $pdfHeader)

Write-Host "`nTest files created:" -ForegroundColor Yellow
Get-ChildItem $testPath | Select-Object Name, Length | Format-Table

Write-Host "`n--- Expected Behavior ---" -ForegroundColor Cyan
Write-Host "1. Non-existent file: Should fail quickly with NotSupportedException"
Write-Host "2. Empty file: Should fail quickly with NotSupportedException"
Write-Host "3. Corrupted file: Should fail after trying 3 methods with clear error"
Write-Host "4. Text file: Should fail quickly with NotSupportedException"
Write-Host "5. Fake PNG (PDF): Should fail quickly with NotSupportedException"

Write-Host "`n--- Actual Test ---" -ForegroundColor Cyan
Write-Host "Launch DocOrganizer.UI.exe and drag these files:" -ForegroundColor Yellow
Write-Host "Path: $testPath" -ForegroundColor White

Write-Host "`nPress Enter when ready to clean up test files..."
Read-Host

# クリーンアップ
Remove-Item -Path $testPath -Recurse -Force
Write-Host "Test files cleaned up." -ForegroundColor Green