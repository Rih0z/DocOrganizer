# タスク完了時チェックリスト

## コード変更後の必須手順

### 1. ビルド確認
```powershell
# 必須: クリーンビルド実行
dotnet clean
dotnet restore  
dotnet build --configuration Release

# エラーがある場合は修正してから次へ
```

### 2. テスト実行
```powershell
# 全テスト実行（必須）
dotnet test --configuration Release

# 失敗したテストがある場合は修正してから次へ
```

### 3. 本番ビルド
```powershell
# 自己完結型EXE生成（必須）
dotnet publish src/DocOrganizer.UI/DocOrganizer.UI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o release

# EXEファイル生成確認
if exist release\DocOrganizer.exe echo "✅ EXE生成成功"
```

### 4. 動作確認
```powershell
# エクスプローラーから起動テスト（管理者権限厳禁）
# 1. release フォルダを開く
# 2. DocOrganizer.exe をダブルクリック
# 3. アプリが起動することを確認
# 4. ドラッグ&ドロップテスト実行
```

### 5. Git操作
```powershell
# 変更をコミット
git add .
git status  # 変更内容確認
git commit -m "[Windows] 修正内容の簡潔な説明"
git push origin main
```

## 品質チェック項目

### コード品質
- [ ] 例外処理が適切に実装されている
- [ ] ログ出力が適切に配置されている
- [ ] メモリリークの可能性がない（IDisposable適切に使用）
- [ ] 非同期処理でConfigureAwait(false)を使用している

### 機能テスト
- [ ] PDF読み込み機能が動作する
- [ ] 画像ファイル（HEIC含む）が正常に処理される
- [ ] ドラッグ&ドロップが正常に動作する
- [ ] PDF保存機能が正常に動作する
- [ ] 回転・削除機能が正常に動作する

### パフォーマンス
- [ ] 大きなファイル（100MB以上）でもクラッシュしない
- [ ] メモリ使用量が適切な範囲内
- [ ] レスポンスが適切（3秒以内でUI応答）

## トラブルシューティング

### ビルドエラー
```powershell
# NuGetパッケージ復元エラー
dotnet nuget locals all --clear
dotnet restore

# 依存関係エラー
dotnet list package --outdated
dotnet add package [パッケージ名] --version [バージョン]
```

### テスト失敗
```powershell
# 詳細なテスト結果
dotnet test --logger "console;verbosity=detailed"

# 特定テストのみ実行
dotnet test --filter "TestMethodName"
```

### 実行時エラー
```powershell
# デバッグ情報付きで実行
dotnet run --project src/DocOrganizer.UI/ --configuration Debug

# ログファイル確認
# アプリケーションログを確認してエラー詳細を特定
```

## 完了報告フォーマット

```
✅ [機能名] 実装完了

【実行結果】
- ビルド: 成功
- テスト: 全て通過 (X個のテスト)
- EXE生成: 成功 (XXXMBのファイル)
- 動作確認: 正常

【ファイルパス】
C:\[パス]\release\DocOrganizer.exe

【変更内容】
- [変更内容1]
- [変更内容2]

【テスト実行結果】
- [テスト結果の詳細]
```

## HEIC処理特有のチェック項目

### HEIC処理確認
- [ ] Magick.NET初期化が正常に完了している
- [ ] HEICファイルの読み込みでクラッシュしない
- [ ] HEIC→JPEG変換が正常に動作する  
- [ ] HEICサムネイル生成が正常に動作する
- [ ] HEIC一時ファイルの適切なクリーンアップ

### Windows環境固有確認
- [ ] Windows 10/11での動作確認
- [ ] ドラッグ&ドロップ機能の動作（通常権限で起動）
- [ ] ファイルパスに日本語が含まれても正常動作
- [ ] 長いパス（260文字以上）でも正常動作