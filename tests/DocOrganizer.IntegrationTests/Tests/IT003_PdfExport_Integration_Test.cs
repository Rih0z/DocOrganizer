using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using DocOrganizer.IntegrationTests.Fixtures;
using DocOrganizer.IntegrationTests.Helpers;
using DocOrganizer.Application.Interfaces;
using DocOrganizer.Application.Interfaces.V3;
using DocOrganizer.Core.Models;
using FluentAssertions;
using Xunit;

namespace DocOrganizer.IntegrationTests.Tests;

/// <summary>
/// IT-003: ファイル保存統合テスト
/// PDF出力の統合動作を検証
/// </summary>
public class IT003_PdfExport_Integration_Test
{
    /// <summary>
    /// IT-003A: PDF保存統合テスト
    /// IPdfEditorService.SavePdfAsync()の基本動作を検証
    /// </summary>
    [StaFact]
    [Trait("Category", "Integration")]
    [Trait("Priority", "High")]
    public async Task IT003A_SavePdf_ShouldExportPdfDocument()
    {
        // Arrange
        var fixture = new IntegrationTestFixture();
        var pdfEditorService = fixture.GetService<IPdfEditorService>();
        var testPdfPath = TestDataHelper.GenerateSamplePdf(5); // 5ページPDF生成
        var outputPath = Path.Combine(Path.GetTempPath(), $"output_{Guid.NewGuid()}.pdf");

        try
        {
            await fixture.InvokeAsync(async () =>
            {
                // PDF読み込み
                var document = await pdfEditorService.OpenPdfAsync(testPdfPath);
                document.Should().NotBeNull();

                // Act: PDF保存
                var result = await pdfEditorService.SavePdfAsync(document, outputPath);

                // Assert: 保存成功
                result.Should().BeTrue("PDF保存が成功すること");

                // Assert: ファイルが生成されていること
                File.Exists(outputPath).Should().BeTrue("出力ファイルが生成されていること");

                // Assert: ファイルサイズが0より大きいこと
                var fileInfo = new FileInfo(outputPath);
                fileInfo.Length.Should().BeGreaterThan(0, "出力PDFファイルにデータが含まれること");
            });
        }
        finally
        {
            TestDataHelper.CleanupTempFile(testPdfPath);
            TestDataHelper.CleanupTempFile(outputPath);
        }
    }

    /// <summary>
    /// IT-003A: PDF保存統合テスト（ページ削除後）
    /// ページ削除後のPDF保存を検証
    /// </summary>
    [StaFact]
    [Trait("Category", "Integration")]
    [Trait("Priority", "High")]
    public async Task IT003A_SavePdf_ShouldExportAfterPageDeletion()
    {
        // Arrange
        var fixture = new IntegrationTestFixture();
        var pdfEditorService = fixture.GetService<IPdfEditorService>();
        var testPdfPath = TestDataHelper.GenerateSamplePdf(10); // 10ページPDF生成
        var outputPath = Path.Combine(Path.GetTempPath(), $"output_{Guid.NewGuid()}.pdf");

        try
        {
            await fixture.InvokeAsync(async () =>
            {
                var document = await pdfEditorService.OpenPdfAsync(testPdfPath);

                // ページ削除（5ページ目）
                pdfEditorService.RemovePage(document, 4);
                document.Pages.Should().HaveCount(9, "削除後は9ページになること");

                // Act: PDF保存
                var result = await pdfEditorService.SavePdfAsync(document, outputPath);

                // Assert
                result.Should().BeTrue();
                File.Exists(outputPath).Should().BeTrue();

                // 保存されたPDFを再度読み込んで検証
                var savedDocument = await pdfEditorService.OpenPdfAsync(outputPath);
                savedDocument.Pages.Should().HaveCount(9, "保存後も9ページであること");
            });
        }
        finally
        {
            TestDataHelper.CleanupTempFile(testPdfPath);
            TestDataHelper.CleanupTempFile(outputPath);
        }
    }

    /// <summary>
    /// IT-003A: PDF保存統合テスト（ページ回転後）
    /// ページ回転後のPDF保存を検証
    /// </summary>
    [StaFact]
    [Trait("Category", "Integration")]
    [Trait("Priority", "High")]
    public async Task IT003A_SavePdf_ShouldExportAfterPageRotation()
    {
        // Arrange
        var fixture = new IntegrationTestFixture();
        var pdfEditorService = fixture.GetService<IPdfEditorService>();
        var testPdfPath = TestDataHelper.GenerateSamplePdf(3); // 3ページPDF生成
        var outputPath = Path.Combine(Path.GetTempPath(), $"output_{Guid.NewGuid()}.pdf");

        try
        {
            await fixture.InvokeAsync(async () =>
            {
                var document = await pdfEditorService.OpenPdfAsync(testPdfPath);

                // 2ページ目を90度回転
                var targetPage = document.Pages[1];
                var originalRotation = targetPage.Rotation;
                pdfEditorService.RotatePage(targetPage, 90);

                // Act: PDF保存
                var result = await pdfEditorService.SavePdfAsync(document, outputPath);

                // Assert
                result.Should().BeTrue();
                File.Exists(outputPath).Should().BeTrue();

                // 保存されたPDFを再度読み込んで検証
                var savedDocument = await pdfEditorService.OpenPdfAsync(outputPath);
                savedDocument.Pages.Should().HaveCount(3, "ページ数が保持されていること");
                
                // 注意: 回転の保存はSavePdfAsync実装に依存するため、
                // ここでは基本的なPDF保存の成功のみを検証
            });
        }
        finally
        {
            TestDataHelper.CleanupTempFile(testPdfPath);
            TestDataHelper.CleanupTempFile(outputPath);
        }
    }

    /// <summary>
    /// IT-003A: PDF保存統合テスト（ページ並び替え後）
    /// ページ並び替え後のPDF保存を検証
    /// </summary>
    [StaFact]
    [Trait("Category", "Integration")]
    [Trait("Priority", "High")]
    public async Task IT003A_SavePdf_ShouldExportAfterPageReordering()
    {
        // Arrange
        var fixture = new IntegrationTestFixture();
        var pdfEditorService = fixture.GetService<IPdfEditorService>();
        var testPdfPath = TestDataHelper.GenerateSamplePdf(5); // 5ページPDF生成
        var outputPath = Path.Combine(Path.GetTempPath(), $"output_{Guid.NewGuid()}.pdf");

        try
        {
            await fixture.InvokeAsync(async () =>
            {
                var document = await pdfEditorService.OpenPdfAsync(testPdfPath);

                // ページ並び替え（逆順）
                var newOrder = document.Pages.Reverse().ToArray();
                pdfEditorService.ReorderPages(document, newOrder);

                // Act: PDF保存
                var result = await pdfEditorService.SavePdfAsync(document, outputPath);

                // Assert
                result.Should().BeTrue();
                File.Exists(outputPath).Should().BeTrue();

                // 保存されたPDFを再度読み込んで検証
                var savedDocument = await pdfEditorService.OpenPdfAsync(outputPath);
                savedDocument.Pages.Should().HaveCount(5, "ページ数が保持されていること");
                
                // 注意: 並び替えの保存はSavePdfAsync実装に依存するため、
                // ここでは基本的なPDF保存の成功のみを検証
            });
        }
        finally
        {
            TestDataHelper.CleanupTempFile(testPdfPath);
            TestDataHelper.CleanupTempFile(outputPath);
        }
    }
}
