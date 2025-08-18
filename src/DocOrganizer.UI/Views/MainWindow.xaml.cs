using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using DocOrganizer.UI.ViewModels;
using DocOrganizer.UI.ViewModels.V3;

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
            System.Diagnostics.Debug.WriteLine($"[MainWindow] DataContext type: {DataContext?.GetType().Name}");

            // Force command refresh
            CommandManager.InvalidateRequerySuggested();
            
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
                
                foreach (var item in toolbar.Items)
                {
                    if (item is Button button)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MainWindow] Button: {button.Name}, Command: {button.Command?.GetType().Name}");
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
                    System.Diagnostics.Debug.WriteLine($"[PageListBox_SelectionChanged] ListBox found, SelectedItem: {listBox.SelectedItem?.GetType().Name}");
                    
                    if (listBox.SelectedItem is V3PageViewModel selectedPage && V3ViewModel != null)
                    {
                        _logger?.LogInformation($"Selected page: {selectedPage.PageNumber}");
                        
                        // 🎯 V3対応: MainCompositeViewModelのSelectedPageを更新
                        V3ViewModel.SelectedPage = selectedPage;
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

        #region Helper Methods

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