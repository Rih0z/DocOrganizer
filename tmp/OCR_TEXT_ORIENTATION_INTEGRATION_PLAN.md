# OCRベース文字向き検出統合計画

## 📋 プロジェクト概要

**目的**: DocOrganizerに「人間が文字を読める向き」を自動検出・補正する機能を統合  
**手法**: OCRライブラリを使用した文字認識精度ベースの向き判定  
**期待効果**: 文書整理時の手動回転作業を大幅削減

## 🎯 実装目標

### Primary Goal
- **文字が読める向きの自動検出**: 0°, 90°, 180°, 270°の中から最適な向きを自動判定
- **自動回転補正**: 検出された向きに画像を自動回転
- **ユーザー確認機能**: 自動補正結果をユーザーが確認・修正可能

### Secondary Goal
- **文書種別の自動識別**: 領収書、請求書、契約書等の文書タイプ判定
- **多言語対応**: 日本語、英語等の文字を含む文書の向き検出

## 🔬 ライブラリ選定結果

### 🏆 推奨: IronOCR
```csharp
// NuGet Package
<PackageReference Include="IronOcr" Version="2025.7.19" />
```

**選定理由**:
- ✅ **99.8%の高精度**文字認識
- ✅ **自動向き補正機能**内蔵
- ✅ **低解像度画像対応**
- ✅ **簡単なNuGet統合**
- ✅ **125言語対応**（日本語完全対応）

### 🥈 代替案: Tesseract.NET + OSD
```csharp
// NuGet Packages
<PackageReference Include="Tesseract" Version="5.2.0" />
```

**特徴**:
- ✅ **無料・オープンソース**
- ✅ **OSD (Orientation Script Detection)**
- ❌ セットアップ複雑（osd.traineddata必要）

## 🏗️ アーキテクチャ設計

### 📋 Serena MCP使用による実装方針

**重要**: この統合計画はSerena MCPツールを使用してアーキテクチャを意識した実装を行います。

#### Serena MCP実装手順
1. **セマンティック分析**: `mcp__serena__find_symbol`, `mcp__serena__get_symbols_overview`で既存アーキテクチャ理解
2. **インターフェース設計**: `mcp__serena__find_referencing_symbols`で既存パターンに準拠
3. **実装**: `mcp__serena__replace_symbol_body`, `mcp__serena__insert_after_symbol`で正確な実装
4. **統合**: 既存のClean Architecture（Application/Infrastructure/UI）層に適切に配置

#### アーキテクチャ分析要件
```bash
# 実装前の必須分析
1. mcp__serena__get_symbols_overview("src/DocOrganizer.Application/Interfaces")
2. mcp__serena__find_symbol("IImageProcessingService", "src")
3. mcp__serena__find_referencing_symbols("IImageProcessingService")
4. 既存パターンの理解後に新規インターフェース設計
```

### 新規インターフェース

#### Serena MCP実装ステップ
```bash
# Step 1: 既存インターフェースパターンの分析
mcp__serena__get_symbols_overview("src/DocOrganizer.Application/Interfaces")

# Step 2: IImageProcessingServiceを参考にした設計
mcp__serena__find_symbol("IImageProcessingService", "src/DocOrganizer.Application/Interfaces")

# Step 3: 既存の依存注入パターン確認
mcp__serena__search_for_pattern("AddScoped.*Service", "src")
```
```csharp
/// <summary>
/// 文字向き検出・補正サービス
/// </summary>
public interface ITextOrientationService
{
    /// <summary>
    /// 文字が最も読みやすい向きを検出（0°, 90°, 180°, 270°）
    /// </summary>
    Task<int> DetectOptimalOrientationAsync(string imagePath);
    
    /// <summary>
    /// 文字認識信頼度を取得
    /// </summary>
    Task<double> GetTextConfidenceAsync(string imagePath, int rotationDegrees);
    
    /// <summary>
    /// 文字が読める向きに自動補正
    /// </summary>
    Task<SkiaSharp.SKBitmap> CorrectToOptimalOrientationAsync(SkiaSharp.SKBitmap image);
    
    /// <summary>
    /// 文書内に読み取り可能な文字が存在するかチェック
    /// </summary>
    Task<bool> HasReadableTextAsync(string imagePath);
}
```

