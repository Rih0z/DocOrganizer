using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using DocOrganizer.Application.Interfaces.V3;
using ImageMagick;

namespace DocOrganizer.Infrastructure.Services.V3
{
    /// <summary>
    /// 🎯 V3.0.025 Magick.NET実装 - PDF→画像変換サービス
    /// HEICConversionServiceパターン完全踏襲による統合実装（PdfiumSharp→Magick.NET変更）
    /// </summary>
    public class MagickNetPdfRenderingService : IPdfRenderingService, IDisposable
    {
        private readonly ILogger<MagickNetPdfRenderingService> _logger;
        private readonly ConcurrentBag<string> _tempFiles = new();
        private bool _disposed = false;
        
        public MagickNetPdfRenderingService(ILogger<MagickNetPdfRenderingService> logger)
        {
            _logger = logger;
        }
        
        /// <summary>
        /// PDFページを一時画像ファイルに変換（HEIC変換サービスパターン準拠）
        /// </summary>
        public async Task<string> ConvertPdfPageToTempImageAsync(string pdfPath, int pageIndex, int dpi = 150)
        {
            return await Task.Run(() =>
            {
                try
                {
                    _logger.LogDebug("[PDF_RENDERING] ページ変換開始: {FileName}, Page: {PageIndex}, DPI: {Dpi}", 
                        Path.GetFileName(pdfPath), pageIndex, dpi);
                    
                    // Magick.NET設定
                    var settings = new MagickReadSettings
                    {
                        Density = new Density(dpi, dpi),
                        Format = MagickFormat.Pdf
                    };
                    
                    using var images = new MagickImageCollection();
                    images.Read(pdfPath, settings);
                    
                    if (pageIndex >= images.Count)
                    {
                        throw new ArgumentException($"ページインデックス {pageIndex} が範囲外です。総ページ数: {images.Count}");
                    }
                    
                    var page = images[pageIndex];
                    
                    // 背景を白に設定（透明部分対応）
                    page.BackgroundColor = MagickColors.White;
                    page.Alpha(AlphaOption.Remove);
                    
                    // 一時ファイルパス生成
                    var tempImagePath = Path.GetTempFileName() + ".png";
                    _tempFiles.Add(tempImagePath);
                    
                    // PNG形式で保存
                    page.Format = MagickFormat.Png;
                    page.Write(tempImagePath);
                    
                    _logger.LogDebug("[PDF_RENDERING] ページ変換完了: {TempPath}, サイズ: {Width}x{Height}", 
                        tempImagePath, page.Width, page.Height);
                    
                    return tempImagePath;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[PDF_RENDERING] ページ変換エラー: {FilePath}, Page: {PageIndex}", pdfPath, pageIndex);
                    throw;
                }
            });
        }
        
        /// <summary>
        /// PDF基本情報を取得
        /// </summary>
        public async Task<PdfInfo> GetPdfInfoAsync(string pdfPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    _logger.LogDebug("[PDF_RENDERING] PDF情報取得開始: {FileName}", Path.GetFileName(pdfPath));
                    
                    var settings = new MagickReadSettings
                    {
                        Format = MagickFormat.Pdf
                    };
                    
                    using var images = new MagickImageCollection();
                    images.Read(pdfPath, settings);
                    
                    var fileInfo = new FileInfo(pdfPath);
                    
                    // 最初のページからサイズを取得
                    var firstPage = images[0];
                    
                    var pdfInfo = new PdfInfo(
                        PageCount: images.Count,
                        Width: firstPage.Width,
                        Height: firstPage.Height,
                        FileSize: fileInfo.Length,
                        Version: "Unknown" // Magick.NETではPDFバージョン取得が困難
                    );
                    
                    _logger.LogDebug("[PDF_RENDERING] PDF情報取得完了: {PageCount}ページ, {Width}x{Height}", 
                        pdfInfo.PageCount, pdfInfo.Width, pdfInfo.Height);
                    
                    return pdfInfo;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[PDF_RENDERING] PDF情報取得エラー: {FilePath}", pdfPath);
                    throw;
                }
            });
        }
        
        /// <summary>
        /// 一時ファイルのクリーンアップ
        /// </summary>
        public void CleanupTempFiles()
        {
            try
            {
                var cleanedCount = 0;
                while (_tempFiles.TryTake(out var tempFile))
                {
                    try
                    {
                        if (File.Exists(tempFile))
                        {
                            File.Delete(tempFile);
                            cleanedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[PDF_RENDERING] 一時ファイル削除失敗: {TempFile}", tempFile);
                    }
                }
                
                if (cleanedCount > 0)
                {
                    _logger.LogDebug("[PDF_RENDERING] 一時ファイル削除完了: {Count}ファイル", cleanedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PDF_RENDERING] クリーンアップエラー");
            }
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    CleanupTempFiles();
                }
                _disposed = true;
            }
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        ~MagickNetPdfRenderingService()
        {
            Dispose(false);
        }
    }
}