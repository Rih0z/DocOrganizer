using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using DocOrganizer.IntegrationTests.Fixtures;
using DocOrganizer.IntegrationTests.Helpers;
using DocOrganizer.Application.Interfaces;

namespace DocOrganizer.IntegrationTests.Tests;

/// <summary>
/// IT-004: ページ削除統合テスト
/// 複数ページ削除・最終ページ削除・無効インデックスの異常系を検証
/// </summary>
public class IT004_PageDeletion_Integration_Test
{
    /// <summary>
    /// IT-004A: 複数ページ削除統合テスト
    /// 複数ページを連続削除した際に全て正しく削除されることを検証
    /// </summary>
    [StaFact]
    [Trait("Category", "Integration")]
    [Trait("Priority", "High")]
    public async Task IT004A_RemoveMultiplePages_ShouldDeleteAllSelectedPages()
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

                // Act: 3ページ削除（index=2, 4, 6を削除）
                // 注意: 削除は後ろから順に実行する（インデックスのずれを防ぐため）
                pdfEditorService.RemovePage(document, 6);
                pdfEditorService.RemovePage(document, 4);
                pdfEditorService.RemovePage(document, 2);

                // Assert: ページ数が3減っていること
                document.Pages.Should().HaveCount(7, "3ページ削除後は7ページになること");

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
    /// IT-004A: 最終ページ削除統合テスト（1ページPDFの削除）
    /// 1ページしかないPDFの最終ページを削除した際の動作を検証
    /// 仕様: 1ページPDFの最終ページ削除は許可される（0ページPDFになる）
    /// </summary>
    [StaFact]
    [Trait("Category", "Integration")]
    [Trait("Priority", "High")]
    public async Task IT004A_RemoveLastPage_ShouldAllowDeletionOfLastPage()
    {
        // Arrange
        var fixture = new IntegrationTestFixture();
        var pdfEditorService = fixture.GetService<IPdfEditorService>();
        var testPdfPath = TestDataHelper.GenerateSamplePdf(1); // 1ページPDF生成

        try
        {
            await fixture.InvokeAsync(async () =>
            {
                var document = await pdfEditorService.OpenPdfAsync(testPdfPath);
                document.Should().NotBeNull();
                document.Pages.Should().HaveCount(1, "削除前は1ページあること");

                // Act: 最終ページ（index=0）を削除
                pdfEditorService.RemovePage(document, 0);

                // Assert: 0ページになること（削除は許可される）
                document.Pages.Should().HaveCount(0, "最終ページ削除後は0ページになること");
            });
        }
        finally
        {
            TestDataHelper.CleanupTempFile(testPdfPath);
        }
    }

    /// <summary>
    /// IT-004A: 無効インデックス削除テスト
    /// 存在しないインデックスでRemovePage()実行時に削除が無視されることを検証
    /// 仕様: 無効なインデックスは静かに無視される（防御的プログラミング）
    /// </summary>
    [StaFact]
    [Trait("Category", "Integration")]
    [Trait("Priority", "High")]
    public async Task IT004A_RemoveInvalidIndex_ShouldIgnoreInvalidIndex()
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
                document.Should().NotBeNull();
                document.Pages.Should().HaveCount(5, "5ページあること");

                // Act: 無効なインデックス（10）で削除試行
                pdfEditorService.RemovePage(document, 10);

                // Assert: ページ数は変わらないこと（無効インデックスは無視される）
                document.Pages.Should().HaveCount(5, "無効なインデックスは無視され、ページ数は変わらないこと");

                // Act: 負のインデックス（-1）で削除試行
                pdfEditorService.RemovePage(document, -1);

                // Assert: ページ数は変わらないこと
                document.Pages.Should().HaveCount(5, "負のインデックスも無視され、ページ数は変わらないこと");
            });
        }
        finally
        {
            TestDataHelper.CleanupTempFile(testPdfPath);
        }
    }
}
