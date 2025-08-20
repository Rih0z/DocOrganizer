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
    /// 🏗️ V3.0.009 HEIC専用プロバイダー - ImageMagick活用による完全対応
    /// 検証→サムネイル→プレビュー→編集の統一的HEIC処理
    /// </summary>
    [ImageProcessingProvider("HEIC", Priority = 100)]
    public class HeicImageProcessingProvider : IImageProcessingProvider
    {
        private readonly IHeicConversionService _heicConversionService;
        private readonly ILogger<HeicImageProcessingProvider> _logger;
        
        public string[] SupportedExtensions => new[] { ".heic", ".heif" };
        public int Priority => 100; // 最高優先度
        public string ProviderName => "ImageMagick HEIC Provider";
        
        public HeicImageProcessingProvider(
            IHeicConversionService heicConversionService,
            ILogger<HeicImageProcessingProvider> logger)
        {
            _heicConversionService = heicConversionService;
            _logger = logger;
        }
        
        public bool SupportsFormat(string extension)
        {
            return extension.Equals(".heic", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".heif", StringComparison.OrdinalIgnoreCase);
        }
        
        /// <summary>
        /// HEIC画像検証（ドラッグ&ドロップ用）
        /// </summary>
        public async Task<ImageValidationResult> ValidateAsync(string filePath)
        {
            try
            {
                _logger.LogDebug("[V3_HEIC] 検証開始: {FileName}", Path.GetFileName(filePath));
                
                var heicInfo = await _heicConversionService.GetHeicInfoAsync(filePath);
                
                return new ImageValidationResult(
                    FilePath: filePath,
                    IsValid: true,
                    IsCorrupted: false,
                    IsZeroBytes: false,
                    FileSize: heicInfo.FileSize,
                    Format: "HEIC",
                    Width: heicInfo.Width,
                    Height: heicInfo.Height,
                    Issues: new List<string>(),
                    ErrorMessage: null
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_HEIC] 検証エラー: {FilePath}", filePath);
                return new ImageValidationResult(
                    FilePath: filePath,
                    IsValid: false,
                    IsCorrupted: true,
                    IsZeroBytes: false,
                    FileSize: 0,
                    Format: "HEIC",
                    Width: 0,
                    Height: 0,
                    Issues: new List<string> { "HEIC読み込みエラー" },
                    ErrorMessage: ex.Message
                );
            }
        }
        
        /// <summary>
        /// HEICサムネイル生成（左パネル、右プレビュー、PDF対応）
        /// </summary>
        public async Task<ImageSource> GenerateThumbnailAsync(string filePath, ThumbnailSize size, int rotation = 0)
        {
            try
            {
                _logger.LogDebug("[V3_HEIC] サムネイル生成開始: {FileName}, サイズ: {Size}, 回転: {Rotation}度", 
                    Path.GetFileName(filePath), size, rotation);

                // HEIC→JPEG変換でImageSharp互換性確保
                var jpegPath = await _heicConversionService.ConvertHeicToTempJpegAsync(filePath);
                
                return await Task.Run(() =>
                {
                    using var image = SixLabors.ImageSharp.Image.Load(jpegPath);
                    
                    // EXIF Orientation自動補正
                    image.Mutate(x => x.AutoOrient());
                    
                    // 回転適用
                    if (rotation > 0)
                    {
                        image.Mutate(x => x.Rotate(rotation));
                    }
                    
                    // サイズ別リサイズ
                    var (width, height) = GetSizeForThumbnailType(size);
                    var targetSize = CalculateResizeWithAspectRatio(image.Width, image.Height, width, height);
                    image.Mutate(x => x.Resize(targetSize.Width, targetSize.Height));

                    // WPFのBitmapImageに変換
                    return ConvertImageSharpToBitmapImage(image);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_HEIC] サムネイル生成エラー: {FilePath}", filePath);
                throw;
            }
        }
        
        /// <summary>
        /// HEICプレビュー画像生成（高解像度表示用）
        /// </summary>
        public async Task<ImageSource> GeneratePreviewAsync(string filePath, int maxWidth = 1920, int maxHeight = 1080)
        {
            try
            {
                _logger.LogDebug("[V3_HEIC] プレビュー生成開始: {FileName}, 上限: {MaxWidth}x{MaxHeight}", 
                    Path.GetFileName(filePath), maxWidth, maxHeight);

                // HEIC→JPEG変換
                var jpegPath = await _heicConversionService.ConvertHeicToTempJpegAsync(filePath);
                
                return await Task.Run(() =>
                {
                    // 🎯 アスペクト比保持修正: 元画像サイズを取得
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(jpegPath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    
                    // アスペクト比を保持してリサイズ制限を適用
                    // どちらか一つだけを指定することでアスペクト比が自動保持される
                    var targetSize = CalculatePreviewSize(jpegPath, maxWidth, maxHeight);
                    if (targetSize.Width < maxWidth)
                    {
                        bitmap.DecodePixelWidth = targetSize.Width;
                    }
                    else if (targetSize.Height < maxHeight)
                    {
                        bitmap.DecodePixelHeight = targetSize.Height;
                    }
                    // 両方とも制限内の場合は元サイズのまま（制限指定なし）
                    
                    bitmap.EndInit();
                    bitmap.Freeze();
                    
                    return bitmap;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_HEIC] プレビュー生成エラー: {FilePath}", filePath);
                throw;
            }
        }
        
        /// <summary>
        /// HEIC画像情報取得
        /// </summary>
        public async Task<DocOrganizer.Application.Interfaces.V3.ImageInfo> GetImageInfoAsync(string filePath)
        {
            try
            {
                var heicInfo = await _heicConversionService.GetHeicInfoAsync(filePath);
                
                return new DocOrganizer.Application.Interfaces.V3.ImageInfo(
                    Width: heicInfo.Width,
                    Height: heicInfo.Height,
                    EXIFRotation: System.Windows.Media.Imaging.Rotation.Rotate0, // HEICはImageMagickで自動補正済み
                    FileSize: heicInfo.FileSize,
                    Format: "HEIC"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_HEIC] 画像情報取得エラー: {FilePath}", filePath);
                throw;
            }
        }
        
        // Private helper methods
        
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

        
        private (int Width, int Height) CalculatePreviewSize(string jpegPath, int maxWidth, int maxHeight)
        {
            try
            {
                // 元画像のサイズを取得（BitmapImageを使用してメタデータのみ読み込み）
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(jpegPath);
                bitmap.CacheOption = BitmapCacheOption.OnDemand; // メタデータのみ
                bitmap.EndInit();
                
                var originalWidth = bitmap.PixelWidth;
                var originalHeight = bitmap.PixelHeight;
                
                // アスペクト比を保持してリサイズ
                return CalculateResizeWithAspectRatio(originalWidth, originalHeight, maxWidth, maxHeight);
            }
            catch
            {
                // エラー時はデフォルトサイズを返す
                return (maxWidth, maxHeight);
            }
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
    }
}