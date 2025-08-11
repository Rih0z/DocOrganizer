# DocOrganizer 回転問題 実装ロードマップ

## 🎯 実装方針

**基本原則**: 段階的実装で確実に問題を解決し、各段階でテスト・検証を実施

## 📋 Phase 1: EXIF Orientation 適切処理実装

### 1.1 ImageSharp EXIF読み取り実装

#### 目標
EXIF Orientationを正確に読み取り、必要な場合のみAutoOrientを適用

#### 実装箇所
`src/DocOrganizer.Infrastructure/Services/ImageProcessingService.cs`

#### 修正内容
```csharp
// LoadImageSafelyAsync()の修正
private async Task<Image> LoadImageSafelyAsync(string imagePath)
{
    var image = await Image.LoadAsync(imagePath);
    
    // EXIF Orientationを取得
    var orientation = GetExifOrientation(image);
    _logger.LogDebug($"EXIF Orientation: {orientation} for {Path.GetFileName(imagePath)}");
    
    // 通常向き(1)以外の場合のみAutoOrientを適用
    if (orientation != 1)
    {
        image.Mutate(x => x.AutoOrient());
        _logger.LogInformation($"AutoOrient applied (orientation {orientation}): {Path.GetFileName(imagePath)}");
    }
    else
    {
        _logger.LogDebug($"AutoOrient skipped (normal orientation): {Path.GetFileName(imagePath)}");
    }
    
    return image;
}

// 新規メソッド: EXIF Orientation取得
private int GetExifOrientation(Image image)
{
    try
    {
        if (image.Metadata?.ExifProfile != null)
        {
            var orientationValue = image.Metadata.ExifProfile.GetValue(ExifTag.Orientation);
            if (orientationValue != null)
            {
                return orientationValue.Value;
            }
        }
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Failed to read EXIF Orientation, assuming normal");
    }
    
    return 1; // デフォルト: Normal
}
```

#### 期待される効果
- **「常に左に90度回転」問題の解決**: Orientation 6の画像のみ補正適用
- **不要な回転の排除**: Normal画像(Orientation 1)は無変更
- **処理の透明性**: ログでEXIF値と処理を確認可能

### 1.2 DetectAndCorrectOrientationAsync()の修正

#### 現在の問題
AutoOrient無効化により、寸法変化による検出が機能しない

#### 修正方針
EXIF Orientationを直接読み取って判定

```csharp
private async Task<int> DetectAndCorrectOrientationAsync(string imagePath)
{
    try
    {
        _logger.LogDebug("Detecting orientation for {ImagePath}", Path.GetFileName(imagePath));
        
        // 画像を読み込み（AutoOrientは適用しない）
        using var image = await Image.LoadAsync(imagePath);
        
        // EXIF Orientationを直接取得
        var orientation = GetExifOrientation(image);
        
        // Orientationに基づく回転角度を計算
        var rotationDegrees = orientation switch
        {
            1 => 0,   // Normal
            3 => 180, // Rotate 180°
            6 => 90,  // Rotate 90° CW → 90度補正が必要
            8 => 270, // Rotate 90° CCW → 270度補正が必要
            _ => 0    // その他は回転なし
        };
        
        _logger.LogInformation("Orientation detection: {ImagePath} EXIF={Orientation} Rotation={Degrees}°", 
            Path.GetFileName(imagePath), orientation, rotationDegrees);
        
        return rotationDegrees;
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Failed to detect orientation for {ImagePath}", imagePath);
        return 0;
    }
}
```

### 1.3 テスト・検証計画

#### 検証用画像準備
1. **Normal画像** (EXIF Orientation = 1): 回転なしで表示確認
2. **90度CW画像** (EXIF Orientation = 6): 適切に補正されることを確認
3. **180度画像** (EXIF Orientation = 3): 適切に補正されることを確認  
4. **HEIC画像**: 既存の処理が正常動作することを確認

#### 検証手順
1. AutoOrient無効化を解除し、新しいロジックを適用
2. 各テスト画像をドラッグ&ドロップ
3. 左側プレビュー、右側プレビュー、PDF出力の向きを確認
4. ログ出力でEXIF値と処理内容を確認

## 📋 Phase 2: WPFバインディング完全修正

