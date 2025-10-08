# プロジェクト構造

## プロジェクト基本情報

- **プロジェクト名**: DocOrganizer - CubePDF Utility互換 汎用PDF編集ツール
- **現在バージョン**: V3.0.123
- **GitHubリポジトリ**: https://github.com/Rih0z/DocOrganizer
- **デフォルトEXE**: `C:\Users\217216X721451\github\DocOrganizer\release-debug\DocOrganizer.exe`
- **リリースEXE**: `C:\Users\217216X721451\github\DocOrganizer\release\DocOrganizer.exe`（明示的指示時）

## 主要機能

### コア機能
- PDF編集機能（CubePDF Utility互換）
- 画像→PDF変換（HEIC/JPG/PNG/JPEG対応）
- ドラッグ&ドロップでのファイル操作
- ページ回転・削除・並び替え
- PDF結合・分割

### V3実装（V3.0.068以降の主要機能）
- **Undo/Redo完全実装**（V3.0.068）
- **複数ページ一括移動**（V3.0.117）
- **複数選択ドラッグ&ドロップ**（V3.0.116）
- **ズーム機能**（V3.0.110）
- **複数選択対応**（V3.0.103）
- **統一ログ管理システム**（V3.0.064）
- **パフォーマンス最適化**（ViewModelの再利用、V3.0.073）

## ディレクトリ構造

```
DocOrganizer/
├── src/                    # ソースコード
│   ├── DocOrganizer.Core/              # ドメイン層
│   │   ├── Commands/                   # コマンドパターン実装
│   │   │   ├── MovePagesCommand.cs    # ページ移動（V3.0.123最適化）
│   │   │   ├── RotatePagesCommand.cs  # ページ回転
│   │   │   ├── DeletePagesCommand.cs  # ページ削除
│   │   │   └── IUndoableCommand.cs    # Undo/Redo基底
│   │   └── Models/                     # ドメインモデル
│   │
│   ├── DocOrganizer.Application/       # アプリケーション層
│   │   └── Services/                   # アプリケーションサービス
│   │
│   ├── DocOrganizer.Infrastructure/    # インフラストラクチャ層
│   │   ├── Providers/                  # プロバイダー実装
│   │   └── Services/                   # 外部ライブラリ連携
│   │
│   └── DocOrganizer.UI/               # プレゼンテーション層（WPF）
│       ├── ViewModels/V3/              # V3 MVVM実装
│       │   ├── DragDropHandlerViewModel.cs    # D&D処理
│       │   ├── PageOperationViewModel.cs      # ページ操作
│       │   └── MainCompositeViewModel.cs      # メイン複合ViewModel
│       ├── Behaviors/                  # WPF Behavior
│       │   └── V3AdvancedDragDropBehavior.cs
│       └── Views/                      # WPF View
│
├── tests/                  # テストプロジェクト
│
├── docs/                   # ドキュメント
│   ├── architecture/       # アーキテクチャ文書
│   ├── guides/            # 運用ガイド
│   ├── reports/           # 最新の重要レポート
│   ├── rule/              # 開発規則・ガイドライン
│   └── archive/           # 過去のレポート（月別）
│
├── .tmp/                   # 一時分析・計画ファイル（.gitignore対象）
├── .logs/                  # デバッグログ出力先（V3.0.064実装）
│   └── debug.log
│
├── release-debug/         # デフォルトビルド出力（デバッグログ有効）
│   ├── DocOrganizer.exe
│   ├── run-debug.bat
│   └── run-production.bat
│
├── release/               # リリースビルド出力（明示的指示時）
│   ├── DocOrganizer.exe
│   ├── run-debug.bat
│   └── run-production.bat
│
├── sample/                # テスト用サンプルファイル
├── scripts/               # ユーティリティスクリプト
├── README.md              # プロジェクト説明
└── CLAUDE.md              # AI開発原則
```

## ビルドコマンド

### デフォルト: release-debugビルド（デバッグログ有効）
```bash
cd C:\Users\217216X721451\github\DocOrganizer
dotnet clean
dotnet restore
dotnet build --configuration Release
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o release-debug
```

**実行ファイル**: `release-debug\DocOrganizer.exe`

