# DocOrganizer.exe 起動テストスクリプト
$exePath = Join-Path $PSScriptRoot "..\release\DocOrganizer.exe"

# EXEファイルの存在確認
if (Test-Path $exePath) {
    Write-Host "✅ EXEファイル発見: $exePath" -ForegroundColor Green
    
    # ファイル情報表示
    $fileInfo = Get-Item $exePath
    Write-Host "📋 ファイル情報:"
    Write-Host "   - フルパス: $($fileInfo.FullName)"
    Write-Host "   - サイズ: $([Math]::Round($fileInfo.Length / 1MB, 2)) MB"
    Write-Host "   - 作成日時: $($fileInfo.CreationTime)"
    Write-Host ""
    
    # 起動テスト
    Write-Host "🚀 DocOrganizer.exe を起動しています..." -ForegroundColor Yellow
    try {
        $process = Start-Process -FilePath $exePath -PassThru
        Write-Host "✅ プロセスID: $($process.Id) - 起動成功" -ForegroundColor Green
        
        # 5秒待機
        Write-Host "⏱️  5秒間動作確認中..."
        Start-Sleep -Seconds 5
        
        # プロセス状態確認
        if (-not $process.HasExited) {
            Write-Host "✅ アプリケーションは正常に動作しています" -ForegroundColor Green
            Write-Host "🛑 テスト完了のため終了します..."
            Stop-Process -Id $process.Id -Force
            Write-Host "✅ テスト完了" -ForegroundColor Green
        } else {
            Write-Host "アプリケーションが予期せず終了しました" -ForegroundColor Red
            Write-Host "   終了コード: $($process.ExitCode)"
        }
    } catch {
        Write-Host "起動エラー: $_" -ForegroundColor Red
    }
} else {
    Write-Host "EXE file not found: $exePath" -ForegroundColor Red
}