### 2.1 RegenerateThumbnailAfterRotation()強化

#### 現在の問題
一意ダミー値メカニズムが部分的にしか機能していない

#### 強化方針
```csharp
public void RegenerateThumbnailAfterRotation()
{
    try
    {
        _logger.LogDebug($"Starting thumbnail regeneration for page {PageNumber} with rotation {_page.Rotation}°");
        
        // 1. 全キャッシュの完全削除
        ClearAllImageCaches();
        
        // 2. WPF Dispatcher上で確実にnull化
        Application.Current.Dispatcher.Invoke(() => {
            ThumbnailImage = null;
            OnPropertyChanged(nameof(ThumbnailImage));
        });
        
        // 3. 非同期で新しいサムネイル生成
        _ = Task.Run(async () => {
            try
            {
                // 回転角度を考慮したサムネイル生成
                await GenerateThumbnailWithRotation(_page.Rotation);
                
                Application.Current.Dispatcher.Invoke(() => {
                    OnPropertyChanged(nameof(ThumbnailImage));
                    _logger.LogDebug($"Thumbnail regeneration completed for page {PageNumber}");
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to regenerate thumbnail for page {PageNumber}");
            }
        });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"Error in RegenerateThumbnailAfterRotation for page {PageNumber}");
        // フォールバック処理
        FallbackThumbnailRegeneration();
    }
}

private void ClearAllImageCaches()
{
    // WeakReference キャッシュクリア
    _optimizedThumbnailCache = null;
    _optimizedPreviewCache = null;
    
    // BitmapImage リソース解放
    if (ThumbnailImage is BitmapImage bitmap)
    {
        try
        {
            bitmap.StreamSource?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to dispose bitmap stream");
        }
    }
    
    // 強制ガベージコレクション
    GC.Collect();
    GC.WaitForPendingFinalizers();
}

private async Task GenerateThumbnailWithRotation(int rotationDegrees)
{
    if (_imageProcessingService == null || string.IsNullOrEmpty(_page.SourceImagePath))
        return;
    
    try
    {
        // 回転角度を明示的に渡してサムネイル生成
        var thumbnailData = await _imageProcessingService.GetImageThumbnailAsync(
            _page.SourceImagePath, 150, 150, rotationDegrees);
        
        if (thumbnailData != null && thumbnailData.Length > 0)
        {
            var bitmap = CreateBitmapFromBytes(thumbnailData);
            
            Application.Current.Dispatcher.Invoke(() => {
                ThumbnailImage = bitmap;
            });
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Failed to generate thumbnail with rotation");
        throw;
    }
}
```

### 2.2 MainViewModel.RotateSelectedPages()の強化

#### CollectionView同期の確実性向上
```csharp
private void RotateSelectedPages(int degrees)
{
    try
    {
        _logger.LogDebug($"Starting rotation of {degrees}° for selected pages");
        
        var selectedPages = Pages.Where(p => p.IsSelected).ToList();
        
        // UI同期実行
        Application.Current.Dispatcher.Invoke(() => {
            
            foreach (var pageVm in selectedPages)
            {
                // Core層データ更新
                pageVm.Page.Rotation = (pageVm.Page.Rotation + degrees) % 360;
                if (pageVm.Page.Rotation < 0) pageVm.Page.Rotation += 360;
                
                // ViewModel同期更新
                pageVm.UpdateRotationSync();
                
                // サムネイル強制再生成
                pageVm.RegenerateThumbnailAfterRotation();
            }
            
            // WPF CollectionView完全リフレッシュ
            ForceCompleteCollectionRefresh();
            
            // 現在選択ページのプレビュー更新
            UpdateCurrentPagePreview();
        });
        
        _logger.LogInformation($"Rotation completed: {selectedPages.Count} pages rotated {degrees}°");
        StatusMessage = $"{selectedPages.Count} ページを回転しました";
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error during page rotation");
        _dialogService.ShowError($"回転エラー: {ex.Message}");
    }
}

private void ForceCompleteCollectionRefresh()
{
    try
    {
        // 1. CollectionViewの強制リフレッシュ
        var collectionView = CollectionViewSource.GetDefaultView(Pages);
        if (collectionView != null)
        {
            collectionView.Refresh();
            _logger.LogDebug("CollectionView refreshed");
        }
        
        // 2. ObservableCollectionの変更通知
        OnPropertyChanged(nameof(Pages));
        
        // 3. 各PageViewModelの個別通知
        foreach (var page in Pages)
        {
            page.OnPropertyChanged(nameof(PageViewModel.ThumbnailImage));
        }
        
        _logger.LogDebug("Complete collection refresh executed");
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Error during collection refresh");
    }
}
```

