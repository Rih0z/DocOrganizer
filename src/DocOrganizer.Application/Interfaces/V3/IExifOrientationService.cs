using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace DocOrganizer.Application.Interfaces.V3
{
    /// <summary>
    /// 🎯 V3新インターフェース: OSS標準EXIF Orientation処理サービス
    /// 責務: WPF標準API活用による正確なEXIF読み取り・処理
    /// 目標: Windows Photo/Paint完全互換の実現
    /// </summary>
    public interface IExifOrientationService
    {
        /// <summary>
        /// EXIF Orientation値を取得
        /// </summary>
        /// <param name="filePath">画像ファイルパス</param>
        /// <returns>EXIF Orientation値（1-8）</returns>
        Task<ushort> GetExifOrientationAsync(string filePath);

        /// <summary>
        /// EXIF OrientationからWPF Rotationに変換
        /// </summary>
        /// <param name="exifOrientation">EXIF Orientation値</param>
        /// <returns>WPF Rotation列挙値</returns>
        Rotation ConvertExifToWpfRotation(ushort exifOrientation);

        /// <summary>
        /// 画像ファイルにEXIF Orientationを設定
        /// </summary>
        /// <param name="filePath">画像ファイルパス</param>
        /// <param name="orientation">EXIF Orientation値</param>
        Task SetExifOrientationAsync(string filePath, ushort orientation);

        /// <summary>
        /// EXIF Orientationを正規化（常に1にリセット）
        /// </summary>
        /// <param name="filePath">画像ファイルパス</param>
        /// <param name="rotation">適用した回転角度</param>
        Task NormalizeExifOrientationAsync(string filePath, Rotation rotation);

        /// <summary>
        /// EXIF Orientation情報を詳細取得
        /// </summary>
        /// <param name="filePath">画像ファイルパス</param>
        /// <returns>EXIF Orientation詳細情報</returns>
        Task<ExifOrientationInfo> GetExifOrientationInfoAsync(string filePath);

        /// <summary>
        /// Windows Photo/Paint互換性チェック
        /// </summary>
        /// <param name="filePath">画像ファイルパス</param>
        /// <returns>互換性検証結果</returns>
        Task<bool> ValidateWindowsCompatibilityAsync(string filePath);
    }

    /// <summary>
    /// EXIF Orientation詳細情報
    /// </summary>
    public record ExifOrientationInfo(
        ushort OrientationValue,
        Rotation RequiredRotation,
        bool IsFlipped,
        string Description,
        bool IsWindowsCompatible);
}