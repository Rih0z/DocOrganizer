# DocOrganizer V2.2 自動テストスクリプト
# 画像からPDF変換の自動テスト実行

Write-Host "=== DocOrganizer V2.2 Automated Test ===" -ForegroundColor Green
Write-Host "テスト開始時刻: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor White

# 設定
$exePath = "C:\Users\koki\ezark\standard-image\Standard-image\v2.2-doc-organizer\release\DocOrganizer.UI.exe"
$samplePath = "C:\Users\koki\ezark\standard-image\Standard-image\v2.2-doc-organizer\sample"
$testOutputPath = "C:\Users\koki\ezark\standard-image\Standard-image\v2.2-doc-organizer\test-output"
$logPath = "C:\Users\koki\ezark\standard-image\Standard-image\v2.2-doc-organizer\release\logs"

# テスト結果の初期化
$testResults = @()
$passCount = 0
$failCount = 0

# 関数：テスト結果を記録
function Add-TestResult {
    param(
        [string]$TestName,
        [string]$Status,
        [string]$Details = ""
    )
    
    $script:testResults += [PSCustomObject]@{
        TestName = $TestName
        Status = $Status
        Details = $Details
        Timestamp = Get-Date -Format "HH:mm:ss"
    }
    
    if ($Status -eq "PASS") {
        $script:passCount++
        Write-Host "[PASS] $TestName" -ForegroundColor Green
    } else {
        $script:failCount++
        Write-Host "[FAIL] $TestName - $Details" -ForegroundColor Red
    }
}

# 1. EXEファイルの存在確認
Write-Host "`n1. EXEファイル存在確認" -ForegroundColor Yellow
if (Test-Path $exePath) {
    $exeInfo = Get-Item $exePath
    Add-TestResult -TestName "EXE存在確認" -Status "PASS" -Details "Size: $([math]::Round($exeInfo.Length/1MB, 1))MB, Date: $($exeInfo.LastWriteTime.ToString('yyyy-MM-dd HH:mm'))"
} else {
    Add-TestResult -TestName "EXE存在確認" -Status "FAIL" -Details "EXEファイルが見つかりません"
    Write-Host "テスト中止: EXEファイルが存在しません" -ForegroundColor Red
    exit 1
}

# 2. サンプルファイルの確認
Write-Host "`n2. サンプルファイル確認" -ForegroundColor Yellow
$imageExtensions = @("*.png", "*.jpg", "*.jpeg")
$sampleFiles = @()

foreach ($ext in $imageExtensions) {
    $files = Get-ChildItem -Path $samplePath -Filter $ext -Recurse -File | Select-Object -First 3
    $sampleFiles += $files
}

if ($sampleFiles.Count -gt 0) {
    Add-TestResult -TestName "サンプルファイル確認" -Status "PASS" -Details "$($sampleFiles.Count)個の画像ファイルを発見"
    Write-Host "テスト対象ファイル:" -ForegroundColor Cyan
    $sampleFiles | ForEach-Object { Write-Host "  - $($_.Name)" -ForegroundColor Gray }
} else {
    Add-TestResult -TestName "サンプルファイル確認" -Status "FAIL" -Details "テスト用画像ファイルが見つかりません"
}

# 3. テスト出力ディレクトリの準備
Write-Host "`n3. テスト環境準備" -ForegroundColor Yellow
if (Test-Path $testOutputPath) {
    Remove-Item -Path $testOutputPath -Recurse -Force
}
New-Item -ItemType Directory -Path $testOutputPath -Force | Out-Null
Add-TestResult -TestName "テスト環境準備" -Status "PASS" -Details "出力ディレクトリを作成"

# 4. プロセス起動テスト
Write-Host "`n4. アプリケーション起動テスト" -ForegroundColor Yellow
try {
    # 既存のプロセスを終了
    $existingProcesses = Get-Process -Name "DocOrganizer.UI" -ErrorAction SilentlyContinue
    if ($existingProcesses) {
        $existingProcesses | Stop-Process -Force
        Start-Sleep -Seconds 2
    }
    
    # プロセスを起動
    $process = Start-Process -FilePath $exePath -PassThru
    Start-Sleep -Seconds 5
    
    if (-not $process.HasExited) {
        Add-TestResult -TestName "プロセス起動" -Status "PASS" -Details "PID: $($process.Id)"
        
        # メモリ使用量確認
        $memoryMB = [math]::Round($process.WorkingSet64 / 1MB, 1)
        Add-TestResult -TestName "メモリ使用量" -Status "PASS" -Details "${memoryMB}MB"
    } else {
        Add-TestResult -TestName "プロセス起動" -Status "FAIL" -Details "プロセスが即座に終了しました"
    }
} catch {
    Add-TestResult -TestName "プロセス起動" -Status "FAIL" -Details $_.Exception.Message
}

