using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using DocOrganizer.Application.Interfaces;
using DocOrganizer.Core.Models;

namespace DocOrganizer.UI.ViewModels.V3
{
    /// <summary>
    /// 🎯 V3アーキテクチャ: ファイル操作専用ViewModel
    /// 責務: Open, Save, SaveAs, New, Close のみ
    /// 目標: 300行以下、10メソッド以下
    /// </summary>
    public partial class DocumentManagementViewModel : ObservableObject
    {
        private readonly IPdfEditorService _pdfEditorService;
        private readonly IDialogService _dialogService;
        private readonly IImageProcessingService _imageProcessingService;

        [ObservableProperty]
        private string statusMessage = "準備完了";

        [ObservableProperty]
        private bool hasDocument;

        [ObservableProperty]
        private string fileInfo = "";

        private PdfDocument? _currentDocument;

        public DocumentManagementViewModel(
            IPdfEditorService pdfEditorService,
            IDialogService dialogService,
            IImageProcessingService imageProcessingService)
        {
            _pdfEditorService = pdfEditorService;
            _dialogService = dialogService;
            _imageProcessingService = imageProcessingService;
        }

        /// <summary>
        /// 新規ドキュメント作成
        /// </summary>
        [RelayCommand]
        private void New()
        {
            try
            {
                // 現在のドキュメントがある場合は確認
                if (_currentDocument != null && _currentDocument.IsModified)
                {
                    var result = _dialogService.ShowMessage(
                        "現在のドキュメントは変更されています。保存しますか？",
                        "確認",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        SaveAsync().Wait();
                    }
                    else if (result == DialogResult.Cancel)
                    {
                        return;
                    }
                }

                // 新規ドキュメントとして空のPDFを作成
                Close();
                StatusMessage = "新規ドキュメント";
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"新規作成エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// ファイルを開く
        /// </summary>
        [RelayCommand]
        private async Task OpenAsync()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "対応ファイル|*.pdf;*.jpg;*.jpeg;*.png;*.heic;*.heif;*.bmp;*.tiff;*.gif;*.webp|PDF ファイル (*.pdf)|*.pdf|画像ファイル|*.jpg;*.jpeg;*.png;*.heic;*.heif;*.bmp;*.tiff;*.gif;*.webp|すべてのファイル (*.*)|*.*",
                Title = "ファイルを開く",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (var fileName in openFileDialog.FileNames)
                {
                    if (IsPdfFile(fileName))
                    {
                        await OpenFileAsync(fileName);
                    }
                    else if (IsImageFile(fileName))
                    {
                        await OpenImageFileAsync(fileName);
                    }
                }
            }
        }

        /// <summary>
        /// 保存
        /// </summary>
        [RelayCommand(CanExecute = nameof(HasDocument))]
        private async Task SaveAsync()
        {
            if (_currentDocument == null) return;

            try
            {
                // 既存ファイルの場合は上書き保存
                if (!string.IsNullOrEmpty(_currentDocument.FilePath) &&
                    File.Exists(_currentDocument.FilePath))
                {
                    await SaveDocumentAsync(_currentDocument.FilePath);
                }
                else
                {
                    // 新規ファイルの場合は名前を付けて保存
                    await SaveAsAsync();
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"保存エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// 名前を付けて保存
        /// </summary>
        [RelayCommand(CanExecute = nameof(HasDocument))]
        private async Task SaveAsAsync()
        {
            if (_currentDocument == null) return;

            try
            {
                // outputフォルダのパスを生成
                var outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output");
                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                var saveDialog = new SaveFileDialog
                {
                    Filter = "PDF files (*.pdf)|*.pdf",
                    DefaultExt = "pdf",
                    InitialDirectory = outputDir,
                    FileName = $"document_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    await SaveDocumentAsync(saveDialog.FileName);
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"保存エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// ドキュメントを閉じる
        /// </summary>
        [RelayCommand]
        private void Close()
        {
            if (_currentDocument != null)
            {
                _pdfEditorService.CloseDocument();
                _currentDocument = null;
                HasDocument = false;
                FileInfo = "";
                StatusMessage = "準備完了";
            }
        }

        // Private helper methods
        private async Task OpenFileAsync(string filePath)
        {
            try
            {
                StatusMessage = $"読み込み中: {Path.GetFileName(filePath)}";
                var document = await _pdfEditorService.OpenPdfAsync(filePath);
                _currentDocument = document;
                HasDocument = true;
                FileInfo = Path.GetFileName(filePath);
                StatusMessage = $"読み込み完了: {Path.GetFileName(filePath)}";
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"ファイルを開けませんでした: {ex.Message}");
                StatusMessage = "エラーが発生しました";
            }
        }

        private async Task OpenImageFileAsync(string filePath)
        {
            try
            {
                StatusMessage = $"画像変換中: {Path.GetFileName(filePath)}";
                var pdfDocument = await _imageProcessingService.ConvertImageToPdfAsync(filePath);
                _currentDocument = pdfDocument;
                HasDocument = true;
                FileInfo = Path.GetFileName(filePath);
                StatusMessage = $"画像変換完了: {Path.GetFileName(filePath)}";
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"画像ファイルを開けませんでした: {ex.Message}");
                StatusMessage = "エラーが発生しました";
            }
        }

        private async Task SaveDocumentAsync(string filePath)
        {
            if (_currentDocument == null) return;

            try
            {
                StatusMessage = "PDF を保存中...";

                // outputフォルダの作成
                var outputDir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                // PDF保存
                var success = await _pdfEditorService.SavePdfAsync(_currentDocument, filePath);

                if (success)
                {
                    StatusMessage = $"保存完了: {Path.GetFileName(filePath)}";
                    _currentDocument.FilePath = filePath;
                    FileInfo = Path.GetFileName(filePath);
                }
                else
                {
                    _dialogService.ShowError("PDFの保存に失敗しました");
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"保存エラー: {ex.Message}");
            }
        }

        private bool IsPdfFile(string filePath)
        {
            return Path.GetExtension(filePath).Equals(".pdf", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsImageFile(string filePath)
        {
            var extension = Path.GetExtension(filePath);
            return extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".heic", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".heif", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".tiff", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".gif", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".webp", StringComparison.OrdinalIgnoreCase);
        }

        // Public properties for external access
        public PdfDocument? CurrentDocument => _currentDocument;

        // Events for notifying other ViewModels
        public event EventHandler<PdfDocument>? DocumentOpened;
        public event EventHandler? DocumentClosed;
        public event EventHandler<string>? DocumentSaved;

        protected virtual void OnDocumentOpened(PdfDocument document)
        {
            DocumentOpened?.Invoke(this, document);
        }

        protected virtual void OnDocumentClosed()
        {
            DocumentClosed?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnDocumentSaved(string filePath)
        {
            DocumentSaved?.Invoke(this, filePath);
        }
    }
}