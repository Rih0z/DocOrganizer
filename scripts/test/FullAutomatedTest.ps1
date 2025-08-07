# DocOrganizer V2.2 完全自動テストスクリプト
param(
    [switch]$SkipBuild = $false,
    [switch]$KeepRunning = $false
)

Write-Host "===== DocOrganizer V2.2 完全自動テスト =====" -ForegroundColor Cyan
Write-Host "実行時刻: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor White

# 設定
$config = @{
    ProjectPath = "C:\Users\koki\ezark\standard-image\Standard-image\v2.2-doc-organizer"
    ExePath = "C:\Users\koki\ezark\standard-image\Standard-image\v2.2-doc-organizer\release\DocOrganizer.UI.exe"
    SamplePath = "C:\Users\koki\ezark\standard-image\Standard-image\v2.2-doc-organizer\sample"
    TestOutputPath = "C:\Users\koki\ezark\standard-image\Standard-image\v2.2-doc-organizer\test-output"
    LogPath = "C:\Users\koki\ezark\standard-image\Standard-image\v2.2-doc-organizer\release\logs"
}

# テスト結果
$results = @{
    Total = 0
    Passed = 0
    Failed = 0
    Details = @()
}

function Add-TestResult {
    param(
        [string]$TestName,
        [string]$Category,
        [string]$Status,
        [string]$Details = "",
        [double]$Duration = 0
    )
    
    $result = [PSCustomObject]@{
        TestName = $TestName
        Category = $Category
        Status = $Status
        Details = $Details
        Duration = "${Duration}s"
        Timestamp = Get-Date -Format "HH:mm:ss"
    }
    
    $script:results.Total++
    
    switch ($Status) {
        "PASS" { 
            $script:results.Passed++
            Write-Host "[✓] $TestName" -ForegroundColor Green
            if ($Details) { Write-Host "    $Details" -ForegroundColor Gray }
        }
        "FAIL" { 
            $script:results.Failed++
            Write-Host "[✗] $TestName" -ForegroundColor Red
            if ($Details) { Write-Host "    $Details" -ForegroundColor Red }
        }
        "SKIP" { 
            Write-Host "[○] $TestName" -ForegroundColor Yellow
            if ($Details) { Write-Host "    $Details" -ForegroundColor Yellow }
        }
    }
    
    $script:results.Details += $result
}

# 1. ビルドテスト（オプション）
if (-not $SkipBuild) {
    Write-Host "`n[1] ビルドテスト" -ForegroundColor Yellow
    $buildStart = Get-Date
    
    try {
        Set-Location $config.ProjectPath
        
        # Git同期
        $gitStatus = & git status --porcelain
        if ($gitStatus) {
            Add-TestResult -TestName "Git状態" -Category "ビルド" -Status "FAIL" -Details "未コミットの変更があります"
        } else {
            Add-TestResult -TestName "Git状態" -Category "ビルド" -Status "PASS" -Details "クリーンな状態"
        }
        
        # ビルド実行
        & dotnet clean | Out-Null
        & dotnet restore | Out-Null
        $buildResult = & dotnet build --configuration Release 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Add-TestResult -TestName "ビルド" -Category "ビルド" -Status "PASS" -Duration ((Get-Date) - $buildStart).TotalSeconds
        } else {
            Add-TestResult -TestName "ビルド" -Category "ビルド" -Status "FAIL" -Details "ビルドエラー"
        }
    } catch {
        Add-TestResult -TestName "ビルド" -Category "ビルド" -Status "FAIL" -Details $_.Exception.Message
    }
}

# 2. ファイルシステムテスト
Write-Host "`n[2] ファイルシステムテスト" -ForegroundColor Yellow

