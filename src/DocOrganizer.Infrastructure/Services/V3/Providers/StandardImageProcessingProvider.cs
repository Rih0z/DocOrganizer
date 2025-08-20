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
    /// 🏗️ V3.0.009 標準画像プロバイダー - ImageSharp最適化による高速処理
    /// JPEG/PNG/BMP形式に最適化された処理
    /// </summary>
    [ImageProcessingProvider("Standard", Priority = 80)]
    public class StandardImageProcessingProvider : IImageProcessingProvider
    {
        private readonly ILogger<StandardImageProcessingProvider> _logger;
        
        public string[] SupportedExtensions => new[] { ".jpg", ".jpeg", ".png", ".bmp" };
        public int Priority => 80;
        public string ProviderName => "ImageSharp Standard Provider";
        
        public StandardImageProcessingProvider(ILogger<StandardImageProcessingProvider> logger)
        {
            _logger = logger;
        }
        
        public bool SupportsFormat(string extension)
        {
            var ext = extension.ToLowerInvariant();
            return ext == ".jpg" || ext == ".jpeg" || ext == ".png" || ext == ".bmp";
        }
        
        /// <summary>
        /// 標準画像検証（ドラッグ&ドロップ用）
        /// </summary>
        public async Task<ImageValidationResult> ValidateAsync(string filePath)
        {
            try
            {
                _logger.LogDebug("[V3_Standard] 検証開始: {FileName}", Path.GetFileName(filePath));
                
                return await Task.Run(() =>
                {
                    using var image = SixLabors.ImageSharp.Image.Load(filePath);
                    var fileInfo = new FileInfo(filePath);
                    
                    return new ImageValidationResult(
                        FilePath: filePath,
                        IsValid: true,
                        IsCorrupted: false,
                        IsZeroBytes: fileInfo.Length == 0,
                        FileSize: fileInfo.Length,
                        Format: Path.GetExtension(filePath).ToUpperInvariant().TrimStart('.'),
                        Width: image.Width,
                        Height: image.Height,
                        Issues: new List<string>(),
                        ErrorMessage: null
                    );
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_Standard] 検証エラー: {FilePath}", filePath);
                return new ImageValidationResult(
                    FilePath: filePath,
                    IsValid: false,
                    IsCorrupted: true,
                    IsZeroBytes: false,
                    FileSize: 0,
                    Format: Path.GetExtension(filePath).ToUpperInvariant().TrimStart('.'),
                    Width: 0,
                    Height: 0,
                    Issues: new List<string> { "画像読み込みエラー" },
                    ErrorMessage: ex.Message
                );
            }
        }
        
        /// <summary>
        /// 標準画像サムネイル生成
        /// </summary>
        public async Task<ImageSource> GenerateThumbnailAsync(string filePath, ThumbnailSize size, int rotation = 0)
        {
            try
            {
                _logger.LogDebug("[V3_Standard] サムネイル生成開始: {FileName}, サイズ: {Size}, 回転: {Rotation}度", 
                    Path.GetFileName(filePath), size, rotation);

                return await Task.Run(() =>
                {
                    using var image = SixLabors.ImageSharp.Image.Load(filePath);
                    
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
                _logger.LogError(ex, "[V3_Standard] サムネイル生成エラー: {FilePath}", filePath);
                throw;
            }
        }
        
        /// <summary>
        /// 標準画像プレビュー生成（高解像度表示用）
        /// </summary>
        public async Task<ImageSource> GeneratePreviewAsync(string filePath, int maxWidth = 1920, int maxHeight = 1080)
        {
            try
            {
                _logger.LogDebug("[V3_Standard] プレビュー生成開始: {FileName}, 上限: {MaxWidth}x{MaxHeight}", 
                    Path.GetFileName(filePath), maxWidth, maxHeight);

                return await Task.Run(() =>
                {
                    // EXIF Orientation検出
                    var rotation = GetRotationFromExif(filePath);

                    // 🎯 アスペクト比保持修正: 元画像サイズを取得してからリサイズ制限適用
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(filePath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    
                    // アスペクト比を保持してリサイズ制限を適用
                    var targetSize = CalculatePreviewSize(filePath, maxWidth, maxHeight);
                    if (targetSize.Width < maxWidth)
                    {
                        bitmap.DecodePixelWidth = targetSize.Width;
                    }
                    else if (targetSize.Height < maxHeight)
                    {
                        bitmap.DecodePixelHeight = targetSize.Height;
                    }
                    // 両方とも制限内の場合は元サイズのまま（制限指定なし）
                    
                    bitmap.Rotation = rotation; // EXIF Orientation適用
                    bitmap.EndInit();
                    bitmap.Freeze();

                    return bitmap;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_Standard] プレビュー生成エラー: {FilePath}", filePath);
                throw;
            }
        }
        
        /// <summary>
        /// 標準画像情報取得
        /// </summary>
        public async Task<DocOrganizer.Application.Interfaces.V3.ImageInfo> GetImageInfoAsync(string filePath)
        {
            try
            {
                return await Task.Run(() =>
                {
                    var fileInfo = new FileInfo(filePath);
                    var rotation = GetRotationFromExif(filePath);

                    using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                    var frame = BitmapFrame.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
                    
                    var format = Path.GetExtension(filePath).ToUpperInvariant().TrimStart('.');

                    return new DocOrganizer.Application.Interfaces.V3.ImageInfo(
                        Width: frame.PixelWidth,
                        Height: frame.PixelHeight,
                        EXIFRotation: rotation,
                        FileSize: fileInfo.Length,
                        Format: format
                    );
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_Standard] 画像情報取得エラー: {FilePath}", filePath);
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

        
        private (int Width, int Height) CalculatePreviewSize(string filePath, int maxWidth, int maxHeight)
        {
            try
            {
                // 元画像のサイズを取得（BitmapImageを使用してメタデータのみ読み込み）
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(filePath);
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
        
        /// <summary>
        /// OSS標準EXIF Orientation読み取り（WPF標準API活用）
        /// 参考: Stack Overflow 47,000+実装例のベストプラクティス
        /// </summary>
        private System.Windows.Media.Imaging.Rotation GetRotationFromExif(string filePath)
        {
            try
            {
                using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                var frame = BitmapFrame.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
                var metadata = frame.Metadata as BitmapMetadata;

                if (metadata?.ContainsQuery("System.Photo.Orientation") == true)
                {
                    var orientationValue = metadata.GetQuery("System.Photo.Orientation");
                    if (orientationValue != null)
                    {
                        var orientation = (ushort)orientationValue;
                        return orientation switch
                        {
                            6 => System.Windows.Media.Imaging.Rotation.Rotate90,   // 右90度回転（時計回り）
                            3 => System.Windows.Media.Imaging.Rotation.Rotate180,  // 180度回転
                            8 => System.Windows.Media.Imaging.Rotation.Rotate270,  // 左90度回転（反時計回り）
                            _ => System.Windows.Media.Imaging.Rotation.Rotate0     // 回転なし（標準）
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[V3_Standard] EXIF読み取り警告（回転なしで続行）: {FilePath}", filePath);
            }

            return System.Windows.Media.Imaging.Rotation.Rotate0;
        }
    }
}