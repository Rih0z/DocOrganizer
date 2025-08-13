# JPEG画像90度回転問題 - 根本解決ドキュメント

## 📋 概要

DocOrganizerにおいて、JPEG画像がドラッグ&ドロップ時に90度左に回転して表示される問題が発生していました。この問題を根本的に解決し、Windows Paintと同等の「ピクセルそのまま表示」を実現しました。

**解決日**: 2025-08-12  
**解決方法**: EXIF情報完全削除アプローチ  
**影響範囲**: JPEG, HEIC, PNG, その他画像ファイル全般

---

## 🔍 問題の詳細

### 症状
- JPEG画像をドラッグ&ドロップすると90度左に回転して表示
- 特に縦向きで撮影された写真で顕著
- 左側サムネイルと右側プレビュー両方で発生
- PNG画像では問題なし

### 根本原因
**Windows Imaging Component (WIC) の自動EXIF Orientation処理**

1. **WPF BitmapImageの内部処理**
   - WPF BitmapImageは内部でWICを使用
   - WICが自動的にEXIF Orientationタグを読み取り
   - 意図しない回転を適用

2. **EXIF Orientation情報の干渉**
   - JPEG画像に含まれるEXIF Orientationタグ
   - 複数のライブラリ（SkiaSharp、ImageSharp、WPF）での重複処理
   - CreateOptionsでは完全に無効化できない

---

## 🛠 解決策

### アプローチ: EXIF完全削除

WPF BitmapImageに渡す前にEXIF情報を完全に削除し、ピクセルデータのみを使用する根本的解決策を採用。

### 実装内容

#### 1. 新メソッド追加 (ImageProcessingService.cs)

```csharp
/// <summary>
/// 画像をEXIF情報完全削除して読み込み（90度回転問題完全解決）
/// </summary>
public Task<SkiaSharp.SKBitmap?> LoadImageWithoutExifAsync(string imagePath)
{
    // SkiaSharp SKCodecでEXIF Orientationを無視してピクセル取得
    using var codec = SkiaSharp.SKCodec.Create(imagePath);
    var info = new SkiaSharp.SKImageInfo(codec.Info.Width, codec.Info.Height, SkiaSharp.SKColorType.Rgba8888);
    var bitmap = new SkiaSharp.SKBitmap(info);
    
    // EXIF情報を無視してデコード
    var result = codec.GetPixels(bitmap.Info, bitmap.GetPixels());
    return Task.FromResult<SkiaSharp.SKBitmap?>(bitmap);
}

/// <summary>
/// WPF用にEXIF完全削除済み画像を生成（PNG再エンコード）
/// </summary>
public async Task<byte[]?> GenerateExifFreeImageForWpfAsync(string imagePath, int targetWidth = 600, int targetHeight = 800)
{
    // EXIF情報を完全削除して読み込み
    using var originalBitmap = await LoadImageWithoutExifAsync(imagePath);
    
    // 適切なサイズにリサイズ後、PNG形式でエンコード（EXIF情報なし）
    using var image = SkiaSharp.SKImage.FromBitmap(resizedBitmap);
    using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
    
    return data.ToArray();
}
```

#### 2. PageViewModel修正 (左側サムネイル用)

```csharp
private async Task ProcessStandardImageAsync(string imagePath, CancellationToken cancellationToken)
{
    // ⭐最終修正: EXIF情報を完全削除してWPF用PNG生成
    var exifFreeImageBytes = await _imageProcessingService.GenerateExifFreeImageForWpfAsync(imagePath, 150, 200);
    
    // ⭐最終修正: EXIF完全削除済みPNGから直接WPF BitmapImage作成
    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
    bitmap.BeginInit();
    bitmap.StreamSource = new System.IO.MemoryStream(exifFreeImageBytes);
    bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
    // ⭐CreateOptions不要: すでにEXIF情報が削除済みPNG
    bitmap.EndInit();
    bitmap.Freeze();
    
    ThumbnailImage = bitmap;
}
```

#### 3. MainViewModel修正 (右側プレビュー用)

```csharp
// ⭐最終修正: 右側プレビューもEXIF情報を完全削除
var exifFreeImageBytes = await _imageProcessingService.GenerateExifFreeImageForWpfAsync(page.SourceImagePath, 1200, 1600);

// ⭐最終修正: EXIF完全削除済みPNGから直接WPF BitmapImage作成
var bitmap = new System.Windows.Media.Imaging.BitmapImage();
bitmap.BeginInit();
bitmap.StreamSource = new System.IO.MemoryStream(exifFreeImageBytes);
bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
// ⭐CreateOptions不要: すでにEXIF情報が削除済みPNG
bitmap.EndInit();
```

