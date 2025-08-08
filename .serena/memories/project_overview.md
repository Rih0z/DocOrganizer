# DocOrganizer プロジェクト概要

## プロジェクト目的
CubePDF Utility互換のモダンなPDF編集・文書管理ツール。画像からPDF変換機能を持つWindows専用アプリケーション。

## 技術スタック
- **フレームワーク**: .NET 6.0 (WPF - Windows専用)
- **アーキテクチャ**: Clean Architecture (4層構造)
- **PDF処理**: PDFsharp, PdfSharp.ImageSharp
- **画像処理**: ImageSharp, Magick.NET (HEIC対応)
- **UI**: WPF + Material Design In XAML
- **テスト**: xUnit
- **依存性注入**: Microsoft.Extensions.DependencyInjection

## プロジェクト構造
```
DocOrganizer/
├── src/
│   ├── DocOrganizer.Core/          # ドメイン層 (エンティティ・ビジネスルール)
│   ├── DocOrganizer.Application/   # アプリケーション層 (ユースケース・インターフェース)
│   ├── DocOrganizer.Infrastructure/ # インフラ層 (外部サービス・データアクセス)
│   └── DocOrganizer.UI/            # プレゼンテーション層 (WPF・MVVM)
├── tests/                          # 単体・統合テスト
├── scripts/                        # ビルド・テスト自動化スクリプト
└── docs/                          # プロジェクトドキュメント
```

## 主要機能
1. **PDF操作**: ドラッグ&ドロップでページ結合・分割・回転・削除
2. **画像対応**: HEIC・JPG・PNG・JPEG形式からPDF変換
3. **向き自動補正**: スキャン文書の向き自動検出・修正
4. **自動更新**: GitHub Releases連携による自動アップデート
5. **高性能**: 高速処理とモダンなUI

## 主要サービス
- **IPdfService**: PDF読み込み・保存・操作
- **IImageProcessingService**: 画像処理・HEIC変換・サムネイル生成
- **IPdfEditorService**: PDF編集操作（回転・削除・並び替え）
- **IUpdateService**: 自動アップデート機能

## 開発環境要件
- Windows 10/11 (64-bit)
- .NET 6.0 SDK
- Visual Studio 2022 (推奨)

## 現在のバージョン
- Version: 2.2.0
- 対象フレームワーク: .NET 6.0
- プラットフォーム: win-x64 (自己完結型)