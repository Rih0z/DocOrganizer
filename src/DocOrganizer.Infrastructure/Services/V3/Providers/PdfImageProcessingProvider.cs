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
    /// 🎯 V3.0.025 PDF専用プロバイダー - PdfiumSharp活用による完全対応
    /// 検証→サムネイル→プレビュー→編集の統一的PDF処理（HEICパターン完全踏襲）
    /// </summary>
    [ImageProcessingProvider("PDF", Priority = 90)] // 🎯 V3.0.027: GhostScript完全回避 - 最高優先度でPDF処理を独占
    public class PdfImageProcessingProvider : IImageProcessingProvider, IDisposable
    {
        private readonly IPdfRenderingService _pdfRenderingService;
        private readonly ILogger<PdfImageProcessingProvider> _logger;
        private readonly PdfPerformanceMonitor _performanceMonitor;
        
        public string[] SupportedExtensions => new[] { ".pdf" };
        public int Priority => 90; // 🎯 V3.0.027: Standard(80)より高く、PDF処理完全独占
        public string ProviderName => "PdfiumSharp PDF Provider (GhostScript-Free)"; // 🎯 明確な識別 // 🎯 明確な識別
        
        public PdfImageProcessingProvider(
            IPdfRenderingService pdfRenderingService,
            ILogger<PdfImageProcessingProvider> logger,
            PdfPerformanceMonitor performanceMonitor)
        {
            _pdfRenderingService = pdfRenderingService ?? throw new ArgumentNullException(nameof(pdfRenderingService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _performanceMonitor = performanceMonitor ?? throw new ArgumentNullException(nameof(performanceMonitor));
            
            // 🎯 V3.0.027: GhostScript完全不要の確認ログ
            _logger.LogInformation("[V3_PDF] PdfiumSharp PDF Provider初期化完了 - GhostScript依存関係なし");
        }
        
        public bool SupportsFormat(string extension)
        {
            return extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase);
        }
        
        /// <summary>
        /// PDF画像検証（ドラッグ&ドロップ用）- パフォーマンス監視対応
        /// </summary>
        public async Task<ImageValidationResult> ValidateAsync(string filePath)
        {
            return await _performanceMonitor.MonitorAsync("PDF Validation", filePath, async () =>
            {
                try
                {
                    _logger.LogDebug("[V3_PDF] 検証開始: {FileName}", Path.GetFileName(filePath));
                    
                    if (!File.Exists(filePath))
                    {
                        return new ImageValidationResult(
                            FilePath: filePath,
                            IsValid: false,
                            IsCorrupted: false,
                            IsZeroBytes: false,
                            FileSize: 0,
                            Format: "PDF",
                            Width: 0,
                            Height: 0,
                            Issues: new List<string> { "ファイルが存在しません" },
                            ErrorMessage: "指定されたPDFファイルが見つかりませんでした"
                        );
                    }

                    var fileInfo = new FileInfo(filePath);
                    if (fileInfo.Length == 0)
                    {
                        return new ImageValidationResult(
                            FilePath: filePath,
                            IsValid: false,
                            IsCorrupted: false,
                            IsZeroBytes: true,
                            FileSize: 0,
                            Format: "PDF",
                            Width: 0,
                            Height: 0,
                            Issues: new List<string> { "ファイルサイズが0バイト" },
                            ErrorMessage: "PDFファイルが空です"
                        );
                    }
                    
                    var pdfInfo = await _pdfRenderingService.GetPdfInfoAsync(filePath);
                    
                    return new ImageValidationResult(
                        FilePath: filePath,
                        IsValid: true,
                        IsCorrupted: false,
                        IsZeroBytes: false,
                        FileSize: pdfInfo.FileSize,
                        Format: "PDF",
                        Width: (int)pdfInfo.Width,
                        Height: (int)pdfInfo.Height,
                        Issues: new List<string>(),
                        ErrorMessage: null
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[V3_PDF] 検証エラー: {FilePath}", filePath);
                    return new ImageValidationResult(
                        FilePath: filePath,
                        IsValid: false,
                        IsCorrupted: true,
                        IsZeroBytes: false,
                        FileSize: 0,
                        Format: "PDF",
                        Width: 0,
                        Height: 0,
                        Issues: new List<string> { "PDF読み込みエラー" },
                        ErrorMessage: ex.Message
                    );
                }
            });
        }
        
        /// <summary>
        /// PDFサムネイル生成（左パネル、右プレビュー、PDF対応）- パフォーマンス監視対応
        /// </summary>
        public async Task<ImageSource> GenerateThumbnailAsync(string filePath, ThumbnailSize size, int rotation = 0)
        {
            return await _performanceMonitor.MonitorAsync("PDF Thumbnail Generation", filePath, async () =>
            {
                try
                {
                    _logger.LogDebug("[V3_PDF] サムネイル生成開始: {FileName}, サイズ: {Size}, 回転: {Rotation}度", 
                        Path.GetFileName(filePath), size, rotation);

                    // PDF→画像変換でImageSharp互換性確保（HEICパターン踏襲）
                    var pageImagePath = await _pdfRenderingService.ConvertPdfPageToTempImageAsync(filePath, 0, 150);
                    
                    return await Task.Run(() =>
                    {
                        using var image = SixLabors.ImageSharp.Image.Load(pageImagePath);
                        
                        // 回転適用（HEICと同様）
                        if (rotation > 0)
                        {
                            image.Mutate(x => x.Rotate(rotation));
                        }
                        
                        // サイズ別リサイズ（HEICと同一処理）
                        var (width, height) = GetSizeForThumbnailType(size);
                        var targetSize = CalculateResizeWithAspectRatio(image.Width, image.Height, width, height);
                        image.Mutate(x => x.Resize(targetSize.Width, targetSize.Height));

                        // WPFのBitmapImageに変換（HEICと同一処理）
                        return ConvertImageSharpToBitmapImage(image);
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[V3_PDF] サムネイル生成エラー: {FilePath}", filePath);
                    throw;
                }
            });
        }
        
        /// <summary>
        /// PDFプレビュー画像生成（高解像度表示用）- パフォーマンス監視対応
        /// </summary>
        public async Task<ImageSource> GeneratePreviewAsync(string filePath, int maxWidth = 1920, int maxHeight = 1080)
        {
            return await _performanceMonitor.MonitorAsync("PDF Preview Generation", filePath, async () =>
            {
                try
                {
                    _logger.LogDebug("[V3_PDF] プレビュー生成開始: {FileName}, 上限: {MaxWidth}x{MaxHeight}", 
                        Path.GetFileName(filePath), maxWidth, maxHeight);

                    // PDF→画像変換（高DPIでプレビュー品質向上）
                    var pageImagePath = await _pdfRenderingService.ConvertPdfPageToTempImageAsync(filePath, 0, 300);
                    
                    return await Task.Run(() =>
                    {
                        // 🎯 アスペクト比保持修正: 元画像サイズを取得（HEICパターン完全踏襲）
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(pageImagePath);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        
                        // アスペクト比を保持してリサイズ制限を適用
                        // どちらか一つだけを指定することでアスペクト比が自動保持される
                        var targetSize = CalculatePreviewSize(pageImagePath, maxWidth, maxHeight);
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
                    _logger.LogError(ex, "[V3_PDF] プレビュー生成エラー: {FilePath}", filePath);
                    throw;
                }
            });
        }
        
        /// <summary>
        /// PDF画像情報取得 - パフォーマンス監視対応
        /// </summary>
        public async Task<DocOrganizer.Application.Interfaces.V3.ImageInfo> GetImageInfoAsync(string filePath)
        {
            return await _performanceMonitor.MonitorAsync("PDF Info Retrieval", filePath, async () =>
            {
                try
                {
                    var pdfInfo = await _pdfRenderingService.GetPdfInfoAsync(filePath);
                    
                    return new DocOrganizer.Application.Interfaces.V3.ImageInfo(
                        Width: (int)pdfInfo.Width,
                        Height: (int)pdfInfo.Height,
                        EXIFRotation: System.Windows.Media.Imaging.Rotation.Rotate0, // PDFは回転情報なし
                        FileSize: pdfInfo.FileSize,
                        Format: "PDF"
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[V3_PDF] 画像情報取得エラー: {FilePath}", filePath);
                    throw;
                }
            });
        }
        
        // Private helper methods（HEICプロバイダーから完全移植）
        
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

        
        private (int Width, int Height) CalculatePreviewSize(string imagePath, int maxWidth, int maxHeight)
        {
            try
            {
                // 元画像のサイズを取得（BitmapImageを使用してメタデータのみ読み込み）
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath);
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
        /// リソース解放処理 - パフォーマンス監視とPDFレンダリングサービスのクリーンアップ
        /// </summary>
        public void Dispose()
        {
            try
            {
                _performanceMonitor?.Dispose();
                _pdfRenderingService?.CleanupTempFiles();
                _logger.LogDebug("[V3_PDF] PdfImageProcessingProvider リソース解放完了");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[V3_PDF] リソース解放中にエラーが発生しましたが、処理を継続します");
            }
        }
    }
}