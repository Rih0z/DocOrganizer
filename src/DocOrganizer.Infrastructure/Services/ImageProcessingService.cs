using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using ImageMagick;
using DocOrganizer.Application.Interfaces;
using DocOrganizer.Core.Models;

namespace DocOrganizer.Infrastructure.Services
{
    /// <summary>
    /// 画像処理で発生する特定の例外
    /// </summary>
    public class ImageProcessingException : Exception
    {
        public ImageProcessingException(string message) : base(message) { }
        public ImageProcessingException(string message, Exception innerException) : base(message, innerException) { }
    }
    public class ImageProcessingService : IImageProcessingService
    {
        private readonly ILogger<ImageProcessingService> _logger;
        private readonly IPdfService _pdfService;
        private static readonly string[] SupportedExtensions = 
        {
            ".jpg", ".jpeg", ".png", ".heic", ".heif", ".bmp", ".tiff", ".gif", ".webp"
        };

        // HEIC処理クラッシュ修正: 安全な初期化管理
        private bool _magickNetInitialized = false;
        private bool _magickNetAvailable = false;
        private readonly object _initLock = new object();

        public ImageProcessingService(ILogger<ImageProcessingService> logger, IPdfService pdfService)
        {
            _logger = logger;
            _pdfService = pdfService;
            
            // エンコーディング問題対策: UTF-8をサポート
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            
            // Magick.NET初期化（安全版）
            _magickNetAvailable = InitializeMagickNetSafely();
        }

        public async Task<PdfDocument> ConvertImageToPdfAsync(string imagePath)
        {
            try
            {
                if (!await IsValidImageAsync(imagePath))
                {
                    throw new ArgumentException($"Invalid image file: {imagePath}");
                }

                // HEIC処理可能性の事前確認
                if (IsHeicFile(imagePath) && !IsHeicProcessingAvailable())
                {
                    throw new NotSupportedException($"HEIC processing unavailable - please install Magick.NET with HEIC support. File: {Path.GetFileName(imagePath)}");
                }

                // 向き自動補正を実行
                var correctedRotation = await DetectAndCorrectOrientationAsync(imagePath);
                
                // 仮想的なPDFドキュメントを作成（実際のPDFファイルは作成しない）
                var pdfDocument = new PdfDocument()
                {
                    IsTemporaryFromImages = true,
                    FilePath = Path.ChangeExtension(imagePath, ".pdf")
                };
                
                // HEICファイルの場合は変換後のJPEGパスを保存
                string effectiveImagePath = imagePath;
                if (IsHeicFile(imagePath))
                {
                    _logger.LogInformation($"HEIC file detected, converting for preview: {Path.GetFileName(imagePath)}");
                    // HEICファイルは一時的にJPEGに変換（後でプレビュー生成に使用）
                    // この時点では変換しない - GetImageThumbnailAsyncで処理される
                }
                
                pdfDocument.SourceImagePaths.Add(imagePath);
                
                // ページを作成（自動補正された回転角度を設定）
                var page = new PdfPage(1)
                {
                    SourceImagePath = imagePath, // HEICファイルのままセット（GetImageThumbnailAsyncで自動変換）
                    Rotation = correctedRotation
                };
                
                // A4サイズの寸法を設定
                const float pageWidth = 595;
                const float pageHeight = 842;
                page.SetDimensions(pageWidth, pageHeight);
                
                // サムネイルは後で生成（高速化のため）
                // 初期表示はnullでOK（LoadThumbnailで生成される）
                
                pdfDocument.AddPage(page);
                pdfDocument.ClearModifiedFlag();

                _logger.LogInformation("Image loaded with auto-orientation detection: {ImagePath}", 
                    Path.GetFileName(imagePath));

                return pdfDocument;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load image: {ImagePath}", imagePath);
                throw;
            }
        }

