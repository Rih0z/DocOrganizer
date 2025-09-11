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

        public MainWindow(ILogger<MainWindow>? logger = null)
        {
            InitializeComponent();
            _logger = logger;

            _logger?.LogInformation("MainWindow initialized");
            
            this.Loaded += MainWindow_Loaded;
            
            // 🚨 緊急F1パッチ - 確実にF1キーを動作させる
            System.Diagnostics.Debug.WriteLine("[F1_DEBUG] KeyDownイベントハンドラー登録開始");
            this.KeyDown += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"[F1_DEBUG] KeyDown発火: Key={e.Key}");
                if (e.Key == Key.F1)
                {
                    System.Diagnostics.Debug.WriteLine("[F1_EMERGENCY] ★★★ F1キー検出 ★★★");
                    DebugLogger.Log("[F1_EMERGENCY] F1キー検出 - 緊急パッチ実行");
                    
                    var vm = DataContext as MainCompositeViewModel;
                    if (vm?.PageOperation?.ShowHelpCommand != null)
                    {
                        vm.PageOperation.ShowHelpCommand.Execute(null);
                        System.Diagnostics.Debug.WriteLine("[F1_EMERGENCY] ShowHelpCommand実行完了");
                        DebugLogger.Log("[F1_EMERGENCY] ShowHelpCommand実行完了");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[F1_EMERGENCY] ShowHelpCommand取得失敗");
                        DebugLogger.Log("[F1_EMERGENCY] ShowHelpCommand取得失敗");
                    }
                    e.Handled = true;
                }
            };
            System.Diagnostics.Debug.WriteLine("[F1_DEBUG] KeyDownイベントハンドラー登録完了");
            
            // キーボードショートカット対応 - 明示的にイベント登録
            this.PreviewKeyDown += Window_PreviewKeyDown;
            System.Diagnostics.Debug.WriteLine("[MainWindow] PreviewKeyDown event handler registered in constructor");
            
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
            System.Diagnostics.Debug.WriteLine("=== F1キーバインディング完全デバッグ開始 ===");
            
            // DataContext存在確認
            if (DataContext == null)
            {
                System.Diagnostics.Debug.WriteLine("[CRITICAL] DataContext is NULL!");
                return;
            }
            
            if (!(DataContext is MainCompositeViewModel vm))
            {
                System.Diagnostics.Debug.WriteLine($"[CRITICAL] DataContext型が不正: {DataContext.GetType().Name}");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"[F1_FIX] DataContext確認OK: {vm.GetType().Name}");
            
            if (vm.PageOperation == null)
            {
                System.Diagnostics.Debug.WriteLine("[CRITICAL] PageOperation is NULL!");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"[F1_FIX] PageOperation確認OK: {vm.PageOperation.GetType().Name}");
            
            if (vm.PageOperation.ShowHelpCommand == null)
            {
                System.Diagnostics.Debug.WriteLine("[CRITICAL] ShowHelpCommand is NULL!");
                return;
            }
            
            System.Diagnostics.Debug.WriteLine($"[F1_FIX] ShowHelpCommand確認OK: {vm.PageOperation.ShowHelpCommand}");
            
            // 全てのF1関連バインディングを完全クリア
            var allF1Bindings = this.InputBindings
                .OfType<KeyBinding>()
                .Where(kb => kb.Key == Key.F1)
                .ToList();
            
            foreach (var binding in allF1Bindings)
            {
                this.InputBindings.Remove(binding);
                System.Diagnostics.Debug.WriteLine("[F1_FIX] 既存F1バインディング削除");
            }
            
            // 確実なF1動的バインディング追加
            var f1Binding = new KeyBinding
            {
                Key = Key.F1,
                Modifiers = ModifierKeys.None,
                Command = vm.PageOperation.ShowHelpCommand
            };
            
            this.InputBindings.Add(f1Binding);
            System.Diagnostics.Debug.WriteLine($"[F1_FIX] F1バインディング追加完了 - Command: {f1Binding.Command}");
            
            // Ctrl+A も同様に動的バインディング
            var ctrlABinding = new KeyBinding
            {
                Key = Key.A,
                Modifiers = ModifierKeys.Control,
                Command = vm.PageOperation.SelectAllCommand
            };
            this.InputBindings.Add(ctrlABinding);
            System.Diagnostics.Debug.WriteLine("[F1_FIX] Ctrl+A バインディング追加完了");
            
            // PreviewKeyDownからF1処理を除去
            this.PreviewKeyDown -= Window_PreviewKeyDown;
            this.PreviewKeyDown += (s, evt) =>
            {
                if (evt.Key == Key.F1)
                {
                    System.Diagnostics.Debug.WriteLine("[F1_FIX] PreviewKeyDownでF1検出 - InputBindingに委譲");
                    return; // InputBindingに処理を委譲
                }
                Window_PreviewKeyDown(s, evt); // その他のキー処理
            };
            
            System.Diagnostics.Debug.WriteLine("=== F1キーバインディング完全デバッグ終了 ===");
            
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
            
            if (PageListBox != null)
            {
                System.Diagnostics.Debug.WriteLine("[MainWindow] PageListBox found and configured");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[MainWindow] PageListBox not found");
            }
            
            // ツールバーのボタンのコマンドバインディングを確認
            var toolbar = this.FindName("MainToolBar") as ToolBar;
            if (toolbar != null)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindow] ToolBar DataContext: {toolbar.DataContext?.GetType().Name}");
                
                int buttonIndex = 0;
                foreach (var item in toolbar.Items)
                {
                    if (item is Button button)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MainWindow] Button[{buttonIndex}]: ToolTip='{button.ToolTip}', Command={button.Command?.GetType().Name ?? "NULL"}, CommandParameter={button.CommandParameter ?? "NULL"}");
                        
                        // 上下移動ボタンを特定して詳細チェック
                        if (button.ToolTip?.ToString() == "上に移動" || button.ToolTip?.ToString() == "下に移動")
                        {
                            System.Diagnostics.Debug.WriteLine($"  [詳細] DataContext={button.DataContext?.GetType().Name ?? "NULL"}");
                            System.Diagnostics.Debug.WriteLine($"  [詳細] IsEnabled={button.IsEnabled}");
                            
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
                System.Diagnostics.Debug.WriteLine("[WARNING] PreviewArea_Drop: Already processing, skipping duplicate event");
                e.Handled = true;
                return;
            }
            
            _isProcessingDrop = true;
            System.Diagnostics.Debug.WriteLine("[DEBUG] PreviewArea_Drop: Started processing");
            
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
                System.Diagnostics.Debug.WriteLine($"[CRITICAL ERROR] PreviewArea_Drop failed: {ex.Message}");
            }
            finally
            {
                _isProcessingDrop = false;
                System.Diagnostics.Debug.WriteLine("[DEBUG] PreviewArea_Drop: Processing completed, flag reset");
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
                System.Diagnostics.Debug.WriteLine("[WARNING] Window_Drop: Already processing, skipping duplicate event");
                e.Handled = true;
                return;
            }
            
            _isProcessingDrop = true;
            System.Diagnostics.Debug.WriteLine("[DEBUG] Window_Drop: Started processing");
            
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
                System.Diagnostics.Debug.WriteLine($"[CRITICAL ERROR] Window_Drop failed: {ex.Message}");
            }
            finally
            {
                _isProcessingDrop = false;
                System.Diagnostics.Debug.WriteLine("[DEBUG] Window_Drop: Processing completed, flag reset");
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
                    System.Diagnostics.Debug.WriteLine($"[PageListBox_SelectionChanged] ListBox found, SelectedItems.Count: {listBox.SelectedItems.Count}");
                    
                    // 🔧 複数選択対応: ListBoxの選択状態をViewModelに同期
                    if (V3ViewModel?.PageOperation?.Pages != null)
                    {
                        // 全ページの選択状態を更新
                        foreach (V3PageViewModel page in V3ViewModel.PageOperation.Pages)
                        {
                            bool shouldBeSelected = listBox.SelectedItems.Contains(page);
                            if (page.IsSelected != shouldBeSelected)
                            {
                                page.IsSelected = shouldBeSelected;
                                System.Diagnostics.Debug.WriteLine($"[複数選択] Page {page.PageNumber}: IsSelected = {shouldBeSelected}");
                            }
                        }
                        
                        // 選択状態の更新を通知
                        V3ViewModel.PageOperation.NotifyPageSelectionChanged();
                        
                        System.Diagnostics.Debug.WriteLine($"[複数選択] 選択ページ数: {listBox.SelectedItems.Count}");
                    }
                    
                    // 単一選択時のプレビュー更新（最初の選択ページ）
                    if (listBox.SelectedItem is V3PageViewModel selectedPage && V3ViewModel != null)
                    {
                        _logger?.LogInformation($"Selected page: {selectedPage.PageNumber}");
                        
                        // 🎯 V3対応: MainCompositeViewModel.SelectedPageを更新
                        V3ViewModel.SelectedPage = selectedPage;
                        
                        // 🚨 新規デバッグ: 詳細ログ出力
                        System.Diagnostics.Debug.WriteLine($"[右側プレビューデバッグ] SelectedPage設定完了: PageNumber={selectedPage.PageNumber}");
                        System.Diagnostics.Debug.WriteLine($"[右側プレビューデバッグ] V3ViewModel.PreviewManagement={V3ViewModel.PreviewManagement != null}");
                        System.Diagnostics.Debug.WriteLine($"[右側プレビューデバッグ] V3ViewModel.PreviewManagement.CurrentPageImage={V3ViewModel.PreviewManagement?.CurrentPageImage != null}");
                        
                        // 🚨 新規デバッグ: SourceImagePath確認
                        if (selectedPage.Page?.SourceImagePath != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[右側プレビューデバッグ] SourceImagePath='{selectedPage.Page.SourceImagePath}'");
                            System.Diagnostics.Debug.WriteLine($"[右側プレビューデバッグ] ファイル存在確認={System.IO.File.Exists(selectedPage.Page.SourceImagePath)}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("[右側プレビューデバッグ] SourceImagePathがNULL");
                        }
                        
                        // 選択状態を明示的に設定
                        foreach (var page in V3ViewModel.Pages)
                        {
                            page.IsSelected = (page == selectedPage);
                        }
                        
                        // ページ選択状態を更新（上下移動ボタンの有効化）
                        // 🔧 根本修正: Pages.Clear()を削除し、通知のみ実行
                        // Clear/Addは不要 - ObservableCollectionの破壊的操作がサムネイル消失の原因
                        if (V3ViewModel.PageOperation != null)
                        {
                            V3ViewModel.PageOperation.NotifyPageSelectionChanged();
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[PageListBox_SelectionChanged] SelectedItem is not V3PageViewModel: {listBox.SelectedItem?.GetType().Name}");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[PageListBox_SelectionChanged] Sender is not ListBox");
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
            System.Diagnostics.Debug.WriteLine($"[Window_PreviewKeyDown] ★★★ EVENT FIRED ★★★ Key pressed: {e.Key}, Modifiers: {Keyboard.Modifiers}");
            
            if (V3ViewModel == null)
            {
                System.Diagnostics.Debug.WriteLine("[Window_PreviewKeyDown] V3ViewModel is null!");
                return;
            }
            
            // F1キーの処理はInputBindingに任せる（二重実装を避けるため削除）
            // InputBinding: <KeyBinding Key="F1" Command="{Binding PageOperation.ShowHelpCommand}"/>
            
            // Ctrl+A - 全選択
            if (e.Key == Key.A && Keyboard.Modifiers == ModifierKeys.Control)
            {
                System.Diagnostics.Debug.WriteLine("[MainWindow] Ctrl+A pressed");
                System.Diagnostics.Debug.WriteLine($"[MainWindow] PageOperation is null: {V3ViewModel.PageOperation == null}");
                
                if (V3ViewModel.PageOperation != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainWindow] SelectAllCommand is null: {V3ViewModel.PageOperation.SelectAllCommand == null}");
                }
                
                if (V3ViewModel.PageOperation?.SelectAllCommand?.CanExecute(null) == true)
                {
                    System.Diagnostics.Debug.WriteLine("[MainWindow] Executing SelectAllCommand");
                    V3ViewModel.PageOperation.SelectAllCommand.Execute(null);
                    
                    // ViewModelの選択状態をListBoxに同期
                    SyncSelectionFromViewModel();
                    
                    e.Handled = true;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[MainWindow] SelectAllCommand cannot execute or is null");
                }
            }
        }
        
        // デバッグ: DataContextとコマンドの確認
        private void DebugDataContext()
        {
            System.Diagnostics.Debug.WriteLine($"[DebugDataContext] DataContext: {DataContext?.GetType().Name}");
            if (DataContext is MainCompositeViewModel vm)
            {
                System.Diagnostics.Debug.WriteLine($"[DebugDataContext] PageOperation: {vm.PageOperation}");
                System.Diagnostics.Debug.WriteLine($"[DebugDataContext] SelectAllCommand: {vm.PageOperation?.SelectAllCommand}");
                System.Diagnostics.Debug.WriteLine($"[DebugDataContext] ShowHelpCommand: {vm.ShowHelpCommand}");
            }
        }
        
        #endregion

        #region Helper Methods
        
        // ViewModelの選択状態をListBoxに反映するヘルパーメソッド
        public void SyncSelectionFromViewModel()
        {
            if (PageListBox != null && V3ViewModel?.PageOperation?.Pages != null)
            {
                PageListBox.SelectionChanged -= PageListBox_SelectionChanged; // 一時的にイベントを無効化
                
                PageListBox.SelectedItems.Clear();
                foreach (var page in V3ViewModel.PageOperation.Pages.Where(p => p.IsSelected))
                {
                    PageListBox.SelectedItems.Add(page);
                }
                
                PageListBox.SelectionChanged += PageListBox_SelectionChanged; // イベントを再有効化
                
                System.Diagnostics.Debug.WriteLine($"[SyncSelectionFromViewModel] ListBox選択同期完了: {PageListBox.SelectedItems.Count}ページ");
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