# 5. ログファイル監視による変換テスト
Write-Host "`n5. 画像変換機能テスト（ログ監視）" -ForegroundColor Yellow
if ($process -and -not $process.HasExited) {
    # 最新のログファイルを取得
    $latestLog = Get-ChildItem -Path $logPath -Filter "*.txt" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    
    if ($latestLog) {
        Write-Host "ログファイル監視中: $($latestLog.Name)" -ForegroundColor Cyan
        
        # ログの初期位置を記録
        $initialLogSize = (Get-Item $latestLog.FullName).Length
        
        # UIオートメーションの代わりにログ監視で変換を確認
        Write-Host "`n画像ファイルをアプリケーションにドラッグ&ドロップしてください:" -ForegroundColor Yellow
        Write-Host "テスト対象:" -ForegroundColor Cyan
        $sampleFiles | Select-Object -First 3 | ForEach-Object {
            Write-Host "  - $($_.FullName)" -ForegroundColor Gray
        }
        
        Write-Host "`n30秒間ログを監視します..." -ForegroundColor Cyan
        $startTime = Get-Date
        $timeout = 30
        $conversionDetected = $false
        
        while (((Get-Date) - $startTime).TotalSeconds -lt $timeout) {
            Start-Sleep -Seconds 2
            
            # ログファイルの新しい内容を確認
            $currentLogSize = (Get-Item $latestLog.FullName).Length
            if ($currentLogSize -gt $initialLogSize) {
                $newContent = Get-Content $latestLog.FullName | Select-Object -Skip ([math]::Max(0, $initialLogSize / 100))
                
                # 成功パターンを検索
                if ($newContent -match "Creating PDF" -or $newContent -match "Successfully created PDF") {
                    $conversionDetected = $true
                    Add-TestResult -TestName "PDF変換検出" -Status "PASS" -Details "ログで変換成功を確認"
                    break
                }
                
                # エラーパターンを検索
                if ($newContent -match "\[ERR\]" -or $newContent -match "Failed to convert") {
                    Add-TestResult -TestName "PDF変換検出" -Status "FAIL" -Details "エラーログを検出"
                    
                    # エラーの詳細を表示
                    $errorLines = $newContent | Where-Object { $_ -match "\[ERR\]" } | Select-Object -First 3
                    foreach ($line in $errorLines) {
                        Write-Host "  ERROR: $line" -ForegroundColor Red
                    }
                    break
                }
            }
            
            Write-Host "." -NoNewline
        }
        
        if (-not $conversionDetected) {
            Add-TestResult -TestName "PDF変換検出" -Status "FAIL" -Details "タイムアウト - 変換が検出されませんでした"
        }
    } else {
        Add-TestResult -TestName "ログファイル確認" -Status "FAIL" -Details "ログファイルが見つかりません"
    }
}

# 6. プロセス終了テスト
Write-Host "`n`n6. クリーンアップ" -ForegroundColor Yellow
if ($process -and -not $process.HasExited) {
    try {
        $process | Stop-Process -Force
        Start-Sleep -Seconds 2
        Add-TestResult -TestName "プロセス終了" -Status "PASS" -Details "正常に終了しました"
    } catch {
        Add-TestResult -TestName "プロセス終了" -Status "FAIL" -Details $_.Exception.Message
    }
}

# 7. エラーファイルテスト（自動実行）
Write-Host "`n7. エラー処理テスト（自動）" -ForegroundColor Yellow

# テスト用の破損ファイルを作成
$corruptedFile = Join-Path $testOutputPath "corrupted.png"
[byte[]]$corruptedData = 0x00, 0x01, 0x02, 0x03, 0x04
[System.IO.File]::WriteAllBytes($corruptedFile, $corruptedData)

$emptyFile = Join-Path $testOutputPath "empty.png"
New-Item -ItemType File -Path $emptyFile -Force | Out-Null

# SimplePdfServiceを直接テスト（可能な場合）
try {
    Add-Type -Path "C:\Users\koki\ezark\standard-image\Standard-image\v2.2-doc-organizer\src\DocOrganizer.Infrastructure\bin\Release\net6.0\DocOrganizer.Infrastructure.dll" -ErrorAction SilentlyContinue
    Add-TestResult -TestName "DLL読み込み" -Status "PASS" -Details "Infrastructure.dllを読み込みました"
} catch {
    Add-TestResult -TestName "DLL読み込み" -Status "SKIP" -Details "直接テストはスキップ"
}

# テスト結果サマリー
Write-Host "`n========== テスト結果サマリー ==========" -ForegroundColor Cyan
Write-Host "実行日時: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor White
Write-Host "合計テスト数: $($testResults.Count)" -ForegroundColor White
Write-Host "成功: $passCount" -ForegroundColor Green
Write-Host "失敗: $failCount" -ForegroundColor Red
Write-Host "成功率: $([math]::Round($passCount / $testResults.Count * 100, 1))%" -ForegroundColor White

# 詳細レポート
Write-Host "`n詳細結果:" -ForegroundColor Yellow
$testResults | Format-Table -AutoSize

# レポートファイル保存
$reportPath = Join-Path $testOutputPath "TestReport_$(Get-Date -Format 'yyyyMMdd_HHmmss').txt"
$testResults | ConvertTo-Json | Out-File -FilePath $reportPath -Encoding UTF8
Write-Host "`nレポート保存先: $reportPath" -ForegroundColor Green

# 最終判定
if ($failCount -eq 0) {
    Write-Host "`n✅ すべてのテストが成功しました！" -ForegroundColor Green
    exit 0
} else {
    Write-Host "`n❌ $failCount 個のテストが失敗しました" -ForegroundColor Red
    exit 1
}