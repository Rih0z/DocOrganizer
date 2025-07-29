# DocOrganizer V2.2 回転修正テストスクリプト
# 作成日: 2025-01-29

param(
    [string]$ExePath = "..\..\release\DocOrganizer.exe"
)

Write-Host "=== DocOrganizer V2.2 回転修正テスト ===" -ForegroundColor Cyan
Write-Host ""

# EXEパスの確認
$fullPath = (Resolve-Path $ExePath -ErrorAction SilentlyContinue).Path
if (-not $fullPath -or -not (Test-Path $fullPath)) {
    Write-Host "❌ EXEファイルが見つかりません: $ExePath" -ForegroundColor Red
    exit 1
}

Write-Host "✅ EXEファイル確認: $fullPath" -ForegroundColor Green

# サンプル画像の確認
$sampleDir = "..\..\sample"
$imageFiles = @()

if (Test-Path $sampleDir) {
    $imageFiles = Get-ChildItem $sampleDir -Include "*.jpg","*.jpeg","*.png","*.heic" -Recurse
    Write-Host "📷 サンプル画像: $($imageFiles.Count) 個" -ForegroundColor Yellow
    foreach ($img in $imageFiles) {
        Write-Host "  - $($img.Name)" -ForegroundColor Gray
    }
} else {
    Write-Host "❌ サンプルフォルダが見つかりません" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "🧪 テスト手順:" -ForegroundColor Cyan
Write-Host "1. DocOrganizerを起動します"
Write-Host "2. 画像ファイルをドラッグ&ドロップしてください"
Write-Host "3. 各ページを回転させてください（右クリック→回転 または ツールバー）"
Write-Host "4. 保存（Ctrl+S）してPDFを出力してください"
Write-Host "5. 出力されたPDFで回転が正しく適用されているか確認してください"
Write-Host ""
Write-Host "⚠️ 注意事項:" -ForegroundColor Yellow
Write-Host "- 縦長の画像が横向きになっていないか"
Write-Host "- 回転させた画像が正しい向きで保存されているか"
Write-Host "- すべての画像が適切なサイズで表示されているか"
Write-Host ""

# アプリケーション起動
Write-Host "🚀 DocOrganizer起動中..." -ForegroundColor Yellow
Start-Process -FilePath $fullPath -PassThru | Out-Null

Write-Host ""
Write-Host "テスト完了後、何かキーを押してください..." -ForegroundColor Cyan
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

# outputフォルダの確認
$outputDir = Join-Path (Split-Path $fullPath -Parent) "output"
if (Test-Path $outputDir) {
    Write-Host ""
    Write-Host "📁 出力されたPDF:" -ForegroundColor Yellow
    $pdfFiles = Get-ChildItem $outputDir -Filter "*.pdf" | Sort-Object LastWriteTime -Descending | Select-Object -First 5
    foreach ($pdf in $pdfFiles) {
        Write-Host "  - $($pdf.Name) ($('{0:N2}' -f ($pdf.Length / 1MB)) MB) - $($pdf.LastWriteTime)" -ForegroundColor Green
    }
    
    if ($pdfFiles.Count -gt 0) {
        Write-Host ""
        Write-Host "最新のPDFを開きますか？ (Y/N)" -ForegroundColor Cyan
        $response = Read-Host
        if ($response -eq 'Y' -or $response -eq 'y') {
            Start-Process $pdfFiles[0].FullName
        }
    }
}

Write-Host ""
Write-Host "✅ テスト完了" -ForegroundColor Green