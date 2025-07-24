using System.Collections.Generic;
using System.Threading.Tasks;
using SkiaSharp;
using DocOrganizer.Core.Models;

namespace DocOrganizer.Application.Interfaces
{
    /// <summary>
    /// PDF操作を抽象化するインターフェース
    /// </summary>
    public interface IPdfService
    {
        /// <summary>
        /// PDFファイルを読み込みます
        /// </summary>
        /// <param name="filePath">PDFファイルのパス</param>
        /// <returns>読み込んだPDF文書</returns>
        Task<PdfDocument> LoadPdfAsync(string filePath);

        /// <summary>
        /// PDF文書を保存します
        /// </summary>
        /// <param name="document">保存するPDF文書</param>
        /// <param name="filePath">保存先のファイルパス</param>
        Task SavePdfAsync(PdfDocument document, string filePath);

        /// <summary>
        /// 複数のPDF文書を結合します
        /// </summary>
        /// <param name="documents">結合するPDF文書の配列</param>
        /// <returns>結合されたPDF文書</returns>
        Task<PdfDocument> MergePdfsAsync(params PdfDocument[] documents);

        /// <summary>
        /// PDF文書を分割します
        /// </summary>
        /// <param name="document">分割するPDF文書</param>
        /// <param name="pageRanges">ページ範囲の配列（開始ページ、終了ページ）</param>
        /// <returns>分割されたPDF文書の配列</returns>
        Task<PdfDocument[]> SplitPdfAsync(PdfDocument document, params (int startPage, int endPage)[] pageRanges);

        /// <summary>
        /// ページのサムネイル画像を抽出します
        /// </summary>
        /// <param name="document">PDF文書</param>
        /// <param name="pageIndex">ページインデックス（0から始まる）</param>
        /// <param name="maxWidth">サムネイルの最大幅</param>
        /// <returns>サムネイル画像</returns>
        Task<SKBitmap> ExtractPageThumbnailAsync(PdfDocument document, int pageIndex, int maxWidth);

        /// <summary>
        /// ページのプレビュー画像を抽出します
        /// </summary>
        /// <param name="document">PDF文書</param>
        /// <param name="pageIndex">ページインデックス（0から始まる）</param>
        /// <param name="scale">スケール（1.0 = 100%）</param>
        /// <returns>プレビュー画像</returns>
        Task<SKBitmap> ExtractPagePreviewAsync(PdfDocument document, int pageIndex, float scale);

        /// <summary>
        /// PDFファイルのページ数を取得します
        /// </summary>
        /// <param name="filePath">PDFファイルのパス</param>
        /// <returns>ページ数</returns>
        int GetPageCount(string filePath);

        /// <summary>
        /// PDFファイルのサイズを取得します
        /// </summary>
        /// <param name="filePath">PDFファイルのパス</param>
        /// <returns>ファイルサイズ（バイト）</returns>
        long GetFileSize(string filePath);

        /// <summary>
        /// 有効なPDFファイルかどうかを確認します
        /// </summary>
        /// <param name="filePath">確認するファイルのパス</param>
        /// <returns>有効なPDFファイルの場合はtrue</returns>
        bool IsValidPdf(string filePath);

        /// <summary>
        /// 画像ファイルからPDFを作成します
        /// </summary>
        /// <param name="imagePath">画像ファイルのパス</param>
        /// <param name="outputPath">出力PDFファイルのパス</param>
        /// <returns>作成されたPDF文書</returns>
        Task<PdfDocument> CreatePdfFromImageAsync(string imagePath, string outputPath);

        /// <summary>
        /// 複数の画像ファイルから1つのPDFを作成します
        /// </summary>
        /// <param name="imagePaths">画像ファイルのパスの列挙</param>
        /// <param name="outputPath">出力PDFファイルのパス</param>
        /// <returns>作成されたPDF文書</returns>
        Task<PdfDocument> CreatePdfFromImagesAsync(IEnumerable<string> imagePaths, string outputPath);

        /// <summary>
        /// SkiaSharpを使用して画像からPDFを作成（ImageSharp依存を回避）
        /// </summary>
        /// <param name="imagePath">画像ファイルのパス</param>
        /// <param name="outputPath">出力PDFファイルのパス</param>
        /// <returns>作成されたPDF文書</returns>
        Task<PdfDocument> CreatePdfFromImageSkiaSharpAsync(string imagePath, string outputPath);
    }
}