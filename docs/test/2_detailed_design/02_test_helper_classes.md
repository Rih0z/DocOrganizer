# テストヘルパークラス詳細設計

## 概要

本ドキュメントは、テストコード実装を効率化するためのヘルパークラスの詳細設計を定義します。

## 1. TestDataGenerator（完全実装仕様）

**目的**: テスト用PDFファイルを動的に生成

**名前空間**: `DocOrganizer.Tests.TestHelpers`

**実装場所**: `tests/TestData/TestDataGenerator.cs`

### 1.1 クラス定義

```csharp
public static class TestDataGenerator
{
    private static readonly string TempDir = Path.Combine(Path.GetTempPath(), "DocOrganizerTests");

    static TestDataGenerator()
    {
        Directory.CreateDirectory(TempDir);
    }

    // PDFSharp を使用したPDF生成
    public static string GeneratePdf(int pageCount, string fileName = null)
    {
        fileName ??= $"test_{pageCount}pages_{Guid.NewGuid():N}.pdf";
        var outputPath = Path.Combine(TempDir, fileName);

        using var document = new PdfDocument();

        for (int i = 0; i < pageCount; i++)
        {
            var page = document.AddPage();
            page.Size = PageSize.A4;

            using var gfx = XGraphics.FromPdfPage(page);
            var font = new XFont("Arial", 20);

            gfx.DrawString(
                $"Page {i + 1}",
                font,
                XBrushes.Black,
                new XRect(0, 0, page.Width, page.Height),
                XStringFormats.Center);
        }

        document.Save(outputPath);
        return outputPath;
    }

    // 破損PDF生成（4種類のバリエーション）
    public static string GenerateCorruptedPdf(CorruptionType type)
    {
        var fileName = $"corrupted_{type}_{Guid.NewGuid():N}.pdf";
        var outputPath = Path.Combine(TempDir, fileName);

        switch (type)
        {
            case CorruptionType.TruncatedFile:
                // ファイルの途中で切断
                File.WriteAllBytes(outputPath, new byte[] { 0x25, 0x50, 0x44, 0x46 }); // "%PDF"
                break;

            case CorruptionType.InvalidHeader:
                // 不正なヘッダー
                File.WriteAllText(outputPath, "This is not a PDF file");
                break;

            case CorruptionType.MissingXref:
                // xrefテーブルが欠落したPDF
                var validPdf = GeneratePdf(1, "temp.pdf");
                var content = File.ReadAllText(validPdf);
                content = content.Replace("xref", "MISSING");
                File.WriteAllText(outputPath, content);
                File.Delete(validPdf);
                break;

            case CorruptionType.InvalidObjects:
                // オブジェクト定義が不正なPDF
                File.WriteAllText(outputPath, @"%PDF-1.4
1 0 obj
<< /Type /Catalog /Pages INVALID >>
endobj
xref
0 2
0000000000 65535 f
0000000009 00000 n
trailer
<< /Size 2 /Root 1 0 R >>
startxref
72
%%EOF");
                break;
        }

        return outputPath;
    }

    // ランダムPDF生成
    public static string GenerateRandomPdf(Random random = null)
    {
        random ??= new Random();
        var pageCount = random.Next(1, 51); // 1～50ページ
        return GeneratePdf(pageCount);
    }

    // 画像生成
    public static string GenerateImage(ImageFormat format, int width = 800, int height = 600)
    {
        var extension = format switch
        {
            ImageFormat.Jpeg => "jpg",
            ImageFormat.Png => "png",
            _ => "jpg"
        };

        var fileName = $"test_image_{Guid.NewGuid():N}.{extension}";
        var outputPath = Path.Combine(TempDir, fileName);

        using var bitmap = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(bitmap);

        graphics.Clear(Color.White);
        graphics.DrawString(
            $"Test Image {width}x{height}",
            new Font("Arial", 20),
            Brushes.Black,
            new PointF(10, 10));

        bitmap.Save(outputPath, format);
        return outputPath;
    }

    // クリーンアップ
    public static void CleanupTempFiles()
    {
        if (Directory.Exists(TempDir))
        {
            Directory.Delete(TempDir, recursive: true);
            Directory.CreateDirectory(TempDir);
        }
    }
}

public enum CorruptionType
{
    TruncatedFile,
    InvalidHeader,
    MissingXref,
    InvalidObjects
}
```

