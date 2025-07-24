using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using SkiaSharp;
using System.Text;
using DocOrganizer.Application.Interfaces;
using CorePdfDocument = DocOrganizer.Core.Models.PdfDocument;
using CorePdfPage = DocOrganizer.Core.Models.PdfPage;

namespace DocOrganizer.Infrastructure.Services
{
    /// <summary>
    /// PDF操作の実装クラス
    /// </summary>
    public partial class PdfService : IPdfService
    {
        private readonly ILogger<PdfService> _logger;

        public PdfService(ILogger<PdfService> logger)
        {
            _logger = logger;
            
            // Register encoding provider for handling various character encodings
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            
            // PDFsharpCore handles encoding automatically
        }

        public async Task<CorePdfDocument> LoadPdfAsync(string filePath)
        {
            _logger.LogInformation("Loading PDF from {FilePath}", filePath);

            return await Task.Run(() =>
            {
                try
                {
                    var pdfSharpDoc = PdfReader.Open(filePath, PdfDocumentOpenMode.Import);
                    var document = new CorePdfDocument(filePath);

                    for (int i = 0; i < pdfSharpDoc.PageCount; i++)
                    {
                        var pdfSharpPage = pdfSharpDoc.Pages[i];
                        var page = new CorePdfPage(i + 1);
                        
                        // ページサイズの設定（ポイント単位）
                        page.SetDimensions(
                            (float)pdfSharpPage.Width.Point,
                            (float)pdfSharpPage.Height.Point
                        );

                        document.AddPage(page);
                    }

                    document.ClearModifiedFlag();
                    _logger.LogInformation("Successfully loaded PDF with {PageCount} pages", document.GetPageCount());
                    return document;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading PDF from {FilePath}", filePath);
                    throw;
                }
            });
        }

