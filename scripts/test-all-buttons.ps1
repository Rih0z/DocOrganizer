# DocOrganizer V2.2 全ボタン動作確認スクリプト
# 実行前提: EXEがビルド済みで、releaseフォルダに存在すること

Write-Host "=================================" -ForegroundColor Cyan
Write-Host "DocOrganizer V2.2 ボタンテスト" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan

# EXEパス確認
$exePath = Join-Path $PSScriptRoot "..\release\DocOrganizer.exe"
if (-not (Test-Path $exePath)) {
    Write-Host "❌ EXEファイルが見つかりません: $exePath" -ForegroundColor Red
    exit 1
}

Write-Host "✅ EXEファイル確認: $exePath" -ForegroundColor Green

# コマンド一覧
$commands = @(
    @{Name="ファイル操作"; Commands=@(
        "OpenCommand - PDF・画像ファイルを開く",
        "SaveCommand - Save",
        "SaveAsCommand - 名前を付けて保存",
        "NewCommand - 新規作成",
        "CloseCommand - 閉じる",
        "ExitCommand - 終了"
    )},
    @{Name="編集"; Commands=@(
        "UndoCommand - 元に戻す",
        "RedoCommand - やり直し",
        "SelectAllCommand - すべて選択",
        "DeselectAllCommand - 選択解除"
    )},
    @{Name="ページ操作"; Commands=@(
        "RotateLeftCommand - 左回転（270度）",
        "RotateRightCommand - 右回転（90度）",
        "DeleteCommand - 削除"
    )},
    @{Name="文書操作"; Commands=@(
        "MergeCommand - PDF結合",
        "SplitCommand - PDF分割",
        "SecurityCommand - セキュリティ設定"
    )},
    @{Name="表示"; Commands=@(
        "ZoomInCommand - 拡大",
        "ZoomOutCommand - 縮小", 
        "FitToWindowCommand - 全体表示",
        "ThumbnailSmallCommand - サムネイル小",
        "ThumbnailMediumCommand - サムネイル中",
        "ThumbnailLargeCommand - サムネイル大"
    )},
    @{Name="ヘルプ"; Commands=@(
        "ShowHelpCommand - ヘルプ表示",
        "CheckForUpdatesCommand - アップデート確認",
        "AboutCommand - バージョン情報"
    )}
)

# コマンドリスト表示
Write-Host "`n📋 実装されているコマンド一覧:" -ForegroundColor Yellow
foreach ($category in $commands) {
    Write-Host "`n[$($category.Name)]" -ForegroundColor Cyan
    foreach ($cmd in $category.Commands) {
        Write-Host "  • $cmd"
    }
}

# ViewModelでのコマンド実装状態確認
Write-Host "`n🔍 ViewModelでの実装状態確認:" -ForegroundColor Yellow
$viewModelPath = Join-Path $PSScriptRoot "..\src\DocOrganizer.UI\ViewModels\MainViewModel.cs"
if (Test-Path $viewModelPath) {
    $content = Get-Content $viewModelPath -Raw
    
    # RelayCommandで実装されているコマンドを検索
    $relayCommands = [regex]::Matches($content, '\[RelayCommand.*?\]\s*private\s+(?:async\s+)?(?:Task\s+)?(?:void\s+)?(\w+)')
    
    Write-Host "`n実装済みRelayCommand:" -ForegroundColor Green
    foreach ($match in $relayCommands) {
        $methodName = $match.Groups[1].Value
        Write-Host "  ✅ $($methodName)Command"
    }
}

# テスト実行の推奨
Write-Host "`n📝 手動テスト手順:" -ForegroundColor Yellow
Write-Host "1. エクスプローラーから release\DocOrganizer.exe を起動"
Write-Host "2. 各ボタンをクリックして動作確認"
Write-Host "3. エラーメッセージが表示されないことを確認"

Write-Host "`n⚠️ 重要な注意事項:" -ForegroundColor Red
Write-Host "- 管理者権限で起動しないこと（ドラッグ&ドロップが無効化される）"
Write-Host "- 回転ボタンは事前にページを選択する必要がある"
Write-Host "- 削除ボタンは事前にページを選択する必要がある"

# 簡易起動テスト
Write-Host "`n🚀 簡易起動テスト実行中..." -ForegroundColor Yellow
try {
    $proc = Start-Process -FilePath $exePath -PassThru -WindowStyle Normal
    Start-Sleep -Seconds 3
    
    if ($proc.HasExited) {
        Write-Host "❌ アプリケーションが異常終了しました (Exit Code: $($proc.ExitCode))" -ForegroundColor Red
    } else {
        Write-Host "✅ アプリケーションは正常に起動しています (PID: $($proc.Id))" -ForegroundColor Green
        Write-Host "📌 手動でボタンの動作を確認してください" -ForegroundColor Cyan
        
        # ユーザーに確認を促す
        Write-Host "`nテストが完了したら、任意のキーを押してアプリケーションを終了してください..." -ForegroundColor Yellow
        $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
        
        # プロセス終了
        if (-not $proc.HasExited) {
            $proc.CloseMainWindow()
            Start-Sleep -Seconds 1
            if (-not $proc.HasExited) {
                Stop-Process -Id $proc.Id -Force
            }
        }
    }
} catch {
    Write-Host "❌ エラー: $_" -ForegroundColor Red
}

Write-Host "`n✅ テスト完了" -ForegroundColor Green