using System.Linq;
using System.Threading.Tasks;
using DocOrganizer.IntegrationTests.Fixtures;
using DocOrganizer.IntegrationTests.Helpers;
using DocOrganizer.Application.Interfaces;
using FluentAssertions;
using Xunit;

namespace DocOrganizer.IntegrationTests.Tests;

/// <summary>
/// IT-001: PDF読み込み統合テスト
/// サービスレイヤーからViewModelまでの統合動作を検証
/// </summary>
public class IT001_PdfLoad_Integration_Test
{
    /// <summary>
    /// IT-001A: サービスレイヤー統合テスト
    /// IPdfService.LoadPdfAsync()の基本動作を検証
    /// </summary>
    [StaFact]
    [Trait("Category", "Integration")]
    [Trait("Priority", "High")]
    public async Task IT001A_LoadPdf_ServiceLayer_ShouldLoadPages()
    {
        // Arrange
        var fixture = new IntegrationTestFixture();
        var pdfService = fixture.GetService<IPdfService>();
        var testPdfPath = TestDataHelper.GenerateSamplePdf(10); // 10ページPDF生成

        try
        {
            // Act & Assert - UIスレッドで実行
            await fixture.InvokeAsync(async () =>
            {
                // PDF読み込み
                var document = await pdfService.LoadPdfAsync(testPdfPath);

                // Assert: PDF読み込み成功
                document.Should().NotBeNull("PDF読み込みが成功すること");

                // Assert: ページ数一致
                document.Pages.Should().HaveCount(10, "10ページのPDFを読み込んだため");

                // Assert: ページプロパティ正常
                document.Pages.All(p => p.Width > 0).Should().BeTrue("全ページにWidth設定があること");
                document.Pages.All(p => p.Height > 0).Should().BeTrue("全ページにHeight設定があること");

                // Assert: ページ順序正常
                for (int i = 0; i < document.Pages.Count; i++)
                {
                    document.Pages[i].PageNumber.Should().Be(i + 1, $"ページ{i + 1}のPageNumberが正しいこと");
                }
            });
        }
        finally
        {
            // Cleanup: 一時ファイル削除
            TestDataHelper.CleanupTempFile(testPdfPath);
        }
    }

    /// <summary>
    /// IT-001A: サービスレイヤー統合テスト（1ページPDF）
    /// 最小ケースでの動作を検証
    /// </summary>
    [StaFact]
    [Trait("Category", "Integration")]
    [Trait("Priority", "Medium")]
    public async Task IT001A_LoadPdf_ServiceLayer_ShouldHandleSinglePagePdf()
    {
        // Arrange
        var fixture = new IntegrationTestFixture();
        var pdfService = fixture.GetService<IPdfService>();
        var testPdfPath = TestDataHelper.GenerateSamplePdf(1); // 1ページPDF生成

        try
        {
            // Act & Assert
            await fixture.InvokeAsync(async () =>
            {
                var document = await pdfService.LoadPdfAsync(testPdfPath);

                // Assert
                document.Should().NotBeNull();
                document.Pages.Should().HaveCount(1, "1ページのPDFを読み込んだため");
                document.Pages[0].Width.Should().BeGreaterThan(0);
                document.Pages[0].Height.Should().BeGreaterThan(0);
            });
        }
        finally
        {
            TestDataHelper.CleanupTempFile(testPdfPath);
        }
    }

    /// <summary>
    /// IT-001A: サービスレイヤー統合テスト（大量ページPDF）
    /// パフォーマンステスト（50ページ）
    /// </summary>
    [StaFact]
    [Trait("Category", "Integration")]
    [Trait("Priority", "High")]
    public async Task IT001A_LoadPdf_ServiceLayer_ShouldLoadLargePdf()
    {
        // Arrange
        var fixture = new IntegrationTestFixture();
        var pdfService = fixture.GetService<IPdfService>();
        var testPdfPath = TestDataHelper.GenerateSamplePdf(50); // 50ページPDF生成

        try
        {
            // Act & Assert
            await fixture.InvokeAsync(async () =>
            {
                var document = await pdfService.LoadPdfAsync(testPdfPath);

                // Assert
                document.Should().NotBeNull();
                document.Pages.Should().HaveCount(50, "50ページのPDFを読み込んだため");
                document.Pages.All(p => p.Width > 0 && p.Height > 0).Should().BeTrue("大量ページでも全ページ正常読み込み");
            });
        }
        finally
        {
            TestDataHelper.CleanupTempFile(testPdfPath);
        }
    }
}
