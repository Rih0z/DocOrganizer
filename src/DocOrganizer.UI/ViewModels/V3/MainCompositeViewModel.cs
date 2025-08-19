using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocOrganizer.Application.Interfaces;
using DocOrganizer.Application.Interfaces.V3;
using System.IO;
using System.Linq;
using System;
using System.Windows.Input;
using DocOrganizer.Core.Models;
using DocOrganizer.UI.ViewModels;

namespace DocOrganizer.UI.ViewModels.V3
{
    /// <summary>
    /// 🎯 V3アーキテクチャ: メイン統合ViewModel
    /// 責務: 全ての子ViewModelの統合・イベント調整のみ
    /// 目標: 200行以下、8メソッド以下
    /// </summary>
    public partial class MainCompositeViewModel : ObservableObject
    {
        // 子ViewModels - Single Responsibility Principleによる分離
        public DocumentManagementViewModel DocumentManagement { get; }
        public PageOperationViewModel PageOperation { get; }
        public PreviewManagementViewModel PreviewManagement { get; }
        public DragDropHandlerViewModel DragDropHandler { get; }
        public StatusManagementViewModel StatusManagement { get; }

        // コマンドプロキシ
        public IRelayCommand? ZoomInCommand => PreviewManagement?.ZoomInCommand;
        public IRelayCommand? ZoomOutCommand => PreviewManagement?.ZoomOutCommand;
        
        // 🔧 明示的コマンド実装 - RelayCommand自動生成に依存しない
        public ICommand ExportPdfCommand { get; private set; }
        
        // 🎯 V3専用: V2依存関係完全削除
        private readonly IPdfExportService _pdfExportService;
        private readonly IThumbnailGeneratorService _thumbnailService;
        private readonly ITextOrientationService _textOrientationService;

        // 🔧 V3リファクタリング: PageOperationのPagesを安定的に参照
        private ObservableCollection<V3PageViewModel>? _pagesCache;
    // 🎯 V3.0新機能: PDF出力関連
    [ObservableProperty]
    private PdfQualitySettings selectedQuality = PdfQualitySettings.GetDefault();

    public PdfQualitySettings[] QualityOptions => PdfQualitySettings.GetPresetSettings();

    [ObservableProperty]
    private bool isExporting;

    [ObservableProperty]
    private double exportProgress;

    [ObservableProperty]
    private string exportStatusMessage = "";
        public ObservableCollection<V3PageViewModel> Pages
        {
            get
            {
                if (_pagesCache == null && PageOperation != null)
                {
                    _pagesCache = PageOperation.Pages;
                }
                return _pagesCache ?? new ObservableCollection<V3PageViewModel>();
            }
        }

        [ObservableProperty]
        private V3PageViewModel? selectedPage;

        [ObservableProperty]
        private PdfDocument? currentDocument;

        public MainCompositeViewModel(
            DocumentManagementViewModel documentManagement,
            PageOperationViewModel pageOperation,
            PreviewManagementViewModel previewManagement,
            DragDropHandlerViewModel dragDropHandler,
            StatusManagementViewModel statusManagement,
            IThumbnailGeneratorService thumbnailService,
            ITextOrientationService textOrientationService,
            IPdfExportService pdfExportService)
        {
            DocumentManagement = documentManagement;
            PageOperation = pageOperation;
            PreviewManagement = previewManagement;
            DragDropHandler = dragDropHandler;
            StatusManagement = statusManagement;
            _thumbnailService = thumbnailService;
            _textOrientationService = textOrientationService;
            _pdfExportService = pdfExportService;

            // 🔧 明示的コマンド初期化 - RelayCommand自動生成の代替
            ExportPdfCommand = new AsyncRelayCommand(ExportPdfAsync, CanExportPdfMethod);

            InitializeEventHandlers();
        }

