using System.Threading.Tasks;

namespace DocOrganizer.Application.Interfaces.V3
{
    /// <summary>
    /// 🏗️ 企業レベル拡張アーキテクチャ: プロバイダー管理システム
    /// 責務: 形式別プロバイダーの動的選択・管理
    /// 設計: Manager Pattern による最適プロバイダー選択
    /// 参考: .NET Service Provider, OSS Plugin Manager
    /// </summary>
    public interface IImageValidationProviderManager
    {
        /// <summary>
        /// 拡張子に最適なプロバイダー取得
        /// </summary>
        /// <param name="extension">ファイル拡張子（例: ".heic"）</param>
        /// <returns>最適プロバイダー</returns>
        IImageValidationProvider GetProvider(string extension);

        /// <summary>
        /// プロバイダー登録（DI自動登録対象）
        /// </summary>
        /// <param name="provider">プロバイダーインスタンス</param>
        void RegisterProvider(IImageValidationProvider provider);

        /// <summary>
        /// 全プロバイダー取得（診断・ログ用）
        /// </summary>
        /// <returns>全プロバイダー配列</returns>
        IImageValidationProvider[] GetAllProviders();

        /// <summary>
        /// 最適プロバイダーによる検証実行
        /// </summary>
        /// <param name="filePath">画像ファイルパス</param>
        /// <returns>検証結果</returns>
        Task<ImageValidationResult> ValidateWithBestProvider(string filePath);
    }
}