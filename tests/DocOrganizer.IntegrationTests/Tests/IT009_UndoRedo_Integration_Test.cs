using System;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using DocOrganizer.IntegrationTests.Fixtures;
using DocOrganizer.IntegrationTests.Helpers;
using DocOrganizer.Application.Interfaces;

namespace DocOrganizer.IntegrationTests.Tests;

/// <summary>
/// IT-009: Undo/Redo統合テスト
/// Undo/Redo機能の統合動作を検証
/// </summary>
public class IT009_UndoRedo_Integration_Test
{
    /// <summary>
    /// IT-009A: ページ回転後のUndo
    /// ページ回転後にUndoして元の状態に戻ることを検証
    /// </summary>
    [StaFact]
    [Trait("Category", "Integration")]
    [Trait("Priority", "High")]
    public async Task IT009A_Undo_After_PageRotation()
    {
        // Arrange
        var fixture = new IntegrationTestFixture();
        var pdfEditorService = fixture.GetService<IPdfEditorService>();
        var testPdfPath = TestDataHelper.GenerateSamplePdf(5); // 5ページPDF生成

        try
        {
            await fixture.InvokeAsync(async () =>
            {
                await pdfEditorService.OpenFileAsync(testPdfPath);
                var document = pdfEditorService.CurrentDocument!;
                var targetPage = document.Pages[2]; // 3ページ目
                var originalRotation = targetPage.Rotation;

                // Act: ページを90度回転
                await pdfEditorService.RotatePagesAsync(new[] { 2 }, 90);

                // Assert: 回転が適用されていること
                targetPage.Rotation.Should().Be((originalRotation + 90) % 360, "90度回転していること");
                pdfEditorService.CanUndo.Should().BeTrue("回転後はUndoが可能であること");

                // Act: Undo実行
                await pdfEditorService.UndoAsync();

                // Assert: 元の回転角度に戻っていること（Undo後にCurrentDocumentが置き換わるため再取得）
                var undoneDocument = pdfEditorService.CurrentDocument!;
                undoneDocument.Pages[2].Rotation.Should().Be(originalRotation, "Undo後は元の回転角度に戻ること");
            });
        }
        finally
        {
            TestDataHelper.CleanupTempFile(testPdfPath);
        }
    }

    /// <summary>
    /// IT-009A: Undo後のRedo
    /// Undo後にRedoして操作を再実行できることを検証
    /// </summary>
    [StaFact]
    [Trait("Category", "Integration")]
    [Trait("Priority", "High")]
    public async Task IT009A_Redo_After_Undo()
    {
        // Arrange
        var fixture = new IntegrationTestFixture();
        var pdfEditorService = fixture.GetService<IPdfEditorService>();
        var testPdfPath = TestDataHelper.GenerateSamplePdf(5); // 5ページPDF生成

        try
        {
            await fixture.InvokeAsync(async () =>
            {
                await pdfEditorService.OpenFileAsync(testPdfPath);
                var document = pdfEditorService.CurrentDocument!;
                var targetPage = document.Pages[1]; // 2ページ目
                var originalRotation = targetPage.Rotation;

                // Act: ページを180度回転
                await pdfEditorService.RotatePagesAsync(new[] { 1 }, 180);
                var rotatedAngle = targetPage.Rotation;

                // Undo実行
                await pdfEditorService.UndoAsync();
                var undoneDocument = pdfEditorService.CurrentDocument!;
                undoneDocument.Pages[1].Rotation.Should().Be(originalRotation, "Undo後は元の状態");

                pdfEditorService.CanRedo.Should().BeTrue("Undo後はRedoが可能であること");

                // Act: Redo実行
                await pdfEditorService.RedoAsync();

                // Assert: 再び回転が適用されていること（Redo後にCurrentDocumentが置き換わるため再取得）
                var redoneDocument = pdfEditorService.CurrentDocument!;
                redoneDocument.Pages[1].Rotation.Should().Be(rotatedAngle, "Redo後は回転が再適用されること");
            });
        }
        finally
        {
            TestDataHelper.CleanupTempFile(testPdfPath);
        }
    }

    /// <summary>
    /// IT-009A: ページ削除後のUndo
    /// ページ削除後にUndoしてページが復元されることを検証
    /// </summary>
    [StaFact]
    [Trait("Category", "Integration")]
    [Trait("Priority", "High")]
    public async Task IT009A_Undo_After_PageDeletion()
    {
        // Arrange
        var fixture = new IntegrationTestFixture();
        var pdfEditorService = fixture.GetService<IPdfEditorService>();
        var testPdfPath = TestDataHelper.GenerateSamplePdf(5); // 5ページPDF生成

        try
        {
            await fixture.InvokeAsync(async () =>
            {
                await pdfEditorService.OpenFileAsync(testPdfPath);
                var document = pdfEditorService.CurrentDocument!;
                document.Pages.Should().HaveCount(5, "削除前は5ページあること");

                // Act: ページ削除（index=2）
                await pdfEditorService.RemovePagesAsync(new[] { 2 });

                // Assert: 4ページになっていること
                document.Pages.Should().HaveCount(4, "削除後は4ページになること");
                pdfEditorService.CanUndo.Should().BeTrue("削除後はUndoが可能であること");

                // Act: Undo実行
                await pdfEditorService.UndoAsync();

                // Assert: 5ページに戻っていること（Undo後にCurrentDocumentが置き換わるため再取得）
                var undoneDocument = pdfEditorService.CurrentDocument!;
                undoneDocument.Pages.Should().HaveCount(5, "Undo後はページが復元され5ページに戻ること");
            });
        }
        finally
        {
            TestDataHelper.CleanupTempFile(testPdfPath);
        }
    }

