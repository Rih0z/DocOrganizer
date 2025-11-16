using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Extensions;
using FluentAssertions;
using DocOrganizer.IntegrationTests.Fixtures;
using DocOrganizer.IntegrationTests.Helpers;
using DocOrganizer.Application.Interfaces;
using DocOrganizer.Application.Interfaces.V3;

namespace DocOrganizer.IntegrationTests.Tests;

/// <summary>
/// IT-010: エラーハンドリング統合テスト
/// 異常系のエラーハンドリングを検証
/// </summary>
public class IT010_ErrorHandling_Integration_Test
{
    /// <summary>
    /// IT-010A: 存在しないファイルでFileNotFoundException
    /// 存在しないパスでOpenPdfAsync()実行時にFileNotFoundExceptionが発生することを検証
    /// </summary>
    [StaFact]
    [Trait("Category", "Integration")]
    [Trait("Priority", "High")]
    public async Task IT010A_OpenPdf_NonExistentFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var fixture = new IntegrationTestFixture();
        var pdfService = fixture.GetService<IPdfService>();
        var nonExistentPath = Path.Combine(Path.GetTempPath(), $"non_existent_{Guid.NewGuid()}.pdf");

        // Act & Assert: 存在しないファイル読み込みで例外発生
        await fixture.InvokeAsync(async () =>
        {
            Func<Task> act = async () => await pdfService.LoadPdfAsync(nonExistentPath);
            await act.Should().ThrowAsync<FileNotFoundException>("存在しないファイルでFileNotFoundExceptionが発生すること");
        });
    }

    /// <summary>
    /// IT-010A: 破損PDFで例外発生
    /// 破損したPDFファイルでOpenPdfAsync()実行時に例外が発生することを検証
    /// </summary>
    [StaFact]
    [Trait("Category", "Integration")]
    [Trait("Priority", "High")]
    public async Task IT010A_OpenPdf_CorruptedFile_ShouldThrowException()
    {
        // Arrange
        var fixture = new IntegrationTestFixture();
        var pdfService = fixture.GetService<IPdfService>();
        var corruptedPdfPath = TestDataHelper.GenerateCorruptedPdf();

        try
        {
            // Act & Assert: 破損PDFで例外発生
            await fixture.InvokeAsync(async () =>
            {
                Func<Task> act = async () => await pdfService.LoadPdfAsync(corruptedPdfPath);
                await act.Should().ThrowAsync<Exception>("破損PDFで例外が発生すること");
            });
        }
        finally
        {
            TestDataHelper.CleanupTempFile(corruptedPdfPath);
        }
    }

    /// <summary>
    /// IT-010A: null引数でNullReferenceException
    /// RotatePage(null, 90)実行時にNullReferenceExceptionが発生することを検証
    /// </summary>
    [StaFact]
    [Trait("Category", "Integration")]
    [Trait("Priority", "High")]
    public async Task IT010A_RotatePage_NullPage_ShouldThrowNullReferenceException()
    {
        // Arrange
        var fixture = new IntegrationTestFixture();
        var pdfEditorService = fixture.GetService<IPdfEditorService>();

        // Act & Assert: null引数でNullReferenceException
        await fixture.InvokeAsync(() =>
        {
            Action act = () => pdfEditorService.RotatePage(null!, 90);
            act.Should().Throw<NullReferenceException>("null引数でNullReferenceExceptionが発生すること");
            return Task.CompletedTask;
        });
    }
}