### 実装クラス

#### Serena MCP実装ガイド
```bash
# Step 1: 既存サービス実装パターンの確認
mcp__serena__get_symbols_overview("src/DocOrganizer.Infrastructure/Services")

# Step 2: ImageProcessingServiceの実装パターン分析
mcp__serena__find_symbol("ImageProcessingService", "src/DocOrganizer.Infrastructure/Services")

# Step 3: コンストラクタと依存注入パターンの確認
mcp__serena__find_symbol("ImageProcessingService/ImageProcessingService", "src")
```
```csharp
/// <summary>
/// IronOCRベースの文字向き検出サービス
/// </summary>
public class IronOcrTextOrientationService : ITextOrientationService
{
    private readonly IronTesseract _ocr;
    private readonly ILogger<IronOcrTextOrientationService> _logger;
    
    public IronOcrTextOrientationService(ILogger<IronOcrTextOrientationService> logger)
    {
        _logger = logger;
        _ocr = new IronTesseract();
        _ocr.Configuration.ReadBarCodes = false;
        _ocr.Configuration.PageSegmentationMode = TesseractPageSegmentationMode.Auto;
    }
    
    public async Task<int> DetectOptimalOrientationAsync(string imagePath)
    {
        var orientations = new[] { 0, 90, 180, 270 };
        var bestOrientation = 0;
        var bestConfidence = 0.0;
        
        foreach (var angle in orientations)
        {
            try
            {
                var confidence = await GetTextConfidenceAsync(imagePath, angle);
                _logger.LogDebug($"Orientation {angle}°: Confidence {confidence:F2}%");
                
                if (confidence > bestConfidence)
                {
                    bestConfidence = confidence;
                    bestOrientation = angle;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"OCR failed for {angle}° rotation: {ex.Message}");
            }
        }
        
        _logger.LogInformation($"Best orientation detected: {bestOrientation}° (confidence: {bestConfidence:F2}%)");
        return bestOrientation;
    }
    
    public async Task<double> GetTextConfidenceAsync(string imagePath, int rotationDegrees)
    {
        using var input = new OcrInput();
        input.LoadImage(imagePath);
        
        if (rotationDegrees != 0)
        {
            input.Rotate(rotationDegrees);
        }
        
        var result = await _ocr.ReadAsync(input);
        return result.Confidence;
    }
    
    public async Task<SkiaSharp.SKBitmap> CorrectToOptimalOrientationAsync(SkiaSharp.SKBitmap image)
    {
        // 一時ファイルに保存してOCR処理
        var tempPath = Path.GetTempFileName() + ".png";
        try
        {
            using (var fileStream = File.OpenWrite(tempPath))
            using (var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100))
            {
                data.SaveTo(fileStream);
            }
            
            var optimalRotation = await DetectOptimalOrientationAsync(tempPath);
            
            if (optimalRotation == 0)
                return image;
            
            // ImageProcessingServiceの統一回転処理を使用
            return _imageProcessingService.RotateImage(image, optimalRotation);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }
    
    public async Task<bool> HasReadableTextAsync(string imagePath)
    {
        try
        {
            using var input = new OcrInput();
            input.LoadImage(imagePath);
            var result = await _ocr.ReadAsync(input);
            
            // 文字認識結果があり、信頼度が一定以上の場合は読み取り可能と判定
            return !string.IsNullOrWhiteSpace(result.Text) && result.Confidence > 30.0;
        }
        catch
        {
            return false;
        }
    }
}
```

## 🔧 統合手順

### Phase 1: 基盤実装 (Week 1-2)

#### Step 1: NuGetパッケージ追加
```xml
<!-- DocOrganizer.Infrastructure.csproj -->
<PackageReference Include="IronOcr" Version="2025.7.19" />
```

#### Step 2: インターフェース・実装追加

**Serena MCP実装手順**:
```bash
# 1. インターフェースファイル作成
mcp__serena__create_text_file("src/DocOrganizer.Application/Interfaces/ITextOrientationService.cs", <interface_content>)

# 2. 実装クラス作成
mcp__serena__create_text_file("src/DocOrganizer.Infrastructure/Services/IronOcrTextOrientationService.cs", <implementation_content>)

# 3. 既存パターンに準拠した実装確認
mcp__serena__find_referencing_symbols("IImageProcessingService", "src")
```
- `src/DocOrganizer.Application/Interfaces/ITextOrientationService.cs`
- `src/DocOrganizer.Infrastructure/Services/IronOcrTextOrientationService.cs`

