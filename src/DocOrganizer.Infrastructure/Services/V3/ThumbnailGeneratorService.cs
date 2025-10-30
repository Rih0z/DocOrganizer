using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DocOrganizer.Application.Interfaces.V3;
using Microsoft.Extensions.Logging;
using System.Windows;
using System.Windows.Media.Imaging;

namespace DocOrganizer.Infrastructure.Services.V3
{
    /// <summary>
    /// 🏗️ V3.0.009 統合サムネイル生成サービス - プロバイダーアーキテクチャ完全対応
    /// 全画像形式対応・無限拡張可能・企業レベル品質
    /// </summary>
    public class ThumbnailGeneratorService : IThumbnailGeneratorService
    {
        private readonly IImageProcessingProviderManager _providerManager;
        private readonly ILogger<ThumbnailGeneratorService> _logger;
        private readonly IAutoCropService _autoCropService;

        public ThumbnailGeneratorService(
            IImageProcessingProviderManager providerManager,
            ILogger<ThumbnailGeneratorService> logger,
            IAutoCropService autoCropService = null) // オプショナル（既存コードとの互換性）
        {
            _providerManager = providerManager;
            _logger = logger;
            _autoCropService = autoCropService;
        }

        /// <summary>
        /// 左側パネル用サムネイル生成（150x200固定）
        /// </summary>
        public async Task<ImageSource> GenerateLeftPanelThumbnailAsync(string filePath, int rotation = 0)
        {
            try
            {
                _logger.LogDebug("[V3_Thumbnail] 左パネル用サムネイル生成開始: {FileName}, 回転: {Rotation}度", 
                    Path.GetFileName(filePath), rotation);

                return await _providerManager.ProcessWithBestProvider(filePath, 
                    provider => provider.GenerateThumbnailAsync(filePath, ThumbnailSize.LeftPanel, rotation));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_Thumbnail] 左パネルサムネイル生成エラー: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// 右側プレビュー用高解像度画像生成
        /// </summary>
        public async Task<ImageSource> GenerateRightPreviewImageAsync(string filePath, int rotation = 0, int maxWidth = 1920, int maxHeight = 1080, bool enableAutoCrop = false)
        {
            try
            {
                _logger.LogDebug("[V3_Thumbnail] 右プレビュー用高解像度生成開始: {FileName}, 回転: {Rotation}度",
                    Path.GetFileName(filePath), rotation);

                // プロバイダーで画像読み込み
                var previewImage = await _providerManager.ProcessWithBestProvider(filePath,
                    provider => provider.GeneratePreviewAsync(filePath, maxWidth, maxHeight));

                // 🎯 V3.0.124: 余白自動削除をオプション化（デフォルト無効）
                if (enableAutoCrop && _autoCropService != null && previewImage is BitmapSource bitmapSource)
                {
                    try
                    {
                        var croppedImage = await _autoCropService.AutoCropAsync(bitmapSource);
                        _logger.LogDebug("[V3_Thumbnail] 余白削除適用完了");
                        previewImage = croppedImage;
                    }
                    catch (Exception cropEx)
                    {
                        _logger.LogWarning(cropEx, "[V3_Thumbnail] 余白削除失敗、元画像を使用");
                    }
                }

                // 🔧 回転適用（WPFのTransformedBitmapを使用）
                if (rotation > 0 && previewImage is BitmapSource rotateSource)
                {
                    var transform = new RotateTransform(rotation);
                    var rotatedBitmap = new TransformedBitmap(rotateSource, transform);
                    rotatedBitmap.Freeze();
                    return rotatedBitmap;
                }

                return previewImage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_Thumbnail] 右プレビュー生成エラー: {FilePath}", filePath);
                throw;
            }
        }

        /// <summary>
        /// 画像をA4フレームにフィット（V3.0.109）
        /// </summary>
        private async Task<ImageSource> FitImageToA4FrameAsync(ImageSource sourceImage, int targetWidth, int targetHeight)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // ImageSourceをBitmapSourceに変換
                    if (sourceImage is not BitmapSource bitmapSource)
                    {
                        _logger.LogWarning("[V3_Thumbnail] ImageSourceがBitmapSourceではありません");
                        return sourceImage;
                    }

