# 空の画像ファイル問題の修正スクリプト
# 実行環境: Windows MCP
# 目的: 0バイトの画像ファイルを削除し、ビルドエラーを解決

Write-Host "空の画像ファイル修正スクリプト" -ForegroundColor Cyan
Write-Host "=============================" -ForegroundColor Cyan

# Step 1: 現在の画像ファイル状況確認
Write-Host "`n1. 現在の画像ファイル状況:"
$imageDir = "src\DocOrganizer.UI\Images"
if (Test-Path $imageDir) {
    Get-ChildItem $imageDir -Filter "*.png" | ForEach-Object {
        Write-Host "  $($_.Name): $($_.Length) bytes" -ForegroundColor $(if ($_.Length -eq 0) { "Red" } else { "Green" })
    }
} else {
    Write-Host "  画像ディレクトリが見つかりません: $imageDir" -ForegroundColor Red
}

# Step 2: 空ファイルの削除
Write-Host "`n2. 空の画像ファイルを削除:"
$deletedFiles = @()
if (Test-Path $imageDir) {
    Get-ChildItem $imageDir -Filter "*.png" | Where-Object { $_.Length -eq 0 } | ForEach-Object {
        Write-Host "  削除: $($_.Name)" -ForegroundColor Yellow
        Remove-Item $_.FullName -Force
        $deletedFiles += $_.Name
    }
    
    if ($deletedFiles.Count -eq 0) {
        Write-Host "  削除対象のファイルはありませんでした" -ForegroundColor Green
    } else {
        Write-Host "  $($deletedFiles.Count)個のファイルを削除しました" -ForegroundColor Green
    }
} else {
    Write-Host "  画像ディレクトリが見つかりません" -ForegroundColor Red
}

# Step 3: プロジェクトファイルのバックアップと修正
Write-Host "`n3. プロジェクトファイルの修正:"
$projectFile = "src\DocOrganizer.UI\DocOrganizer.UI.csproj"
if (Test-Path $projectFile) {
    # バックアップ作成
    $backupFile = "$projectFile.backup"
    Copy-Item $projectFile $backupFile
    Write-Host "  バックアップ作成: $backupFile" -ForegroundColor Green
    
    # プロジェクトファイル読み込み
    $content = Get-Content $projectFile -Raw
    
    # 画像ファイル参照を削除または修正
    $newContent = $content -replace '(?s)<ItemGroup>\s*<!-- アイコンとリソース.*?</ItemGroup>', ''
    $newContent = $newContent -replace '(?s)<!-- 空の画像ファイルによるビルドエラー回避.*?</ItemGroup>', ''
    
    # 修正版を保存
    $newContent | Set-Content $projectFile -Encoding UTF8
    Write-Host "  プロジェクトファイルを修正しました" -ForegroundColor Green
} else {
    Write-Host "  プロジェクトファイルが見つかりません: $projectFile" -ForegroundColor Red
}

# Step 4: MainWindow.xamlの画像参照を削除
Write-Host "`n4. XAML画像参照の修正:"
$xamlFile = "src\DocOrganizer.UI\Views\MainWindow.xaml"
if (Test-Path $xamlFile) {
    $xamlContent = Get-Content $xamlFile -Raw
    
    # 画像参照を削除（MenuItem.Icon、Image Source等）
    $newXamlContent = $xamlContent -replace '<MenuItem\.Icon>.*?</MenuItem\.Icon>', ''
    $newXamlContent = $newXamlContent -replace '<Image Source="/Images/.*?" Width=".*?" Height=".*?"/>', '<Rectangle Width="20" Height="20" Fill="Gray"/>'
    $newXamlContent = $newXamlContent -replace 'Source="/Images/.*?"', 'Source=""'
    
    # バックアップと保存
    Copy-Item $xamlFile "$xamlFile.backup"
    $newXamlContent | Set-Content $xamlFile -Encoding UTF8
    Write-Host "  XAML画像参照を修正しました" -ForegroundColor Green
} else {
    Write-Host "  XAMLファイルが見つかりません: $xamlFile" -ForegroundColor Red
}

# Step 5: テストビルド
Write-Host "`n5. テストビルド実行:"
try {
    Write-Host "  Clean..."
    dotnet clean --configuration Release --verbosity quiet
    
    Write-Host "  Restore..."
    dotnet restore --verbosity quiet
    
    Write-Host "  Build..."
    dotnet build --configuration Release --no-restore --verbosity quiet
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  ✅ ビルド成功!" -ForegroundColor Green
        
        # パブリッシュテスト
        Write-Host "  Publish test..."
        dotnet publish src/DocOrganizer.UI -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o release --verbosity quiet
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "  ✅ パブリッシュ成功!" -ForegroundColor Green
            
            # EXE確認
            if (Test-Path "release\DocOrganizer.UI.exe") {
                $exe = Get-Item "release\DocOrganizer.UI.exe"
                Write-Host "  ✅ EXE生成完了: $($exe.FullName)" -ForegroundColor Green
                Write-Host "     サイズ: $([math]::Round($exe.Length/1MB, 1))MB" -ForegroundColor Green
            } else {
                Write-Host "  ❌ EXEが見つかりません" -ForegroundColor Red
            }
        } else {
            Write-Host "  ❌ パブリッシュ失敗" -ForegroundColor Red
        }
    } else {
        Write-Host "  ❌ ビルド失敗" -ForegroundColor Red
    }
} catch {
    Write-Host "  ❌ ビルド例外: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n修正完了!" -ForegroundColor Magenta
Write-Host "==========" -ForegroundColor Magenta