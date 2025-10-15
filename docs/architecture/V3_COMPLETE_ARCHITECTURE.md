# DocOrganizer V3 完全アーキテクチャ解説

**作成日**: 2025-08-21
**最終更新**: 2025-10-14
**対象バージョン**: V3.0.129
**目的**: V3アーキテクチャの全体像と技術詳細を包括的に解説

## 🏗️ アーキテクチャ概要

DocOrganizer V3は、Clean Architecture + Provider Pattern + MVVM パターンを採用した、エンタープライズレベルの拡張可能アーキテクチャです。

```
┌─────────────────────────────────────────────────────────────┐
│                    V3 Complete Architecture                 │
├─────────────────────────────────────────────────────────────┤
│  UI Layer (WPF MVVM)                                       │
│  ├── MainCompositeViewModel                                │
│  ├── DocumentManagementViewModel                           │
│  ├── PreviewManagementViewModel                            │
│  ├── PageOperationViewModel                                │
│  ├── DragDropHandlerViewModel                              │
│  └── StatusManagementViewModel                             │
├─────────────────────────────────────────────────────────────┤
│  Application Layer (Interfaces)                            │
│  ├── IImageProcessingProvider                              │
│  ├── IImageValidationProvider                              │
│  ├── IThumbnailGeneratorService                            │
│  ├── IFileAdditionService                                  │
│  └── IPdfExportService                                     │
├─────────────────────────────────────────────────────────────┤
│  Infrastructure Layer (Services & Providers)               │
│  ├── Provider Pattern Implementation                       │
│  │   ├── HeicImageProcessingProvider                       │
│  │   ├── GifImageProcessingProvider                        │
│  │   ├── WebPImageProcessingProvider                       │
│  │   ├── StandardImageProcessingProvider                   │
│  │   ├── PdfImageProcessingProvider                        │
│  │   └── PsdImageProcessingProvider                        │
│  ├── Provider Managers                                     │
│  │   ├── ImageProcessingProviderManager                    │
│  │   └── ImageValidationProviderManager                    │
│  └── Core Services                                         │
│      ├── HeicConversionService                             │
│      ├── ExifOrientationService                            │
│      └── ThumbnailGeneratorService                         │
├─────────────────────────────────────────────────────────────┤
│  Core Layer (Domain)                                       │
│  └── Business Logic & Entities                             │
└─────────────────────────────────────────────────────────────┘
```

## 📱 UI Layer - MVVM分離アーキテクチャ

### MainCompositeViewModel
**役割**: 全てのViewModelを統合し、ViewModel間の協調を管理
```csharp
// UI層の統合ハブ
public class MainCompositeViewModel
{
    private readonly DocumentManagementViewModel _documentManager;
    private readonly PreviewManagementViewModel _previewManager;
    private readonly PageOperationViewModel _pageOperationManager;
    private readonly DragDropHandlerViewModel _dragDropHandler;
    private readonly StatusManagementViewModel _statusManager;
}
```

### 専門化されたViewModel構成
| ViewModel | 実装規模 | 責務 |
|-----------|---------|------|
| **DocumentManagementViewModel** | 714行 | ファイル管理（New/Open/Save）・PDF編集（Split/Merge）・Undo/Redo |
| **PreviewManagementViewModel** | 663行 | プレビュー表示・ズーム機能（In/Out/FitToWindow） |
| **PageOperationViewModel** | 1283行 | 回転・削除・移動・並び替え・選択管理・キーボードナビゲーション |
| **DragDropHandlerViewModel** | 957行 | ドラッグ&ドロップ処理・ファイル追加・ページ並び替え・自動スクロール |
| **StatusManagementViewModel** | 201行 | 状態管理・進捗表示・メッセージ表示 |
| **MainCompositeViewModel** | 913行 | ViewModel統合・イベント管理・PDF出力 |

**合計実装規模**: 4731行（V3.0.129時点）

### UI設計哲学（V3.0.128-129）
**シンプリシティ第一**:
- メニューは最小限（PDF編集・ヘルプのみ）
- ツールバーアイコンで直感的操作
- キーボードショートカットで上級者対応
- ViewModelがすべての機能を提供（UI要素に依存しない）

## 🔧 Provider Pattern - 拡張可能な画像処理アーキテクチャ

### IImageProcessingProvider統一インターフェース
```csharp
public interface IImageProcessingProvider
{
    // 画像検証（ドラッグ&ドロップ時）
    Task<ImageValidationResult> ValidateAsync(string filePath);
    
    // サムネイル生成（左パネル・右プレビュー・PDF対応）
    Task<ImageSource> GenerateThumbnailAsync(string filePath, ThumbnailSize size, int rotation = 0);
    
    // プレビュー画像生成（高解像度表示用）
    Task<ImageSource> GeneratePreviewAsync(string filePath, int maxWidth = 1920, int maxHeight = 1080);
    
    // 画像情報取得（サイズ、形式、EXIF等）
    Task<ImageInfo> GetImageInfoAsync(string filePath);
    
    // プロバイダー情報
    bool SupportsFormat(string extension);
    string[] SupportedExtensions { get; }
    int Priority { get; }
    string ProviderName { get; }
}
```