                    // 元画像のサイズ取得
                    var sourceWidth = bitmapSource.PixelWidth;
                    var sourceHeight = bitmapSource.PixelHeight;
                    
                    // アスペクト比を保持してフィット計算
                    double sourceAspect = (double)sourceWidth / sourceHeight;
                    double targetAspect = (double)targetWidth / targetHeight;
                    
                    int drawWidth, drawHeight;
                    
                    if (sourceAspect > targetAspect)
                    {
                        // 画像が横長: 幅に合わせる
                        drawWidth = targetWidth;
                        drawHeight = (int)(targetWidth / sourceAspect);
                    }
                    else
                    {
                        // 画像が縦長: 高さに合わせる
                        drawHeight = targetHeight;
                        drawWidth = (int)(targetHeight * sourceAspect);
                    }
                    
                    // V3.0.109: 画像を最大化して表示（余白なし）
                    // 元画像がA4比率に近い場合はそのまま使用
                    if (Math.Abs(sourceAspect - targetAspect) < 0.01)
                    {
                        // ほぼA4比率なのでそのままリサイズ
                        var resized = new TransformedBitmap(bitmapSource, 
                            new ScaleTransform(
                                (double)targetWidth / sourceWidth,
                                (double)targetHeight / sourceHeight));
                        resized.Freeze();
                        return resized;
                    }
                    
                    // A4フレームに合わせて白背景で描画
                    var drawingVisual = new DrawingVisual();
                    using (var drawingContext = drawingVisual.RenderOpen())
                    {
                        // 白背景を描画
                        drawingContext.DrawRectangle(
                            Brushes.White,
                            null,
                            new Rect(0, 0, targetWidth, targetHeight));
                        
                        // 中央配置で画像を描画
                        var x = (targetWidth - drawWidth) / 2.0;
                        var y = (targetHeight - drawHeight) / 2.0;
                        
                        // スケーリングされた画像を描画
                        var scaledBitmap = new TransformedBitmap(bitmapSource,
                            new ScaleTransform(
                                (double)drawWidth / sourceWidth,
                                (double)drawHeight / sourceHeight));
                        
                        drawingContext.DrawImage(scaledBitmap,
                            new Rect(x, y, drawWidth, drawHeight));
                    }
                    
                    // DrawingVisualをBitmapSourceに変換
                    var renderTarget = new RenderTargetBitmap(
                        targetWidth, targetHeight, 96, 96, PixelFormats.Pbgra32);
                    renderTarget.Render(drawingVisual);
                    renderTarget.Freeze();
                    
