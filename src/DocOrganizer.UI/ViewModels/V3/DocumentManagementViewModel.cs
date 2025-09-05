using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using DocOrganizer.Application.Interfaces;
using DocOrganizer.Application.Interfaces.V3;
using DocOrganizer.Core.Models;
using System.Linq;

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
        // 🎯 V3専用: V2のIImageProcessingService削除済み
        private readonly IFileAdditionService _fileAdditionService;
        private readonly IPdfExportService _pdfExportService;                    // ✅ 追加
        private readonly IDocumentToV3ConverterService _v3ConverterService;      // ✅ 追加

        [ObservableProperty]
        private string statusMessage = "準備完了";

        [ObservableProperty]
        private bool hasDocument;

        [ObservableProperty]
        private string fileInfo = "";

        private PdfDocument? _currentDocument;

        // 🆕 PDF編集機能追加
        [ObservableProperty]
        private bool isPdfDocument;

        [ObservableProperty]
        private bool canSplitPdf;

        [ObservableProperty]
        private bool canMergePdf;

        // 🆕 HEIC/GIF対応フラグ
        [ObservableProperty]
        private bool hasHeicFiles;

        [ObservableProperty]
        private bool hasGifFiles;

        public DocumentManagementViewModel(
        IPdfEditorService pdfEditorService,
        IDialogService dialogService,
        IFileAdditionService fileAdditionService,
        IPdfExportService pdfExportService,
        IDocumentToV3ConverterService v3ConverterService)
    {
        _pdfEditorService = pdfEditorService;
        _dialogService = dialogService;
        _fileAdditionService = fileAdditionService;
        _pdfExportService = pdfExportService;        // ✅ 追加
        _v3ConverterService = v3ConverterService;    // ✅ 追加
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
        [RelayCommand]
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
        [RelayCommand]
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
        /// 🆕 PDF分割機能
        /// </summary>
        [RelayCommand]
        private async Task SplitPdfAsync()
        {
            if (_currentDocument == null || !IsPdfDocument || _currentDocument.Pages.Count <= 1)
            {
                _dialogService.ShowInformation("PDF分割を実行するには2ページ以上のPDFが必要です");
                return;
            }

            try
            {
                // 分割位置の選択（簡易入力）
                var splitPosition = _dialogService.ShowInputDialog(
                    $"PDF分割位置を入力してください (1-{_currentDocument.Pages.Count - 1})",
                    "PDF分割",
                    "1");

                if (!int.TryParse(splitPosition, out var splitIndex) || 
                    splitIndex < 1 || splitIndex >= _currentDocument.Pages.Count)
                {
                    _dialogService.ShowError("有効な分割位置を入力してください");
                    return;
                }

                // 出力ディレクトリ準備
                var outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output");
                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                var firstHalfPath = Path.Combine(outputDir, $"document_part1_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
                var secondHalfPath = Path.Combine(outputDir, $"document_part2_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

                // ✅ 既存PdfDocumentメソッドを活用した安全な分割
                var firstHalf = new PdfDocument();
                var secondHalf = new PdfDocument();

                // 前半部分のページをコピー
                for (int i = 0; i < splitIndex; i++)
                {
                    var pageClone = ClonePdfPage(_currentDocument.Pages[i]);
                    firstHalf.AddPage(pageClone);
                }

                // 後半部分のページをコピー
                for (int i = splitIndex; i < _currentDocument.Pages.Count; i++)
                {
                    var pageClone = ClonePdfPage(_currentDocument.Pages[i]);
                    secondHalf.AddPage(pageClone);
                }

                // ✅ 既存保存メソッドを活用
                var firstSaved = await _pdfEditorService.SavePdfAsync(firstHalf, firstHalfPath);
                var secondSaved = await _pdfEditorService.SavePdfAsync(secondHalf, secondHalfPath);

                if (firstSaved && secondSaved)
                {
                    StatusMessage = $"PDF分割完了: {Path.GetFileName(firstHalfPath)}, {Path.GetFileName(secondHalfPath)}";
                    _dialogService.ShowInformation($"PDF分割が完了しました:\n前半: {firstHalfPath}\n後半: {secondHalfPath}");
                }
                else
                {
                    _dialogService.ShowError("PDF分割中にエラーが発生しました");
                }

                // リソース解放
                firstHalf.Dispose();
                secondHalf.Dispose();
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"PDF分割エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// 🆕 PDF結合機能
        /// </summary>
        [RelayCommand]
        private async Task MergePdfAsync()
        {
            if (_currentDocument == null)
            {
                _dialogService.ShowInformation("結合する基準となるPDFを開いてください");
                return;
            }

            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "PDF ファイル (*.pdf)|*.pdf",
                    Title = "結合するPDFファイルを選択",
                    Multiselect = true
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var mergedDocument = new PdfDocument();

                    // ✅ 現在の文書のページを最初に追加
                    foreach (var page in _currentDocument.Pages)
                    {
                        var pageClone = ClonePdfPage(page);
                        mergedDocument.AddPage(pageClone);
                    }

                    // ✅ 選択されたPDFファイルを順次結合
                    foreach (var filePath in openFileDialog.FileNames)
                    {
                        try
                        {
                            var otherDoc = await _pdfEditorService.OpenPdfAsync(filePath);
                            foreach (var page in otherDoc.Pages)
                            {
                                var pageClone = ClonePdfPage(page);
                                mergedDocument.AddPage(pageClone);
                            }
                        }
                        catch (Exception ex)
                        {
                            _dialogService.ShowError($"ファイル結合エラー ({Path.GetFileName(filePath)}): {ex.Message}");
                        }
                    }

                    // 結合結果を現在の文書として設定
                    _currentDocument?.Dispose();
                    _currentDocument = mergedDocument;
                    
                    // UI状態更新
                    UpdateDocumentState();
                    StatusMessage = $"PDF結合完了: {openFileDialog.FileNames.Length + 1}ファイル結合";
                    
                    // イベント通知
                    OnDocumentOpened(mergedDocument);
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"PDF結合エラー: {ex.Message}");
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
                
                // 🆕 PDF編集フラグリセット
                UpdateDocumentState();
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
                
                // 🆕 PDF編集状態更新
                UpdateDocumentState();
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
                
                // 🎯 V3実装: FileAdditionService.CreateNewDocumentFromFilesAsyncを使用
                var files = new[] { filePath };
                var (pdfDocument, result) = await _fileAdditionService.CreateNewDocumentFromFilesAsync(files);
                
                _currentDocument = pdfDocument;
                HasDocument = true;
                FileInfo = Path.GetFileName(filePath);
                StatusMessage = $"画像変換完了: {Path.GetFileName(filePath)}";
                
                // 🆕 ファイル形式判定とフラグ設定
                UpdateDocumentState();
                
                // 🎯 V3イベント: ドキュメント開始イベント発火
                OnDocumentOpened(pdfDocument);
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
            
            // 🚨 緊急デバッグログ追加
            await AppendDebugLogAsync($"[SaveDocument] PDF保存開始: {filePath}");
            await AppendDebugLogAsync($"[SaveDocument] _currentDocument != null: {_currentDocument != null}");
            await AppendDebugLogAsync($"[SaveDocument] _currentDocument.Pages.Count: {_currentDocument?.Pages?.Count}");
            await AppendDebugLogAsync($"[SaveDocument] _currentDocument.SourceImagePaths?.Count: {_currentDocument?.SourceImagePaths?.Count}");

            // output フォルダの作成
            var outputDir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // 🚨 V3判定の詳細ログ
            await AppendDebugLogAsync($"[SaveDocument] V3判定開始");
            var hasV3Content = await _v3ConverterService.HasV3EditableContentAsync(_currentDocument);
            await AppendDebugLogAsync($"[SaveDocument] HasV3EditableContent: {hasV3Content}");

            if (hasV3Content)
            {
                await AppendDebugLogAsync($"[SaveDocument] V3処理分岐開始");
                
                // ✅ V3統一処理（画像ベースのPDF）
                var pageData = await _v3ConverterService.GetCurrentEditStateAsync(_currentDocument);
                await AppendDebugLogAsync($"[SaveDocument] V3PageData取得完了: {pageData?.Count}ページ");
                
                var settings = new PdfQualitySettings(
                    DocOrganizer.Core.Models.QualityLevel.High,
                    1920,
                    1080, 
                    95,
                    "高品質"
                );

                await AppendDebugLogAsync($"[SaveDocument] V3 ExportCurrentStateAsync開始");
                var success = await _pdfExportService.ExportCurrentStateAsync(pageData, settings, filePath);
                await AppendDebugLogAsync($"[SaveDocument] V3 ExportCurrentStateAsync結果: {success}");
                
                if (success)
                {
                    StatusMessage = $"保存完了: {Path.GetFileName(filePath)}";
                    _currentDocument.FilePath = filePath;
                    FileInfo = Path.GetFileName(filePath);
                    await AppendDebugLogAsync($"[SaveDocument] V3処理成功");
                }
                else
                {
                    await AppendDebugLogAsync($"[SaveDocument] V3処理失敗");
                    _dialogService.ShowError("V3 PDF出力に失敗しました");
                }
            }
            else
            {
                await AppendDebugLogAsync($"[SaveDocument] 従来処理分岐開始");
                
                // ✅ 従来処理（通常のPDF文書）
                await AppendDebugLogAsync($"[SaveDocument] 従来 SavePdfAsync開始");
                var success = await _pdfEditorService.SavePdfAsync(_currentDocument, filePath);
                await AppendDebugLogAsync($"[SaveDocument] 従来 SavePdfAsync結果: {success}");
                
                if (success)
                {
                    StatusMessage = $"保存完了: {Path.GetFileName(filePath)}";
                    _currentDocument.FilePath = filePath;
                    FileInfo = Path.GetFileName(filePath);
                    await AppendDebugLogAsync($"[SaveDocument] 従来処理成功");
                }
                else
                {
                    await AppendDebugLogAsync($"[SaveDocument] 従来処理失敗");
                    _dialogService.ShowError("PDF保存に失敗しました");
                }
            }
        }
        catch (Exception ex)
        {
            await AppendDebugLogAsync($"[SaveDocument] 例外発生: {ex.Message}");
            await AppendDebugLogAsync($"[SaveDocument] StackTrace: {ex.StackTrace}");
            _dialogService.ShowError($"保存エラー: {ex.Message}");
        }
    }

        /// <summary>
        /// 🆕 PDF編集状態の更新
        /// </summary>
        private void UpdateDocumentState()
        {
            IsPdfDocument = _currentDocument?.FilePath?.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) == true;
            CanSplitPdf = IsPdfDocument && _currentDocument?.Pages?.Count > 1;
            CanMergePdf = IsPdfDocument;
            
            // ファイル形式判定
            if (_currentDocument?.SourceImagePaths?.Any() == true)
            {
                HasHeicFiles = _currentDocument.SourceImagePaths.Any(IsHeicFile);
                HasGifFiles = _currentDocument.SourceImagePaths.Any(IsGifFile);
            }
            else
            {
                HasHeicFiles = false;
                HasGifFiles = false;
            }
        }

        /// <summary>
        /// 🆕 PDFページの安全なクローン
        /// </summary>
        private PdfPage ClonePdfPage(PdfPage original)
        {
            // 新しいPdfPageインスタンスを作成
            var clone = new PdfPage(original.PageNumber);
            
            clone.Rotation = original.Rotation;
            return clone;
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

        /// <summary>
        /// 🆕 HEIC判定
        /// </summary>
        private bool IsHeicFile(string filePath)
        {
            var extension = Path.GetExtension(filePath);
            return extension.Equals(".heic", StringComparison.OrdinalIgnoreCase) ||
                   extension.Equals(".heif", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 🆕 GIF判定
        /// </summary>
        private bool IsGifFile(string filePath)
        {
            return Path.GetExtension(filePath).Equals(".gif", StringComparison.OrdinalIgnoreCase);
        }

        // Public properties for external access
        public PdfDocument? CurrentDocument => _currentDocument;

        // Events for notifying other ViewModels
        public event EventHandler<DocumentOpenedEventArgs>? DocumentOpened;
        public event EventHandler? DocumentClosed;
        public event EventHandler<DocumentSavedEventArgs>? DocumentSaved;

        protected virtual void OnDocumentOpened(PdfDocument document)
        {
            DocumentOpened?.Invoke(this, new DocumentOpenedEventArgs(document));
        }

        protected virtual void OnDocumentClosed()
        {
            DocumentClosed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 🚨 緊急デバッグログ出力メソッド
        /// </summary>
        private async Task AppendDebugLogAsync(string message)
        {
            try
            {
                var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
                await DocOrganizer.Core.Logging.DebugLogger.LogAsync(message, "DocumentManagement");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] {message}");
            }
            catch { /* ログ出力エラーは無視 */ }
        }

        protected virtual void OnDocumentSaved(string filePath)
        {
            DocumentSaved?.Invoke(this, new DocumentSavedEventArgs(filePath));
        }
    }
}