#### Step 3: 依存注入設定

**Serena MCP実装手順**:
```bash
# 1. 既存の依存注入設定を確認
mcp__serena__search_for_pattern("AddScoped.*ImageProcessingService", "src")

# 2. Program.csまたはStartup.csの場所特定
mcp__serena__find_file("Program.cs", "src")

# 3. 依存注入設定の追加
mcp__serena__replace_regex(<existing_di_pattern>, <new_di_with_text_orientation>)
```
```csharp
// Program.cs または Startup.cs
services.AddScoped<ITextOrientationService, IronOcrTextOrientationService>();
```

### Phase 2: UI統合 (Week 3)

#### Step 1: PageViewModelに統合

**Serena MCP実装手順**:
```bash
# 1. PageViewModelの現在の構造を分析
mcp__serena__get_symbols_overview("src/DocOrganizer.UI/ViewModels/PageViewModel.cs")

# 2. 既存のコマンドパターンを確認
mcp__serena__find_symbol("RegenerateThumbnailAfterRotationAsync", "src/DocOrganizer.UI/ViewModels")

# 3. 新しいコマンドを既存パターンに準拠して追加
mcp__serena__insert_after_symbol("RegenerateThumbnailAfterRotationAsync", "src/DocOrganizer.UI/ViewModels/PageViewModel.cs", <new_command_implementation>)

# 4. 依存注入フィールドを追加
mcp__serena__find_symbol("PageViewModel/_imageProcessingService", "src") # 既存パターン確認
mcp__serena__replace_symbol_body("PageViewModel", "src/DocOrganizer.UI/ViewModels/PageViewModel.cs", <updated_constructor>)
```
```csharp
public class PageViewModel : ObservableObject
{
    private readonly ITextOrientationService _textOrientationService;
    
    [RelayCommand]
    public async Task AutoCorrectOrientationAsync()
    {
        try
        {
            IsProcessing = true;
            StatusMessage = "文字向きを自動検出中...";
            
            var optimalRotation = await _textOrientationService.DetectOptimalOrientationAsync(_page.SourceImagePath);
            
            if (optimalRotation != _page.Rotation)
            {
                _page.Rotation = optimalRotation;
                await RegenerateThumbnailAfterRotationAsync();
                StatusMessage = $"文字が読める向き（{optimalRotation}°）に自動補正しました";
            }
            else
            {
                StatusMessage = "既に最適な向きです";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"自動補正エラー: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }
}
```

#### Step 2: MainViewModelに一括処理機能

**Serena MCP実装手順**:
```bash
# 1. MainViewModelの既存コマンド構造分析
mcp__serena__get_symbols_overview("src/DocOrganizer.UI/ViewModels/MainViewModel.cs")

# 2. 既存の一括処理コマンドパターン確認
mcp__serena__find_symbol("RotateSelectedPages", "src/DocOrganizer.UI/ViewModels")

# 3. 類似コマンドの実装パターンを参考に新規実装
mcp__serena__insert_after_symbol("RotateSelectedPages", "src/DocOrganizer.UI/ViewModels/MainViewModel.cs", <auto_correct_command>)

# 4. 既存のPages反復処理パターンを確認
mcp__serena__search_for_pattern("foreach.*Pages", "src/DocOrganizer.UI/ViewModels/MainViewModel.cs")
```
```csharp
[RelayCommand]
public async Task AutoCorrectAllPagesOrientationAsync()
{
    var pagesWithText = new List<PageViewModel>();
    
    foreach (var page in Pages)
    {
        if (await _textOrientationService.HasReadableTextAsync(page.Page.SourceImagePath))
        {
            pagesWithText.Add(page);
        }
    }
    
    if (pagesWithText.Any())
    {
        StatusMessage = $"{pagesWithText.Count}ページの文字向きを自動補正中...";
        
        foreach (var page in pagesWithText)
        {
            await page.AutoCorrectOrientationAsync();
        }
        
        StatusMessage = "全ページの自動補正が完了しました";
    }
    else
    {
        StatusMessage = "文字を含むページが見つかりませんでした";
    }
}
```

