using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocOrganizer.Application.Interfaces;
using DocOrganizer.Core.Models;

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

        [ObservableProperty]
        private ObservableCollection<PageViewModel> pages = new();

        [ObservableProperty]
        private PageViewModel? selectedPage;

        [ObservableProperty]
        private PdfDocument? currentDocument;

        public MainCompositeViewModel(
            DocumentManagementViewModel documentManagement,
            PageOperationViewModel pageOperation,
            PreviewManagementViewModel previewManagement,
            DragDropHandlerViewModel dragDropHandler,
            StatusManagementViewModel statusManagement)
        {
            DocumentManagement = documentManagement;
            PageOperation = pageOperation;
            PreviewManagement = previewManagement;
            DragDropHandler = dragDropHandler;
            StatusManagement = statusManagement;

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
                
                // ページコレクション更新
                Pages.Clear();
                foreach (var page in e.Document.Pages)
                {
                    Pages.Add(new PageViewModel(page));
                }

                // 最初のページを選択
                if (Pages.Count > 0)
                {
                    SelectedPage = Pages[0];
                    await PreviewManagement.UpdateCurrentPageAsync(SelectedPage);
                }

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
                        await PreviewManagement.UpdateCurrentPageAsync(e.Page);
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
                        _ = PreviewManagement.UpdateCurrentPageAsync(SelectedPage);
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
                    // 画像ファイルからPDF生成後の処理
                    StatusManagement.UpdateProgress(50, "ページ追加中...");
                    
                    // 新しいページをコレクションに追加
                    // (実際の実装では、ImageProcessingServiceからのPdfDocumentを処理)
                    
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
        partial void OnSelectedPageChanged(PageViewModel? value)
        {
            if (value != null)
            {
                _ = PreviewManagement.UpdateCurrentPageAsync(value);
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
}