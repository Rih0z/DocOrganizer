using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using DocOrganizer.UI.ViewModels;
using DocOrganizer.UI.ViewModels.V3;
using DocOrganizer.Core.Logging;

namespace DocOrganizer.UI.Views
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private Point _startPoint;
        private bool _isDragging;
        private readonly ILogger<MainWindow>? _logger;

        // ドラッグ&ドロップ重複処理防止フラグ
        private bool _isProcessingDrop = false;

        // ✅ V3.0.141: クリックとD&D開始を区別するための時刻記録
        private DateTime _lastMouseDownTime;
        private const int ClickThresholdMs = 200;  // 200ms以下はクリック、以上はD&D

        public MainWindow(ILogger<MainWindow>? logger = null)
        {
            InitializeComponent();
            _logger = logger;

            // バージョン情報を統一ソースから設定
            this.Title = DocOrganizer.Core.VersionInfo.DisplayVersion;

            _logger?.LogInformation("MainWindow initialized");
            
            this.Loaded += MainWindow_Loaded;
            
            // 🚨 緊急F1パッチ - 確実にF1キーを動作させる
            this.KeyDown += (s, e) =>
            {
                    if (e.Key == Key.F1)
                {
                            DebugLogger.Log("[F1_EMERGENCY] F1キー検出 - 緊急パッチ実行");
                    
                    var vm = DataContext as MainCompositeViewModel;
                    if (vm?.PageOperation?.ShowHelpCommand != null)
                    {
                        vm.PageOperation.ShowHelpCommand.Execute(null);
                                    DebugLogger.Log("[F1_EMERGENCY] ShowHelpCommand実行完了");
                    }
                    else
                    {
                                    DebugLogger.Log("[F1_EMERGENCY] ShowHelpCommand取得失敗");
                    }
                    e.Handled = true;
                }
            };
            
            // キーボードショートカット対応 - 明示的にイベント登録
            this.PreviewKeyDown += Window_PreviewKeyDown;
            
            // ウィンドウ終了時のクリーンアップ
            this.Closing += MainWindow_Closing;
        }
        
        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _logger?.LogInformation("Cleaning up HEIC cache on window close");
            // V3PageViewModel cleanup if needed
        }
        
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            
            // DataContext存在確認
            if (DataContext == null)
            {
                    return;
            }
            
            if (!(DataContext is MainCompositeViewModel vm))
            {
                return;
            }
            
            if (vm.PageOperation == null)
            {
                    return;
            }
            
            if (vm.PageOperation.ShowHelpCommand == null)
            {
                    return;
            }
            
            
            // 全てのF1関連バインディングを完全クリア
            var allF1Bindings = this.InputBindings
                .OfType<KeyBinding>()
                .Where(kb => kb.Key == Key.F1)
                .ToList();
            
            foreach (var binding in allF1Bindings)
            {
                this.InputBindings.Remove(binding);
                }
            
            // 確実なF1動的バインディング追加
            var f1Binding = new KeyBinding
            {
                Key = Key.F1,
                Modifiers = ModifierKeys.None,
                Command = vm.PageOperation.ShowHelpCommand
            };
            
            this.InputBindings.Add(f1Binding);
            
            // Ctrl+A の動的バインディングは削除（XAMLで定義済み）
            // XAMLでの定義を優先し、重複を避ける
            // <KeyBinding Key="A" Modifiers="Ctrl" Command="{Binding PageOperation.SelectAllCommand}"/>
            
            // PreviewKeyDownからF1処理を除去
            this.PreviewKeyDown -= Window_PreviewKeyDown;
            this.PreviewKeyDown += (s, evt) =>
            {
                if (evt.Key == Key.F1)
                {
                            return; // InputBindingに処理を委譲
                }
                Window_PreviewKeyDown(s, evt); // その他のキー処理
            };
            
            
            // Force command refresh
            CommandManager.InvalidateRequerySuggested();
            
            // デバッグ: コマンドバインディング確認
            DebugDataContext();
            
            // Add fallback click handler for Open button
            if (this.FindName("OpenButton") is Button openButton)
            {
                openButton.Click += (s, args) =>
                {
                    // 🎯 V3対応: MainCompositeViewModelのDocumentManagementを使用
                    if (V3ViewModel?.DocumentManagement?.OpenCommand != null && V3ViewModel.DocumentManagement.OpenCommand.CanExecute(null))
                    {
                        V3ViewModel.DocumentManagement.OpenCommand.Execute(null);
                    }
                };
            }
            
            // V3.0.115: PageOperationViewModelに選択同期アクションを登録
            if (V3ViewModel?.PageOperation != null)
            {
                V3ViewModel.PageOperation.SetSyncSelectionAction(
                    syncAction: () => SyncSelectionFromViewModel(),
                    disableEvents: () => 
                    {
                        if (PageListBox != null)
                        {
                            PageListBox.SelectionChanged -= PageListBox_SelectionChanged;
                                        }
                    },
                    enableEvents: () => 
                    {
                        if (PageListBox != null)
                        {
                            PageListBox.SelectionChanged += PageListBox_SelectionChanged;
                                        }
                    }
                );
                }
            
            if (PageListBox != null)
            {
                }
            else
            {
                }
            
            // ツールバーのボタンのコマンドバインディングを確認
            var toolbar = this.FindName("MainToolBar") as ToolBar;
            if (toolbar != null)
            {
                int buttonIndex = 0;
                foreach (var item in toolbar.Items)
                {
                    if (item is Button button)
                    {
                        // 上下移動ボタンを特定して詳細チェック
                        if (button.ToolTip?.ToString() == "上に移動" || button.ToolTip?.ToString() == "下に移動")
                        {
                            // ViewModelから直接コマンドを取得して確認
                            // V3.0.050: 手動Clickハンドラーを削除（二重実行の原因）
                            // CommandバインディングがXAMLで設定されているため、手動ハンドラーは不要
                        }
                        buttonIndex++;
                    }
                }
            }
        }

        // 🎯 V3対応: MainCompositeViewModelのみサポート
        private MainCompositeViewModel? V3ViewModel => DataContext as MainCompositeViewModel;

        #region Thumbnail List Drag & Drop

        private void ThumbnailList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // ドラッグ開始位置を記録
            _startPoint = e.GetPosition(null);
        }

        private void ThumbnailList_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            // マウスが押されていない場合は何もしない
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            // ドラッグ開始の閾値チェック
            Point currentPosition = e.GetPosition(null);
            Vector diff = _startPoint - currentPosition;

            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                // ドラッグされているListBoxItemを取得
                System.Windows.Controls.ListBox listBox = sender as System.Windows.Controls.ListBox;
                System.Windows.Controls.ListBoxItem listBoxItem = FindAncestor<System.Windows.Controls.ListBoxItem>((DependencyObject)e.OriginalSource);

                if (listBoxItem != null && listBox != null)
                {
                    // 選択されたページを取得
                    var selectedPages = listBox.SelectedItems.Cast<V3PageViewModel>().ToList();

                    if (selectedPages.Any())
                    {
                        // ドラッグデータを作成
                        System.Windows.DataObject dragData = new System.Windows.DataObject();
                        dragData.SetData("PageViewModels", selectedPages);

                        // ドラッグ操作を開始
                        _isDragging = true;
                        DragDrop.DoDragDrop(listBoxItem, dragData, System.Windows.DragDropEffects.Move);
                        _isDragging = false;
                    }
                }
            }
        }

        private async void ThumbnailList_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("PageViewModels"))
            {
                var droppedPages = e.Data.GetData("PageViewModels") as System.Collections.Generic.List<V3PageViewModel>;
                if (droppedPages != null && droppedPages.Any())
                {
                    // ドロップ位置を取得
                    System.Windows.Controls.ListBox listBox = sender as System.Windows.Controls.ListBox;
                    System.Windows.Controls.ListBoxItem targetItem = FindAncestor<System.Windows.Controls.ListBoxItem>((DependencyObject)e.OriginalSource);

                    if (targetItem != null && listBox != null)
                    {
                        var targetPage = targetItem.DataContext as V3PageViewModel;
                        if (targetPage != null && V3ViewModel?.DragDropHandler != null)
                        {
                            // 🎯 V3対応: DragDropHandlerViewModelを使用
                            await V3ViewModel.DragDropHandler.HandlePageReorderAsync(droppedPages, targetPage);
                        }
                    }
                }
            }
        }

        #endregion

        #region Preview Area Drag & Drop

        private void PreviewArea_DragEnter(object sender, DragEventArgs e)
        {
            // PDFファイルまたは画像ファイルのドラッグを検出
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                string[] items = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                bool hasSupportedItem = false;
                
                foreach (var item in items)
                {
                    if (File.Exists(item) && IsSupportedFileType(item))
                    {
                        hasSupportedItem = true;
                        break;
                    }
                    else if (Directory.Exists(item))
                    {
                        // フォルダの場合、中にサポートされるファイルがあるか確認
                        var files = Directory.GetFiles(item, "*.*", SearchOption.AllDirectories);
                        if (files.Any(f => IsSupportedFileType(f)))
                        {
                            hasSupportedItem = true;
                            break;
                        }
                    }
                }
                
                if (hasSupportedItem && V3ViewModel?.DragDropHandler != null)
                {
                    e.Effects = System.Windows.DragDropEffects.Copy;
                    V3ViewModel.DragDropHandler.ShowDragOverlay();
                }
                else
                {
                    e.Effects = System.Windows.DragDropEffects.None;
                }
            }
            else
            {
                e.Effects = System.Windows.DragDropEffects.None;
            }
            
            e.Handled = true;
        }

        private void PreviewArea_DragLeave(object sender, DragEventArgs e)
        {
            // オーバーレイを非表示
            if (V3ViewModel?.DragDropHandler != null)
            {
                V3ViewModel.DragDropHandler.HideDragOverlay();
            }
        }

        private async void PreviewArea_Drop(object sender, DragEventArgs e)
        {
            // ⭐重複処理防止チェック
            if (_isProcessingDrop)
            {
                    e.Handled = true;
                return;
            }
            
            _isProcessingDrop = true;
            
            try
            {
                // オーバーレイを非表示
                if (V3ViewModel?.DragDropHandler != null)
                {
                    V3ViewModel.DragDropHandler.HideDragOverlay();
                }

                if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
                {
                    string[] droppedItems = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                    
                    // ファイルとフォルダを展開
                    var allFiles = new System.Collections.Generic.List<string>();
                    
                    foreach (var item in droppedItems)
                    {
                        if (File.Exists(item))
                        {
                            // ファイルの場合
                            allFiles.Add(item);
                        }
                        else if (Directory.Exists(item))
                        {
                            // フォルダの場合、対応形式のファイルを再帰的に検索
                            var folderFiles = Directory.GetFiles(item, "*.*", SearchOption.AllDirectories)
                                .Where(f => IsSupportedFileType(f))
                                .ToList();
                            allFiles.AddRange(folderFiles);
                        }
                    }
                    
                    // 対応ファイル形式のフィルタリング
                    var supportedFiles = allFiles.Where(f => IsSupportedFileType(f)).ToList();
                    
                    if (supportedFiles.Any() && V3ViewModel?.DragDropHandler != null)
                    {
                        // 🎯 V3対応: DragDropHandlerViewModelでファイル処理
                        await V3ViewModel.DragDropHandler.HandleFilesDropAsync(supportedFiles);
                    }
                    else if (V3ViewModel?.StatusManagement != null)
                    {
                        V3ViewModel.StatusManagement.ShowWarning("対応していないファイル形式です");
                    }
                }
            }
            catch (Exception ex)
            {
                if (V3ViewModel?.StatusManagement != null)
                {
                    V3ViewModel.StatusManagement.ShowError($"ファイル処理エラー: {ex.Message}", ex);
                }
                }
            finally
            {
                _isProcessingDrop = false;
                }
            
            e.Handled = true;
        }

        #endregion

        #region Window Drag & Drop

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                string[] items = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                bool hasSupportedItem = false;
                
                foreach (var item in items)
                {
                    if (File.Exists(item) && IsSupportedFileType(item))
                    {
                        hasSupportedItem = true;
                        break;
                    }
                    else if (Directory.Exists(item))
                    {
                        // フォルダの場合、中にサポートされるファイルがあるか確認
                        var files = Directory.GetFiles(item, "*.*", SearchOption.AllDirectories);
                        if (files.Any(f => IsSupportedFileType(f)))
                        {
                            hasSupportedItem = true;
                            break;
                        }
                    }
                }
                
                if (hasSupportedItem && V3ViewModel?.DragDropHandler != null)
                {
                    e.Effects = System.Windows.DragDropEffects.Copy;
                    V3ViewModel.DragDropHandler.ShowDragOverlay();
                }
                else
                {
                    e.Effects = System.Windows.DragDropEffects.None;
                }
            }
            else
            {
                e.Effects = System.Windows.DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            // DragEnterと同じ処理
            Window_DragEnter(sender, e);
        }

        private void Window_DragLeave(object sender, DragEventArgs e)
        {
            // ウィンドウ外にドラッグした場合のみオーバーレイを非表示
            Point pt = e.GetPosition(this);
            if (pt.X < 0 || pt.Y < 0 || pt.X > ActualWidth || pt.Y > ActualHeight)
            {
                if (V3ViewModel?.DragDropHandler != null)
                {
                    V3ViewModel.DragDropHandler.HideDragOverlay();
                }
            }
        }

        private async void Window_Drop(object sender, DragEventArgs e)
        {
            // ⭐重複処理防止チェック
            if (_isProcessingDrop)
            {
                    e.Handled = true;
                return;
            }
            
            _isProcessingDrop = true;
            
            try
            {
                // オーバーレイを非表示
                if (V3ViewModel?.DragDropHandler != null)
                {
                    V3ViewModel.DragDropHandler.HideDragOverlay();
                }

                if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
                {
                    string[] droppedItems = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                    
                    var allFiles = new System.Collections.Generic.List<string>();
                    
                    foreach (var item in droppedItems)
                    {
                        if (File.Exists(item))
                        {
                            allFiles.Add(item);
                        }
                        else if (Directory.Exists(item))
                        {
                            var folderFiles = Directory.GetFiles(item, "*.*", SearchOption.AllDirectories)
                                .Where(f => IsSupportedFileType(f))
                                .ToList();
                            allFiles.AddRange(folderFiles);
                        }
                    }
                    
                    var supportedFiles = allFiles.Where(f => IsSupportedFileType(f)).ToList();
                    
                    if (supportedFiles.Any() && V3ViewModel?.DragDropHandler != null)
                    {
                        // 🎯 V3対応: DragDropHandlerViewModelでファイル処理
                        await V3ViewModel.DragDropHandler.HandleFilesDropAsync(supportedFiles);
                    }
                    else if (V3ViewModel?.StatusManagement != null)
                    {
                        V3ViewModel.StatusManagement.ShowWarning("対応していないファイル形式です");
                    }
                }
            }
            catch (Exception ex)
            {
                if (V3ViewModel?.StatusManagement != null)
                {
                    V3ViewModel.StatusManagement.ShowError($"ファイル処理エラー: {ex.Message}", ex);
                }
                }
            finally
            {
                _isProcessingDrop = false;
                }
            
            e.Handled = true;
        }

        #endregion

        #region ListBox Events

        private void PageListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                _logger?.LogInformation("PageListBox_SelectionChanged event fired");
                
                if (sender is ListBox listBox)
                {
        
                    // ✅ V3.0.130: Ctrl/Shiftなしの単独クリック時は他の選択を解除
                    // ✅ V3.0.141: 時間判定でクリックとD&D開始を区別（複数選択D&D対応）
                    bool isCtrlPressed = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
                    bool isShiftPressed = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

                    if (!isCtrlPressed && !isShiftPressed && listBox.SelectedItems.Count == 1)
                    {
                        // ✅ V3.0.141: 時間判定でクリックとD&D開始を区別
                        var elapsed = (DateTime.Now - _lastMouseDownTime).TotalMilliseconds;

                        if (elapsed < ClickThresholdMs)  // 200ms以下 = クリック
                        {
                            // 短時間クリック: 他の選択を解除（バグ4対策）
                            var selectedItem = listBox.SelectedItem as V3PageViewModel;
                            if (selectedItem != null && V3ViewModel?.PageOperation?.Pages != null)
                            {
                                                    foreach (var page in V3ViewModel.PageOperation.Pages)
                                {
                                    page.IsSelected = (page == selectedItem);
                                }
                            }
                        }
                        else
                        {
                            // 長押し（D&D開始）: 選択解除しない（複数選択D&D対応）
                                        }
                    }

                    // 🎯 V3.0.121: 二重バインディング防止 - 手動同期ループを完全削除
                    // TwoWayBindingが既に同期を保証しているため、手動同期は不要かつ有害

                    // ✅ 選択状態の変更を通知（ボタン有効化等のUI更新用）
                    if (V3ViewModel?.PageOperation != null)
                    {
                        V3ViewModel.PageOperation.NotifyPageSelectionChanged();
                                }
                    
                    // 単一選択時のプレビュー更新（最初の選択ページ）
                    if (listBox.SelectedItem is V3PageViewModel selectedPage && V3ViewModel != null)
                    {
                        _logger?.LogInformation($"Selected page: {selectedPage.PageNumber}");
                        
                        // 🎯 V3対応: MainCompositeViewModel.SelectedPageを更新
                        V3ViewModel.SelectedPage = selectedPage;
                        
                        // 🚨 新規デバッグ: 詳細ログ出力
                                                            

                        
                        // V3.0.102: 複数選択対応 - 単一選択の強制を削除
                        // 以下のコードは複数選択を破壊するためコメントアウト
                        // foreach (var page in V3ViewModel.Pages)
                        // {
                        //     page.IsSelected = (page == selectedPage);
                        // }
                        
                        // ページ選択状態を更新（上下移動ボタンの有効化）
                        // 🔧 根本修正: Pages.Clear()を削除し、通知のみ実行
                        // Clear/Addは不要 - ObservableCollectionの破壊的操作がサムネイル消失の原因
                        if (V3ViewModel.PageOperation != null)
                        {
                            V3ViewModel.PageOperation.NotifyPageSelectionChanged();
                        }
                    }
                }
                else
                {
                        }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in PageListBox_SelectionChanged");
            }
        }

        #endregion

        #region Event Handlers
        
        // PreviewKeyDownイベントハンドラー（ショートカットキー対応）
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            
            if (V3ViewModel == null)
            {
                    return;
            }
            
            // F1キーの処理はInputBindingに任せる（二重実装を避けるため削除）
            // InputBinding: <KeyBinding Key="F1" Command="{Binding PageOperation.ShowHelpCommand}"/>
            
            // Ctrl+A - 全選択（コメントアウト: XAMLのKeyBindingを使用）
            // PreviewKeyDownでの処理は削除し、XAMLのKeyBindingのみを使用
            // これにより、ListBoxの標準的な選択動作との競合を防ぐ
        }
        
        // デバッグ: DataContextとコマンドの確認
        private void DebugDataContext()
        {
            // デバッグログ削除済み
        }
        
        #endregion

        #region Helper Methods
        
        // ViewModelの選択状態をListBoxに反映するヘルパーメソッド
        // ViewModelの選択状態をListBoxに反映するヘルパーメソッド
        public void SyncSelectionFromViewModel()
        {
            if (PageListBox != null && V3ViewModel?.PageOperation?.Pages != null)
            {
                try
                {
                    // V3.0.115: イベント制御は呼び出し側（RefreshPageListWithSelection）で実施
                    // ここではイベント制御を行わず、純粋に選択同期のみ実行

                    // ⭐ V3.0.151: Clear()を使わず、差分更新（Pages.Clear()と同じ問題を回避）
                    // Clear()はTwoWayバインディングでViewModel側のIsSelectedをfalseにしてしまう
                    var selectedPages = V3ViewModel.PageOperation.Pages.Where(p => p.IsSelected).ToList();

                    // 不要な選択を削除
                    var toRemove = PageListBox.SelectedItems.Cast<V3PageViewModel>()
                        .Where(p => !selectedPages.Contains(p))
                        .ToList();

                    DocOrganizer.Core.Logging.DebugLogger.Log($"[SyncSelectionFromViewModel] 差分更新: 削除={toRemove.Count}, 追加予定={selectedPages.Count}");

                    foreach (var page in toRemove)
                    {
                        PageListBox.SelectedItems.Remove(page);
                    }

                    // 不足している選択を追加
                    foreach (var page in selectedPages)
                    {
                        if (!PageListBox.SelectedItems.Contains(page))
                        {
                            // ✅ V3.0.145: 仮想化対策 - 対象ページを可視化してから選択
                            // ScrollIntoView()により、可視範囲外のページを仮想化解除し、確実に選択可能にする
                            PageListBox.ScrollIntoView(page);
                            PageListBox.SelectedItems.Add(page);
                        }
                    }

                    // ✅ V3.0.145: 最初の選択ページにフォーカスを移動し、UI更新を強制
                    if (selectedPages.Any())
                    {
                        var firstSelected = selectedPages[0];

                        // 最初の選択ページを可視化（複数選択時も最初のページを基準に）
                        PageListBox.ScrollIntoView(firstSelected);

                        // ✅ レイアウト更新を強制（Dispatcher待機せず、即座にUI反映）
                        PageListBox.UpdateLayout();

                        // ⭐ V3.0.147 第3層: UpdateLayout後に選択状態を再確認・再設定（100%保証）
                        Dispatcher.Invoke(() =>
                        {
                            // 選択が反映されていない場合、自動リトライ
                            if (PageListBox.SelectedItems.Count == 0 && selectedPages.Any())
                            {
                                DebugLogger.Log("[SyncSelectionFromViewModel] 第3層: 選択未反映検出、再試行");
                                foreach (var page in selectedPages)
                                {
                                    if (!PageListBox.SelectedItems.Contains(page))
                                    {
                                        PageListBox.SelectedItems.Add(page);
                                    }
                                }
                                PageListBox.UpdateLayout();
                            }

                            DebugLogger.Log($"[SyncSelectionFromViewModel] 選択同期完了: {PageListBox.SelectedItems.Count}ページ選択済み, 先頭PageNumber={firstSelected.PageNumber}");
                        }, System.Windows.Threading.DispatcherPriority.DataBind);
                    }
                    else
                    {
                        DebugLogger.Log("[SyncSelectionFromViewModel] 選択同期完了: 選択ページなし");
                    }
                }
                catch (Exception ex)
                {
                    // ❌ 選択同期失敗時も処理を継続（致命的ではない）
                    // ScrollIntoView()は仮想化されたアイテムに対して稀に例外をスローする
                    // UpdateLayout()はレイアウトサイクル中に呼ばれると例外をスローする可能性
                    DebugLogger.Log($"[SyncSelectionFromViewModel] 例外発生: {ex.Message}");

                    // スタックトレースも記録（デバッグ用）
                    if (ex.StackTrace != null)
                    {
                        DebugLogger.Log($"[SyncSelectionFromViewModel] StackTrace: {ex.StackTrace}");
                    }

                    // 例外が発生しても処理を継続（選択同期は次の機会に再試行される）
                }
            }
        }

        // ✅ V3.0.130: 全選択時にListBoxに直接全選択を指示するヘルパーメソッド
        // ListBoxの仮想化により、ViewModelのIsSelectedだけでは可視領域のみ選択される問題を解決
        public void ForceListBoxFullSelection()
        {
            if (PageListBox != null && V3ViewModel?.PageOperation?.Pages != null)
            {
                PageListBox.SelectAll();  // ✅ ListBoxに直接全選択を指示
            }
        }

        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            do
            {
                if (current is T)
                {
                    return (T)current;
                }
                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
            }
            while (current != null);

            return null;
        }

        /// <summary>
        /// ✅ V3.0.141: PageListBoxのマウスダウン時刻を記録
        /// クリックとD&D開始を区別するための時刻記録
        /// </summary>
        private void PageListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _lastMouseDownTime = DateTime.Now;
        }

        private bool IsSupportedFileType(string filePath)
        {
            return IsPdfFile(filePath) || IsImageFile(filePath);
        }

        private bool IsPdfFile(string filePath)
        {
            return Path.GetExtension(filePath).Equals(".pdf", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsImageFile(string filePath)
        {
            var extension = Path.GetExtension(filePath);
            // HEIC形式を再有効化（ImageMagick変換対応済み）
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

        #endregion
    }
}