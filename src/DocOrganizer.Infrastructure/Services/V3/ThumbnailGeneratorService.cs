using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DocOrganizer.Application.Interfaces.V3;
using Microsoft.Extensions.Logging;

namespace DocOrganizer.Infrastructure.Services.V3
{
    /// <summary>
    /// 🏗️ V3.0.009 統合サムネイル生成サービス - プロバイダーアーキテクチャ完全対応
    /// 全画像形式対応・無限拡張可能・企業レベル品質
    /// </summary>
    public class ThumbnailGeneratorService : IThumbnailGeneratorService
    {
        private readonly IImageProcessingProviderManager _providerManager;
        private readonly ILogger<ThumbnailGeneratorService> _logger;

        public ThumbnailGeneratorService(
            IImageProcessingProviderManager providerManager,
            ILogger<ThumbnailGeneratorService> logger)
        {
            _providerManager = providerManager;
            _logger = logger;
        }

        /// <summary>
        /// 左側パネル用サムネイル生成（150x200固定）
        /// </summary>
        public async Task<ImageSource> GenerateLeftPanelThumbnailAsync(string filePath, int rotation = 0)
        {
            try
            {
                _logger.LogDebug("[V3_Thumbnail] 左パネル用サムネイル生成開始: {FileName}, 回転: {Rotation}度", 
                    Path.GetFileName(filePath), rotation);

                return await _providerManager.ProcessWithBestProvider(filePath, 
                    provider => provider.GenerateThumbnailAsync(filePath, ThumbnailSize.LeftPanel, rotation));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_Thumbnail] 左パネルサムネイル生成エラー: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// 右側プレビュー用高解像度画像生成
        /// </summary>
        public async Task<ImageSource> GenerateRightPreviewImageAsync(string filePath, int rotation = 0, int maxWidth = 1920, int maxHeight = 1080)
        {
            try
            {
                _logger.LogDebug("[V3_Thumbnail] 右プレビュー用高解像度生成開始: {FileName}, 回転: {Rotation}度, 上限: {MaxWidth}x{MaxHeight}", 
                    Path.GetFileName(filePath), rotation, maxWidth, maxHeight);

                // 🎯 V3.0.009 プロバイダーアーキテクチャによる統一処理
                var previewImage = await _providerManager.ProcessWithBestProvider(filePath, 
                    provider => provider.GeneratePreviewAsync(filePath, maxWidth, maxHeight));
                
                // 🔧 回転適用（WPFのTransformedBitmapを使用）
                if (rotation > 0 && previewImage is BitmapSource bitmapSource)
                {
                    var transform = new RotateTransform(rotation);
                    var rotatedBitmap = new TransformedBitmap(bitmapSource, transform);
                    rotatedBitmap.Freeze();
                    return rotatedBitmap;
                }
                
                return previewImage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_Thumbnail] 右プレビュー生成エラー: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// PDFページからサムネイル生成
        /// </summary>
        public async Task<ImageSource> GeneratePdfPageThumbnailAsync(string pdfFilePath, int pageIndex, ThumbnailSize thumbnailSize)
        {
            try
            {
                _logger.LogDebug("[V3_Thumbnail] PDFサムネイル生成開始: {FileName}, Page: {PageIndex}, Size: {Size}", 
                    Path.GetFileName(pdfFilePath), pageIndex, thumbnailSize);

                return await Task.Run(() =>
                {
                    // PDFSharp使用してページ画像を取得
                    // （実装例 - 実際の実装では適切なPDFライブラリを使用）
                    var pageImagePath = ExtractPdfPageAsImage(pdfFilePath, pageIndex);
                    
                    // 抽出した画像をプロバイダーでサムネイル化
                    return _providerManager.ProcessWithBestProvider(pageImagePath, 
                        provider => provider.GenerateThumbnailAsync(pageImagePath, thumbnailSize, 0)).Result;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_Thumbnail] PDFサムネイル生成エラー: {FilePath}, Page: {PageIndex}", pdfFilePath, pageIndex);
                throw;
            }
        }

        /// <summary>
        /// 一括サムネイル生成（パフォーマンス最適化）
        /// </summary>
        public async Task<ImageSource[]> GenerateBatchThumbnailsAsync(string[] filePaths, ThumbnailSize thumbnailSize)
        {
            try
            {
                _logger.LogDebug("[V3_Thumbnail] 一括サムネイル生成開始: {FileCount}件, Size: {Size}", filePaths.Length, thumbnailSize);

                var tasks = filePaths.Select(async filePath =>
                {
                    return thumbnailSize switch
                    {
                        ThumbnailSize.LeftPanel => await GenerateLeftPanelThumbnailAsync(filePath),
                        ThumbnailSize.RightPreview => await GenerateRightPreviewImageAsync(filePath),
                        ThumbnailSize.PdfPreview => await GeneratePdfPageThumbnailAsync(filePath, 0, thumbnailSize),
                        _ => throw new ArgumentException($"未対応のサムネイルサイズ: {thumbnailSize}")
                    };
                });

                var results = await Task.WhenAll(tasks);
                
                _logger.LogDebug("[V3_Thumbnail] 一括サムネイル生成完了: {SuccessCount}/{TotalCount}", 
                    results.Count(r => r != null), filePaths.Length);

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_Thumbnail] 一括サムネイル生成エラー");
                throw;
            }
        }

        // Private helper methods
        
        private string ExtractPdfPageAsImage(string pdfFilePath, int pageIndex)
        {
            // 🎯 実装例: PDF ページを画像として抽出
            // 実際の実装では、PdfSharp や iTextSharp などを使用してPDFページを画像に変換
            
            var tempImagePath = Path.GetTempFileName() + ".png";
            
            // PDF ページ抽出処理（実装が必要）
            // ここでは仮の実装
            
            return tempImagePath;
        }
    }
}