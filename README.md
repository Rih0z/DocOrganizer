# DocOrganizer V2.2 - CubePDF互換版

## 概要

DocOrganizer V2.2は、CubePDF UtilityのUIデザインに準拠したPDF編集ツールです。
V2.1のMaterial DesignからWindows標準UIに変更し、CubePDFユーザーに馴染みやすいインターフェースを提供します。

## V2.2の主な変更点

### UI/UXの変更
- **Material Design → Windows標準UI**: CubePDF Utilityと同じ操作感
- **シンプルな2ペインレイアウト**: 左側サムネイル、右側プレビュー
- **標準的なツールバー**: Windowsアプリケーションの標準的な配置
- **クラシックな色調**: グレー基調のWindows標準カラー

### 操作性の改善
- **標準的な選択方式**: Ctrl+クリック、Shift+クリックでの複数選択
- **Windows標準のドラッグ&ドロップ**: 視覚的にシンプルな操作
- **右クリックメニュー**: コンテキストメニューの充実

## 技術仕様

### システム要件
- Windows 10/11 (64-bit)
- .NET 9.0 Runtime
- 4GB以上のメモリ推奨

### 使用技術
- WPF (Windows Presentation Foundation)
- Windows標準コントロール
- iText7 (PDF処理)
- MVVM パターン

## 機能一覧

### 基本機能（CubePDF Utility互換）
- [ ] PDFファイルの開く/保存
- [ ] ページサムネイル表示
- [ ] ページの回転（90度単位）
- [ ] ページの削除
- [ ] ページの並び替え（ドラッグ&ドロップ）
- [ ] PDF結合
- [ ] PDF分割
- [ ] セキュリティ設定

### 拡張機能（文書整理特化）
- [ ] 高度なファイル名生成
- [ ] 文書タイプ分類
- [ ] 定型並び順テンプレート
- [ ] 自動フォルダ整理

## 開発ガイド

### ビルド手順
```bash
# リポジトリのクローン
git clone https://github.com/Rih0z/DocOrganizer.git
cd DocOrganizer

# 依存関係の復元
dotnet restore

# ビルド
dotnet build --configuration Release

# 実行
dotnet run --project src/DocOrganizer.UI
```

### プロジェクト構造
```
DocOrganizer/
├── src/
│   ├── DocOrganizer.Core/        # ドメインモデル
│   ├── DocOrganizer.Application/ # アプリケーション層
│   ├── DocOrganizer.Infrastructure/ # インフラ層
│   └── DocOrganizer.UI/         # プレゼンテーション層
├── tests/                        # テストプロジェクト
├── docs/                         # ドキュメント
└── samples/                      # サンプルファイル
```

## ライセンス
MIT License

## 貢献
プルリクエストを歓迎します。大きな変更の場合は、まずissueを作成して変更内容を議論してください。

## 作者
DocOrganizer Project Team