    /// <summary>
    /// IT-009A: 複数Undo後のRedo
    /// 複数回Undoした後にRedoできることを検証
    /// </summary>
    [StaFact]
    [Trait("Category", "Integration")]
    [Trait("Priority", "Medium")]
    public async Task IT009A_Redo_After_Multiple_Undos()
    {
        // Arrange
        var fixture = new IntegrationTestFixture();
        var pdfEditorService = fixture.GetService<IPdfEditorService>();
        var testPdfPath = TestDataHelper.GenerateSamplePdf(5); // 5ページPDF生成

        try
        {
            await fixture.InvokeAsync(async () =>
            {
                await pdfEditorService.OpenFileAsync(testPdfPath);
                var document = pdfEditorService.CurrentDocument!;
                var page1 = document.Pages[0];
                var page2 = document.Pages[1];
                var originalRotation1 = page1.Rotation;
                var originalRotation2 = page2.Rotation;

                // Act: 2つの操作を実行
                await pdfEditorService.RotatePagesAsync(new[] { 0 }, 90);  // 1ページ目を回転
                await pdfEditorService.RotatePagesAsync(new[] { 1 }, 180); // 2ページ目を回転

                // 2回Undo
                await pdfEditorService.UndoAsync(); // 2ページ目の回転をUndo
                await pdfEditorService.UndoAsync(); // 1ページ目の回転をUndo

                // Assert: 両方とも元に戻っていること（Undo後にCurrentDocumentが置き換わるため再取得）
                var undoneDocument = pdfEditorService.CurrentDocument!;
                undoneDocument.Pages[0].Rotation.Should().Be(originalRotation1, "2回Undo後、1ページ目は元の状態");
                undoneDocument.Pages[1].Rotation.Should().Be(originalRotation2, "2回Undo後、2ページ目は元の状態");

                pdfEditorService.CanRedo.Should().BeTrue("Undo後はRedoが可能であること");

                // Act: 1回Redo
                await pdfEditorService.RedoAsync();

                // Assert: 1ページ目の回転が再適用されること（Redo後にCurrentDocumentが置き換わるため再取得）
                var redoneDocument = pdfEditorService.CurrentDocument!;
                redoneDocument.Pages[0].Rotation.Should().Be((originalRotation1 + 90) % 360, "Redo後、1ページ目は回転が再適用");
                redoneDocument.Pages[1].Rotation.Should().Be(originalRotation2, "2ページ目はまだ元の状態");
            });
        }
        finally
        {
            TestDataHelper.CleanupTempFile(testPdfPath);
        }
    }

    /// <summary>
    /// IT-009A: 初期状態でCanUndoがfalse
    /// PDF読み込み直後はUndoできないことを検証
    /// </summary>
    [StaFact]
    [Trait("Category", "Integration")]
    [Trait("Priority", "Medium")]
    public async Task IT009A_CanUndo_ShouldBeFalse_Initially()
    {
        // Arrange
        var fixture = new IntegrationTestFixture();
        var pdfEditorService = fixture.GetService<IPdfEditorService>();
        var testPdfPath = TestDataHelper.GenerateSamplePdf(3); // 3ページPDF生成

        try
        {
            await fixture.InvokeAsync(async () =>
            {
                // Act: PDF読み込み
                await pdfEditorService.OpenFileAsync(testPdfPath);
                var document = pdfEditorService.CurrentDocument!;

                // Assert: 初期状態ではCanUndoはfalse
                pdfEditorService.CanUndo.Should().BeFalse("PDF読み込み直後はUndoできないこと");
                pdfEditorService.CanRedo.Should().BeFalse("PDF読み込み直後はRedoできないこと");
            });
        }
        finally
        {
            TestDataHelper.CleanupTempFile(testPdfPath);
        }
    }

    /// <summary>
    /// IT-009A: 新規操作後にCanRedoがfalse
    /// Undo後に新規操作を実行するとRedoスタックがクリアされることを検証
    /// </summary>
    [StaFact]
    [Trait("Category", "Integration")]
    [Trait("Priority", "Medium")]
    public async Task IT009A_CanRedo_ShouldBeFalse_After_NewOperation()
    {
        // Arrange
        var fixture = new IntegrationTestFixture();
        var pdfEditorService = fixture.GetService<IPdfEditorService>();
        var testPdfPath = TestDataHelper.GenerateSamplePdf(5); // 5ページPDF生成

        try
        {
            await fixture.InvokeAsync(async () =>
            {
                await pdfEditorService.OpenFileAsync(testPdfPath);
                var document = pdfEditorService.CurrentDocument!;

                // Act: 操作→Undo→新規操作
                await pdfEditorService.RotatePagesAsync(new[] { 0 }, 90);  // 1ページ目を回転
                await pdfEditorService.UndoAsync();                        // Undo

                pdfEditorService.CanRedo.Should().BeTrue("Undo直後はRedoが可能");

                // 新規操作を実行
                await pdfEditorService.RotatePagesAsync(new[] { 1 }, 180); // 2ページ目を回転

                // Assert: 新規操作後はCanRedoがfalseになること
                pdfEditorService.CanRedo.Should().BeFalse("新規操作後はRedoスタックがクリアされること");
            });
        }
        finally
        {
            TestDataHelper.CleanupTempFile(testPdfPath);
        }
    }
}