### 実装済みプロバイダー一覧

#### 1. HeicImageProcessingProvider
- **優先度**: 100 (最高)
- **対応形式**: `.heic`, `.heif`
- **特徴**: ImageMagick連携によるHEIC→JPEG変換
- **実装**: V3.0.009で完全対応（アスペクト比保持修正済み）

#### 2. GifImageProcessingProvider
- **優先度**: 90
- **対応形式**: `.gif`
- **特徴**: アニメーションGIF対応（最初フレーム抽出）
- **実装**: フレーム数解析・再生時間計算

#### 3. WebPImageProcessingProvider
- **優先度**: 85
- **対応形式**: `.webp`
- **特徴**: 次世代画像形式対応
- **実装**: ImageSharp使用

#### 4. StandardImageProcessingProvider
- **優先度**: 80
- **対応形式**: `.jpg`, `.jpeg`, `.png`, `.bmp`
- **特徴**: 標準画像形式の最適化処理
- **実装**: ImageSharp + EXIF自動補正

#### 5. PdfImageProcessingProvider
- **対応形式**: `.pdf`
- **特徴**: PDF処理専用プロバイダー
- **実装**: PdfiumViewer使用

#### 6. PsdImageProcessingProvider
- **対応形式**: `.psd`
- **特徴**: Photoshop形式対応
- **実装**: ImageSharp使用

### ImageProcessingProviderManager
**設計パターン**: Strategy Pattern + Factory Pattern
```csharp
public class ImageProcessingProviderManager : IImageProcessingProviderManager
{
    // 拡張子ベースの最適プロバイダー自動選択
    public IImageProcessingProvider GetProvider(string extension)
    
    // 優先度ベースプロバイダー登録
    public void RegisterProvider(IImageProcessingProvider provider)
    
    // 最適プロバイダーでの統一処理
    public async Task<T> ProcessWithBestProvider<T>(string filePath, Func<IImageProcessingProvider, Task<T>> processor)
}
```

## 🎯 Service Layer - 専門化されたサービス群

### HeicConversionService
**目的**: HEIC専用変換サービス
```csharp
public interface IHeicConversionService
{
    Task<string> ConvertHeicToTempJpegAsync(string heicPath);
    Task<HeicInfo> GetHeicInfoAsync(string heicPath);
    void CleanupTempFiles();
}
```

### ThumbnailGeneratorService
**目的**: プロバイダーを活用した統一サムネイル生成
```csharp
public class ThumbnailGeneratorService : IThumbnailGeneratorService
{
    // プロバイダーマネージャーを使用した統一処理
    public async Task<ImageSource> GenerateLeftPanelThumbnailAsync(string filePath, int rotation = 0)
    public async Task<ImageSource> GenerateRightPreviewImageAsync(string filePath, int rotation = 0, int maxWidth = 1920, int maxHeight = 1080)
}
```

### ExifOrientationService
**目的**: EXIF Orientation自動補正
```csharp
public interface IExifOrientationService
{
    Task<BitmapSource> ApplyExifOrientationAsync(BitmapSource source, string filePath);
    Rotation GetExifRotation(string filePath);
}
```

## 🔄 Dependency Injection - 自動プロバイダー発見

### ServiceCollectionExtensions
```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddV3ImageProcessingServices(this IServiceCollection services)
    {
        // 属性ベース自動プロバイダー発見
        var providerTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.GetCustomAttribute<ImageProcessingProviderAttribute>() != null)
            .Where(t => typeof(IImageProcessingProvider).IsAssignableFrom(t));

        foreach (var providerType in providerTypes)
        {
            services.AddScoped(typeof(IImageProcessingProvider), providerType);
        }

        // マネージャー・サービス登録
        services.AddScoped<IImageProcessingProviderManager, ImageProcessingProviderManager>();
        services.AddScoped<IThumbnailGeneratorService, ThumbnailGeneratorService>();
        services.AddScoped<IHeicConversionService, HeicConversionService>();
        
        return services;
    }
}
```

### プロバイダー属性による自動登録
```csharp
[ImageProcessingProvider("HEIC", Priority = 100)]
public class HeicImageProcessingProvider : IImageProcessingProvider
{
    // HEIC専用実装
}

[ImageProcessingProvider("GIF", Priority = 90)]
public class GifImageProcessingProvider : IImageProcessingProvider
{
    // GIF専用実装
}
```

## 🚀 将来拡張シナリオ

