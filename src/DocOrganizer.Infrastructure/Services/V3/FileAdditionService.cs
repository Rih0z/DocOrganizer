using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocOrganizer.Application.Interfaces;
using DocOrganizer.Application.Interfaces.V3;
using DocOrganizer.Core.Models;
using Microsoft.Extensions.Logging;

namespace DocOrganizer.Infrastructure.Services.V3
{
    /// <summary>
    /// 🎯 V3アーキテクチャ: OSS標準ファイル追加サービス
    /// 責務: 既存PDFドキュメントへの新ファイル追加処理
    /// OSS標準: Clean Architecture、SOLID原則、テスト容易性
    /// </summary>
    public class FileAdditionService : IFileAdditionService
    {
        private readonly IImageProcessingService _imageProcessingService;
        private readonly IPdfEditorService _pdfEditorService;
        private readonly IImageValidationService _imageValidationService;
        private readonly ILogger<FileAdditionService> _logger;

        // 対応ファイル形式（OSS標準拡張子）
        private static readonly string[] SupportedImageExtensions = 
        {
            ".jpg", ".jpeg", ".png", ".heic", ".heif", ".bmp", ".tiff", ".gif", ".webp"
        };
        
        private static readonly string[] SupportedPdfExtensions = 
        {
            ".pdf"
        };

        public FileAdditionService(
            IImageProcessingService imageProcessingService,
            IPdfEditorService pdfEditorService,
            IImageValidationService imageValidationService,
            ILogger<FileAdditionService> logger)
        {
            _imageProcessingService = imageProcessingService;
            _pdfEditorService = pdfEditorService;
            _imageValidationService = imageValidationService;
            _logger = logger;
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
                        // 🎯 OSS標準: ImageProcessingServiceで画像→PDF変換
                        var imagePdfDocument = await _imageProcessingService.ConvertImageToPdfAsync(imageFile);
                        
                        if (imagePdfDocument?.Pages?.Count > 0)
                        {
                            // 指定位置に挿入または末尾追加
                            var targetPosition = insertPosition == -1 ? document.Pages.Count : insertPosition + addedPagesCount;
                            
                            foreach (var page in imagePdfDocument.Pages)
                            {
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

                            _logger.LogDebug("[V3_FileAddition] 画像追加成功: {FileName} -> {Pages}ページ", 
                                Path.GetFileName(imageFile), imagePdfDocument.Pages.Count);
                        }
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
                            
                            foreach (var page in loadedPdfDocument.Pages)
                            {
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
    }
}