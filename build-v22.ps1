# DocOrganizer V2.2 高速化版ビルドスクリプト
Write-Host "=== DocOrganizer V2.2 高速化版ビルド開始 ===" -ForegroundColor Green

# 作業ディレクトリ確認
$projectDir = "C:\Users\koki\ezark\standard-image\Standard-image\v2.2-doc-organizer"
Set-Location $projectDir
Write-Host "作業ディレクトリ: $projectDir"

# Git同期
Write-Host "`n[1/5] Git同期中..." -ForegroundColor Yellow
git pull origin main

# クリーンビルド
Write-Host "`n[2/5] プロジェクトクリーニング中..." -ForegroundColor Yellow
dotnet clean

Write-Host "`n[3/5] 依存関係復元中..." -ForegroundColor Yellow
dotnet restore

Write-Host "`n[4/5] リリースビルド実行中..." -ForegroundColor Yellow
dotnet build --configuration Release

Write-Host "`n[5/5] 単一実行ファイル生成中..." -ForegroundColor Yellow
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o release

# 生成確認
Write-Host "`n=== ビルド完了確認 ===" -ForegroundColor Green
$exePath = Join-Path $projectDir "release\DocOrganizer.UI.exe"
if (Test-Path $exePath) {
    $fileInfo = Get-Item $exePath
    Write-Host "✅ EXE生成成功: $exePath" -ForegroundColor Green
    Write-Host "ファイルサイズ: $([math]::Round($fileInfo.Length / 1MB, 1)) MB" -ForegroundColor Cyan
    Write-Host "作成日時: $($fileInfo.LastWriteTime)" -ForegroundColor Cyan
    Write-Host "`n高速化されたDocOrganizer V2.2が正常にビルドされました。" -ForegroundColor Green
    Write-Host "画像読み込みが大幅に高速化され、PDF変換は保存時に実行されます。" -ForegroundColor Cyan
} else {
    Write-Host "❌ EXE生成失敗" -ForegroundColor Red
}