### 新形式追加の簡単さ
```csharp
// AVIF形式追加例
[ImageProcessingProvider("AVIF", Priority = 95)]
public class AvifImageProcessingProvider : IImageProcessingProvider
{
    public string[] SupportedExtensions => new[] { ".avif" };
    
    // 4つのメソッド実装のみで全システム対応完了
    public async Task<ImageValidationResult> ValidateAsync(string filePath) { /* 実装 */ }
    public async Task<ImageSource> GenerateThumbnailAsync(string filePath, ThumbnailSize size, int rotation = 0) { /* 実装 */ }
    public async Task<ImageSource> GeneratePreviewAsync(string filePath, int maxWidth = 1920, int maxHeight = 1080) { /* 実装 */ }
    public async Task<ImageInfo> GetImageInfoAsync(string filePath) { /* 実装 */ }
}

// 自動的に全システムで利用可能になる（既存コード変更不要）
```

## 📊 V3アーキテクチャの利点

### ✅ 技術的利点
1. **無限拡張可能性**: 新形式追加が1クラスで完結
2. **テスタビリティ**: プロバイダー別独立テストが容易
3. **保守性**: 形式別の独立実装で影響範囲限定
4. **パフォーマンス**: 形式別最適化処理

### ✅ ビジネス価値
1. **将来対応力**: 新しい画像形式への迅速対応
2. **品質**: 企業レベルの設計パターン採用
3. **開発効率**: Clean Architecture による開発生産性向上

## 🔧 技術スタック

| 層 | 技術 |
|----|------|
| **UI** | WPF + MVVM + Material Design |
| **DI** | Microsoft.Extensions.DependencyInjection |
| **画像処理** | SixLabors.ImageSharp + ImageMagick |
| **PDF処理** | PDFsharp |
| **ログ** | Microsoft.Extensions.Logging |
| **テスト** | xUnit + FluentAssertions + Moq |

## 📋 実装完了状況 (V3.0.129)

### ✅ 完全実装済み（V3.0.129時点）
- [x] Provider Pattern 完全実装
- [x] HEIC完全対応（V3.0.009修正済み）
- [x] GIF/WebP対応
- [x] アスペクト比保持修正
- [x] 統一サムネイル生成
- [x] EXIF自動補正
- [x] プロバイダー自動発見
- [x] Undo/Redo機能（V3.0.068）
- [x] 複数選択機能（V3.0.103）
- [x] ズーム機能（V3.0.110）
- [x] 複数ページ一括移動（V3.0.117）
- [x] ドラッグ自動スクロール（V3.0.125）
- [x] キーボードナビゲーション完全対応（V3.0.127）
- [x] UI簡素化・ミニマルデザイン（V3.0.128-129）

### 🎯 アーキテクチャの成熟度
- **パフォーマンス**: ViewModel再利用で最適化済み（V3.0.073）
- **保守性**: Clean Architectureで高い変更容易性
- **拡張性**: Provider Patternで新形式追加が容易
- **ユーザビリティ**: シンプルなUI + 強力なショートカット

### 🔄 将来の拡張候補
- [ ] 追加画像形式対応（AVIF、JPEG XL）
- [ ] キャッシュ機能強化
- [ ] クラウド連携機能

---

## 🏆 設計原則の実践

### Clean Architecture
- UI層とビジネスロジックの完全分離
- Provider Patternによる拡張可能設計
- Dependency Injectionによる疎結合

### SOLID原則
- **S**ingle Responsibility: 各ViewModelが単一責務
- **O**pen/Closed: Provider追加で機能拡張（既存コード変更不要）
- **L**iskov Substitution: IImageProcessingProvider実装の交換可能性
- **I**nterface Segregation: 目的別インターフェース分離
- **D**ependency Inversion: 抽象への依存（具象への依存なし）

---

## 📊 コードメトリクス（V3.0.129）

### ViewModelクラス規模
| ViewModel | 行数 | 主要メソッド数 | イベント数 | 依存サービス数 |
|-----------|------|---------------|-----------|---------------|
| MainCompositeViewModel | 913 | 20+ | 7 | 3 |
| DocumentManagementViewModel | 714 | 15+ | 3 | 6 |
| PreviewManagementViewModel | 663 | 12+ | 1 | 4 |
| PageOperationViewModel | 1283 | 30+ | 5 | 4 |
| DragDropHandlerViewModel | 957 | 15+ | 6 | 3 |
| StatusManagementViewModel | 201 | 7 | 6 | 2 |
| **合計** | **4731** | **99+** | **28** | **22** |

### Provider実装数
- IImageProcessingProvider実装: **6クラス**
- サポート画像形式: **HEIC, GIF, WebP, JPG, PNG, BMP, PDF, PSD**

### UI実装
- メニュー項目: **2カテゴリ**（PDF編集、ヘルプ）
- ツールバーボタン: **15個**
- キーボードショートカット: **50+**

---

**DocOrganizer V3.0.129は、127バージョンの継続的改善を経て、エンタープライズグレードの成熟したアーキテクチャを実現しています。**