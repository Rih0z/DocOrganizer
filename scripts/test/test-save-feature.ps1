# DocOrganizer V2.2 保存機能テストスクリプト
# 作成日: 2025-01-29

param(
    [string]$ExePath = "..\..\release\DocOrganizer.exe"
)

Write-Host "=== DocOrganizer V2.2 保存機能テスト ===" -ForegroundColor Cyan
Write-Host ""

# EXEパスの確認
$fullPath = (Resolve-Path $ExePath -ErrorAction SilentlyContinue).Path
if (-not $fullPath -or -not (Test-Path $fullPath)) {
    Write-Host "❌ EXEファイルが見つかりません: $ExePath" -ForegroundColor Red
    exit 1
}

Write-Host "✅ EXEファイル確認: $fullPath" -ForegroundColor Green

# outputフォルダの準備
$outputDir = Join-Path (Split-Path $fullPath -Parent) "output"
Write-Host "📁 出力フォルダ: $outputDir" -ForegroundColor Yellow

# テスト用PDFファイルの確認
$testPdf = "..\..\sample\test.pdf"
if (-not (Test-Path $testPdf)) {
    Write-Host "❌ テスト用PDFファイルが見つかりません: $testPdf" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "🧪 テスト手順:" -ForegroundColor Cyan
Write-Host "1. DocOrganizerを起動します"
Write-Host "2. sample\test.pdf を開いてください"
Write-Host "3. 保存ボタン（Ctrl+S）を押してください"
Write-Host "4. outputフォルダに保存されることを確認してください"
Write-Host "5. 名前を付けて保存（Ctrl+Shift+S）も試してください"
Write-Host ""

# アプリケーション起動
Write-Host "🚀 DocOrganizer起動中..." -ForegroundColor Yellow
Start-Process -FilePath $fullPath -PassThru | Out-Null

Write-Host ""
Write-Host "テスト完了後、何かキーを押してください..." -ForegroundColor Cyan
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")

# outputフォルダの内容確認
if (Test-Path $outputDir) {
    Write-Host ""
    Write-Host "📁 outputフォルダの内容:" -ForegroundColor Yellow
    Get-ChildItem $outputDir -Filter "*.pdf" | ForEach-Object {
        Write-Host "  - $($_.Name) ($('{0:N2}' -f ($_.Length / 1MB)) MB)" -ForegroundColor Green
    }
} else {
    Write-Host "⚠️ outputフォルダが作成されていません" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "✅ テスト完了" -ForegroundColor Green