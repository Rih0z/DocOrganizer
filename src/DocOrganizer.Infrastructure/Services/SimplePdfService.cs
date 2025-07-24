using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using DocOrganizer.Core.Models;

namespace DocOrganizer.Infrastructure.Services
{
    /// <summary>
    /// SkiaSharpのみを使用したシンプルなPDF生成
    /// PDFsharpCoreの依存を完全に排除
    /// </summary>
    public static class SimplePdfService
    {
        public static async Task<PdfDocument> CreatePdfFromImageSimpleAsync(string imagePath, string outputPath, ILogger logger)
        {
            return await Task.Run(() =>
            {
                try
                {
                    logger.LogInformation("Creating PDF using SkiaSharp only: {ImagePath}", imagePath);

                    // 画像を読み込み
                    using var bitmap = SKBitmap.Decode(imagePath);
                    if (bitmap == null)
                    {
                        throw new InvalidOperationException($"Failed to decode image: {imagePath}");
                    }

                    // PDFドキュメントを作成
                    using var stream = new SKFileWStream(outputPath);
                    using var document = SKDocument.CreatePdf(stream);
                    
                    // A4サイズ（72 DPIで595x842ポイント）
                    const float pageWidth = 595;
                    const float pageHeight = 842;

                    // ページを作成
                    using var canvas = document.BeginPage(pageWidth, pageHeight);
                    
                    // 画像のアスペクト比を保持してスケール
                    float imageAspect = (float)bitmap.Width / bitmap.Height;
                    float pageAspect = pageWidth / pageHeight;
                    
                    float drawWidth, drawHeight;
                    if (imageAspect > pageAspect)
                    {
                        drawWidth = pageWidth;
                        drawHeight = pageWidth / imageAspect;
                    }
                    else
                    {
                        drawHeight = pageHeight;
                        drawWidth = pageHeight * imageAspect;
                    }
                    
                    float x = (pageWidth - drawWidth) / 2;
                    float y = (pageHeight - drawHeight) / 2;
                    
                    // 画像を描画
                    var destRect = SKRect.Create(x, y, drawWidth, drawHeight);
                    canvas.DrawBitmap(bitmap, destRect);
                    
                    document.EndPage();
                    document.Close();

                    logger.LogInformation("Successfully created PDF using SkiaSharp");

                    // PDFドキュメントオブジェクトを作成して返す
                    var pdfDoc = new PdfDocument(outputPath);
                    
                    var page = new PdfPage(1);
                    page.SetDimensions((int)pageWidth, (int)pageHeight);
                    
                    // 元画像をサムネイルとして設定
                    var thumbnailBitmap = GenerateThumbnail(bitmap, 150);
                    page.SetThumbnailImage(thumbnailBitmap);
                    
                    pdfDoc.AddPage(page);
                    
                    return pdfDoc;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to create PDF from image: {ImagePath}", imagePath);
                    throw;
                }
            });
        }

        /// <summary>
        /// 画像からサムネイルを生成
        /// </summary>
        /// <param name="sourceBitmap">元画像</param>
        /// <param name="targetWidth">目標幅</param>
        /// <returns>サムネイル画像</returns>
        private static SKBitmap GenerateThumbnail(SKBitmap sourceBitmap, int targetWidth)
        {
            // アスペクト比を保持してサムネイルサイズを計算
            float aspectRatio = (float)sourceBitmap.Height / sourceBitmap.Width;
            int targetHeight = (int)(targetWidth * aspectRatio);

            // サムネイル用のビットマップを作成
            var thumbnail = new SKBitmap(targetWidth, targetHeight);
            
            using (var canvas = new SKCanvas(thumbnail))
            {
                // 高品質なリサイズ設定
                using (var paint = new SKPaint())
                {
                    paint.IsAntialias = true;
                    paint.FilterQuality = SKFilterQuality.High;
                    
                    // 元画像をサムネイルサイズに描画
                    var destRect = SKRect.Create(0, 0, targetWidth, targetHeight);
                    canvas.DrawBitmap(sourceBitmap, destRect, paint);
                }
            }

            return thumbnail;
        }

        /// <summary>
        /// 画像パスリストから直接PDFファイルを生成（PdfDocumentオブジェクトは返さない）
        /// </summary>
        public static async Task CreatePdfFileFromImagesAsync(IEnumerable<string> imagePaths, string outputPath, ILogger logger)
        {
            await CreatePdfFileFromImagesWithRotationAsync(imagePaths, null, outputPath, logger);
        }
        
