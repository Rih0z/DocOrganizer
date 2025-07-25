using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using DocOrganizer.Application.Interfaces;
using DocOrganizer.Core.Models;

namespace DocOrganizer.Infrastructure.Services
{
    /// <summary>
    /// PDF編集サービスの実装
    /// </summary>
    public class PdfEditorService : IPdfEditorService
    {
        private readonly IPdfService _pdfService;
        private readonly ILogger<PdfEditorService> _logger;
        private readonly Stack<PdfDocument> _undoStack = new();
        private readonly Stack<PdfDocument> _redoStack = new();

        public PdfDocument? CurrentDocument { get; private set; }

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public PdfEditorService(IPdfService pdfService, ILogger<PdfEditorService> logger)
        {
            _pdfService = pdfService;
            _logger = logger;
        }

        public async Task OpenFileAsync(string filePath)
        {
            _logger.LogInformation("Opening PDF file: {FilePath}", filePath);

            try
            {
                CurrentDocument?.Dispose();
                CurrentDocument = await _pdfService.LoadPdfAsync(filePath);
                
                // サムネイルを生成
                await UpdateAllThumbnailsAsync();
                
                // Undo/Redoスタックをクリア
                ClearHistory();
                
                _logger.LogInformation("Successfully opened PDF with {PageCount} pages", CurrentDocument.GetPageCount());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open PDF file: {FilePath}", filePath);
                throw;
            }
        }

        public async Task SaveAsync()
        {
            if (CurrentDocument == null || string.IsNullOrEmpty(CurrentDocument.FilePath))
            {
                throw new InvalidOperationException("No document is currently open or file path is not set");
            }

            await SaveAsAsync(CurrentDocument.FilePath);
        }

        public async Task SaveAsAsync(string filePath)
        {
            if (CurrentDocument == null)
            {
                throw new InvalidOperationException("No document is currently open");
            }

            _logger.LogInformation("Saving PDF to: {FilePath}", filePath);

            try
            {
                await _pdfService.SavePdfAsync(CurrentDocument, filePath);
                CurrentDocument.FilePath = filePath;
                CurrentDocument.ClearModifiedFlag();
                
                _logger.LogInformation("Successfully saved PDF");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save PDF to: {FilePath}", filePath);
                throw;
            }
        }

        public void CloseDocument()
        {
            _logger.LogInformation("Closing current document");
            
            CurrentDocument?.Dispose();
            CurrentDocument = null;
            ClearHistory();
        }

        public async Task RemovePagesAsync(int[] pageIndices)
        {
            if (CurrentDocument == null)
            {
                throw new InvalidOperationException("No document is currently open");
            }

            _logger.LogInformation("Removing {Count} pages", pageIndices.Length);
            
            SaveStateForUndo();

            // インデックスを降順でソート（後ろから削除するため）
            var sortedIndices = pageIndices.OrderByDescending(i => i).ToArray();
            
            foreach (var index in sortedIndices)
            {
                if (index >= 0 && index < CurrentDocument.Pages.Count)
                {
                    CurrentDocument.RemovePageAt(index);
                }
            }

            await Task.CompletedTask;
        }

        public async Task RotatePagesAsync(int[] pageIndices, int degrees)
        {
            if (CurrentDocument == null)
            {
                throw new InvalidOperationException("No document is currently open");
            }

            _logger.LogInformation("Rotating {Count} pages by {Degrees} degrees", pageIndices.Length, degrees);
            
            SaveStateForUndo();
            CurrentDocument.RotatePages(pageIndices, degrees);
            
            // 該当ページのプレビューを更新
            foreach (var index in pageIndices)
            {
                await UpdatePageThumbnailAsync(index);
            }
        }

        public async Task ReorderPagesAsync(int fromIndex, int toIndex)
        {
            if (CurrentDocument == null)
            {
                throw new InvalidOperationException("No document is currently open");
            }

            _logger.LogInformation("Reordering page from {FromIndex} to {ToIndex}", fromIndex, toIndex);
            
            SaveStateForUndo();
            CurrentDocument.MovePage(fromIndex, toIndex);
            
            await Task.CompletedTask;
        }

        public async Task MergeWithAsync(string[] filePaths)
        {
            if (CurrentDocument == null)
            {
                throw new InvalidOperationException("No document is currently open");
            }

            _logger.LogInformation("Merging with {Count} files", filePaths.Length);
            
            SaveStateForUndo();

            var documentsToMerge = new List<PdfDocument> { CurrentDocument };
            
            foreach (var filePath in filePaths)
            {
                var doc = await _pdfService.LoadPdfAsync(filePath);
                documentsToMerge.Add(doc);
            }

            var mergedDoc = await _pdfService.MergePdfsAsync(documentsToMerge.ToArray());
            
            // 追加されたドキュメントを破棄
            for (int i = 1; i < documentsToMerge.Count; i++)
            {
                documentsToMerge[i].Dispose();
            }

            // 現在のドキュメントを更新
            CurrentDocument = mergedDoc;
            await UpdateAllThumbnailsAsync();
        }

