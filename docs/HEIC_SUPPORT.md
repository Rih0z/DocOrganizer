# HEIC画像対応仕様書

## 概要

DocOrganizer 2.2では、HEIC (High Efficiency Image Container) 形式の画像ファイルに完全対応しています。iPhoneやiPadで撮影された画像の高品質プレビュー表示と編集機能を提供します。

## サポートされる形式

- `.heic` - HEIC形式（推奨）
- `.heif` - HEIF形式

## 主要機能

### 1. 高品質プレビュー表示
- **解像度**: 最大1200x1600ピクセル
- **品質**: 98%品質設定 + 300DPI
- **処理**: Lanczosフィルター + シャープニング適用
- **出力**: 無圧縮PNG形式で最終表示

### 2. 自動向き補正
- EXIF情報に基づく自動回転
- 手動回転操作も対応（90°、180°、270°）

### 3. PDF変換
- 高品質HEIC → PDF変換
- メタデータ保持
- ファイルサイズ最適化

## アーキテクチャ

### 処理フロー

```
HEICファイル投下
     ↓
ImageProcessingService.GenerateHighQualityPreviewAsync()
     ↓ (品質98% + DPI300 + シャープニング)
MainViewModel.UpdatePreview()
     ↓ (HEIC判定による強制高解像度処理)  
高品質プレビュー表示 (1200x1600)
```

### コンポーネント構成

#### 1. **ImageProcessingService** (`src/DocOrganizer.Infrastructure/Services/`)
- `GenerateHighQualityPreviewAsync()` - HEIC専用高品質プレビュー生成
- `GenerateHeicHighQualityPreviewAsync()` - HEIC最適化処理
- ImageMagick.NET + Magick.NET使用

#### 2. **PageViewModel** (`src/DocOrganizer.UI/ViewModels/`)
- サムネイル(150x150)とプレビュー処理の完全分離
- 低品質サムネイルの高品質プレビューへの誤用防止

#### 3. **MainViewModel** (`src/DocOrganizer.UI/ViewModels/`)  
- `IsHeicFile()` - HEIC判定
- HEIC強制高解像度プレビュー条件分岐
- 元画像パス活用による品質保持

## 技術仕様

### 品質設定

| 項目 | サムネイル用 | プレビュー用 |
|-----|-------------|-------------|
| 解像度 | 150x150px | 1200x1600px |
| 品質 | 80% | 98% |
| DPI | 72 | 300 |
| フィルター | Standard | Lanczos |
| シャープニング | なし | あり |

### パフォーマンス

- **初回表示**: 0.5-2秒（ファイルサイズ依存）
- **メモリ使用量**: 追加20-50MB（一時的）
- **CPU使用率**: 中程度（プレビュー生成時のみ）

### 互換性

- **Windows**: 完全対応（Windows Imaging Component + ImageMagick）
- **依存関係**: 
  - Magick.NET-Q16-AnyCPU v13.3.0以上
  - ImageMagick-7.1.0以上

## 使用方法

### 基本操作

1. **ファイル読み込み**
   - HEICファイルをアプリにドラッグ&ドロップ
   - または「ファイルを開く」からHEICファイルを選択

2. **プレビュー表示**
   - 自動的に高品質プレビューが生成される
   - 文字やテキストが鮮明に表示される

3. **編集操作**
   - 回転: 90°単位での回転
   - 削除: 不要ページの削除
   - 並び替え: ドラッグ&ドロップで順序変更

4. **PDF変換・保存**
   - 「PDFとして保存」で高品質PDF出力
   - 元解像度維持

### トラブルシューティング

#### Q: HEICファイルが読み込めない
**A:** 以下を確認してください：
- Windowsの「HEIF画像拡張機能」がインストールされているか
- ファイルが破損していないか
- 管理者権限でアプリを起動していないか（ドラッグ&ドロップが無効化される）

#### Q: プレビューがぼやけて見える
**A:** 以下の場合に発生する可能性があります：
- 古いバージョンのDocOrganizerを使用している
- HEICファイルが既に低解像度で保存されている
- 十分なメモリが確保できていない

#### Q: 処理が遅い
**A:** 以下で改善できます：
- 不要なアプリケーションを終了してメモリを確保
- HEICファイルサイズが大きい場合は処理時間が長くなることは正常

## 更新履歴

### V2.2.0 (2025-08-07)
- **新機能**: HEIC高品質プレビュー機能実装
- **改善**: サムネイル/プレビュー処理分離によるアーキテクチャ最適化
- **修正**: プレビュー品質劣化問題の根本的解決

### 技術的な改善点
- ImageMagick品質設定: 80% → 98%
- プレビュー解像度: 600px → 1200px
- DPI設定: 標準 → 300DPI
- フィルタリング: Standard → Lanczos + シャープニング

## 開発者向け情報

### カスタマイズ

品質設定のカスタマイズは `ImageProcessingService.cs` で可能：

```csharp
// 品質調整（95-99推奨）
magickImage.Quality = 98;

// DPI調整（200-400推奨）  
magickImage.Density = new ImageMagick.Density(300, 300);

// 最大解像度調整
int maxWidth = 1200;  // デフォルト
int maxHeight = 1600; // デフォルト
```

### API拡張

新しい高品質プレビューAPI：

```csharp
Task<SkiaSharp.SKBitmap?> GenerateHighQualityPreviewAsync(
    string imagePath, 
    int maxWidth = 1200, 
    int maxHeight = 1600
);
```

## ライセンス

このHEIC対応機能は以下のオープンソースライブラリを使用しています：

- **ImageMagick**: Apache 2.0 License  
- **Magick.NET**: Apache 2.0 License
- **SkiaSharp**: MIT License

---

**DocOrganizer 2.2 - HEIC高品質対応版**  
最終更新: 2025-08-07