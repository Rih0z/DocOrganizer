using System.Threading.Tasks;
using DocOrganizer.Core.Models;

namespace DocOrganizer.Application.Interfaces
{
    /// <summary>
    /// PDF編集操作を提供するサービスインターフェース
    /// </summary>
    public interface IPdfEditorService
    {
        /// <summary>
        /// 現在編集中のPDF文書
        /// </summary>
        PdfDocument? CurrentDocument { get; }

        /// <summary>
        /// PDFファイルを開きます
        /// </summary>
        /// <param name="filePath">開くPDFファイルのパス</param>
        Task OpenFileAsync(string filePath);

        /// <summary>
        /// 現在のPDF文書を保存します
        /// </summary>
        Task SaveAsync();

        /// <summary>
        /// 現在のPDF文書を別名で保存します
        /// </summary>
        /// <param name="filePath">保存先のファイルパス</param>
        Task SaveAsAsync(string filePath);

        /// <summary>
        /// 現在のPDF文書を閉じます
        /// </summary>
        void CloseDocument();

        /// <summary>
        /// 選択されたページを削除します
        /// </summary>
        /// <param name="pageIndices">削除するページのインデックス配列</param>
        Task RemovePagesAsync(int[] pageIndices);

        /// <summary>
        /// 選択されたページを回転します
        /// </summary>
        /// <param name="pageIndices">回転するページのインデックス配列</param>
        /// <param name="degrees">回転角度（90, 180, 270）</param>
        Task RotatePagesAsync(int[] pageIndices, int degrees);

        /// <summary>
        /// ページを並び替えます
        /// </summary>
        /// <param name="fromIndex">移動元のインデックス</param>
        /// <param name="toIndex">移動先のインデックス</param>
        Task ReorderPagesAsync(int fromIndex, int toIndex);

        /// <summary>
        /// 現在の文書に別のPDFを結合します
        /// </summary>
        /// <param name="filePaths">結合するPDFファイルのパス配列</param>
        Task MergeWithAsync(string[] filePaths);

        /// <summary>
        /// 現在の文書を分割します
        /// </summary>
        /// <param name="splitPoints">分割ポイント（ページ番号）の配列</param>
        /// <param name="outputDirectory">出力ディレクトリ</param>
        /// <param name="fileNamePattern">ファイル名パターン（{0}はインデックス）</param>
        Task SplitDocumentAsync(int[] splitPoints, string outputDirectory, string fileNamePattern);

        /// <summary>
        /// ページのサムネイルを更新します
        /// </summary>
        /// <param name="pageIndex">更新するページのインデックス</param>
        Task UpdatePageThumbnailAsync(int pageIndex);

        /// <summary>
        /// すべてのページのサムネイルを更新します
        /// </summary>
        Task UpdateAllThumbnailsAsync();

        /// <summary>
        /// 元に戻す操作が可能かどうか
        /// </summary>
        bool CanUndo { get; }

        /// <summary>
        /// やり直し操作が可能かどうか
        /// </summary>
        bool CanRedo { get; }

        /// <summary>
        /// 元に戻す
        /// </summary>
        Task UndoAsync();

        /// <summary>
        /// やり直し
        /// </summary>
        Task RedoAsync();

        /// <summary>
        /// PDFファイルを開いてドキュメントを返します
        /// </summary>
        /// <param name="filePath">開くPDFファイルのパス</param>
        /// <returns>開いたPDFドキュメント</returns>
        Task<PdfDocument> OpenPdfAsync(string filePath);

        /// <summary>
        /// PDFを指定したパスに保存します
        /// </summary>
        /// <param name="document">保存するPDFドキュメント</param>
        /// <param name="filePath">保存先のファイルパス</param>
        /// <returns>保存結果</returns>
        Task<bool> SavePdfAsync(PdfDocument document, string filePath);

        /// <summary>
        /// ページを回転します
        /// </summary>
        /// <param name="page">対象のページ</param>
        /// <param name="degrees">回転角度</param>
        void RotatePage(PdfPage page, int degrees);

        /// <summary>
        /// ページを削除します
        /// </summary>
        /// <param name="document">対象のドキュメント</param>
        /// <param name="pageIndex">削除するページのインデックス</param>
        void RemovePage(PdfDocument document, int pageIndex);

        /// <summary>
        /// 複数のPDFを結合します
        /// </summary>
        /// <param name="filePaths">結合するPDFファイルのパス配列</param>
        /// <returns>結合されたPDFドキュメント</returns>
        Task<PdfDocument> MergePdfsAsync(string[] filePaths);

        /// <summary>
        /// ページを並び替えます
        /// </summary>
        /// <param name="document">対象のドキュメント</param>
        /// <param name="newOrder">新しいページ順序の配列</param>
        void ReorderPages(PdfDocument document, PdfPage[] newOrder);

        /// <summary>
        /// ページのプレビュー画像を取得します
        /// </summary>
        /// <param name="document">対象のドキュメント</param>
        /// <param name="pageIndex">ページインデックス</param>
        /// <param name="scale">拡大率</param>
        /// <returns>プレビュー画像</returns>
        Task<SkiaSharp.SKBitmap> GetPagePreviewAsync(PdfDocument document, int pageIndex, float scale);
    }
}