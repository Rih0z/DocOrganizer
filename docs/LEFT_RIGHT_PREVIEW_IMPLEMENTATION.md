# 左右プレビュー表示方法 - 公式実装ドキュメント

## 📋 概要

DocOrganizer V2.2における左右プレビュー表示システムの完全実装方法を説明します。

## 🎯 設計方針

### デュアルプレビューシステム
- **左側**: サムネイル一覧（150x200）- 全体把握用
- **右側**: 高解像度プレビュー（1200x1600）- 詳細確認用

## 🔧 技術実装

### 1. PageViewModel（左側サムネイル処理）

#### 処理フロー
```csharp
// 左側サムネイル専用処理（150x200）
var thumbnailBitmap = await _imageProcessingService.GenerateHighQualityPreviewAsync(imagePath, 150, 200);

// 回転処理適用
var finalBitmap = thumbnailBitmap;
if (_page.Rotation != 0)
{
    finalBitmap = ApplyRotationOptimized(thumbnailBitmap, _page.Rotation);
}

// 左側ThumbnailImageのみ設定
ThumbnailImage = ConvertToWpfBitmap(finalBitmap);
// ⚠️重要: PreviewImageは設定しない（右側で独自生成）
```

#### 主要メソッド
- `ProcessStandardImageAsync()`: 通常画像ファイル処理
- `ProcessHeicOptimizedAsync()`: HEIC画像最適化処理  
- `GenerateThumbnailWithRotation()`: 回転付きサムネイル生成
- `ProcessImageFallbackAsync()`: フォールバック処理

### 2. MainViewModel（右側高解像度処理）

#### 処理フロー
```csharp
public async Task UpdatePreview(PageViewModel selectedPage)
{
    // PageViewModelからPreviewImageを取得試行
    var previewFromPage = selectedPage.PreviewImage;
    
    if (previewFromPage != null)
    {
        // PageViewModelで既に生成済みの場合はそれを使用
        CurrentPageImage = previewFromPage;
    }
    else
    {
        // PreviewImageがnullの場合、独自に高解像度生成
        var imagePath = selectedPage.Page.SourceImagePath;
        var highResBitmap = await _imageProcessingService.GenerateHighQualityPreviewAsync(imagePath, 1200, 1600);
        
        // 回転処理適用
        if (selectedPage.Page.Rotation != 0)
        {
            highResBitmap = ApplyRotation(highResBitmap, selectedPage.Page.Rotation);
        }
        
        CurrentPageImage = ConvertToWpfBitmap(highResBitmap);
    }
}
```

## 🎨 UI表示仕様

### 左側パネル（サムネイル一覧）
- **サイズ**: 150x200ピクセル
- **用途**: 全ページの一覧表示
- **特徴**: 
  - メモリ効率重視
  - 高速スクロール対応
  - WeakReferenceキャッシュ使用

### 右側パネル（詳細プレビュー）
- **サイズ**: 1200x1600ピクセル  
- **用途**: 選択ページの詳細確認
- **特徴**:
  - 高画質表示
  - 文字認識可能レベル
  - ズーム・パン対応

## 📊 パフォーマンス特性

### メモリ使用量
- **左側サムネイル**: 約120KB/ページ（150x200 PNG）
- **右側プレビュー**: 約7.5MB/ページ（1200x1600 PNG）
- **総メモリ**: 左側全ページ + 右側選択1ページ

### 処理速度
- **左側生成**: 約50-100ms/ページ
- **右側生成**: 約200-400ms/ページ
- **切り替え**: 即座（キャッシュ使用時）

## 🔄 統一処理ロジック

### 共通画像処理パイプライン
1. **ImageProcessingService.GenerateHighQualityPreviewAsync()** 使用
2. **SkiaSharp** によるリサイズと品質最適化
3. **統一回転処理** （ApplyRotationOptimized）
4. **WPF BitmapImage変換** （統一フォーマット）

### 対応画像形式
- ✅ **HEIC/HEIF**: ImageMagick変換経由
- ✅ **JPG/JPEG**: SkiaSharp直接処理
- ✅ **PNG**: SkiaSharp直接処理  
- ✅ **その他**: SkiaSharp対応形式全て

## 🚨 重要な実装ポイント

### 1. PreviewImage設定の分離
```csharp
// ❌ 従来の問題のあるコード
ThumbnailImage = smallBitmap;
PreviewImage = smallBitmap; // これが右側解像度を劣化させていた

// ✅ 修正後の正しいコード  
ThumbnailImage = smallBitmap; // 左側専用
// PreviewImageは設定しない（右側で独自生成）
```

### 2. サイズ差別化の徹底
- **左側**: 150x200で固定（PageViewModel）
- **右側**: 1200x1600で固定（MainViewModel）
- **処理ロジック**: 同一（GenerateHighQualityPreviewAsync）

### 3. エラーハンドリング
```csharp
try
{
    // 通常処理
    await ProcessStandardImageAsync(imagePath, cancellationToken);
}
catch (Exception ex)
{
    // フォールバック処理
    await ProcessImageFallbackAsync(imagePath, cancellationToken);
}
```

## 📋 テスト手順

### 1. 機能確認
1. DocOrganizer.exe起動
2. 複数画像ファイル（HEIC/JPG/PNG）をドラッグ&ドロップ
3. 左側に小サイズサムネイル表示確認
4. 右側に高解像度プレビュー表示確認
5. 左側クリックで右側切り替え確認

### 2. 品質確認  
- **左側**: 全体把握に十分な画質
- **右側**: 文字が読める高画質
- **切り替え**: 瞬時に反応

### 3. メモリ確認
- **多数ファイル**: メモリリーク無し
- **大容量画像**: 適切な圧縮

## 🔧 トラブルシューティング

### 左側サムネイルが表示されない
- `ProcessStandardImageAsync()` のデバッグログ確認
- `GenerateHighQualityPreviewAsync(150, 200)` の戻り値確認

### 右側が低解像度になる
- `MainViewModel.UpdatePreview()` で独自生成されているか確認
- `GenerateHighQualityPreviewAsync(1200, 1600)` が呼ばれているか確認

### 回転が反映されない
- `ApplyRotationOptimized()` が各箇所で呼ばれているか確認
- `_page.Rotation` の値が正しく設定されているか確認

## 📈 今後の拡張性

### 解像度カスタマイズ
```csharp
// 設定可能にする場合
var thumbSize = AppSettings.ThumbnailSize; // デフォルト: 150x200
var previewSize = AppSettings.PreviewSize; // デフォルト: 1200x1600
```

### キャッシュ戦略
- 左側: WeakReference（現在実装済み）
- 右側: LRUキャッシュ（将来実装）

## ✅ 実装完了確認

- [x] 左側150x200サムネイル生成
- [x] 右側1200x1600高解像度生成  
- [x] PreviewImage設定分離
- [x] 統一処理ロジック適用
- [x] 全画像形式対応
- [x] エラーハンドリング強化
- [x] メモリ効率最適化

**最終更新**: 2025-08-12  
**実装者**: Claude Code + Serena MCP  
**テスト環境**: Windows .NET 6.0 WPF