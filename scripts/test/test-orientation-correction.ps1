# DocOrganizer V2.2 向き自動補正機能テストスクリプト
# 2025-07-24

Write-Host "=== DocOrganizer V2.2 向き自動補正機能テスト開始 ===" -ForegroundColor Green

# 1. EXEファイルの確認
$exePath = "C:\Users\koki\ezark\standard-image\Standard-image\v2.2-doc-organizer\release\DocOrganizer.UI.exe"
$samplePath = "C:\Users\koki\ezark\standard-image\Standard-image\v2.2-doc-organizer\sample"
$logPath = "C:\Users\koki\ezark\standard-image\Standard-image\v2.2-doc-organizer\release\logs"

Write-Host "1. EXEファイル確認中..." -ForegroundColor Yellow
if (Test-Path $exePath) {
    $exeInfo = Get-Item $exePath
    Write-Host "✅ EXEファイル存在: $($exeInfo.Length / 1MB)MB - $($exeInfo.LastWriteTime)" -ForegroundColor Green
} else {
    Write-Host "❌ EXEファイルが見つかりません: $exePath" -ForegroundColor Red
    exit 1
}

# 2. テスト画像ファイル確認
Write-Host "`n2. テスト画像ファイル確認中..." -ForegroundColor Yellow
$testImages = @(
    "$samplePath\1- tate-png.png",      # 縦長画像 - 向き補正のメインテスト
    "$samplePath\1- hidari-ping.png",   # 左向き画像
    "$samplePath\1- migi-png.png",      # 右向き画像
    "$samplePath\JPG\IMG_7347.JPG",     # JPEG画像（EXIFデータ有の可能性）
    "$samplePath\HEIC\IMG_5331.HEIC"    # HEIC画像（EXIFデータ有の可能性）
)

foreach ($image in $testImages) {
    if (Test-Path $image) {
        $imageInfo = Get-Item $image
        Write-Host "✅ $($imageInfo.Name): $($imageInfo.Length / 1KB)KB" -ForegroundColor Green
    } else {
        Write-Host "⚠️  画像ファイル不在: $image" -ForegroundColor Yellow
    }
}

