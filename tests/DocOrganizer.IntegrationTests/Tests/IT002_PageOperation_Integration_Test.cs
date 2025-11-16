using System;
using System.Linq;
using System.Threading.Tasks;
using DocOrganizer.IntegrationTests.Fixtures;
using DocOrganizer.IntegrationTests.Helpers;
using DocOrganizer.Application.Interfaces;
using FluentAssertions;
using Xunit;

namespace DocOrganizer.IntegrationTests.Tests;

/// <summary>
/// IT-002: ページ操作統合テスト
/// ページ削除・移動・回転の統合動作を検証
/// </summary>
public class IT002_PageOperation_Integration_Test
{
    /// <summary>
    /// IT-002A: ページ削除統合テスト
    /// IPdfEditorService.RemovePage()の基本動作を検証
    /// </summary>
    [StaFact]
    [Trait("Category", "Integration")]
    [Trait("Priority", "High")]
    public async Task IT002A_RemovePage_ShouldDeletePageFromDocument()
    {
        // Arrange
        var fixture = new IntegrationTestFixture();
        var pdfEditorService = fixture.GetService<IPdfEditorService>();
        var testPdfPath = TestDataHelper.GenerateSamplePdf(10); // 10ページPDF生成

        try
        {
            await fixture.InvokeAsync(async () =>
            {
                // PDF読み込み
                var document = await pdfEditorService.OpenPdfAsync(testPdfPath);
                document.Should().NotBeNull();
                document.Pages.Should().HaveCount(10, "削除前は10ページあること");

                // Act: 5ページ目（index=4）を削除
                pdfEditorService.RemovePage(document, 4);

                // Assert: ページ数が1減っていること
                document.Pages.Should().HaveCount(9, "削除後は9ページになること");

                // Assert: すべてのページが正常に保持されていること
                document.Pages.All(p => p.Width > 0 && p.Height > 0).Should().BeTrue("削除後も残りページは正常であること");
            });
        }
        finally
        {
            TestDataHelper.CleanupTempFile(testPdfPath);
        }
    }

    /// <summary>
    /// IT-002A: ページ削除統合テスト（先頭ページ）
    /// 先頭ページの削除動作を検証
    /// </summary>
    [StaFact]
    [Trait("Category", "Integration")]
    [Trait("Priority", "Medium")]
    public async Task IT002A_RemovePage_ShouldDeleteFirstPage()
    {
        // Arrange
        var fixture = new IntegrationTestFixture();
        var pdfEditorService = fixture.GetService<IPdfEditorService>();
        var testPdfPath = TestDataHelper.GenerateSamplePdf(5); // 5ページPDF生成

        try
        {
            await fixture.InvokeAsync(async () =>
            {
                var document = await pdfEditorService.OpenPdfAsync(testPdfPath);

                // Act: 先頭ページ（index=0）を削除
                pdfEditorService.RemovePage(document, 0);

                // Assert
                document.Pages.Should().HaveCount(4, "削除後は4ページになること");
            });
        }
        finally
        {
            TestDataHelper.CleanupTempFile(testPdfPath);
        }
    }

    /// <summary>
    /// IT-002A: ページ削除統合テスト（最終ページ）
    /// 最終ページの削除動作を検証
    /// </summary>
    [StaFact]
    [Trait("Category", "Integration")]
    [Trait("Priority", "Medium")]
    public async Task IT002A_RemovePage_ShouldDeleteLastPage()
    {
        // Arrange
        var fixture = new IntegrationTestFixture();
        var pdfEditorService = fixture.GetService<IPdfEditorService>();
        var testPdfPath = TestDataHelper.GenerateSamplePdf(5); // 5ページPDF生成

        try
        {
            await fixture.InvokeAsync(async () =>
            {
                var document = await pdfEditorService.OpenPdfAsync(testPdfPath);

                // Act: 最終ページ（index=4）を削除
                pdfEditorService.RemovePage(document, 4);

                // Assert
                document.Pages.Should().HaveCount(4, "削除後は4ページになること");
            });
        }
        finally
        {
            TestDataHelper.CleanupTempFile(testPdfPath);
        }
    }

    /// <summary>
    /// IT-002B: ページ回転統合テスト
    /// IPdfEditorService.RotatePage()の基本動作を検証
    /// </summary>
    [StaFact]
    [Trait("Category", "Integration")]
    [Trait("Priority", "High")]
    public async Task IT002B_RotatePage_ShouldRotatePageBy90Degrees()
    {
        // Arrange
        var fixture = new IntegrationTestFixture();
        var pdfEditorService = fixture.GetService<IPdfEditorService>();
        var testPdfPath = TestDataHelper.GenerateSamplePdf(5); // 5ページPDF生成

        try
        {
            await fixture.InvokeAsync(async () =>
            {
                var document = await pdfEditorService.OpenPdfAsync(testPdfPath);
                var targetPage = document.Pages[2]; // 3ページ目
                var originalRotation = targetPage.Rotation;

                // Act: 90度回転
                pdfEditorService.RotatePage(targetPage, 90);

                // Assert: 回転角度が90度増加していること
                targetPage.Rotation.Should().Be((originalRotation + 90) % 360, "90度回転していること");
            });
        }
        finally
        {
            TestDataHelper.CleanupTempFile(testPdfPath);
        }
    }

