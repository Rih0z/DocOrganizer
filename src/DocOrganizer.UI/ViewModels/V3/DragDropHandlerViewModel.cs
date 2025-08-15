using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
    public partial class DragDropHandlerViewModel : ObservableObject
    {
        private readonly IImageProcessingService _imageProcessingService;
        private readonly IImageLoaderService _imageLoaderService;
        private readonly IDialogService _dialogService;

        [ObservableProperty]
        private bool isProcessing;

        [ObservableProperty]
        private string statusMessage = "準備完了";

        [ObservableProperty]
        private string dragOverlayVisibility = "Collapsed";

        public DragDropHandlerViewModel(
            IImageProcessingService imageProcessingService,
            IImageLoaderService imageLoaderService,
            IDialogService dialogService)
        {
            _imageProcessingService = imageProcessingService;
            _imageLoaderService = imageLoaderService;
            _dialogService = dialogService;
        }

        /// <summary>
        /// ファイルドロップ処理
        /// </summary>
        public async Task HandleFilesDropAsync(IEnumerable<string> filePaths)
        {
            if (IsProcessing) return;

            try
            {
                IsProcessing = true;
                DragOverlayVisibility = "Collapsed";

                var filesList = filePaths.ToList();
                var imageFiles = filesList.Where(IsImageFile).ToList();
                var pdfFiles = filesList.Where(IsPdfFile).ToList();

                StatusMessage = $"{filesList.Count} 個のファイルを処理中...";

                // 🎯 V3新実装: 画像とPDFを統合処理
                if (imageFiles.Any())
                {
                    await ProcessImageFilesAsync(imageFiles);
                }

                if (pdfFiles.Any())
                {
                    await ProcessPdfFilesAsync(pdfFiles);
                }

                StatusMessage = $"{filesList.Count} 個のファイル処理完了";

                // イベント通知
                FilesProcessed?.Invoke(this, new FilesProcessedEventArgs(imageFiles, pdfFiles));
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"ファイル処理エラー: {ex.Message}");
                StatusMessage = "ファイル処理エラー";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        /// <summary>
        /// ページ並び替え処理
        /// </summary>
        public async Task HandlePageReorderAsync(List<PageViewModel> pagesToMove, PageViewModel targetPage)
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

        // Private helper methods
        private async Task ProcessImageFilesAsync(List<string> imageFiles)
        {
            try
            {
                // 🎯 V3新実装: OSS標準ImageLoaderService使用
                var imageInfoTasks = imageFiles.Select(async file =>
                {
                    var info = await _imageLoaderService.GetImageInfoAsync(file);
                    return new { FilePath = file, Info = info };
                });

                var imageInfos = await Task.WhenAll(imageInfoTasks);

                // ImageProcessingServiceで統合PDF作成
                var pdfDocument = await _imageProcessingService.ConvertImagesToPdfAsync(imageFiles);

                // イベント通知
                ImageFilesProcessed?.Invoke(this, new ImageFilesProcessedEventArgs(imageFiles, pdfDocument));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"画像ファイル処理失敗: {ex.Message}", ex);
            }
        }

        private async Task ProcessPdfFilesAsync(List<string> pdfFiles)
        {
            try
            {
                foreach (var pdfFile in pdfFiles)
                {
                    // DocumentManagementViewModelに委譲
                    PdfFileProcessed?.Invoke(this, new PdfFileProcessedEventArgs(pdfFile));
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"PDFファイル処理失敗: {ex.Message}", ex);
            }
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

        // Events for coordination with other ViewModels
        public event EventHandler<FilesProcessedEventArgs>? FilesProcessed;
        public event EventHandler<ImageFilesProcessedEventArgs>? ImageFilesProcessed;
        public event EventHandler<PdfFileProcessedEventArgs>? PdfFileProcessed;
        public event EventHandler<PageReorderEventArgs>? PageReorderRequested;
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
        public List<PageViewModel> PagesToMove { get; }
        public PageViewModel TargetPage { get; }

        public PageReorderEventArgs(List<PageViewModel> pagesToMove, PageViewModel targetPage)
        {
            PagesToMove = pagesToMove;
            TargetPage = targetPage;
        }
    }
}