# 3. アプリケーション起動
Write-Host "`n3. DocOrganizerを起動中..." -ForegroundColor Yellow
try {
    # 既存プロセスの確認
    $existingProcess = Get-Process -Name "DocOrganizer.UI" -ErrorAction SilentlyContinue
    if ($existingProcess) {
        Write-Host "既存のプロセスを終了: PID $($existingProcess.Id)" -ForegroundColor Yellow
        $existingProcess | Stop-Process -Force
        Start-Sleep -Seconds 2
    }
    
    # 新しいプロセスを起動
    $process = Start-Process -FilePath $exePath -PassThru -WindowStyle Normal
    Write-Host "✅ アプリケーション起動成功: PID $($process.Id)" -ForegroundColor Green
    Start-Sleep -Seconds 3  # UI初期化の待機
} catch {
    Write-Host "❌ アプリケーション起動失敗: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# 4. ログ監視の準備
Write-Host "`n4. ログ監視開始..." -ForegroundColor Yellow
$currentDate = Get-Date -Format "yyyyMMdd"
$latestLogFile = Get-ChildItem -Path $logPath -Filter "doc-organizer-$currentDate*.txt" | Sort-Object LastWriteTime -Descending | Select-Object -First 1

if ($latestLogFile) {
    Write-Host "監視対象ログファイル: $($latestLogFile.Name)" -ForegroundColor Green
    $initialLogSize = $latestLogFile.Length
} else {
    Write-Host "⚠️  ログファイルが見つかりません" -ForegroundColor Yellow
}

# 5. 手動ドラッグ&ドロップの指示
Write-Host "`n5. === 手動テスト実行 ===" -ForegroundColor Cyan
Write-Host "以下の手順で向き自動補正機能をテストしてください:" -ForegroundColor White
Write-Host ""
Write-Host "📁 テスト用画像の場所:" -ForegroundColor Yellow
Write-Host "   $samplePath" -ForegroundColor Gray
Write-Host ""
Write-Host "🖱️  テスト手順:" -ForegroundColor Yellow
Write-Host "   1. エクスプローラーでサンプルフォルダを開く" -ForegroundColor Gray
Write-Host "   2. '1- tate-png.png' をDocOrganizerにドラッグ&ドロップ" -ForegroundColor Gray
Write-Host "   3. 左側パネルでサムネイルが生成されることを確認" -ForegroundColor Gray
Write-Host "   4. プレビューで画像の向きが補正されていることを確認" -ForegroundColor Gray
Write-Host "   5. 他の画像（JPG、HEIC）も同様にテスト" -ForegroundColor Gray
Write-Host ""
Write-Host "✨ 確認ポイント:" -ForegroundColor Yellow
Write-Host "   • 縦長画像が自動的に右に90度回転（270度補正）されているか" -ForegroundColor Gray
Write-Host "   • EXIFデータがある画像の向きが正しく補正されているか" -ForegroundColor Gray
Write-Host "   • プレビュー表示で補正結果が正しく反映されているか" -ForegroundColor Gray
Write-Host ""

# 6. サンプルフォルダを開く
Write-Host "6. サンプルフォルダを開きます..." -ForegroundColor Yellow
Start-Process explorer $samplePath

# 7. 待機とログ監視
Write-Host "`n7. ログ監視中（30秒間）..." -ForegroundColor Yellow
Write-Host "ドラッグ&ドロップを実行してください..."

for ($i = 30; $i -gt 0; $i--) {
    Write-Host "残り時間: $i 秒" -NoNewline
    Start-Sleep -Seconds 1
    Write-Host "`r" -NoNewline
}

# 8. ログ解析
Write-Host "`n`n8. ログ解析中..." -ForegroundColor Yellow
if ($latestLogFile) {
    $latestLogFile.Refresh()
    if ($latestLogFile.Length -gt $initialLogSize) {
        Write-Host "✅ ログファイルが更新されました" -ForegroundColor Green
        
        # 最後の100行を読み取り
        $logContent = Get-Content $latestLogFile.FullName -Tail 100
        
        # 向き補正関連のログを検索
        $orientationLogs = $logContent | Where-Object { 
            $_ -match "Auto-corrected image orientation" -or 
            $_ -match "rotation" -or 
            $_ -match "aspect ratio" -or
            $_ -match "EXIF" 
        }
        
        if ($orientationLogs) {
            Write-Host "`n🎯 向き自動補正ログ:" -ForegroundColor Green
            foreach ($log in $orientationLogs) {
                Write-Host "   $log" -ForegroundColor White
            }
        } else {
            Write-Host "⚠️  向き補正関連のログが見つかりませんでした" -ForegroundColor Yellow
        }
        
        # エラーログをチェック
        $errorLogs = $logContent | Where-Object { $_ -match "\[ERR\]" -or $_ -match "ERROR" -or $_ -match "Exception" }
        if ($errorLogs) {
            Write-Host "`n❌ エラーログ:" -ForegroundColor Red
            foreach ($error in $errorLogs[-5..-1]) {  # 最後の5個のエラー
                Write-Host "   $error" -ForegroundColor Red
            }
        } else {
            Write-Host "✅ エラーは発生していません" -ForegroundColor Green
        }
    } else {
        Write-Host "⚠️  ログファイルが更新されていません（ドラッグ&ドロップが実行されていない可能性）" -ForegroundColor Yellow
    }
}

# 9. テスト結果サマリー
Write-Host "`n=== テスト結果サマリー ===" -ForegroundColor Cyan
Write-Host "テスト実行時刻: $(Get-Date)" -ForegroundColor White
Write-Host "アプリケーション: DocOrganizer V2.2" -ForegroundColor White
Write-Host "テスト機能: 向き自動補正" -ForegroundColor White
Write-Host ""
Write-Host "次の手順:" -ForegroundColor Yellow
Write-Host "1. アプリケーションで実際にドラッグ&ドロップを実行" -ForegroundColor Gray
Write-Host "2. 向き補正結果を目視で確認" -ForegroundColor Gray
Write-Host "3. ログファイルで補正内容を確認" -ForegroundColor Gray
Write-Host ""
Write-Host "=== テスト完了 ===" -ForegroundColor Green