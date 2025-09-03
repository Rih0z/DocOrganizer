using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using DocOrganizer.Application.Interfaces.V3;
using PdfiumViewer;
using System.Drawing;
using System.Drawing.Imaging;

namespace DocOrganizer.Infrastructure.Services.V3
{
    /// <summary>
    /// 🎯 V3.0.028 PdfiumViewer.Updated実装 - PDF→画像変換サービス
    /// GhostScript完全不要、Chrome実績のPDFiumエンジン採用
    /// MagickNetPdfRenderingServiceパターン完全踏襲による置換実装
    /// </summary>
    public class PdfiumViewerRenderingService : IPdfRenderingService, IDisposable
    {
        private readonly ILogger<PdfiumViewerRenderingService> _logger;
        private readonly ConcurrentBag<string> _tempFiles = new();
        private bool _disposed = false;
        
        public PdfiumViewerRenderingService(ILogger<PdfiumViewerRenderingService> logger)
        {
            _logger = logger;
            _logger.LogInformation("[PDFIUM_RENDERING] PdfiumViewer PDF Provider初期化完了 - GhostScript完全不要");
        }
        
        /// <summary>
        /// PDFページを一時画像ファイルに変換（HEICパターン踏襲・PDFium実装）
        /// </summary>
        public async Task<string> ConvertPdfPageToTempImageAsync(string pdfPath, int pageIndex, int dpi = 150)
        {
            return await Task.Run(() =>
            {
                try
                {
                    _logger.LogDebug("[PDFIUM_RENDERING] ページ変換開始: {FileName}, Page: {PageIndex}, DPI: {Dpi}", 
                        Path.GetFileName(pdfPath), pageIndex, dpi);
                    
                    using var document = PdfDocument.Load(pdfPath);
                    
                    if (pageIndex >= document.PageCount)
                    {
                        throw new ArgumentException($"ページインデックス {pageIndex} が範囲外です。総ページ数: {document.PageCount}");
                    }
                    
                    // DPIベースサイズ計算（標準PDF単位: 72DPI）
                    var dpiScale = (float)dpi / 72.0f;
                    var pageSize = document.PageSizes[pageIndex];
                    var renderWidth = (int)(pageSize.Width * dpiScale);
                    var renderHeight = (int)(pageSize.Height * dpiScale);
                    
                    using var image = document.Render(pageIndex, renderWidth, renderHeight, dpi, dpi, false);
                    
                    // 一時ファイルパス生成
                    var tempImagePath = Path.GetTempFileName() + ".png";
                    _tempFiles.Add(tempImagePath);
                    
                    // PNG形式で保存（品質重視）
                    image.Save(tempImagePath, ImageFormat.Png);
                    
                    _logger.LogDebug("[PDFIUM_RENDERING] ページ変換完了: {TempPath}, サイズ: {Width}x{Height}", 
                        tempImagePath, image.Width, image.Height);
                    
                    return tempImagePath;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[PDFIUM_RENDERING] ページ変換エラー: {FilePath}, Page: {PageIndex}", pdfPath, pageIndex);
                    throw;
                }
            });
        }
        
        /// <summary>
        /// PDF基本情報を取得（PDFium高速アクセス）
        /// </summary>
        public async Task<PdfInfo> GetPdfInfoAsync(string pdfPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    _logger.LogDebug("[PDFIUM_RENDERING] PDF情報取得開始: {FileName}", Path.GetFileName(pdfPath));
                    
                    using var document = PdfDocument.Load(pdfPath);
                    var fileInfo = new FileInfo(pdfPath);
                    
                    // 最初のページからサイズを取得（PDFium最適化）
                    var firstPageSize = document.PageSizes[0];
                    
                    var pdfInfo = new PdfInfo(
                        PageCount: document.PageCount,
                        Width: (int)firstPageSize.Width,
                        Height: (int)firstPageSize.Height,
                        FileSize: fileInfo.Length,
                        Version: "PDFium" // PDFiumエンジン使用を明示
                    );
                    
                    _logger.LogDebug("[PDFIUM_RENDERING] PDF情報取得完了: {PageCount}ページ, {Width}x{Height}", 
                        pdfInfo.PageCount, pdfInfo.Width, pdfInfo.Height);
                    
                    return pdfInfo;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[PDFIUM_RENDERING] PDF情報取得エラー: {FilePath}", pdfPath);
                    throw;
                }
            });
        }
        
        /// <summary>
        /// 一時ファイルのクリーンアップ（MagickNet踏襲実装）
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
                        _logger.LogWarning(ex, "[PDFIUM_RENDERING] 一時ファイル削除失敗: {TempFile}", tempFile);
                    }
                }
                
                if (cleanedCount > 0)
                {
                    _logger.LogDebug("[PDFIUM_RENDERING] 一時ファイル削除完了: {Count}ファイル", cleanedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PDFIUM_RENDERING] クリーンアップエラー");
            }
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    CleanupTempFiles();
                    _logger.LogDebug("[PDFIUM_RENDERING] PdfiumViewerRenderingService リソース解放完了");
                }
                _disposed = true;
            }
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        ~PdfiumViewerRenderingService()
        {
            Dispose(false);
        }
    }
}