## 📋 Phase 3: 統合テスト・検証

### 3.1 自動テスト実装

#### ユニットテスト追加
```csharp
[Test]
public async Task LoadImageSafelyAsync_WithExifOrientation6_AppliesAutoOrient()
{
    // Arrange
    var testImagePath = "test-images/orientation-6.jpg"; // 90度CW画像
    
    // Act
    var result = await _imageProcessingService.LoadImageSafelyAsync(testImagePath);
    
    // Assert
    Assert.IsNotNull(result);
    // 90度回転後の寸法チェック（横長 → 縦長）
    Assert.IsTrue(result.Height > result.Width);
}

[Test]
public async Task LoadImageSafelyAsync_WithExifOrientation1_SkipsAutoOrient()
{
    // Arrange  
    var testImagePath = "test-images/orientation-1.jpg"; // Normal画像
    
    // Act
    var result = await _imageProcessingService.LoadImageSafelyAsync(testImagePath);
    
    // Assert
    Assert.IsNotNull(result);
    // 元の寸法のまま（変更なし）
    Assert.IsTrue(result.Width > result.Height); // 横長のまま
}
```

#### UI統合テスト
```csharp
[Test]
public void RotateSelectedPages_UpdatesLeftPreview()
{
    // Arrange
    var mainViewModel = CreateTestMainViewModel();
    var testPage = CreateTestPageViewModel();
    mainViewModel.Pages.Add(testPage);
    testPage.IsSelected = true;
    
    // Act
    mainViewModel.RotateRightCommand.Execute(null);
    
    // Assert
    Thread.Sleep(100); // UI更新を待機
    Assert.IsNotNull(testPage.ThumbnailImage);
    Assert.AreEqual(90, testPage.Page.Rotation);
}
```

### 3.2 マニュアルテスト計画

#### テストケース一覧
1. **EXIF Orientation別テスト**
   - Normal画像 (値1): 回転なしで正常表示
   - 180度画像 (値3): 自動補正で正常表示
   - 90度CW画像 (値6): 自動補正で正常表示
   - 90度CCW画像 (値8): 自動補正で正常表示

2. **手動回転テスト**
   - 各画像で90度右回転: 左側プレビュー即座更新
   - 各画像で90度左回転: 左側プレビュー即座更新
   - 複数ページ一括回転: 全ページ同期更新

3. **HEIC特別テスト**
   - HEIC画像読み込み: 正常表示
   - HEIC画像回転: 左側プレビュー更新

## ⏱️ 実装スケジュール

### Week 1: Phase 1実装
- **Day 1-2**: EXIF Orientation読み取り実装
- **Day 3-4**: AutoOrient条件適用実装  
- **Day 5**: Phase 1テスト・検証

### Week 2: Phase 2実装
- **Day 1-3**: WPFバインディング強化実装
- **Day 4-5**: 統合テスト・デバッグ

### Week 3: 最終調整・リリース
- **Day 1-2**: 統合テスト・性能最適化
- **Day 3**: ドキュメント更新
- **Day 4-5**: 最終EXE生成・配布準備

## 🎯 成功基準

### 技術基準
- [ ] EXIF Orientation値の100%正確な読み取り
- [ ] 必要最小限のAutoOrient適用（重複ゼロ）
- [ ] 左側プレビューの100%確実な更新
- [ ] メモリリーク・性能劣化なし

### ユーザー体験基準  
- [ ] 画像が期待通りの向きで表示される
- [ ] 回転操作の結果が即座に反映される
- [ ] 全ての表示箇所で一致した表示
- [ ] 操作の快適性・応答性

---

**実装開始準備**: ✅ **完了**  
**次のアクション**: Phase 1.1 EXIF Orientation実装開始  
**予定完成日**: 2025年8月22日