### リリース版ビルド（ユーザーから明示的指示がある場合のみ）
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o release
```

**実行ファイル**: `release\DocOrganizer.exe`

### ビルド後の確認
- エクスプローラーから起動（**管理者権限での起動は厳禁**）
- デバッグログ: `.logs/debug.log`を確認

## アーキテクチャ

### Clean Architecture + Provider Pattern + MVVM
- **UI Layer**: WPF MVVM
- **Application Layer**: サービス・インターフェース
- **Infrastructure Layer**: Provider実装・外部ライブラリ連携
- **Core Layer**: ドメインモデル・ビジネスロジック

### コマンドパターン（V3.0.068）
- `IUndoableCommand`: Undo/Redo可能なコマンドインターフェース
- `MovePagesCommand`: ページ移動コマンド（V3.0.123で処理順序最適化）
- `RotatePagesCommand`: ページ回転コマンド
- `DeletePagesCommand`: ページ削除コマンド
- `BatchCommand`: 複数コマンドの一括実行

### Provider Pattern実装
- ImageProcessingProvider（画像処理）
- ValidationProvider（検証）
- PdfRenderingService（PDF処理）

## 技術スタック

### フレームワーク
- **.NET**: 6.0
- **UI**: WPF (Windows Presentation Foundation)

### PDF処理
- **PDFsharp**: PDF生成・編集
- **PdfiumViewer**: PDFレンダリングエンジン（V3.0.030採用）

### 画像処理
- **ImageSharp**: 画像変換・処理
- **Magick.NET**: 画像余白削除・高度な画像処理

### OCR
- **IronOCR**: OCR機能

### DI・ログ
- **Microsoft.Extensions.DependencyInjection**: 依存性注入
- **DebugLogger**: 統一ログ管理（V3.0.064実装）

## デバッグログシステム（V3.0.064実装）

### 環境変数
```powershell
# デバッグON
$env:DOCORGANIZER_DEBUG = "true"

# デバッグOFF
$env:DOCORGANIZER_DEBUG = "false"
```

### ログ出力先
- **パス**: `.logs/debug.log`
- **フォーマット**: `[yyyy-MM-dd HH:mm:ss] [カテゴリ] メッセージ`

### 起動スクリプト
- `release-debug\run-debug.bat`: デバッグモード起動
- `release-debug\run-production.bat`: 本番モード起動

## V3.0.123 最新実装詳細

### MovePagesCommand（複数ページ移動修正）
```csharp
// 🎯 V3.0.123: 複数ページ移動時の位置ズレ修正
// 移動方向を判定し、適切な順序で処理

// 下移動: 後ろから処理（降順） - 前のページに影響しない
// 上移動: 前から処理（昇順） - 後ろのページに影響しない
```

**実装ファイル**: `src/DocOrganizer.Core/Commands/MovePagesCommand.cs:98-125`

### V3アーキテクチャ文書
- **[V3完全アーキテクチャ](../architecture/V3_COMPLETE_ARCHITECTURE.md)**
- **[画像表示システム](../architecture/V3_ARCHITECTURE_IMAGE_DISPLAY.md)**
- **[回転と画像置換](../architecture/V3_ROTATION_AND_IMAGE_REPLACEMENT.md)**

## 関連ドキュメント

### 開発規約
- **[debug_logging_system.md](debug_logging_system.md)** - デバッグログシステム詳細
- **[version_management.md](version_management.md)** - バージョン管理手順
- **[development_principles.md](development_principles.md)** - 開発原則

### 運用ガイド
- **[環境変数](../guides/environment_variables.md)** - 環境変数設定
- **[HEIC対応ガイド](../guides/heic_support_guide.md)** - HEIC画像処理
- **[PDF保存ガイド](../guides/pdf_save_guide.md)** - PDF保存機能
- **[Ghostscript不要実装](../guides/ghostscript_free_implementation.md)** - Ghostscript依存削除

### 最新レポート
- **[複数選択移動修正](../reports/v3.0.123_multiple_selection_move_fix.md)** - V3.0.123
- **[複数選択UI修正](../reports/v3.0.122_multiple_selection_ui_fix.md)** - V3.0.122
- **[ズーム機能修正](../reports/v3.0.110_zoom_feature_fix.md)** - V3.0.110

## 重要な注意事項

1. **管理者権限で起動しない** - ドラッグ&ドロップが無効になる
2. **デフォルトはrelease-debugビルド** - デバッグログ有効版
3. **ログはDebugLogger使用** - ハードコード禁止
4. **Git同期を忘れずに** - 作業前後で必ず実行
5. **バージョン更新は必須** - ビルド時に最後の桁を+1
