using System;
using System.Threading.Tasks;
using System.Windows.Media;
using DocOrganizer.Application.Interfaces.V3;

namespace DocOrganizer.Application.Interfaces.V3
{
    /// <summary>
    /// 🏗️ V3.0.009 究極拡張可能アーキテクチャ: 統一画像処理プロバイダー
    /// OSS標準Strategy Pattern準拠 - 無限に形式追加可能
    /// </summary>
    public interface IImageProcessingProvider
    {
        /// <summary>
        /// 画像検証（ドラッグ&ドロップ時の検証）
        /// </summary>
        Task<ImageValidationResult> ValidateAsync(string filePath);
        
        /// <summary>
        /// サムネイル生成（左パネル、右プレビュー、PDF対応）
        /// </summary>
        Task<ImageSource> GenerateThumbnailAsync(string filePath, ThumbnailSize size, int rotation = 0);
        
        /// <summary>
        /// プレビュー画像生成（高解像度表示用）
        /// </summary>
        Task<ImageSource> GeneratePreviewAsync(string filePath, int maxWidth = 1920, int maxHeight = 1080);
        
        /// <summary>
        /// 画像情報取得（サイズ、形式、EXIF等）
        /// </summary>
        Task<ImageInfo> GetImageInfoAsync(string filePath);
        
        /// <summary>
        /// プロバイダー情報
        /// </summary>
        bool SupportsFormat(string extension);
        string[] SupportedExtensions { get; }
        int Priority { get; }
        string ProviderName { get; }
    }
    
    /// <summary>
    /// プロバイダー動的管理 - 無限拡張対応
    /// </summary>
    public interface IImageProcessingProviderManager
    {
        IImageProcessingProvider GetProvider(string extension);
        void RegisterProvider(IImageProcessingProvider provider);
        IImageProcessingProvider[] GetAllProviders();
        Task<T> ProcessWithBestProvider<T>(string filePath, Func<IImageProcessingProvider, Task<T>> processor);
    }
    
    /// <summary>
    /// サムネイルサイズ統一定義
    /// </summary>
    public enum ThumbnailSize
    {
        LeftPanel,     // 左パネル用 150x200
        RightPreview,  // 右プレビュー用 1920x1080
        PdfPreview     // PDF用 300x400
    }
}