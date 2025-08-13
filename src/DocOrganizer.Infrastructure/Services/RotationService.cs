using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using ImageMagick;
using DocOrganizer.Application.Interfaces;

namespace DocOrganizer.Infrastructure.Services
{
    /// <summary>
    /// 統一回転処理サービス
    /// 全ての画像回転処理を一箇所で管理し、詳細なログを出力
    /// </summary>
    public class RotationService : IRotationService
    {
        private readonly ILogger<RotationService> _logger;
        private readonly RotationStatistics _statistics;
        private bool _loggingEnabled = true;

        public RotationService(ILogger<RotationService> logger)
        {
            _logger = logger;
            _statistics = new RotationStatistics();
        }

        public async Task<int> DetectRequiredRotationAsync(string imagePath)
        {
            try
            {
                var exifOrientation = await GetExifOrientationAsync(imagePath);
                _statistics.ExifDetections++;
                
                // EXIF Orientationから必要な回転角度を算出
                var requiredRotation = exifOrientation switch
                {
                    3 => 180, // 上下逆
                    6 => 270, // 右90度回転（時計回り90度）
                    8 => 90,  // 左90度回転（反時計回り90度）
                    _ => 0    // 回転不要
                };

                if (_loggingEnabled)
                {
                    _logger.LogInformation("[RotationService] EXIF分析: {ImagePath} - Orientation={Orientation}, RequiredRotation={Rotation}°", 
                        Path.GetFileName(imagePath), exifOrientation, requiredRotation);
                }

                return requiredRotation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RotationService] EXIF分析エラー: {ImagePath}", imagePath);
                return 0;
            }
        }

        public SKBitmap RotateImage(SKBitmap source, int rotationDegrees, string operationId = "")
        {
            if (source == null) return null;
            if (rotationDegrees == 0) return source.Copy();

            var normalizedRotation = ((rotationDegrees % 360) + 360) % 360;
            if (normalizedRotation == 0) return source.Copy();

            try
            {
                _statistics.TotalRotations++;
                _statistics.LastOperationId = operationId;

                if (_loggingEnabled)
                {
                    _logger.LogInformation("[RotationService] 回転処理開始: {Rotation}度, Size={Width}x{Height} ({OperationId})", 
                        normalizedRotation, source.Width, source.Height, operationId);
                }

                // 回転後のサイズを計算
                int newWidth = (normalizedRotation == 90 || normalizedRotation == 270) ? source.Height : source.Width;
                int newHeight = (normalizedRotation == 90 || normalizedRotation == 270) ? source.Width : source.Height;

                var rotatedBitmap = new SKBitmap(newWidth, newHeight, source.ColorType, source.AlphaType);

                using (var canvas = new SKCanvas(rotatedBitmap))
                {
                    canvas.Clear(SKColors.Transparent);
                    
                    // キャンバスの中心を回転の基点とする
                    float centerX = newWidth / 2f;
                    float centerY = newHeight / 2f;
                    
                    canvas.Translate(centerX, centerY);
                    canvas.RotateDegrees(normalizedRotation);
                    
                    // 元画像を中心に配置して描画
                    float drawX = -source.Width / 2f;
                    float drawY = -source.Height / 2f;
                    canvas.DrawBitmap(source, drawX, drawY);
                }

                var details = $"{normalizedRotation}度, {source.Width}x{source.Height} → {newWidth}x{newHeight}";
                _statistics.LastRotationDetails = details;

                if (_loggingEnabled)
                {
                    _logger.LogInformation("[RotationService] 回転完了: {Details} ({OperationId})", 
                        details, operationId);
                }

                return rotatedBitmap;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RotationService] 回転処理エラー: {Rotation}度 ({OperationId})", 
                    normalizedRotation, operationId);
                return source.Copy();
            }
        }


        public async Task<SKBitmap> LoadImageWithoutAutoRotationAsync(string imagePath)
        {
            try
            {
                _statistics.AutoRotationsPrevented++;

                if (_loggingEnabled)
                {
                    _logger.LogInformation("[RotationService] EXIF無視読み込み開始: {ImagePath}", 
                        Path.GetFileName(imagePath));
                }

                // ファイルから直接読み込み、EXIF Orientationを完全無視
                using var fileStream = File.OpenRead(imagePath);
                using var codec = SKCodec.Create(fileStream);
                
                if (codec == null)
                {
                    throw new InvalidOperationException($"Failed to create codec for: {imagePath}");
                }

                // 元の画像サイズでピクセルデータのみを読み込み
                var imageInfo = new SKImageInfo(codec.Info.Width, codec.Info.Height, SKColorType.Rgba8888);
                var bitmap = SKBitmap.Decode(codec, imageInfo);

                if (bitmap == null)
                {
                    throw new InvalidOperationException($"Failed to decode image: {imagePath}");
                }

                if (_loggingEnabled)
                {
                    _logger.LogInformation("[RotationService] EXIF無視読み込み完了: {ImagePath} - Size={Width}x{Height}", 
                        Path.GetFileName(imagePath), bitmap.Width, bitmap.Height);
                }

                return bitmap;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RotationService] EXIF無視読み込みエラー: {ImagePath}", imagePath);
                throw;
            }
        }

        public async Task<int> GetExifOrientationAsync(string imagePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var magickImage = new MagickImage(imagePath);
                    return (int)magickImage.Orientation;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[RotationService] EXIF Orientation読み取りエラー: {ImagePath}", imagePath);
                    return 1; // TopLeft (回転なし)
                }
            });
        }

        public void SetLoggingEnabled(bool enabled)
        {
            _loggingEnabled = enabled;
            if (_loggingEnabled)
            {
                _logger.LogInformation("[RotationService] ログ出力が有効化されました");
            }
        }

        public RotationStatistics GetStatistics()
        {
            return new RotationStatistics
            {
                TotalRotations = _statistics.TotalRotations,
                ExifDetections = _statistics.ExifDetections,
                AutoRotationsPrevented = _statistics.AutoRotationsPrevented,
                LastOperationId = _statistics.LastOperationId,
                LastRotationDetails = _statistics.LastRotationDetails
            };
        }
    }
}