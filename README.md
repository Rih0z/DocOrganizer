# DocOrganizer

[![Version](https://img.shields.io/badge/version-3.0.129-blue.svg)](https://github.com/Rih0z/DocOrganizer/releases)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
[![Platform](https://img.shields.io/badge/platform-Windows-lightgrey.svg)](https://www.microsoft.com/windows)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

**CubePDF Utility互換のモダンなPDF編集・文書管理ツール**（最終更新: 2025-10-14 / Serena MCP分析済み）

## ⚡ 主な機能

- 📄 **PDF操作**: ドラッグ&ドロップでページ結合・分割・回転・削除
- 🖼️ **画像対応**: **HEIC/GIF/WebP/PSD完全対応**（6つのProvider実装）
- ⌨️ **キーボード操作**: **50+ショートカット完全実装**（CubePDF互換）
- 🎨 **シンプルUI**: ミニマルデザイン - メニュー2つ・ツールバー15個（**V3.0.128-129**）
- 🧭 **高度なナビゲーション**: Up/Downキーでページ移動（**V3.0.127**）
- 🖱️ **ドラッグ自動スクロール**: 最適化された速度制御（**V3.0.125-126**）
- ↩️ **Undo/Redo**: 全操作の取り消し・やり直し機能（V3.0.068）
- ⚡ **高性能**: ViewModel再利用・最適化済み（6つのViewModel・合計4731行）

## 🚀 3ステップで使用開始

1. **📥 ダウンロード**: [Releases](https://github.com/Rih0z/DocOrganizer/releases)から`DocOrganizer.exe`を取得
2. **▶️ 起動**: エクスプローラーからダブルクリック（⚠️ **管理者権限厳禁**）
3. **🎯 操作**: ファイルをドラッグ&ドロップ → 整理 → PDF保存

## 📦 V3.0.129 最新の改善

### 🎨 UI簡素化・ミニマルデザイン (V3.0.128-129)
- **メニューバーをPDF編集・ヘルプのみに簡素化**
- ツールバーアイコンで全操作可能（15個のボタン）
- 50+キーボードショートカット完全実装
- 視認性向上・直感的な操作

### 🧭 キーボードナビゲーション完全対応 (V3.0.127)
- **単独Up/Downキーでページ移動**（PageUp/PageDownと同じ動作）
- 回転後の強制1枚目移動バグを完全修正
- CubePDF Utility完全互換

### 🖱️ ドラッグ自動スクロール (V3.0.125-126)
- **最適化された速度制御**（3イベントに1回実行）
- 体感3倍減速で最大コントロール性実現
- 1px/イベント最低速度

### 🏗️ エンタープライズアーキテクチャ
- **ViewModelクラス**: 6つ・合計4731行
- **Provider Pattern**: 6つのProvider（HEIC/GIF/WebP/Standard/PDF/PSD）
- **Clean Architecture**: 完全な層分離
- **SOLID原則**: 完全実践

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
| `Ctrl+Z` | 元に戻す | 直前の操作を取り消し ⭐修正済み |
| `Ctrl+Y` | やり直し | 取り消した操作をやり直し ⭐修正済み |
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

### ナビゲーション ⭐V3.0.127強化
| ショートカット | 機能 | 説明 |
|---------------|------|------|
| **`Up`** | **前のページ** | **1つ前のページへ** ⭐NEW |
| **`Down`** | **次のページ** | **1つ次のページへ** ⭐NEW |
| `PageUp` | 前のページ | 1つ前のページへ |
| `PageDown` | 次のページ | 1つ次のページへ |
| `Home` | 最初のページ | 先頭ページへ移動 |
| `End` | 最後のページ | 最終ページへ移動 |
| `Ctrl+G` | ページジャンプ | 指定ページへ移動 |
| `Ctrl+H` / `F1` | ヘルプ | ヘルプを表示 |

**完全なショートカット一覧**: [`docs/guides/keyboard_shortcuts_guide.md`](docs/guides/keyboard_shortcuts_guide.md)

## 📋 動作環境

- **OS**: Windows 10/11 (64-bit)
- **.NET**: .NET 8.0 ランタイム統合済み
- **ファイルサイズ**: 107MB（単一実行ファイル）
- **その他**: インストール不要（自己完結型EXE）

## 📚 詳細ドキュメント

### アーキテクチャ（最新）
| 内容 | 参照先 |
|------|--------|
| **🏗️ V3完全アーキテクチャ** ⭐ | [`docs/architecture/V3_COMPLETE_ARCHITECTURE.md`](docs/architecture/V3_COMPLETE_ARCHITECTURE.md) |
| **📱 画像表示システム** | [`docs/architecture/V3_ARCHITECTURE_IMAGE_DISPLAY.md`](docs/architecture/V3_ARCHITECTURE_IMAGE_DISPLAY.md) |
| **🔧 回転と画像置換** | [`docs/architecture/V3_ROTATION_AND_IMAGE_REPLACEMENT.md`](docs/architecture/V3_ROTATION_AND_IMAGE_REPLACEMENT.md) |
| **🖱️ ドラッグ&ドロップ技術分析** | [`docs/architecture/drag_drop_architecture_analysis.md`](docs/architecture/drag_drop_architecture_analysis.md) |

### 運用ガイド
| 内容 | 参照先 |
|------|--------|
| **⌨️ キーボードショートカット完全ガイド** ⭐NEW | [`docs/guides/keyboard_shortcuts_guide.md`](docs/guides/keyboard_shortcuts_guide.md) |
| **🖼️ HEIC対応ガイド** | [`docs/guides/heic_support_guide.md`](docs/guides/heic_support_guide.md) |
| **📄 PDF保存ガイド** | [`docs/guides/pdf_save_guide.md`](docs/guides/pdf_save_guide.md) |
| **🔧 環境変数設定** | [`docs/guides/environment_variables.md`](docs/guides/environment_variables.md) |

### 開発規約
| 内容 | 参照先 |
|------|--------|
| **📋 プロジェクト構造** | [`docs/rule/project_structure.md`](docs/rule/project_structure.md) |
| **🔢 バージョン管理手順** | [`docs/rule/version_management.md`](docs/rule/version_management.md) |
| **🐛 デバッグログシステム** | [`docs/rule/debug_logging_system.md`](docs/rule/debug_logging_system.md) |

### 最新レポート（V3.0.127-129）
| 内容 | 参照先 |
|------|--------|
| **🎨 表示メニュー削除** | [`docs/reports/v3.0.129/`](docs/reports/v3.0.129/) |
| **🎨 UI簡素化実装** | [`docs/reports/v3.0.128/`](docs/reports/v3.0.128/) |
| **🧭 回転後キーボードナビゲーション修正** | [`docs/reports/v3.0.127/`](docs/reports/v3.0.127/) |

**全ドキュメント**: [`docs/README.md`](docs/README.md)を参照

## ⚙️ 設定システム

### 環境変数による制御

| 環境変数 | デフォルト値 | 説明 |
|----------|-------------|------|
| `DOCORGANIZER_DEBUG` | `true` | デバッグログ出力制御 |
| `DOCORGANIZER_LOG_PATH` | `.logs` | ログ出力ディレクトリ |
| `DOCORGANIZER_OCR_ENABLED` | `false` | OCR機能制御（V3.0.085で無効化） |

### 設定ファイル

- **統一設定**: `config/AppSettings.json`
- **詳細仕様**: [CLAUDE.md](CLAUDE.md)を参照

### 起動オプション

V3.0.085では起動用バッチファイルを提供：
- `run-normal.bat` - 通常起動（推奨）
- `run-debug.bat` - デバッグモード
- `run-with-ocr.bat` - OCR有効モード（将来用）

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

**DocOrganizer V3.0.129** - プロフェッショナルな文書整理を簡単に・高速に

---

**📊 V3アーキテクチャの成熟度**:
- ViewModelクラス: **6つ・合計4731行**
- Provider実装: **6クラス**（HEIC/GIF/WebP/Standard/PDF/PSD）
- キーボードショートカット: **50+完全実装**
- **127バージョン**の継続的改善を経て、エンタープライズグレードの成熟したアーキテクチャを実現