        public async Task<PdfDocument> ConvertImagesToPdfAsync(IEnumerable<string> imagePaths)
        {
            try
            {
                var validPaths = new List<string>();
                foreach (var path in imagePaths)
                {
                    if (await IsValidImageAsync(path))
                    {
                        validPaths.Add(path);
                    }
                }

                if (!validPaths.Any())
                {
                    throw new ArgumentException("No valid image files found");
                }

                // 仮想的なPDFドキュメントを作成（実際のPDFファイルは作成しない）
                var pdfDocument = new PdfDocument()
                {
                    IsTemporaryFromImages = true,
                    FilePath = Path.Combine(Path.GetTempPath(), $"images_{DateTime.Now:yyyyMMddHHmmss}.pdf")
                };
                pdfDocument.SourceImagePaths.AddRange(validPaths);
                
                int pageNumber = 1;
                foreach (var imagePath in validPaths)
                {
                    // 向き自動補正を実行
                    var correctedRotation = await DetectAndCorrectOrientationAsync(imagePath);
                    
                    // ページを作成（自動補正された回転角度を設定）
                    var page = new PdfPage(pageNumber++)
                    {
                        SourceImagePath = imagePath,
                        Rotation = correctedRotation
                    };
                    
                    // A4サイズの寸法を設定
                    const float pageWidth = 595;
                    const float pageHeight = 842;
                    page.SetDimensions(pageWidth, pageHeight);
                    
                    // サムネイルは後で生成（高速化のため）
                    
                    pdfDocument.AddPage(page);
                    
                    _logger.LogInformation("Image loaded without auto-rotation: {ImagePath}", 
                        Path.GetFileName(imagePath));
                }
                
                pdfDocument.ClearModifiedFlag();
                return pdfDocument;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load images");
                throw;
            }
        }

        public async Task<byte[]> GetImageThumbnailAsync(string imagePath, int width = 150, int height = 150)
        {
            try
            {
                if (!await IsValidImageAsync(imagePath))
                {
                    throw new ArgumentException($"Invalid image file: {imagePath}");
                }

                var tempImagePath = imagePath;
                
                if (IsHeicFile(imagePath))
                {
                    tempImagePath = await ConvertHeicToJpegAsync(imagePath);
                }

                using var image = await LoadImageSafelyAsync(tempImagePath);
                
                // バグ修正：画像の向き自動補正を確実に適用
                // 要件定義書（tmp/DocOrganizer2.2_画像向き修正要件定義書.md）準拠
                image.Mutate(x => x
                    .AutoOrient()  // EXIF情報に基づく自動回転
                    .Resize(new ResizeOptions
                    {
                        Size = new Size(width, height),
                        Mode = ResizeMode.Max
                    }));

                using var ms = new MemoryStream();
                await image.SaveAsJpegAsync(ms, new JpegEncoder { Quality = 80 });

                if (tempImagePath != imagePath && File.Exists(tempImagePath))
                {
                    File.Delete(tempImagePath);
                }

                return ms.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate thumbnail: {ImagePath}", imagePath);
                throw;
            }
        }

