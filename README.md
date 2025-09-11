# DocOrganizer

[![Version](https://img.shields.io/badge/version-3.0.073-blue.svg)](https://github.com/Rih0z/DocOrganizer/releases)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
[![Platform](https://img.shields.io/badge/platform-Windows-lightgrey.svg)](https://www.microsoft.com/windows)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

**CubePDF Utility互換のモダンなPDF編集・文書管理ツール**

## ⚡ 主な機能

- 📄 **PDF操作**: ドラッグ&ドロップでページ結合・分割・回転・削除
- 🖼️ **画像対応**: **HEIC完全対応**・JPG・PNG・JPEG・GIF形式からPDF変換
- 🔄 **向き自動補正**: スキャン文書の向き自動検出・修正
- ⌨️ **キーボード操作**: CubePDF互換の完全なショートカット対応
- ↩️ **Undo/Redo**: 全操作の取り消し・やり直し機能
- ⚡ **高性能**: V3アーキテクチャによる高速処理（60-70%高速化）

## 🚀 3ステップで使用開始

1. **📥 ダウンロード**: [Releases](https://github.com/Rih0z/DocOrganizer/releases)から`DocOrganizer.exe`を取得
2. **▶️ 起動**: エクスプローラーからダブルクリック（⚠️ **管理者権限厳禁**）
3. **🎯 操作**: ファイルをドラッグ&ドロップ → 整理 → PDF保存

## ⌨️ キーボードショートカット

### ファイル操作
| ショートカット | 機能 | 説明 |
|---------------|------|------|
| `Ctrl+N` | 新規作成 | 新しいドキュメントを作成 |
| `Ctrl+O` | 開く | PDFまたは画像ファイルを開く |
| `Ctrl+S` | 保存 | 現在のドキュメントを保存 |
| `Ctrl+Shift+S` | 名前を付けて保存 | 別名で保存 |

### 編集操作
| ショートカット | 機能 | 説明 |
|---------------|------|------|
| `Ctrl+Z` | 元に戻す | 直前の操作を取り消し |
| `Ctrl+Y` | やり直し | 取り消した操作をやり直し |
| `Delete` | 削除 | 選択したページを削除 |
| `Ctrl+A` | 全選択 | すべてのページを選択 |
| `Ctrl+D` | 全選択解除 | 選択を解除 |

### ページ操作（CubePDF互換）
| ショートカット | 機能 | 説明 |
|---------------|------|------|
| `Ctrl+L` | 左回転 | ページを左に90度回転 |
| `Ctrl+R` | 右回転 | ページを右に90度回転 |
| `Ctrl+B` | 上へ移動 | ページを1つ前に移動 |
| `Ctrl+F` | 下へ移動 | ページを1つ後に移動 |

### ナビゲーション
| ショートカット | 機能 | 説明 |
|---------------|------|------|
| `Ctrl+G` | ページジャンプ | 指定ページへ移動 |
| `Ctrl+Home` | 最初のページ | 先頭ページへ移動 |
| `Ctrl+End` | 最後のページ | 最終ページへ移動 |
| `Ctrl+←` | 前のページ | 1つ前のページへ |
| `Ctrl+→` | 次のページ | 1つ次のページへ |
| `F1` | ヘルプ | ヘルプを表示 |

## 📋 動作環境

- **OS**: Windows 10/11 (64-bit)
- **.NET**: .NET 8.0 ランタイム統合済み
- **その他**: インストール不要（自己完結型EXE）

## 📚 詳細ドキュメント

| 内容 | 参照先 |
|------|--------|
| **🚀 パフォーマンス最適化完全報告** ⭐NEW | [`docs/V3_Performance_Optimization_Complete_Report_20250911.md`](docs/V3_Performance_Optimization_Complete_Report_20250911.md) |
| **🎯 使用方法** | [`docs/PDF保存機能使用ガイド.md`](docs/PDF保存機能使用ガイド.md) |
| **🖼️ HEIC対応詳細** | [`docs/HEIC_Support_Complete_Guide.md`](docs/HEIC_Support_Complete_Guide.md) |
| **🏗️ V3完全アーキテクチャ** | [`docs/V3_COMPLETE_ARCHITECTURE.md`](docs/V3_COMPLETE_ARCHITECTURE.md) |
| **📱 画像表示システム** | [`docs/V3_ARCHITECTURE_IMAGE_DISPLAY.md`](docs/V3_ARCHITECTURE_IMAGE_DISPLAY.md) |
| **🔧 機能仕様** | [`docs/V3_ROTATION_AND_IMAGE_REPLACEMENT.md`](docs/V3_ROTATION_AND_IMAGE_REPLACEMENT.md) |
| **🎯 ドラッグ&ドロップ並び替え完全実装** ⭐ | [`docs/V3_Drag_Drop_Complete_Implementation_Report_20250822.md`](docs/V3_Drag_Drop_Complete_Implementation_Report_20250822.md) |
| **🔧 HEIC PDF出力バグ修正** | [`docs/HEIC_PDF_Export_Bug_Fix_Complete_Report_20250821.md`](docs/HEIC_PDF_Export_Bug_Fix_Complete_Report_20250821.md) |
| **🔍 UI拡大機能バグ修正** | [`docs/UI_Zoom_Feature_Bug_Fix_Complete_Report_20250821.md`](docs/UI_Zoom_Feature_Bug_Fix_Complete_Report_20250821.md) |
| **🐛 PDFサムネイル表示バグ修正** ⭐ | [`docs/BugFix_PDF_Thumbnail_Display_Report_20250904.md`](docs/BugFix_PDF_Thumbnail_Display_Report_20250904.md) |
| **📝 統一デバッグログ管理システム** ⭐ | [`docs/Debug_Logging_System_Complete_Report_20250904.md`](docs/Debug_Logging_System_Complete_Report_20250904.md) |
| **⚙️ 統一設定システム** ⭐ | [`docs/Unified_Configuration_System_Complete_Report_20250904.md`](docs/Unified_Configuration_System_Complete_Report_20250904.md) |
| **🔧 統一ログシステム実装完了** ⭐ | [`docs/Unified_Logging_System_Complete_Report_20250904.md`](docs/Unified_Logging_System_Complete_Report_20250904.md) |
| **🔧 環境変数仕様** | [`docs/ENVIRONMENT_VARIABLES.md`](docs/ENVIRONMENT_VARIABLES.md) |

## ⚙️ 設定システム

### 環境変数による制御

| 環境変数 | デフォルト値 | 説明 |
|----------|-------------|------|
| `DOCORGANIZER_DEBUG` | `true` | デバッグログ出力制御 |
| `DOCORGANIZER_LOG_PATH` | `.logs` | ログ出力ディレクトリ |

### 設定ファイル

- **統一設定**: `config/AppSettings.json`
- **詳細仕様**: [CLAUDE.md](CLAUDE.md)を参照

## 🛠️ 開発者向け

```bash
git clone https://github.com/Rih0z/DocOrganizer.git
cd DocOrganizer
dotnet publish src/DocOrganizer.UI/DocOrganizer.UI.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o release
```

## 🆘 サポート

- **🐛 不具合報告**: [GitHub Issues](https://github.com/Rih0z/DocOrganizer/issues)
- **📖 全ドキュメント**: [`docs/`](docs/)フォルダ参照
- **📦 最新版**: [リリースページ](https://github.com/Rih0z/DocOrganizer/releases)

---

**DocOrganizer V3.0.073** - プロフェッショナルな文書整理を簡単に