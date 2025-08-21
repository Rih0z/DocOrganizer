# DocOrganizer

[![Version](https://img.shields.io/badge/version-3.0.009-blue.svg)](https://github.com/Rih0z/DocOrganizer/releases)
[![.NET](https://img.shields.io/badge/.NET-6.0-purple.svg)](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
[![Platform](https://img.shields.io/badge/platform-Windows-lightgrey.svg)](https://www.microsoft.com/windows)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

**CubePDF Utility互換のモダンなPDF編集・文書管理ツール**

## ⚡ 主な機能

- 📄 **PDF操作**: ドラッグ&ドロップでページ結合・分割・回転・削除
- 🖼️ **画像対応**: **HEIC完全対応**・JPG・PNG・JPEG・GIF形式からPDF変換
- 🔄 **向き自動補正**: スキャン文書の向き自動検出・修正
- 🔄 **自動更新**: GitHub Releases連携による自動アップデート
- ⚡ **高性能**: V3アーキテクチャによる高速処理

## 🚀 3ステップで使用開始

1. **📥 ダウンロード**: [Releases](https://github.com/Rih0z/DocOrganizer/releases)から`DocOrganizer.exe`を取得
2. **▶️ 起動**: エクスプローラーからダブルクリック（⚠️ **管理者権限厳禁**）
3. **🎯 操作**: ファイルをドラッグ&ドロップ → 整理 → PDF保存

## 📋 動作環境

- **OS**: Windows 10/11 (64-bit)
- **その他**: インストール不要（自己完結型EXE）

## 📚 詳細ドキュメント

| 内容 | 参照先 |
|------|--------|
| **🎯 使用方法** | [`docs/PDF保存機能使用ガイド.md`](docs/PDF保存機能使用ガイド.md) |
| **🖼️ HEIC対応詳細** | [`docs/HEIC_Support_Complete_Guide.md`](docs/HEIC_Support_Complete_Guide.md) |
| **🏗️ 技術仕様** | [`docs/V3_ARCHITECTURE_IMAGE_DISPLAY.md`](docs/V3_ARCHITECTURE_IMAGE_DISPLAY.md) |
| **🔧 機能仕様** | [`docs/V3_ROTATION_AND_IMAGE_REPLACEMENT.md`](docs/V3_ROTATION_AND_IMAGE_REPLACEMENT.md) |

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

**DocOrganizer V3.0.009** - プロフェッショナルな文書整理を簡単に