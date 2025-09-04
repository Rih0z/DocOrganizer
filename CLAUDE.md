# Claude.md - DocOrganizer AIコーディング原則（簡潔版）

```yaml
ai_coding_principles:
  version: "3.0"
  last_updated: "2025-09-04"
  project: "DocOrganizer - CubePDF Utility互換 汎用PDF編集ツール"
  current_version: "3.0.031"
```

## 🎯 必須確認事項

**作業開始前に必ず以下のドキュメントを確認すること：**

1. **[開発原則](docs/rule/development_principles.md)** - 第1条〜第17条の必須規則
2. **[プロジェクト構造](docs/rule/project_structure.md)** - ディレクトリ構成・技術スタック
3. **[バージョン管理](docs/rule/version_management.md)** - バージョン更新手順
4. **[デバッグログ](docs/rule/debug_logging_system.md)** - ログ出力規則

## ⚡ クイックリファレンス

### 現在の情報
- **バージョン**: V3.0.031
- **GitHub**: https://github.com/Rih0z/DocOrganizer
- **EXE**: `C:\Users\217216X721451\github\DocOrganizer\release\DocOrganizer.exe`

### 必須コマンド
```bash
# 作業開始
git pull origin main

# ビルド
dotnet clean
dotnet restore
dotnet build --configuration Release
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o release

# 起動
release\run-debug.bat      # デバッグモード
release\run-production.bat  # 本番モード
```

### デバッグ設定
```powershell
# 環境変数
$env:DOCORGANIZER_DEBUG = "true"   # デバッグON
$env:DOCORGANIZER_DEBUG = "false"  # デバッグOFF
```

## 📚 主要ドキュメント

### アーキテクチャ
- [V3完全アーキテクチャ](docs/V3_COMPLETE_ARCHITECTURE.md)
- [画像表示システム](docs/V3_ARCHITECTURE_IMAGE_DISPLAY.md)

### 最新実装
- [PDFサムネイル表示バグ修正](docs/BugFix_PDF_Thumbnail_Display_Report_20250904.md)
- [統一ログ管理システム](docs/Debug_Logging_System_Complete_Report_20250904.md)
- [ドラッグ&ドロップ実装](docs/V3_Drag_Drop_Complete_Implementation_Report_20250822.md)

## 🚨 重要な注意事項

1. **管理者権限で起動しない** - ドラッグ&ドロップが無効になる
2. **ビルド時はバージョン更新** - 第17条に従う
3. **ログはDebugLogger使用** - ハードコード禁止
4. **Git同期を忘れずに** - 作業前後で必ず実行

## 📋 バージョン履歴（最新5件）

| バージョン | 日付 | 主な変更 |
|-----------|------|----------|
| V3.0.031 | 2025-09-03 | PDF表示バグ完全修正 |
| V3.0.030 | 2025-09-03 | PdfiumViewerエンジン採用 |
| V3.0.028 | 2025-09-03 | GhostScript依存排除 |
| V3.0.026 | 2025-08-22 | PDF Provider本格運用 |
| V3.0.025 | 2025-08-22 | ドラッグ&ドロップ実装 |

[完全な履歴はdocs/rule/version_management.mdを参照]

---

**重要**: 詳細な規則・手順は必ず`docs/rule/`フォルダ内のドキュメントを参照すること