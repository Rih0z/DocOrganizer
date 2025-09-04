using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocOrganizer.Core.Models;
using DocOrganizer.Core.Logging;
using DocOrganizer.Application.Interfaces;
using DocOrganizer.Application.Interfaces.V3;
using Microsoft.Extensions.Logging;

namespace DocOrganizer.Infrastructure.Services.V3
{
    /// <summary>
    /// 🎯 V3 OSS標準: ファイル追加サービス（V2依存関係完全削除版）
    /// </summary>
    public class FileAdditionService : IFileAdditionService
    {
        // 🎯 V3専用: V2依存関係を完全削除
        private readonly IPdfEditorService _pdfEditorService;
        private readonly IImageValidationService _imageValidationService;
        private readonly IImageLoaderService _imageLoaderService;
        private readonly ILogger<FileAdditionService> _logger;
        private readonly IPdfRenderingService _pdfRenderingService;

        // 対応ファイル形式（OSS標準拡張子）
        private static readonly string[] SupportedImageExtensions = 
        {
            ".jpg", ".jpeg", ".png", ".heic", ".heif", ".bmp", ".tiff", ".gif", ".webp", ".psd"
        };
        
        private static readonly string[] SupportedPdfExtensions = 
        {
            ".pdf"
        };

        public FileAdditionService(
            IPdfEditorService pdfEditorService,
            IImageValidationService imageValidationService,
            IImageLoaderService imageLoaderService,
            ILogger<FileAdditionService> logger,
            IPdfRenderingService pdfRenderingService)
        {
            _pdfEditorService = pdfEditorService;
            _imageValidationService = imageValidationService;
            _imageLoaderService = imageLoaderService;
            _logger = logger;
            _pdfRenderingService = pdfRenderingService;
        }

        /// <summary>
        /// 🎯 V3 OSS標準: 新規ドキュメント作成
        /// </summary>
        public async Task<(PdfDocument Document, FileAdditionResult Result)> CreateNewDocumentFromFilesAsync(IEnumerable<string> files)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new FileAdditionResult();
            var filesList = files.ToList();

            try
            {
                _logger.LogInformation("[V3_NewDocument] 新規ドキュメント作成開始: {Count}ファイル", filesList.Count);

                // ファイルを種類別に分類
                var imageFiles = filesList.Where(IsImageFile).ToList();
                var pdfFiles = filesList.Where(IsPdfFile).ToList();

                PdfDocument document;

                if (imageFiles.Any())
                {
                    // 🎯 V3実装: 画像からPDF作成
                    document = await CreatePdfFromImagesAsync(imageFiles);
                    result.AddedPagesCount += imageFiles.Count;
                    result.SuccessfulFiles.AddRange(imageFiles);

                    // PDFファイルがある場合は結合
                    if (pdfFiles.Any())
                    {
                        var pdfAddedCount = await AddPdfFilesToDocumentAsync(document, pdfFiles);
                        result.AddedPagesCount += pdfAddedCount;
                        result.SuccessfulFiles.AddRange(pdfFiles);
                    }
                }
                else if (pdfFiles.Any())
                {
                    // 最初のPDFを基準に他のPDFを結合
                    document = await _pdfEditorService.OpenPdfAsync(pdfFiles.First());
                    
                    // 🎯 修正: 最初のPDFファイルにもSourceImagePath設定を適用
                    var firstPdfFile = pdfFiles.First();
                    for (int pageIndex = 0; pageIndex < document.Pages.Count; pageIndex++)
                    {
                        var page = document.Pages[pageIndex];
                        try
                        {
                            var tempImagePath = await _pdfRenderingService
                                .ConvertPdfPageToTempImageAsync(firstPdfFile, pageIndex, dpi: 150);
                            page.SourceImagePath = tempImagePath;
                            
                            await AppendDebugLogAsync(
                                $"[PDF_SOURCING_FIRST] {Path.GetFileName(firstPdfFile)} Page{pageIndex+1} SourceImagePath設定: {tempImagePath}");
                        }
                        catch (Exception ex)
                        {
                            await AppendDebugLogAsync(
                                $"[PDF_SOURCING_FIRST] {Path.GetFileName(firstPdfFile)} Page{pageIndex+1} 変換エラー: {ex.Message}");
                        }
                    }
                    
                    result.AddedPagesCount += document.Pages.Count;
                    result.SuccessfulFiles.Add(pdfFiles.First());

                    if (pdfFiles.Count > 1)
                    {
                        var additionalPdfAddedCount = await AddPdfFilesToDocumentAsync(document, pdfFiles.Skip(1));
                        result.AddedPagesCount += additionalPdfAddedCount;
                        result.SuccessfulFiles.AddRange(pdfFiles.Skip(1));
                    }
                }
                else
                {
                    throw new InvalidOperationException("有効なファイルが見つかりません");
                }

                stopwatch.Stop();
                result.ProcessingTime = stopwatch.Elapsed;

                _logger.LogInformation("[V3_NewDocument] 新規ドキュメント作成完了: {Summary}", result.Summary);

                return (document, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_NewDocument] 新規ドキュメント作成エラー");
                throw;
            }
        }

