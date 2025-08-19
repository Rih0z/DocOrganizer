using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocOrganizer.Application.Interfaces;
using DocOrganizer.Application.Interfaces.V3;
using DocOrganizer.Core.Models;

namespace DocOrganizer.UI.ViewModels.V3
{
    /// <summary>
    /// 🎯 V3アーキテクチャ: ドラッグ&ドロップ専用ViewModel
    /// 責務: ファイル処理、ページ並び替えのみ
    /// 目標: 150行以下、4メソッド以下
    /// </summary>
    public partial class DragDropHandlerViewModel : ObservableObject, IAdvancedDropHandler, IAdvancedDragHandler
    {
        // 🎯 V3専用: V2のIImageProcessingService依存関係削除済み
        private readonly IImageLoaderService _imageLoaderService;
        private readonly IDialogService _dialogService;
        private readonly IFileAdditionService _fileAdditionService;

        [ObservableProperty]
        private bool isProcessing;

        [ObservableProperty]
        private string statusMessage = "準備完了";

        [ObservableProperty]
        private string dragOverlayVisibility = "Collapsed";

        [ObservableProperty]
        private double progressPercentage;

        [ObservableProperty]
        private string progressDetail = "";

        // 現在処理中のドキュメント（ファイル追加用）
        private PdfDocument? _currentDocument;

        public DragDropHandlerViewModel(
            IImageLoaderService imageLoaderService,
            IDialogService dialogService,
            IFileAdditionService fileAdditionService)
        {
            // 🎯 V3専用: V2のIImageProcessingService依存関係削除
            _imageLoaderService = imageLoaderService;
            _dialogService = dialogService;
            _fileAdditionService = fileAdditionService;

            // OSS標準: イベント駆動アーキテクチャ
            _fileAdditionService.ProgressUpdated += OnFileAdditionProgress;
            _fileAdditionService.AdditionCompleted += OnFileAdditionCompletedFromService;
            _fileAdditionService.ErrorOccurred += OnFileAdditionError;
        }

        #region OSS標準: IAdvancedDropHandler実装

