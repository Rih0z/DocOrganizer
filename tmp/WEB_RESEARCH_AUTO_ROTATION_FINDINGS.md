# Web調査結果: 自動回転実装方法の決定

## 📋 調査結果まとめ

### 1. ImageSharp 3.x AutoOrient() のベストプラクティス

#### 基本的な使用方法
```csharp
// ✅ 推奨: AutoOrientは他のMutate操作の前に実行
image.Mutate(x => x.AutoOrient().Resize(width, height));

// ❌ 問題: AutoOrientを後で実行すると幅・高さが混乱
image.Mutate(x => x.Resize(width, height).AutoOrient());
```

#### EXIF Orientationリセット
```csharp
// AutoOrient後にEXIF Orientation値を1（Normal）に設定
if (image.Metadata.ExifProfile != null)
{
    image.Metadata.ExifProfile.SetValue(ExifTag.Orientation, (ushort)1);
}
```

### 2. EXIF Orientation値の正確な解釈表

| EXIF値 | 回転角度 | 説明 | 対応アクション |
|--------|----------|------|----------------|
| 1 | 0° | 正常（上が上） | 何もしない |
| 2 | 0° | 水平反転 | FlipHorizontally |
| 3 | 180° | 180度回転 | Rotate(180) |
| 4 | 0° | 垂直反転 | FlipVertically |
| 5 | 90° | 90度回転 + 水平反転 | Rotate(90) + FlipHorizontally |
| 6 | 90° | 時計回り90度回転 | Rotate(90) |
| 7 | 270° | 270度回転 + 水平反転 | Rotate(270) + FlipHorizontally |
| 8 | 270° | 反時計回り90度回転 | Rotate(270) |

**重要**: 実際の画像では主に1, 3, 6, 8のみが使用される

### 3. SkiaSharp回転処理のパフォーマンス最適化

#### メモリ管理の問題
- SkiaSharpでの回転処理はメモリ集約的
- System.Drawingと比較して約4倍の処理時間
- 適切なリソース管理が必須

#### 最適化されたSkiaSharp回転実装
```csharp
public static SKBitmap RotateImageOptimized(SKBitmap source, int degrees)
{
    if (degrees == 0 || source == null) return source;
    
    // 正規化（0, 90, 180, 270のみ対応）
    var normalizedDegrees = ((degrees % 360) + 360) % 360;
    if (normalizedDegrees == 0) return source;
    
    // 新しいサイズを計算
    bool swapDimensions = normalizedDegrees == 90 || normalizedDegrees == 270;
    int newWidth = swapDimensions ? source.Height : source.Width;
    int newHeight = swapDimensions ? source.Width : source.Height;
    
    // usingでリソース管理
    using var rotatedBitmap = new SKBitmap(newWidth, newHeight, source.ColorType, source.AlphaType);
    using var canvas = new SKCanvas(rotatedBitmap);
    
    // キャンバスの状態管理
    canvas.Save();
    canvas.Clear(SKColors.Transparent);
    
    // 中心を軸とした回転
    float centerX = newWidth / 2f;
    float centerY = newHeight / 2f;
    
    canvas.Translate(centerX, centerY);
    canvas.RotateDegrees(normalizedDegrees);
    canvas.Translate(-source.Width / 2f, -source.Height / 2f);
    
    canvas.DrawBitmap(source, 0, 0);
    canvas.Restore();
    
    // 新しいBitmapを作成して返す（元のcanvasはusing内で破棄）
    return rotatedBitmap.Copy();
}
```

## 🎯 統一実装方針の決定

### Phase 1: ImageSharp 1.0.4 → 3.x アップグレード検討
- **現在**: SixLabors.ImageSharp 1.0.4
- **最新**: SixLabors.ImageSharp 3.1.11
- **メリット**: AutoOrient()の改善、EXIF処理の精度向上
- **課題**: Breaking Changes対応

### Phase 2: 統一EXIF Orientation処理サービス
```csharp
public interface IImageOrientationService
{
    Task<SKBitmap> CorrectImageOrientationAsync(string imagePath);
    ExifOrientation GetImageOrientation(string imagePath);
    SKBitmap ApplyOrientationCorrection(SKBitmap image, ExifOrientation orientation);
    SKBitmap RotateImage(SKBitmap image, int degrees);
}
```

### Phase 3: 全画像形式統一パイプライン
```csharp
public async Task<SKBitmap> ProcessImageWithCorrectOrientationAsync(string imagePath)
{
    // 1. 画像読み込み（EXIF情報保持）
    using var image = await LoadImageWithExifAsync(imagePath);
    
    // 2. EXIF Orientationによる自動補正
    var orientation = GetImageOrientation(imagePath);
    var correctedImage = ApplyOrientationCorrection(image, orientation);
    
    // 3. 必要に応じて追加の手動回転
    if (manualRotationDegrees != 0)
    {
        correctedImage = RotateImage(correctedImage, manualRotationDegrees);
    }
    
    return correctedImage;
}
```

## 🔧 実装優先度

### 高優先度
1. **統一回転処理メソッドの改善**: 現在のRotateImage()をSkiaSharpベストプラクティスで最適化
2. **EXIF Orientation対応表の実装**: 正確な8値マッピング
3. **メモリ管理の強化**: usingとリソース解放の徹底

### 中優先度
1. **ImageSharpアップグレード**: 3.x系への段階的移行
2. **統一OrientationServiceの実装**: インターフェース設計と実装

### 低優先度
1. **パフォーマンス最適化**: SkiaSharp代替の検討

## 🚨 重要な発見と対策

### 1. EXIF Orientation判定の修正
現在のコードにある不正確なマッピングを修正：

```csharp
// ❌ 現在の問題のあるコード
var rotationDegrees = orientation switch
{
    ExifOrientation.Rotate90 => 90,
    ExifOrientation.Rotate180 => 180, 
    ExifOrientation.Rotate270 => 270,
    _ => 0
};

// ✅ 正確な8値マッピング
var rotationDegrees = orientation switch
{
    ExifOrientation.TopLeft => 0,      // 1
    ExifOrientation.TopRight => 0,     // 2 (Flip needed)
    ExifOrientation.BottomRight => 180, // 3
    ExifOrientation.BottomLeft => 0,   // 4 (Flip needed)
    ExifOrientation.LeftTop => 90,     // 5 (Rotate + Flip)
    ExifOrientation.RightTop => 90,    // 6
    ExifOrientation.RightBottom => 270, // 7 (Rotate + Flip)
    ExifOrientation.LeftBottom => 270, // 8
    _ => 0
};
```

### 2. AutoOrient重複処理の解決
- LoadImageSafelyAsync()で1回のみAutoOrient実行
- GetImageThumbnailAsync()では重複実行を避ける
- EXIF Orientationリセットの実装

### 3. SkiaSharp回転処理の最適化
- メモリリークの防止
- Canvas状態管理の改善
- 新サイズ計算の正確性

## 📋 次のステップ

1. **統一回転処理の改善** (即座実装)
2. **EXIF Orientation正確マッピング実装** (即座実装)
3. **AutoOrient重複削除** (即座実装)
4. **ImageSharpアップグレード検討** (段階的実装)

**調査完了日**: 2025-08-12  
**実装開始**: 即座に統一回転処理から開始