        public async Task SplitDocumentAsync(int[] splitPoints, string outputDirectory, string fileNamePattern)
        {
            if (CurrentDocument == null)
            {
                throw new InvalidOperationException("No document is currently open");
            }

            _logger.LogInformation("Splitting document at {Count} points", splitPoints.Length);

            var ranges = new List<(int, int)>();
            int startPage = 1;

            foreach (var splitPoint in splitPoints.OrderBy(p => p))
            {
                if (splitPoint > startPage)
                {
                    ranges.Add((startPage, splitPoint - 1));
                    startPage = splitPoint;
                }
            }
            
            // 最後の範囲
            if (startPage <= CurrentDocument.GetPageCount())
            {
                ranges.Add((startPage, CurrentDocument.GetPageCount()));
            }

            var splitDocs = await _pdfService.SplitPdfAsync(CurrentDocument, ranges.ToArray());

            // ファイルとして保存
            for (int i = 0; i < splitDocs.Length; i++)
            {
                var fileName = string.Format(fileNamePattern, i + 1);
                var filePath = Path.Combine(outputDirectory, fileName);
                await _pdfService.SavePdfAsync(splitDocs[i], filePath);
                splitDocs[i].Dispose();
            }
        }

        public async Task UpdatePageThumbnailAsync(int pageIndex)
        {
            if (CurrentDocument == null || pageIndex < 0 || pageIndex >= CurrentDocument.Pages.Count)
            {
                return;
            }

            var page = CurrentDocument.Pages[pageIndex];
            
            // すでにサムネイルが存在する場合はスキップ
            if (page.ThumbnailImage != null)
            {
                _logger.LogDebug("Page {PageNumber} already has thumbnail", page.PageNumber);
                return;
            }
            
            var thumbnail = await _pdfService.ExtractPageThumbnailAsync(CurrentDocument, pageIndex, 120);
            page.SetThumbnailImage(thumbnail);
            // サムネイルはpageが管理するため、ここではdisposeしない
        }

        public async Task UpdateAllThumbnailsAsync()
        {
            if (CurrentDocument == null)
            {
                return;
            }

            _logger.LogInformation("Updating all thumbnails");

            for (int i = 0; i < CurrentDocument.Pages.Count; i++)
            {
                await UpdatePageThumbnailAsync(i);
            }
        }

        public async Task UndoAsync()
        {
            if (!CanUndo)
            {
                return;
            }

            _logger.LogInformation("Performing undo");

            // 現在の状態をRedoスタックに保存
            if (CurrentDocument != null)
            {
                _redoStack.Push(CloneDocument(CurrentDocument));
            }

            // Undoスタックから復元
            CurrentDocument?.Dispose();
            CurrentDocument = _undoStack.Pop();
            
            await UpdateAllThumbnailsAsync();
        }

        public async Task RedoAsync()
        {
            if (!CanRedo)
            {
                return;
            }

            _logger.LogInformation("Performing redo");

            // 現在の状態をUndoスタックに保存
            if (CurrentDocument != null)
            {
                _undoStack.Push(CloneDocument(CurrentDocument));
            }

            // Redoスタックから復元
            CurrentDocument?.Dispose();
            CurrentDocument = _redoStack.Pop();
            
            await UpdateAllThumbnailsAsync();
        }

        private void SaveStateForUndo()
        {
            if (CurrentDocument != null)
            {
                _undoStack.Push(CloneDocument(CurrentDocument));
                _redoStack.Clear();
            }
        }

        private void ClearHistory()
        {
            while (_undoStack.Count > 0)
            {
                _undoStack.Pop().Dispose();
            }
            
            while (_redoStack.Count > 0)
            {
                _redoStack.Pop().Dispose();
            }
        }

        private PdfDocument CloneDocument(PdfDocument source)
        {
            var clone = new PdfDocument(source.FilePath ?? string.Empty);
            
            foreach (var page in source.Pages)
            {
                var clonedPage = new PdfPage(page.PageNumber)
                {
                    Rotation = page.Rotation
                };
                clonedPage.SetDimensions(page.Width, page.Height);
                clone.AddPage(clonedPage);
            }

            if (!source.IsModified)
            {
                clone.ClearModifiedFlag();
            }

            return clone;
        }

