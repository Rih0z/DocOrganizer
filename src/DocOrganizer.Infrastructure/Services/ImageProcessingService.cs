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
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;
using SkiaSharp;
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
        private readonly IRotationService _rotationService;
        private static readonly string[] SupportedExtensions = 
        {
            ".jpg", ".jpeg", ".png", ".heic", ".heif", ".bmp", ".tiff", ".gif", ".webp"
        };

        // HEIC処理クラッシュ修正: 安全な初期化管理
        private bool _magickNetInitialized = false;
        private bool _magickNetAvailable = false;
        private readonly object _initLock = new object();

        public ImageProcessingService(ILogger<ImageProcessingService> logger, IPdfService pdfService, IRotationService rotationService)
        {
            _logger = logger;
            _pdfService = pdfService;
            _rotationService = rotationService;
            
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

                // ⭐完全修正: EXIF自動回転を完全無効化 - ピクセルそのまま表示
                var correctedRotation = 0; // EXIF自動回転を無効化してピクセルそのまま表示
                _logger.LogInformation("[ImageProcessingService] EXIF自動回転無効化 - 強制0度回転 for {ImagePath}", 
                    Path.GetFileName(imagePath));
                
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
            System.Diagnostics.Debug.WriteLine("[ConvertImagesToPdfAsync] Starting conversion");
            
            try
            {
                // シンプルな基本検証のみ
                var validPaths = imagePaths
                    .Where(path => !string.IsNullOrEmpty(path) && File.Exists(path))
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"[ConvertImagesToPdfAsync] Valid paths: {validPaths.Count}");

                if (!validPaths.Any())
                {
                    throw new ArgumentException("No valid image files found");
                }

                // 最小限のPDFドキュメント作成
                var pdfDocument = new PdfDocument()
                {
                    IsTemporaryFromImages = true,
                    FilePath = Path.Combine(Path.GetTempPath(), $"images_{DateTime.Now:yyyyMMddHHmmss}.pdf")
                };
                
                pdfDocument.SourceImagePaths.AddRange(validPaths);
                
                int pageNumber = 1;
                foreach (var imagePath in validPaths)
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"[ConvertImagesToPdfAsync] Processing: {Path.GetFileName(imagePath)}");
                        
                        // ⭐修正: EXIF自動回転を完全無効化（OCR以外の自動回転機能を無効）
System.Diagnostics.Debug.WriteLine($"[ConvertImagesToPdfAsync] EXIF自動回転無効化モード: {Path.GetFileName(imagePath)}");

