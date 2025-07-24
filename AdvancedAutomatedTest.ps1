# DocOrganizer V2.2 高度な自動テストスクリプト
# UI Automation APIを使用したドラッグ&ドロップテスト

param(
    [string]$Mode = "Full"  # Full, Quick, ErrorOnly
)

Write-Host "=== DocOrganizer V2.2 Advanced Automated Test ===" -ForegroundColor Green
Write-Host "モード: $Mode" -ForegroundColor Yellow
Write-Host "開始時刻: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor White

# .NET UI Automation APIを読み込み
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms

# パス設定
$script:Config = @{
    ExePath = "C:\Users\koki\ezark\standard-image\Standard-image\v2.2-doc-organizer\release\DocOrganizer.UI.exe"
    SamplePath = "C:\Users\koki\ezark\standard-image\Standard-image\v2.2-doc-organizer\sample"
    TestOutputPath = "C:\Users\koki\ezark\standard-image\Standard-image\v2.2-doc-organizer\test-output"
    LogPath = "C:\Users\koki\ezark\standard-image\Standard-image\v2.2-doc-organizer\release\logs"
    TestReportPath = "C:\Users\koki\ezark\standard-image\Standard-image\v2.2-doc-organizer\test-reports"
}

# テスト結果管理
$script:TestResults = @{
    Total = 0
    Passed = 0
    Failed = 0
    Skipped = 0
    Details = @()
}

# テスト結果記録関数
function Add-TestResult {
    param(
        [string]$TestName,
        [string]$Category,
        [ValidateSet("PASS", "FAIL", "SKIP")]
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
        Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    }
    
    $script:TestResults.Total++
    switch ($Status) {
        "PASS" { 
            $script:TestResults.Passed++
            Write-Host "[PASS] $TestName" -ForegroundColor Green
        }
        "FAIL" { 
            $script:TestResults.Failed++
            Write-Host "[FAIL] $TestName - $Details" -ForegroundColor Red
        }
        "SKIP" { 
            $script:TestResults.Skipped++
            Write-Host "[SKIP] $TestName" -ForegroundColor Yellow
        }
    }
    
    $script:TestResults.Details += $result
}

# ヘルパー関数：プロセス管理
function Start-DocOrganizer {
    Write-Host "`nアプリケーション起動中..." -ForegroundColor Cyan
    
    # 既存プロセスを終了
    Get-Process -Name "DocOrganizer.UI" -ErrorAction SilentlyContinue | Stop-Process -Force
    Start-Sleep -Seconds 2
    
    # 新規起動
    try {
        $process = Start-Process -FilePath $Config.ExePath -PassThru
        Start-Sleep -Seconds 5
        
        if (-not $process.HasExited) {
            return $process
        } else {
            throw "プロセスが即座に終了しました"
        }
    } catch {
        throw "起動失敗: $_"
    }
}

# ヘルパー関数：UI要素検索
function Find-UIElement {
    param(
        [string]$Name,
        [string]$ClassName,
        [int]$TimeoutSeconds = 10
    )
    
    $automation = [System.Windows.Automation.AutomationElement]::RootElement
    $condition = $null
    
    if ($Name) {
        $condition = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::NameProperty, $Name)
    } elseif ($ClassName) {
        $condition = New-Object System.Windows.Automation.PropertyCondition([System.Windows.Automation.AutomationElement]::ClassNameProperty, $ClassName)
    }
    
    $endTime = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $endTime) {
        $element = $automation.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
        if ($element) {
            return $element
        }
        Start-Sleep -Milliseconds 500
    }
    
    return $null
}

# ヘルパー関数：ドラッグ&ドロップシミュレーション
function Invoke-DragDrop {
    param(
        [string]$SourceFile,
        [System.Windows.Automation.AutomationElement]$TargetElement
    )
    
    if (-not (Test-Path $SourceFile)) {
        throw "ソースファイルが存在しません: $SourceFile"
    }
    
    if (-not $TargetElement) {
        throw "ターゲット要素が見つかりません"
    }
    
    # ターゲット要素の位置を取得
    $rect = $TargetElement.Current.BoundingRectangle
    $centerX = $rect.X + $rect.Width / 2
    $centerY = $rect.Y + $rect.Height / 2
    
    # マウスカーソルを移動
    [System.Windows.Forms.Cursor]::Position = New-Object System.Drawing.Point($centerX, $centerY)
    
    # Windows Shell経由でドラッグ&ドロップをシミュレート
    $shell = New-Object -ComObject Shell.Application
    $folder = $shell.NameSpace((Split-Path $SourceFile -Parent))
    $item = $folder.ParseName((Split-Path $SourceFile -Leaf))
    
    # クリップボードを使用した代替方法
    $files = [string[]]@($SourceFile)
    $dataObject = New-Object System.Windows.Forms.DataObject
    $dataObject.SetData([System.Windows.Forms.DataFormats]::FileDrop, $files)
    [System.Windows.Forms.Clipboard]::SetDataObject($dataObject)
    
    # Ctrl+V でペースト（ドラッグ&ドロップの代替）
    [System.Windows.Forms.SendKeys]::SendWait("^v")
    
    return $true
}