        public async Task SavePdfAsync(CorePdfDocument document, string filePath)
        {
            _logger.LogInformation("Saving PDF to {FilePath}", filePath);

            // 画像から作成された仮想PDFの場合、実際のPDFを生成
            if (document.IsTemporaryFromImages && document.SourceImagePaths.Any())
            {
                _logger.LogInformation("Converting images to actual PDF");
                
                // 元の画像パスと回転情報から実際のPDFを生成
                var imagePaths = new List<string>();
                var rotations = new List<int>();
                foreach (var page in document.Pages)
                {
                    if (!string.IsNullOrEmpty(page.SourceImagePath))
                    {
                        imagePaths.Add(page.SourceImagePath);
                        rotations.Add(page.Rotation);
                    }
                }
                
                if (imagePaths.Any())
                {
                    // SimplePdfServiceを使用して実際のPDFを生成（回転情報付き）
                    await SimplePdfService.CreatePdfFileFromImagesWithRotationAsync(imagePaths, rotations, filePath, _logger);
                    _logger.LogInformation("Successfully converted images to PDF with rotation: {FilePath}", filePath);
                    return;
                }
            }

            // 通常のPDF保存処理
            await Task.Run(() =>
            {
                try
                {
                    var outputDoc = new PdfSharpCore.Pdf.PdfDocument();

                    // 元のPDFを読み込み
                    PdfSharpCore.Pdf.PdfDocument? sourceDoc = null;
                    if (!string.IsNullOrEmpty(document.FilePath) && File.Exists(document.FilePath))
                    {
                        sourceDoc = PdfReader.Open(document.FilePath, PdfDocumentOpenMode.Import);
                    }

                    // ページを追加（回転を考慮）
                    foreach (var page in document.Pages)
                    {
                        if (sourceDoc != null && page.PageNumber <= sourceDoc.PageCount)
                        {
                            var sourcePage = sourceDoc.Pages[page.PageNumber - 1];
                            var newPage = outputDoc.AddPage(sourcePage);

                            // 回転の適用
                            newPage.Rotate = page.Rotation;
                        }
                    }

                    outputDoc.Save(filePath);
                    _logger.LogInformation("Successfully saved PDF to {FilePath}", filePath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving PDF to {FilePath}", filePath);
                    throw;
                }
            });
        }

        public async Task<CorePdfDocument> MergePdfsAsync(params CorePdfDocument[] documents)
        {
            _logger.LogInformation("Merging {Count} PDFs", documents.Length);

            return await Task.Run(() =>
            {
                try
                {
                    var mergedDoc = new CorePdfDocument();
                    int pageNumber = 1;

                    foreach (var doc in documents)
                    {
                        foreach (var page in doc.Pages)
                        {
                            var newPage = new CorePdfPage(pageNumber++)
                            {
                                Rotation = page.Rotation
                            };
                            newPage.SetDimensions(page.Width, page.Height);
                            mergedDoc.AddPage(newPage);
                        }
                    }

                    _logger.LogInformation("Successfully merged PDFs into {PageCount} pages", mergedDoc.GetPageCount());
                    return mergedDoc;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error merging PDFs");
                    throw;
                }
            });
        }

        public async Task<CorePdfDocument[]> SplitPdfAsync(CorePdfDocument document, params (int startPage, int endPage)[] pageRanges)
        {
            _logger.LogInformation("Splitting PDF into {Count} parts", pageRanges.Length);

            return await Task.Run(() =>
            {
                try
                {
                    var results = new CorePdfDocument[pageRanges.Length];

                    for (int i = 0; i < pageRanges.Length; i++)
                    {
                        var range = pageRanges[i];
                        var splitDoc = new CorePdfDocument();
                        int newPageNumber = 1;

                        for (int pageIndex = range.startPage - 1; pageIndex < range.endPage && pageIndex < document.Pages.Count; pageIndex++)
                        {
                            var sourcePage = document.Pages[pageIndex];
                            var newPage = new CorePdfPage(newPageNumber++)
                            {
                                Rotation = sourcePage.Rotation
                            };
                            newPage.SetDimensions(sourcePage.Width, sourcePage.Height);
                            splitDoc.AddPage(newPage);
                        }

                        results[i] = splitDoc;
                    }

                    _logger.LogInformation("Successfully split PDF");
                    return results;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error splitting PDF");
                    throw;
                }
            });
        }

        public async Task<SKBitmap> ExtractPageThumbnailAsync(CorePdfDocument document, int pageIndex, int maxWidth)
        {
            _logger.LogDebug("Extracting thumbnail for page {PageIndex}", pageIndex);

            return await Task.Run(() =>
            {
                try
                {
                    var page = document.Pages[pageIndex];
                    
                    // すでにサムネイルが設定されている場合はそれを返す
                    if (page.ThumbnailImage != null)
                    {
                        // 既存のサムネイルが指定サイズより大きい場合はリサイズ
                        if (page.ThumbnailImage.Width > maxWidth)
                        {
                            var aspectRatio = (float)page.ThumbnailImage.Height / page.ThumbnailImage.Width;
                            var targetHeight = (int)(maxWidth * aspectRatio);
                            
                            var resizedBitmap = new SKBitmap(maxWidth, targetHeight);
                            using (var canvas = new SKCanvas(resizedBitmap))
                            {
                                using (var paint = new SKPaint())
                                {
                                    paint.IsAntialias = true;
                                    paint.FilterQuality = SKFilterQuality.High;
                                    
                                    var destRect = SKRect.Create(0, 0, maxWidth, targetHeight);
                                    canvas.DrawBitmap(page.ThumbnailImage, destRect, paint);
                                }
                            }
                            return resizedBitmap;
                        }
                        else
                        {
                            // サイズが適切な場合はそのまま返す
                            return page.ThumbnailImage.Copy();
                        }
                    }
                    
                    // サムネイルがない場合はプレースホルダー画像を生成
                    var placeholderAspectRatio = page.Height / page.Width;
                    var placeholderHeight = (int)(maxWidth * placeholderAspectRatio);

                    var bitmap = new SKBitmap(maxWidth, placeholderHeight);
                    using (var canvas = new SKCanvas(bitmap))
                    {
                        // 白背景
                        canvas.Clear(SKColors.White);

                        // 枠線
                        using (var paint = new SKPaint())
                        {
                            paint.Color = SKColors.LightGray;
                            paint.Style = SKPaintStyle.Stroke;
                            paint.StrokeWidth = 2;
                            canvas.DrawRect(1, 1, maxWidth - 2, placeholderHeight - 2, paint);

                            // ページ番号
                            paint.Color = SKColors.Gray;
                            paint.Style = SKPaintStyle.Fill;
                            paint.TextSize = 24;
                            paint.TextAlign = SKTextAlign.Center;
                            canvas.DrawText($"Page {page.PageNumber}", maxWidth / 2, placeholderHeight / 2, paint);
                        }
                    }

                    return bitmap;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error extracting thumbnail for page {PageIndex}", pageIndex);
                    throw;
                }
            });
        }

        public async Task<SKBitmap> ExtractPagePreviewAsync(CorePdfDocument document, int pageIndex, float scale)
        {
            _logger.LogDebug("Extracting preview for page {PageIndex} at scale {Scale}", pageIndex, scale);

            return await Task.Run(() =>
            {
                try
                {
                    var page = document.Pages[pageIndex];
                    
                    // 画像ベースのページで元画像がある場合は、それを使用
                    if (!string.IsNullOrEmpty(page.SourceImagePath) && File.Exists(page.SourceImagePath))
                    {
                        using var originalBitmap = SKBitmap.Decode(page.SourceImagePath);
                        if (originalBitmap != null)
                        {
                            // 回転を適用
                            var rotatedBitmap = RotateBitmap(originalBitmap, page.Rotation);
                            
                            // スケーリング
                            var scaledWidth = (int)(rotatedBitmap.Width * scale);
                            var scaledHeight = (int)(rotatedBitmap.Height * scale);
                            
                            var scaledBitmap = new SKBitmap(scaledWidth, scaledHeight);
                            using (var canvas = new SKCanvas(scaledBitmap))
                            {
                                using (var paint = new SKPaint())
                                {
                                    paint.IsAntialias = true;
                                    paint.FilterQuality = SKFilterQuality.High;
                                    
                                    var destRect = SKRect.Create(0, 0, scaledWidth, scaledHeight);
                                    canvas.DrawBitmap(rotatedBitmap, destRect, paint);
                                }
                            }
                            
                            rotatedBitmap.Dispose();
                            return scaledBitmap;
                        }
                    }
                    // すでにサムネイルが設定されている場合は、それを拡大して使用
                    else if (page.ThumbnailImage != null)
                    {
                        var scaledWidth = (int)(page.ThumbnailImage.Width * scale);
                        var scaledHeight = (int)(page.ThumbnailImage.Height * scale);
                        
                        var scaledBitmap = new SKBitmap(scaledWidth, scaledHeight);
                        using (var canvas = new SKCanvas(scaledBitmap))
                        {
                            using (var paint = new SKPaint())
                            {
                                paint.IsAntialias = true;
                                paint.FilterQuality = SKFilterQuality.High;
                                
                                var destRect = SKRect.Create(0, 0, scaledWidth, scaledHeight);
                                canvas.DrawBitmap(page.ThumbnailImage, destRect, paint);
                            }
                        }
                        return scaledBitmap;
                    }
                    
                    // サムネイルがない場合はプレースホルダーを生成
                    var width = (int)(page.Width * scale);
                    var height = (int)(page.Height * scale);

                    var bitmap = new SKBitmap(width, height);
                    using (var canvas = new SKCanvas(bitmap))
                    {
                        // 白背景
                        canvas.Clear(SKColors.White);

                        // 枠線とコンテンツプレースホルダー
                        using (var paint = new SKPaint())
                        {
                            paint.Color = SKColors.LightGray;
                            paint.Style = SKPaintStyle.Stroke;
                            paint.StrokeWidth = 1;
                            canvas.DrawRect(0, 0, width - 1, height - 1, paint);

                            // コンテンツ領域
                            paint.Color = SKColors.WhiteSmoke;
                            paint.Style = SKPaintStyle.Fill;
                            var margin = width * 0.1f;
                            canvas.DrawRect(margin, margin, width - 2 * margin, height - 2 * margin, paint);

                            // ページ番号
                            paint.Color = SKColors.Gray;
                            paint.TextSize = 48 * scale;
                            paint.TextAlign = SKTextAlign.Center;
                            canvas.DrawText($"Page {page.PageNumber}", width / 2, height / 2, paint);
                        }
                    }

                    return bitmap;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error extracting preview for page {PageIndex}", pageIndex);
                    throw;
                }
            });
        }

        public int GetPageCount(string filePath)
        {
            try
            {
                using (var doc = PdfReader.Open(filePath, PdfDocumentOpenMode.InformationOnly))
                {
                    return doc.PageCount;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting page count for {FilePath}", filePath);
                return 0;
            }
        }

        public long GetFileSize(string filePath)
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                return fileInfo.Exists ? fileInfo.Length : 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file size for {FilePath}", filePath);
                return 0;
            }
        }