// 回転なしでページ作成（元画像をそのまま使用）
var page = new PdfPage(pageNumber++)
{
    SourceImagePath = imagePath,
    Rotation = 0  // ⭐修正: 自動回転を完全無効化
};
                        
                        // 固定サイズ設定
                        page.SetDimensions(595, 842); // A4サイズ
                        
                        pdfDocument.AddPage(page);
                        
                        System.Diagnostics.Debug.WriteLine($"[ConvertImagesToPdfAsync] Added page {pageNumber - 1} with rotation 0° (auto-rotation disabled)");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ConvertImagesToPdfAsync] Skipped {imagePath}: {ex.Message}");
                        // 個別エラーは無視して継続
                    }
                }
                
                if (pdfDocument.Pages.Count == 0)
                {
                    throw new InvalidOperationException("No pages could be created");
                }
                
                System.Diagnostics.Debug.WriteLine($"[ConvertImagesToPdfAsync] Created PDF with {pdfDocument.Pages.Count} pages");
                
                pdfDocument.ClearModifiedFlag();
                return pdfDocument;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ConvertImagesToPdfAsync] Error: {ex.Message}");
                throw new InvalidOperationException($"Failed to convert images: {ex.Message}", ex);
            }
        }

        public async Task<byte[]> GetImageThumbnailAsync(string imagePath, int width = 150, int height = 150)
        {
            // デフォルトは0度回転
            return await GetImageThumbnailAsync(imagePath, width, height, 0);
        }

        /// <summary>
        /// ★修正案C: 回転角度を指定してサムネイル生成
        /// </summary>
        public async Task<byte[]> GetImageThumbnailAsync(string imagePath, int width = 150, int height = 150, int rotationDegrees = 0)
        {
            try
            {
                if (!await IsValidImageAsync(imagePath))
                {
                    throw new ArgumentException($"Invalid image file: {imagePath}");
                }

                // 🚀 Phase 1最適化: HEIC処理の統一化と2重変換排除
                if (IsHeicFile(imagePath))
                {
                    return await GetHeicThumbnailOptimizedAsync(imagePath, width, height, rotationDegrees);
                }

                // ★統一サービス使用: EXIF自動回転を無効化してロード
                using var skBitmap = await _rotationService.LoadImageWithoutAutoRotationAsync(imagePath);
                _logger.LogInformation("[ImageProcessingService] EXIF無視読み込み完了 - Size: {Width}x{Height} for {ImagePath}", 
                    skBitmap?.Width ?? 0, skBitmap?.Height ?? 0, Path.GetFileName(imagePath));
                
                // SKBitmapをImageSharpのImageに変換
                using var skImage = SKImage.FromBitmap(skBitmap);
                using var skData = skImage.Encode(SKEncodedImageFormat.Png, 100);
                using var stream = new MemoryStream(skData.ToArray());
                
                // ⭐完全修正: ImageSharpでもEXIF自動回転を無効化
                // ストリームから読み込みでEXIF無視（SkiaSharpから変換済みなので既にEXIF適用済み）
                using var image = await Image.LoadAsync(stream);
                
                // 既にSkiaSharpでEXIF無視読み込み済みなので、ImageSharpでは追加処理不要
                _logger.LogDebug($"ImageSharp loaded from SkiaSharp stream (EXIF already handled): {Path.GetFileName(imagePath)}");
                
                // ⭐完全無効化: すべての回転処理を無効化（手動回転も含む）
                // Windowsプレビューで正しく表示される画像をそのまま使用
                _logger.LogDebug($"All rotation processing disabled - using image as-is: {Path.GetFileName(imagePath)}");
                
                // リサイズ処理
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(width, height),
                    Mode = ResizeMode.Max
                }));

                using var ms = new MemoryStream();
                await image.SaveAsJpegAsync(ms, new JpegEncoder { Quality = 80 });
                
                System.Diagnostics.Debug.WriteLine($"[GetImageThumbnailAsync] 修正版C - 回転角度 {rotationDegrees}度 適用: {Path.GetFileName(imagePath)}");
                return ms.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate thumbnail: {ImagePath}", imagePath);
                throw;
            }
        }

        /// <summary>
        /// HEIC画像専用の最適化サムネイル生成（2重変換排除・高速化）
        /// </summary>
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        private async Task<byte[]> GetHeicThumbnailOptimizedAsync(string heicPath, int width, int height, int rotationDegrees = 0)
        {
            try
            {
                _logger.LogDebug($"[HEIC最適化] サムネイル生成開始: {Path.GetFileName(heicPath)} ({width}x{height})");

                // 🚀 Phase 3最適化: Windows標準対応優先・段階的フォールバック
                var supportLevel = GetHeicSupportLevel();
                
                switch (supportLevel)
                {
                    case HeicSupportLevel.WindowsNative:
                        return await GenerateHeicThumbnailWithWicAsync(heicPath, width, height);
                        
                    case HeicSupportLevel.MagickNet:
                        return await GenerateHeicThumbnailWithMagickAsync(heicPath, width, height);
                        
                    case HeicSupportLevel.None:
                    default:
                        throw new NotSupportedException(
                            $"HEIC not supported - install Microsoft HEIF Extensions from Microsoft Store. File: {Path.GetFileName(heicPath)}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[HEIC最適化] サムネイル生成エラー: {HeicPath}", heicPath);
                
                // HEICエラーの特別処理
                if (IsHeicProcessingError(ex))
                {
                    throw new ImageProcessingException($"HEIC thumbnail generation failed: {ex.Message}", ex);
                }
                
                throw;
            }
        }

        /// <summary>
        /// Windows標準WIC経由HEIC処理（最高性能）
        /// </summary>
        private async Task<byte[]> GenerateHeicThumbnailWithWicAsync(string heicPath, int width, int height)
        {
            try
            {
                _logger.LogDebug($"[WIC-HEIC] Windows標準処理開始: {Path.GetFileName(heicPath)}");
                
                // TODO: Phase 3実装予定 - WIC Interop Library使用
                // 現在はMagick.NETにフォールバック
                _logger.LogWarning("[WIC-HEIC] Windows標準処理は次期実装予定 - Magick.NETで処理");
                return await GenerateHeicThumbnailWithMagickAsync(heicPath, width, height);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WIC-HEIC] Windows標準処理エラー - Magick.NETにフォールバック");
                return await GenerateHeicThumbnailWithMagickAsync(heicPath, width, height);
            }
        }
        
        /// <summary>
        /// Magick.NET経由HEIC処理（安定版・現行方式）
        /// </summary>
        private async Task<byte[]> GenerateHeicThumbnailWithMagickAsync(string heicPath, int width, int height)
        {
            // HEIC処理可能性の事前確認
            if (!IsHeicProcessingAvailable())
            {
                throw new NotSupportedException($"Magick.NET HEIC processing unavailable. File: {heicPath}");
            }

            using (var magickImage = new MagickImage())
            {
                // HEIC読み込み設定
                magickImage.Settings.BackgroundColor = MagickColors.White;
                magickImage.ColorSpace = ColorSpace.sRGB;
                
                // 非同期での読み込み
                await Task.Run(() => 
{
    magickImage.Read(heicPath);
    // ⭐修正: ImageMagickのAutoOrient機能を明示的に無効化
    magickImage.Orientation = ImageMagick.OrientationType.TopLeft; // 強制的にOrientation=1に設定
});
                
                // 基本検証
                if (magickImage.Width == 0 || magickImage.Height == 0)
                {
                    throw new InvalidOperationException($"Invalid HEIC dimensions: {magickImage.Width}x{magickImage.Height}");
                }
                
                _logger.LogDebug($"[Magick-HEIC] 原画像サイズ: {magickImage.Width}x{magickImage.Height}");
                
                // 🚀 直接サムネイル生成（中間JPEG作成なし）
                // ★修正: AutoOrient重複削除 - 統一的な向き補正はLoadImageSafelyAsyncで実行
                magickImage.Format = MagickFormat.Jpeg;
                magickImage.Quality = 80;
                
                // サイズ調整
                var geometry = new MagickGeometry((uint)Math.Max(1, width), (uint)Math.Max(1, height))
                {
                    IgnoreAspectRatio = false
                };
                magickImage.Resize(geometry);
                
                // 直接バイト配列として取得（ファイル作成なし）
                var thumbnailBytes = magickImage.ToByteArray();
                
                _logger.LogDebug($"[Magick-HEIC] サムネイル生成完了: {thumbnailBytes.Length} bytes");
                return thumbnailBytes;
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
                await Task.Run(() => 
{
    magickImage.Read(imagePath);
    // ⭐修正: ImageMagickのAutoOrient機能を明示的に無効化
    magickImage.Orientation = ImageMagick.OrientationType.TopLeft; // 強制的にOrientation=1に設定
});
                
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
        /// Windows標準HEIC拡張機能の検出（2025年ベストプラクティス）
        /// </summary>
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        private bool CheckWindowsHeicSupport()
        {
            try
            {
                // Windows 11 22H2以降の自動判定
                var osVersion = Environment.OSVersion.Version;
                var isWindows11 = osVersion.Major >= 10 && osVersion.Build >= 22621; // Windows 11 22H2
                
                if (isWindows11)
                {
                    _logger.LogDebug("Windows 11 22H2+ detected - HEIC native support available");
                    return CheckHeicExtensionInstalled();
                }
                
                // Windows 10の場合は拡張機能チェックのみ
                _logger.LogDebug("Windows 10 detected - checking HEIC extensions");
                return CheckHeicExtensionInstalled();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to check Windows HEIC support");
                return false;
            }
        }
        
        /// <summary>
        /// Microsoft HEIF画像拡張機能のインストール状況確認
        /// </summary>
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        private bool CheckHeicExtensionInstalled()
        {
            try
            {
                // レジストリチェック: HEIF Decoder
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Classes\CLSID\{7ED96837-96F0-4812-B211-F13C24117ED3}");
                    
                if (key != null)
                {
                    _logger.LogDebug("Microsoft HEIF Image Extensions detected via registry");
                    return true;
                }
                
                // WIC Codec確認: HEIC Decoder CLSID
                var heicDecoderClsid = new Guid("7ED96837-96F0-4812-B211-F13C24117ED3");
                
                // COM オブジェクト作成テスト（軽量チェック）
                var comType = Type.GetTypeFromCLSID(heicDecoderClsid, false);
                if (comType != null)
                {
                    _logger.LogDebug("WIC HEIC Decoder CLSID confirmed");
                    return true;
                }
                
                _logger.LogWarning("Microsoft HEIF Image Extensions not found - install from Microsoft Store");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to verify HEIC extension installation");
                return false;
            }
        }
        
        /// <summary>
        /// HEIC処理能力の総合判定（Windows拡張機能優先・Magick.NETフォールバック）
        /// </summary>
        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        private HeicSupportLevel GetHeicSupportLevel()
        {
            // 1. Windows標準対応チェック（最優先）
            if (CheckWindowsHeicSupport())
            {
                _logger.LogInformation("HEIC Support: Windows Native (Optimal)");
                return HeicSupportLevel.WindowsNative;
            }
            
            // 2. Magick.NET対応チェック（フォールバック）
            if (IsHeicProcessingAvailable())
            {
                _logger.LogInformation("HEIC Support: Magick.NET (Fallback)");
                return HeicSupportLevel.MagickNet;
            }
            
            // 3. 対応不可
            _logger.LogWarning("HEIC Support: None - Please install Microsoft HEIF Extensions");
            return HeicSupportLevel.None;
        }
        
        /// <summary>
        /// HEIC対応レベル定義
        /// </summary>
        private enum HeicSupportLevel
        {
            None = 0,           // 対応不可
            MagickNet = 1,      // Magick.NET経由（現在の方式）
            WindowsNative = 2   // Windows標準（最適）
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
                    await Task.Run(() => 
{
    magickImage.Read(heicPath);
    // ⭐修正: ImageMagickのAutoOrient機能を明示的に無効化
    magickImage.Orientation = ImageMagick.OrientationType.TopLeft; // 強制的にOrientation=1に設定
});
                    
                    // 基本的な検証
                    if (magickImage.Width == 0 || magickImage.Height == 0)
                    {
                        throw new InvalidOperationException($"Invalid HEIC dimensions: {magickImage.Width}x{magickImage.Height}");
                    }
                    
                    _logger.LogDebug($"HEIC dimensions: {magickImage.Width}x{magickImage.Height}");
                    
                    // JPEG変換設定
                    magickImage.Format = MagickFormat.Jpeg;
                    magickImage.Quality = 95;
                    
                    // ★修正: AutoOrient重複削除 - HEIC変換でもAutoOrient統一化
                    // 向きの自動補正は後続のLoadImageSafelyAsyncで統一処理
                    
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
            var extension = Path.GetExtension(imagePath);
            return extension.Equals(".heic", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".heif", StringComparison.OrdinalIgnoreCase);
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
                // ⭐統一回転サービス使用: EXIF自動回転を完全無効化
                _logger.LogDebug($"[LoadImageSafelyAsync] RotationServiceでEXIF無視読み込み開始: {imagePath}");
                
                // SkiaSharpで読み込み後、ImageSharpに変換
                using var skBitmap = await _rotationService.LoadImageWithoutAutoRotationAsync(imagePath);
                
                // SKBitmapをImageSharpのImageに変換
                using var skImage = SKImage.FromBitmap(skBitmap);
                using var skData = skImage.Encode(SKEncodedImageFormat.Png, 100);
                using var stream = new MemoryStream(skData.ToArray());
                
                // ⭐完全修正: ImageSharpでもEXIF自動回転を無効化  
                // ストリームから読み込みでEXIF無視（SkiaSharpから変換済みなので既にEXIF適用済み）
                var image = await Image.LoadAsync(stream);
                
                // 既にSkiaSharpでEXIF無視読み込み済みなので、ImageSharpでは追加処理不要
                _logger.LogDebug($"ImageSharp loaded from SkiaSharp stream (EXIF already handled): {Path.GetFileName(imagePath)}");
                
                _logger.LogDebug($"[LoadImageSafelyAsync] 完了: {Path.GetFileName(imagePath)} - {image.Width}x{image.Height}");
                
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
                
                _logger.LogWarning($"Basic ImageSharp load failed for {imagePath}: {ex.GetType().Name} - {ex.Message}");
                
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
                    
                    // ⭐完全修正: ImageSharpでもEXIF自動回転を無効化
                    // バイト配列ストリームからの読み込みではEXIFメタデータが既に除去されている
                    var image = await Image.LoadAsync(stream);
                    
                    // バイト配列読み込みではEXIF情報は含まれていない
                    _logger.LogDebug($"ImageSharp loaded from byte stream (no EXIF data): {Path.GetFileName(imagePath)}");
                    
                    // ⭐廃止: バイト配列読み込みでもAutoOrientは無効化
                    _logger.LogDebug($"AutoOrient disabled for byte-loaded image: {Path.GetFileName(imagePath)}");
                    
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
                    await Task.Run(() => 
{
    magickImage.Read(imagePath);
    // ⭐修正: ImageMagickのAutoOrient機能を明示的に無効化
    magickImage.Orientation = ImageMagick.OrientationType.TopLeft; // 強制的にOrientation=1に設定
});
                    
                    // 基本的な検証
                    if (magickImage.Width == 0 || magickImage.Height == 0)
                    {
                        throw new InvalidOperationException("Invalid image dimensions detected");
                    }
                    
                    // JPEG形式で保存
                    magickImage.Format = MagickFormat.Jpeg;
                    magickImage.Quality = 90;
                    
                    // ★修正: AutoOrient重複削除 - MagickNet内でのAutoOrient削除
                    // 向きの自動補正はImageSharpのLoadImageSafelyAsyncで統一して行う
                    
                    await Task.Run(() => magickImage.Write(tempJpegPath));
                }
                
                // ImageSharpで最終読み込み
                // ⭐完全修正: ImageSharpでもEXIF自動回転を無効化
                // Magick.NETで既にOrientationType.TopLeftに強制設定済み
                var result = await Image.LoadAsync(tempJpegPath);
                
                // Magick.NETでEXIF OrientationをTopLeftに強制設定済みなので追加処理不要
                _logger.LogDebug($"ImageSharp loaded Magick.NET processed file (Orientation=TopLeft): {Path.GetFileName(tempJpegPath)}");
                
                // ★修正: AutoOrient重複削除 - ImageSharpでの重複AutoOrientも削除
                // LoadImageSafelyAsyncで既に適用済みのため不要
                
                _logger.LogDebug($"MagickNet conversion completed without AutoOrient duplication: {Path.GetFileName(imagePath)}");
                
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
                _logger.LogDebug("Detecting orientation for {ImagePath}", Path.GetFileName(imagePath));
                
                // 画像を読み込み（AutoOrientは適用せずEXIF情報のみ取得）
                // ⭐重要: EXIF Orientation検出のためEXIFメタデータが必要
                // ここではOrientationのみ取得してAutoOrientは実行しない
                using var image = await Image.LoadAsync(imagePath);
                
                // EXIFメタデータは読み込むが自動回転は実行しない設計
                _logger.LogDebug($"ImageSharp loaded for EXIF detection (no AutoOrient): {Path.GetFileName(imagePath)}");
                
                // EXIF Orientationを直接取得
                var orientation = GetExifOrientation(image);
                
                // Orientationに基づく回転角度を計算（⭐修正: 回転方向を逆転）
                var rotationDegrees = orientation switch
                {
                    1 => 0,   // Normal - 回転なし
                    2 => 0,   // Flip horizontal - 反転のみ（回転なし）  
                    3 => 180, // Rotate 180°
                    4 => 0,   // Flip vertical - 反転のみ（回転なし）
                    5 => 0,   // Transpose - 複合変換（回転なし）
                    6 => 270, // ⭐修正: 90度時計回りが必要 → 270度で実装（90度反時計回り相当）
                    7 => 0,   // Transverse - 複合変換（回転なし）  
                    8 => 90,  // ⭐修正: 90度反時計回りが必要 → 90度で実装
                    _ => 0    // 未知の値は回転なし
                };
                
                _logger.LogInformation("Orientation detection complete for {ImagePath}: EXIF={Orientation}, RequiredRotation={Degrees}°", 
                    Path.GetFileName(imagePath), orientation, rotationDegrees);
                
                // ⭐修正: 計算した回転角度を正しく返す（0固定を廃止）
                return rotationDegrees;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to detect orientation for {ImagePath}", imagePath);
                return 0; // エラー時は回転なし
            }
        }

        /// <summary>
        /// ★統一回転処理 - RotationServiceに委譲
        /// </summary>
        public SkiaSharp.SKBitmap RotateImage(SkiaSharp.SKBitmap source, int rotationDegrees)
        {
            // 統一RotationServiceに委譲（ログ出力・統計機能付き）
            return _rotationService.RotateImage(source, rotationDegrees, "ImageProcessingService.RotateImage");
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
                        // ⭐完全修正: ImageSharpでもEXIF自動回転を無効化
                        // HEIC→JPEG変換済みファイルを読み込み（既にEXIF処理済み）
                        var heicResult = await Image.LoadAsync(tempJpegPath);
                        
                        // HEIC変換済みJPEGファイルではEXIF Orientationは適切に処理済み
                        _logger.LogDebug($"ImageSharp loaded HEIC-converted JPEG: {Path.GetFileName(tempJpegPath)}");
                        return heicResult;
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
                // ⭐完全修正: ImageSharpでもEXIF自動回転を無効化
                // 通常画像ファイルの直接読み込み - AutoOrientは手動制御
                var result = await Image.LoadAsync(imagePath);
                
                // 通常画像読み込み完了（AutoOrientは実行しない）
                _logger.LogDebug($"ImageSharp loaded standard image (no AutoOrient): {Path.GetFileName(imagePath)}");
                return result;
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
        /// <summary>
        /// ImageSharpを使用してEXIF Orientationを取得
        /// </summary>
        /// <param name="image">読み込み済みのImageSharp画像</param>
        /// <returns>EXIF Orientation値（1=Normal, 3=180°, 6=90°CW, 8=90°CCW, etc.）</returns>
        private int GetExifOrientation(Image image)
        {
            try
            {
                // ImageSharpのEXIFプロファイルからOrientation情報を取得
                if (image.Metadata?.ExifProfile != null)
                {
                    // ImageSharp 3.x API: TryGetValueを使用
                    if (image.Metadata.ExifProfile.TryGetValue(SixLabors.ImageSharp.Metadata.Profiles.Exif.ExifTag.Orientation, out var orientationValue) && orientationValue != null)
                    {
                        var orientation = (int)orientationValue.Value;
                        _logger.LogDebug($"EXIF Orientation read successfully: {orientation}");
                        return orientation;
                    }
                }
                
                _logger.LogDebug("No EXIF Orientation found, assuming normal (1)");
                return 1; // デフォルト: Normal
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to read EXIF Orientation, assuming normal (1)");
                return 1; // エラー時はNormalとして扱う
            }
        }
        
        /// <summary>
        /// ファイルパスから直接EXIF Orientationを取得（バックアップ用）
        /// </summary>
        /// <param name="imagePath">画像ファイルパス</param>
        /// <returns>EXIF Orientation値</returns>
        private int GetExifOrientationFromFile(string imagePath)
        {
            try
            {
                using var image = Image.Load(imagePath);
                return GetExifOrientation(image);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to read EXIF Orientation from file: {imagePath}");
                return 1;
            }
        }

        /// <summary>
        /// 画像の縦横比を取得
        /// </summary>
        private float GetImageAspectRatio(string imagePath)
        {
            try
            {
                // ⭐修正: EXIF Orientationを無視してデコード
SkiaSharp.SKBitmap bitmap;
using (var stream = File.OpenRead(imagePath))
{
    // ⭐重要修正: SkiaSharpのEXIF Orientation自動適用を無効化
    using var codec = SkiaSharp.SKCodec.Create(stream);
    bitmap = SkiaSharp.SKBitmap.Decode(codec, new SkiaSharp.SKImageInfo(codec.Info.Width, codec.Info.Height));
}
                if (bitmap == null || bitmap.Width == 0) return 0;
                
                return (float)bitmap.Height / bitmap.Width;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to get aspect ratio from {ImagePath}", imagePath);
                return 0;
            }
        }

        /// <summary>
        /// 高品質プレビュー生成（文字視認性を優先した処理）
        /// HEICファイル専用の高解像度プレビュー生成
        /// </summary>
        /// <param name="imagePath">画像ファイルパス</param>
        /// <param name="maxWidth">最大幅（デフォルト: 1200px）</param>
        /// <param name="maxHeight">最大高さ（デフォルト: 1600px）</param>
        /// <returns>高品質プレビュー画像のSKBitmap</returns>
        public async Task<SkiaSharp.SKBitmap?> GenerateHighQualityPreviewAsync(string imagePath, int maxWidth = 1200, int maxHeight = 1600)
        {
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
            {
                return null;
            }

            try
            {
                _logger.LogDebug($"Generating high quality preview: {imagePath} (Max: {maxWidth}x{maxHeight})");

                var extension = Path.GetExtension(imagePath).ToLowerInvariant();
                
                // HEICファイルの場合は高品質変換処理
                if (extension == ".heic" || extension == ".heif")
                {
                    return await GenerateHeicHighQualityPreviewAsync(imagePath, maxWidth, maxHeight);
                }
                
                // 一般画像ファイルの高品質プレビュー
                return await GenerateStandardHighQualityPreviewAsync(imagePath, maxWidth, maxHeight);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate high quality preview for {ImagePath}", imagePath);
                return null;
            }
        }

        /// <summary>
        /// HEIC専用高品質プレビュー生成
        /// </summary>
        private async Task<SkiaSharp.SKBitmap?> GenerateHeicHighQualityPreviewAsync(string heicPath, int maxWidth, int maxHeight)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var magickImage = new ImageMagick.MagickImage(heicPath);
// ⭐修正: ImageMagick AutoOrient完全無効化
magickImage.Orientation = ImageMagick.OrientationType.TopLeft;
                    
                    // 高品質設定
                    magickImage.Quality = 98; // 高品質（文字視認性重視）
                    magickImage.Density = new ImageMagick.Density(300, 300); // 高DPI
                    
                    // 向き自動補正
                    // ★修正: AutoOrient重複削除 - 最後の残りの重複箇所も統一化
                    
                    // アスペクト比を維持してリサイズ
                    var originalWidth = (int)magickImage.Width;
                    var originalHeight = (int)magickImage.Height;
                    
                    // サイズ制限に基づいたリサイズ計算
                    int newWidth = originalWidth;
                    int newHeight = originalHeight;
                    
                    if (originalWidth > maxWidth || originalHeight > maxHeight)
                    {
                        double scaleX = (double)maxWidth / originalWidth;
                        double scaleY = (double)maxHeight / originalHeight;
                        double scale = Math.Min(scaleX, scaleY);
                        
                        newWidth = (int)(originalWidth * scale);
                        newHeight = (int)(originalHeight * scale);
                    }
                    
                    // 高品質リサイズ
                    magickImage.FilterType = ImageMagick.FilterType.Lanczos;
                    magickImage.Resize((uint)newWidth, (uint)newHeight);
                    
                    // シャープニング（文字の鮮鋭化）
                    magickImage.Sharpen();
                    
                    // SKBitmapに変換
                    using var stream = new MemoryStream();
                    magickImage.Format = ImageMagick.MagickFormat.Png; // 無圧縮PNG
                    magickImage.Write(stream);
                    stream.Position = 0;
                    
                    // ⭐修正: EXIF無視でデコード（HEIC処理でも統一）
// ⭐重要修正: SkiaSharpのEXIF Orientation自動適用を無効化
using var codec = SkiaSharp.SKCodec.Create(stream);
var skBitmap = SkiaSharp.SKBitmap.Decode(codec, new SkiaSharp.SKImageInfo(codec.Info.Width, codec.Info.Height));
                    _logger.LogDebug($"HEIC high quality preview generated: {newWidth}x{newHeight}");
                    
                    return skBitmap;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate HEIC high quality preview: {HeicPath}", heicPath);
                    return null;
                }
            });
        }

        /// <summary>
        /// 一般画像ファイル用高品質プレビュー生成
        /// </summary>
        private async Task<SkiaSharp.SKBitmap?> GenerateStandardHighQualityPreviewAsync(string imagePath, int maxWidth, int maxHeight)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // ⭐修正: EXIF Orientationを無視してデコード
                    SkiaSharp.SKBitmap originalBitmap;
                    using (var stream = File.OpenRead(imagePath))
                    {
                        // EXIF情報を無視してraw画像データをデコード
                        // ⭐重要修正: SkiaSharpのEXIF Orientation自動適用を無効化
                        using var codec = SkiaSharp.SKCodec.Create(stream);
                        originalBitmap = SkiaSharp.SKBitmap.Decode(codec, new SkiaSharp.SKImageInfo(codec.Info.Width, codec.Info.Height));
                    }
                    
                    if (originalBitmap == null) return null;
                    
                    // アスペクト比を維持してリサイズ
                    int newWidth = originalBitmap.Width;
                    int newHeight = originalBitmap.Height;
                    
                    if (originalBitmap.Width > maxWidth || originalBitmap.Height > maxHeight)
                    {
                        double scaleX = (double)maxWidth / originalBitmap.Width;
                        double scaleY = (double)maxHeight / originalBitmap.Height;
                        double scale = Math.Min(scaleX, scaleY);
                        
                        newWidth = (int)(originalBitmap.Width * scale);
                        newHeight = (int)(originalBitmap.Height * scale);
                    }
                    
                    // 高品質リサイズ
                    var resizedBitmap = originalBitmap.Resize(new SkiaSharp.SKImageInfo(newWidth, newHeight), SkiaSharp.SKFilterQuality.High);
                    _logger.LogDebug($"Standard high quality preview generated (EXIF ignored): {newWidth}x{newHeight}");
                    
                    // 元のBitmapを解放
                    originalBitmap.Dispose();
                    
                    return resizedBitmap;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to generate standard high quality preview: {ImagePath}", imagePath);
                    return null;
                }
            });
        }
    }
}