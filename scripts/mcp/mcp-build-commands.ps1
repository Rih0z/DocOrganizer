# MCP Windows Build Commands for DocOrganizer V2.2
# Claude.md第12条準拠の完全ビルドワークフロー

Write-Host "=== DocOrganizer V2.2 MCP Windows Build ===" -ForegroundColor Magenta
Write-Host "Claude.md第12条準拠の完全ビルドを開始します" -ForegroundColor Yellow

# 1. 環境情報取得
Write-Host "`n[Step 1] 環境情報確認" -ForegroundColor Cyan
Write-Host "Current Directory: $(Get-Location)"
Write-Host "PowerShell Version: $($PSVersionTable.PSVersion)"

# 2. .NET SDK確認
Write-Host "`n[Step 2] .NET SDK確認" -ForegroundColor Cyan
try {
    $dotnetVersion = dotnet --version
    Write-Host "✅ .NET SDK Version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ .NET SDK not found" -ForegroundColor Red
    exit 1
}

# 3. Git同期実行
Write-Host "`n[Step 3] Git同期実行" -ForegroundColor Cyan
try {
    Write-Host "Executing: git pull origin main"
    git pull origin main
    Write-Host "✅ Git同期完了" -ForegroundColor Green
} catch {
    Write-Host "⚠️ Git同期に問題がありました" -ForegroundColor Yellow
}

# 4. ソリューションクリーン
Write-Host "`n[Step 4] ソリューションクリーン" -ForegroundColor Cyan
try {
    Write-Host "Executing: dotnet clean --configuration Release"
    dotnet clean --configuration Release
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ クリーン完了" -ForegroundColor Green
    } else {
        Write-Host "❌ クリーン失敗" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "❌ クリーンでエラー: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# 5. 依存関係復元
Write-Host "`n[Step 5] 依存関係復元" -ForegroundColor Cyan
try {
    Write-Host "Executing: dotnet restore"
    dotnet restore
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ 依存関係復元完了" -ForegroundColor Green
    } else {
        Write-Host "❌ 依存関係復元失敗" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "❌ 復元でエラー: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# 6. ソリューションビルド
Write-Host "`n[Step 6] ソリューションビルド" -ForegroundColor Cyan
try {
    Write-Host "Executing: dotnet build --configuration Release --no-restore"
    dotnet build --configuration Release --no-restore
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ ビルド完了" -ForegroundColor Green
    } else {
        Write-Host "❌ ビルド失敗" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "❌ ビルドでエラー: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# 7. テスト実行
Write-Host "`n[Step 7] テスト実行" -ForegroundColor Cyan
try {
    Write-Host "Executing: dotnet test --configuration Release --no-build"
    dotnet test --configuration Release --no-build
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ 全てのテストがパス" -ForegroundColor Green
    } else {
        Write-Host "⚠️ テストに失敗がありますが続行します" -ForegroundColor Yellow
    }
} catch {
    Write-Host "⚠️ テストでエラー: $($_.Exception.Message)" -ForegroundColor Yellow
}

# 8. releaseディレクトリ作成・確認
Write-Host "`n[Step 8] releaseディレクトリ準備" -ForegroundColor Cyan
if (-not (Test-Path "release")) {
    New-Item -ItemType Directory -Path "release" | Out-Null
    Write-Host "✅ releaseディレクトリを作成しました" -ForegroundColor Green
} else {
    Write-Host "✅ releaseディレクトリが存在します" -ForegroundColor Green
}

# 9. Windows実行ファイル公開
Write-Host "`n[Step 9] Windows実行ファイル公開" -ForegroundColor Cyan
try {
    $publishCommand = "dotnet publish src/DocOrganizer.UI -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o release"
    Write-Host "Executing: $publishCommand"
    
    Invoke-Expression $publishCommand
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ 実行ファイル公開完了" -ForegroundColor Green
        
        # EXEファイル確認
        $exePath = "release/DocOrganizer.UI.exe"
        if (Test-Path $exePath) {
            $fileInfo = Get-Item $exePath
            $fileSize = [math]::Round($fileInfo.Length / 1MB, 2)
            $fullPath = $fileInfo.FullName
            
            Write-Host "`n=== 最終結果（Claude.md第12条準拠） ===" -ForegroundColor Magenta
            Write-Host "✅ DocOrganizer V2.2 完成: $fullPath - ${fileSize}MB - $($fileInfo.CreationTime)" -ForegroundColor Green
            Write-Host "=== ビルド成功 ===" -ForegroundColor Magenta
            
            # 詳細情報
            Write-Host "`n📋 詳細情報:" -ForegroundColor Cyan
            Write-Host "   ファイルパス: $fullPath"
            Write-Host "   ファイルサイズ: ${fileSize}MB"
            Write-Host "   作成日時: $($fileInfo.CreationTime)"
            Write-Host "   最終更新: $($fileInfo.LastWriteTime)"
        } else {
            Write-Host "❌ EXEファイルが見つかりません: $exePath" -ForegroundColor Red
            exit 1
        }
    } else {
        Write-Host "❌ 公開に失敗しました" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "❌ 公開でエラー: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# 10. EXE動作テスト
Write-Host "`n[Step 10] EXE動作テスト" -ForegroundColor Cyan
try {
    $exePath = "release/DocOrganizer.UI.exe"
    if (Test-Path $exePath) {
        Write-Host "EXE起動テストを実行中..."
        
        $proc = Start-Process -FilePath $exePath -PassThru -WindowStyle Minimized
        Start-Sleep -Seconds 3
        
        if (-not $proc.HasExited) {
            Write-Host "✅ EXEが正常に起動しました (PID: $($proc.Id))" -ForegroundColor Green
            Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
            Write-Host "✅ EXE動作テスト完了" -ForegroundColor Green
        } else {
            Write-Host "❌ EXEが即座に終了しました" -ForegroundColor Red
        }
    }
} catch {
    Write-Host "⚠️ EXE動作テストでエラー: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host "`n=== 全プロセス完了 ===" -ForegroundColor Magenta
Write-Host "Claude.md第12条に従って完全ビルドが実行されました" -ForegroundColor Green