using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace DocOrganizer.Tests.TestHelpers
{
    /// <summary>
    /// テスト用データ生成ヘルパークラス
    /// </summary>
    public static class TestDataGenerator
    {
        private static readonly string TempDir = Path.Combine(Path.GetTempPath(), "DocOrganizerTests");

        static TestDataGenerator()
        {
            Directory.CreateDirectory(TempDir);
        }

        /// <summary>
        /// PDFSharpを使用してテスト用PDFを動的生成
        /// </summary>
        /// <param name="pageCount">ページ数</param>
        /// <param name="fileName">ファイル名（省略時は自動生成）</param>
        /// <returns>生成されたPDFファイルのパス</returns>
        public static string GeneratePdf(int pageCount, string? fileName = null)
        {
            fileName ??= $"test_{pageCount}pages_{Guid.NewGuid():N}.pdf";
            var outputPath = Path.Combine(TempDir, fileName);

            using var document = new PdfDocument();

            for (int i = 0; i < pageCount; i++)
            {
                var page = document.AddPage();
                page.Size = PdfSharp.PageSize.A4;

                using var gfx = XGraphics.FromPdfPage(page);
                var font = new XFont("Arial", 20, XFontStyleEx.Regular);

                gfx.DrawString(
                    $"Page {i + 1}",
                    font,
                    XBrushes.Black,
                    new XRect(0, 0, page.Width.Point, page.Height.Point),
                    XStringFormats.Center);
            }

            document.Save(outputPath);
            return outputPath;
        }

        /// <summary>
        /// 破損PDFを生成（4種類のバリエーション）
        /// </summary>
        /// <param name="type">破損タイプ</param>
        /// <returns>生成された破損PDFファイルのパス</returns>
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

        /// <summary>
        /// ランダムなページ数のPDFを生成
        /// </summary>
        /// <param name="random">乱数生成器（省略時は新規作成）</param>
        /// <returns>生成されたPDFファイルのパス</returns>
        public static string GenerateRandomPdf(Random? random = null)
        {
            random ??= new Random();
            var pageCount = random.Next(1, 51); // 1～50ページ
            return GeneratePdf(pageCount);
        }

        /// <summary>
        /// テスト用画像を生成
        /// </summary>
        /// <param name="format">画像フォーマット</param>
        /// <param name="width">幅（デフォルト: 800px）</param>
        /// <param name="height">高さ（デフォルト: 600px）</param>
        /// <returns>生成された画像ファイルのパス</returns>
        public static string GenerateImage(System.Drawing.Imaging.ImageFormat format, int width = 800, int height = 600)
        {
            var extension = format.Equals(System.Drawing.Imaging.ImageFormat.Jpeg) ? "jpg" :
                          format.Equals(System.Drawing.Imaging.ImageFormat.Png) ? "png" :
                          "jpg";

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

        /// <summary>
        /// 一時ファイルをクリーンアップ
        /// </summary>
        public static void CleanupTempFiles()
        {
            if (Directory.Exists(TempDir))
            {
                Directory.Delete(TempDir, recursive: true);
                Directory.CreateDirectory(TempDir);
            }
        }
    }

    /// <summary>
    /// PDF破損タイプ
    /// </summary>
    public enum CorruptionType
    {
        /// <summary>ファイルが途中で切断されている</summary>
        TruncatedFile,

        /// <summary>PDFヘッダーが不正</summary>
        InvalidHeader,

        /// <summary>xrefテーブルが欠落</summary>
        MissingXref,

        /// <summary>オブジェクト定義が不正</summary>
        InvalidObjects
    }
}
