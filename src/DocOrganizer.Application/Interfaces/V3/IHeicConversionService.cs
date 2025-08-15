using System.Threading.Tasks;

namespace DocOrganizer.Application.Interfaces.V3
{
    /// <summary>
    /// 🎯 V3新インターフェース: OSS標準HEIC変換サービス
    /// 責務: Magick.NET特化による高速HEIC変換
    /// 目標: HEIC回転編集バグの根本解決
    /// </summary>
    public interface IHeicConversionService
    {
        /// <summary>
        /// HEICからJPEGに変換
        /// </summary>
        /// <param name="heicFilePath">HEICファイルパス</param>
        /// <param name="jpegOutputPath">JPEG出力パス</param>
        /// <param name="quality">JPEG品質（1-100）</param>
        /// <returns>変換成功可否</returns>
        Task<bool> ConvertHeicToJpegAsync(string heicFilePath, string jpegOutputPath, int quality = 90);

        /// <summary>
        /// HEICからPNGに変換
        /// </summary>
        /// <param name="heicFilePath">HEICファイルパス</param>
        /// <param name="pngOutputPath">PNG出力パス</param>
        /// <returns>変換成功可否</returns>
        Task<bool> ConvertHeicToPngAsync(string heicFilePath, string pngOutputPath);

        /// <summary>
        /// HEIC一時JPEG変換（回転編集用）
        /// </summary>
        /// <param name="heicFilePath">HEICファイルパス</param>
        /// <returns>一時JPEG変換パス</returns>
        Task<string> ConvertHeicToTempJpegAsync(string heicFilePath);

        /// <summary>
        /// HEIC情報取得
        /// </summary>
        /// <param name="heicFilePath">HEICファイルパス</param>
        /// <returns>HEIC詳細情報</returns>
        Task<HeicImageInfo> GetHeicInfoAsync(string heicFilePath);

        /// <summary>
        /// HEIC対応判定
        /// </summary>
        /// <param name="filePath">ファイルパス</param>
        /// <returns>HEIC形式可否</returns>
        bool IsHeicFile(string filePath);

        /// <summary>
        /// 一括HEIC変換
        /// </summary>
        /// <param name="heicFiles">HEICファイル配列</param>
        /// <param name="outputFormat">出力形式</param>
        /// <returns>変換結果配列</returns>
        Task<HeicConversionResult[]> ConvertHeicBatchAsync(string[] heicFiles, HeicOutputFormat outputFormat);
    }

    /// <summary>
    /// HEIC画像情報
    /// </summary>
    public record HeicImageInfo(
        int Width,
        int Height,
        long FileSize,
        string Format,
        bool HasExifOrientation,
        ushort ExifOrientation);

    /// <summary>
    /// HEIC変換結果
    /// </summary>
    public record HeicConversionResult(
        string OriginalPath,
        string ConvertedPath,
        bool Success,
        string? ErrorMessage);

    /// <summary>
    /// HEIC出力形式
    /// </summary>
    public enum HeicOutputFormat
    {
        Jpeg,
        Png
    }
}