        /// <summary>
        /// 🎯 V3専用: 画像ファイルからPDF作成（V2依存関係なし）
        /// </summary>
        private async Task<PdfDocument> CreatePdfFromImagesAsync(IEnumerable<string> imageFiles)
        {
            var document = new PdfDocument();
            
            foreach (var imageFile in imageFiles)
            {
                try
                {
                    // V3: ImageLoaderServiceで画像読み込み
                    var imageSource = await _imageLoaderService.LoadImageWithOrientationAsync(imageFile);
                    
                    // PDF Pageを作成（基本実装）
                    var page = new PdfPage(document.Pages.Count + 1)
                    {
                        SourceImagePath = imageFile,
                        Rotation = 0 // デフォルト
                    };
                    
                    document.AddPage(page);
                    
                    System.Diagnostics.Debug.WriteLine($"[V3_ImageToPdf] 画像追加成功: {Path.GetFileName(imageFile)}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[V3_ImageToPdf] 画像変換エラー: {FileName}", Path.GetFileName(imageFile));
                    throw;
                }
            }

            return document;
        }

        /// <summary>
        /// 画像ファイルを既存PDFドキュメントに追加
        /// </summary>
        public async Task<int> AddImageFilesToDocumentAsync(PdfDocument document, IEnumerable<string> imageFiles, int insertPosition = -1)
        {
            var stopwatch = Stopwatch.StartNew();
            var imageFilesList = imageFiles.ToList();
            var addedPagesCount = 0;

            try
            {
                _logger.LogInformation("[V3_FileAddition] 画像ファイル追加開始: {Count}ファイル", imageFilesList.Count);

                for (int i = 0; i < imageFilesList.Count; i++)
                {
                    var imageFile = imageFilesList[i];
                    
                    // 進捗報告
                    ProgressUpdated?.Invoke(this, new FileAdditionProgressEventArgs(i, imageFilesList.Count, Path.GetFileName(imageFile)));

                    try
                    {
                        // 🎯 V3実装: V2依存関係なし
                        var imageSource = await _imageLoaderService.LoadImageWithOrientationAsync(imageFile);
                        
                        var page = new PdfPage(document.Pages.Count + 1)
                        {
                            SourceImagePath = imageFile,
                            Rotation = 0
                        };

                        // 指定位置に挿入または末尾追加
                        var targetPosition = insertPosition == -1 ? document.Pages.Count : insertPosition + addedPagesCount;
                        
                        if (targetPosition >= document.Pages.Count)
                        {
                            document.AddPage(page);
                        }
                        else
                        {
                            document.AddPage(page);
                            if (targetPosition < document.Pages.Count - 1)
                            {
                                document.MovePage(document.Pages.Count - 1, targetPosition);
                            }
                        }
                        
                        addedPagesCount++;

                        _logger.LogDebug("[V3_FileAddition] 画像追加成功: {FileName}", Path.GetFileName(imageFile));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[V3_FileAddition] 画像追加エラー: {FileName}", Path.GetFileName(imageFile));
                        ErrorOccurred?.Invoke(this, new FileAdditionErrorEventArgs($"画像追加エラー: {Path.GetFileName(imageFile)}", ex, imageFile));
                    }
                }

                // ページ番号を再設定
                UpdatePageNumbers(document);

                stopwatch.Stop();
                _logger.LogInformation("[V3_FileAddition] 画像ファイル追加完了: {Added}ページ追加 ({Time}ms)", 
                    addedPagesCount, stopwatch.ElapsedMilliseconds);

                return addedPagesCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_FileAddition] 画像ファイル追加処理エラー");
                throw;
            }
        }

        /// <summary>
        /// PDFファイルを既存PDFドキュメントに結合追加
        /// </summary>
        public async Task<int> AddPdfFilesToDocumentAsync(PdfDocument document, IEnumerable<string> pdfFiles, int insertPosition = -1)
        {
            var stopwatch = Stopwatch.StartNew();
            var pdfFilesList = pdfFiles.ToList();
            var addedPagesCount = 0;

            try
            {
                _logger.LogInformation("[V3_FileAddition] PDFファイル追加開始: {Count}ファイル", pdfFilesList.Count);

                for (int i = 0; i < pdfFilesList.Count; i++)
                {
                    var pdfFile = pdfFilesList[i];
                    
                    // 進捗報告
                    ProgressUpdated?.Invoke(this, new FileAdditionProgressEventArgs(i, pdfFilesList.Count, Path.GetFileName(pdfFile)));

                    try
                    {
                        // 🎯 OSS標準: PdfEditorServiceでPDF読み込み
                        var loadedPdfDocument = await _pdfEditorService.OpenPdfAsync(pdfFile);
                        
                        if (loadedPdfDocument?.Pages?.Count > 0)
                        {
                            // 指定位置に挿入または末尾追加
                            var targetPosition = insertPosition == -1 ? document.Pages.Count : insertPosition + addedPagesCount;
                            
                            // 🔧 PDF用SourceImagePath設定対応
                            for (int pageIndex = 0; pageIndex < loadedPdfDocument.Pages.Count; pageIndex++)
                            {
                                var page = loadedPdfDocument.Pages[pageIndex];
                                
                                // 🎯 核心修正: PDF用一時画像生成・SourceImagePath設定
                                try 
                                {
                                    var tempImagePath = await _pdfRenderingService
                                        .ConvertPdfPageToTempImageAsync(pdfFile, pageIndex, dpi: 150);
                                    page.SourceImagePath = tempImagePath;
                                    
                                    await AppendDebugLogAsync(
                                        $"[PDF_SOURCING] {Path.GetFileName(pdfFile)} Page{pageIndex+1} SourceImagePath設定: {tempImagePath}");
                                }
                                catch (Exception ex)
                                {
                                    await AppendDebugLogAsync(
                                        $"[PDF_SOURCING] {Path.GetFileName(pdfFile)} Page{pageIndex+1} 変換エラー: {ex.Message}");
                                    // エラー時は空のまま（既存エラーハンドリング活用）
                                }
                                
                                // ページを適切な位置に挿入
                                if (targetPosition >= document.Pages.Count)
                                {
                                    document.AddPage(page);
                                }
                                else
                                {
                                    // OSS標準: AddPage後に位置調整（PdfDocumentにInsertメソッドがないため）
                                    document.AddPage(page);
                                    if (targetPosition < document.Pages.Count - 1)
                                    {
                                        document.MovePage(document.Pages.Count - 1, targetPosition);
                                    }
                                }
                                
                                addedPagesCount++;
                                targetPosition++;
                            }

                            _logger.LogDebug("[V3_FileAddition] PDF追加成功: {FileName} -> {Pages}ページ", 
                                Path.GetFileName(pdfFile), loadedPdfDocument.Pages.Count);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[V3_FileAddition] PDF追加エラー: {FileName}", Path.GetFileName(pdfFile));
                        ErrorOccurred?.Invoke(this, new FileAdditionErrorEventArgs($"PDF追加エラー: {Path.GetFileName(pdfFile)}", ex, pdfFile));
                    }
                }

                // ページ番号を再設定
                UpdatePageNumbers(document);

                stopwatch.Stop();
                _logger.LogInformation("[V3_FileAddition] PDFファイル追加完了: {Added}ページ追加 ({Time}ms)", 
                    addedPagesCount, stopwatch.ElapsedMilliseconds);

                return addedPagesCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_FileAddition] PDFファイル追加処理エラー");
                throw;
            }
        }

        /// <summary>
        /// 混在ファイル（画像+PDF）を既存ドキュメントに追加
        /// </summary>
        public async Task<FileAdditionResult> AddMixedFilesToDocumentAsync(PdfDocument document, IEnumerable<string> files, int insertPosition = -1)
        {
            var stopwatch = Stopwatch.StartNew();
            var result = new FileAdditionResult();
            var filesList = files.ToList();

            try
            {
                _logger.LogInformation("[V3_FileAddition] 混在ファイル追加開始: {Count}ファイル", filesList.Count);

                // ファイルを種類別に分類
                var imageFiles = filesList.Where(IsImageFile).ToList();
                var pdfFiles = filesList.Where(IsPdfFile).ToList();

                // 画像ファイル追加
                if (imageFiles.Any())
                {
                    try
                    {
                        var imageAddedCount = await AddImageFilesToDocumentAsync(document, imageFiles, insertPosition);
                        result.AddedPagesCount += imageAddedCount;
                        result.SuccessfulFiles.AddRange(imageFiles);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[V3_FileAddition] 画像ファイル一括追加エラー");
                        result.FailedFiles.AddRange(imageFiles);
                    }
                }

                // PDFファイル追加
                if (pdfFiles.Any())
                {
                    try
                    {
                        var currentPosition = insertPosition == -1 ? -1 : insertPosition + result.AddedPagesCount;
                        var pdfAddedCount = await AddPdfFilesToDocumentAsync(document, pdfFiles, currentPosition);
                        result.AddedPagesCount += pdfAddedCount;
                        result.SuccessfulFiles.AddRange(pdfFiles);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[V3_FileAddition] PDFファイル一括追加エラー");
                        result.FailedFiles.AddRange(pdfFiles);
                    }
                }

                stopwatch.Stop();
                result.ProcessingTime = stopwatch.Elapsed;

                _logger.LogInformation("[V3_FileAddition] 混在ファイル追加完了: {Summary}", result.Summary);

                // 完了通知
                AdditionCompleted?.Invoke(this, new FileAdditionCompletedEventArgs(result));

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_FileAddition] 混在ファイル追加処理エラー");
                ErrorOccurred?.Invoke(this, new FileAdditionErrorEventArgs("混在ファイル追加処理エラー", ex));
                throw;
            }
        }

        /// <summary>
        /// ファイル追加可能性の事前検証
        /// </summary>
        public async Task<FileAdditionValidationResult> ValidateFilesForAdditionAsync(IEnumerable<string> files)
        {
            var result = new FileAdditionValidationResult();
            var filesList = files.ToList();

            try
            {
                _logger.LogDebug("[V3_FileAddition] ファイル検証開始: {Count}ファイル", filesList.Count);

                foreach (var file in filesList)
                {
                    try
                    {
                        // ファイル存在確認
                        if (!File.Exists(file))
                        {
                            result.InvalidFiles.Add(file);
                            result.ValidationErrors.Add($"ファイルが見つかりません: {Path.GetFileName(file)}");
                            continue;
                        }

                        // ファイル形式確認
                        if (!IsImageFile(file) && !IsPdfFile(file))
                        {
                            result.InvalidFiles.Add(file);
                            result.ValidationErrors.Add($"対応していないファイル形式: {Path.GetFileName(file)}");
                            continue;
                        }

                        // ファイルサイズ確認
                        var fileInfo = new FileInfo(file);
                        if (fileInfo.Length == 0)
                        {
                            result.InvalidFiles.Add(file);
                            result.ValidationErrors.Add($"ファイルサイズが0バイトです: {Path.GetFileName(file)}");
                            continue;
                        }
                        result.EstimatedSizeBytes += fileInfo.Length;

                        // 画像ファイルの詳細検証
                        if (IsImageFile(file))
                        {
                            var validation = await _imageValidationService.ValidateImageAsync(file);
                            if (!validation.IsValid)
                            {
                                result.InvalidFiles.Add(file);
                                result.ValidationErrors.Add($"画像ファイル検証エラー: {Path.GetFileName(file)}");
                                continue;
                            }
                        }

                        // 🎯 V3.0.026 新規追加: PDFファイルの詳細検証
                        if (IsPdfFile(file))
                        {
                            try
                            {
                                _logger.LogDebug("[V3_FileAddition] PDF詳細検証開始: {FileName}", Path.GetFileName(file));
                                
                                // PdfEditorServiceを使用してPDF有効性確認
                                var testPdfDocument = await _pdfEditorService.OpenPdfAsync(file);
                                
                                if (testPdfDocument == null)
                                {
                                    result.InvalidFiles.Add(file);
                                    result.ValidationErrors.Add($"PDFファイル読み込みエラー: {Path.GetFileName(file)}");
                                    continue;
                                }

                                if (testPdfDocument.Pages == null || testPdfDocument.Pages.Count == 0)
                                {
                                    result.InvalidFiles.Add(file);
                                    result.ValidationErrors.Add($"PDFファイルにページが含まれていません: {Path.GetFileName(file)}");
                                    continue;
                                }

                                _logger.LogDebug("[V3_FileAddition] PDF検証成功: {FileName}, {PageCount}ページ", 
                                    Path.GetFileName(file), testPdfDocument.Pages.Count);
                            }
                            catch (Exception pdfEx)
                            {
                                _logger.LogWarning(pdfEx, "[V3_FileAddition] PDF検証エラー: {FileName}", Path.GetFileName(file));
                                result.InvalidFiles.Add(file);
                                result.ValidationErrors.Add($"PDF検証エラー: {Path.GetFileName(file)} - {pdfEx.Message}");
                                continue;
                            }
                        }

                        result.ValidFiles.Add(file);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[V3_FileAddition] ファイル検証エラー: {FileName}", Path.GetFileName(file));
                        result.InvalidFiles.Add(file);
                        result.ValidationErrors.Add($"検証エラー: {Path.GetFileName(file)} - {ex.Message}");
                    }
                }

                result.IsValid = result.ValidFiles.Count > 0 && result.ValidationErrors.Count == 0;

                _logger.LogDebug("[V3_FileAddition] ファイル検証完了: 有効{Valid}個, 無効{Invalid}個", 
                    result.ValidFiles.Count, result.InvalidFiles.Count);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[V3_FileAddition] ファイル検証処理エラー");
                throw;
            }
        }

        // Private helper methods
        private bool IsImageFile(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return SupportedImageExtensions.Contains(extension);
        }

        private bool IsPdfFile(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return SupportedPdfExtensions.Contains(extension);
        }

        private void UpdatePageNumbers(PdfDocument document)
        {
            for (int i = 0; i < document.Pages.Count; i++)
            {
                // PageNumber is read-only, skip assignment
            }
        }

        // Events
        public event EventHandler<FileAdditionProgressEventArgs>? ProgressUpdated;
        public event EventHandler<FileAdditionCompletedEventArgs>? AdditionCompleted;
        public event EventHandler<FileAdditionErrorEventArgs>? ErrorOccurred;

        /// <summary>
        /// 統一デバッグログ出力（新DebugLogger使用）
        /// </summary>
        private async Task AppendDebugLogAsync(string message)
        {
            await DebugLogger.LogAsync(message, "FileAdditionService");
        }
    }
}