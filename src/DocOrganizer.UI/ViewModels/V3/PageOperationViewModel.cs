using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocOrganizer.Application.Interfaces;
using DocOrganizer.Core.Models;

namespace DocOrganizer.UI.ViewModels.V3
{
    /// <summary>
    /// 🎯 V3アーキテクチャ: ページ操作専用ViewModel
    /// 責務: Rotate, Delete, Move, Reorder のみ
    /// 目標: 250行以下、8メソッド以下
    /// </summary>
    public partial class PageOperationViewModel : ObservableObject
    {
        private readonly IPdfEditorService _pdfEditorService;
        private readonly IDialogService _dialogService;

        [ObservableProperty]
        private ObservableCollection<V3PageViewModel> pages = new();

        [ObservableProperty]
        private bool hasSelectedPages;

        [ObservableProperty]
        private bool canMoveUp;

        [ObservableProperty]
        private bool canMoveDown;

        [ObservableProperty]
        private string statusMessage = "準備完了";

        private PdfDocument? _currentDocument;

        public PageOperationViewModel(
            IPdfEditorService pdfEditorService,
            IDialogService dialogService)
        {
            _pdfEditorService = pdfEditorService;
            _dialogService = dialogService;
        }

        /// <summary>
        /// 左回転（反時計回り90度）
        /// </summary>
        [RelayCommand(CanExecute = nameof(HasSelectedPages))]
        private async Task RotateLeftAsync()
        {
            await RotateSelectedPagesAsync(270); // 左回転 = 270度（反時計回り）
        }

        /// <summary>
        /// 右回転（時計回り90度）
        /// </summary>
        [RelayCommand(CanExecute = nameof(HasSelectedPages))]
        private async Task RotateRightAsync()
        {
            await RotateSelectedPagesAsync(90); // 右回転 = 90度（時計回り）
        }