                    _logger.LogDebug("[V3_Thumbnail] A4フレームフィット完了: {Width}x{Height}", targetWidth, targetHeight);
                    return renderTarget;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[V3_Thumbnail] A4フレームフィットエラー");
                    return sourceImage; // エラー時は元画像を返す
                }
            });
        }

        /// <summary>
        /// PDFページからサムネイル生成
        /// </summary>
        public async Task<ImageSource> GeneratePdfPageThumbnailAsync(string pdfFilePath, int pageIndex, ThumbnailSize thumbnailSize)
        {
            try
            {
                _logger.LogDebug("[V3_Thumbnail] PDFサムネイル生成開始: {FileName}, Page: {PageIndex}, Size: {Size}", 
                    Path.GetFileName(pdfFilePath), pageIndex, thumbnailSize);

                return await Task.Run(() =>
                {
                    // PDFSharp使用してページ画像を取得
                    // （実装例 - 実際の実装では適切なPDFライブラリを使用）
                    var pageImagePath = ExtractPdfPageAsImage(pdfFilePath, pageIndex);
                    
                    // 抽出した画像をプロバイダーでサムネイル化
                    return _providerManager.ProcessWithBestProvider(pageImagePath, 
                        provider => provider.GenerateThumbnailAsync(pageImagePath, thumbnailSize, 0)).Result;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_Thumbnail] PDFサムネイル生成エラー: {FilePath}, Page: {PageIndex}", pdfFilePath, pageIndex);
                throw;
            }
        }

        /// <summary>
        /// 一括サムネイル生成（パフォーマンス最適化）
        /// </summary>
        public async Task<ImageSource[]> GenerateBatchThumbnailsAsync(string[] filePaths, ThumbnailSize thumbnailSize)
        {
            try
            {
                _logger.LogDebug("[V3_Thumbnail] 一括サムネイル生成開始: {FileCount}件, Size: {Size}", filePaths.Length, thumbnailSize);

                var tasks = filePaths.Select(async filePath =>
                {
                    return thumbnailSize switch
                    {
                        ThumbnailSize.LeftPanel => await GenerateLeftPanelThumbnailAsync(filePath),
                        ThumbnailSize.RightPreview => await GenerateRightPreviewImageAsync(filePath),
                        ThumbnailSize.PdfPreview => await GeneratePdfPageThumbnailAsync(filePath, 0, thumbnailSize),
                        _ => throw new ArgumentException($"未対応のサムネイルサイズ: {thumbnailSize}")
                    };
                });

                var results = await Task.WhenAll(tasks);
                
                _logger.LogDebug("[V3_Thumbnail] 一括サムネイル生成完了: {SuccessCount}/{TotalCount}", 
                    results.Count(r => r != null), filePaths.Length);

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_Thumbnail] 一括サムネイル生成エラー");
                throw;
            }
        }

        // Private helper methods
        

        /// <summary>
        /// 🚀 V3.0.143: キャッシュされたSKBitmapをメモリ上で回転
        /// ディスクI/O不要で超高速（GPU不要、全環境で同じ速度）
        /// </summary>
        /// <param name="source">元のSKBitmap</param>
        /// <param name="degrees">回転角度（90, 180, 270）</param>
        /// <returns>回転済みSKBitmap、失敗時はnull</returns>
        public SkiaSharp.SKBitmap? RotateCachedBitmap(SkiaSharp.SKBitmap source, int degrees)
        {
            if (source == null)
            {
                _logger?.LogWarning("[RotateCachedBitmap] source is null");
                return null;
            }

            try
            {
                // 回転角度を正規化（0-359）
                degrees = (degrees % 360 + 360) % 360;

                // 回転不要の場合は元のBitmapをそのまま返す（コピーなし）
                if (degrees == 0)
                {
                    return source;
                }

                // 新しいサイズを計算
                int newWidth = (degrees == 90 || degrees == 270) ? source.Height : source.Width;
                int newHeight = (degrees == 90 || degrees == 270) ? source.Width : source.Height;

                // メモリ不足チェック（500MB制限）
                long estimatedBytes = (long)newWidth * newHeight * 4;  // RGBA = 4 bytes/pixel
                if (estimatedBytes > 500 * 1024 * 1024)
                {
                    _logger?.LogWarning("[RotateCachedBitmap] 画像が大きすぎます: {Size}MB - フォールバック",
                        estimatedBytes / 1024 / 1024);
                    return null;  // フォールバックに委譲
                }

                // 回転済みBitmapを作成
                var rotated = new SkiaSharp.SKBitmap(newWidth, newHeight, source.ColorType, source.AlphaType);
                if (rotated == null)
                {
                    _logger?.LogError("[RotateCachedBitmap] SKBitmap作成失敗");
                    return null;
                }

                // Canvas作成と回転描画
                using (var canvas = new SkiaSharp.SKCanvas(rotated))
                {
                    if (canvas == null)
                    {
                        rotated.Dispose();
                        _logger?.LogError("[RotateCachedBitmap] SKCanvas作成失敗");
                        return null;
                    }

                    // 回転の中心を画像中央に設定
                    canvas.Translate(newWidth / 2f, newHeight / 2f);
                    canvas.RotateDegrees(degrees);
                    canvas.Translate(-source.Width / 2f, -source.Height / 2f);
                    canvas.DrawBitmap(source, 0, 0);
                }

                _logger?.LogDebug("[RotateCachedBitmap] 成功: {Width}x{Height}, {Degrees}度",
                    newWidth, newHeight, degrees);

                return rotated;
            }
            catch (OutOfMemoryException ex)
            {
                _logger?.LogError(ex, "[RotateCachedBitmap] メモリ不足");
                return null;  // フォールバックに委譲
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[RotateCachedBitmap] 予期しないエラー");
                return null;  // フォールバックに委譲
            }
        }

        /// <summary>
        /// 🚀 V3.0.143: キャッシュされたSKBitmapから回転済みBitmapSourceを生成
        /// rotatedBitmapをout引数で返し、呼び出し側でPdfPage.SetThumbnailImageに設定
        /// </summary>
        /// <param name="cachedBitmap">キャッシュされたSKBitmap</param>
        /// <param name="rotation">回転角度</param>
        /// <param name="rotatedBitmap">回転済みSKBitmap（呼び出し側がSetThumbnailImageで設定）</param>
        /// <returns>表示用BitmapSource、失敗時はnull</returns>
        public BitmapSource? GenerateBitmapSourceFromCache(SkiaSharp.SKBitmap cachedBitmap, int rotation, out SkiaSharp.SKBitmap? rotatedBitmap)
        {
            rotatedBitmap = null;

            if (cachedBitmap == null)
            {
                _logger?.LogWarning("[GenerateBitmapSourceFromCache] cachedBitmap is null");
                return null;
            }

            try
            {
                // キャッシュを回転
                rotatedBitmap = RotateCachedBitmap(cachedBitmap, rotation);
                if (rotatedBitmap == null)
                {
                    _logger?.LogWarning("[GenerateBitmapSourceFromCache] 回転失敗 - フォールバック必要");
                    return null;
                }

                // BitmapSourceに変換
                var bitmapSource = ConvertSKBitmapToBitmapSource(rotatedBitmap);

                if (bitmapSource == null)
                {
                    _logger?.LogWarning("[GenerateBitmapSourceFromCache] BitmapSource変換失敗");

                    // 変換失敗時はrotatedBitmapをDispose（メモリリーク防止）
                    if (rotatedBitmap != cachedBitmap)
                    {
                        rotatedBitmap.Dispose();
                        rotatedBitmap = null;
                    }

                    return null;
                }

                // Freeze処理（UI最適化）
                if (bitmapSource.CanFreeze && !bitmapSource.IsFrozen)
                {
                    bitmapSource.Freeze();
                }

                _logger?.LogDebug("[GenerateBitmapSourceFromCache] 成功: {Width}x{Height}",
                    bitmapSource.PixelWidth, bitmapSource.PixelHeight);

                return bitmapSource;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[GenerateBitmapSourceFromCache] 予期しないエラー");

                // エラー時はrotatedBitmapをDispose
                if (rotatedBitmap != null && rotatedBitmap != cachedBitmap)
                {
                    rotatedBitmap.Dispose();
                    rotatedBitmap = null;
                }

                return null;
            }
        }

        /// <summary>
        /// 🚀 V3.0.143: SKBitmapをBitmapSourceに変換
        /// </summary>
        private BitmapSource? ConvertSKBitmapToBitmapSource(SkiaSharp.SKBitmap skBitmap)
        {
            try
            {
                if (skBitmap == null) return null;

                var width = skBitmap.Width;
                var height = skBitmap.Height;
                var dpi = 96.0;

                // SKBitmapのピクセルデータを取得
                var pixels = skBitmap.Pixels;
                var stride = width * 4; // BGRA32形式
                var pixelData = new byte[height * stride];

                // SKColorからBGRA形式に変換
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        var skColor = pixels[y * width + x];
                        var index = (y * width + x) * 4;

                        pixelData[index] = skColor.Blue;      // B
                        pixelData[index + 1] = skColor.Green;  // G
                        pixelData[index + 2] = skColor.Red;    // R
                        pixelData[index + 3] = skColor.Alpha;  // A
                    }
                }

                // BitmapSourceを作成
                var bitmap = BitmapSource.Create(
                    width, height,
                    dpi, dpi,
                    System.Windows.Media.PixelFormats.Bgra32,
                    null,
                    pixelData,
                    stride);

                // Freezeして不変にする
                if (bitmap.CanFreeze && !bitmap.IsFrozen)
                {
                    bitmap.Freeze();
                }

                return bitmap;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[ConvertSKBitmapToBitmapSource] 変換エラー");
                return null;
            }
        }

        private string ExtractPdfPageAsImage(string pdfFilePath, int pageIndex)
        {
            // 🎯 実装例: PDF ページを画像として抽出
            // 実際の実装では、PdfSharp や iTextSharp などを使用してPDFページを画像に変換
            
            var tempImagePath = Path.GetTempFileName() + ".png";
            
            // PDF ページ抽出処理（実装が必要）
            // ここでは仮の実装
            
            return tempImagePath;
        }
    }
}