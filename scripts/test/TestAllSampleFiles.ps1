# 全サンプルファイルのテスト
Write-Host "=== DocOrganizer V2.2 全サンプルファイルテスト ===" -ForegroundColor Cyan
Write-Host ""

# アプリケーションが起動しているか確認
$process = Get-Process -Name "DocOrganizer.UI" -ErrorAction SilentlyContinue
if (-not $process) {
    Write-Host "アプリケーションを起動します..." -ForegroundColor Yellow
    $exePath = "release\DocOrganizer.UI.exe"
    if (Test-Path $exePath) {
        Start-Process -FilePath $exePath
        Start-Sleep -Seconds 3
        Write-Host "✅ アプリケーションを起動しました" -ForegroundColor Green
    } else {
        Write-Host "❌ エラー: $exePath が見つかりません" -ForegroundColor Red
        exit 1
    }
} else {
    Write-Host "✅ アプリケーションは既に起動しています (PID: $($process.Id))" -ForegroundColor Green
}

Write-Host ""
Write-Host "利用可能なサンプルファイル:" -ForegroundColor Yellow
Write-Host ""

# PDFファイル
Write-Host "[PDFファイル]" -ForegroundColor Cyan
if (Test-Path "test1.pdf") { Write-Host "  - test1.pdf" }
if (Test-Path "test2.pdf") { Write-Host "  - test2.pdf" }

# PNG画像
Write-Host ""
Write-Host "[PNG画像]" -ForegroundColor Cyan
if (Test-Path "sample\PNG") {
    $pngFiles = Get-ChildItem "sample\PNG\*.png"
    $pngFiles | ForEach-Object { Write-Host "  - $($_.FullName)" }
    Write-Host "  合計: $($pngFiles.Count) ファイル" -ForegroundColor Yellow
}

# JPG画像
Write-Host ""
Write-Host "[JPG画像]" -ForegroundColor Cyan
if (Test-Path "sample\JPG") {
    $jpgFiles = Get-ChildItem "sample\JPG\*.jpg", "sample\JPG\*.JPG"
    $jpgFiles | ForEach-Object { Write-Host "  - $($_.FullName)" }
    Write-Host "  合計: $($jpgFiles.Count) ファイル" -ForegroundColor Yellow
}

# JPEG画像
Write-Host ""
Write-Host "[JPEG画像]" -ForegroundColor Cyan
if (Test-Path "sample\jpeg") {
    $jpegFiles = Get-ChildItem "sample\jpeg\*.jpeg"
    $jpegFiles | ForEach-Object { Write-Host "  - $($_.FullName)" }
    Write-Host "  合計: $($jpegFiles.Count) ファイル" -ForegroundColor Yellow
}

# HEIC画像
Write-Host ""
Write-Host "[HEIC画像]" -ForegroundColor Cyan
if (Test-Path "sample\HEIC") {
    $heicFiles = Get-ChildItem "sample\HEIC\*.heic", "sample\HEIC\*.HEIC"
    $heicFiles | ForEach-Object { Write-Host "  - $($_.FullName)" }
    Write-Host "  合計: $($heicFiles.Count) ファイル" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== テスト手順 ===" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. 個別ファイルテスト:" -ForegroundColor Cyan
Write-Host "   - 各種類のファイルを1つずつドラッグアンドドロップ"
Write-Host "   - エラーが表示されるファイルを記録"
Write-Host ""
Write-Host "2. 複数ファイルテスト:" -ForegroundColor Cyan
Write-Host "   - 同じ種類の複数ファイルを選択してドラッグアンドドロップ"
Write-Host "   - 異なる種類の複数ファイルを選択してドラッグアンドドロップ"
Write-Host ""
Write-Host "3. フォルダテスト:" -ForegroundColor Cyan
Write-Host "   - sample\PNG フォルダ全体をドラッグアンドドロップ"
Write-Host "   - sample\JPG フォルダ全体をドラッグアンドドロップ"
Write-Host "   - sample フォルダ全体をドラッグアンドドロップ"
Write-Host ""
Write-Host "4. エラー確認:" -ForegroundColor Cyan
Write-Host "   - 読み込めなかったファイルのエラーメッセージを確認"
Write-Host "   - 'missing method' エラーが出る場合は詳細を記録"
Write-Host ""
Write-Host "=== 自動テスト用コマンド ===" -ForegroundColor Green
Write-Host ""
Write-Host "# 全PNGファイルをエクスプローラーで開く（手動ドラッグ用）"
Write-Host 'explorer.exe "sample\PNG"' -ForegroundColor Gray
Write-Host ""
Write-Host "# 全JPGファイルをエクスプローラーで開く（手動ドラッグ用）"
Write-Host 'explorer.exe "sample\JPG"' -ForegroundColor Gray
Write-Host ""
Write-Host "テスト開始前に、任意のキーを押してください..." -ForegroundColor Yellow
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")