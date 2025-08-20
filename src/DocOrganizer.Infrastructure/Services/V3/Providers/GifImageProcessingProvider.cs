using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using DocOrganizer.Application.Interfaces.V3;
using DocOrganizer.Application.Attributes;
using System.Collections.Generic;
using System.Linq;

namespace DocOrganizer.Infrastructure.Services.V3.Providers
{
    /// <summary>
    /// 🏗️ V3.0.009 GIF専用プロバイダー - アニメーション対応特化処理
    /// GIF特有のフレーム解析・アニメーション検出・最適化
    /// </summary>
    [ImageProcessingProvider("GIF", Priority = 90)]
    public class GifImageProcessingProvider : IImageProcessingProvider
    {
        private readonly ILogger<GifImageProcessingProvider> _logger;
        
        public string[] SupportedExtensions => new[] { ".gif" };
        public int Priority => 90;
        public string ProviderName => "ImageSharp GIF Animation Provider";
        
        public GifImageProcessingProvider(ILogger<GifImageProcessingProvider> logger)
        {
            _logger = logger;
        }
        
        public bool SupportsFormat(string extension)
        {
            return extension.Equals(".gif", StringComparison.OrdinalIgnoreCase);
        }
        
        /// <summary>
        /// GIF画像検証（アニメーション情報含む）
        /// </summary>
        public async Task<ImageValidationResult> ValidateAsync(string filePath)
        {
            try
            {
                _logger.LogDebug("[V3_GIF] 検証開始: {FileName}", Path.GetFileName(filePath));
                
                return await Task.Run(() =>
                {
                    using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(filePath);
                    var fileInfo = new FileInfo(filePath);
                    
                    var frameCount = image.Frames.Count;
                    var isAnimated = frameCount > 1;
                    var totalDuration = CalculateAnimationDuration(image);
                    
                    var issues = new List<string>();
                    if (isAnimated && frameCount > 100)
                    {
                        issues.Add($"フレーム数が多い({frameCount}フレーム) - 処理に時間がかかる可能性");
                    }
                    
                    _logger.LogDebug("[V3_GIF] GIF解析完了: {FrameCount}フレーム, アニメーション: {IsAnimated}, 総再生時間: {Duration}ms", 
                        frameCount, isAnimated, totalDuration.TotalMilliseconds);
                    
                    return new ImageValidationResult(
                        FilePath: filePath,
                        IsValid: true,
                        IsCorrupted: false,
                        IsZeroBytes: fileInfo.Length == 0,
                        FileSize: fileInfo.Length,
                        Format: "GIF",
                        Width: image.Width,
                        Height: image.Height,
                        Issues: issues,
                        ErrorMessage: null
                    );
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_GIF] 検証エラー: {FilePath}", filePath);
                return new ImageValidationResult(
                    FilePath: filePath,
                    IsValid: false,
                    IsCorrupted: true,
                    IsZeroBytes: false,
                    FileSize: 0,
                    Format: "GIF",
                    Width: 0,
                    Height: 0,
                    Issues: new List<string> { "GIF読み込みエラー" },
                    ErrorMessage: ex.Message
                );
            }
        }
        
        /// <summary>
        /// GIFサムネイル生成（最初のフレームを静止画として処理）
        /// </summary>
        public async Task<ImageSource> GenerateThumbnailAsync(string filePath, ThumbnailSize size, int rotation = 0)
        {
            try
            {
                _logger.LogDebug("[V3_GIF] サムネイル生成開始: {FileName}, サイズ: {Size}, 回転: {Rotation}度", 
                    Path.GetFileName(filePath), size, rotation);

                return await Task.Run(() =>
                {
                    using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(filePath);
                    
                    var frameCount = image.Frames.Count;
                    _logger.LogDebug("[V3_GIF] GIF解析完了: {FrameCount}フレーム, サイズ: {Width}x{Height}", 
                        frameCount, image.Width, image.Height);
                    
                    // 回転適用
                    if (rotation > 0)
                    {
                        image.Mutate(x => x.Rotate(rotation));
                    }
                    
                    // サイズ別リサイズ
                    var (width, height) = GetSizeForThumbnailType(size);
                    var targetSize = CalculateResizeWithAspectRatio(image.Width, image.Height, width, height);
                    image.Mutate(x => x.Resize(targetSize.Width, targetSize.Height));

                    // 最初のフレームをJPEGとしてメモリに変換（品質・パフォーマンス最適化）
                    using var memoryStream = new MemoryStream();
                    image.SaveAsJpeg(memoryStream, new JpegEncoder { Quality = 95 });
                    memoryStream.Position = 0;

                    // WPF BitmapSourceに変換
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.StreamSource = memoryStream;
                    bitmap.EndInit();
                    bitmap.Freeze();

                    _logger.LogDebug("[V3_GIF] ImageSharp GIF変換完了: {FileName} ({FrameCount}フレーム → 静止画)", 
                        Path.GetFileName(filePath), frameCount);

                    return bitmap;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_GIF] サムネイル生成エラー: {FilePath}", filePath);
                throw;
            }
        }
        
        /// <summary>
        /// GIFプレビュー生成（高解像度静止画として処理）
        /// </summary>
        public async Task<ImageSource> GeneratePreviewAsync(string filePath, int maxWidth = 1920, int maxHeight = 1080)
        {
            try
            {
                _logger.LogDebug("[V3_GIF] プレビュー生成開始: {FileName}, 上限: {MaxWidth}x{MaxHeight}", 
                    Path.GetFileName(filePath), maxWidth, maxHeight);

                return await Task.Run(() =>
                {
                    // GIFの場合はWPF標準読み込みを使用（アニメーション保持の場合）
                    try
                    {
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
                        
                        bitmap.EndInit();
                        bitmap.Freeze();
                        
                        return bitmap;
                    }
                    catch (Exception wpfEx)
                    {
                        _logger.LogWarning(wpfEx, "[V3_GIF] WPF標準読み込み失敗、ImageSharpにフォールバック: {FilePath}", filePath);
                        
                        // フォールバック: ImageSharpで最初のフレームを静止画として処理
                        return GenerateThumbnailAsync(filePath, ThumbnailSize.RightPreview, 0).Result;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_GIF] プレビュー生成エラー: {FilePath}", filePath);
                throw;
            }
        }
        
        /// <summary>
        /// GIF画像情報取得（アニメーション情報含む）
        /// </summary>
        public async Task<DocOrganizer.Application.Interfaces.V3.ImageInfo> GetImageInfoAsync(string filePath)
        {
            try
            {
                return await Task.Run(() =>
                {
                    var fileInfo = new FileInfo(filePath);

                    using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                    var frame = BitmapFrame.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
                    
                    return new DocOrganizer.Application.Interfaces.V3.ImageInfo(
                        Width: frame.PixelWidth,
                        Height: frame.PixelHeight,
                        EXIFRotation: System.Windows.Media.Imaging.Rotation.Rotate0, // GIFは通常EXIFなし
                        FileSize: fileInfo.Length,
                        Format: "GIF"
                    );
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_GIF] 画像情報取得エラー: {FilePath}", filePath);
                throw;
            }
        }
        
        // Private helper methods
        
        private TimeSpan CalculateAnimationDuration(Image<Rgba32> image)
        {
            try
            {
                // GIF特有のフレーム持続時間計算
                var totalDelay = 0;
                foreach (var frame in image.Frames)
                {
                    var gifMetadata = frame.Metadata.GetGifMetadata();
                    totalDelay += gifMetadata.FrameDelay * 10; // GIFの遅延は1/100秒単位
                }
                return TimeSpan.FromMilliseconds(totalDelay);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[V3_GIF] アニメーション時間計算警告");
                return TimeSpan.Zero;
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
    }
}