    /// <summary>
    /// IT-002B: ページ回転統合テスト（180度）
    /// 180度回転の動作を検証
    /// </summary>
    [StaFact]
    [Trait("Category", "Integration")]
    [Trait("Priority", "Medium")]
    public async Task IT002B_RotatePage_ShouldRotatePageBy180Degrees()
    {
        // Arrange
        var fixture = new IntegrationTestFixture();
        var pdfEditorService = fixture.GetService<IPdfEditorService>();
        var testPdfPath = TestDataHelper.GenerateSamplePdf(3); // 3ページPDF生成

        try
        {
            await fixture.InvokeAsync(async () =>
            {
                var document = await pdfEditorService.OpenPdfAsync(testPdfPath);
                var targetPage = document.Pages[1]; // 2ページ目
                var originalRotation = targetPage.Rotation;

                // Act: 180度回転
                pdfEditorService.RotatePage(targetPage, 180);

                // Assert
                targetPage.Rotation.Should().Be((originalRotation + 180) % 360, "180度回転していること");
            });
        }
        finally
        {
            TestDataHelper.CleanupTempFile(testPdfPath);
        }
    }

    /// <summary>
    /// IT-002B: ページ回転統合テスト（270度）
    /// 270度回転の動作を検証
    /// </summary>
    [StaFact]
    [Trait("Category", "Integration")]
    [Trait("Priority", "Medium")]
    public async Task IT002B_RotatePage_ShouldRotatePageBy270Degrees()
    {
        // Arrange
        var fixture = new IntegrationTestFixture();
        var pdfEditorService = fixture.GetService<IPdfEditorService>();
        var testPdfPath = TestDataHelper.GenerateSamplePdf(3); // 3ページPDF生成

        try
        {
            await fixture.InvokeAsync(async () =>
            {
                var document = await pdfEditorService.OpenPdfAsync(testPdfPath);
                var targetPage = document.Pages[0]; // 1ページ目
                var originalRotation = targetPage.Rotation;

                // Act: 270度回転
                pdfEditorService.RotatePage(targetPage, 270);

                // Assert
                targetPage.Rotation.Should().Be((originalRotation + 270) % 360, "270度回転していること");
            });
        }
        finally
        {
            TestDataHelper.CleanupTempFile(testPdfPath);
        }
    }

    /// <summary>
    /// IT-002C: ページ並び替え統合テスト
    /// IPdfEditorService.ReorderPages()の基本動作を検証
    /// </summary>
    [StaFact]
    [Trait("Category", "Integration")]
    [Trait("Priority", "High")]
    public async Task IT002C_ReorderPages_ShouldRearrangePages()
    {
        // Arrange
        var fixture = new IntegrationTestFixture();
        var pdfEditorService = fixture.GetService<IPdfEditorService>();
        var testPdfPath = TestDataHelper.GenerateSamplePdf(5); // 5ページPDF生成

        try
        {
            await fixture.InvokeAsync(async () =>
            {
                var document = await pdfEditorService.OpenPdfAsync(testPdfPath);

                // 元の順序: Page 1, 2, 3, 4, 5
                // 新しい順序: Page 5, 4, 3, 2, 1（逆順）
                var newOrder = document.Pages.Reverse().ToArray();

                // Act: ページ並び替え
                pdfEditorService.ReorderPages(document, newOrder);

                // Assert: ページ順序が逆転していること
                document.Pages.Should().HaveCount(5, "ページ数は変わらないこと");
                document.Pages[0].PageNumber.Should().Be(5, "1番目のページが元の5ページ目であること");
                document.Pages[1].PageNumber.Should().Be(4, "2番目のページが元の4ページ目であること");
                document.Pages[2].PageNumber.Should().Be(3, "3番目のページが元の3ページ目であること");
                document.Pages[3].PageNumber.Should().Be(2, "4番目のページが元の2ページ目であること");
                document.Pages[4].PageNumber.Should().Be(1, "5番目のページが元の1ページ目であること");
            });
        }
        finally
        {
            TestDataHelper.CleanupTempFile(testPdfPath);
        }
    }

    /// <summary>
    /// IT-002C: ページ並び替え統合テスト（部分入れ替え）
    /// 一部のページの順序入れ替えを検証
    /// </summary>
    [StaFact]
    [Trait("Category", "Integration")]
    [Trait("Priority", "Medium")]
    public async Task IT002C_ReorderPages_ShouldSwapSpecificPages()
    {
        // Arrange
        var fixture = new IntegrationTestFixture();
        var pdfEditorService = fixture.GetService<IPdfEditorService>();
        var testPdfPath = TestDataHelper.GenerateSamplePdf(5); // 5ページPDF生成

        try
        {
            await fixture.InvokeAsync(async () =>
            {
                var document = await pdfEditorService.OpenPdfAsync(testPdfPath);

                // 元の順序: Page 1, 2, 3, 4, 5
                // 新しい順序: Page 1, 3, 2, 4, 5（2ページ目と3ページ目を入れ替え）
                var newOrder = new[]
                {
                    document.Pages[0], // Page 1
                    document.Pages[2], // Page 3
                    document.Pages[1], // Page 2
                    document.Pages[3], // Page 4
                    document.Pages[4]  // Page 5
                };

                // Act: ページ並び替え
                pdfEditorService.ReorderPages(document, newOrder);

                // Assert
                document.Pages.Should().HaveCount(5);
                document.Pages[0].PageNumber.Should().Be(1);
                document.Pages[1].PageNumber.Should().Be(3);
                document.Pages[2].PageNumber.Should().Be(2);
                document.Pages[3].PageNumber.Should().Be(4);
                document.Pages[4].PageNumber.Should().Be(5);
            });
        }
        finally
        {
            TestDataHelper.CleanupTempFile(testPdfPath);
        }
    }
}