### Phase 3: UI要素追加 (Week 4)

#### Step 1: ツールバーボタン追加

**Serena MCP実装手順**:
```bash
# 1. MainWindow.xamlの現在のツールバー構造確認
mcp__serena__search_for_pattern("ToolBar.*Button", "src/DocOrganizer.UI/Views/MainWindow.xaml")

# 2. 既存ボタンのコマンドバインディングパターン確認
mcp__serena__search_for_pattern("Command=.*RotateCommand", "src/DocOrganizer.UI/Views")

# 3. 新しいボタンを既存パターンに準拠して追加
mcp__serena__replace_regex(<existing_toolbar_pattern>, <new_toolbar_with_auto_correct_button>)
```
```xml
<!-- MainWindow.xaml -->
<Button Content="📖 文字向き自動補正" 
        Command="{Binding AutoCorrectAllPagesOrientationCommand}"
        ToolTip="全ページの文字が読める向きに自動補正" />
```

#### Step 2: 右クリックコンテキストメニュー
```xml
<!-- ページサムネイル右クリックメニュー -->
<MenuItem Header="文字向き自動補正" 
          Command="{Binding AutoCorrectOrientationCommand}" />
```

### Phase 4: 設定・オプション (Week 5)

#### Step 1: 自動補正設定
```csharp
public class TextOrientationSettings
{
    public bool AutoCorrectOnLoad { get; set; } = false;
    public double MinimumConfidence { get; set; } = 50.0;
    public bool ShowConfidenceScores { get; set; } = true;
    public string[] SupportedLanguages { get; set; } = { "jpn", "eng" };
}
```

#### Step 2: ドラッグ&ドロップ時の自動実行
```csharp
// MainWindow.xaml.cs
private async void Window_Drop(object sender, DragEventArgs e)
{
    // ... 既存のファイル処理 ...
    
    if (AppSettings.AutoCorrectTextOrientation)
    {
        await ViewModel.AutoCorrectAllPagesOrientationAsync();
    }
}
```

## 🧪 テスト計画

### Unit Tests

**Serena MCP実装手順**:
```bash
# 1. 既存テストプロジェクト構造の確認
mcp__serena__get_symbols_overview("tests")

# 2. ImageProcessingServiceのテストパターン分析
mcp__serena__find_file("*ImageProcessingService*Tests.cs", "tests")

# 3. 既存テストの実装パターンを確認
mcp__serena__find_symbol("ImageProcessingServiceTests", "tests")

# 4. 新規テストクラスを既存パターンに準拠して作成
mcp__serena__create_text_file("tests/DocOrganizer.Application.Tests/Services/TextOrientationServiceTests.cs", <test_implementation>)
```
```csharp
[Test]
public async Task DetectOptimalOrientation_ShouldReturn90_ForRotatedText()
{
    // Arrange
    var service = new IronOcrTextOrientationService(_logger);
    var rotatedImagePath = "test_rotated_90.png";
    
    // Act
    var result = await service.DetectOptimalOrientationAsync(rotatedImagePath);
    
    // Assert
    Assert.AreEqual(90, result);
}
```

### Integration Tests
- 各種文書タイプでのテスト
- 日本語・英語混在文書のテスト
- 低解像度画像でのテスト
- 大量ファイル処理のパフォーマンステスト

## 📊 パフォーマンス考慮事項

### 処理時間
- **1ページあたり**: 2-5秒程度（4回転分のOCR処理）
- **最適化案**: 並列処理、キャッシュ機能、信頼度による早期終了

### メモリ使用量
- **IronOCR**: 約50-100MB（ランタイム）
- **一時ファイル**: OCR処理用の画像ファイル

### 最適化戦略
```csharp
// 並列処理による高速化
public async Task<int> DetectOptimalOrientationParallelAsync(string imagePath)
{
    var orientations = new[] { 0, 90, 180, 270 };
    var tasks = orientations.Select(async angle => new 
    {
        Angle = angle,
        Confidence = await GetTextConfidenceAsync(imagePath, angle)
    });
    
    var results = await Task.WhenAll(tasks);
    return results.OrderByDescending(r => r.Confidence).First().Angle;
}
```

