using System.Collections.Generic;
using System.Threading.Tasks;

namespace DocOrganizer.Application.Interfaces.V3
{
    /// <summary>
    /// 🎯 V3新インターフェース: OSS標準画像検証サービス
    /// 責務: 画像ファイルの検証・修復・品質チェック専用
    /// 目標: 0バイトファイル等の問題の事前検出・修復
    /// </summary>
    public interface IImageValidationService
    {
        /// <summary>
        /// 画像ファイルの基本検証
        /// </summary>
        /// <param name="filePath">画像ファイルパス</param>
        /// <returns>検証結果</returns>
        Task<ImageValidationResult> ValidateImageAsync(string filePath);

        /// <summary>
        /// 画像ファイルの修復試行
        /// </summary>
        /// <param name="filePath">破損画像ファイルパス</param>
        /// <returns>修復結果</returns>
        Task<ImageRepairResult> RepairImageAsync(string filePath);

        /// <summary>
        /// 一括画像検証
        /// </summary>
        /// <param name="filePaths">画像ファイルパス配列</param>
        /// <returns>検証結果配列</returns>
        Task<ImageValidationResult[]> ValidateBatchAsync(string[] filePaths);

        /// <summary>
        /// 対応形式判定
        /// </summary>
        /// <param name="filePath">ファイルパス</param>
        /// <returns>対応形式可否</returns>
        bool IsSupportedImageFormat(string filePath);

        /// <summary>
        /// 画像品質評価
        /// </summary>
        /// <param name="filePath">画像ファイルパス</param>
        /// <returns>品質評価結果</returns>
        Task<ImageQualityAssessment> AssessImageQualityAsync(string filePath);

        /// <summary>
        /// 無効ファイル除去
        /// </summary>
        /// <param name="filePaths">ファイルパス配列</param>
        /// <returns>有効ファイルパス配列</returns>
        Task<string[]> FilterValidImagesAsync(string[] filePaths);
    }

    /// <summary>
    /// 画像検証結果
    /// </summary>
    public record ImageValidationResult(
        string FilePath,
        bool IsValid,
        bool IsCorrupted,
        bool IsZeroBytes,
        long FileSize,
        string Format,
        int Width,
        int Height,
        List<string> Issues,
        string? ErrorMessage);

    /// <summary>
    /// 画像修復結果
    /// </summary>
    public record ImageRepairResult(
        string OriginalPath,
        string? RepairedPath,
        bool RepairSuccessful,
        List<string> RepairActions,
        string? ErrorMessage);

    /// <summary>
    /// 画像品質評価
    /// </summary>
    public record ImageQualityAssessment(
        string FilePath,
        ImageQualityLevel QualityLevel,
        double Resolution,
        double CompressionRatio,
        bool HasArtifacts,
        List<string> QualityIssues,
        Dictionary<string, object> Metrics);

    /// <summary>
    /// 画像品質レベル
    /// </summary>
    public enum ImageQualityLevel
    {
        Poor,
        Fair,
        Good,
        Excellent
    }
}