        public bool IsValidPdf(string filePath)
        {
            try
            {
                using (var doc = PdfReader.Open(filePath, PdfDocumentOpenMode.InformationOnly))
                {
                    return doc.PageCount > 0;
                }
            }
            catch
            {
                return false;
            }
        }

        public async Task<CorePdfDocument> CreatePdfFromImageAsync(string imagePath, string outputPath)
        {
            // SimplePdfServiceを使用（PDFsharpCore依存を完全に回避）
            return await SimplePdfService.CreatePdfFromImageSimpleAsync(imagePath, outputPath, _logger);
        }

        public async Task<CorePdfDocument> CreatePdfFromImagesAsync(IEnumerable<string> imagePaths, string outputPath)
        {
            _logger.LogInformation("Creating PDF from {Count} images", imagePaths.Count());

            // SimplePdfServiceを使用（PDFsharpCore依存を完全に回避）
            return await SimplePdfService.CreatePdfFromImagesSimpleAsync(imagePaths, outputPath, _logger);
        }

        private async Task<CorePdfDocument> CreatePdfFromImagesAsyncOld(IEnumerable<string> imagePaths, string outputPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var outputDoc = new PdfSharpCore.Pdf.PdfDocument();
                    
                    foreach (var imagePath in imagePaths)
                    {
                        var page = outputDoc.AddPage();
                        page.Size = PdfSharpCore.PageSize.A4;
                        
                        using (var graphics = PdfSharpCore.Drawing.XGraphics.FromPdfPage(page))
                        {
                            // メモリストリーム経由で画像を読み込み（ImageSharp依存を回避）
                        using (var bitmap = SKBitmap.Decode(imagePath))
                        {
                            var tempPath = Path.GetTempFileName() + ".png";
                            try
                            {
                                using (var skImage = SKImage.FromBitmap(bitmap))
                                using (var data = skImage.Encode(SKEncodedImageFormat.Png, 100))
                                using (var stream = File.OpenWrite(tempPath))
                                {
                                    data.SaveTo(stream);
                                }

                                using (var ms = new MemoryStream(File.ReadAllBytes(tempPath)))
                                using (var image = PdfSharpCore.Drawing.XImage.FromStream(() => ms))
                            {
                                var pageWidth = page.Width;
                                var pageHeight = page.Height;
                                var imageAspect = (double)image.PixelWidth / image.PixelHeight;
                                var pageAspect = pageWidth / pageHeight;
                                
                                double drawWidth, drawHeight;
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
                                
                                var x = (pageWidth - drawWidth) / 2;
                                var y = (pageHeight - drawHeight) / 2;
                                
                                graphics.DrawImage(image, x, y, drawWidth, drawHeight);
                                }
                            }
                            finally
                            {
                                if (File.Exists(tempPath))
                                    File.Delete(tempPath);
                            }
                        }
                        }
                    }
                    
                    outputDoc.Save(outputPath);
                    
                    // 保存したPDFを読み込む
                    var document = LoadPdfAsync(outputPath).GetAwaiter().GetResult();
                    
                    _logger.LogInformation("Successfully created PDF from {Count} images", imagePaths.Count());
                    return document;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating PDF from images");
                    throw;
                }
            });
        }

        public async Task<CorePdfDocument> CreatePdfFromImageSkiaSharpAsync(string imagePath, string outputPath)
        {
            _logger.LogInformation("Creating PDF from image {ImagePath} using SimplePdfService", imagePath);

            // SimplePdfServiceを使用（PDFsharpCore依存を完全に回避）
            return await SimplePdfService.CreatePdfFromImageSimpleAsync(imagePath, outputPath, _logger);
        }

        private SKBitmap RotateBitmap(SKBitmap source, int degrees)
        {
            if (degrees == 0) return source.Copy();
            
            var radians = degrees * Math.PI / 180;
            var sine = Math.Abs(Math.Sin(radians));
            var cosine = Math.Abs(Math.Cos(radians));
            var originalWidth = source.Width;
            var originalHeight = source.Height;
            
            // 回転後のサイズを計算
            var rotatedWidth = (int)(cosine * originalWidth + sine * originalHeight);
            var rotatedHeight = (int)(cosine * originalHeight + sine * originalWidth);
            
            var rotatedBitmap = new SKBitmap(rotatedWidth, rotatedHeight);
            
            using (var canvas = new SKCanvas(rotatedBitmap))
            {
                canvas.Clear(SKColors.Transparent);
                canvas.Translate(rotatedWidth / 2, rotatedHeight / 2);
                canvas.RotateDegrees(degrees);
                canvas.Translate(-originalWidth / 2, -originalHeight / 2);
                canvas.DrawBitmap(source, 0, 0);
            }
            
            return rotatedBitmap;
        }

        private async Task<CorePdfDocument> CreatePdfFromImageSkiaSharpAsyncOld(string imagePath, string outputPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var outputDoc = new PdfSharpCore.Pdf.PdfDocument();
                    var page = outputDoc.AddPage();
                    page.Size = PdfSharpCore.PageSize.A4;

                    using (var bitmap = SKBitmap.Decode(imagePath))
                    {
                        using (var graphics = PdfSharpCore.Drawing.XGraphics.FromPdfPage(page))
                        {
                            // SkiaSharpのビットマップを一時的なPNG形式で保存
                            var tempPath = Path.GetTempFileName() + ".png";
                            try
                            {
                                using (var image = SKImage.FromBitmap(bitmap))
                                using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                                using (var stream = File.OpenWrite(tempPath))
                                {
                                    data.SaveTo(stream);
                                }

                                // PDFsharpCoreで画像を読み込み（メモリストリーム経由）
                                using (var ms = new MemoryStream(File.ReadAllBytes(tempPath)))
                                using (var xImage = PdfSharpCore.Drawing.XImage.FromStream(() => ms))
                                {
                                    var pageWidth = page.Width;
                                    var pageHeight = page.Height;
                                    var imageAspect = (double)xImage.PixelWidth / xImage.PixelHeight;
                                    var pageAspect = pageWidth / pageHeight;

                                    double drawWidth, drawHeight;
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

                                    var x = (pageWidth - drawWidth) / 2;
                                    var y = (pageHeight - drawHeight) / 2;

                                    graphics.DrawImage(xImage, x, y, drawWidth, drawHeight);
                                }
                            }
                            finally
                            {
                                if (File.Exists(tempPath))
                                {
                                    File.Delete(tempPath);
                                }
                            }
                        }
                    }

                    outputDoc.Save(outputPath);

                    // 保存したPDFを読み込む
                    var document = LoadPdfAsync(outputPath).GetAwaiter().GetResult();

                    _logger.LogInformation("Successfully created PDF from image using SkiaSharp");
                    return document;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating PDF from image using SkiaSharp");
                    throw;
                }
            });
        }
    }
}