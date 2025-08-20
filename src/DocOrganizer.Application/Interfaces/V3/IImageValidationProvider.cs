using System.Threading.Tasks;

namespace DocOrganizer.Application.Interfaces.V3
{
    /// <summary>
    /// 🏗️ 企業レベル拡張アーキテクチャ: 画像検証プロバイダー
    /// 責務: 形式別最適化された画像検証処理
    /// 設計: Strategy Pattern + Provider Pattern による拡張可能アーキテクチャ
    /// 参考: ImageSharp IImageProcessor, GIMP Plugin Architecture, FFmpeg Provider System
    /// </summary>
    public interface IImageValidationProvider
    {
        /// <summary>
        /// 画像検証処理（形式特化）
        /// </summary>
        /// <param name="filePath">画像ファイルパス</param>
        /// <returns>検証結果</returns>
        Task<ImageValidationResult> ValidateAsync(string filePath);

        /// <summary>
        /// 形式サポート判定
        /// </summary>
        /// <param name="extension">ファイル拡張子（例: ".heic"）</param>
        /// <returns>サポート可否</returns>
        bool SupportsFormat(string extension);

        /// <summary>
        /// サポート対象拡張子一覧
        /// </summary>
        string[] SupportedExtensions { get; }

        /// <summary>
        /// プロバイダー優先度（高い順に選択）
        /// HEIC: 100, GIF: 90, Standard: 80
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// プロバイダー識別名（ログ・診断用）
        /// </summary>
        string ProviderName { get; }
    }
}