## 💰 ライセンス・コスト

### IronOCR
- **開発**: 無料試用版（透かし入り）
- **商用**: $749/developer（年間ライセンス）
- **配布**: ロイヤリティフリー

### Tesseract.NET
- **完全無料**: Apache 2.0ライセンス
- **制限なし**: 商用利用可能

## 🚀 段階的展開計画

### MVP (Minimum Viable Product)
1. **基本的な4方向検出**（0°, 90°, 180°, 270°）
2. **手動実行機能**（ボタンクリック）
3. **処理状況表示**

### Enhanced Version
1. **自動実行オプション**（ファイル読み込み時）
2. **文書タイプ判定**
3. **信頼度スコア表示**
4. **詳細設定画面**

### Advanced Features
1. **微細角度補正**（1度単位の回転）
2. **文書レイアウト解析**
3. **多言語自動検出**
4. **OCR結果のプレビュー表示**

## 📋 実装チェックリスト

### Phase 1: 基盤実装

**Serena MCP実行チェックリスト**:
- [ ] `mcp__serena__get_symbols_overview("src/DocOrganizer.Application/Interfaces")` - 既存インターフェースパターン分析
- [ ] `mcp__serena__find_symbol("IImageProcessingService")` - 参考インターフェース確認
- [ ] `mcp__serena__create_text_file()` - ITextOrientationServiceインターフェース作成
- [ ] `mcp__serena__get_symbols_overview("src/DocOrganizer.Infrastructure/Services")` - サービス実装パターン分析
- [ ] `mcp__serena__create_text_file()` - IronOcrTextOrientationService実装作成
- [ ] `mcp__serena__search_for_pattern("AddScoped")` - 依存注入パターン確認
- [ ] `mcp__serena__replace_regex()` - 依存注入設定追加

### Phase 1: 基盤実装（従来版）
- [ ] ITextOrientationServiceインターフェース設計
- [ ] IronOcrTextOrientationService実装
- [ ] NuGetパッケージ統合
- [ ] 依存注入設定
- [ ] 基本的なテストケース作成

### Phase 2: UI統合

**Serena MCP実行チェックリスト**:
- [ ] `mcp__serena__get_symbols_overview("src/DocOrganizer.UI/ViewModels/PageViewModel.cs")` - PageViewModel構造分析
- [ ] `mcp__serena__find_symbol("RegenerateThumbnailAfterRotationAsync")` - 既存コマンドパターン確認
- [ ] `mcp__serena__insert_after_symbol()` - AutoCorrectOrientationCommandメソッド追加
- [ ] `mcp__serena__get_symbols_overview("src/DocOrganizer.UI/ViewModels/MainViewModel.cs")` - MainViewModel構造分析
- [ ] `mcp__serena__find_symbol("RotateSelectedPages")` - 一括処理パターン確認
- [ ] `mcp__serena__insert_after_symbol()` - AutoCorrectAllPagesOrientationCommand追加

### Phase 2: UI統合（従来版）
- [ ] PageViewModelに自動補正コマンド追加
- [ ] MainViewModelに一括処理機能追加
- [ ] 処理状況表示機能
- [ ] エラーハンドリング実装

### Phase 3: UX改善

**Serena MCP実行チェックリスト**:
- [ ] `mcp__serena__search_for_pattern("ToolBar.*Button", "src/DocOrganizer.UI/Views/MainWindow.xaml")` - 既存ツールバー構造確認
- [ ] `mcp__serena__search_for_pattern("Command=.*RotateCommand", "src/DocOrganizer.UI/Views")` - コマンドバインディングパターン確認
- [ ] `mcp__serena__replace_regex()` - 新規ツールバーボタン追加
- [ ] `mcp__serena__search_for_pattern("ContextMenu", "src/DocOrganizer.UI/Views")` - 右クリックメニューパターン確認
- [ ] `mcp__serena__replace_regex()` - 右クリックメニュー項目追加

### Phase 3: UX改善（従来版）
- [ ] ツールバーボタン追加
- [ ] 右クリックメニュー追加
- [ ] 進捗表示の改善
- [ ] ユーザー設定画面

