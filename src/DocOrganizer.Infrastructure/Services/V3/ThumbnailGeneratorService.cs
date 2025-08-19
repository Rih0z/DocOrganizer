using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media; // For RotateTransform and TransformedBitmap
using DocOrganizer.Application.Interfaces.V3;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;

namespace DocOrganizer.Infrastructure.Services.V3
{
    /// <summary>
    /// 🎯 V3実装: OSS標準サムネイル生成サービス
    /// 技術: ImageSharp AutoOrient + WPFサムネイル分離
    /// 目標: 左右プレビューの完全独立生成
    /// </summary>
    public class ThumbnailGeneratorService : IThumbnailGeneratorService
    {
        private readonly ILogger<ThumbnailGeneratorService> _logger;
        private readonly IImageLoaderService _imageLoaderService;

        public ThumbnailGeneratorService(
            ILogger<ThumbnailGeneratorService> logger,
            IImageLoaderService imageLoaderService)
        {
            _logger = logger;
            _imageLoaderService = imageLoaderService;
        }

        /// <summary>
        /// 左側パネル用サムネイル生成（150x200固定）
        /// </summary>
        public async Task<ImageSource> GenerateLeftPanelThumbnailAsync(string filePath, int rotation = 0)
        {
            try
            {
                _logger.LogDebug("[V3_Thumbnail] 左パネル用サムネイル生成開始: {FileName}, 回転: {Rotation}度", Path.GetFileName(filePath), rotation);

                return await Task.Run(() =>
                {
                    // 🎯 ImageSharp AutoOrient使用
                    using var image = SixLabors.ImageSharp.Image.Load(filePath);
                    
                    // EXIF Orientation自動補正
                    image.Mutate(x => x.AutoOrient());
                    
                    // 🔧 回転適用（0, 90, 180, 270度）
                    if (rotation > 0)
                    {
                        image.Mutate(x => x.Rotate(rotation));
                    }
                    
                    // 150x200にリサイズ（アスペクト比保持）
                    var targetSize = CalculateResizeWithAspectRatio(image.Width, image.Height, 150, 200);
                    image.Mutate(x => x.Resize(targetSize.Width, targetSize.Height));

                    // WPFのBitmapImageに変換
                    return ConvertImageSharpToBitmapImage(image);
                });
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

                // 🎯 V3新アプローチ: ImageLoaderServiceを活用して画像を読み込み
                var imageSource = await _imageLoaderService.LoadHighQualityImageAsync(filePath, maxWidth, maxHeight);
                
                // 🔧 回転適用（WPFのTransformedBitmapを使用）
                if (rotation > 0 && imageSource is BitmapSource bitmapSource)
                {
                    var transform = new RotateTransform(rotation);
                    var rotatedBitmap = new TransformedBitmap(bitmapSource, transform);
                    rotatedBitmap.Freeze();
                    return rotatedBitmap;
                }
                
                return imageSource;
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
                    
                    // 抽出した画像をサムネイル化
                    using var image = SixLabors.ImageSharp.Image.Load(pageImagePath);
                    
                    var (width, height) = GetSizeForThumbnailType(thumbnailSize);
                    var targetSize = CalculateResizeWithAspectRatio(image.Width, image.Height, width, height);
                    
                    image.Mutate(x => x.Resize(targetSize.Width, targetSize.Height));
                    
                    return ConvertImageSharpToBitmapImage(image);
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
        
        private (int Width, int Height) CalculateResizeWithAspectRatio(int originalWidth, int originalHeight, int maxWidth, int maxHeight)
        {
            var ratioX = (double)maxWidth / originalWidth;
            var ratioY = (double)maxHeight / originalHeight;
            var ratio = Math.Min(ratioX, ratioY);

            return ((int)(originalWidth * ratio), (int)(originalHeight * ratio));
        }

        private (int Width, int Height) GetSizeForThumbnailType(ThumbnailSize thumbnailSize)
        {
            return thumbnailSize switch
            {
                ThumbnailSize.LeftPanel => (150, 200),
                ThumbnailSize.RightPreview => (1920, 1080),
                ThumbnailSize.PdfPreview => (300, 400),
                _ => throw new ArgumentException($"未対応のサムネイルサイズ: {thumbnailSize}")
            };
        }

        private BitmapImage ConvertImageSharpToBitmapImage(SixLabors.ImageSharp.Image image)
        {
            using var memoryStream = new MemoryStream();
            
            // PNG形式で出力（品質重視）
            image.Save(memoryStream, new PngEncoder());
            memoryStream.Position = 0;

            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = memoryStream;
            bitmapImage.EndInit();
            bitmapImage.Freeze();

            return bitmapImage;
        }

        private string ExtractPdfPageAsImage(string pdfFilePath, int pageIndex)
        {
            // 🎯 実装例: PDFページを画像として抽出
            // 実際の実装では、PdfSharpやiTextSharpなどを使用してPDFページを画像に変換
            
            var tempImagePath = Path.GetTempFileName() + ".png";
            
            // PDFページ抽出処理（実装が必要）
            // ここでは仮の実装
            
            return tempImagePath;
        }
    }
}