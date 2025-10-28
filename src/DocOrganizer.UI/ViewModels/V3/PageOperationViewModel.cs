using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocOrganizer.Application.Interfaces;
using DocOrganizer.Application.Interfaces.V3;
using DocOrganizer.Core.Models;
using DocOrganizer.Core.Logging;
using DocOrganizer.Core.Services;
using DocOrganizer.Core.Commands;
using DocOrganizer.UI.Views;  // ✅ V3.0.130: MainWindow参照用

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
        private readonly IUndoRedoService _undoRedoService;
        private readonly IThumbnailGeneratorService _thumbnailService;
        private bool _isMovingPage = false;  // 再入防止フラグ
        private Action? _syncSelectionToView;  // V3.0.115: View選択状態同期用コールバック
        private Action? _disableSelectionEvents;  // V3.0.115: SelectionChangedイベント無効化用
        private Action? _enableSelectionEvents;   // V3.0.115: SelectionChangedイベント再有効化用

        [ObservableProperty]
        private ObservableCollection<V3PageViewModel> pages = new();

        [ObservableProperty]
        private bool hasSelectedPages;

        [ObservableProperty]
        private bool isAllPagesSelected;

        [ObservableProperty]
        private int selectedPagesCount;

        [ObservableProperty]
        private bool canMoveUp;

        [ObservableProperty]
        private bool canMoveDown;

        [ObservableProperty]
        private string statusMessage = "準備完了";

        /// <summary>
        /// ✅ V3.0.132: 選択ページ数の表示テキスト
        /// ViewModelの選択状態を直接表示（ListBoxの仮想化に依存しない）
        /// </summary>
        public string SelectedPagesCountText
        {
            get
            {
                if (Pages == null || Pages.Count == 0)
                    return "";

                var selectedCount = Pages.Count(p => p.IsSelected);
                return selectedCount > 0 ? $"{selectedCount}ページ選択" : "";
            }
        }

        private PdfDocument? _currentDocument;

        // コマンドプロパティ（明示的定義）
        public IRelayCommand SelectAllCommand { get; private set; }
        public IRelayCommand ShowHelpCommand { get; private set; }
        public IRelayCommand RotateLeftCommand { get; private set; }
        public IRelayCommand RotateRightCommand { get; private set; }
        public IRelayCommand DeleteSelectedPagesCommand { get; private set; }
        public IRelayCommand MovePageUpCommand { get; private set; }
        public IRelayCommand MovePageDownCommand { get; private set; }
        public IRelayCommand AutoCorrectAllPagesOrientationCommand { get; private set; }
        
        // 新規追加コマンド
        public IRelayCommand DeselectAllCommand { get; private set; }
        public IRelayCommand GoToPageCommand { get; private set; }
        public IRelayCommand PreviousPageCommand { get; private set; }
        public IRelayCommand NextPageCommand { get; private set; }
        public IRelayCommand FirstPageCommand { get; private set; }
        public IRelayCommand LastPageCommand { get; private set; }
        
        // テスト用コマンド（デバッグ用）
        public IRelayCommand TestErrorDialogCommand { get; private set; }
        public IRelayCommand TestWarningDialogCommand { get; private set; }
        public IRelayCommand TestConfirmationDialogCommand { get; private set; }
        public IRelayCommand TestInputDialogCommand { get; private set; }

        // 自動回転コマンド（未実装）
        private void AutoCorrectAllPagesOrientation()
        {
            // エラーダイアログ表示を削除 - V3.0.081
            // _dialogService.ShowInformation("自動回転機能は現在実装中です。次のバージョンで利用可能になります。");
        }

        // 全選択コマンド (Ctrl+A)
        private void SelectAll()
        {
            if (Pages == null || Pages.Count == 0) return;

            System.Diagnostics.Debug.WriteLine($"[SelectAll] 全選択開始: {Pages.Count}ページ");

            foreach (var page in Pages)
            {
                page.IsSelected = true;
            }

            UpdateSelectionState();
            StatusMessage = $"全てのページ ({Pages.Count}ページ) を選択しました";

            // ✅ V3.0.130: MainWindowに全選択を通知（ListBoxの仮想化対策）
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
                mainWindow?.ForceListBoxFullSelection();
            });

            // 選択状態変更を通知
            NotifyPageSelectionChanged();

            // ポップアップは表示しない（ステータスメッセージのみ）
        }

        // ヘルプ表示コマンド (F1)
        private void ShowHelp()
        {
            // デバッグ用ログ
            System.Diagnostics.Debug.WriteLine("[ShowHelp] ヘルプ表示メソッド実行開始");
            DebugLogger.Log("[ShowHelp] ヘルプ表示メソッド実行開始");
            
            var helpMessage = @"DocOrganizer V3.0.082 - ショートカットキー一覧

【基本操作】
Ctrl+A: 全ページを選択
Ctrl+Shift+D: 選択解除
Delete / Ctrl+D: 選択したページを削除
F1 / Ctrl+H: このヘルプを表示

【ページ操作】（CubePDF互換）
Ctrl+B: 選択ページを上に移動（Back）
Ctrl+F: 選択ページを下に移動（Forward）
Alt+↑/↓: 選択ページを上下に移動（代替）
PageUp/PageDown: 前後のページへ移動
Home/End: 最初/最後のページへ
Ctrl+G: ページジャンプ

【回転】（CubePDF互換）
Ctrl+L: 左回転（反時計回り）（Left）
Ctrl+R: 右回転（時計回り）（Right）
Alt+←/→: 左右回転（代替）

【ファイル操作】
Ctrl+N: 新規作成
Ctrl+O: ファイルを開く
Ctrl+S: 保存
Ctrl+Shift+S: 名前を付けて保存
Ctrl+W: 閉じる
Ctrl+Q / Alt+F4: 終了

【編集操作】
Ctrl+Z: 元に戻す（Undo）
Ctrl+Y: やり直し（Redo）

【複数選択】
Ctrl+クリック: 個別選択の追加/削除
Shift+クリック: 範囲選択
ドラッグ&ドロップ: ページの並び替え

【表示】
Ctrl+ +/-: 拡大/縮小
Ctrl+0: ウィンドウに合わせる
F11: フルスクリーン

注：CubePDF Utility互換のキーボード操作を採用しています。";

            _dialogService.ShowInformation(helpMessage, "ヘルプ");
            
            // デバッグ用ログ
            System.Diagnostics.Debug.WriteLine("[ShowHelp] ヘルプダイアログ表示完了");
            DebugLogger.Log("[ShowHelp] ヘルプダイアログ表示完了");
        }

        public PageOperationViewModel(
            IPdfEditorService pdfEditorService,
            IDialogService dialogService,
            IUndoRedoService undoRedoService,
            IThumbnailGeneratorService thumbnailService)
        {
            _pdfEditorService = pdfEditorService;
            _dialogService = dialogService;
            _undoRedoService = undoRedoService;
            _thumbnailService = thumbnailService;
            
            // コマンドを明示的に初期化
            SelectAllCommand = new RelayCommand(SelectAll);
            ShowHelpCommand = new RelayCommand(ShowHelp);
            
            // ページ操作コマンド（非同期）
            RotateLeftCommand = new AsyncRelayCommand(RotateLeftAsync);
            RotateRightCommand = new AsyncRelayCommand(RotateRightAsync);
            DeleteSelectedPagesCommand = new AsyncRelayCommand(DeleteSelectedPagesAsync);
            MovePageUpCommand = new AsyncRelayCommand(MovePageUpAsync);
            MovePageDownCommand = new AsyncRelayCommand(MovePageDownAsync);
            AutoCorrectAllPagesOrientationCommand = new RelayCommand(AutoCorrectAllPagesOrientation);
            
            // 新規追加コマンド
            DeselectAllCommand = new RelayCommand(DeselectAll);
            GoToPageCommand = new RelayCommand(GoToPage);
            PreviousPageCommand = new RelayCommand(PreviousPage);
            NextPageCommand = new RelayCommand(NextPage);
            FirstPageCommand = new RelayCommand(FirstPage);
            LastPageCommand = new RelayCommand(LastPage);
            
            // テスト用コマンド（デバッグ用）
            TestErrorDialogCommand = new RelayCommand(TestErrorDialog);
            TestWarningDialogCommand = new RelayCommand(TestWarningDialog);
            TestConfirmationDialogCommand = new RelayCommand(TestConfirmationDialog);
            TestInputDialogCommand = new RelayCommand(TestInputDialog);
            
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
            System.Diagnostics.Debug.WriteLine($"[PageOperationViewModel] Constructor - SelectAllCommand: {SelectAllCommand != null}");
            System.Diagnostics.Debug.WriteLine($"[PageOperationViewModel] Constructor - ShowHelpCommand: {ShowHelpCommand != null}");
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
        private async Task RotateLeftAsync()
        {
            if (_currentDocument == null || !Pages.Any(p => p.IsSelected))
            {
                return;
            }
            
            // V3.0.108: 選択状態を保存
            var selectedPageIds = Pages.Where(p => p.IsSelected)
                                      .Select(p => p.Id)
                                      .ToHashSet();
            
            var selectedPages = Pages.Where(p => p.IsSelected)
                .Select(vm => vm.Page)
                .ToList();
            
            var selectedViewModels = Pages.Where(p => p.IsSelected).ToList();
            
            var command = new RotatePagesCommand(
                selectedPages,
                270, // 左回転 = 270度（反時計回り）
                () => {
                    // V3.0.108: 同期的なRefreshを使用し、選択状態を保持
                    RefreshPageListWithSelection(selectedPageIds);
                    PagesChanged?.Invoke(this, EventArgs.Empty);
                    
                    // V3.0.088: ID再検索方式で最新インスタンス取得（古いインスタンス参照問題の解決）
                    var updatedViewModels = Pages.Where(vm => selectedPageIds.Contains(vm.Id)).ToList();
                    
                    foreach (var pageViewModel in updatedViewModels)
                    {
                        PageRotated?.Invoke(this, new PageOperationEventArgs(pageViewModel));
                    }
                }
            );
            
            _undoRedoService.Execute(command);
            
            // V3.0.108: コマンド実行後の選択復元は不要（RefreshPageListWithSelectionで処理済み）
            // RestoreSelection(selectedPageIds);
            
            StatusMessage = "選択したページを左回転しました";
        }

        /// <summary>
        /// 右回転（時計回り90度）
        /// </summary>
        private async Task RotateRightAsync()
        {
            if (_currentDocument == null || !Pages.Any(p => p.IsSelected))
            {
                return;
            }
            
            // V3.0.108: 選択状態を保存
            var selectedPageIds = Pages.Where(p => p.IsSelected)
                                      .Select(p => p.Id)
                                      .ToHashSet();
            
            var selectedPages = Pages.Where(p => p.IsSelected)
                .Select(vm => vm.Page)
                .ToList();
            
            var selectedViewModels = Pages.Where(p => p.IsSelected).ToList();
            
            var command = new RotatePagesCommand(
                selectedPages,
                90, // 右回転 = 90度（時計回り）
                () => {
                    // V3.0.108: 同期的なRefreshを使用し、選択状態を保持
                    RefreshPageListWithSelection(selectedPageIds);
                    PagesChanged?.Invoke(this, EventArgs.Empty);
                    
                    // V3.0.088: ID再検索方式で最新インスタンス取得（古いインスタンス参照問題の解決）
                    var updatedViewModels = Pages.Where(vm => selectedPageIds.Contains(vm.Id)).ToList();
                    
                    foreach (var pageViewModel in updatedViewModels)
                    {
                        PageRotated?.Invoke(this, new PageOperationEventArgs(pageViewModel));
                    }
                }
            );
            
            _undoRedoService.Execute(command);
            
            // V3.0.108: コマンド実行後の選択復元は不要（RefreshPageListWithSelectionで処理済み）
            // RestoreSelection(selectedPageIds);
            
            StatusMessage = "選択したページを右回転しました";
        }

        /// <summary>
        /// 選択ページ削除
        /// </summary>
        private async Task DeleteSelectedPagesAsync()
        {
            if (_currentDocument == null || !Pages.Any(p => p.IsSelected))
            {
                return;
            }
            if (_currentDocument == null) return;

            var selectedPages = Pages.Where(p => p.IsSelected)
                .Select(vm => vm.Page)  // PdfPageオブジェクト取得
                .ToList();

            // DeletePagesCommandを作成して実行
            var command = new DeletePagesCommand(
                _currentDocument,
                selectedPages,
                () => {
                    // UIの更新
                    RefreshPageList();
                    PagesChanged?.Invoke(this, EventArgs.Empty);
                }
            );
            
            try
            {
                _undoRedoService.Execute(command);
                
                // UIの更新
                UpdatePageNumbers();
                StatusMessage = $"{selectedPages.Count} ページを削除しました";
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
        private async Task MovePageUpAsync()
        {
            System.Diagnostics.Debug.WriteLine("[MovePageUpAsync] メソッドが呼び出されました！");
            
            if (_currentDocument == null || Pages.Count <= 1) 
            {
                return;
            }
            
            // 🆕 V3.0.117: 全ての選択ページを取得（インデックス順）
            var selectedPages = Pages.Where(p => p.IsSelected)
                                     .OrderBy(p => Pages.IndexOf(p))
                                     .ToList();

            if (!selectedPages.Any())
            {
                return;
            }

            // 🆕 V3.0.117: 選択状態を保存（V3.0.115パターン）
            var selectedPageIds = selectedPages.Select(p => p.Id).ToHashSet();

            // 🆕 V3.0.117: 各ページの移動先を計算
            var pageMoves = new List<(PdfPage page, int newPosition)>();
            for (int i = 0; i < selectedPages.Count; i++)
            {
                var page = selectedPages[i];
                int currentIndex = Pages.IndexOf(page);

                // 先頭ページは移動できない
                if (currentIndex == 0)
                    continue;

                int newPosition = currentIndex - 1;

                // 🎯 V3.0.123: 相対位置保持ロジック削除
                // 全ての選択ページを移動対象に追加（MovePagesCommandで処理）

                pageMoves.Add((page.Page, newPosition));
            }

            // 移動するページがない場合は終了
            if (!pageMoves.Any())
            {
                StatusMessage = "これ以上上に移動できません";
                await AppendDebugLogAsync("[MovePageUp] Cannot move up - already at top");
                return;
            }

            // 🆕 V3.0.117: 複数ページ用コンストラクタ使用
            var command = new MovePagesCommand(
                _currentDocument,
                pageMoves,
                () => {
                    // V3.0.115: 選択状態を保持してリフレッシュ
                    RefreshPageListWithSelection(selectedPageIds);
                    PagesChanged?.Invoke(this, EventArgs.Empty);
                }
            );
            
            _undoRedoService.Execute(command);
            StatusMessage = selectedPages.Count == 1 
                ? $"ページ {selectedPages[0].PageNumber} を上に移動しました"
                : $"{selectedPages.Count}ページを上に移動しました";
            
            await AppendDebugLogAsync($"[MovePageUp] Moved {selectedPages.Count} page(s) up");
        }

        /// <summary>
        /// ページを下に移動
        /// </summary>
        private async Task MovePageDownAsync()
        {
            System.Diagnostics.Debug.WriteLine("[MovePageDownAsync] メソッドが呼び出されました！");
            
            if (_currentDocument == null || Pages.Count <= 1) 
            {
                return;
            }
            
            // 🆕 V3.0.117: 全ての選択ページを取得（インデックス降順）
            var selectedPages = Pages.Where(p => p.IsSelected)
                                     .OrderByDescending(p => Pages.IndexOf(p))
                                     .ToList();

            if (!selectedPages.Any())
            {
                return;
            }

            // 🆕 V3.0.117: 選択状態を保存（V3.0.115パターン）
            var selectedPageIds = selectedPages.Select(p => p.Id).ToHashSet();

            // 🆕 V3.0.117: 各ページの移動先を計算（下から処理）
            var pageMoves = new List<(PdfPage page, int newPosition)>();
            for (int i = 0; i < selectedPages.Count; i++)
            {
                var page = selectedPages[i];
                int currentIndex = Pages.IndexOf(page);

                // 末尾ページは移動できない
                if (currentIndex >= Pages.Count - 1)
                    continue;

                int newPosition = currentIndex + 1;

                // 🎯 V3.0.123: 相対位置保持ロジック削除
                // 全ての選択ページを移動対象に追加（MovePagesCommandで処理）

                pageMoves.Add((page.Page, newPosition));
            }

            // 移動するページがない場合は終了
            if (!pageMoves.Any())
            {
                StatusMessage = "これ以上下に移動できません";
                await AppendDebugLogAsync("[MovePageDown] Cannot move down - already at bottom");
                return;
            }

            // 🆕 V3.0.117: 複数ページ用コンストラクタ使用
            var command = new MovePagesCommand(
                _currentDocument,
                pageMoves,
                () => {
                    // V3.0.115: 選択状態を保持してリフレッシュ
                    RefreshPageListWithSelection(selectedPageIds);
                    PagesChanged?.Invoke(this, EventArgs.Empty);
                }
            );
            
            _undoRedoService.Execute(command);
            StatusMessage = selectedPages.Count == 1 
                ? $"ページ {selectedPages[0].PageNumber} を下に移動しました"
                : $"{selectedPages.Count}ページを下に移動しました";
            
            await AppendDebugLogAsync($"[MovePageDown] Moved {selectedPages.Count} page(s) down");
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
            SelectedPagesCount = selectedCount;
            IsAllPagesSelected = Pages.Count > 0 && selectedCount == Pages.Count;
            
            System.Diagnostics.Debug.WriteLine($"[UpdateSelectionState] SelectedCount: {selectedCount}, HasSelectedPages: {HasSelectedPages}, IsAllPagesSelected: {IsAllPagesSelected}");

            // 🎯 V3.0.122: 複数選択時も上下移動ボタン有効化
            // V3.0.117でMovePageUpAsync/Downは既に複数対応済み
            if (selectedCount >= 1)
            {
                var selectedPages = Pages.Where(p => p.IsSelected).ToList();

                // 最小インデックスが0より大きければ上移動可能
                var minIndex = selectedPages.Min(p => Pages.IndexOf(p));
                CanMoveUp = minIndex > 0;

                // 最大インデックスが末尾より小さければ下移動可能
                var maxIndex = selectedPages.Max(p => Pages.IndexOf(p));
                CanMoveDown = maxIndex < Pages.Count - 1;
                
                System.Diagnostics.Debug.WriteLine($"[UpdateSelectionState] SelectedCount: {selectedCount}, MinIndex: {minIndex}, MaxIndex: {maxIndex}, CanMoveUp: {CanMoveUp}, CanMoveDown: {CanMoveDown}");
            }
            else
            {
                CanMoveUp = false;
                CanMoveDown = false;
                System.Diagnostics.Debug.WriteLine("[UpdateSelectionState] No selection - CanMoveUp/Down = false");
            }

            // Force command state refresh
            MovePageUpCommand?.NotifyCanExecuteChanged();
            MovePageDownCommand?.NotifyCanExecuteChanged();
            
            // プロパティ変更通知でコマンドの状態も更新される
            OnPropertyChanged(nameof(CanMoveUp));
            OnPropertyChanged(nameof(CanMoveDown));
            OnPropertyChanged(nameof(HasSelectedPages));
            OnPropertyChanged(nameof(SelectedPagesCount));
            OnPropertyChanged(nameof(IsAllPagesSelected));
            // ✅ V3.0.132: 選択数テキストの更新を通知（ListBox仮想化に依存しない正確な表示）
            OnPropertyChanged(nameof(SelectedPagesCountText));
        }

        // 選択状態を復元するメソッド
        private void RestoreSelection(HashSet<Guid> selectedPageIds)
        {
            if (selectedPageIds == null || selectedPageIds.Count == 0)
                return;

            foreach (var pageVm in Pages)
            {
                pageVm.IsSelected = selectedPageIds.Contains(pageVm.Id);
            }

            UpdateSelectionState();

            DebugLogger.Log($"[RestoreSelection] 選択状態を復元: {selectedPageIds.Count}ページ");
        }

        // Public methods for external coordination
        public void SetCurrentDocument(PdfDocument? document)
        {
            _currentDocument = document;
            UpdateSelectionState();
        }

        /// <summary>
        /// V3.0.115: View選択状態同期アクションを設定
        /// MainWindow.xaml.csのSyncSelectionFromViewModelを呼び出すためのコールバック
        /// </summary>
        /// <summary>
        /// V3.0.115: View選択状態同期アクションを設定
        /// MainWindow.xaml.csのSyncSelectionFromViewModelを呼び出すためのコールバック
        /// </summary>
        public void SetSyncSelectionAction(Action syncAction, Action disableEvents, Action enableEvents)
        {
            _syncSelectionToView = syncAction;
            _disableSelectionEvents = disableEvents;
            _enableSelectionEvents = enableEvents;
            DebugLogger.Log("[PageOperationViewModel] SyncSelectionAction registered with event control");
        }
        
        /// <summary>
        /// ドキュメントからページリストを再読み込み（Undo/Redo後に使用）
        /// V3.0.073最適化: ViewModelの再利用を最大化してパフォーマンス向上
        /// </summary>
        private async void RefreshPageList()
        {
            if (_currentDocument == null)
            {
                Pages.Clear();
                return;
            }
            
            DebugLogger.Log($"[RefreshPageList] 最適化版開始: 既存VM数={Pages.Count}, 新規ページ数={_currentDocument.Pages.Count}");
            
            // 既存のPageViewModelをIDでマッピング
            var existingPageVms = Pages.ToDictionary(vm => vm.Id);
            
            // 新しいページリストを構築
            var newPages = new ObservableCollection<V3PageViewModel>();
            var tasksToRun = new List<Task>();
            
            foreach (var page in _currentDocument.Pages)
            {
                V3PageViewModel pageVm;
                
                // 既存のViewModelがあれば再利用（サムネイル保持）
                if (existingPageVms.TryGetValue(page.Id, out var existingVm))
                {
                    pageVm = existingVm;
                    DebugLogger.Log($"[RefreshPageList] 既存VM再利用: PageId={page.Id}, 現在Rotation={pageVm.Rotation}, 新Rotation={page.Rotation}");
                    
                    // UpdateFromModelAsyncで効率的に更新
                    var updateTask = pageVm.UpdateFromModelAsync(page);
                    tasksToRun.Add(updateTask);
                }
                else
                {
                    // 新規ページの場合のみ新しいViewModelを作成
                    pageVm = new V3PageViewModel(page, _thumbnailService);
                    DebugLogger.Log($"[RefreshPageList] 新規VM作成: PageId={page.Id}, Rotation={page.Rotation}");
                    
                    // 回転状態をViewModelに同期（重要：これがないと回転後の削除→Undoでサムネイルが表示されない）
                    pageVm.UpdateRotationSync();
                    
                    // 最適化されたサムネイル生成（回転考慮済み）
                    var loadTask = pageVm.LoadThumbnailWithRotationAsync();
                    tasksToRun.Add(loadTask);
                }
                
                newPages.Add(pageVm);
            }
            
            // バッチ処理で非同期タスクを効率的に実行
            if (tasksToRun.Count > 0)
            {
                DebugLogger.Log($"[RefreshPageList] {tasksToRun.Count}個の非同期タスクを実行中...");
                await Task.WhenAll(tasksToRun);
                DebugLogger.Log($"[RefreshPageList] 非同期タスク完了");
            }
            
            // Pagesコレクションを更新
            Pages.Clear();
            foreach (var pageVm in newPages)
            {
                Pages.Add(pageVm);
            }
            
            UpdatePageNumbers();
            UpdateSelectionState();
            
            DebugLogger.Log($"[RefreshPageList] 最適化版完了: 最終VM数={Pages.Count}");
        }

        /// <summary>
        /// V3.0.108: 選択状態を保持しながらページリストをリフレッシュする同期メソッド
        /// async void RefreshPageList()の非同期問題を回避するための実装
        /// </summary>
        private void RefreshPageListWithSelection(HashSet<Guid> selectedIds)
        {
            if (_currentDocument == null)
            {
                Pages.Clear();
                return;
            }
            
            DebugLogger.Log($"[RefreshPageListWithSelection] 開始: 選択ID数={selectedIds?.Count ?? 0}, 既存VM数={Pages.Count}, 新規ページ数={_currentDocument.Pages.Count}");
            
            // V3.0.115: Pages.Clear()によるSelectionChangedイベント発火を防ぐため、イベントを一時無効化
            _disableSelectionEvents?.Invoke();
            
            // 既存のPageViewModelをIDでマッピング
            var existingPageVms = Pages.ToDictionary(vm => vm.Id);
            
            // 新しいページリストを構築
            var newPages = new ObservableCollection<V3PageViewModel>();
            var tasksToRun = new List<Task>();
            
            foreach (var page in _currentDocument.Pages)
            {
                V3PageViewModel pageVm;
                
                // 既存のViewModelがあれば再利用（サムネイル保持）
                if (existingPageVms.TryGetValue(page.Id, out var existingVm))
                {
                    pageVm = existingVm;
                    DebugLogger.Log($"[RefreshPageListWithSelection] 既存VM再利用: PageId={page.Id}, 現在Rotation={pageVm.Rotation}, 新Rotation={page.Rotation}");
                    
                    // 回転状態を同期的に更新
                    pageVm.UpdateRotationSync();
                }
                else
                {
                    // 新規ページの場合のみ新しいViewModelを作成
                    pageVm = new V3PageViewModel(page, _thumbnailService);
                    DebugLogger.Log($"[RefreshPageListWithSelection] 新規VM作成: PageId={page.Id}, Rotation={page.Rotation}");
                    
                    // 回転状態をViewModelに同期
                    pageVm.UpdateRotationSync();
                }
                
                // 選択状態を保持/復元
                if (selectedIds != null && selectedIds.Contains(pageVm.Id))
                {
                    pageVm.IsSelected = true;
                    DebugLogger.Log($"[RefreshPageListWithSelection] 選択状態復元: PageId={pageVm.Id}");
                }
                
                newPages.Add(pageVm);
            }
            
            // Pagesコレクションを更新
            Pages.Clear();
            foreach (var pageVm in newPages)
            {
                Pages.Add(pageVm);
            }
            
            UpdatePageNumbers();
            UpdateSelectionState();
            
            // ✅ V3.0.131: V3.0.115〜V3.0.129の実証済み方式に復帰
            // 選択同期は即座に実行（選択が外れたように見える問題を解決）
            // イベント再有効化のみを遅延実行（遅延SelectionChangedイベントを防止）
            _syncSelectionToView?.Invoke();
            DebugLogger.Log("[RefreshPageListWithSelection] ListBox選択同期完了（即座実行）");

            // イベント再有効化のみをDispatcher遅延実行
            // WPFのDispatcher経由で次のUIサイクルまで待機し、遅延SelectionChangedを防ぐ
            System.Windows.Application.Current?.Dispatcher.InvokeAsync(() =>
            {
                _enableSelectionEvents?.Invoke();
                DebugLogger.Log("[RefreshPageListWithSelection] イベント再有効化完了（Dispatcher経由）");
            }, System.Windows.Threading.DispatcherPriority.Loaded);
            
            DebugLogger.Log($"[RefreshPageListWithSelection] 完了: 最終VM数={Pages.Count}, 選択数={Pages.Count(p => p.IsSelected)}");
            
            // サムネイルの更新は非同期でバックグラウンド実行（UIブロックを回避）
            Task.Run(async () => {
                DebugLogger.Log($"[RefreshPageListWithSelection] サムネイル非同期更新開始");
                foreach (var pageVm in newPages)
                {
                    await pageVm.LoadThumbnailWithRotationAsync();
                }
                DebugLogger.Log($"[RefreshPageListWithSelection] サムネイル非同期更新完了");
            });
        }

        public void NotifyPageSelectionChanged()
        {
            System.Diagnostics.Debug.WriteLine("[NotifyPageSelectionChanged] Called");
            UpdateSelectionState();
        }

        // Events for coordination with other ViewModels
        public event EventHandler? PagesChanged;
        
        // テスト用メソッド（デバッグ用）
        private void TestErrorDialog()
        {
            // エラーダイアログ表示を削除 - V3.0.081
            // _dialogService.ShowError("これはエラーダイアログのテストです。エラーアイコンが表示されることを確認してください。", "エラーテスト");
        }
        
        private void TestWarningDialog()
        {
            // エラーダイアログ表示を削除 - V3.0.081
            // _dialogService.ShowWarning("これは警告ダイアログのテストです。警告アイコンが表示されることを確認してください。", "警告テスト");
        }
        
        private void TestConfirmationDialog()
        {
            var result = _dialogService.ShowConfirmation("これは確認ダイアログのテストです。Yes/Noボタンが表示されることを確認してください。", "確認テスト");
            // エラーダイアログ表示を削除 - V3.0.081
            // _dialogService.ShowInformation($"選択結果: {(result ? "Yes" : "No")}", "結果");
        }
        
        private void TestInputDialog()
        {
            var input = _dialogService.ShowInputDialog("これは入力ダイアログのテストです。テキストを入力してください。", "入力テスト", "デフォルト値");
            if (input != null)
            {
                // エラーダイアログ表示を削除 - V3.0.081
                // _dialogService.ShowInformation($"入力された値: {input}", "結果");
            }
            else
            {
                // エラーダイアログ表示を削除 - V3.0.081
                // _dialogService.ShowInformation("キャンセルされました", "結果");
            }
        }
        
        // 新規追加メソッド
        private void DeselectAll()
        {
            if (Pages == null || Pages.Count == 0) return;

            System.Diagnostics.Debug.WriteLine("[DeselectAll] 選択解除開始");

            foreach (var page in Pages)
            {
                page.IsSelected = false;
            }

            UpdateSelectionState();
            StatusMessage = "全ての選択を解除しました";
            
            // 選択状態変更を通知
            NotifyPageSelectionChanged();
            
            // ポップアップは表示しない（ステータスメッセージのみ）
        }
        
        private void GoToPage()
        {
            if (Pages == null || Pages.Count == 0) return;
            
            var input = _dialogService.ShowInputDialog(
                $"ページ番号を入力してください (1-{Pages.Count}):", 
                "ページへ移動", 
                "1");
                
            if (int.TryParse(input, out int pageNumber))
            {
                if (pageNumber >= 1 && pageNumber <= Pages.Count)
                {
                    // 全ての選択を解除
                    foreach (var page in Pages)
                    {
                        page.IsSelected = false;
                    }
                    
                    // 指定ページを選択
                    Pages[pageNumber - 1].IsSelected = true;
                    UpdateSelectionState();
                    StatusMessage = $"ページ {pageNumber} に移動しました";
                }
                else
                {
                    // エラーダイアログ表示を削除 - V3.0.081
                    // _dialogService.ShowError($"無効なページ番号です。1-{Pages.Count}の範囲で入力してください。");
                    StatusMessage = $"無効なページ番号です。1-{Pages.Count}の範囲で入力してください。";
                }
            }
        }
        
        private void PreviousPage()
        {
            if (Pages == null || Pages.Count == 0) return;

            // ✅ V3.0.131: 単一選択のみを対象とする（複数選択時のFirstOrDefault問題を回避）
            var selectedPages = Pages.Where(p => p.IsSelected).ToList();
            if (selectedPages.Count != 1)
            {
                // 複数選択または未選択時はキーボードナビゲーション無効
                return;
            }

            var selectedPage = selectedPages[0];
            var currentIndex = Pages.IndexOf(selectedPage);
            if (currentIndex > 0)
            {
                selectedPage.IsSelected = false;
                Pages[currentIndex - 1].IsSelected = true;
                UpdateSelectionState();
                StatusMessage = $"ページ {currentIndex} に移動しました";
            }
        }
        
        private void NextPage()
        {
            if (Pages == null || Pages.Count == 0) return;

            // ✅ V3.0.131: 単一選択のみを対象とする（複数選択時のFirstOrDefault問題を回避）
            var selectedPages = Pages.Where(p => p.IsSelected).ToList();
            if (selectedPages.Count != 1)
            {
                // 複数選択または未選択時はキーボードナビゲーション無効
                return;
            }

            var selectedPage = selectedPages[0];
            var currentIndex = Pages.IndexOf(selectedPage);
            if (currentIndex < Pages.Count - 1)
            {
                selectedPage.IsSelected = false;
                Pages[currentIndex + 1].IsSelected = true;
                UpdateSelectionState();
                StatusMessage = $"ページ {currentIndex + 2} に移動しました";
            }
        }
        
        private void FirstPage()
        {
            if (Pages == null || Pages.Count == 0) return;
            
            foreach (var page in Pages)
            {
                page.IsSelected = false;
            }
            
            Pages[0].IsSelected = true;
            UpdateSelectionState();
            StatusMessage = "最初のページに移動しました";
        }
        
        private void LastPage()
        {
            if (Pages == null || Pages.Count == 0) return;
            
            foreach (var page in Pages)
            {
                page.IsSelected = false;
            }
            
            Pages[Pages.Count - 1].IsSelected = true;
            UpdateSelectionState();
            StatusMessage = "最後のページに移動しました";
        }
        
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