---

## 🎯 技術的ポイント

### 1. SkiaSharp SKCodec使用
- `SKCodec.GetPixels()`でEXIF Orientationを無視
- 元画像のピクセルデータをそのまま取得
- 外部依存ライブラリの影響を排除

### 2. PNG再エンコード
- PNG形式にはEXIF情報が含まれない
- WPF BitmapImage側でのEXIF処理を完全回避
- 品質劣化なしでメタデータ削除

### 3. CreateOptions不要
- 従来のCreateOptions設定では不完全
- EXIF情報自体が存在しないため確実
- WPF側での追加設定不要

### 4. 統一アプローチ
- 左側サムネイル（150x200）と右側プレビュー（1200x1600）で同じ手法
- 全画像形式（JPEG, HEIC, PNG等）で統一
- 一貫性のある表示結果

---

## 📊 効果と結果

### ✅ 解決された問題
- [x] JPEG画像の90度回転問題
- [x] HEIC画像の回転問題  
- [x] 左右プレビューの一貫性
- [x] Windows Paintと同等の表示

### 🔧 追加されたメソッド
- `LoadImageWithoutExifAsync` - EXIF無視読み込み
- `GenerateExifFreeImageForWpfAsync` - WPF用EXIF削除画像生成

### 📈 パフォーマンス
- 元画像→PNG変換のオーバーヘッドは最小限
- メモリ使用量は適切なサイズ調整で最適化
- 表示速度は従来と同等

---

## 🧪 テスト結果

### テスト環境
- **OS**: Windows 11
- **フレームワーク**: .NET 6.0 WPF
- **画像ライブラリ**: SkiaSharp 2.88.6

### テストケース
1. **JPEG縦向き画像**: ✅ 正常表示（回転なし）
2. **JPEG横向き画像**: ✅ 正常表示  
3. **HEIC画像**: ✅ 正常表示
4. **PNG画像**: ✅ 従来通り正常
5. **左右プレビュー一致**: ✅ 同じ向きで表示

### 検証方法
```
C:\Users\217216X721451\github\DocOrganizer\release\DocOrganizer.exe
```
- エクスプローラーから直接起動（管理者権限不使用）
- sample/JPGフォルダの画像をドラッグ&ドロップ
- 左側サムネイルと右側プレビューの向きが一致することを確認

---

## 🔄 従来手法との比較

| 手法 | CreateOptions | EXIF削除アプローチ |
|------|---------------|-------------------|
| **実装複雑度** | 簡単 | 中程度 |
| **効果** | 不完全 | 完全 |
| **WIC依存** | あり | なし |
| **互換性** | 画像による | 全画像で統一 |
| **保守性** | 低い | 高い |

---

## 📚 参考資料

### 調査したOSSプロジェクト
- **MoonPdfPanel**: WPF PDF Viewer
- **QuestPDF**: .NET PDF生成ライブラリ
- **Cyotek Image Viewer**: C# EXIF処理実装

### 技術文献
- [Microsoft Learn - WPF Imaging Overview](https://learn.microsoft.com/ja-jp/dotnet/desktop/wpf/graphics-multimedia/imaging-overview)
- [SkiaSharp Documentation - SKCodec](https://docs.microsoft.com/en-us/xamarin/xamarin-forms/user-interface/graphics/skiasharp/)
- [Windows Imaging Component (WIC) API](https://docs.microsoft.com/en-us/windows/win32/wic/-wic-lh)

---

## 🚀 今後の改善案

### 1. パフォーマンス最適化
- [ ] PNG変換結果のキャッシュ機能
- [ ] 非同期処理の最適化
- [ ] メモリ使用量の更なる削減

### 2. 機能拡張
- [ ] ユーザー設定でEXIF尊重モード選択
- [ ] 手動回転機能との統合
- [ ] プレビューサイズの動的調整

### 3. 品質向上
- [ ] 単体テストの充実
- [ ] パフォーマンステストの自動化
- [ ] エラーハンドリングの強化

---

## 👥 開発履歴

| 日付 | 担当者 | 内容 |
|------|--------|------|
| 2025-08-12 | Claude Code | 根本原因特定とEXIF削除アプローチ実装 |
| 2025-08-12 | Claude Code | 左右プレビュー統一と最終テスト |
| 2025-08-12 | Claude Code | ドキュメント作成とGitHub push |

**Commit**: `4db4f8e - [根本修正完了] JPEG画像90度回転問題 - EXIF完全削除アプローチで解決`

---

*このドキュメントは DocOrganizer v2.2 の90度回転問題解決に関する技術資料です。*