        /// <summary>
        /// 画像パスリストから直接PDFファイルを生成（回転情報付き）
        /// </summary>
        public static async Task CreatePdfFileFromImagesWithRotationAsync(IEnumerable<string> imagePaths, IEnumerable<int>? rotations, string outputPath, ILogger logger)
        {
            await Task.Run(() =>
            {
                try
                {
                    logger.LogInformation("Creating PDF file directly from images");

                    // PDFドキュメントを作成
                    using var stream = new SKFileWStream(outputPath);
                    using var document = SKDocument.CreatePdf(stream);
                    
                    // A4サイズ（72 DPIで595x842ポイント）
                    const float pageWidth = 595;
                    const float pageHeight = 842;

                    var rotationList = rotations?.ToList();
                    int index = 0;
                    
                    foreach (var imagePath in imagePaths)
                    {
                        // 画像を読み込み
                        using var bitmap = SKBitmap.Decode(imagePath);
                        if (bitmap == null)
                        {
                            logger.LogWarning("Failed to decode image, skipping: {ImagePath}", imagePath);
                            index++;
                            continue;
                        }

                        // 回転角度を取得
                        int rotation = (rotationList != null && index < rotationList.Count) ? rotationList[index] : 0;
                        
                        // ページを作成
                        using var canvas = document.BeginPage(pageWidth, pageHeight);
                        
                        // 保存状態をプッシュ
                        canvas.Save();
                        
                        // ページの中心に移動して回転
                        if (rotation != 0)
                        {
                            canvas.Translate(pageWidth / 2, pageHeight / 2);
                            canvas.RotateDegrees(rotation);
                            canvas.Translate(-pageWidth / 2, -pageHeight / 2);
                        }
                        
                        // 画像のアスペクト比を保持してスケール
                        float imageAspect = (float)bitmap.Width / bitmap.Height;
                        float pageAspect = pageWidth / pageHeight;
                        
                        float drawWidth, drawHeight;
                        if (imageAspect > pageAspect)
                        {
                            drawWidth = pageWidth;
                            drawHeight = pageWidth / imageAspect;
                        }
                        else
                        {
                            drawHeight = pageHeight;
                            drawWidth = pageHeight * imageAspect;
                        }
                        
                        float x = (pageWidth - drawWidth) / 2;
                        float y = (pageHeight - drawHeight) / 2;
                        
                        // 画像を描画
                        var destRect = SKRect.Create(x, y, drawWidth, drawHeight);
                        canvas.DrawBitmap(bitmap, destRect);
                        
                        // 保存状態を復元
                        canvas.Restore();
                        
                        document.EndPage();
                        index++;
                    }

                    document.Close();
                    logger.LogInformation("Successfully created PDF file at {OutputPath}", outputPath);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to create PDF file from images");
                    throw;
                }
            });
        }

        /// <summary>
        /// 画像パスリストからPDFを生成（PdfDocumentオブジェクトを返す - 既存の互換性のため）
        /// </summary>
        public static async Task<PdfDocument> CreatePdfFromImagesSimpleAsync(IEnumerable<string> imagePaths, string outputPath, ILogger logger)
        {
            return await Task.Run(() =>
            {
                try
                {
                    logger.LogInformation("Creating multi-page PDF using SkiaSharp only");

                    // PDFドキュメントを作成
                    using var stream = new SKFileWStream(outputPath);
                    using var document = SKDocument.CreatePdf(stream);
                    
                    // A4サイズ（72 DPIで595x842ポイント）
                    const float pageWidth = 595;
                    const float pageHeight = 842;

                    var pdfDoc = new PdfDocument(outputPath);
                    int pageNumber = 1;

                    foreach (var imagePath in imagePaths)
                    {
                        // 画像を読み込み
                        using var bitmap = SKBitmap.Decode(imagePath);
                        if (bitmap == null)
                        {
                            logger.LogWarning("Failed to decode image, skipping: {ImagePath}", imagePath);
                            continue;
                        }

                        // ページを作成
                        using var canvas = document.BeginPage(pageWidth, pageHeight);
                        
                        // 画像のアスペクト比を保持してスケール
                        float imageAspect = (float)bitmap.Width / bitmap.Height;
                        float pageAspect = pageWidth / pageHeight;
                        
                        float drawWidth, drawHeight;
                        if (imageAspect > pageAspect)
                        {
                            drawWidth = pageWidth;
                            drawHeight = pageWidth / imageAspect;
                        }
                        else
                        {
                            drawHeight = pageHeight;
                            drawWidth = pageHeight * imageAspect;
                        }
                        
                        float x = (pageWidth - drawWidth) / 2;
                        float y = (pageHeight - drawHeight) / 2;
                        
                        // 画像を描画
                        var destRect = SKRect.Create(x, y, drawWidth, drawHeight);
                        canvas.DrawBitmap(bitmap, destRect);
                        
                        document.EndPage();

                        // ページ情報を追加
                        var page = new PdfPage(pageNumber++);
                        page.SetDimensions((int)pageWidth, (int)pageHeight);
                        
                        // 元画像をサムネイルとして設定
                        var thumbnailBitmap = GenerateThumbnail(bitmap, 150);
                        page.SetThumbnailImage(thumbnailBitmap);
                        
                        pdfDoc.AddPage(page);
                    }

                    document.Close();
                    logger.LogInformation("Successfully created multi-page PDF with {PageCount} pages", pageNumber - 1);

                    return pdfDoc;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to create PDF from images");
                    throw;
                }
            });
        }