        public async Task<bool> IsValidImageAsync(string imagePath)
        {
            try
            {
                _logger.LogDebug($"Validating image: {imagePath}");
                
                if (!File.Exists(imagePath))
                {
                    _logger.LogDebug($"File not found: {imagePath}");
                    return false;
                }

                var extension = Path.GetExtension(imagePath).ToLowerInvariant();
                if (!SupportedExtensions.Contains(extension))
                {
                    _logger.LogDebug($"Unsupported extension: {extension}");
                    return false;
                }

                // ファイルサイズチェック
                var fileInfo = new FileInfo(imagePath);
                if (fileInfo.Length == 0)
                {
                    _logger.LogDebug($"Empty file detected: {imagePath}");
                    return false;
                }

                if (fileInfo.Length > 100_000_000) // 100MB制限
                {
                    _logger.LogDebug($"File too large ({fileInfo.Length} bytes): {imagePath}");
                    return false;
                }

                // 形式別検証
                if (IsHeicFile(imagePath))
                {
                    return await ValidateHeicFileAsync(imagePath);
                }
                else
                {
                    return await ValidateGenericImageAsync(imagePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Image validation failed: {imagePath}");
                return false;
            }
        }
        
        private async Task<bool> ValidateHeicFileAsync(string imagePath)
        {
            try
            {
                using var magickImage = new MagickImage();
                await Task.Run(() => magickImage.Read(imagePath));
                
                var isValid = magickImage.Width > 0 && magickImage.Height > 0;
                _logger.LogDebug($"HEIC validation result: {isValid} ({magickImage.Width}x{magickImage.Height})");
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"HEIC validation failed: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> ValidateGenericImageAsync(string imagePath)
        {
            try
            {
                using var image = await LoadImageSafelyAsync(imagePath);
                var isValid = image.Width > 0 && image.Height > 0;
                _logger.LogDebug($"Generic image validation result: {isValid} ({image.Width}x{image.Height})");
                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"Generic image validation failed: {ex.Message}");
                return false;
            }
        }

        public async Task<string> GetImageInfoAsync(string imagePath)
        {
            try
            {
                if (!await IsValidImageAsync(imagePath))
                {
                    return "Invalid image file";
                }

                var fileInfo = new FileInfo(imagePath);
                var extension = Path.GetExtension(imagePath).ToUpper();
                
                if (IsHeicFile(imagePath))
                {
                    using var magickImage = new MagickImage(imagePath);
                    return $"{extension} - {magickImage.Width}x{magickImage.Height} - {FormatFileSize(fileInfo.Length)}";
                }
                else
                {
                    using var image = await LoadImageSafelyAsync(imagePath);
                    return $"{extension} - {image.Width}x{image.Height} - {FormatFileSize(fileInfo.Length)}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get image info: {ImagePath}", imagePath);
                return "Error reading image info";
            }
        }

        /// <summary>
        /// HEIC処理可能性の事前確認
        /// </summary>
        private bool IsHeicProcessingAvailable()
        {
            return _magickNetAvailable;
        }

        /// <summary>
        /// HEIC処理クラッシュ修正: 安全なHEIC→JPEG変換
        /// AutoOrient二重処理の防止とエラーハンドリング強化
        /// </summary>
        private async Task<string> ConvertHeicToJpegAsync(string heicPath)
        {
            // HEIC処理可能性の事前確認
            if (!IsHeicProcessingAvailable())
            {
                throw new NotSupportedException($"HEIC processing unavailable - Magick.NET initialization failed. File: {heicPath}");
            }

            var tempJpegPath = Path.GetTempFileName() + ".jpg";
            
            try
            {
                _logger.LogDebug($"Converting HEIC to JPEG: {heicPath} -> {tempJpegPath}");
                
                using (var magickImage = new MagickImage())
                {
                    // HEIC読み込み設定
                    magickImage.Settings.BackgroundColor = MagickColors.White;
                    magickImage.ColorSpace = ColorSpace.sRGB;
                    
                    // 非同期での読み込み
                    await Task.Run(() => magickImage.Read(heicPath));
                    
                    // 基本的な検証
                    if (magickImage.Width == 0 || magickImage.Height == 0)
                    {
                        throw new InvalidOperationException($"Invalid HEIC dimensions: {magickImage.Width}x{magickImage.Height}");
                    }
                    
                    _logger.LogDebug($"HEIC dimensions: {magickImage.Width}x{magickImage.Height}");
                    
                    // JPEG変換設定
                    magickImage.Format = MagickFormat.Jpeg;
                    magickImage.Quality = 95;
                    
                    // バグ修正: AutoOrientはここで1回のみ実行
                    // LoadImageSafelyAsyncでの二重処理を防ぐため、フラグを設定
                    magickImage.AutoOrient();
                    
                    // JPEG出力
                    await Task.Run(() => magickImage.Write(tempJpegPath));
                }
                
                // 出力ファイル検証
                if (!File.Exists(tempJpegPath))
                {
                    throw new InvalidOperationException("JPEG output file was not created");
                }
                
                var outputFileInfo = new FileInfo(tempJpegPath);
                if (outputFileInfo.Length == 0)
                {
                    throw new InvalidOperationException("JPEG output file is empty");
                }
                
                _logger.LogDebug($"HEIC conversion completed: {outputFileInfo.Length} bytes");
                
                return tempJpegPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to convert HEIC to JPEG: {HeicPath}", heicPath);
                
                // 失敗時は一時ファイルをクリーンアップ
                if (File.Exists(tempJpegPath))
                {
                    try
                    {
                        File.Delete(tempJpegPath);
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.LogWarning(cleanupEx, $"Failed to cleanup temp file: {tempJpegPath}");
                    }
                }
                
                // HEIC専用エラーハンドリング
                if (IsHeicProcessingError(ex))
                {
                    throw new ImageProcessingException($"HEIC processing failed: {ex.Message}", ex);
                }
                else
                {
                    throw new InvalidOperationException($"HEIC to JPEG conversion failed for {heicPath}: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// HEIC処理エラーの判定
        /// </summary>
        private static bool IsHeicProcessingError(Exception ex)
        {
            return ex is MagickException || 
                   ex.Message.Contains("HEIC", StringComparison.OrdinalIgnoreCase) ||
                   ex.Message.Contains("Magick", StringComparison.OrdinalIgnoreCase) ||
                   ex.Message.Contains("initialization", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 一時ファイル削除の安全実行
        /// </summary>
        private void CleanupTempFile(string tempPath)
        {
            if (File.Exists(tempPath))
            {
                try
                {
                    File.Delete(tempPath);
                }
                catch (Exception deleteEx)
                {
                    _logger.LogWarning(deleteEx, "Failed to delete temporary file: {TempPath}", tempPath);
                }
            }
        }

        private bool IsHeicFile(string imagePath)
        {
            var extension = Path.GetExtension(imagePath).ToLowerInvariant();
            return extension == ".heic" || extension == ".heif";
        }

        private (int width, int height) CalculateOptimalSize(int originalWidth, int originalHeight)
        {
            // A4サイズ（595x842 points）に最適化
            const int maxWidth = 595;
            const int maxHeight = 842;
            
            double widthRatio = (double)maxWidth / originalWidth;
            double heightRatio = (double)maxHeight / originalHeight;
            double ratio = Math.Min(widthRatio, heightRatio);
            
            if (ratio >= 1.0)
            {
                // 元画像がA4より小さい場合はそのまま
                return (originalWidth, originalHeight);
            }
            
            return ((int)(originalWidth * ratio), (int)(originalHeight * ratio));
        }

        private string FormatFileSize(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB" };
            int counter = 0;
            decimal number = bytes;
            
            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }
            
            return $"{number:n1} {suffixes[counter]}";
        }
        
        /// <summary>
        /// エンコーディング問題とメソッド不整合に対応した安全な画像読み込み
        /// attempt to access a missing method、encoding 1512エラー対策
        /// </summary>
        private async Task<Image> LoadImageSafelyAsync(string imagePath)
        {
            _logger.LogDebug($"Starting safe image load for: {imagePath}");
            
            // 事前チェック: 復旧不可能なエラーを早期検出
            if (!await IsRecoverableImageFileAsync(imagePath))
            {
                throw new NotSupportedException($"File cannot be processed as image: {imagePath}");
            }
            
            try
            {
                // Step 1: 基本的な画像読み込みを試行
                _logger.LogDebug($"Attempting basic ImageSharp load for: {imagePath}");
                var image = await Image.LoadAsync(imagePath);
                
                // バグ修正: HEIC変換後ファイルの二重AutoOrient防止
                // ConvertHeicToJpegAsyncで既にAutoOrientが適用されたファイルの検出
                bool isHeicConvertedFile = Path.GetExtension(imagePath).Equals(".jpg", StringComparison.OrdinalIgnoreCase) && 
                                         imagePath.Contains(Path.GetTempPath());
                
                if (!isHeicConvertedFile)
                {
                    // バグ修正：画像読み込み時に必ずAutoOrientを適用
                    // 要件定義書（tmp/DocOrganizer2.2_画像向き修正要件定義書.md）準拠
                    image.Mutate(x => x.AutoOrient());
                    _logger.LogDebug($"AutoOrient applied to: {Path.GetFileName(imagePath)}");
                }
                else
                {
                    _logger.LogDebug($"Skipping AutoOrient for HEIC-converted file: {Path.GetFileName(imagePath)}");
                }
                
                return image;
            }
            catch (Exception ex)
            {
                // 復旧不可能なエラーの早期判定
                if (IsUnrecoverableError(ex))
                {
                    _logger.LogError($"Unrecoverable error for {imagePath}: {ex.GetType().Name} - {ex.Message}");
                    throw new NotSupportedException($"Image file format not supported or corrupted: {imagePath}", ex);
                }
                
                // 具体的な「attempt to access missing method」エラーのキャッチ
                var isMissingMethodError = ex.Message.Contains("attempt to access", StringComparison.OrdinalIgnoreCase) || 
                                         ex.Message.Contains("missing method", StringComparison.OrdinalIgnoreCase) ||
                                         ex.GetType().Name.Contains("MissingMethod");
                
                if (isMissingMethodError)
                {
                    _logger.LogError($"MISSING METHOD ERROR detected for {imagePath}: {ex.GetType().Name} - {ex.Message}");
                }
                else
                {
                    _logger.LogWarning($"Basic ImageSharp load failed for {imagePath}: {ex.GetType().Name} - {ex.Message}");
                }
                
                // Step 2: バイト配列経由での読み込みを試行
                try
                {
                    _logger.LogDebug($"Attempting byte array loading for: {imagePath}");
                    var imageBytes = await File.ReadAllBytesAsync(imagePath);
                    
                    if (imageBytes.Length == 0)
                    {
                        throw new InvalidOperationException("Empty file detected");
                    }
                    
                    using var stream = new MemoryStream(imageBytes);
                    var image = await Image.LoadAsync(stream);
                    
                    // バグ修正：バイト配列から読み込んだ場合もAutoOrientを適用
                    image.Mutate(x => x.AutoOrient());
                    
                    return image;
                }
                catch (Exception innerEx)
                {
                    _logger.LogWarning($"Byte array loading failed for {imagePath}: {innerEx.GetType().Name} - {innerEx.Message}");
                    
                    // Step 3: Magick.NET経由での変換処理
                    try
                    {
                        _logger.LogDebug($"Attempting Magick.NET conversion for: {imagePath}");
                        return await ConvertWithMagickNetAsync(imagePath);
                    }
                    catch (Exception magickEx)
                    {
                        _logger.LogError(magickEx, $"All loading methods failed for {imagePath} - FINAL FAILURE");
                        
                        // 最終的に失敗した場合は、明確な理由と共に例外をスロー
                        throw new ImageProcessingException(
                            $"Cannot process image file: {Path.GetFileName(imagePath)}. " +
                            $"This file may be corrupted, in an unsupported format, or require additional codecs.",
                            ex);
                    }
                }
            }
        }
        
        /// <summary>
        /// Magick.NETを使用した安全な画像変換処理
        /// </summary>
        private async Task<Image> ConvertWithMagickNetAsync(string imagePath)
        {
            var tempJpegPath = Path.GetTempFileName() + ".jpg";
            
            try
            {
                // Magick.NET設定の初期化（動的Ghostscript検出）
                InitializeMagickNetSafely();
                
                using (var magickImage = new MagickImage())
                {
                    // より安全な読み込み設定
                    magickImage.Settings.BackgroundColor = MagickColors.White;
                    magickImage.ColorSpace = ColorSpace.sRGB;
                    
                    // 画像読み込み
                    await Task.Run(() => magickImage.Read(imagePath));
                    
                    // 基本的な検証
                    if (magickImage.Width == 0 || magickImage.Height == 0)
                    {
                        throw new InvalidOperationException("Invalid image dimensions detected");
                    }
                    
                    // JPEG形式で保存
                    magickImage.Format = MagickFormat.Jpeg;
                    magickImage.Quality = 90;
                    magickImage.AutoOrient();
                    
                    await Task.Run(() => magickImage.Write(tempJpegPath));
                }
                
                // ImageSharpで最終読み込み
                var result = await Image.LoadAsync(tempJpegPath);
                
                // バグ修正：Magick.NET変換後もAutoOrientを適用
                result.Mutate(x => x.AutoOrient());
                
                return result;
            }
            finally
            {
                // 一時ファイルクリーンアップ
                if (File.Exists(tempJpegPath))
                {
                    try
                    {
                        File.Delete(tempJpegPath);
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.LogWarning(cleanupEx, $"Failed to delete temporary file: {tempJpegPath}");
                    }
                }
            }
        }
        
        /// <summary>
        /// 復旧可能な画像ファイルかを事前チェック
        /// 開けないファイルの無限ループを防止
        /// </summary>
        private async Task<bool> IsRecoverableImageFileAsync(string imagePath)
        {
            try
            {
                // 基本的なファイル存在・サイズチェック
                if (!File.Exists(imagePath))
                {
                    _logger.LogWarning($"File does not exist: {imagePath}");
                    return false;
                }
                
                var fileInfo = new FileInfo(imagePath);
                if (fileInfo.Length == 0)
                {
                    _logger.LogWarning($"Empty file detected: {imagePath}");
                    return false;
                }
                
                if (fileInfo.Length > 500_000_000) // 500MB制限
                {
                    _logger.LogWarning($"File too large ({fileInfo.Length} bytes): {imagePath}");
                    return false;
                }
                
                // マジックナンバーチェック（ファイル形式の基本検証）
                var header = new byte[12];
                using (var stream = File.OpenRead(imagePath))
                {
                    await stream.ReadAsync(header, 0, header.Length);
                }
                
                return IsKnownImageFormat(header, Path.GetExtension(imagePath));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Pre-check failed for {imagePath}");
                return false;
            }
        }
        
        /// <summary>
        /// マジックナンバーによる画像形式判定
        /// </summary>
        private bool IsKnownImageFormat(byte[] header, string extension)
        {
            if (header.Length < 4) return false;
            
            // PNG: 89 50 4E 47
            if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47)
                return true;
                
            // JPEG: FF D8 FF
            if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
                return true;
                
            // HEIC: ftypheic or ftypmif1
            if (header.Length >= 12)
            {
                var heicSignature = Encoding.ASCII.GetString(header, 4, 8);
                if (heicSignature.Contains("heic") || heicSignature.Contains("mif1"))
                    return true;
            }
            
            // BMP: 42 4D
            if (header[0] == 0x42 && header[1] == 0x4D)
                return true;
                
            // GIF: 47 49 46 38
            if (header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x38)
                return true;
                
            // TIFF: 49 49 2A 00 or 4D 4D 00 2A
            if ((header[0] == 0x49 && header[1] == 0x49 && header[2] == 0x2A && header[3] == 0x00) ||
                (header[0] == 0x4D && header[1] == 0x4D && header[2] == 0x00 && header[3] == 0x2A))
                return true;
                
            // WebP: 52 49 46 46...57 45 42 50
            if (header.Length >= 12 && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 &&
                header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50)
                return true;
            
            // 拡張子による補完判定
            var ext = extension.ToLowerInvariant();
            if (SupportedExtensions.Contains(ext))
            {
                _logger.LogDebug($"File format determined by extension: {ext}");
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 復旧不可能なエラーの判定
        /// </summary>
        private bool IsUnrecoverableError(Exception ex)
        {
            // ファイルアクセス系エラー
            if (ex is UnauthorizedAccessException || 
                ex is DirectoryNotFoundException || 
                ex is FileNotFoundException ||
                ex is PathTooLongException)
            {
                return true;
            }
            
            // 完全に破損したファイル
            if (ex.Message.Contains("corrupted", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("invalid", StringComparison.OrdinalIgnoreCase) ||
                ex.Message.Contains("not supported", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            
            // OutOfMemoryException（巨大ファイル）
            if (ex is OutOfMemoryException)
            {
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// HEIC処理クラッシュ修正: 安全なMagick.NET初期化処理
        /// クラッシュ防止とスレッドセーフな実装
        /// </summary>
        private bool InitializeMagickNetSafely()
        {
            lock (_initLock)
            {
                if (_magickNetInitialized)
                {
                    _logger.LogDebug("Magick.NET already initialized");
                    return _magickNetAvailable;
                }

                try
                {
                    _logger.LogDebug("Attempting Magick.NET initialization...");
                    
                    // Ghostscriptの動的検出
                    var ghostscriptPath = FindGhostscriptPath();
                    if (!string.IsNullOrEmpty(ghostscriptPath))
                    {
                        _logger.LogDebug($"Setting Ghostscript directory: {ghostscriptPath}");
                        MagickNET.SetGhostscriptDirectory(ghostscriptPath);
                    }
                    else
                    {
                        _logger.LogWarning("Ghostscript not found - HEIC processing may be limited");
                    }
                    
                    // 安全な初期化実行
                    MagickNET.Initialize();
                    
                    // 初期化成功の確認
                    var formatCount = MagickNET.SupportedFormats?.Count() ?? 0;
                    if (formatCount > 0)
                    {
                        _logger.LogInformation($"Magick.NET initialized successfully with {formatCount} supported formats");
                        _magickNetInitialized = true;
                        _magickNetAvailable = true;
                        return true;
                    }
                    else
                    {
                        _logger.LogWarning("Magick.NET initialized but no formats available");
                        _magickNetInitialized = true;
                        _magickNetAvailable = false;
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Magick.NET initialization failed completely - HEIC processing disabled");
                    _magickNetInitialized = true;
                    _magickNetAvailable = false;
                    return false;
                }
            }
        }
        
        /// <summary>
        /// 環境に応じたGhostscriptパスの動的検出
        /// </summary>
        private string? FindGhostscriptPath()
        {
            try
            {
                var possiblePaths = new[]
                {
                    @"C:\Program Files\gs",
                    @"C:\Program Files (x86)\gs",
                    Environment.GetEnvironmentVariable("GS_BIN_PATH"),
                    Environment.GetEnvironmentVariable("GHOSTSCRIPT_BIN"),
                };
                
                foreach (var basePath in possiblePaths.Where(p => !string.IsNullOrEmpty(p)))
                {
                    if (!Directory.Exists(basePath)) continue;
                    
                    // 最新バージョンのGhostscriptを検出
                    var versions = Directory.GetDirectories(basePath, "gs*")
                        .Where(d => Directory.Exists(Path.Combine(d, "bin")))
                        .OrderByDescending(d => d)
                        .ToArray();
                    
                    if (versions.Any())
                    {
                        var latestVersion = Path.Combine(versions.First(), "bin");
                        _logger.LogDebug($"Found Ghostscript: {latestVersion}");
                        return latestVersion;
                    }
                }
                
                _logger.LogDebug("Ghostscript not found - some features may be limited");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during Ghostscript detection");
                return null;
            }
        }

        /// <summary>
        /// 画像の向きを自動検出して補正する
        /// </summary>
        /// <param name="imagePath">画像ファイルのパス</param>
        /// <returns>補正に必要な回転角度（0, 90, 180, 270）</returns>
        private async Task<int> DetectAndCorrectOrientationAsync(string imagePath)
        {
            try
            {
                // バグ報告ドキュメント（tmp/DocOrganizer2.2_画像向き修正要件定義書.md）に基づく修正
                // 縦向き画像が横向きで表示される問題を解決
                
                _logger.LogDebug("Detecting orientation for {ImagePath}", Path.GetFileName(imagePath));
                
                // 画像を一時的に読み込んで向き情報を取得
                using var tempImage = await LoadImageForOrientationCheckAsync(imagePath);
                
                // ImageSharpは自動的にEXIF情報を読み取り、AutoOrient()で正しい向きに変換する
                // ただし、ここでは回転角度のみを検出し、実際の回転は行わない
                var originalWidth = tempImage.Width;
                var originalHeight = tempImage.Height;
                
                // AutoOrient()を適用して向きを補正
                tempImage.Mutate(x => x.AutoOrient());
                
                // 向きが変わったかチェック
                var rotatedWidth = tempImage.Width;
                var rotatedHeight = tempImage.Height;
                
                int rotation = 0;
                if (originalWidth == rotatedHeight && originalHeight == rotatedWidth)
                {
                    // 幅と高さが入れ替わった = 90度または270度回転
                    // ImageSharpのAutoOrientは正しい向きにするので、ここでは回転不要
                    rotation = 0;
                }
                
                _logger.LogInformation("Orientation detection complete for {ImagePath}: rotation={Rotation}", 
                    Path.GetFileName(imagePath), rotation);
                
                // 画像読み込み処理では、ImageSharpのAutoOrient()に任せるため、
                // ここでは常に0を返す（AutoOrientが自動的に処理するため）
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to detect orientation for {ImagePath}", imagePath);
                return 0; // エラー時は回転なし
            }
        }
        
        /// <summary>
        /// 向きチェック用の画像読み込み（メモリ効率を考慮）
        /// </summary>
        private async Task<Image> LoadImageForOrientationCheckAsync(string imagePath)
        {
            try
            {
                // HEICファイルの場合は先に変換
                if (IsHeicFile(imagePath))
                {
                    var tempJpegPath = await ConvertHeicToJpegAsync(imagePath);
                    try
                    {
                        return await Image.LoadAsync(tempJpegPath);
                    }
                    finally
                    {
                        if (File.Exists(tempJpegPath))
                        {
                            File.Delete(tempJpegPath);
                        }
                    }
                }
                
                // 通常の画像ファイル
                return await Image.LoadAsync(imagePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load image for orientation check: {ImagePath}", imagePath);
                throw;
            }
        }

        /// <summary>
        /// EXIFデータから回転情報を取得
        /// </summary>
        private int GetExifRotation(string imagePath)
        {
            try
            {
                using var bitmap = SkiaSharp.SKBitmap.Decode(imagePath);
                if (bitmap == null) return 0;
                
                // SkiaSharpでEXIF情報は自動的に適用されるため、
                // 既に正しい向きになっている可能性が高い
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to read EXIF data from {ImagePath}", imagePath);
                return 0;
            }
        }

        /// <summary>
        /// 画像の縦横比を取得
        /// </summary>
        private float GetImageAspectRatio(string imagePath)
        {
            try
            {
                using var bitmap = SkiaSharp.SKBitmap.Decode(imagePath);
                if (bitmap == null || bitmap.Width == 0) return 0;
                
                return (float)bitmap.Height / bitmap.Width;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to get aspect ratio from {ImagePath}", imagePath);
                return 0;
            }
        }
    }
}