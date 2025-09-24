using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DocOrganizer.Application.Interfaces.V3;
using ImageMagick;
using Microsoft.Extensions.Logging;

namespace DocOrganizer.Infrastructure.Services.V3
{
    /// <summary>
    /// 画像余白自動削除サービス実装
    /// ユーザー要求：余白は絶対に必要なし
    /// </summary>
    public class AutoCropService : IAutoCropService
    {
        private readonly ILogger<AutoCropService> _logger;

        public AutoCropService(ILogger<AutoCropService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 画像の余白を自動削除（必須機能）
        /// </summary>
        public async Task<BitmapSource> AutoCropAsync(BitmapSource source)
        {
            string tempPath = null;
            try
            {
                _logger.LogDebug("[AutoCrop] 余白削除開始 サイズ: {Width}x{Height}",
                    source.PixelWidth, source.PixelHeight);

                // BitmapSourceを一時ファイルに保存
                tempPath = Path.GetTempFileName() + ".png";
                using (var fileStream = new FileStream(tempPath, FileMode.Create))
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(source));
                    encoder.Save(fileStream);
                }

                // Magick.NETでトリミング
                using (var image = new MagickImage(tempPath))
                {
                    // 元のサイズを記録
                    var originalWidth = image.Width;
                    var originalHeight = image.Height;

                    // EXIF自動回転を適用
                    image.AutoOrient();

                    // 余白を自動削除（Fuzz 1%で近似色も含める）
                    image.ColorFuzz = new Percentage(1);
                    image.Trim();

                    var newWidth = image.Width;
                    var newHeight = image.Height;

                    var cropRatio = 1 - (double)(newWidth * newHeight) / (originalWidth * originalHeight);

                    _logger.LogInformation(
                        "[AutoCrop] 余白削除完了: {OriginalSize} → {NewSize} (削除率: {CropRatio:P})",
                        $"{originalWidth}x{originalHeight}",
                        $"{newWidth}x{newHeight}",
                        cropRatio
                    );

                    // BitmapSourceに変換して返す
                    using (var stream = new MemoryStream())
                    {
                        image.Write(stream, MagickFormat.Png);
                        stream.Position = 0;

                        var decoder = new PngBitmapDecoder(
                            stream,
                            BitmapCreateOptions.PreservePixelFormat,
                            BitmapCacheOption.OnLoad
                        );

                        var result = decoder.Frames[0];
                        result.Freeze();
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AutoCrop] 余白削除エラー");
                // エラー時は元画像を返す（フェイルセーフ）
                return source;
            }
            finally
            {
                // 一時ファイル削除
                if (!string.IsNullOrEmpty(tempPath) && File.Exists(tempPath))
                {
                    try
                    {
                        File.Delete(tempPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[AutoCrop] 一時ファイル削除失敗: {TempPath}", tempPath);
                    }
                }
            }
        }

        /// <summary>
        /// ファイルパスから直接余白削除（高速版）
        /// </summary>
        public async Task<byte[]> TrimWhitespaceAsync(string imagePath, double fuzzPercentage = 1.0)
        {
            return await Task.Run(() =>
            {
                _logger.LogDebug("[AutoCrop] ファイル余白削除開始: {FileName}",
                    Path.GetFileName(imagePath));

                using var image = new MagickImage(imagePath);

                var originalWidth = image.Width;
                var originalHeight = image.Height;

                // EXIF自動回転適用
                image.AutoOrient();

                // 余白削除
                image.ColorFuzz = new Percentage(fuzzPercentage);
                image.Trim();

                _logger.LogDebug(
                    "[AutoCrop] ファイル余白削除完了: {FileName}, {OriginalSize} → {NewSize}",
                    Path.GetFileName(imagePath),
                    $"{originalWidth}x{originalHeight}",
                    $"{image.Width}x{image.Height}"
                );

                // バイト配列として返す
                return image.ToByteArray(MagickFormat.Png);
            });
        }

        /// <summary>
        /// クロップ領域の分析
        /// </summary>
        public async Task<CropInfo> AnalyzeCropAreaAsync(BitmapSource source)
        {
            string tempPath = null;
            try
            {
                // BitmapSourceを一時ファイルに保存
                tempPath = Path.GetTempFileName() + ".png";
                using (var fileStream = new FileStream(tempPath, FileMode.Create))
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(source));
                    encoder.Save(fileStream);
                }

                return await Task.Run(() =>
                {
                    using (var image = new MagickImage(tempPath))
                    {
                        var originalWidth = image.Width;
                        var originalHeight = image.Height;

                        // クローンを作成してトリミング
                        using (var clone = image.Clone() as MagickImage)
                        {
                            clone.ColorFuzz = new Percentage(1);
                            clone.Trim();

                            var left = (int)clone.Page.X;
                            var top = (int)clone.Page.Y;
                            var width = (int)clone.Width;
                            var height = (int)clone.Height;

                            return new CropInfo
                            {
                                Left = left,
                                Top = top,
                                Width = width,
                                Height = height,
                                CropRatio = 1 - (double)(width * height) / (originalWidth * originalHeight),
                                WasCropped = width < originalWidth || height < originalHeight
                            };
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AutoCrop] クロップ領域分析エラー");
                return new CropInfo { WasCropped = false };
            }
            finally
            {
                // 一時ファイル削除
                if (!string.IsNullOrEmpty(tempPath) && File.Exists(tempPath))
                {
                    try
                    {
                        File.Delete(tempPath);
                    }
                    catch { }
                }
            }
        }
    }
}