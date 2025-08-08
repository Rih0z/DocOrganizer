# 推奨コマンド集

## ビルド・実行コマンド

### ビルドコマンド
```powershell
# クリーンビルド
dotnet clean
dotnet restore
dotnet build --configuration Release

# 本番リリース用（自己完結型EXE生成）
dotnet publish src/DocOrganizer.UI/DocOrganizer.UI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o release
```

### テストコマンド
```powershell
# 全テスト実行
dotnet test --configuration Release

# 特定プロジェクトのテスト
dotnet test tests/DocOrganizer.Core.Tests/
dotnet test tests/DocOrganizer.Application.Tests/
dotnet test tests/DocOrganizer.UI.Tests/
```

### アプリケーション実行
```powershell
# デバッグ実行
dotnet run --project src/DocOrganizer.UI/

# リリース版実行（エクスプローラーから）
# ⚠️ 管理者権限で起動しない（ドラッグ&ドロップが無効化される）
release/DocOrganizer.exe
```

## Windows用ユーティリティコマンド

### ファイル操作
```cmd
# ディレクトリ一覧
dir /s /b *.cs        # C#ファイル一覧
dir /s /b *.csproj    # プロジェクトファイル一覧

# ファイル検索
findstr /s /i "HEIC" *.cs    # ソースコード内文字列検索
```

### Git操作
```powershell
# 基本的なGit操作
git status
git pull origin main
git add .
git commit -m "変更内容"
git push origin main
```

### 自動化スクリプト
```powershell
# ビルドスクリプト
scripts/build/build-windows.ps1
scripts/build/BUILD_ON_WINDOWS.bat

# テストスクリプト
scripts/test/AutomatedTest.ps1
scripts/test/ComprehensiveTest.ps1
scripts/test/test-exe-simple.ps1
```

## 開発ワークフロー

### 1. 作業開始時
```powershell
git pull origin main
dotnet clean
dotnet restore
dotnet build --configuration Release
```

### 2. 作業完了時
```powershell
dotnet test --configuration Release
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o release
git add .
git commit -m "変更内容"
git push origin main
```

### 3. トラブルシューティング
```powershell
# ビルドキャッシュクリア
dotnet clean
Remove-Item -Recurse -Force bin, obj -ErrorAction SilentlyContinue

# NuGetキャッシュクリア
dotnet nuget locals all --clear
```