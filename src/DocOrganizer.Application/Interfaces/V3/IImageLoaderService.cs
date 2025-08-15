using System.Threading.Tasks;
using System.Windows.Media;

namespace DocOrganizer.Application.Interfaces.V3
{
    /// <summary>
    /// 🎯 V3新インターフェース: OSS標準画像読み込みサービス
    /// 責務: BitmapImage.Rotation使用による標準EXIF処理
    /// 目標: WPF標準機能活用、Stack Overflow実証済みパターン採用
    /// </summary>
    public interface IImageLoaderService
    {
        /// <summary>
        /// OSS標準パターンによる画像読み込み（EXIF Orientation自動適用）
        /// </summary>
        /// <param name="filePath">画像ファイルパス</param>
        /// <returns>EXIF Orientationが適用されたBitmapSource</returns>
        Task<ImageSource> LoadImageWithOrientationAsync(string filePath);

        /// <summary>
        /// 高品質プレビュー用画像読み込み
        /// </summary>
        /// <param name="filePath">画像ファイルパス</param>
        /// <param name="maxWidth">最大幅</param>
        /// <param name="maxHeight">最大高さ</param>
        /// <returns>品質最適化されたBitmapSource</returns>
        Task<ImageSource> LoadHighQualityImageAsync(string filePath, int maxWidth = 1920, int maxHeight = 1080);

        /// <summary>
        /// 画像の基本情報取得
        /// </summary>
        /// <param name="filePath">画像ファイルパス</param>
        /// <returns>幅、高さ、EXIF Orientation情報</returns>
        Task<ImageInfo> GetImageInfoAsync(string filePath);
    }

    /// <summary>
    /// 画像情報
    /// </summary>
    public record ImageInfo(
        int Width,
        int Height,
        System.Windows.Media.Imaging.Rotation EXIFRotation,
        long FileSize,
        string Format);
}