        /// <summary>
        /// 子ViewModel間のイベント調整設定
        /// </summary>
        private void InitializeEventHandlers()
        {
            // DocumentManagement → 他ViewModels
            DocumentManagement.DocumentOpened += OnDocumentOpened;
            DocumentManagement.DocumentSaved += OnDocumentSaved;
            DocumentManagement.DocumentClosed += OnDocumentClosed;

            // PageOperation → 他ViewModels
            PageOperation.PageRotated += OnPageRotated;
            PageOperation.PageDeleted += OnPageDeleted;
            PageOperation.PageMoved += OnPageMoved;
            // 🔧 V3リファクタリング: PagesChangedイベントハンドラー追加
            PageOperation.PagesChanged += OnPagesChanged;

            // DragDropHandler → 他ViewModels
            DragDropHandler.FilesProcessed += OnFilesProcessed;
            DragDropHandler.PageReorderRequested += OnPageReorderRequested;
            // 🔧 V3修正: FilesAddedToDocumentで既に処理されているため削除
            // DragDropHandler.FileAdditionCompleted += OnFileAdditionCompleted;
            // 🔧 V3リファクタリング: 削除されたイベントをコメントアウト
            // DragDropHandler.FileAdditionFailed += OnFileAdditionFailed;
            DragDropHandler.NewDocumentCreated += OnNewDocumentCreated;
            DragDropHandler.FilesAddedToDocument += OnFilesAddedToDocument;

            // StatusManagement → 他ViewModels
            StatusManagement.OperationStarted += OnOperationStarted;
            StatusManagement.OperationCompleted += OnOperationCompleted;

            // PreviewManagement → StatusManagement
            PreviewManagement.PreviewUpdated += OnPreviewUpdated;
        }

