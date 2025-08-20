using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using DocOrganizer.Application.Interfaces.V3;
using Microsoft.Extensions.Logging;

namespace DocOrganizer.Infrastructure.Services.V3
{
    /// <summary>
    /// 🏗️ V3.0.009 統合画像読み込みサービス - プロバイダーアーキテクチャ完全対応
    /// 全画像形式対応・無限拡張可能・企業レベル品質
    /// </summary>
    public class ImageLoaderService : IImageLoaderService
    {
        private readonly IImageProcessingProviderManager _providerManager;
        private readonly ILogger<ImageLoaderService> _logger;

        public ImageLoaderService(
            IImageProcessingProviderManager providerManager,
            ILogger<ImageLoaderService> logger)
        {
            _providerManager = providerManager;
            _logger = logger;
        }

        /// <summary>
        /// 統一画像読み込み（プロバイダーによる形式別最適化）
        /// </summary>
        public async Task<ImageSource> LoadImageWithOrientationAsync(string filePath)
        {
            try
            {
                _logger.LogDebug("[V3_ImageLoader] 統一画像読み込み開始: {FileName}", Path.GetFileName(filePath));

                return await _providerManager.ProcessWithBestProvider(filePath, 
                    provider => provider.GeneratePreviewAsync(filePath));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_ImageLoader] 統一読み込みエラー: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// 高品質プレビュー用画像読み込み
        /// </summary>
        public async Task<ImageSource> LoadHighQualityImageAsync(string filePath, int maxWidth = 1920, int maxHeight = 1080)
        {
            try
            {
                _logger.LogDebug("[V3_ImageLoader] 高品質読み込み開始: {FileName}, サイズ上限: {MaxWidth}x{MaxHeight}", 
                    Path.GetFileName(filePath), maxWidth, maxHeight);

                return await _providerManager.ProcessWithBestProvider(filePath, 
                    provider => provider.GeneratePreviewAsync(filePath, maxWidth, maxHeight));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_ImageLoader] 高品質読み込みエラー: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// 画像情報取得
        /// </summary>
        public async Task<ImageInfo> GetImageInfoAsync(string filePath)
        {
            try
            {
                _logger.LogDebug("[V3_ImageLoader] 画像情報取得開始: {FileName}", Path.GetFileName(filePath));

                return await _providerManager.ProcessWithBestProvider(filePath, 
                    provider => provider.GetImageInfoAsync(filePath));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_ImageLoader] 画像情報取得エラー: {FilePath}", filePath);
                throw;
            }
        }
    }
}