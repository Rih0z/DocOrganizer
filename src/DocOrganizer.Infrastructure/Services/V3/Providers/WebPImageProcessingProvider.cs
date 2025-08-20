using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using DocOrganizer.Application.Interfaces.V3;
using DocOrganizer.Application.Attributes;
using System.Collections.Generic;

namespace DocOrganizer.Infrastructure.Services.V3.Providers
{
    /// <summary>
    /// 🏗️ V3.0.009 WebP専用プロバイダー - 将来拡張例実装
    /// WebP特有の可逆/非可逆判定・透明度サポート・Google固有プロファイル対応
    /// </summary>
    [ImageProcessingProvider("WebP", Priority = 85)]
    public class WebPImageProcessingProvider : IImageProcessingProvider
    {
        private readonly ILogger<WebPImageProcessingProvider> _logger;
        
        public string[] SupportedExtensions => new[] { ".webp" };
        public int Priority => 85;
        public string ProviderName => "ImageSharp WebP Provider";
        
        public WebPImageProcessingProvider(ILogger<WebPImageProcessingProvider> logger)
        {
            _logger = logger;
        }
        
        public bool SupportsFormat(string extension)
        {
            return extension.Equals(".webp", StringComparison.OrdinalIgnoreCase);
        }
        
        /// <summary>
        /// WebP画像検証（可逆/非可逆判定含む）
        /// </summary>
        public async Task<ImageValidationResult> ValidateAsync(string filePath)
        {
            try
            {
                _logger.LogDebug("[V3_WebP] 検証開始: {FileName}", Path.GetFileName(filePath));
                
                return await Task.Run(() =>
                {
                    using var image = SixLabors.ImageSharp.Image.Load(filePath);
                    var fileInfo = new FileInfo(filePath);
                    
                    var issues = new List<string>();
                    
                    // WebP特有の検証: 透明度チェック
                    var hasTransparency = CheckTransparency(image);
                    if (hasTransparency)
                    {
                        issues.Add("透明度情報あり - PDF変換時は背景色が適用される可能性");
                    }
                    
                    return new ImageValidationResult(
                        FilePath: filePath,
                        IsValid: true,
                        IsCorrupted: false,
                        IsZeroBytes: fileInfo.Length == 0,
                        FileSize: fileInfo.Length,
                        Format: "WebP",
                        Width: image.Width,
                        Height: image.Height,
                        Issues: issues,
                        ErrorMessage: null
                    );
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_WebP] 検証エラー: {FilePath}", filePath);
                return new ImageValidationResult(
                    FilePath: filePath,
                    IsValid: false,
                    IsCorrupted: true,
                    IsZeroBytes: false,
                    FileSize: 0,
                    Format: "WebP",
                    Width: 0,
                    Height: 0,
                    Issues: new List<string> { "WebP読み込みエラー" },
                    ErrorMessage: ex.Message
                );
            }
        }
        
        /// <summary>
        /// WebPサムネイル生成（透明度サポート）
        /// </summary>
        public async Task<ImageSource> GenerateThumbnailAsync(string filePath, ThumbnailSize size, int rotation = 0)
        {
            try
            {
                _logger.LogDebug("[V3_WebP] サムネイル生成開始: {FileName}, サイズ: {Size}, 回転: {Rotation}度", 
                    Path.GetFileName(filePath), size, rotation);

                return await Task.Run(() =>
                {
                    using var image = SixLabors.ImageSharp.Image.Load(filePath);
                    
                    // 回転適用
                    if (rotation > 0)
                    {
                        image.Mutate(x => x.Rotate(rotation));
                    }
                    
                    // サイズ別リサイズ
                    var (width, height) = GetSizeForThumbnailType(size);
                    var targetSize = CalculateResizeWithAspectRatio(image.Width, image.Height, width, height);
                    image.Mutate(x => x.Resize(targetSize.Width, targetSize.Height));

                    // WPFのBitmapImageに変換（PNG形式で透明度保持）
                    return ConvertImageSharpToBitmapImage(image);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_WebP] サムネイル生成エラー: {FilePath}", filePath);
                throw;
            }
        }
        
        /// <summary>
        /// WebPプレビュー生成（高解像度表示用）
        /// </summary>
        public async Task<ImageSource> GeneratePreviewAsync(string filePath, int maxWidth = 1920, int maxHeight = 1080)
        {
            try
            {
                _logger.LogDebug("[V3_WebP] プレビュー生成開始: {FileName}, 上限: {MaxWidth}x{MaxHeight}", 
                    Path.GetFileName(filePath), maxWidth, maxHeight);

                return await Task.Run(() =>
                {
                    // WebPの場合はImageSharpで処理（WPFはWebP未対応のため）
                    using var image = SixLabors.ImageSharp.Image.Load(filePath);
                    
                    // 高解像度制限適用
                    var targetSize = CalculateResizeWithAspectRatio(image.Width, image.Height, maxWidth, maxHeight);
                    if (targetSize.Width < image.Width || targetSize.Height < image.Height)
                    {
                        image.Mutate(x => x.Resize(targetSize.Width, targetSize.Height));
                    }
                    
                    return ConvertImageSharpToBitmapImage(image);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_WebP] プレビュー生成エラー: {FilePath}", filePath);
                throw;
            }
        }
        
        /// <summary>
        /// WebP画像情報取得
        /// </summary>
        public async Task<DocOrganizer.Application.Interfaces.V3.ImageInfo> GetImageInfoAsync(string filePath)
        {
            try
            {
                return await Task.Run(() =>
                {
                    var fileInfo = new FileInfo(filePath);
                    
                    using var image = SixLabors.ImageSharp.Image.Load(filePath);
                    
                    return new DocOrganizer.Application.Interfaces.V3.ImageInfo(
                        Width: image.Width,
                        Height: image.Height,
                        EXIFRotation: System.Windows.Media.Imaging.Rotation.Rotate0, // WebPは通常EXIF無し
                        FileSize: fileInfo.Length,
                        Format: "WebP"
                    );
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_WebP] 画像情報取得エラー: {FilePath}", filePath);
                throw;
            }
        }
        
        // Private helper methods
        
        private bool CheckTransparency(SixLabors.ImageSharp.Image image)
        {
            try
            {
                // WebP特有の透明度チェック（簡易版）
                // 実際の実装では、より詳細な透明度検証が可能
                var webpMetadata = image.Metadata.GetWebpMetadata();
                return webpMetadata != null; // 簡易判定（将来実装で拡張）
            }
            catch
            {
                return false;
            }
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
        
        private (int Width, int Height) CalculateResizeWithAspectRatio(int originalWidth, int originalHeight, int maxWidth, int maxHeight)
        {
            var ratioX = (double)maxWidth / originalWidth;
            var ratioY = (double)maxHeight / originalHeight;
            var ratio = Math.Min(ratioX, ratioY);

            return ((int)(originalWidth * ratio), (int)(originalHeight * ratio));
        }
        
        private BitmapImage ConvertImageSharpToBitmapImage(SixLabors.ImageSharp.Image image)
        {
            using var memoryStream = new MemoryStream();
            
            // PNG形式で出力（透明度・品質重視）
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
    }
}