### Phase 4: 最適化

**Serena MCP実行チェックリスト**:
- [ ] `mcp__serena__find_symbol("DetectOptimalOrientationAsync")` - 実装されたメソッドの確認
- [ ] `mcp__serena__replace_symbol_body()` - 並列処理版への置き換え
- [ ] `mcp__serena__search_for_pattern("WeakReference", "src")` - 既存キャッシュパターン確認
- [ ] `mcp__serena__insert_after_symbol()` - キャッシュ機能追加
- [ ] `mcp__serena__execute_shell_command("dotnet build")` - ビルドテスト実行
- [ ] パフォーマンス測定とメモリプロファイリング

### Phase 4: 最適化（従来版）
- [ ] 並列処理実装
- [ ] キャッシュ機能追加
- [ ] パフォーマンス測定
- [ ] メモリ使用量最適化

## 🎯 成功指標

### 機能的指標
- ✅ **検出精度**: 95%以上の文字向き検出精度
- ✅ **処理速度**: 1ページ3秒以内
- ✅ **ユーザー満足度**: 手動回転作業の80%削減

### 技術的指標
- ✅ **システム安定性**: エラー率1%以下
- ✅ **メモリ効率**: 100MBのRAM増加以内
- ✅ **統合性**: 既存機能への影響なし

## 📅 実装スケジュール

| Phase | 期間 | 主要デリバラブル |
|-------|------|------------------|
| Phase 1 | Week 1-2 | 基盤サービス実装完了 |
| Phase 2 | Week 3 | UI統合完了 |
| Phase 3 | Week 4 | UX改善完了 |
| Phase 4 | Week 5 | 最適化・テスト完了 |

**プロジェクト完了予定**: 5週間後  
**最初のMVP**: 2週間後

## 🛠️ Serena MCP実装実行例

### 完全実装フロー（コマンド実行順序）

```bash
# === Phase 1: アーキテクチャ分析 ===
mcp__serena__get_symbols_overview("src/DocOrganizer.Application/Interfaces")
mcp__serena__find_symbol("IImageProcessingService", "src/DocOrganizer.Application/Interfaces")
mcp__serena__get_symbols_overview("src/DocOrganizer.Infrastructure/Services")
mcp__serena__find_symbol("ImageProcessingService", "src/DocOrganizer.Infrastructure/Services")

# === Phase 2: インターフェース・実装作成 ===
mcp__serena__create_text_file("src/DocOrganizer.Application/Interfaces/ITextOrientationService.cs", <interface_code>)
mcp__serena__create_text_file("src/DocOrganizer.Infrastructure/Services/IronOcrTextOrientationService.cs", <implementation_code>)

# === Phase 3: 依存注入統合 ===
mcp__serena__search_for_pattern("AddScoped.*ImageProcessingService", "src")
mcp__serena__find_file("Program.cs", "src")
mcp__serena__replace_regex(<existing_di>, <new_di_with_text_orientation>)

# === Phase 4: UI統合 ===
mcp__serena__get_symbols_overview("src/DocOrganizer.UI/ViewModels/PageViewModel.cs")
mcp__serena__find_symbol("RegenerateThumbnailAfterRotationAsync", "src/DocOrganizer.UI/ViewModels")
mcp__serena__insert_after_symbol("RegenerateThumbnailAfterRotationAsync", "src/DocOrganizer.UI/ViewModels/PageViewModel.cs", <auto_correct_command>)

# === Phase 5: ビルドテスト ===
mcp__serena__execute_shell_command("dotnet build --configuration Release")
mcp__serena__execute_shell_command("dotnet test")
```

### Serena MCP使用のメリット

1. **アーキテクチャ準拠**: 既存のClean Architectureパターンを自動的に分析・準拠
2. **セマンティック理解**: コードの意味を理解した上での実装
3. **エラー防止**: 既存パターンに基づく実装でインテグレーションエラーを防止
4. **効率的開発**: 手動コード検索・分析時間を大幅短縮

---

**最終更新**: 2025-08-12  
**文書作成者**: Claude Code + Serena MCP  
**実装予定チーム**: DocOrganizer Development Team