### 1.2 使用例

```csharp
[Fact]
public async Task Example_GeneratePdf()
{
    // 10ページPDF生成
    var pdfPath = TestDataGenerator.GeneratePdf(pageCount: 10);

    // テスト実行
    var document = await pdfService.LoadPdfAsync(pdfPath);

    // クリーンアップ
    TestDataGenerator.CleanupTempFiles();
}
```

---

## 2. TestDataBuilder（ビルダーパターン）

**目的**: テストデータをビルダーパターンで構築

### 2.1 PdfDocumentBuilder

```csharp
public class PdfDocumentBuilder
{
    private string _filePath = "test.pdf";
    private List<PdfPage> _pages = new();

    public PdfDocumentBuilder WithFilePath(string filePath)
    {
        _filePath = filePath;
        return this;
    }

    public PdfDocumentBuilder WithPage(int pageNumber, int rotation = 0, double width = 595, double height = 842)
    {
        _pages.Add(new PdfPage
        {
            Id = Guid.NewGuid(),
            PageNumber = pageNumber,
            Rotation = rotation,
            Width = width,
            Height = height
        });
        return this;
    }

    public PdfDocumentBuilder WithPages(int count)
    {
        for (int i = 0; i < count; i++)
        {
            WithPage(i + 1);
        }
        return this;
    }

    public PdfDocument Build()
    {
        return new PdfDocument
        {
            FilePath = _filePath,
            Pages = new ObservableCollection<PdfPage>(_pages)
        };
    }
}
```

**使用例**:

```csharp
var document = new PdfDocumentBuilder()
    .WithFilePath("test.pdf")
    .WithPages(10)
    .Build();
```

---

## 3. AssertionHelper（カスタムアサーション）

**目的**: ドメイン固有のアサーションを提供

### 3.1 PdfAssertions

```csharp
public static class PdfAssertions
{
    public static PdfDocumentAssertions Should(this PdfDocument document)
    {
        return new PdfDocumentAssertions(document);
    }
}

public class PdfDocumentAssertions
{
    private readonly PdfDocument _document;

    public PdfDocumentAssertions(PdfDocument document)
    {
        _document = document;
    }

    public AndConstraint<PdfDocumentAssertions> HavePageCount(int expected, string because = "")
    {
        _document.Pages.Should().HaveCount(expected, because);
        return new AndConstraint<PdfDocumentAssertions>(this);
    }

    public AndConstraint<PdfDocumentAssertions> HavePageRotation(int pageIndex, int expectedRotation, string because = "")
    {
        _document.Pages[pageIndex].Rotation.Should().Be(expectedRotation, because);
        return new AndConstraint<PdfDocumentAssertions>(this);
    }

    public AndConstraint<PdfDocumentAssertions> HaveA4Pages(string because = "")
    {
        _document.Pages.Should().OnlyContain(
            p => Math.Abs(p.Width - 595) < 10 && Math.Abs(p.Height - 842) < 10,
            because);
        return new AndConstraint<PdfDocumentAssertions>(this);
    }
}
```

**使用例**:

```csharp
document.Should()
    .HavePageCount(10)
    .And.HaveA4Pages()
    .And.HavePageRotation(4, 90);
```

---

## 4. MockHelper（モック生成ヘルパー）

**目的**: 依存関係のモックを簡単に生成

### 4.1 PdfServiceMockBuilder