        /// <summary>
        /// 🔧 Phase 1改善: ページ読み込み処理の統一
        /// 重複していた3箇所の処理を1つのメソッドに統合
        /// 増分更新対応: incrementalUpdate=trueで新規ページのみ追加
        /// </summary>
        private async Task LoadPagesAsync(PdfDocument document, bool incrementalUpdate = false)
        {
            try
            {
                // CurrentDocument設定と各ViewModelへの通知
                CurrentDocument = document;
                PageOperation.SetCurrentDocument(CurrentDocument);
                PreviewManagement.SetCurrentDocument(CurrentDocument);
                DragDropHandler.SetCurrentDocument(CurrentDocument);
                
                // 既存のページ数を記録
                var existingPageCount = PageOperation.Pages.Count;
                
                if (!incrementalUpdate)
                {
                    // 🔧 完全リロード: 初回読み込みや新規ドキュメント作成時
                    await AppendDebugLogAsync($"[LoadPagesAsync] 完全リロードモード - 全ページクリア");
                    _pagesCache = null;
                    PageOperation.Pages.Clear();
                    existingPageCount = 0;
                }
                else
                {
                    // 🔧 増分更新: 既存ドキュメントへのページ追加時
                    await AppendDebugLogAsync($"[LoadPagesAsync] 増分更新モード - 既存{existingPageCount}ページ保持");
                }
                
                // 新規ページのみを追加（増分更新時は既存ページ以降のみ）
                for (int i = existingPageCount; i < document.Pages.Count; i++)
                {
                    var page = document.Pages[i];
                    var pageViewModel = new V3PageViewModel(page, _thumbnailService, _textOrientationService);
                    await pageViewModel.LoadLeftThumbnailAsync();
                    PageOperation.Pages.Add(pageViewModel);
                    await AppendDebugLogAsync($"[LoadPagesAsync] 新規Page追加: PageNumber={pageViewModel.PageNumber} (Index={i})");
                }
                
                // 🔧 アーキテクチャレベル修正: 対症療法的な再読み込みを削除
                // BitmapSourceのFreeze処理により、画像は永続的に保持されるため不要
                
                // 選択ページの処理
                if (!incrementalUpdate && Pages.Count > 0)
                {
                    // 完全リロード時: 最初のページを選択
                    SelectedPage = Pages[0];
                    await PreviewManagement.UpdatePreviewAsync(SelectedPage, true);
                    await AppendDebugLogAsync($"[LoadPagesAsync] 最初のページを選択: PageNumber={SelectedPage.PageNumber}");
                }
                else if (incrementalUpdate && existingPageCount < Pages.Count)
                {
                    // 増分更新時: 新しく追加された最初のページを選択
                    SelectedPage = Pages[existingPageCount];
                    await PreviewManagement.UpdatePreviewAsync(SelectedPage, true);
                    await AppendDebugLogAsync($"[LoadPagesAsync] 新規追加ページを選択: PageNumber={SelectedPage.PageNumber}");
                }
                
                await AppendDebugLogAsync($"[LoadPagesAsync] 完了 - 総ページ数: {Pages.Count} (新規追加: {document.Pages.Count - existingPageCount}ページ)");
                
                // PDF出力ボタンの有効状態を更新
                await AppendDebugLogAsync("[LoadPagesAsync] CanExportPdf通知を送信");
                OnPropertyChanged(nameof(CanExportPdf));
                
                // 🔧 コマンドの有効状態を強制更新
                (ExportPdfCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            }
            catch (Exception ex)
            {
                await AppendDebugLogAsync($"[LoadPagesAsync] エラー: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// ドキュメント開封時の処理
        /// </summary>
        private async void OnDocumentOpened(object? sender, DocumentOpenedEventArgs e)
        {
            StatusManagement.StartOperation("ドキュメント読み込み中...");

            try
            {
                // 🔧 V3修正: DragDropHandlerのCurrentDocumentを更新
                DragDropHandler.SetCurrentDocument(e.Document);
                
                // 🔧 Phase 1改善: LoadPagesAsyncメソッドに統一
                await LoadPagesAsync(e.Document);
                StatusManagement.CompleteOperation($"{Pages.Count}ページのドキュメントを開きました");
            }
            catch (Exception ex)
            {
                StatusManagement.ShowError($"ドキュメント読み込みエラー: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// ページ回転時の処理
        /// </summary>
        private async void OnPageRotated(object? sender, PageOperationEventArgs e)
        {
            try
            {
                var pageIndex = Pages.IndexOf(e.Page);
                if (pageIndex >= 0)
                {
                    // ページコレクション更新
                    Pages[pageIndex] = e.Page;
                    
                    // 選択ページが回転対象の場合、プレビュー更新
                    if (SelectedPage?.Id == e.Page.Id)
                    {
                        SelectedPage = e.Page;
                        await PreviewManagement.UpdatePreviewAsync(e.Page, true);
                    }
                }
            }
            catch (Exception ex)
            {
                StatusManagement.ShowError($"ページ回転更新エラー: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// ページ削除時の処理
        /// </summary>
        private void OnPageDeleted(object? sender, PageOperationEventArgs e)
        {
            try
            {
                // 🔧 V3リファクタリング: PageOperation.Pagesから既に削除されているため、ここでは調整のみ
                
                // 削除されたページが選択されていた場合の調整
                if (SelectedPage?.Id == e.Page.Id)
                {
                    SelectedPage = Pages.Count > 0 ? Pages[0] : null;
                    if (SelectedPage != null)
                    {
                        _ = PreviewManagement.UpdatePreviewAsync(SelectedPage, false);
                    }
                }

                StatusManagement.CompleteOperation($"ページを削除しました（残り{Pages.Count}ページ）");
            }
            catch (Exception ex)
            {
                StatusManagement.ShowError($"ページ削除更新エラー: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// ファイルドロップ処理時の調整
        /// </summary>
        private async void OnFilesProcessed(object? sender, FilesProcessedEventArgs e)
        {
            try
            {
                if (e.ImageFiles.Count > 0)
                {
                    // 🎯 V3修正: 画像ファイル処理はNewDocumentCreatedイベントに移行済み
                    // このメソッドは廃止予定 - DragDropHandlerがV3サービスを使用
                    
                    StatusManagement.CompleteOperation($"{e.ImageFiles.Count}個の画像ファイルを追加しました");
                }

                if (e.PdfFiles.Count > 0)
                {
                    // PDFファイル結合処理
                    StatusManagement.UpdateProgress(75, "PDF結合中...");
                    
                    StatusManagement.CompleteOperation($"{e.PdfFiles.Count}個のPDFファイルを結合しました");
                }
            }
            catch (Exception ex)
            {
                StatusManagement.ShowError($"ファイル処理エラー: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 🎯 V3 OSS標準: ファイル追加完了時の処理
        /// </summary>
        // 🔧 V3修正: FilesAddedToDocumentで既に処理されているため削除
        // LoadPagesAsyncの重複呼び出しを防ぐためコメントアウト
        /*
        private async void OnFileAdditionCompleted(object? sender, FileAdditionCompletedEventArgs e)
        {
            try
            {
                StatusManagement.StartOperation("ドキュメント更新中...");
                
                // 🔧 Phase 1改善: LoadPagesAsyncメソッドに統一
                await LoadPagesAsync(e.UpdatedDocument);
                
                // 新しく追加されたページを選択
                if (e.AddedPageCount > 0 && Pages.Count > 0)
                {
                    var newPageIndex = Math.Max(0, Pages.Count - e.AddedPageCount);
                    SelectedPage = Pages[newPageIndex];
                }
                
                StatusManagement.CompleteOperation($"{e.AddedPageCount}個のページを追加しました（合計{Pages.Count}ページ）");
            }
            catch (Exception ex)
            {
                StatusManagement.ShowError($"ファイル追加エラー: {ex.Message}", ex);
            }
        }
        */

        /// <summary>
        /// 🎯 V3 OSS標準: 新規ドキュメント作成時の処理
        /// </summary>
        private async void OnNewDocumentCreated(object? sender, NewDocumentCreatedEventArgs e)
        {
            try
            {
                await AppendDebugLogAsync("[OnNewDocumentCreated] 新規ドキュメント作成イベント受信");
                StatusManagement.StartOperation("新規ドキュメント作成中...");
                
                // 🔧 V3修正: DragDropHandlerのCurrentDocumentを更新
                DragDropHandler.SetCurrentDocument(e.Document);
                
                // 🔧 Phase 1改善: LoadPagesAsyncメソッドに統一
                await LoadPagesAsync(e.Document);
                
                StatusManagement.CompleteOperation($"新規ドキュメント作成完了（{Pages.Count}ページ）");
            }
            catch (Exception ex)
            {
                await AppendDebugLogAsync($"[OnNewDocumentCreated例外] エラー: {ex.Message}");
                StatusManagement.ShowError($"新規ドキュメント作成エラー: {ex.Message}", ex);
            }
        }

        private async void OnFilesAddedToDocument(object? sender, FilesAddedEventArgs e)
        {
            try
            {
                await AppendDebugLogAsync($"[OnFilesAddedToDocument] 既存ドキュメントへのファイル追加イベント受信");
                StatusManagement.StartOperation("既存ドキュメントにファイル追加中...");
                
                // 🔧 V3修正: DragDropHandlerのCurrentDocumentを更新
                DragDropHandler.SetCurrentDocument(e.Document);
                
                // 既存のページは保持し、新規追加ページのみを処理
                await LoadPagesAsync(e.Document, incrementalUpdate: true);
                
                // 新しく追加されたページを選択
                if (e.Result != null && e.Result.AddedPagesCount > 0 && Pages.Count > 0)
                {
                    var newPageIndex = Math.Max(0, Pages.Count - e.Result.AddedPagesCount);
                    SelectedPage = Pages[newPageIndex];
                    StatusManagement.CompleteOperation($"{e.Result.AddedPagesCount}個のページを追加しました（合計{Pages.Count}ページ）");
                }
                else
                {
                    StatusManagement.CompleteOperation($"ファイル追加完了（合計{Pages.Count}ページ）");
                }
            }
            catch (Exception ex)
            {
                StatusManagement.ShowError($"ファイル追加処理エラー: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// ページ並び替え要求時の処理
        /// </summary>
        private void OnPageReorderRequested(object? sender, PageReorderEventArgs e)
        {
            try
            {
                // PageOperationViewModelに並び替え処理を委譲
                _ = PageOperation.ReorderPagesAsync(e.PagesToMove, e.TargetPage);
            }
            catch (Exception ex)
            {
                StatusManagement.ShowError($"ページ並び替えエラー: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 操作開始時のUI状態調整
        /// </summary>
        private void OnOperationStarted(object? sender, OperationStatusEventArgs e)
        {
            // UI無効化やローディング表示などの調整
        }

        /// <summary>
        /// 操作完了時のUI状態調整
        /// </summary>
        private void OnOperationCompleted(object? sender, OperationStatusEventArgs e)
        {
            // UI有効化やローディング非表示などの調整
        }

        /// <summary>
        /// プレビュー更新時の調整
        /// </summary>
        private void OnPreviewUpdated(object? sender, PreviewUpdatedEventArgs e)
        {
            // プレビュー更新に連動したUI調整があればここで実施
        }

        private void OnDocumentSaved(object? sender, DocumentSavedEventArgs e)
        {
            StatusManagement.ShowSuccess($"ドキュメントを保存しました: {e.FilePath}");
        }

        private void OnDocumentClosed(object? sender, EventArgs e)
        {
            CurrentDocument = null;
            PageOperation.Pages.Clear();
            SelectedPage = null;
            PreviewManagement.ClearPreview();
            
            // 🔧 V3修正: DragDropHandlerのCurrentDocumentもクリア
            DragDropHandler.SetCurrentDocument(null);
            
            StatusManagement.CompleteOperation("ドキュメントを閉じました");
        }

        private void OnPageMoved(object? sender, PageOperationEventArgs e)
        {
            // ページ移動時の調整（必要に応じて実装）
        }

        /// <summary>
        /// 🔧 V3リファクタリング: Pagesコレクション変更時の処理
        /// PageOperationViewModelでページ順序が変更された際に呼ばれる
        /// </summary>
        private async void OnPagesChanged(object? sender, EventArgs e)
        {
            try
            {
                await AppendDebugLogAsync("[OnPagesChanged] ページコレクション変更イベント受信");
                
                // 🔧 修正: キャッシュをクリアして新しい参照を取得
                _pagesCache = null;
                
                // 🔧 V3修正: DragDropHandlerのCurrentDocumentを同期
                // 編集操作（移動、削除、回転）後もCurrentDocumentが同じであることを保証
                if (CurrentDocument != null)
                {
                    DragDropHandler.SetCurrentDocument(CurrentDocument);
                    await AppendDebugLogAsync("[OnPagesChanged] DragDropHandlerのCurrentDocumentを同期");
                }
                
                // 🔧 アーキテクチャレベル修正: 対症療法的な通知を削除
                // ObservableCollectionの変更は自動的に通知される
                
                // 選択ページが存在する場合はプレビューを更新
                if (SelectedPage != null)
                {
                    await PreviewManagement.UpdatePreviewAsync(SelectedPage, false);
                    await AppendDebugLogAsync($"[OnPagesChanged] プレビュー更新完了 - SelectedPage: {SelectedPage.PageNumber}");
                }
                
                await AppendDebugLogAsync($"[OnPagesChanged] 処理完了 - 総ページ数: {Pages.Count}");
                
                // PDF出力ボタンの有効状態を更新
                await AppendDebugLogAsync("[OnPagesChanged] CanExportPdf通知を送信");
                OnPropertyChanged(nameof(CanExportPdf));
                
                // 🔧 コマンドの有効状態を強制更新
                (ExportPdfCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            }
            catch (Exception ex)
            {
                await AppendDebugLogAsync($"[OnPagesChanged] エラー: {ex.Message}");
                StatusManagement.ShowError($"ページ更新エラー: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 選択ページ変更時のプレビュー更新
        /// </summary>
        partial void OnSelectedPageChanged(V3PageViewModel? value)
        {
            if (value != null)
            {
                // 🎯 V3アーキテクチャ修正: PreviewManagementViewModelを使用して右側プレビュー更新
                _ = PreviewManagement.UpdatePreviewAsync(value, false);
                
                // 🔧 真の修正: 全ページのサムネイルが読み込まれていることを確認
                // 初回読み込み時のみサムネイルが生成されている問題を修正
                _ = Task.Run(async () =>
                {
                    foreach (var page in Pages)
                    {
                        if (page.ThumbnailImage == null)
                        {
                            await page.LoadLeftThumbnailAsync();
                            await AppendDebugLogAsync($"[OnSelectedPageChanged] サムネイル読み込み: PageNumber={page.PageNumber}");
                        }
                    }
                });
            }
            else
            {
                // 選択解除時はプレビュークリア
                PreviewManagement.ClearPreview();
            }
        }

        /// <summary>
        /// PDF出力コマンド
        /// </summary>
        private async Task ExportPdfAsync()
        {
            await AppendDebugLogAsync("[ExportPdfAsync] PDF出力ボタンがクリックされました");
            
            if (Pages == null || !Pages.Any())
            {
                await AppendDebugLogAsync("[ExportPdfAsync] Pages が null または空のため処理をスキップ");
                return;
            }

            try
            {
                IsExporting = true;
                ExportProgress = 0;
                ExportStatusMessage = "PDF出力を準備中...";

                // 保存先選択
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "PDF files (*.pdf)|*.pdf",
                    DefaultExt = "pdf",
                    InitialDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output"),
                    FileName = $"document_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    // 進行状況イベント購読
                    _pdfExportService.ProgressChanged += OnExportProgressChanged;

                    try
                    {
                        await AppendDebugLogAsync($"[MainCompositeViewModel] PDF出力開始: {Pages.Count}ページ, 画質: {SelectedQuality.DisplayName}");

                        // V3PageViewModelをPdfExportPageDataに変換
                        var pageData = Pages.Select(p => new PdfExportPageData
                        {
                            ImagePath = p.Page.SourceImagePath,
                            Rotation = p.Rotation,
                            PageIndex = Pages.IndexOf(p)
                        });

                        var success = await _pdfExportService.ExportCurrentStateAsync(
                            pageData, 
                            SelectedQuality, 
                            saveDialog.FileName
                        );

                        if (success)
                        {
                            ExportStatusMessage = $"PDF出力完了: {Path.GetFileName(saveDialog.FileName)}";
                            await AppendDebugLogAsync($"[MainCompositeViewModel] PDF出力成功: {saveDialog.FileName}");
                            
                            // 保存先フォルダを開く
                            var directoryPath = Path.GetDirectoryName(saveDialog.FileName);
                            if (!string.IsNullOrEmpty(directoryPath))
                            {
                                System.Diagnostics.Process.Start("explorer.exe", directoryPath);
                            }
                        }
                        else
                        {
                            ExportStatusMessage = "PDF出力に失敗しました";
                            await AppendDebugLogAsync("[MainCompositeViewModel] PDF出力失敗");
                        }
                    }
                    finally
                    {
                        _pdfExportService.ProgressChanged -= OnExportProgressChanged;
                    }
                }
            }
            catch (Exception ex)
            {
                ExportStatusMessage = $"PDF出力エラー: {ex.Message}";
                await AppendDebugLogAsync($"[MainCompositeViewModel] PDF出力エラー: {ex.Message}");
            }
            finally
            {
                IsExporting = false;
                ExportProgress = 0;
            }
        }


        
        /// <summary>
        /// PDF出力可能かどうかを判定するメソッド（RelayCommand用）
        /// </summary>
        private bool CanExportPdfMethod()
        {
            return Pages != null && Pages.Any() && !IsExporting;
        }

        /// <summary>
        /// PDF出力可能かどうかのプロパティ（UI バインディング用）
        /// </summary>
        public bool CanExportPdf 
        { 
            get
            {
                var canExport = Pages != null && Pages.Any() && !IsExporting;
                _ = AppendDebugLogAsync($"[CanExportPdf] 結果: {canExport}, Pages数: {Pages?.Count ?? 0}, IsExporting: {IsExporting}");
                return canExport;
            }
        }

        private void OnExportProgressChanged(object? sender, PdfExportProgressEventArgs e)
        {
            ExportProgress = e.ProgressPercentage;
            ExportStatusMessage = e.CurrentOperation;
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
                System.Diagnostics.Debug.WriteLine($"[MAIN_COMPOSITE_DEBUG] {message}");
            }
            catch
            {
                // ログ出力エラーは無視
            }
        }

        /// <summary>
        /// リソース解放
        /// </summary>
        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            
            // プロパティ変更に応じた調整処理
            if (e.PropertyName == nameof(IsExporting))
            {
                OnPropertyChanged(nameof(CanExportPdf));
                
                // 🔧 コマンドの有効状態を強制更新
                (ExportPdfCommand as AsyncRelayCommand)?.NotifyCanExecuteChanged();
            }
        }
    }

    // Missing Event argument classes
    public class DocumentOpenedEventArgs : EventArgs
    {
        public PdfDocument Document { get; }

        public DocumentOpenedEventArgs(PdfDocument document)
        {
            Document = document;
        }
    }

    public class PageOperationEventArgs : EventArgs
    {
        public V3PageViewModel Page { get; }

        public PageOperationEventArgs(V3PageViewModel page)
        {
            Page = page;
        }
    }

    public class PreviewUpdatedEventArgs : EventArgs
    {
        public V3PageViewModel Page { get; }

        public PreviewUpdatedEventArgs(V3PageViewModel page)
        {
            Page = page;
        }
    }

    public class DocumentSavedEventArgs : EventArgs
    {
        public string FilePath { get; }

        public DocumentSavedEventArgs(string filePath)
        {
            FilePath = filePath;
        }
    }

    /// <summary>
    /// 🎯 V3 OSS標準: ファイル追加完了イベント引数
    /// </summary>
    public class FileAdditionCompletedEventArgs : EventArgs
    {
        public PdfDocument UpdatedDocument { get; }
        public int AddedPageCount { get; }
        public System.Collections.Generic.List<string> AddedFiles { get; }

        public FileAdditionCompletedEventArgs(PdfDocument updatedDocument, int addedPageCount, System.Collections.Generic.List<string> addedFiles)
        {
            UpdatedDocument = updatedDocument;
            AddedPageCount = addedPageCount;
            AddedFiles = addedFiles;
        }
    }

    /// <summary>
    /// 🎯 V3 OSS標準: ファイル追加失敗イベント引数
    /// </summary>
    public class FileAdditionFailedEventArgs : EventArgs
    {
        public string ErrorMessage { get; }
        public Exception? Exception { get; }
        public System.Collections.Generic.List<string> FailedFiles { get; }

        public FileAdditionFailedEventArgs(string errorMessage, Exception? exception, System.Collections.Generic.List<string> failedFiles)
        {
            ErrorMessage = errorMessage;
            Exception = exception;
            FailedFiles = failedFiles;
        }
    }
}