# DocOrganizer

[![Version](https://img.shields.io/badge/version-3.0.009-blue.svg)](https://github.com/Rih0z/DocOrganizer/releases)
[![.NET](https://img.shields.io/badge/.NET-6.0-purple.svg)](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
[![Platform](https://img.shields.io/badge/platform-Windows-lightgrey.svg)](https://www.microsoft.com/windows)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

**CubePDF Utility互換のモダンなPDF編集・文書管理ツール**

## ⚡ 主な機能

| 機能 | 説明 |
|------|------|
| 📄 **PDF操作** | ドラッグ&ドロップでページ結合・分割・回転・削除 |
| 🖼️ **画像対応** | **HEIC完全対応**・JPG・PNG・JPEG形式からPDF変換 |
| 🔄 **向き自動補正** | スキャン文書の向き自動検出・修正 |
| 🔄 **自動更新** | GitHub Releases連携による自動アップデート |
| ⚡ **高性能** | 高速処理とモダンなUI |

## 🚀 使い方

1. **ダウンロード**: [Releases](https://github.com/Rih0z/DocOrganizer/releases)から最新の`DocOrganizer.exe`を取得
2. **起動**: エクスプローラーからダブルクリック（⚠️ **管理者権限は厳禁**）
3. **操作**: ファイルをドラッグ&ドロップ → 整理 → PDF保存

## 📋 動作環境

- Windows 10/11 (64-bit)
- インストール不要（自己完結型）

## 🛠️ ビルド手順

```bash
git clone https://github.com/Rih0z/DocOrganizer.git
cd DocOrganizer
dotnet publish src/DocOrganizer.UI/DocOrganizer.UI.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o release
```

## 📚 ドキュメント

| 項目 | 場所 |
|------|------|
| **📄 PDF保存機能** | [`docs/PDF保存機能使用ガイド.md`](docs/PDF保存機能使用ガイド.md) |
| **🖼️ HEIC完全対応ガイド** | [`docs/HEIC_Support_Complete_Guide.md`](docs/HEIC_Support_Complete_Guide.md) |
| **📖 完全ガイド** | [`docs/PROJECT_OVERVIEW.md`](docs/PROJECT_OVERVIEW.md) |
| **🏗️ アーキテクチャ** | [`docs/PROJECT_OVERVIEW.md#technical-architecture`](docs/PROJECT_OVERVIEW.md#technical-architecture) |
| **🔧 開発ガイド** | [`docs/PROJECT_OVERVIEW.md#development-workflow`](docs/PROJECT_OVERVIEW.md#development-workflow) |
| **🧪 テスト** | [`scripts/test/`](scripts/test/) |
| **📋 ビルドガイド** | [`docs/build/`](docs/build/) |

## 🆘 サポート

- **🐛 不具合報告**: [GitHub Issues](https://github.com/Rih0z/DocOrganizer/issues)
- **📖 詳細情報**: [`docs/`](docs/)フォルダを参照
  - [V3アーキテクチャ - 画像表示の仕組み](docs/V3_ARCHITECTURE_IMAGE_DISPLAY.md)
  - [V3.0 回転・画像入れ替え機能](docs/V3_ROTATION_AND_IMAGE_REPLACEMENT.md)
- **📦 最新版**: [リリースページ](https://github.com/Rih0z/DocOrganizer/releases)

## 🏗️ プロジェクト構成

```
DocOrganizer/
├── src/           # 🔧 ソースコード（Clean Architecture）
├── tests/         # 🧪 単体・統合テスト
├── scripts/       # 📜 ビルド・自動化スクリプト
├── docs/          # 📚 ドキュメント
└── release/       # 📦 ビルド出力
```

---

**DocOrganizer** - プロフェッショナルな文書整理を簡単に