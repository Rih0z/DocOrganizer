# テストデバッグスクリプト
# エラーログを確認するため、コンソールウィンドウで実行

Write-Host "DocOrganizer V2.2 デバッグモード起動中..." -ForegroundColor Yellow
Write-Host "ドラッグ&ドロップのエラーを確認します" -ForegroundColor Yellow
Write-Host ""

# アプリケーションを起動
$exePath = ".\release\DocOrganizer.UI.exe"

if (Test-Path $exePath) {
    Write-Host "EXEファイルを起動します: $exePath" -ForegroundColor Green
    Write-Host "デバッグ出力を監視中..." -ForegroundColor Cyan
    Write-Host ""
    
    # アプリケーションを起動してデバッグ出力を表示
    & $exePath
    
} else {
    Write-Host "エラー: EXEファイルが見つかりません" -ForegroundColor Red
    Write-Host "パス: $exePath"
}

Write-Host ""
Write-Host "終了しました" -ForegroundColor Yellow
Read-Host "Enterキーで終了"