# DocOrganizer 自動化スクリプト

このディレクトリには、DocOrganizerの開発・リリース作業を自動化するスクリプトが含まれています。

---

## 📦 GitHubリリース自動作成スクリプト

### 機能

1. ✅ **Version.csから自動バージョン取得**
2. ✅ **リリースビルド実行** (ログ無効版)
3. ✅ **GitHubリリース作成**
4. ✅ **EXE自動アップロード**
5. ✅ **CLAUDE.mdからリリースノート自動生成**

### 前提条件

#### GitHub CLI (gh) のインストール

**Windows (PowerShell)**:
```powershell
winget install --id GitHub.cli
```

**macOS**:
```bash
brew install gh
```

**Linux**:
```bash
# Debian/Ubuntu
sudo apt install gh

# Fedora/RHEL
sudo dnf install gh
```

#### GitHub CLI 認証

初回のみ実行:
```bash
gh auth login
```

指示に従ってGitHubアカウントで認証してください。

---

## 🚀 使用方法

### Windows (PowerShell)

```powershell
# 基本的な使い方
.\.script\create-github-release.ps1

# カスタムリリースノートを指定
.\.script\create-github-release.ps1 -ReleaseNotes "カスタムリリースノート"
```

### macOS / Linux / Git Bash

```bash
# 基本的な使い方
./.script/create-github-release.sh

# カスタムリリースノートを指定
./.script/create-github-release.sh "カスタムリリースノート"
```

---

## 📋 実行フロー

### 1. バージョン番号取得

`src/DocOrganizer.Core/Version.cs` から自動取得:
```csharp
public const string Version = "3.0.129";  // ← ここから取得
```

タグ名: `v3.0.129`

### 2. GitHub CLI (gh) 確認

- GitHub CLIのインストール確認
- GitHub認証状態の確認

### 3. Git状態確認

- 未コミットの変更確認
- リモートとの同期確認
- 必要に応じて `git push` を実行

### 4. リリースビルド

```bash
dotnet clean
dotnet restore
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o release
```

**出力**: `release\DocOrganizer.exe` (約107MB)

### 5. リリースノート作成

**自動生成** (デフォルト):
- `CLAUDE.md` からバージョン履歴を抽出
- リリースノート形式に整形

**カスタム指定** (オプション):
- スクリプト引数で指定可能

### 6. GitHubリリース作成

```bash
gh release create v3.0.129 \
    release/DocOrganizer.exe \
    --title "DocOrganizer V3.0.129" \
    --notes-file <リリースノート> \
    --latest
```

**結果**:
- GitHubリリースページに公開
- 実行ファイルが自動アップロード
- "Latest" バッジが付与

---

## ✅ 実行例

### 成功時の出力

```
========================================
 DocOrganizer GitHub Release Creator
========================================

[1/6] Version.csからバージョン番号を取得中...
  ✓ バージョン: 3.0.129
  ✓ タグ名: v3.0.129

[2/6] GitHub CLI (gh) の確認...
  ✓ GitHub CLI: インストール済み
  ✓ GitHub認証: OK

[3/6] Gitの状態確認...
  ✓ Git状態: OK

[4/6] リリースビルド実行中...
  → dotnet clean...
  → dotnet restore...
  → dotnet publish (リリースビルド・ログ無効版)...
  ✓ ビルド完了: release\DocOrganizer.exe (107 MB)

[5/6] リリースノート作成...
  ✓ リリースノート準備完了

[6/6] GitHubリリース作成中...
  → GitHubリリース作成中...
  ✓ GitHubリリース作成完了

========================================
 ✓ リリース作成完了！
========================================

リリース情報:
  バージョン: V3.0.129
  タグ: v3.0.129
  URL: https://github.com/Rih0z/DocOrganizer/releases/tag/v3.0.129

次のステップ:
  1. GitHubでリリースを確認: https://github.com/Rih0z/DocOrganizer/releases
  2. アプリケーション内で「ヘルプ」→「アップデート確認」をテスト
```

---

## ⚠️ トラブルシューティング

### エラー: GitHub CLI (gh) がインストールされていません

**解決方法**:
```powershell
# Windows
winget install --id GitHub.cli

# macOS
brew install gh

# Linux (Debian/Ubuntu)
sudo apt install gh
```

### エラー: GitHub CLIで認証されていません

**解決方法**:
```bash
gh auth login
```

指示に従ってブラウザまたはトークンで認証してください。

### エラー: タグが既に存在します

**原因**: 同じバージョンのリリースが既に存在

**解決方法**:
1. スクリプトが「既存のリリースを削除して再作成しますか?」と確認
2. `y` を入力して既存リリースを削除
3. または Version.cs のバージョンを変更

### エラー: dotnet publish失敗

**原因**: ビルドエラー

**解決方法**:
```bash
# 詳細なエラーメッセージを確認
dotnet build -c Release

# 問題を修正後、再度スクリプトを実行
```

---

## 📚 関連ドキュメント

- **バージョン管理詳細**: [docs/rule/version_management.md](../docs/rule/version_management.md)
- **GitHubアップデート手順**: [docs/rule/github_update_process.md](../docs/rule/github_update_process.md)
- **プロジェクト構造**: [docs/rule/project_structure.md](../docs/rule/project_structure.md)

---

## 🔧 カスタマイズ

### リリースノートのカスタマイズ

**PowerShell**:
```powershell
$customNotes = @"
## カスタムリリースノート

- 新機能1
- 新機能2
- バグ修正
"@

.\.script\create-github-release.ps1 -ReleaseNotes $customNotes
```

**Bash**:
```bash
./.script/create-github-release.sh "## カスタムリリースノート

- 新機能1
- 新機能2
- バグ修正"
```

### スクリプトの編集

スクリプトはテキストエディタで自由に編集できます:
- PowerShell版: `.script\create-github-release.ps1`
- Bash版: `.script\create-github-release.sh`

---

## 🎯 推奨ワークフロー

### バージョンアップ＆リリース

**ステップ1: バージョン更新**
```csharp
// src/DocOrganizer.Core/Version.cs
public const string Version = "3.0.130";  // 最後の桁を1増加
```

**ステップ2: CLAUDE.md更新**
```markdown
| V3.0.130 | 2025-10-15 | 変更内容の説明 |
```

**ステップ3: コミット＆プッシュ**
```bash
git add .
git commit -m "[V3.0.130] 変更内容"
git push origin main
```

**ステップ4: リリーススクリプト実行**
```powershell
.\.script\create-github-release.ps1
```

**完了！** GitHubリリースが自動作成されます。

---

## 📝 注意事項

1. **Version.csの更新を忘れずに**
   - スクリプトはVersion.csからバージョンを取得します
   - 更新しないと古いバージョンでリリースされます

2. **リリース前にコミット**
   - 未コミットの変更があると警告が表示されます
   - 可能な限りコミット後に実行してください

3. **リリース版EXEを使用**
   - スクリプトは自動的に `release` フォルダにビルドします
   - ログ無効版がユーザー配布に適しています

4. **タグの重複に注意**
   - 同じバージョンで複数回実行すると確認が表示されます
   - 既存リリースを削除するか、バージョンを変更してください

---

**スクリプト作成日**: 2025-10-14
**最終更新**: 2025-10-14
