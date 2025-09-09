using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
        private bool _isMovingPage = false;  // 再入防止フラグ

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

        // 自動回転コマンド（未実装）
        [RelayCommand]
        private void AutoCorrectAllPagesOrientation()
        {
            _dialogService.ShowInformation("自動回転機能は現在実装中です。次のバージョンで利用可能になります。");
        }

        public PageOperationViewModel(
            IPdfEditorService pdfEditorService,
            IDialogService dialogService)
        {
            _pdfEditorService = pdfEditorService;
            _dialogService = dialogService;
            
            // 初期状態を設定
            CanMoveUp = false;
            CanMoveDown = false;
            HasSelectedPages = false;
            
            // Pagesコレクションの変更を監視
            Pages.CollectionChanged += OnPagesCollectionChanged;
            
            // デバッグ: コマンドが生成されているか確認
            System.Diagnostics.Debug.WriteLine($"[PageOperationViewModel] Constructor - MovePageUpCommand: {MovePageUpCommand != null}");
            System.Diagnostics.Debug.WriteLine($"[PageOperationViewModel] Constructor - MovePageDownCommand: {MovePageDownCommand != null}");
            System.Diagnostics.Debug.WriteLine($"[PageOperationViewModel] Constructor - RotateLeftCommand: {RotateLeftCommand != null}");
            System.Diagnostics.Debug.WriteLine($"[PageOperationViewModel] Constructor - RotateRightCommand: {RotateRightCommand != null}");
            System.Diagnostics.Debug.WriteLine($"[PageOperationViewModel] Constructor - DeleteSelectedPagesCommand: {DeleteSelectedPagesCommand != null}");
        }

        private void OnPagesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // Move操作中はイベント処理をスキップ
            if (_isMovingPage)
            {
                System.Diagnostics.Debug.WriteLine("[OnPagesCollectionChanged] Skipped - Move operation in progress");
                return;
            }
            
            UpdateSelectionState();
        }

        /// <summary>
        /// 左回転（反時計回り90度）
        /// </summary>
        [RelayCommand]
        private async Task RotateLeftAsync()
        {
            if (_currentDocument == null || !Pages.Any(p => p.IsSelected))
            {
                return;
            }
            await RotateSelectedPagesAdvancedAsync(270); // 左回転 = 270度（反時計回り）
        }

        /// <summary>
        /// 右回転（時計回り90度）
        /// </summary>
        [RelayCommand]
        private async Task RotateRightAsync()
        {
            if (_currentDocument == null || !Pages.Any(p => p.IsSelected))
            {
                return;
            }
            await RotateSelectedPagesAdvancedAsync(90); // 右回転 = 90度（時計回り）
        }

        /// <summary>
        /// 選択ページ削除
        /// </summary>
        [RelayCommand]
        private async Task DeleteSelectedPagesAsync()
        {
            if (_currentDocument == null || !Pages.Any(p => p.IsSelected))
            {
                return;
            }
            if (_currentDocument == null) return;

            var selectedPages = Pages.Where(p => p.IsSelected).OrderByDescending(p => p.PageNumber).ToList();

            // 確認ダイアログなしで直接削除
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
                // エラーメッセージは表示しない（ログのみ）
                await AppendDebugLogAsync($"[RemovePageAsync] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// ページを上に移動
        /// </summary>
        [RelayCommand]
        private async Task MovePageUpAsync()
        {
            System.Diagnostics.Debug.WriteLine("[MovePageUpAsync] メソッドが呼び出されました！");
            
            // 再入防止チェック
            if (_isMovingPage)
            {
                await AppendDebugLogAsync("[MovePageUp] 再入防止: 既に移動処理中");
                return;
            }
            
            if (_currentDocument == null || Pages.Count <= 1) 
            {
                return;
            }
            
            // 最初の選択ページを取得
            var selectedPage = Pages.FirstOrDefault(p => p.IsSelected);
            if (selectedPage == null)
            {
                return;
            }

            _isMovingPage = true;  // フラグセット（CollectionChangedイベントを無効化）
            try
            {
                var currentIndex = Pages.IndexOf(selectedPage);
                await AppendDebugLogAsync($"[MovePageUp] START - Index: {currentIndex}, Count: {Pages.Count}");
                
                if (currentIndex <= 0) 
                {
                    await AppendDebugLogAsync($"[MovePageUp] Cannot move up - already at top or not found");
                    return;
                }

                // V3.0.031の実装に完全復元
                await AppendDebugLogAsync($"[MovePageUp] 移動前: {string.Join(",", Pages.Select(p => p.PageNumber))}");
                
                // ObservableCollectionで位置を移動
                Pages.Move(currentIndex, currentIndex - 1);
                await AppendDebugLogAsync($"[MovePageUp] 移動後: {string.Join(",", Pages.Select(p => p.PageNumber))}");

                // PDFドキュメント側も同じ順序に更新
                if (_currentDocument != null && currentIndex < _currentDocument.Pages.Count)
                {
                    _currentDocument.MovePage(currentIndex, currentIndex - 1);
                }

                // ページ番号を再設定
                UpdatePageNumbers();

                // CollectionChangedイベントがスキップされたため、手動で状態を更新
                UpdateSelectionState();

                StatusMessage = $"ページ {selectedPage.PageNumber} を上に移動しました";
                
                // Force UI collection refresh
                OnPropertyChanged(nameof(Pages));
                
                // イベント通知
                PagesChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                // エラーメッセージは表示しない（ログのみ）
                await AppendDebugLogAsync($"[MovePageUp Error] {ex.Message}");
            }
            finally
            {
                _isMovingPage = false;  // フラグリセット（CollectionChangedイベントを再有効化）
                await AppendDebugLogAsync("[MovePageUp] END - フラグリセット");
            }
        }

        /// <summary>
        /// ページを下に移動
        /// </summary>
        [RelayCommand]
        private async Task MovePageDownAsync()
        {
            System.Diagnostics.Debug.WriteLine("[MovePageDownAsync] メソッドが呼び出されました！");
            
            // 再入防止チェック
            if (_isMovingPage)
            {
                await AppendDebugLogAsync("[MovePageDown] 再入防止: 既に移動処理中");
                return;
            }
            
            if (_currentDocument == null || Pages.Count <= 1) 
            {
                return;
            }
            
            // 最初の選択ページを取得
            var selectedPage = Pages.FirstOrDefault(p => p.IsSelected);
            if (selectedPage == null)
            {
                return;
            }

            _isMovingPage = true;  // フラグセット（CollectionChangedイベントを無効化）
            try
            {
                var currentIndex = Pages.IndexOf(selectedPage);
                await AppendDebugLogAsync($"[MovePageDown] START - Index: {currentIndex}, Count: {Pages.Count}");
                
                if (currentIndex >= Pages.Count - 1 || currentIndex < 0) 
                {
                    await AppendDebugLogAsync($"[MovePageDown] Cannot move down - already at bottom or not found");
                    return;
                }

                // V3.0.031の実装に完全復元
                await AppendDebugLogAsync($"[MovePageDown] 移動前: {string.Join(",", Pages.Select(p => p.PageNumber))}");
                
                // ObservableCollectionで位置を移動
                Pages.Move(currentIndex, currentIndex + 1);
                await AppendDebugLogAsync($"[MovePageDown] 移動後: {string.Join(",", Pages.Select(p => p.PageNumber))}");

                // PDFドキュメント側も同じ順序に更新
                if (_currentDocument != null && currentIndex < _currentDocument.Pages.Count)
                {
                    _currentDocument.MovePage(currentIndex, currentIndex + 1);
                }

                // ページ番号を再設定
                UpdatePageNumbers();

                // CollectionChangedイベントがスキップされたため、手動で状態を更新
                UpdateSelectionState();

                StatusMessage = $"ページ {selectedPage.PageNumber} を下に移動しました";
                
                // Force UI collection refresh
                OnPropertyChanged(nameof(Pages));
                
                // イベント通知
                PagesChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                // エラーメッセージは表示しない（ログのみ）
                await AppendDebugLogAsync($"[MovePageDown Error] {ex.Message}");
            }
            finally
            {
                _isMovingPage = false;  // フラグリセット（CollectionChangedイベントを再有効化）
                await AppendDebugLogAsync("[MovePageDown] END - フラグリセット");
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
                // エラーメッセージは表示しない（ログのみ）
                await AppendDebugLogAsync($"[SortPages Error] {ex.Message}");
            }
        }

        /// <summary>
        /// 🎯 V3.0.025: InsertIndexベースのページ並び替え（ドラッグ&ドロップ対応）
        /// </summary>
        public async Task ReorderPagesAsync(List<V3PageViewModel> pagesToMove, int insertIndex)
        {
            if (_currentDocument == null || pagesToMove == null || insertIndex < 0)
                return;

            try
            {
                await AppendDebugLogAsync($"[ReorderPagesAsync] InsertIndex版開始 - 移動ページ数: {pagesToMove.Count}, 挿入位置: {insertIndex}");

                // 挿入位置がページ数を超えないよう調整
                int targetIndex = Math.Min(insertIndex, Pages.Count);
                await AppendDebugLogAsync($"[ReorderPagesAsync] 調整後のターゲットインデックス: {targetIndex}");

                // ドラッグされたページを一時的に削除
                var movingPages = new List<(V3PageViewModel page, int originalIndex)>();
                foreach (var page in pagesToMove.OrderByDescending(p => Pages.IndexOf(p)))
                {
                    int originalIndex = Pages.IndexOf(page);
                    if (originalIndex != -1)
                    {
                        movingPages.Insert(0, (page, originalIndex));
                        Pages.RemoveAt(originalIndex);
                        await AppendDebugLogAsync($"[ReorderPagesAsync] ページ削除: インデックス {originalIndex}");

                        // ターゲットインデックスの調整（削除されたページが挿入位置より前にある場合）
                        if (originalIndex < targetIndex)
                            targetIndex--;
                    }
                }

                await AppendDebugLogAsync($"[ReorderPagesAsync] 最終挿入位置: {targetIndex}");

                // ターゲット位置に挿入
                foreach (var (page, originalIndex) in movingPages)
                {
                    Pages.Insert(targetIndex, page);
                    await AppendDebugLogAsync($"[ReorderPagesAsync] ページ挿入: 位置 {targetIndex}, 元インデックス {originalIndex}");
                    targetIndex++;
                }

                // ページ番号を再設定
                UpdatePageNumbers();
                await AppendDebugLogAsync("[ReorderPagesAsync] ページ番号更新完了");

                // 実際のPDFドキュメントのページ順序も更新
                _pdfEditorService.ReorderPages(_currentDocument, Pages.Select(p => p.Page).ToArray());
                await AppendDebugLogAsync("[ReorderPagesAsync] PDFドキュメント並び替え完了");

                StatusMessage = $"{pagesToMove.Count} ページを位置 {insertIndex} に移動しました";

                // イベント通知
                PagesChanged?.Invoke(this, EventArgs.Empty);
                await AppendDebugLogAsync("[ReorderPagesAsync] InsertIndex版完了");
            }
            catch (Exception ex)
            {
                await AppendDebugLogAsync($"[ReorderPagesAsync] 例外発生: {ex.Message}");
                // エラーメッセージは表示しない（ログのみ）
                await AppendDebugLogAsync($"[SortPages Error] {ex.Message}");
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
                // エラーメッセージは表示しない（ログのみ）
                await AppendDebugLogAsync($"[RotatePage Error] {ex.Message}");
            }
        }

        /// <summary>
        /// 🆕 HEIC編集対応: 選択ページの形式に応じた回転処理
        /// </summary>
        private async Task RotateSelectedPagesAdvancedAsync(int degrees)
        {
            try
            {
                if (_currentDocument == null) return;

                var selectedPages = Pages.Where(p => p.IsSelected).ToList();
                if (!selectedPages.Any()) return;

                foreach (var pageVm in selectedPages)
                {
                    // HEIC形式の場合は特別処理
                    if (IsHeicSource(pageVm))
                    {
                        await RotateHeicPageAsync(pageVm, degrees);
                    }
                    else
                    {
                        // 通常の回転処理（既存実装）
                        await RotateStandardPageAsync(pageVm, degrees);
                    }
                }

                StatusMessage = $"{selectedPages.Count} ページを{Math.Abs(degrees)}度回転しました";
                
                // イベント通知
                PagesChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                // エラーメッセージは表示しない（ログのみ）
                await AppendDebugLogAsync($"[RotatePage Error] {ex.Message}");
            }
        }

        /// <summary>
        /// 🆕 HEICページの特別回転処理
        /// </summary>
        private async Task RotateHeicPageAsync(V3PageViewModel pageVm, int degrees)
        {
            try
            {
                // Core層データ更新（回転角度計算）
                var newRotation = (pageVm.Page.Rotation + degrees) % 360;
                if (newRotation < 0) newRotation += 360;

                pageVm.Page.Rotation = newRotation;

                // ページViewModelの回転更新
                pageVm.UpdateRotationSync();

                // 🎯 HEICの場合はより高品質なサムネイル再生成
                await pageVm.RegenerateThumbnailAfterRotationAsync();

                await AppendDebugLogAsync($"[HEIC_ROTATION] HEIC回転処理完了 - ページ{pageVm.PageNumber}: {degrees}度, 累積回転: {newRotation}度");
            }
            catch (Exception ex)
            {
                await AppendDebugLogAsync($"[HEIC_ROTATION_ERROR] HEICページ回転エラー - ページ{pageVm.PageNumber}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 🆕 標準ページの回転処理
        /// </summary>
        private async Task RotateStandardPageAsync(V3PageViewModel pageVm, int degrees)
        {
            try
            {
                // Core層データ更新（回転角度計算）
                var newRotation = (pageVm.Page.Rotation + degrees) % 360;
                if (newRotation < 0) newRotation += 360;

                pageVm.Page.Rotation = newRotation;

                // ページViewModelの回転更新
                pageVm.UpdateRotationSync();

                // サムネイル再生成
                await pageVm.RegenerateThumbnailAfterRotationAsync();

                await AppendDebugLogAsync($"[STANDARD_ROTATION] 標準回転処理完了 - ページ{pageVm.PageNumber}: {degrees}度, 累積回転: {newRotation}度");
            }
            catch (Exception ex)
            {
                await AppendDebugLogAsync($"[STANDARD_ROTATION_ERROR] 標準ページ回転エラー - ページ{pageVm.PageNumber}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 🆕 HEIC形式判定
        /// </summary>
        private bool IsHeicSource(V3PageViewModel pageVm)
        {
            // ページの元ファイルパスからHEIC判定
            var sourcePath = pageVm.Page?.ImagePath ?? "";
            if (string.IsNullOrEmpty(sourcePath)) return false;
            
            var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
            return extension == ".heic" || extension == ".heif";
        }

        /// <summary>
        /// 🆕 GIF形式判定
        /// </summary>
        private bool IsGifSource(V3PageViewModel pageVm)
        {
            var sourcePath = pageVm.Page?.ImagePath ?? "";
            if (string.IsNullOrEmpty(sourcePath)) return false;
            
            return Path.GetExtension(sourcePath).ToLowerInvariant() == ".gif";
        }

        /// <summary>
        /// 🆕 統一編集可能性判定
        /// </summary>
        public bool CanEditSelectedPages()
        {
            var selectedPages = Pages.Where(p => p.IsSelected).ToList();
            if (!selectedPages.Any()) return false;

            // 全ての選択ページが編集可能形式かチェック
            foreach (var page in selectedPages)
            {
                if (!IsEditableFormat(page))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 🆕 編集可能形式判定
        /// </summary>
        private bool IsEditableFormat(V3PageViewModel pageVm)
        {
            // PDF、HEIC、GIF、標準画像形式は全て編集可能
            return IsHeicSource(pageVm) || IsGifSource(pageVm) || IsStandardImageFormat(pageVm);
        }

        /// <summary>
        /// 🆕 標準画像形式判定
        /// </summary>
        private bool IsStandardImageFormat(V3PageViewModel pageVm)
        {
            var sourcePath = pageVm.Page?.ImagePath ?? "";
            if (string.IsNullOrEmpty(sourcePath)) return true; // PDFページの場合

            var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
            return extension == ".jpg" || extension == ".jpeg" || 
                   extension == ".png" || extension == ".bmp" || 
                   extension == ".tiff" || extension == ".webp";
        }

        private void UpdatePageNumbers()
        {
            // V3.0.048: インデクサアクセスを避けてforeachを使用
            int pageNumber = 1;
            foreach (var page in Pages)
            {
                // ページ番号が変更された場合のみ更新
                if (page.PageNumber != pageNumber)
                {
                    page.UpdatePageNumber(pageNumber);
                }
                pageNumber++;
            }
            
            // V3.0.048: CollectionChangedの連鎖を防ぐため、個別のPropertyChangedは発火させない
            // OnPropertyChanged(nameof(Pages)); // 削除
        }

        private void UpdateSelectionState()
        {
            var selectedCount = Pages.Count(p => p.IsSelected);
            HasSelectedPages = selectedCount > 0;
            
            System.Diagnostics.Debug.WriteLine($"[UpdateSelectionState] SelectedCount: {selectedCount}, HasSelectedPages: {HasSelectedPages}");

            // 移動可能性を判定
            if (selectedCount == 1)
            {
                var selectedPage = Pages.FirstOrDefault(p => p.IsSelected);
                if (selectedPage != null)
                {
                    var selectedIndex = Pages.IndexOf(selectedPage);
                    var oldCanMoveUp = CanMoveUp;
                    var oldCanMoveDown = CanMoveDown;
                    
                    CanMoveUp = selectedIndex > 0;
                    CanMoveDown = selectedIndex < Pages.Count - 1;
                    
                    System.Diagnostics.Debug.WriteLine($"[UpdateSelectionState] SelectedIndex: {selectedIndex}, CanMoveUp: {oldCanMoveUp} -> {CanMoveUp}, CanMoveDown: {oldCanMoveDown} -> {CanMoveDown}, PagesCount: {Pages.Count}");
                }
            }
            else
            {
                var oldCanMoveUp = CanMoveUp;
                var oldCanMoveDown = CanMoveDown;
                CanMoveUp = false;
                CanMoveDown = false;
                System.Diagnostics.Debug.WriteLine($"[UpdateSelectionState] Multiple or no selection - CanMoveUp: {oldCanMoveUp} -> false, CanMoveDown: {oldCanMoveDown} -> false");
            }

            // Force command state refresh
            MovePageUpCommand?.NotifyCanExecuteChanged();
            MovePageDownCommand?.NotifyCanExecuteChanged();
            
            // プロパティ変更通知でコマンドの状態も更新される
            OnPropertyChanged(nameof(CanMoveUp));
            OnPropertyChanged(nameof(CanMoveDown));
            OnPropertyChanged(nameof(HasSelectedPages));
        }

        // Public methods for external coordination
        public void SetCurrentDocument(PdfDocument? document)
        {
            _currentDocument = document;
            UpdateSelectionState();
        }

        public void NotifyPageSelectionChanged()
        {
            System.Diagnostics.Debug.WriteLine("[NotifyPageSelectionChanged] Called");
            UpdateSelectionState();
        }

        // Events for coordination with other ViewModels
        public event EventHandler? PagesChanged;
        
        /// <summary>
        /// 🚨 緊急デバッグ: ファイルに詳細ログを出力（第16条準拠）
        /// </summary>
        private async Task AppendDebugLogAsync(string message)
        {
            try
            {
                var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
                await DocOrganizer.Core.Logging.DebugLogger.LogAsync(message, "PageOperation");
                System.Diagnostics.Debug.WriteLine($"[PAGE_OPERATION_DEBUG] {message}");
            }
            catch
            {
                // ログ出力エラーは無視
            }
        }
        public event EventHandler<PageOperationEventArgs>? PageRotated;
        public event EventHandler<PageOperationEventArgs>? PageDeleted;
        public event EventHandler<List<V3PageViewModel>>? PagesDeleted;
        public event EventHandler<PageOperationEventArgs>? PageMoved;
    }
}