# プロジェクト構造

## プロジェクト基本情報

- **プロジェクト名**: DocOrganizer - CubePDF Utility互換 汎用PDF編集ツール
- **現在バージョン**: V3.0.031
- **GitHubリポジトリ**: https://github.com/Rih0z/DocOrganizer
- **最新EXE**: `C:\Users\217216X721451\github\DocOrganizer\release\DocOrganizer.exe`

## 主要機能
- PDF編集機能（CubePDF Utility互換）
- 画像→PDF変換（HEIC/JPG/PNG/JPEG対応）
- ドラッグ&ドロップでのファイル操作
- 自動アップデート機能（GitHub Releases連携）
- ページ回転・削除・並び替え
- PDF結合・分割

## ディレクトリ構造

```
DocOrganizer/
├── src/                    # ソースコード
│   ├── DocOrganizer.Core/              # ドメイン層
│   ├── DocOrganizer.Application/       # アプリケーション層
│   ├── DocOrganizer.Infrastructure/    # インフラストラクチャ層
│   └── DocOrganizer.UI/               # プレゼンテーション層（WPF）
├── tests/                  # テストプロジェクト
├── docs/                   # ドキュメント
│   └── rule/              # 開発規則・ガイドライン
├── tmp/                    # 一時分析・計画ファイル
├── release/               # ビルド出力・実行ファイル
│   ├── DocOrganizer.exe
│   ├── run-debug.bat     # デバッグ起動スクリプト
│   └── run-production.bat # 本番起動スクリプト
├── sample/                # テスト用サンプルファイル
├── scripts/               # ユーティリティスクリプト
├── README.md              # プロジェクト説明
└── CLAUDE.md              # AI開発原則（簡潔版）
```

## ビルドコマンド

### 標準ビルド
```bash
dotnet publish src/DocOrganizer.UI/DocOrganizer.UI.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o release
```

### クリーンビルド
```bash
dotnet clean
dotnet restore
dotnet build --configuration Release
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o release
```

## アーキテクチャ

### Clean Architecture + Provider Pattern + MVVM
- **UI Layer**: WPF MVVM
- **Application Layer**: サービス・インターフェース
- **Infrastructure Layer**: Provider実装・外部ライブラリ連携
- **Core Layer**: ドメインモデル・ビジネスロジック

### Provider Pattern実装
- ImageProcessingProvider（画像処理）
- ValidationProvider（検証）
- PdfRenderingService（PDF処理）

## 技術スタック
- **.NET**: 6.0
- **UI**: WPF (Windows Presentation Foundation)
- **PDF**: PDFsharp, PdfiumViewer
- **画像**: ImageSharp, Magick.NET
- **OCR**: IronOCR
- **DI**: Microsoft.Extensions.DependencyInjection