        public async Task<PdfDocument> OpenPdfAsync(string filePath)
        {
            _logger.LogInformation("Opening PDF file: {FilePath}", filePath);

            try
            {
                var document = await _pdfService.LoadPdfAsync(filePath);
                
                _logger.LogInformation("Successfully opened PDF with {PageCount} pages", document.GetPageCount());
                return document;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open PDF: {FilePath}", filePath);
                throw;
            }
        }

        public async Task<bool> SavePdfAsync(PdfDocument document, string filePath)
        {
            _logger.LogInformation("Saving PDF to: {FilePath}", filePath);

            try
            {
                await _pdfService.SavePdfAsync(document, filePath);
                document.FilePath = filePath;
                document.ClearModifiedFlag();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save PDF: {FilePath}", filePath);
                return false;
            }
        }

        public void RotatePage(PdfPage page, int degrees)
        {
            _logger.LogInformation("Rotating page {PageNumber} by {Degrees} degrees", page.PageNumber, degrees);

            try
            {
                int newRotation = (page.Rotation + degrees) % 360;
                // 負の値を正の値に変換
                if (newRotation < 0)
                {
                    newRotation += 360;
                }
                
                // 90度単位に正規化（0, 90, 180, 270のみ）
                newRotation = ((newRotation + 45) / 90) * 90;
                if (newRotation >= 360)
                {
                    newRotation = 0;
                }
                
                page.Rotation = newRotation;
                
                // ドキュメントを変更済みとしてマーク
                if (CurrentDocument != null)
                {
                    CurrentDocument.RotatePages(new[] { page.PageNumber - 1 }, 0); // IsModifiedフラグを設定
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to rotate page {PageNumber}", page.PageNumber);
                throw;
            }
        }

        public void RemovePage(PdfDocument document, int pageIndex)
        {
            _logger.LogInformation("Removing page at index {PageIndex}", pageIndex);

            try
            {
                if (pageIndex >= 0 && pageIndex < document.GetPageCount())
                {
                    document.RemovePageAt(pageIndex);
                    // IsModifiedは自動的に設定される
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove page at index {PageIndex}", pageIndex);
                throw;
            }
        }

        public async Task<PdfDocument> MergePdfsAsync(string[] filePaths)
        {
            _logger.LogInformation("Merging {Count} PDF files", filePaths.Length);

            try
            {
                var documents = new List<PdfDocument>();
                foreach (var path in filePaths)
                {
                    var doc = await _pdfService.LoadPdfAsync(path);
                    documents.Add(doc);
                }

                var mergedDocument = await _pdfService.MergePdfsAsync(documents.ToArray());
                
                // 使用したドキュメントを破棄
                foreach (var doc in documents)
                {
                    doc.Dispose();
                }

                return mergedDocument;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to merge PDF files");
                throw;
            }
        }

        public void ReorderPages(PdfDocument document, PdfPage[] newOrder)
        {
            _logger.LogInformation("Reordering pages in document");

            try
            {
                // PdfDocumentには専用のReorderPagesメソッドがないため、
                // 現在のページリストと新しい順序を比較して、MovePage操作で実現する
                var currentPages = document.Pages.ToList();
                
                for (int i = 0; i < newOrder.Length; i++)
                {
                    var targetPage = newOrder[i];
                    var currentIndex = -1;
                    
                    // 現在のインデックスを見つける
                    for (int j = i; j < currentPages.Count; j++)
                    {
                        if (currentPages[j].PageNumber == targetPage.PageNumber)
                        {
                            currentIndex = j;
                            break;
                        }
                    }
                    
                    if (currentIndex >= 0 && currentIndex != i)
                    {
                        document.MovePage(currentIndex, i);
                        // リストも更新
                        var page = currentPages[currentIndex];
                        currentPages.RemoveAt(currentIndex);
                        currentPages.Insert(i, page);
                    }
                }
                // IsModifiedは自動的に設定される
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reorder pages");
                throw;
            }
        }

        public async Task<SkiaSharp.SKBitmap> GetPagePreviewAsync(PdfDocument document, int pageIndex, float scale)
        {
            _logger.LogInformation("Getting page preview for page {PageIndex} at scale {Scale}", pageIndex, scale);

            try
            {
                if (document == null || pageIndex < 0 || pageIndex >= document.Pages.Count)
                {
                    throw new ArgumentException("Invalid document or page index");
                }

                // PdfServiceを使用してプレビューを生成
                return await _pdfService.ExtractPagePreviewAsync(document, pageIndex, scale);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get page preview");
                throw;
            }
        }
    }
}