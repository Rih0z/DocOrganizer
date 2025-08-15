using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocOrganizer.Application.Interfaces.V3;
using ImageMagick;
using Microsoft.Extensions.Logging;

namespace DocOrganizer.Infrastructure.Services.V3
{
    /// <summary>
    /// 🎯 V3実装: OSS標準HEIC変換サービス
    /// 技術: Magick.NET専用による高速HEIC処理
    /// 目標: HEIC回転編集バグの根本解決
    /// </summary>
    public class HeicConversionService : IHeicConversionService
    {
        private readonly ILogger<HeicConversionService> _logger;

        public HeicConversionService(ILogger<HeicConversionService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// HEICからJPEGに変換
        /// </summary>
        public async Task<bool> ConvertHeicToJpegAsync(string heicFilePath, string jpegOutputPath, int quality = 90)
        {
            try
            {
                _logger.LogDebug("[V3_HEIC] HEIC→JPEG変換開始: {InputFile} → {OutputFile}, 品質: {Quality}", 
                    Path.GetFileName(heicFilePath), Path.GetFileName(jpegOutputPath), quality);

                return await Task.Run(() =>
                {
                    using var image = new MagickImage(heicFilePath);
                    
                    // 🎯 EXIF Orientation自動適用
                    image.AutoOrient();
                    
                    // JPEG設定
                    image.Format = MagickFormat.Jpeg;
                    image.Quality = (uint)quality;
                    
                    // 出力ディレクトリ確保
                    var outputDir = Path.GetDirectoryName(jpegOutputPath);
                    if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                    {
                        Directory.CreateDirectory(outputDir);
                    }
                    
                    image.Write(jpegOutputPath);
                    
                    _logger.LogDebug("[V3_HEIC] HEIC→JPEG変換完了: サイズ {Width}x{Height}, ファイル: {OutputFile}", 
                        image.Width, image.Height, Path.GetFileName(jpegOutputPath));
                    
                    return true;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_HEIC] HEIC→JPEG変換エラー: {InputFile}", heicFilePath);
                return false;
            }
        }

        /// <summary>
        /// HEICからPNGに変換
        /// </summary>
        public async Task<bool> ConvertHeicToPngAsync(string heicFilePath, string pngOutputPath)
        {
            try
            {
                _logger.LogDebug("[V3_HEIC] HEIC→PNG変換開始: {InputFile} → {OutputFile}", 
                    Path.GetFileName(heicFilePath), Path.GetFileName(pngOutputPath));

                return await Task.Run(() =>
                {
                    using var image = new MagickImage(heicFilePath);
                    
                    // EXIF Orientation自動適用
                    image.AutoOrient();
                    
                    // PNG設定（ロスレス）
                    image.Format = MagickFormat.Png;
                    
                    // 出力ディレクトリ確保
                    var outputDir = Path.GetDirectoryName(pngOutputPath);
                    if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                    {
                        Directory.CreateDirectory(outputDir);
                    }
                    
                    image.Write(pngOutputPath);
                    
                    _logger.LogDebug("[V3_HEIC] HEIC→PNG変換完了: {OutputFile}", Path.GetFileName(pngOutputPath));
                    
                    return true;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_HEIC] HEIC→PNG変換エラー: {InputFile}", heicFilePath);
                return false;
            }
        }

        /// <summary>
        /// HEIC一時JPEG変換（回転編集用）
        /// </summary>
        public async Task<string> ConvertHeicToTempJpegAsync(string heicFilePath)
        {
            try
            {
                var tempJpegPath = Path.GetTempFileName() + ".jpg";
                
                _logger.LogDebug("[V3_HEIC] HEIC一時変換開始: {InputFile} → {TempFile}", 
                    Path.GetFileName(heicFilePath), Path.GetFileName(tempJpegPath));

                var success = await ConvertHeicToJpegAsync(heicFilePath, tempJpegPath, 95); // 高品質
                
                if (success)
                {
                    _logger.LogDebug("[V3_HEIC] HEIC一時変換成功: {TempFile}", Path.GetFileName(tempJpegPath));
                    return tempJpegPath;
                }
                else
                {
                    throw new InvalidOperationException("HEIC一時変換に失敗しました");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_HEIC] HEIC一時変換エラー: {InputFile}", heicFilePath);
                throw;
            }
        }

        /// <summary>
        /// HEIC情報取得
        /// </summary>
        public async Task<HeicImageInfo> GetHeicInfoAsync(string heicFilePath)
        {
            try
            {
                return await Task.Run(() =>
                {
                    using var image = new MagickImage(heicFilePath);
                    var fileInfo = new FileInfo(heicFilePath);
                    
                    // EXIF Orientation取得
                    var hasExifOrientation = image.GetExifProfile()?.GetValue(ExifTag.Orientation) != null;
                    var exifOrientation = hasExifOrientation 
                        ? (ushort)image.GetExifProfile()!.GetValue(ExifTag.Orientation)!.Value
                        : (ushort)1;
                    
                    return new HeicImageInfo(
                        Width: (int)image.Width,
                        Height: (int)image.Height,
                        FileSize: fileInfo.Length,
                        Format: "HEIC",
                        HasExifOrientation: hasExifOrientation,
                        ExifOrientation: exifOrientation
                    );
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_HEIC] HEIC情報取得エラー: {FilePath}", heicFilePath);
                throw;
            }
        }

        /// <summary>
        /// HEIC対応判定
        /// </summary>
        public bool IsHeicFile(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension == ".heic" || extension == ".heif";
        }

        /// <summary>
        /// 一括HEIC変換
        /// </summary>
        public async Task<HeicConversionResult[]> ConvertHeicBatchAsync(string[] heicFiles, HeicOutputFormat outputFormat)
        {
            try
            {
                _logger.LogDebug("[V3_HEIC] 一括HEIC変換開始: {FileCount}件, 形式: {Format}", heicFiles.Length, outputFormat);

                var tasks = heicFiles.Select(async heicFile =>
                {
                    try
                    {
                        var outputExtension = outputFormat == HeicOutputFormat.Jpeg ? ".jpg" : ".png";
                        var outputPath = Path.ChangeExtension(heicFile, outputExtension);
                        
                        var success = outputFormat == HeicOutputFormat.Jpeg
                            ? await ConvertHeicToJpegAsync(heicFile, outputPath)
                            : await ConvertHeicToPngAsync(heicFile, outputPath);

                        return new HeicConversionResult(
                            OriginalPath: heicFile,
                            ConvertedPath: outputPath,
                            Success: success,
                            ErrorMessage: success ? null : "変換処理が失敗しました"
                        );
                    }
                    catch (Exception ex)
                    {
                        return new HeicConversionResult(
                            OriginalPath: heicFile,
                            ConvertedPath: "",
                            Success: false,
                            ErrorMessage: ex.Message
                        );
                    }
                });

                var results = await Task.WhenAll(tasks);
                
                var successCount = results.Count(r => r.Success);
                _logger.LogDebug("[V3_HEIC] 一括HEIC変換完了: {SuccessCount}/{TotalCount}", successCount, heicFiles.Length);

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_HEIC] 一括HEIC変換エラー");
                throw;
            }
        }
    }
}