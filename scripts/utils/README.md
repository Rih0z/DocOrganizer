# GitHub Release Upload Scripts

DocOrganizerの大容量EXEファイルをGitHub Releasesにアップロードするためのスクリプト集

## 📜 スクリプト一覧

### 1. `upload-release.ps1`
**単体リリースアップロードスクリプト**

既存のEXEファイルをGitHub Releasesにアップロードします。

```powershell
# 基本使用方法
.\scripts\utils\upload-release.ps1 -Version "2.2.0" -GitHubToken "ghp_xxxxxxxxxxxx"

# カスタムEXEパス指定
.\scripts\utils\upload-release.ps1 -Version "2.2.0" -GitHubToken "ghp_xxxxxxxxxxxx" -ExePath "custom\path\DocOrganizer.exe"
```

**パラメータ:**
- `-Version` (必須): リリースバージョン (例: "2.2.0")
- `-GitHubToken` (必須): GitHub Personal Access Token
- `-ExePath` (省略可): EXEファイルパス (デフォルト: "release\DocOrganizer.exe")
- `-Owner` (省略可): GitHubユーザー名 (デフォルト: "Rih0z")
- `-Repo` (省略可): リポジトリ名 (デフォルト: "DocOrganizer")

### 2. `build-and-release.ps1` 
**完全自動ビルド＆リリーススクリプト**

ビルドからGitHub Releaseまでの全工程を自動化します。

```powershell
# 完全自動実行
.\scripts\build\build-and-release.ps1 -Version "2.2.0" -GitHubToken "ghp_xxxxxxxxxxxx"

# テストスキップ
.\scripts\build\build-and-release.ps1 -Version "2.2.0" -GitHubToken "ghp_xxxxxxxxxxxx" -SkipTests

# ビルドスキップ（既存EXEを使用）
.\scripts\build\build-and-release.ps1 -Version "2.2.0" -GitHubToken "ghp_xxxxxxxxxxxx" -SkipBuild
```

**パラメータ:**
- `-Version` (必須): リリースバージョン
- `-GitHubToken` (必須): GitHub Personal Access Token
- `-Configuration` (省略可): ビルド構成 (デフォルト: "Release")
- `-Runtime` (省略可): ランタイム (デフォルト: "win-x64")
- `-SkipBuild`: ビルドをスキップ
- `-SkipTests`: テストをスキップ

## 🔑 GitHub Token取得方法

1. **GitHub Settings**: https://github.com/settings/tokens にアクセス
2. **Generate new token (classic)** をクリック
3. **権限設定**:
   - `repo` (Full control of private repositories)
   - `write:packages` (Upload packages to GitHub Package Registry)
4. **Token保存**: 生成されたトークンを安全な場所に保存

## 🚀 実行手順

### 手順1: トークン準備
```powershell
# 環境変数に設定（推奨）
$env:GITHUB_TOKEN = "ghp_your_token_here"
```

### 手順2: ビルド＆リリース
```powershell
# プロジェクトルートで実行
cd C:\path\to\DocOrganizer

# 完全自動実行
.\scripts\build\build-and-release.ps1 -Version "2.2.0" -GitHubToken $env:GITHUB_TOKEN
```

### 手順3: 確認
- [Releases Page](https://github.com/Rih0z/DocOrganizer/releases) でリリースを確認
- DocOrganizer.exe のダウンロードリンクをテスト

## 📋 実行プロセス

`build-and-release.ps1` の実行ステップ:

1. **環境確認**: .NET SDK, Git の存在確認
2. **Git状態確認**: 未コミット変更の警告
3. **バージョン更新**: .csprojファイルのバージョン番号更新
4. **テスト実行**: 単体・統合テストの実行
5. **ビルド実行**: クリーンアップ → 復元 → ビルド → パブリッシュ
6. **EXE確認**: 生成されたファイルのサイズ・存在確認
7. **Git コミット**: バージョン更新とビルド結果をコミット・プッシュ
8. **Release作成**: GitHub APIでリリース作成とEXEアップロード

## ⚠️ 注意事項

### セキュリティ
- **GitHub Tokenは絶対に公開しない**
- 環境変数またはセキュアな場所に保存
- スクリプト実行後はTokenを削除推奨

### ファイルサイズ制限
- GitHub通常Push制限: 100MB
- GitHub Release制限: 2GB (十分)
- DocOrganizer.exe: 約200MB (問題なし)

### エラーハンドリング
- 各ステップでエラー検出時は即座に停止
- 詳細なエラーメッセージを表示
- ロールバック機能は未実装

## 🔧 トラブルシューティング

### 問題: "既存のリリースが存在します"
**解決策**: 既存のリリースを自動検出して使用します（正常動作）

### 問題: "アップロード失敗"  
**解決策**: 
1. GitHub Tokenの権限確認
2. ネットワーク接続確認
3. ファイルサイズ確認（2GB以下）

### 問題: "ビルド失敗"
**解決策**:
1. .NET SDK バージョン確認
2. 依存関係の確認 (`dotnet restore`)
3. プロジェクトファイルの構文確認

## 📈 使用例

### 開発版リリース
```powershell
.\scripts\build\build-and-release.ps1 -Version "2.2.0-beta" -GitHubToken $env:GITHUB_TOKEN
```

### 緊急パッチリリース
```powershell
# テストスキップで高速リリース
.\scripts\build\build-and-release.ps1 -Version "2.2.1" -GitHubToken $env:GITHUB_TOKEN -SkipTests
```

### 手動ビルド後のリリース
```powershell
# ビルド済みEXEを使用
.\scripts\utils\upload-release.ps1 -Version "2.2.0" -GitHubToken $env:GITHUB_TOKEN
```

---

**これで大容量EXEファイルの制限を回避して、プロフェッショナルなリリース配布が可能になります！** 🚀