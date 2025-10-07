using System.Threading.Tasks;
using System.Windows.Media;

namespace DocOrganizer.Application.Interfaces.V3
{
    /// <summary>
    /// 🎯 V3新インターフェース: OSS標準サムネイル生成サービス
    /// 責務: ImageSharp AutoOrient活用による高速サムネイル生成
    /// 目標: 左側150x200・右側高解像度の完全分離
    /// </summary>
    public interface IThumbnailGeneratorService
    {
        /// <summary>
        /// 左側パネル用サムネイル生成（150x200固定）
        /// </summary>
        /// <param name="filePath">画像ファイルパス</param>
        /// <param name="rotation">回転角度（0, 90, 180, 270）</param>
        /// <returns>150x200サイズのサムネイル</returns>
        Task<ImageSource> GenerateLeftPanelThumbnailAsync(string filePath, int rotation = 0);

        /// <summary>
        /// 右側プレビュー用高解像度画像生成
        /// </summary>
        /// <param name="filePath">画像ファイルパス</param>
        /// <param name="rotation">回転角度（0, 90, 180, 270）</param>
        /// <param name="maxWidth">最大幅（デフォルト: 1920）</param>
        /// <param name="maxHeight">最大高さ（デフォルト: 1080）</param>
        /// <param name="enableAutoCrop">余白自動削除を有効化（デフォルト: false）</param>
        /// <returns>高解像度プレビュー画像</returns>
        Task<ImageSource> GenerateRightPreviewImageAsync(string filePath, int rotation = 0, int maxWidth = 1920, int maxHeight = 1080, bool enableAutoCrop = false);

        /// <summary>
        /// PDFページからサムネイル生成
        /// </summary>
        /// <param name="pdfFilePath">PDFファイルパス</param>
        /// <param name="pageIndex">ページインデックス</param>
        /// <param name="thumbnailSize">サムネイルサイズ</param>
        /// <returns>PDFページサムネイル</returns>
        Task<ImageSource> GeneratePdfPageThumbnailAsync(string pdfFilePath, int pageIndex, ThumbnailSize thumbnailSize);

        /// <summary>
        /// 一括サムネイル生成（パフォーマンス最適化）
        /// </summary>
        /// <param name="filePaths">画像ファイルパス配列</param>
        /// <param name="thumbnailSize">サムネイルサイズ</param>
        /// <returns>サムネイル配列</returns>
        Task<ImageSource[]> GenerateBatchThumbnailsAsync(string[] filePaths, ThumbnailSize thumbnailSize);
    }
}