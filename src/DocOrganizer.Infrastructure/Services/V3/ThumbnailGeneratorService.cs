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
        /// 右側プレビュー用高解像度画像生成（余白自動削除適用）
        /// </summary>
        public async Task<ImageSource> GenerateRightPreviewImageAsync(string filePath, int rotation = 0, int maxWidth = 1920, int maxHeight = 1080)
        {
            try
            {
                _logger.LogDebug("[V3_Thumbnail] 右プレビュー用高解像度生成開始: {FileName}, 回転: {Rotation}度",
                    Path.GetFileName(filePath), rotation);

                // プロバイダーで画像読み込み
                var previewImage = await _providerManager.ProcessWithBestProvider(filePath,
                    provider => provider.GeneratePreviewAsync(filePath, maxWidth, maxHeight));

                // 🎯 V3.0.111: 余白自動削除を必ず適用（ユーザー要求：余白は絶対に必要なし）
                if (_autoCropService != null && previewImage is BitmapSource bitmapSource)
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