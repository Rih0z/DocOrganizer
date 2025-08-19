using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocOrganizer.Application.Interfaces;
using DocOrganizer.Application.Interfaces.V3;
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
        
        // 🎯 V3専用: V2依存関係完全削除
        private readonly IThumbnailGeneratorService _thumbnailService;
        private readonly ITextOrientationService _textOrientationService;

        [ObservableProperty]
        private ObservableCollection<V3PageViewModel> pages = new();

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
            ITextOrientationService textOrientationService)
        {
            DocumentManagement = documentManagement;
            PageOperation = pageOperation;
            PreviewManagement = previewManagement;
            DragDropHandler = dragDropHandler;
            StatusManagement = statusManagement;
            _thumbnailService = thumbnailService;
            _textOrientationService = textOrientationService;

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

            // DragDropHandler → 他ViewModels
            DragDropHandler.FilesProcessed += OnFilesProcessed;
            DragDropHandler.PageReorderRequested += OnPageReorderRequested;
            DragDropHandler.FileAdditionCompleted += OnFileAdditionCompleted;
            DragDropHandler.FileAdditionFailed += OnFileAdditionFailed;
            // 🚨 致命的修正: 欠落していた新規ドキュメント作成イベント
            DragDropHandler.NewDocumentCreated += OnNewDocumentCreated;
            DragDropHandler.FilesAddedToDocument += OnFilesAddedToDocument;

            // StatusManagement → 他ViewModels
            StatusManagement.OperationStarted += OnOperationStarted;
            StatusManagement.OperationCompleted += OnOperationCompleted;

            // PreviewManagement → StatusManagement
            PreviewManagement.PreviewUpdated += OnPreviewUpdated;
        }

        /// <summary>
        /// ドキュメント開封時の処理
        /// </summary>
        private async void OnDocumentOpened(object? sender, DocumentOpenedEventArgs e)
        {
            StatusManagement.StartOperation("ドキュメント読み込み中...");

            try
            {
                CurrentDocument = e.Document;
                
                // 🎯 V3修正: PreviewManagementViewModelにCurrentDocument設定（必須）
                PreviewManagement.SetCurrentDocument(CurrentDocument);
                
                // 🚨 緊急デバッグ: ドキュメント詳細ログ
                System.Diagnostics.Debug.WriteLine($"[緊急デバッグ] OnDocumentOpened開始: Document.Pages.Count={e.Document.Pages.Count}");
                
                // ページコレクション更新
                Pages.Clear();
                foreach (var page in e.Document.Pages)
                {
                    // 🎯 V3修正: V2依存関係除去 - IImageProcessingServiceを削除
                    var pageViewModel = new V3PageViewModel(page, _thumbnailService, _textOrientationService);
                    await pageViewModel.LoadLeftThumbnailAsync(); // V3 OSS標準サムネイル生成
                    Pages.Add(pageViewModel);
                    System.Diagnostics.Debug.WriteLine($"[緊急デバッグ] Page追加: PageNumber={pageViewModel.PageNumber}, SourceImagePath='{page.SourceImagePath}'");
                }

                // 🚨 緊急デバッグ: Pagesコレクション状況
                System.Diagnostics.Debug.WriteLine($"[緊急デバッグ] Pages.Count={Pages.Count}");
                
                // 最初のページを選択
                if (Pages.Count > 0)
                {
                    SelectedPage = Pages[0];
                    System.Diagnostics.Debug.WriteLine($"[緊急デバッグ] SelectedPage設定: PageNumber={SelectedPage.PageNumber}");
                    await PreviewManagement.UpdatePreviewAsync(SelectedPage, true);
                }

                StatusManagement.CompleteOperation($"{Pages.Count}ページのドキュメントを開きました");
            }
            catch (Exception ex)
            {
                StatusManagement.ShowError($"ドキュメント読み込みエラー: {ex.Message}", ex);
                System.Diagnostics.Debug.WriteLine($"[緊急デバッグ] OnDocumentOpened例外: {ex.Message}");
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
                Pages.Remove(e.Page);
                
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
        private async void OnFileAdditionCompleted(object? sender, FileAdditionCompletedEventArgs e)
        {
            try
            {
                StatusManagement.StartOperation("ドキュメント更新中...");

                // ドキュメント更新
                CurrentDocument = e.UpdatedDocument;
                
                // ページコレクション完全更新
                Pages.Clear();
                foreach (var page in e.UpdatedDocument.Pages)
                {
                    // 🎯 V3修正: V2依存関係除去 - IImageProcessingServiceを削除
                    var pageViewModel = new V3PageViewModel(page, _thumbnailService, _textOrientationService);
                    Pages.Add(pageViewModel);
                }

                // 新しく追加されたページを選択
                if (e.AddedPageCount > 0 && Pages.Count > 0)
                {
                    var newPageIndex = Math.Max(0, Pages.Count - e.AddedPageCount);
                    SelectedPage = Pages[newPageIndex];
                    await PreviewManagement.UpdatePreviewAsync(SelectedPage, true);
                }

                // 他のViewModelに変更を通知（DocumentManagementは内部で変更通知する）
                PageOperation.SetCurrentDocument(CurrentDocument);
                PreviewManagement.SetCurrentDocument(CurrentDocument);

                StatusManagement.CompleteOperation($"{e.AddedPageCount}ページを追加しました（合計{Pages.Count}ページ）");
            }
            catch (Exception ex)
            {
                StatusManagement.ShowError($"ファイル追加後の更新エラー: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 🎯 V3 OSS標準: ファイル追加失敗時の処理
        /// </summary>
        private void OnFileAdditionFailed(object? sender, FileAdditionFailedEventArgs e)
        {
            StatusManagement.ShowError($"ファイル追加エラー: {e.ErrorMessage}", e.Exception);
        }

        /// <summary>
        /// 🚨 致命的修正: 新規ドキュメント作成完了時の処理
        /// </summary>
        private async void OnNewDocumentCreated(object? sender, NewDocumentCreatedEventArgs e)
        {
            try
            {
                // 🚨 緊急デバッグ: ファイルに出力
                await AppendDebugLogAsync($"[OnNewDocumentCreated開始] Pages={e.Document.Pages.Count}");
                StatusManagement.StartOperation("新規ドキュメント読み込み中...");

                // ドキュメント更新
                CurrentDocument = e.Document;
                await AppendDebugLogAsync($"[OnNewDocumentCreated] CurrentDocument設定完了: CurrentDocument={CurrentDocument != null}");
                
                // 🎯 V3修正: 先にPreviewManagementにCurrentDocumentを設定（重要！）
                await AppendDebugLogAsync("[OnNewDocumentCreated] PreviewManagementにCurrentDocument設定開始");
                PreviewManagement.SetCurrentDocument(CurrentDocument);
                await AppendDebugLogAsync("[OnNewDocumentCreated] PreviewManagementにCurrentDocument設定完了");
                
                // ページコレクション完全更新
                await AppendDebugLogAsync("[OnNewDocumentCreated] Pages.Clear()開始");
                Pages.Clear();
                foreach (var page in e.Document.Pages)
                {
                    // 🎯 V3修正: V2依存関係除去 - IImageProcessingServiceを削除
                    var pageViewModel = new V3PageViewModel(page, _thumbnailService, _textOrientationService);
                    await pageViewModel.LoadLeftThumbnailAsync(); // V3 OSS標準サムネイル生成
                    Pages.Add(pageViewModel);
                }
                await AppendDebugLogAsync($"[OnNewDocumentCreated] Pages追加完了: Pages.Count={Pages.Count}");

                // 🎯 V3修正: PreviewManagement設定後に最初のページを選択
                if (Pages.Count > 0)
                {
                    SelectedPage = Pages[0];
                    await AppendDebugLogAsync($"[OnNewDocumentCreated] SelectedPage設定: PageNumber={SelectedPage.PageNumber}");
                    await AppendDebugLogAsync("[OnNewDocumentCreated] PreviewManagement.UpdatePreviewAsync実行開始");
                    await PreviewManagement.UpdatePreviewAsync(SelectedPage, true);
                    await AppendDebugLogAsync("[OnNewDocumentCreated] PreviewManagement.UpdatePreviewAsync実行完了");
                }

                // 他のViewModelに変更を通知
                await AppendDebugLogAsync("[OnNewDocumentCreated] 他ViewModelにCurrentDocument通知開始");
                PageOperation.SetCurrentDocument(CurrentDocument);
                DragDropHandler.SetCurrentDocument(CurrentDocument);
                await AppendDebugLogAsync("[OnNewDocumentCreated] 他ViewModelにCurrentDocument通知完了");

                StatusManagement.CompleteOperation($"{e.SourceFiles.Count}個のファイルから{Pages.Count}ページのドキュメントを作成しました");
                await AppendDebugLogAsync($"[OnNewDocumentCreated完了] 処理完了: {e.SourceFiles.Count}ファイル → {Pages.Count}ページ");
            }
            catch (Exception ex)
            {
                await AppendDebugLogAsync($"[OnNewDocumentCreated例外] エラー: {ex.Message}");
                await AppendDebugLogAsync($"[OnNewDocumentCreated例外] スタックトレース: {ex.StackTrace}");
                StatusManagement.ShowError($"新規ドキュメント作成後の更新エラー: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 🎯 V3 OSS標準: ファイル追加完了時の処理
        /// </summary>
        private async void OnFilesAddedToDocument(object? sender, FilesAddedEventArgs e)
        {
            try
            {
                StatusManagement.StartOperation("ドキュメント更新中...");

                // ドキュメント更新
                CurrentDocument = e.Document;
                
                // ページコレクション完全更新
                Pages.Clear();
                foreach (var page in e.Document.Pages)
                {
                    // 🎯 V3修正: V2依存関係除去 - IImageProcessingServiceを削除
                    var pageViewModel = new V3PageViewModel(page, _thumbnailService, _textOrientationService);
                    await pageViewModel.LoadLeftThumbnailAsync(); // V3 OSS標準サムネイル生成
                    Pages.Add(pageViewModel);
                }

                // 新しく追加されたページを選択
                if (Pages.Count > 0)
                {
                    SelectedPage = Pages[Pages.Count - 1]; // 最後のページを選択
                    await PreviewManagement.UpdatePreviewAsync(SelectedPage, true);
                }

                // 他のViewModelに変更を通知
                PageOperation.SetCurrentDocument(CurrentDocument);
                PreviewManagement.SetCurrentDocument(CurrentDocument);
                DragDropHandler.SetCurrentDocument(CurrentDocument);

                StatusManagement.CompleteOperation($"ファイルを追加しました（合計{Pages.Count}ページ）");
            }
            catch (Exception ex)
            {
                StatusManagement.ShowError($"ファイル追加後の更新エラー: {ex.Message}", ex);
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
            Pages.Clear();
            SelectedPage = null;
            PreviewManagement.ClearPreview();
            StatusManagement.CompleteOperation("ドキュメントを閉じました");
        }

        private void OnPageMoved(object? sender, PageOperationEventArgs e)
        {
            // ページ移動時の調整（必要に応じて実装）
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
            }
            else
            {
                // 選択解除時はプレビュークリア
                PreviewManagement.ClearPreview();
            }
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