        /// <summary>
        /// 🎯 OSS標準: ドロップ可能性判定
        /// </summary>
        public async Task<bool> CanDropAsync(IAdvancedDropInfo dropInfo)
        {
            try
            {
                if (IsProcessing) return false;

                // ファイルドロップの場合
                if (dropInfo.FilePaths != null && dropInfo.FilePaths.Length > 0)
                {
                    var validationResult = await _fileAdditionService.ValidateFilesForAdditionAsync(dropInfo.FilePaths);
                    return validationResult.IsValid || validationResult.ValidFiles.Any();
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"🚨 V3 CanDrop Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 🎯 OSS標準: ドロップ処理実行
        /// </summary>
        public async Task DropAsync(IAdvancedDropInfo dropInfo)
        {
            try
            {
                if (IsProcessing) return;

                if (dropInfo.FilePaths != null && dropInfo.FilePaths.Length > 0)
                {
                    await HandleFilesDropAsync(dropInfo.FilePaths);
                    dropInfo.Effects = DragDropEffects.Copy;
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"ドロップ処理エラー: {ex.Message}");
                dropInfo.Effects = DragDropEffects.None;
            }
        }

        /// <summary>
        /// 🎯 OSS標準: ドラッグオーバー処理
        /// </summary>
        public async Task DragOverAsync(IAdvancedDropInfo dropInfo)
        {
            try
            {
                if (await CanDropAsync(dropInfo))
                {
                    ShowDragOverlay();
                    StatusMessage = $"{dropInfo.FilePaths?.Length ?? 0} 個のファイル - ドロップして追加";
                }
                else
                {
                    HideDragOverlay();
                    StatusMessage = "サポートされていないファイル形式";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"🚨 V3 DragOver Error: {ex.Message}");
                HideDragOverlay();
            }
        }

        #endregion

        #region OSS標準: IAdvancedDragHandler実装

        /// <summary>
        /// 🎯 OSS標準: ドラッグ開始処理
        /// </summary>
        public async Task<object> StartDragAsync(IAdvancedDragInfo dragInfo)
        {
            try
            {
                // ページViewModelからのドラッグの場合
                if (dragInfo.SourceItem is V3PageViewModel pageViewModel)
                {
                    return new DataObject(DataFormats.Serializable, pageViewModel);
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"🚨 V3 StartDrag Error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 🎯 OSS標準: ドラッグ完了処理
        /// </summary>
        public async Task DragCompletedAsync(IAdvancedDragCompletedInfo dragCompletedInfo)
        {
            try
            {
                if (dragCompletedInfo.IsCancelled)
                {
                    StatusMessage = "ドラッグがキャンセルされました";
                }
                else
                {
                    StatusMessage = "ドラッグ完了";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"🚨 V3 DragCompleted Error: {ex.Message}");
            }
        }

        #endregion

        /// <summary>
        /// 🎯 V3 OSS標準: ファイルドロップ処理（既存ドキュメントへの追加対応）
        /// </summary>
        public async Task HandleFilesDropAsync(IEnumerable<string> filePaths)
        {
            if (IsProcessing) return;

            try
            {
                IsProcessing = true;
                DragOverlayVisibility = "Collapsed";
                ProgressPercentage = 0;

                var filesList = filePaths.ToList();
                StatusMessage = $"{filesList.Count} 個のファイルを検証中...";

                // 🎯 OSS標準: 事前検証
                var validationResult = await _fileAdditionService.ValidateFilesForAdditionAsync(filesList);
                
                if (!validationResult.IsValid)
                {
                    _dialogService.ShowWarning($"無効なファイルが含まれています:\n{string.Join("\n", validationResult.ValidationErrors)}");
                    
                    if (!validationResult.ValidFiles.Any())
                    {
                        StatusMessage = "追加可能なファイルがありません";
                        return;
                    }
                }

                var validFiles = validationResult.ValidFiles;
                StatusMessage = $"{validFiles.Count} 個のファイルを処理中...";

                // 🎯 OSS標準: 既存ドキュメントへの追加 vs 新規ドキュメント作成
                if (_currentDocument != null)
                {
                    // 既存ドキュメントに追加
                    await AddFilesToExistingDocumentAsync(validFiles);
                }
                else
                {
                    // 新規ドキュメント作成
                    await CreateNewDocumentFromFilesAsync(validFiles);
                }

                StatusMessage = $"{validFiles.Count} 個のファイル処理完了";

                // イベント通知
                FilesProcessed?.Invoke(this, new FilesProcessedEventArgs(
                    validFiles.Where(IsImageFile).ToList(),
                    validFiles.Where(IsPdfFile).ToList()));
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"ファイル処理エラー: {ex.Message}");
                StatusMessage = "ファイル処理エラー";
            }
            finally
            {
                IsProcessing = false;
                ProgressPercentage = 0;
                ProgressDetail = "";
            }
        }

        /// <summary>
        /// ページ並び替え処理
        /// </summary>
        public async Task HandlePageReorderAsync(List<V3PageViewModel> pagesToMove, V3PageViewModel targetPage)
        {
            if (IsProcessing) return;

            try
            {
                IsProcessing = true;
                StatusMessage = $"{pagesToMove.Count} ページを並び替え中...";

                // PageOperationViewModelに委譲
                PageReorderRequested?.Invoke(this, new PageReorderEventArgs(pagesToMove, targetPage));

                StatusMessage = $"{pagesToMove.Count} ページを並び替え完了";
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"並び替えエラー: {ex.Message}");
                StatusMessage = "並び替えエラー";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        /// <summary>
        /// ドラッグオーバー表示制御
        /// </summary>
        public void ShowDragOverlay()
        {
            DragOverlayVisibility = "Visible";
        }

        public void HideDragOverlay()
        {
            DragOverlayVisibility = "Collapsed";
        }

        /// <summary>
        /// 現在のドキュメントを設定（ファイル追加機能用）
        /// </summary>
        public void SetCurrentDocument(PdfDocument? document)
        {
            _currentDocument = document;
        }

        // Private methods - OSS標準実装

        /// <summary>
        /// 🎯 OSS標準: 既存ドキュメントへのファイル追加
        /// </summary>
        private async Task AddFilesToExistingDocumentAsync(List<string> files)
        {
            if (_currentDocument == null) return;

            try
            {
                StatusMessage = "既存ドキュメントにファイルを追加中...";
                
                // FileAdditionServiceで追加処理
                var result = await _fileAdditionService.AddMixedFilesToDocumentAsync(_currentDocument, files);
                
                StatusMessage = $"ファイル追加完了: {result.Summary}";

                // 追加完了イベント
                FilesAddedToDocument?.Invoke(this, new FilesAddedEventArgs(_currentDocument, result));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"既存ドキュメントへの追加失敗: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 🎯 OSS標準: 新規ドキュメント作成
        /// </summary>
        private async Task CreateNewDocumentFromFilesAsync(List<string> files)
{
    try
    {
        StatusMessage = "新規ドキュメントを作成中...";
        
        // 🚨 緊急デバッグ: ファイルに出力
        await AppendDebugLogAsync($"[CreateNewDocument開始] files.Count={files.Count}");
        
        // 🎯 V3 OSS標準: FileAdditionService.CreateNewDocumentFromFilesAsync を使用
        await AppendDebugLogAsync("[CreateNewDocument] FileAdditionService.CreateNewDocumentFromFilesAsync実行開始");
        var (pdfDocument, result) = await _fileAdditionService.CreateNewDocumentFromFilesAsync(files);
        
        await AppendDebugLogAsync($"[CreateNewDocument] FileAdditionService完了: Document.Pages.Count={pdfDocument.Pages.Count}");
        
        // 🎯 V3イベント駆動: NewDocumentCreatedイベント発火
        await AppendDebugLogAsync("[CreateNewDocument] NewDocumentCreatedイベント発火開始");
        NewDocumentCreated?.Invoke(this, new NewDocumentCreatedEventArgs(pdfDocument, files));
        await AppendDebugLogAsync("[CreateNewDocument] NewDocumentCreatedイベント発火完了");

        StatusMessage = $"新規ドキュメント作成完了: {result.Summary}";
        await AppendDebugLogAsync($"[CreateNewDocument完了] StatusMessage: {StatusMessage}");
    }
    catch (Exception ex)
    {
        await AppendDebugLogAsync($"[CreateNewDocument例外] エラー: {ex.Message}");
        await AppendDebugLogAsync($"[CreateNewDocument例外] スタックトレース: {ex.StackTrace}");
        throw new InvalidOperationException($"新規ドキュメント作成失敗: {ex.Message}", ex);
    }
}

        // FileAdditionService イベントハンドラー
        private void OnFileAdditionProgress(object? sender, FileAdditionProgressEventArgs e)
        {
            ProgressPercentage = e.ProgressPercentage;
            ProgressDetail = $"処理中: {Path.GetFileName(e.CurrentFile)} ({e.ProcessedCount}/{e.TotalCount})";
        }

        private void OnFileAdditionCompletedFromService(object? sender, DocOrganizer.Application.Interfaces.V3.FileAdditionCompletedEventArgs e)
        {
            StatusMessage = $"ファイル追加完了: {e.Result.Summary}";
            
            // 🎯 V3 OSS標準: MainCompositeViewModelに通知
            // Note: FileAdditionResult doesn't contain UpdatedDocument, need to get it from current document
            var mainEventArgs = new FileAdditionCompletedEventArgs(
                _currentDocument!, 
                e.Result.AddedPagesCount, 
                e.Result.SuccessfulFiles);
            FileAdditionCompleted?.Invoke(this, mainEventArgs);
        }

        private void OnFileAdditionError(object? sender, FileAdditionErrorEventArgs e)
        {
            _dialogService.ShowError($"ファイル追加エラー: {e.ErrorMessage}");
            
            // 🎯 V3 OSS標準: MainCompositeViewModelに通知
            var mainEventArgs = new FileAdditionFailedEventArgs(
                e.ErrorMessage, 
                e.Exception, 
                new List<string> { e.FailedFile ?? "不明なファイル" });
            FileAdditionFailed?.Invoke(this, mainEventArgs);
        }

        private bool IsImageFile(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension == ".jpg" || extension == ".jpeg" || extension == ".png" ||
                   extension == ".heic" || extension == ".heif" || extension == ".bmp" ||
                   extension == ".tiff" || extension == ".gif" || extension == ".webp";
        }

        private bool IsPdfFile(string filePath)
        {
            return Path.GetExtension(filePath).Equals(".pdf", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 🚨 緊急デバッグ: ファイルに詳細ログを出力（第16条準拠）
        /// </summary>
        private async Task AppendDebugLogAsync(string message)
        {
            try
            {
                var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
                var logPath = @"C:\Users\217216X721451\github\DocOrganizer\release\DEBUG_LOG.txt";
                await System.IO.File.AppendAllTextAsync(logPath, logMessage + Environment.NewLine);
                System.Diagnostics.Debug.WriteLine($"[DRAGDROP_DEBUG] {message}");
            }
            catch
            {
                // ログ出力エラーは無視
            }
        }

        // Events for coordination with other ViewModels
        public event EventHandler<FilesProcessedEventArgs>? FilesProcessed;
        public event EventHandler<FilesAddedEventArgs>? FilesAddedToDocument;
        public event EventHandler<NewDocumentCreatedEventArgs>? NewDocumentCreated;
        public event EventHandler<PageReorderEventArgs>? PageReorderRequested;
        
        // 🎯 V3 OSS標準: ファイル追加イベント
        public event EventHandler<FileAdditionCompletedEventArgs>? FileAdditionCompleted;
        public event EventHandler<FileAdditionFailedEventArgs>? FileAdditionFailed;
    }

    /// <summary>
    /// ファイル追加完了イベント引数
    /// </summary>
    public class FilesAddedEventArgs : EventArgs
    {
        public PdfDocument Document { get; }
        public FileAdditionResult Result { get; }

        public FilesAddedEventArgs(PdfDocument document, FileAdditionResult result)
        {
            Document = document;
            Result = result;
        }
    }

    /// <summary>
    /// 新規ドキュメント作成完了イベント引数
    /// </summary>
    public class NewDocumentCreatedEventArgs : EventArgs
    {
        public PdfDocument Document { get; }
        public List<string> SourceFiles { get; }

        public NewDocumentCreatedEventArgs(PdfDocument document, List<string> sourceFiles)
        {
            Document = document;
            SourceFiles = sourceFiles;
        }
    }

    // Event argument classes
    public class FilesProcessedEventArgs : EventArgs
    {
        public List<string> ImageFiles { get; }
        public List<string> PdfFiles { get; }

        public FilesProcessedEventArgs(List<string> imageFiles, List<string> pdfFiles)
        {
            ImageFiles = imageFiles;
            PdfFiles = pdfFiles;
        }
    }

    public class ImageFilesProcessedEventArgs : EventArgs
    {
        public List<string> ImageFiles { get; }
        public PdfDocument PdfDocument { get; }

        public ImageFilesProcessedEventArgs(List<string> imageFiles, PdfDocument pdfDocument)
        {
            ImageFiles = imageFiles;
            PdfDocument = pdfDocument;
        }
    }

    public class PdfFileProcessedEventArgs : EventArgs
    {
        public string FilePath { get; }

        public PdfFileProcessedEventArgs(string filePath)
        {
            FilePath = filePath;
        }
    }

    public class PageReorderEventArgs : EventArgs
    {
        public List<V3PageViewModel> PagesToMove { get; }
        public V3PageViewModel TargetPage { get; }

        public PageReorderEventArgs(List<V3PageViewModel> pagesToMove, V3PageViewModel targetPage)
        {
            PagesToMove = pagesToMove;
            TargetPage = targetPage;
        }
    }
}