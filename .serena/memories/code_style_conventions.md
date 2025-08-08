# コードスタイル・規約

## 基本方針
- Clean Architecture準拠
- SOLID原則の徹底
- 日本語コメント使用
- プロフェッショナル品質のコード

## 命名規則

### クラス・インターフェース
```csharp
// インターフェース: I + PascalCase
public interface IPdfService { }
public interface IImageProcessingService { }

// クラス: PascalCase
public class PdfService { }
public class MainViewModel { }
```

### メソッド・プロパティ
```csharp
// メソッド: PascalCase + Async suffix
public async Task<bool> LoadPdfAsync(string filePath) { }

// プロパティ: PascalCase
public string FilePath { get; set; }

// プライベートフィールド: _camelCase
private readonly IPdfService _pdfService;
```

### 定数・列挙型
```csharp
// 定数: PascalCase
public const int MaxFileSize = 100 * 1024 * 1024;

// 列挙型: PascalCase
public enum RotationAngle
{
    None = 0,
    Clockwise90 = 90,
    Clockwise180 = 180,
    Clockwise270 = 270
}
```

## ファイル構成規則

### 1つのファイル1つのクラス
```csharp
// ✅ Good: PdfService.cs
public class PdfService : IPdfService { }

// ❌ Bad: Services.cs に複数クラス
```

### 名前空間構造
```csharp
namespace DocOrganizer.Core.Models { }        // エンティティ
namespace DocOrganizer.Application.Interfaces { } // アプリケーション層
namespace DocOrganizer.Infrastructure.Services { } // インフラ層
namespace DocOrganizer.UI.ViewModels { }      // UI層
```

## コメント規則

### 日本語コメント使用
```csharp
/// <summary>
/// PDFファイルを非同期で読み込む
/// </summary>
/// <param name="filePath">読み込むPDFファイルのパス</param>
/// <returns>読み込まれたPDF文書</returns>
public async Task<PdfDocument> LoadPdfAsync(string filePath)
{
    // HEIC処理可能性の事前確認
    if (!IsHeicSupported())
    {
        throw new NotSupportedException("HEIC processing unavailable");
    }
    
    return document;
}
```

### デバッグコメント
```csharp
// デバッグ出力は System.Diagnostics.Debug.WriteLine使用
System.Diagnostics.Debug.WriteLine($"[LoadThumbnail] 画像ファイルから生成 (HEIC: {isHeic}): {filePath}");
```

## エラーハンドリング

### 例外処理
```csharp
try
{
    // メイン処理
    var result = await ProcessImageAsync(imagePath);
    return result;
}
catch (ImageProcessingException ex)
{
    _logger.LogError(ex, "画像処理エラー: {ImagePath}", imagePath);
    throw;
}
catch (Exception ex)
{
    _logger.LogError(ex, "予期しないエラー: {ImagePath}", imagePath);
    throw new ImageProcessingException($"画像処理に失敗しました: {imagePath}", ex);
}
```

### ログ出力
```csharp
// 構造化ログ使用
_logger.LogInformation("HEIC file detected, converting for preview: {FileName}", Path.GetFileName(imagePath));
_logger.LogWarning("Failed to convert HEIC, skipping: {ImagePath}", imagePath);
_logger.LogError(ex, "Failed to convert HEIC to JPEG: {HeicPath}", heicPath);
```

## 非同期処理

### async/await パターン
```csharp
// ✅ Good: ConfigureAwait(false) 使用
public async Task<byte[]> ProcessImageAsync(string imagePath)
{
    var data = await File.ReadAllBytesAsync(imagePath).ConfigureAwait(false);
    return data;
}

// Task.Run使用時の注意
_ = Task.Run(async () => await LoadThumbnailAsync().ConfigureAwait(false));
```

## MVVM パターン

### ViewModelの実装
```csharp
public class MainViewModel : INotifyPropertyChanged
{
    private string _title;
    public string Title 
    { 
        get => _title; 
        set 
        { 
            _title = value; 
            OnPropertyChanged(); 
        } 
    }
    
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
```

## 依存性注入

### インターフェース定義
```csharp
// Application層でインターフェース定義
public interface IPdfService
{
    Task<PdfDocument> LoadAsync(string filePath);
    Task SaveAsync(PdfDocument document, string outputPath);
}

// Infrastructure層で実装
public class PdfService : IPdfService
{
    public async Task<PdfDocument> LoadAsync(string filePath) { /* 実装 */ }
}
```

## テスト規約

### テストクラス命名
```csharp
// テストクラス: [対象クラス]Tests
public class PdfServiceTests
{
    // テストメソッド: [メソッド名]_[状況]_[期待結果]
    [Fact]
    public async Task LoadAsync_ValidPdfFile_ReturnsDocument()
    {
        // Arrange
        var service = new PdfService();
        var filePath = "test.pdf";
        
        // Act
        var result = await service.LoadAsync(filePath);
        
        // Assert
        Assert.NotNull(result);
    }
}
```