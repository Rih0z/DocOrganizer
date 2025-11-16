using System;
using System.IO;
using PdfSharp.Pdf;
using PdfSharp.Drawing;

namespace DocOrganizer.IntegrationTests.Helpers;

/// <summary>
/// 統合テスト用のテストデータ生成ヘルパー
/// </summary>
public static class TestDataHelper
{
    /// <summary>
    /// 指定ページ数のサンプルPDFを動的生成
    /// </summary>
    /// <param name="pageCount">生成するページ数</param>
    /// <returns>生成されたPDFファイルの一時パス</returns>
    public static string GenerateSamplePdf(int pageCount)
    {
        if (pageCount <= 0)
            throw new ArgumentException("Page count must be greater than 0", nameof(pageCount));

        var tempPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.pdf");

        using var document = new PdfDocument();
        for (int i = 0; i < pageCount; i++)
        {
            var page = document.AddPage();
            using var gfx = XGraphics.FromPdfPage(page);

            // ページ番号を描画
            var font = new XFont("Arial", 20);
            gfx.DrawString($"Page {i + 1}", font, XBrushes.Black, 100, 100);

            // テストデータとして識別用の情報を追加
            var smallFont = new XFont("Arial", 10);
            gfx.DrawString($"Test PDF - Generated at {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                smallFont, XBrushes.Gray, 100, 150);
        }

        document.Save(tempPath);
        return tempPath;
    }

    /// <summary>
    /// 指定ページ数のサンプルPDFを生成（カスタムテキスト）
    /// </summary>
    /// <param name="pageCount">生成するページ数</param>
    /// <param name="pageTexts">各ページのカスタムテキスト（省略可）</param>
    /// <returns>生成されたPDFファイルの一時パス</returns>
    public static string GenerateSamplePdfWithText(int pageCount, string[]? pageTexts = null)
    {
        if (pageCount <= 0)
            throw new ArgumentException("Page count must be greater than 0", nameof(pageCount));

        if (pageTexts != null && pageTexts.Length != pageCount)
            throw new ArgumentException("pageTexts length must match pageCount", nameof(pageTexts));

        var tempPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.pdf");

        using var document = new PdfDocument();
        for (int i = 0; i < pageCount; i++)
        {
            var page = document.AddPage();
            using var gfx = XGraphics.FromPdfPage(page);

            var font = new XFont("Arial", 20);
            var text = pageTexts?[i] ?? $"Page {i + 1}";
            gfx.DrawString(text, font, XBrushes.Black, 100, 100);
        }

        document.Save(tempPath);
        return tempPath;
    }

    /// <summary>
    /// 破損したPDFファイルを生成（エラーハンドリングテスト用）
    /// </summary>
    /// <returns>生成された破損PDFファイルの一時パス</returns>
    public static string GenerateCorruptedPdf()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"corrupted_{Guid.NewGuid()}.pdf");

        // 破損PDFを生成（PDFヘッダーのみで不完全なファイル）
        File.WriteAllText(tempPath, "%PDF-1.4\n%%EOF");

        return tempPath;
    }

    /// <summary>
    /// テスト終了後の一時ファイルクリーンアップ
    /// </summary>
    /// <param name="filePath">削除する一時ファイルパス</param>
    public static void CleanupTempFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
            }
            catch
            {
                // テスト失敗時にファイルロックされている可能性があるため、
                // クリーンアップ失敗は無視（OSの一時ファイル削除に任せる）
            }
        }
    }
}