        /// <summary>
        /// 選択ページ削除
        /// </summary>
        [RelayCommand(CanExecute = nameof(HasSelectedPages))]
        private async Task DeleteSelectedPagesAsync()
        {
            if (_currentDocument == null) return;

            var selectedPages = Pages.Where(p => p.IsSelected).OrderByDescending(p => p.PageNumber).ToList();

            if (_dialogService.ShowConfirmation($"{selectedPages.Count} ページを削除しますか？"))
            {
                try
                {
                    foreach (var pageVm in selectedPages)
                    {
                        _pdfEditorService.RemovePage(_currentDocument, pageVm.PageNumber);
                        Pages.Remove(pageVm);
                    }

                    // ページ番号を再設定
                    UpdatePageNumbers();

                    StatusMessage = $"{selectedPages.Count} ページを削除しました";
                    
                    // イベント通知
                    PagesChanged?.Invoke(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    _dialogService.ShowError($"削除エラー: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// ページを上に移動
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanMoveUp))]
        private async Task MovePageUpAsync()
        {
            if (_currentDocument == null || !CanMoveUp) return;

            try
            {
                var selectedPage = Pages.FirstOrDefault(p => p.IsSelected);
                if (selectedPage == null) return;

                var currentIndex = Pages.IndexOf(selectedPage);
                if (currentIndex <= 0) return;

                // ObservableCollectionで位置を移動
                Pages.Move(currentIndex, currentIndex - 1);

                // PDFドキュメント側も同じ順序に更新
                if (currentIndex < _currentDocument.Pages.Count)
                {
                    _currentDocument.MovePage(currentIndex, currentIndex - 1);
                }

                // ページ番号を再設定
                UpdatePageNumbers();

                // UI状態を更新
                UpdateSelectionState();

                StatusMessage = $"ページ {selectedPage.PageNumber} を上に移動しました";
                
                // イベント通知
                PagesChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"移動エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// ページを下に移動
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanMoveDown))]
        private async Task MovePageDownAsync()
        {
            if (_currentDocument == null || !CanMoveDown) return;

            try
            {
                var selectedPage = Pages.FirstOrDefault(p => p.IsSelected);
                if (selectedPage == null) return;

                var currentIndex = Pages.IndexOf(selectedPage);
                if (currentIndex >= Pages.Count - 1) return;

                // ObservableCollectionで位置を移動
                Pages.Move(currentIndex, currentIndex + 1);

                // PDFドキュメント側も同じ順序に更新
                if (currentIndex + 1 < _currentDocument.Pages.Count)
                {
                    _currentDocument.MovePage(currentIndex, currentIndex + 1);
                }

                // ページ番号を再設定
                UpdatePageNumbers();

                // UI状態を更新
                UpdateSelectionState();

                StatusMessage = $"ページ {selectedPage.PageNumber} を下に移動しました";
                
                // イベント通知
                PagesChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"移動エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// ページ並び替え（ドラッグ&ドロップ用）
        /// </summary>
        public async Task ReorderPagesAsync(List<V3PageViewModel> pagesToMove, V3PageViewModel targetPage)
        {
            if (_currentDocument == null || pagesToMove == null || targetPage == null)
                return;

            try
            {
                // ドラッグされたページとターゲットページのインデックスを取得
                int targetIndex = Pages.IndexOf(targetPage);
                if (targetIndex == -1) return;

                // ドラッグされたページを一時的に削除
                var movingPages = new List<(V3PageViewModel page, int originalIndex)>();
                foreach (var page in pagesToMove.OrderByDescending(p => Pages.IndexOf(p)))
                {
                    int originalIndex = Pages.IndexOf(page);
                    if (originalIndex != -1)
                    {
                        movingPages.Insert(0, (page, originalIndex));
                        Pages.RemoveAt(originalIndex);

                        // ターゲットインデックスの調整
                        if (originalIndex < targetIndex)
                            targetIndex--;
                    }
                }

                // ターゲット位置に挿入
                foreach (var (page, _) in movingPages)
                {
                    Pages.Insert(targetIndex, page);
                    targetIndex++;
                }

                // ページ番号を再設定
                UpdatePageNumbers();

                // 実際のPDFドキュメントのページ順序も更新
                _pdfEditorService.ReorderPages(_currentDocument, Pages.Select(p => p.Page).ToArray());

                StatusMessage = $"{pagesToMove.Count} ページを移動しました";
                
                // イベント通知
                PagesChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"並び替えエラー: {ex.Message}");
            }
        }

        // Private helper methods
        private async Task RotateSelectedPagesAsync(int degrees)
        {
            try
            {
                if (_currentDocument == null) return;

                var selectedPages = Pages.Where(p => p.IsSelected).ToList();
                if (!selectedPages.Any()) return;

                // 現在選択されているページを保持
                var currentSelectedPage = selectedPages.FirstOrDefault();

                foreach (var pageVm in selectedPages)
                {
                    // Core層データ更新（回転角度計算）
                    var newRotation = (pageVm.Page.Rotation + degrees) % 360;
                    if (newRotation < 0) newRotation += 360;

                    pageVm.Page.Rotation = newRotation;

                    // ページViewModelの回転更新
                    pageVm.UpdateRotationSync();

                    // サムネイル再生成
                    await pageVm.RegenerateThumbnailAfterRotationAsync();
                }

                StatusMessage = $"{selectedPages.Count} ページを{Math.Abs(degrees)}度回転しました";
                
                // イベント通知
                PagesChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"回転エラー: {ex.Message}");
            }
        }

        private void UpdatePageNumbers()
        {
            for (int i = 0; i < Pages.Count; i++)
            {
                Pages[i].UpdatePageNumber(i + 1);
            }
        }

        private void UpdateSelectionState()
        {
            var selectedCount = Pages.Count(p => p.IsSelected);
            HasSelectedPages = selectedCount > 0;

            // 移動可能性を判定
            if (selectedCount == 1)
            {
                var selectedPage = Pages.FirstOrDefault(p => p.IsSelected);
                if (selectedPage != null)
                {
                    var selectedIndex = Pages.IndexOf(selectedPage);
                    CanMoveUp = selectedIndex > 0;
                    CanMoveDown = selectedIndex < Pages.Count - 1;
                }
            }
            else
            {
                CanMoveUp = false;
                CanMoveDown = false;
            }

            // コマンドの状態変更を通知
            MovePageUpCommand?.NotifyCanExecuteChanged();
            MovePageDownCommand?.NotifyCanExecuteChanged();
            RotateLeftCommand?.NotifyCanExecuteChanged();
            RotateRightCommand?.NotifyCanExecuteChanged();
            DeleteSelectedPagesCommand?.NotifyCanExecuteChanged();
        }

        // Public methods for external coordination
        public void SetCurrentDocument(PdfDocument? document)
        {
            _currentDocument = document;
            UpdateSelectionState();
        }

        public void NotifyPageSelectionChanged()
        {
            UpdateSelectionState();
        }

        // Events for coordination with other ViewModels
        public event EventHandler? PagesChanged;
        public event EventHandler<PageOperationEventArgs>? PageRotated;
        public event EventHandler<PageOperationEventArgs>? PageDeleted;
        public event EventHandler<List<V3PageViewModel>>? PagesDeleted;
        public event EventHandler<PageOperationEventArgs>? PageMoved;
    }
}