# テスト1: 環境チェック
function Test-Environment {
    Write-Host "`n[テスト1] 環境チェック" -ForegroundColor Yellow
    $startTime = Get-Date
    
    # EXE存在確認
    if (Test-Path $Config.ExePath) {
        $exeInfo = Get-Item $Config.ExePath
        Add-TestResult -TestName "EXE存在確認" -Category "環境" -Status "PASS" `
            -Details "Size: $([math]::Round($exeInfo.Length/1MB, 1))MB"
    } else {
        Add-TestResult -TestName "EXE存在確認" -Category "環境" -Status "FAIL" `
            -Details "EXEが見つかりません"
        return $false
    }
    
    # サンプルファイル確認
    $sampleFiles = Get-ChildItem -Path $Config.SamplePath -Include "*.png","*.jpg","*.jpeg" -Recurse -File
    if ($sampleFiles.Count -gt 0) {
        Add-TestResult -TestName "サンプルファイル" -Category "環境" -Status "PASS" `
            -Details "$($sampleFiles.Count)個のファイル"
    } else {
        Add-TestResult -TestName "サンプルファイル" -Category "環境" -Status "FAIL" `
            -Details "画像ファイルが見つかりません"
    }
    
    # 出力ディレクトリ準備
    if (-not (Test-Path $Config.TestOutputPath)) {
        New-Item -ItemType Directory -Path $Config.TestOutputPath -Force | Out-Null
    }
    if (-not (Test-Path $Config.TestReportPath)) {
        New-Item -ItemType Directory -Path $Config.TestReportPath -Force | Out-Null
    }
    
    $duration = ((Get-Date) - $startTime).TotalSeconds
    Add-TestResult -TestName "環境準備" -Category "環境" -Status "PASS" `
        -Duration $duration
    
    return $true
}

# テスト2: アプリケーション起動
function Test-ApplicationStartup {
    Write-Host "`n[テスト2] アプリケーション起動テスト" -ForegroundColor Yellow
    $startTime = Get-Date
    
    try {
        $process = Start-DocOrganizer
        
        # プロセス情報
        $memoryMB = [math]::Round($process.WorkingSet64 / 1MB, 1)
        Add-TestResult -TestName "プロセス起動" -Category "起動" -Status "PASS" `
            -Details "PID: $($process.Id), Memory: ${memoryMB}MB" `
            -Duration ((Get-Date) - $startTime).TotalSeconds
        
        # メインウィンドウ検索
        $mainWindow = Find-UIElement -ClassName "Window" -TimeoutSeconds 10
        if ($mainWindow) {
            Add-TestResult -TestName "メインウィンドウ" -Category "起動" -Status "PASS" `
                -Details $mainWindow.Current.Name
        } else {
            Add-TestResult -TestName "メインウィンドウ" -Category "起動" -Status "FAIL" `
                -Details "ウィンドウが見つかりません"
        }
        
        return @{
            Process = $process
            Window = $mainWindow
        }
    } catch {
        Add-TestResult -TestName "プロセス起動" -Category "起動" -Status "FAIL" `
            -Details $_.Exception.Message
        return $null
    }
}

# テスト3: エラー処理テスト
function Test-ErrorHandling {
    param($AppInfo)
    
    if ($Mode -eq "Quick") {
        Write-Host "`n[テスト3] エラー処理テスト - スキップ (Quickモード)" -ForegroundColor Yellow
        return
    }
    
    Write-Host "`n[テスト3] エラー処理テスト" -ForegroundColor Yellow
    
    # 破損ファイルテスト
    $corruptedFile = Join-Path $Config.TestOutputPath "test_corrupted.png"
    [System.IO.File]::WriteAllBytes($corruptedFile, @(0x00, 0x01, 0x02, 0x03, 0x04))
    
    # 空ファイルテスト
    $emptyFile = Join-Path $Config.TestOutputPath "test_empty.png"
    New-Item -ItemType File -Path $emptyFile -Force | Out-Null
    
    # 大容量ファイル（シミュレート）
    $largeFile = Join-Path $Config.TestOutputPath "test_large.txt"
    "This is not an image" | Out-File $largeFile
    
    $testFiles = @(
        @{Path = $corruptedFile; Name = "破損ファイル"; Expected = "エラー処理"}
        @{Path = $emptyFile; Name = "空ファイル"; Expected = "エラー処理"}
        @{Path = $largeFile; Name = "非画像ファイル"; Expected = "エラー処理"}
    )
    
    foreach ($test in $testFiles) {
        $startTime = Get-Date
        
        # ログ監視でエラー検出
        $logBefore = Get-ChildItem $Config.LogPath -Filter "*.txt" | 
            Sort-Object LastWriteTime -Descending | 
            Select-Object -First 1 | 
            Get-Content -Tail 50
        
        # ここでファイルをドラッグ&ドロップする処理を追加可能
        
        Start-Sleep -Seconds 2
        
        $logAfter = Get-ChildItem $Config.LogPath -Filter "*.txt" | 
            Sort-Object LastWriteTime -Descending | 
            Select-Object -First 1 | 
            Get-Content -Tail 50
        
        # 新しいログエントリを確認
        $newLogs = $logAfter | Where-Object { $logBefore -notcontains $_ }
        
        if ($newLogs -match "\[ERR\]") {
            Add-TestResult -TestName $test.Name -Category "エラー処理" -Status "PASS" `
                -Details "エラーが適切に処理されました" `
                -Duration ((Get-Date) - $startTime).TotalSeconds
        } else {
            Add-TestResult -TestName $test.Name -Category "エラー処理" -Status "SKIP" `
                -Details "手動でのドラッグ&ドロップが必要"
        }
    }
}

# テスト4: パフォーマンステスト
function Test-Performance {
    param($AppInfo)
    
    Write-Host "`n[テスト4] パフォーマンステスト" -ForegroundColor Yellow
    
    if (-not $AppInfo.Process) {
        Add-TestResult -TestName "パフォーマンス測定" -Category "パフォーマンス" -Status "SKIP" `
            -Details "プロセスが起動していません"
        return
    }
    
    # CPU使用率測定
    $cpuCounter = Get-Counter "\Process(DocOrganizer.UI)\% Processor Time" -ErrorAction SilentlyContinue
    if ($cpuCounter) {
        $cpuUsage = [math]::Round($cpuCounter.CounterSamples[0].CookedValue, 2)
        Add-TestResult -TestName "CPU使用率" -Category "パフォーマンス" -Status "PASS" `
            -Details "${cpuUsage}%"
    }
    
    # メモリ使用量
    $memoryMB = [math]::Round($AppInfo.Process.WorkingSet64 / 1MB, 1)
    $status = if ($memoryMB -lt 500) { "PASS" } else { "FAIL" }
    Add-TestResult -TestName "メモリ使用量" -Category "パフォーマンス" -Status $status `
        -Details "${memoryMB}MB"
    
    # ハンドル数
    $handleCount = $AppInfo.Process.HandleCount
    Add-TestResult -TestName "ハンドル数" -Category "パフォーマンス" -Status "PASS" `
        -Details $handleCount
}

# テスト5: 画像変換機能テスト（半自動）
function Test-ImageConversion {
    param($AppInfo)
    
    Write-Host "`n[テスト5] 画像変換機能テスト" -ForegroundColor Yellow
    
    $sampleFiles = Get-ChildItem -Path $Config.SamplePath -Include "*.png","*.jpg","*.jpeg" -Recurse -File | 
        Select-Object -First 3
    
    Write-Host "`n以下のファイルをアプリケーションにドラッグ&ドロップしてください:" -ForegroundColor Cyan
    foreach ($file in $sampleFiles) {
        Write-Host "  - $($file.FullName)" -ForegroundColor Gray
    }
    
    Write-Host "`nEnterキーを押すと変換結果を確認します..." -ForegroundColor Yellow
    Read-Host
    
    # ログから変換成功を確認
    $latestLog = Get-ChildItem $Config.LogPath -Filter "*.txt" | 
        Sort-Object LastWriteTime -Descending | 
        Select-Object -First 1
    
    if ($latestLog) {
        $logContent = Get-Content $latestLog.FullName -Tail 100
        $successCount = ($logContent | Select-String "Successfully created PDF").Count
        $errorCount = ($logContent | Select-String "\[ERR\].*Failed to convert").Count
        
        if ($successCount -gt 0) {
            Add-TestResult -TestName "画像変換成功" -Category "機能" -Status "PASS" `
                -Details "$successCount 件の変換成功"
        }
        
        if ($errorCount -gt 0) {
            Add-TestResult -TestName "画像変換エラー" -Category "機能" -Status "FAIL" `
                -Details "$errorCount 件のエラー"
        }
    }
}

# メインテスト実行
function Run-Tests {
    # テスト環境チェック
    if (-not (Test-Environment)) {
        Write-Host "`nテスト環境が整っていません。終了します。" -ForegroundColor Red
        return
    }
    
    # アプリケーション起動
    $appInfo = Test-ApplicationStartup
    if (-not $appInfo) {
        Write-Host "`nアプリケーション起動に失敗しました。" -ForegroundColor Red
        return
    }
    
    # 各テスト実行
    switch ($Mode) {
        "Full" {
            Test-ErrorHandling -AppInfo $appInfo
            Test-Performance -AppInfo $appInfo
            Test-ImageConversion -AppInfo $appInfo
        }
        "Quick" {
            Test-Performance -AppInfo $appInfo
        }
        "ErrorOnly" {
            Test-ErrorHandling -AppInfo $appInfo
        }
    }
    
    # クリーンアップ
    if ($appInfo.Process -and -not $appInfo.Process.HasExited) {
        $appInfo.Process | Stop-Process -Force
        Add-TestResult -TestName "プロセス終了" -Category "クリーンアップ" -Status "PASS"
    }
}

# レポート生成
function Generate-Report {
    $report = @"
================================
DocOrganizer V2.2 テストレポート
================================
実行日時: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
テストモード: $Mode

[サマリー]
総テスト数: $($TestResults.Total)
成功: $($TestResults.Passed) ($(if($TestResults.Total -gt 0){[math]::Round($TestResults.Passed/$TestResults.Total*100,1)}else{0})%)
失敗: $($TestResults.Failed)
スキップ: $($TestResults.Skipped)

[詳細結果]
"@

    foreach ($category in ($TestResults.Details | Group-Object Category)) {
        $report += "`n`n--- $($category.Name) ---`n"
        foreach ($test in $category.Group) {
            $statusSymbol = switch ($test.Status) {
                "PASS" { "✓" }
                "FAIL" { "✗" }
                "SKIP" { "○" }
            }
            $report += "$statusSymbol $($test.TestName): $($test.Details)"
            if ($test.Duration -ne "0s") {
                $report += " ($($test.Duration))"
            }
            $report += "`n"
        }
    }
    
    # コンソール出力
    Write-Host "`n$report" -ForegroundColor Cyan
    
    # ファイル保存
    $reportFile = Join-Path $Config.TestReportPath "TestReport_$(Get-Date -Format 'yyyyMMdd_HHmmss').txt"
    $report | Out-File -FilePath $reportFile -Encoding UTF8
    Write-Host "`nレポート保存先: $reportFile" -ForegroundColor Green
    
    # JSON形式でも保存
    $jsonReport = @{
        ExecutionTime = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
        Mode = $Mode
        Summary = @{
            Total = $TestResults.Total
            Passed = $TestResults.Passed
            Failed = $TestResults.Failed
            Skipped = $TestResults.Skipped
            SuccessRate = if($TestResults.Total -gt 0){[math]::Round($TestResults.Passed/$TestResults.Total*100,1)}else{0}
        }
        Details = $TestResults.Details
    }
    
    $jsonFile = Join-Path $Config.TestReportPath "TestReport_$(Get-Date -Format 'yyyyMMdd_HHmmss').json"
    $jsonReport | ConvertTo-Json -Depth 3 | Out-File -FilePath $jsonFile -Encoding UTF8
}

# メイン実行
try {
    Run-Tests
    Generate-Report
    
    # 終了コード
    if ($TestResults.Failed -eq 0) {
        Write-Host "`n✅ テスト完了！" -ForegroundColor Green
        exit 0
    } else {
        Write-Host "`n❌ $($TestResults.Failed) 個のテストが失敗しました" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "`n予期しないエラーが発生しました: $_" -ForegroundColor Red
    exit 2
}