```csharp
public class PdfServiceMockBuilder
{
    private readonly Mock<IPdfService> _mock = new();

    public PdfServiceMockBuilder SetupLoadPdfAsync(string filePath, PdfDocument returnDocument)
    {
        _mock.Setup(x => x.LoadPdfAsync(filePath))
            .ReturnsAsync(returnDocument);
        return this;
    }

    public PdfServiceMockBuilder SetupLoadPdfAsyncThrows<TException>(string filePath)
        where TException : Exception, new()
    {
        _mock.Setup(x => x.LoadPdfAsync(filePath))
            .ThrowsAsync(new TException());
        return this;
    }

    public IPdfService Build() => _mock.Object;
    public Mock<IPdfService> BuildMock() => _mock;
}
```

**使用例**:

```csharp
var pdfService = new PdfServiceMockBuilder()
    .SetupLoadPdfAsync("test.pdf", testDocument)
    .Build();
```

---

## 5. TestFixtureBase（テストベースクラス）

**目的**: 共通のセットアップ・クリーンアップを提供

```csharp
public abstract class TestFixtureBase : IDisposable
{
    protected readonly ITestOutputHelper Output;
    protected readonly string TestDataPath;

    protected TestFixtureBase(ITestOutputHelper output)
    {
        Output = output;
        TestDataPath = GetTestDataPath();
    }

    protected string GetTestPdfPath(string fileName)
    {
        return Path.Combine(TestDataPath, "Pdfs", fileName);
    }

    protected string GetTestImagePath(string fileName)
    {
        return Path.Combine(TestDataPath, "Images", fileName);
    }

    private static string GetTestDataPath()
    {
        var baseDir = AppContext.BaseDirectory;
        var solutionRoot = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..\..\.."));
        return Path.Combine(solutionRoot, "tests", "TestData");
    }

    public virtual void Dispose()
    {
        TestDataGenerator.CleanupTempFiles();
    }
}
```

**使用例**:

```csharp
public class PdfServiceTests : TestFixtureBase
{
    private readonly IPdfService _sut;

    public PdfServiceTests(ITestOutputHelper output) : base(output)
    {
        _sut = new PdfService();
    }

    [Fact]
    public async Task LoadPdf_Test()
    {
        var pdfPath = GetTestPdfPath("sample_10pages.pdf");
        var document = await _sut.LoadPdfAsync(pdfPath);
        // ...
    }
}
```

---

## 6. PerformanceAssertions

**目的**: パフォーマンステスト用のアサーション

```csharp
public static class PerformanceAssertions
{
    public static void ShouldCompleteWithin(this Action action, TimeSpan threshold, string because = "")
    {
        var stopwatch = Stopwatch.StartNew();
        action();
        stopwatch.Stop();

        stopwatch.Elapsed.Should().BeLessThan(threshold, because);
    }

    public static async Task ShouldCompleteWithinAsync(this Func<Task> action, TimeSpan threshold, string because = "")
    {
        var stopwatch = Stopwatch.StartNew();
        await action();
        stopwatch.Stop();

        stopwatch.Elapsed.Should().BeLessThan(threshold, because);
    }

    public static void ShouldUseMemoryLessThan(this Action action, long maxMemoryIncreaseMB, string because = "")
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var initialMemory = GC.GetTotalMemory(forceFullCollection: true);

        action();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(forceFullCollection: true);
        var memoryIncreaseMB = (finalMemory - initialMemory) / 1024.0 / 1024.0;

        memoryIncreaseMB.Should().BeLessThan(maxMemoryIncreaseMB, because);
    }
}
```

**使用例**:

```csharp
await (() => pdfService.LoadPdfAsync(largePdfPath))
    .ShouldCompleteWithinAsync(TimeSpan.FromSeconds(3));

(() => ProcessLargeData())
    .ShouldUseMemoryLessThan(maxMemoryIncreaseMB: 100);
```

---

## まとめ

これらのヘルパークラスにより、テストコードの可読性と保守性が大幅に向上します。実装フェーズでは、これらのヘルパーを最初に実装し、その後各テストケースを実装することを推奨します。
