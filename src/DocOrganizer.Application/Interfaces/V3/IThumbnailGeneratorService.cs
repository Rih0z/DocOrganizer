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
        /// 🚀 V3.0.143: キャッシュされたSKBitmapをメモリ上で回転
        /// ディスクI/O不要で超高速（GPU不要、全環境で同じ速度）
        /// </summary>
        /// <param name="source">元のSKBitmap</param>
        /// <param name="degrees">回転角度（90, 180, 270）</param>
        /// <returns>回転済みSKBitmap、失敗時はnull</returns>
        SkiaSharp.SKBitmap? RotateCachedBitmap(SkiaSharp.SKBitmap source, int degrees);

        /// <summary>
        /// 🚀 V3.0.143: キャッシュされたSKBitmapから回転済みBitmapSourceを生成
        /// rotatedBitmapをout引数で返し、呼び出し側でPdfPage.SetThumbnailImageに設定
        /// </summary>
        /// <param name="cachedBitmap">キャッシュされたSKBitmap</param>
        /// <param name="rotation">回転角度</param>
        /// <param name="rotatedBitmap">回転済みSKBitmap（呼び出し側がSetThumbnailImageで設定）</param>
        /// <returns>表示用BitmapSource、失敗時はnull</returns>
        System.Windows.Media.Imaging.BitmapSource? GenerateBitmapSourceFromCache(SkiaSharp.SKBitmap cachedBitmap, int rotation, out SkiaSharp.SKBitmap? rotatedBitmap);

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