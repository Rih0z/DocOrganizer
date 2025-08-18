---
# ⚠️ 重要: このドキュメントはV2アーキテクチャ用です

🚨 **V2コード廃止による影響:**
- このドキュメントで言及するPageViewModel、MainViewModelは全て廃止済み
- V3アーキテクチャで左右プレビューは完全再実装済み

✅ **V3での実装状況:**
- V3PageViewModel: LoadLeftThumbnailAsync / LoadRightPreviewAsync
- IThumbnailGeneratorService: 左右分離による最適化
- 90度回転バグ完全修正済み

---

# 左右プレビュー実装 (V2アーキテクチャ - 廃止済み)

**注意: 以下のコードは全て廃止されており、V3で再実装済みです**

## V3での実装 (現在の正式版)

### 1. V3PageViewModel（左右統合管理）
```csharp
/// <summary>
/// 🎯 V3 OSS標準: 左側サムネイル生成
/// </summary>
public async Task LoadLeftThumbnailAsync()
{
    var thumbnailImageSource = await _thumbnailService.GenerateLeftPanelThumbnailAsync(_page.SourceImagePath);
    ThumbnailImage = thumbnailImageSource as BitmapSource;
}

/// <summary>
/// 🎯 V3 OSS標準: 右側プレビュー生成
/// </summary>
public async Task LoadRightPreviewAsync()
{
    var previewImageSource = await _thumbnailService.GenerateRightPreviewImageAsync(_page.SourceImagePath);
    PreviewImage = previewImageSource as BitmapSource;
}
```

### 2. IThumbnailGeneratorService（OSS標準分離）
```csharp
/// <summary>
/// 左側パネル用サムネイル生成（150x200固定）
/// </summary>
Task<ImageSource> GenerateLeftPanelThumbnailAsync(string filePath);

/// <summary>
/// 右側プレビュー用高解像度画像生成
/// </summary>
Task<ImageSource> GenerateRightPreviewImageAsync(string filePath, int maxWidth = 1920, int maxHeight = 1080);
```

### 3. MainCompositeViewModel（統合調整）
```csharp
// 選択ページ変更時のプレビュー更新
partial void OnSelectedPageChanged(V3PageViewModel? value)
{
    if (value != null)
    {
        // V3 OSS標準: 右側プレビュー生成
        _ = value.LoadRightPreviewAsync();
    }
}
```

## V3の優位性

### アーキテクチャ改善
- **責務分離**: 左右プレビューがサービス層で分離
- **テスト可能**: Clean Architecture準拠
- **保守性**: Single Responsibility Principle

### 技術改善  
- **非同期処理**: 90度回転バグ根本修正
- **型安全**: BitmapSource統一による安定性
- **パフォーマンス**: OSS標準ライブラリ最適化

### サイズ仕様
- **左側**: 150x200（サムネイル）
- **右側**: 1920x1080上限（高解像度プレビュー）

## 結論

V2で問題となっていた左右プレビューの実装は、V3 Clean Architectureにより完全に再設計され、安定動作しています。