        public static async Task<SKBitmap?> ExtractPagePreviewAsync(PdfDocument document, int pageIndex, float scale = 1.0f)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (document == null || pageIndex < 0 || pageIndex >= document.Pages.Count)
                    {
                        return null;
                    }

                    var page = document.Pages[pageIndex];
                    
                    // 画像ベースのページの場合
                    if (!string.IsNullOrEmpty(page.SourceImagePath) && File.Exists(page.SourceImagePath))
                    {
                        // 元画像から高解像度プレビューを生成
                        using var originalBitmap = SKBitmap.Decode(page.SourceImagePath);
                        if (originalBitmap == null) return null;

                        // スケールを適用したサイズを計算
                        int previewWidth = (int)(600 * scale); // プレビュー用の標準幅
                        int previewHeight = (int)(originalBitmap.Height * (double)previewWidth / originalBitmap.Width);
                        
                        // 回転を適用
                        var finalBitmap = originalBitmap;
                        if (page.Rotation != 0)
                        {
                            finalBitmap = ApplyRotation(originalBitmap, page.Rotation);
                        }

                        // プレビューサイズにリサイズ
                        var previewBitmap = finalBitmap.Resize(new SKImageInfo(previewWidth, previewHeight), SKFilterQuality.High);
                        
                        // 回転を適用した場合は一時ビットマップを解放
                        if (finalBitmap != originalBitmap)
                        {
                            finalBitmap.Dispose();
                        }

                        return previewBitmap;
                    }

                    // PDFページの場合は既存のサムネイルをスケールアップ
                    if (page.ThumbnailImage != null)
                    {
                        var thumbnailWidth = (int)(page.ThumbnailImage.Width * scale);
                        var thumbnailHeight = (int)(page.ThumbnailImage.Height * scale);
                        return page.ThumbnailImage.Resize(new SKImageInfo(thumbnailWidth, thumbnailHeight), SKFilterQuality.High);
                    }

                    return null;
                }
                catch (Exception ex)
                {
                    // ログ出力は呼び出し元で行う
                    return null;
                }
            });
        }

        public static async Task<SKBitmap?> ExtractPageThumbnailAsync(PdfDocument document, int pageIndex, int thumbnailSize)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (document == null || pageIndex < 0 || pageIndex >= document.Pages.Count)
                    {
                        return null;
                    }

                    var page = document.Pages[pageIndex];
                    
                    // 画像ベースのページの場合
                    if (!string.IsNullOrEmpty(page.SourceImagePath) && File.Exists(page.SourceImagePath))
                    {
                        using var originalBitmap = SKBitmap.Decode(page.SourceImagePath);
                        if (originalBitmap == null) return null;

                        // 回転を適用
                        var finalBitmap = originalBitmap;
                        if (page.Rotation != 0)
                        {
                            finalBitmap = ApplyRotation(originalBitmap, page.Rotation);
                        }

                        // サムネイルサイズにリサイズ
                        var thumbnailBitmap = GenerateThumbnail(finalBitmap, thumbnailSize);
                        
                        // 回転を適用した場合は一時ビットマップを解放
                        if (finalBitmap != originalBitmap)
                        {
                            finalBitmap.Dispose();
                        }

                        return thumbnailBitmap;
                    }

                    return null;
                }
                catch (Exception ex)
                {
                    return null;
                }
            });
        }

        private static SKBitmap ApplyRotation(SKBitmap originalBitmap, int rotation)
        {
            if (rotation == 0) return originalBitmap;

            var rotatedInfo = new SKImageInfo(
                rotation == 90 || rotation == 270 ? originalBitmap.Height : originalBitmap.Width,
                rotation == 90 || rotation == 270 ? originalBitmap.Width : originalBitmap.Height,
                originalBitmap.ColorType,
                originalBitmap.AlphaType
            );

            var rotatedBitmap = new SKBitmap(rotatedInfo);
            using var canvas = new SKCanvas(rotatedBitmap);
            
            canvas.Clear(SKColors.White);
            canvas.Translate(rotatedInfo.Width / 2f, rotatedInfo.Height / 2f);
            canvas.RotateDegrees(rotation);
            canvas.Translate(-originalBitmap.Width / 2f, -originalBitmap.Height / 2f);
            canvas.DrawBitmap(originalBitmap, 0, 0);

            return rotatedBitmap;
        }
    }
}