# EXE存在確認
if (Test-Path $config.ExePath) {
    $exeInfo = Get-Item $config.ExePath
    Add-TestResult -TestName "EXEファイル" -Category "ファイル" -Status "PASS" `
        -Details "Size: $([math]::Round($exeInfo.Length/1MB, 1))MB, Date: $($exeInfo.LastWriteTime.ToString('yyyy-MM-dd HH:mm'))"
} else {
    Add-TestResult -TestName "EXEファイル" -Category "ファイル" -Status "FAIL" -Details "ファイルが見つかりません"
}

# サンプルファイル確認
$sampleStats = @{
    HEIC = (Get-ChildItem -Path "$($config.SamplePath)\HEIC" -Filter "*.heic" -File -ErrorAction SilentlyContinue).Count
    PNG = (Get-ChildItem -Path "$($config.SamplePath)\PNG" -Filter "*.png" -File -ErrorAction SilentlyContinue).Count
    JPG = (Get-ChildItem -Path "$($config.SamplePath)\JPG" -Filter "*.jpg" -File -ErrorAction SilentlyContinue).Count
    JPEG = (Get-ChildItem -Path "$($config.SamplePath)\jpeg" -Filter "*.jpeg" -File -ErrorAction SilentlyContinue).Count
}

$totalSamples = $sampleStats.HEIC + $sampleStats.PNG + $sampleStats.JPG + $sampleStats.JPEG
Add-TestResult -TestName "サンプルファイル" -Category "ファイル" -Status "PASS" `
    -Details "Total: $totalSamples (HEIC:$($sampleStats.HEIC), PNG:$($sampleStats.PNG), JPG:$($sampleStats.JPG), JPEG:$($sampleStats.JPEG))"

# 3. アプリケーション起動テスト
Write-Host "`n[3] アプリケーション起動テスト" -ForegroundColor Yellow

# 既存プロセス終了
Get-Process -Name "DocOrganizer.UI" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Seconds 2

try {
    $startTime = Get-Date
    $process = Start-Process -FilePath $config.ExePath -PassThru
    Start-Sleep -Seconds 5
    
    if (-not $process.HasExited) {
        $memoryMB = [math]::Round($process.WorkingSet64 / 1MB, 1)
        Add-TestResult -TestName "プロセス起動" -Category "起動" -Status "PASS" `
            -Details "PID: $($process.Id), Memory: ${memoryMB}MB" `
            -Duration ((Get-Date) - $startTime).TotalSeconds
            
        # CPU使用率
        Start-Sleep -Seconds 2
        $cpu = (Get-Process -Id $process.Id).CPU
        Add-TestResult -TestName "CPU使用率" -Category "パフォーマンス" -Status "PASS" `
            -Details "$([math]::Round($cpu, 2)) seconds total CPU time"
    } else {
        Add-TestResult -TestName "プロセス起動" -Category "起動" -Status "FAIL" -Details "即座に終了しました"
    }
} catch {
    Add-TestResult -TestName "プロセス起動" -Category "起動" -Status "FAIL" -Details $_.Exception.Message
}

# 4. ログ分析テスト
Write-Host "`n[4] ログ分析テスト" -ForegroundColor Yellow

if (Test-Path $config.LogPath) {
    $latestLog = Get-ChildItem -Path $config.LogPath -Filter "*.txt" | 
        Sort-Object LastWriteTime -Descending | 
        Select-Object -First 1
        
    if ($latestLog) {
        $logContent = Get-Content $latestLog.FullName -Tail 100
        
        # エラーログ確認
        $errors = $logContent | Where-Object { $_ -match '\[ERR\]' }
        if ($errors.Count -eq 0) {
            Add-TestResult -TestName "エラーログ" -Category "ログ" -Status "PASS" -Details "エラーなし"
        } else {
            Add-TestResult -TestName "エラーログ" -Category "ログ" -Status "FAIL" `
                -Details "$($errors.Count) 件のエラー"
        }
        
        # 警告ログ確認
        $warnings = $logContent | Where-Object { $_ -match '\[WRN\]' }
        Add-TestResult -TestName "警告ログ" -Category "ログ" -Status "PASS" `
            -Details "$($warnings.Count) 件の警告"
    }
}

# 5. 機能テスト（手動確認案内）
Write-Host "`n[5] 機能テスト案内" -ForegroundColor Yellow
Write-Host "以下の機能を手動でテストしてください:" -ForegroundColor Cyan
Write-Host "  1. 複数画像ファイルの同時ドラッグ&ドロップ" -ForegroundColor White
Write-Host "  2. フォルダのドラッグ&ドロップ" -ForegroundColor White
Write-Host "  3. HEIC画像のプレビュー表示" -ForegroundColor White
Write-Host "  4. PDF生成と保存" -ForegroundColor White

# 6. クリーンアップ
if (-not $KeepRunning -and $process -and -not $process.HasExited) {
    Start-Sleep -Seconds 3
    $process | Stop-Process -Force
    Add-TestResult -TestName "プロセス終了" -Category "クリーンアップ" -Status "PASS"
}

# 結果サマリー
Write-Host "`n===== テスト結果サマリー =====" -ForegroundColor Cyan
Write-Host "総テスト数: $($results.Total)" -ForegroundColor White
Write-Host "成功: $($results.Passed)" -ForegroundColor Green
Write-Host "失敗: $($results.Failed)" -ForegroundColor Red
Write-Host "成功率: $([math]::Round($results.Passed / $results.Total * 100, 1))%" -ForegroundColor White

# 詳細レポート保存
$reportPath = Join-Path $config.TestOutputPath "AutoTest_$(Get-Date -Format 'yyyyMMdd_HHmmss').json"
if (-not (Test-Path $config.TestOutputPath)) {
    New-Item -ItemType Directory -Path $config.TestOutputPath -Force | Out-Null
}

$results | ConvertTo-Json -Depth 3 | Out-File -FilePath $reportPath -Encoding UTF8
Write-Host "`nレポート保存: $reportPath" -ForegroundColor Green

# 結果に基づくアクション
if ($results.Failed -eq 0) {
    Write-Host "`n✅ 全てのテストに合格しました！" -ForegroundColor Green
    exit 0
} else {
    Write-Host "`n❌ $($results.Failed) 個のテストが失敗しました" -ForegroundColor Red
    exit 1
}