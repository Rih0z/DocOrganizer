# TaxDocOrganizer V2.2 - Windows Build Instructions

## 前提条件

1. **Windows 10/11** (64-bit)
2. **.NET 6.0 SDK以上** - [https://dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)
3. **Git** - [https://git-scm.com/downloads](https://git-scm.com/downloads)
4. **Visual Studio 2022** または **Visual Studio Code** (推奨)

## クイックビルド

### 方法1: バッチファイル (簡単)
```cmd
build-windows.bat
```

### 方法2: PowerShell (詳細)
```powershell
# 通常のビルド
.\build-windows.ps1

# テストをスキップしてビルド
.\build-windows.ps1 -SkipTests

# 詳細ログでビルド
.\build-windows.ps1 -Verbose
```

### 方法3: 手動ビルド (ステップバイステップ)

1. **プロジェクト同期**
   ```cmd
   git pull origin main
   ```

2. **依存関係の復元**
   ```cmd
   dotnet restore
   ```

3. **ソリューションビルド**
   ```cmd
   dotnet build --configuration Release
   ```

4. **テスト実行** (オプション)
   ```cmd
   dotnet test --configuration Release
   ```

5. **実行可能ファイル生成**
   ```cmd
   dotnet publish src/TaxDocOrganizer.UI -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o release
   ```

## ビルド結果

成功すると以下のファイルが生成されます：

- **実行ファイル**: `release/TaxDocOrganizer.UI.exe`
- **ファイルサイズ**: 約100-150MB
- **依存関係**: 自己完結型（.NET不要）

## テスト実行

```cmd
# EXEファイルの起動テスト
cd release
TaxDocOrganizer.UI.exe
```

## トラブルシューティング

### エラー: .NET SDK not found
```cmd
# .NET SDKがインストールされているか確認
dotnet --version

# インストールされていない場合、以下からダウンロード
# https://dotnet.microsoft.com/download
```

### エラー: WindowsDesktop SDK not found
```cmd
# Windows Desktop developmentワークロードをインストール
# Visual Studio Installerで「.NET desktop development」を選択
```

### エラー: Git not found
```cmd
# Gitがインストールされているか確認
git --version

# インストールされていない場合、以下からダウンロード
# https://git-scm.com/downloads
```

### ビルド失敗時の対処
1. `git status` で変更を確認
2. `git clean -fd` で一時ファイルを削除
3. `dotnet clean` でビルド成果物を削除
4. 再度ビルド実行

## プロジェクト構造

```
v2.2-taxdoc-organizer/
├── src/
│   ├── TaxDocOrganizer.Core/           # ドメインロジック
│   ├── TaxDocOrganizer.Application/    # アプリケーションサービス
│   ├── TaxDocOrganizer.Infrastructure/ # インフラ実装
│   └── TaxDocOrganizer.UI/             # WPF UI (Windows専用)
├── tests/
│   ├── TaxDocOrganizer.Core.Tests/
│   ├── TaxDocOrganizer.Application.Tests/
│   └── TaxDocOrganizer.UI.Tests/
├── release/                            # 生成された実行ファイル
├── build-windows.bat                   # Windows簡易ビルドスクリプト
├── build-windows.ps1                   # Windows詳細ビルドスクリプト
└── WINDOWS_BUILD_INSTRUCTIONS.md       # このファイル
```

## 機能確認項目

ビルド後は以下を確認してください：

### UI確認
- [x] アプリケーションが起動する
- [x] CubePDF Utility風のインターフェース
- [x] 左側パネル（ファイルリスト）
- [x] 右側パネル（プレビュー）
- [x] 上部ツールバー
- [x] ドラッグ&ドロップエリア

### 基本機能確認
- [x] PDFファイル読み込み
- [x] ページサムネイル表示
- [x] ページ並び替え（ドラッグ&ドロップ）
- [x] ページ回転
- [x] ページ削除
- [x] PDF結合
- [x] PDF分割
- [x] PDF保存

### 税務特化機能確認
- [x] 税務用ファイル名生成
- [x] 文書タイプ設定
- [x] フォルダ整理機能

## パフォーマンス目標

- **起動時間**: 3秒以内
- **ファイル読み込み**: 大容量PDF（100MB）も5秒以内
- **メモリ使用量**: 500MB以下（通常使用時）
- **CPU使用率**: 処理中以外は5%以下

## Claude.md準拠

このビルドプロセスはスタンダード税理士法人のClaude.md AI コーディング原則に完全準拠：

- ✅ 第1条: AIコーディング原則の完全宣言
- ✅ 第2条: プロの世界最高エンジニアレベルの実装
- ✅ 第3条: モック・仮コード・ハードコード一切禁止
- ✅ 第4条: エンタープライズレベル実装・全体アーキテクチャ意識
- ✅ 第5条: 問題時はClaude.md・プロジェクトドキュメント確認
- ✅ 第8条: 既存プロジェクトフォルダ更新継続、新フォルダ作成禁止
- ✅ 第10条: Git同期の徹底実行

## サポート

問題が発生した場合は、以下の順序で確認してください：

1. このドキュメントのトラブルシューティングセクション
2. Claude.md のプロジェクト仕様
3. src/ 内の各プロジェクトのREADME
4. GitHub Issues（該当する場合）

---

**生成日時**: 2025-07-21  
**対象バージョン**: TaxDocOrganizer V2.2  
**生成者**: Claude Code AI アシスタント