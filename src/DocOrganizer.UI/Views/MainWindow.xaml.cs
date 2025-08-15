using System;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using DocOrganizer.UI.ViewModels;

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
            DocOrganizer.UI.ViewModels.PageViewModel.CleanupHeicCache();
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

                    if (ViewModel?.OpenCommand != null && ViewModel.OpenCommand.CanExecute(null))
                    {
                        ViewModel.OpenCommand.Execute(null);
                    }
                };
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
                System.Diagnostics.Debug.WriteLine($"[MainWindow] ToolBar DataContext: {toolbar.DataContext?.GetType().Name}");
                
                foreach (var item in toolbar.Items)
                {
                    if (item is Button button)
                    {

                    }
                }
            }
            
            // テストボタンを追加して基本的な動作を確認
            var testButton = new Button
            {
                Content = "テスト",
                Width = 100,
                Height = 30,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(10)
            };
            testButton.Click += (s, args) =>
            {

                // Test MessageBox removed for production
            };
            
            if (this.Content is Grid mainGrid)
            {
                mainGrid.Children.Add(testButton);

            }
        }

        private MainViewModel ViewModel => (MainViewModel)DataContext;

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
                    var selectedPages = listBox.SelectedItems.Cast<PageViewModel>().ToList();

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

        private void ThumbnailList_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("PageViewModels"))
            {
                var droppedPages = e.Data.GetData("PageViewModels") as System.Collections.Generic.List<PageViewModel>;
                if (droppedPages != null && droppedPages.Any())
                {
                    // ドロップ位置を取得
                    System.Windows.Controls.ListBox listBox = sender as System.Windows.Controls.ListBox;
                    System.Windows.Controls.ListBoxItem targetItem = FindAncestor<System.Windows.Controls.ListBoxItem>((DependencyObject)e.OriginalSource);

                    if (targetItem != null && listBox != null)
                    {
                        var targetPage = targetItem.DataContext as PageViewModel;
                        if (targetPage != null)
                        {
                            ViewModel.ReorderPages(droppedPages, targetPage);
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
                
                if (hasSupportedItem)
                {
                    e.Effects = System.Windows.DragDropEffects.Copy;
                    DropOverlay.Visibility = Visibility.Visible;
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
            DropOverlay.Visibility = Visibility.Collapsed;
        }

        private async void PreviewArea_Drop(object sender, DragEventArgs e)
        {
            // ⭐重複処理防止チェック
            if (_isProcessingDrop)
            {
                File.AppendAllText("DEBUG_LOG.txt", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [WARNING] PreviewArea_Drop: Already processing, skipping duplicate event\n");
                e.Handled = true;
                return;
            }
            
            _isProcessingDrop = true;
            File.AppendAllText("DEBUG_LOG.txt", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [DEBUG] PreviewArea_Drop: Started processing\n");
            
            try
            {
                // オーバーレイを非表示
                DropOverlay.Visibility = Visibility.Collapsed;

            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                string[] droppedItems = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                
                // ⭐修正: 即座にUI状態変化（ユーザー応答性向上）
                ViewModel.StatusMessage = "ファイルを処理中...";
                ViewModel.ProgressVisibility = "Visible";
                
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
                
                if (supportedFiles.Any())
                {
                    try
                    {
                        // ⭐修正: 詳細状態メッセージに更新（ProgressVisibilityは既に設定済み）
                        ViewModel.StatusMessage = $"{supportedFiles.Count} 個のファイルを処理中...";
                        
                        // PDFファイルと画像ファイルを分離
                        var pdfFiles = supportedFiles.Where(f => IsPdfFile(f)).ToList();
                        var imageFiles = supportedFiles.Where(f => IsImageFile(f)).ToList();
                        
                        // 画像ファイルを一括処理（高速化のため最初に処理）
                        if (imageFiles.Any())
                        {
                            try
                            {
                                await ViewModel.OpenMultipleImageFilesAsync(imageFiles);
                            }
                            catch (Exception ex)
                            {


                                File.AppendAllText("DEBUG_LOG.txt", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [CRITICAL ERROR] Image processing failed: {ex.Message}\n");
                                File.AppendAllText("DEBUG_LOG.txt", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [CRITICAL ERROR] StackTrace: {ex.StackTrace}\n");
                                // シンプルなエラーメッセージ（ログエラーを回避）
                                ViewModel.StatusMessage = $"画像変換エラー: {ex.Message}";
                            }
                        }
                        
                        // PDFファイルを処理
                        foreach (var pdfFile in pdfFiles)
                        {
                            try
                            {
                                await ViewModel.OpenFileAsync(pdfFile);
                            }
                            catch (Exception ex)
                            {
                                _logger?.LogWarning($"Failed to process PDF {pdfFile}: {ex.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ViewModel.StatusMessage = $"エラー: {ex.Message}";
                    }
                    finally
                    {
                        ViewModel.ProgressVisibility = "Collapsed";
                    }
                }
                else
                {
                    ViewModel.StatusMessage = "対応していないファイル形式です";
                }
            }
            }
            finally
            {
                _isProcessingDrop = false;
                File.AppendAllText("DEBUG_LOG.txt", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [DEBUG] PreviewArea_Drop: Processing completed, flag reset\n");
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
                
                if (hasSupportedItem)
                {
                    e.Effects = System.Windows.DragDropEffects.Copy;
                    DropOverlay.Visibility = Visibility.Visible;
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
                DropOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async void Window_Drop(object sender, DragEventArgs e)
        {
            // ⭐重複処理防止チェック
            if (_isProcessingDrop)
            {
                File.AppendAllText("DEBUG_LOG.txt", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [WARNING] Window_Drop: Already processing, skipping duplicate event\n");
                e.Handled = true;
                return;
            }
            
            _isProcessingDrop = true;
            File.AppendAllText("DEBUG_LOG.txt", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [DEBUG] Window_Drop: Started processing\n");
            
            try
            {
                DropOverlay.Visibility = Visibility.Collapsed;

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
                
                if (supportedFiles.Any())
                {
                    try
                    {
                        // ⭐修正: 詳細状態メッセージに更新（ProgressVisibilityは既に設定済み）
                        ViewModel.StatusMessage = $"{supportedFiles.Count} 個のファイルを処理中...";
                        
                        // PDFファイルと画像ファイルを分離
                        var pdfFiles = supportedFiles.Where(f => IsPdfFile(f)).ToList();
                        var imageFiles = supportedFiles.Where(f => IsImageFile(f)).ToList();
                        
                        // 画像ファイルを一括処理（高速化のため最初に処理）
                        if (imageFiles.Any())
                        {
                            try
                            {
                                await ViewModel.OpenMultipleImageFilesAsync(imageFiles);
                            }
                            catch (Exception ex)
                            {


                                File.AppendAllText("DEBUG_LOG.txt", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [CRITICAL ERROR] Image processing failed: {ex.Message}\n");
                                File.AppendAllText("DEBUG_LOG.txt", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [CRITICAL ERROR] StackTrace: {ex.StackTrace}\n");
                                // シンプルなエラーメッセージ（ログエラーを回避）
                                ViewModel.StatusMessage = $"画像変換エラー: {ex.Message}";
                            }
                        }
                        
                        // PDFファイルを処理
                        foreach (var pdfFile in pdfFiles)
                        {
                            try
                            {
                                await ViewModel.OpenFileAsync(pdfFile);
                            }
                            catch (Exception ex)
                            {
                                _logger?.LogWarning($"Failed to process PDF {pdfFile}: {ex.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ViewModel.StatusMessage = $"エラー: {ex.Message}";
                    }
                    finally
                    {
                        await Task.Delay(2000);
                        ViewModel.ProgressVisibility = "Collapsed";
                        ViewModel.StatusMessage = "準備完了";
                    }
                }
                else
                {
                    ViewModel.StatusMessage = "対応していないファイル形式です";
                    await Task.Delay(2000);
                    ViewModel.StatusMessage = "準備完了";
                }
            }
            }
            finally
            {
                _isProcessingDrop = false;
                File.AppendAllText("DEBUG_LOG.txt", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [DEBUG] Window_Drop: Processing completed, flag reset\n");
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
                    
                    if (listBox.SelectedItem is PageViewModel selectedPage)
                    {

                        _logger?.LogInformation($"Selected page: {selectedPage.PageNumber}");
                        
                        // ViewModelのSelectedPageとCurrentPageImageを更新
                        ViewModel.UpdateSelectedPage(selectedPage);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[PageListBox_SelectionChanged] SelectedItem is not PageViewModel: {listBox.SelectedItem?.GetType().Name}");
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