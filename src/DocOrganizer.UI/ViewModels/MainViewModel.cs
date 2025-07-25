using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using DocOrganizer.Application.Interfaces;
using DocOrganizer.Core.Models;

namespace DocOrganizer.UI.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IPdfEditorService _pdfEditorService;
        private readonly IDialogService _dialogService;
        private readonly IImageProcessingService _imageProcessingService;
        private readonly IUpdateService? _updateService;
        
        [ObservableProperty]
        private ObservableCollection<PageViewModel> pages = new();
        
        [ObservableProperty]
        private string statusMessage = "準備完了";
        
        [ObservableProperty]
        private string pageCountText = "0 ページ";
        
        [ObservableProperty]
        private string pageInfo = "";
        
        [ObservableProperty]
        private string fileInfo = "";
        
        [ObservableProperty]
        private int progressValue;
        
        [ObservableProperty]
        private string progressVisibility = "Collapsed";
        
        [ObservableProperty]
        private double previewWidth = 800; // 適切な最大幅
        
        [ObservableProperty]
        private double previewHeight = 1000; // 適切な最大高さ
        
        [ObservableProperty]
        private object? currentPageImage;
        
        [ObservableProperty]
        private string emptyStateVisibility = "Visible";
        
        [ObservableProperty]
        private bool hasDocument;
        
        [ObservableProperty]
        private bool hasSelectedPages;
        
        [ObservableProperty]
        private bool canMerge;
        
        [ObservableProperty]
        private string zoomLevel = "100%";

        private PdfDocument? _currentDocument;
        private readonly ObservableCollection<PdfDocument> _openDocuments = new();
        private PageViewModel? _selectedPage;

        public MainViewModel(IPdfEditorService pdfEditorService, IDialogService dialogService, IImageProcessingService imageProcessingService, IUpdateService? updateService = null)
        {
            _pdfEditorService = pdfEditorService;
            _dialogService = dialogService;
            _imageProcessingService = imageProcessingService;
            _updateService = updateService;
            
            System.Diagnostics.Debug.WriteLine("[MainViewModel] Constructor called");
            
            // コマンドの初期化状態を確認（CommunityToolkit.Mvvmは自動生成するので後で確認）
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] OpenCommand: {OpenCommand != null}");
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] SaveCommand: {SaveCommand != null}");
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] RotateLeftCommand: {RotateLeftCommand != null}");
                System.Diagnostics.Debug.WriteLine($"[MainViewModel] RotateRightCommand: {RotateRightCommand != null}");
                
                // CommandManagerの動作確認
                CommandManager.InvalidateRequerySuggested();
                System.Diagnostics.Debug.WriteLine("[MainViewModel] CommandManager.InvalidateRequerySuggested called");
            }));
        }

        [RelayCommand]
        private async Task OpenAsync()
        {
            System.Diagnostics.Debug.WriteLine("[OpenAsync] Command executed!");
            _dialogService.ShowInformation("Openコマンドが実行されました！");
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "対応ファイル|*.pdf;*.jpg;*.jpeg;*.png;*.heic;*.heif;*.bmp;*.tiff;*.gif;*.webp|PDF ファイル (*.pdf)|*.pdf|画像ファイル|*.jpg;*.jpeg;*.png;*.heic;*.heif;*.bmp;*.tiff;*.gif;*.webp|すべてのファイル (*.*)|*.*",
                Title = "ファイルを開く",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (var fileName in openFileDialog.FileNames)
                {
                    if (IsPdfFile(fileName))
                    {
                        await OpenFileAsync(fileName);
                    }
                    else if (IsImageFile(fileName))
                    {
                        await OpenImageFileAsync(fileName);
                    }
                }
            }
        }

        public async Task OpenFileAsync(string filePath)
        {
            try
            {
                StatusMessage = $"読み込み中: {Path.GetFileName(filePath)}";
                ProgressVisibility = "Visible";
                
                var document = await _pdfEditorService.OpenPdfAsync(filePath);
                _openDocuments.Add(document);
                
                if (_currentDocument == null)
                {
                    SetCurrentDocument(document);
                }
                
                UpdateUI();
                StatusMessage = $"読み込み完了: {Path.GetFileName(filePath)}";
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"ファイルを開けませんでした: {ex.Message}");
                StatusMessage = "エラーが発生しました";
            }
            finally
            {
                ProgressVisibility = "Collapsed";
            }
        }

        private async void SetCurrentDocument(PdfDocument document)
        {
            _currentDocument = document;
            Pages.Clear();
            
            // まずPageViewModelを作成
            foreach (var page in document.Pages)
            {
                var pageVm = new PageViewModel(page);
                pageVm.PropertyChanged += PageViewModel_PropertyChanged;
                Pages.Add(pageVm);
            }
            
            EmptyStateVisibility = "Collapsed";
            UpdateUI();
            
            // 最初のページを自動選択
            if (Pages.Any())
            {
                Pages.First().IsSelected = true;
                UpdateSelectedPage(Pages.First());
            }
            
            // サムネイル更新を非同期で実行（UIをブロックしない）
            _ = Task.Run(async () =>
            {
                try 
                {
                    await UpdateAllThumbnailsAsync();
                }
                catch (Exception ex)
                {
                    // エラーは無視（サムネイルは既に設定されている可能性が高い）
                    System.Diagnostics.Debug.WriteLine($"サムネイル更新エラー: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"スタックトレース: {ex.StackTrace}");
                }
            });
        }
        
        private async Task UpdateAllThumbnailsAsync()
        {
            try
            {
                await _pdfEditorService.UpdateAllThumbnailsAsync();
                
                // PageViewModelのサムネイルを更新
                foreach (var pageVm in Pages)
                {
                    pageVm.LoadThumbnail();
                }
            }
            catch (Exception ex)
            {
                // Failed to update thumbnails - エラーはUIに表示済み
            }
        }

        private void PageViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PageViewModel.IsSelected))
            {
                UpdateSelectionState();
            }
        }

        private void UpdateSelectionState()
        {
            System.Diagnostics.Debug.WriteLine("[UpdateSelectionState] Called");
            
            var selectedCount = Pages.Count(p => p.IsSelected);
            HasSelectedPages = selectedCount > 0;
            
            System.Diagnostics.Debug.WriteLine($"[UpdateSelectionState] Selected count: {selectedCount}, HasSelectedPages: {HasSelectedPages}");
            
            if (selectedCount == 1)
            {
                var selectedPage = Pages.FirstOrDefault(p => p.IsSelected);
                if (selectedPage != null)
                {
                    PageInfo = $"ページ {selectedPage.PageNumber}/{Pages.Count}";
                    System.Diagnostics.Debug.WriteLine($"[UpdateSelectionState] Single page selected: {selectedPage.PageNumber}");
                    // UpdateSelectedPageを使用（UpdatePreviewではなく）
                    UpdateSelectedPage(selectedPage);
                }
            }
            else if (selectedCount > 1)
            {
                PageInfo = $"{selectedCount} ページ選択中";
            }
            else
            {
                PageInfo = "";
            }
        }

        private async void UpdatePreview(PageViewModel pageViewModel)
        {
            try
            {
                if (pageViewModel?.Page == null) return;

                // プレビュー用の大きな画像を生成
                if (_currentDocument != null)
                {
                    var pageIndex = _currentDocument.Pages.ToList().IndexOf(pageViewModel.Page);
                    if (pageIndex >= 0)
                    {
                        // 高解像度プレビューを生成（スケール1.5倍）
                        var previewBitmap = await _pdfEditorService.GetPagePreviewAsync(_currentDocument, pageIndex, 1.5f);
                        
                        if (previewBitmap != null)
                        {
                            // UIスレッドで実行
                            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                try
                                {
                                    // SkiaSharpのSKBitmapをWPFで表示可能な形式に変換
                                    using var data = previewBitmap.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                                    bitmap.BeginInit();
                                    bitmap.StreamSource = new System.IO.MemoryStream(data.ToArray());
                                    bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                                    bitmap.EndInit();
                                    bitmap.Freeze();
                                    
                                    CurrentPageImage = bitmap;
                                    
                                    // プレビューサイズを更新
                                    PreviewWidth = bitmap.PixelWidth;
                                    PreviewHeight = bitmap.PixelHeight;
                                }
                                catch (Exception uiEx)
                                {
                                    System.Diagnostics.Debug.WriteLine($"プレビューUI更新エラー: {uiEx.Message}");
                                }
                            });
                            
                            previewBitmap.Dispose();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // エラーをログに記録するが、UIには表示しない（プレビュー更新は頻繁に呼ばれるため）
                System.Diagnostics.Debug.WriteLine($"プレビュー表示エラー: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"スタックトレース: {ex.StackTrace}");
            }
        }

        private void UpdateUI()
        {
            HasDocument = _currentDocument != null;
            CanMerge = _openDocuments.Count > 1;
            PageCountText = $"{Pages.Count} ページ";
            
            if (_currentDocument != null)
            {
                FileInfo = Path.GetFileName(_currentDocument.FilePath);
            }
            
            // 各プロパティの変更を通知
            OnPropertyChanged(nameof(HasDocument));
            OnPropertyChanged(nameof(CanMerge));
            OnPropertyChanged(nameof(HasSelectedPages));
            
            // コマンドの再評価を強制
            System.Diagnostics.Debug.WriteLine("[UpdateUI] Forcing command re-evaluation");
            System.Diagnostics.Debug.WriteLine($"[UpdateUI] HasDocument: {HasDocument}, HasSelectedPages: {HasSelectedPages}");
            
            // CommandManagerに再評価を要求
            System.Windows.Application.Current?.Dispatcher.BeginInvoke(new Action(() =>
            {
                CommandManager.InvalidateRequerySuggested();
                
                // 各コマンドの通知を強制（CommunityToolkit.Mvvmのコマンド用）
                try
                {
                    OpenCommand?.NotifyCanExecuteChanged();
                    SaveCommand?.NotifyCanExecuteChanged();
                    SaveAsCommand?.NotifyCanExecuteChanged();
                    RotateLeftCommand?.NotifyCanExecuteChanged();
                    RotateRightCommand?.NotifyCanExecuteChanged();
                    DeleteCommand?.NotifyCanExecuteChanged();
                    MergeCommand?.NotifyCanExecuteChanged();
                    SplitCommand?.NotifyCanExecuteChanged();
                    System.Diagnostics.Debug.WriteLine("[UpdateUI] Command notifications sent");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[UpdateUI] Error notifying commands: {ex.Message}");
                }
            }));
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (_currentDocument == null) return;
            
            try
            {
                StatusMessage = "保存中...";
                ProgressVisibility = "Visible";
                
                var result = await _pdfEditorService.SavePdfAsync(_currentDocument, _currentDocument.FilePath);
                
                if (result)
                {
                    StatusMessage = "保存完了";
                }
                else
                {
                    _dialogService.ShowError("保存に失敗しました");
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"保存エラー: {ex.Message}");
            }
            finally
            {
                ProgressVisibility = "Collapsed";
            }
        }

        [RelayCommand(CanExecute = nameof(HasDocument))]
        private async Task SaveAsAsync()
        {
            if (_currentDocument == null) return;
            
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PDF ファイル (*.pdf)|*.pdf",
                Title = "名前を付けて保存",
                FileName = Path.GetFileName(_currentDocument.FilePath)
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    StatusMessage = "保存中...";
                    ProgressVisibility = "Visible";
                    
                    var result = await _pdfEditorService.SavePdfAsync(_currentDocument, saveFileDialog.FileName);
                    
                    if (result)
                    {
                        StatusMessage = $"保存完了: {Path.GetFileName(saveFileDialog.FileName)}";
                    }
                    else
                    {
                        _dialogService.ShowError("保存に失敗しました");
                    }
                }
                catch (Exception ex)
                {
                    _dialogService.ShowError($"保存エラー: {ex.Message}");
                }
                finally
                {
                    ProgressVisibility = "Collapsed";
                }
            }
        }

        [RelayCommand(CanExecute = nameof(HasSelectedPages))]
        private void RotateLeft()
        {
            System.Diagnostics.Debug.WriteLine("[RotateLeft] Command executed");
            System.Diagnostics.Debug.WriteLine($"[RotateLeft] HasSelectedPages: {HasSelectedPages}");
            RotateSelectedPages(270); // 左回転 = 270度（反時計回り）
        }

        [RelayCommand(CanExecute = nameof(HasSelectedPages))]
        private void RotateRight()
        {
            System.Diagnostics.Debug.WriteLine("[RotateRight] Command executed");
            System.Diagnostics.Debug.WriteLine($"[RotateRight] HasSelectedPages: {HasSelectedPages}");
            RotateSelectedPages(90); // 右回転 = 90度（時計回り）
        }

        private void RotateSelectedPages(int degrees)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[RotateSelectedPages] Called with degrees: {degrees}");
                
                if (_currentDocument == null)
                {
                    System.Diagnostics.Debug.WriteLine("[RotateSelectedPages] _currentDocument is null");
                    return;
                }
                
                var selectedPages = Pages.Where(p => p.IsSelected).ToList();
                System.Diagnostics.Debug.WriteLine($"[RotateSelectedPages] Selected pages count: {selectedPages.Count}");
                
                foreach (var pageVm in selectedPages)
                {
                    System.Diagnostics.Debug.WriteLine($"[RotateSelectedPages] Rotating page {pageVm.PageNumber}");
                    var page = _currentDocument.Pages[pageVm.PageNumber - 1]; // PageNumberは1-based, indexは0-based
                    _pdfEditorService.RotatePage(page, degrees);
                    pageVm.UpdateRotation();
                }
                
                // 単一ページが選択されている場合、プレビューも更新
                if (selectedPages.Count == 1)
                {
                    System.Diagnostics.Debug.WriteLine("[RotateSelectedPages] Updating preview for single page");
                    UpdateSelectedPage(selectedPages[0]);
                }
                
                StatusMessage = $"{selectedPages.Count} ページを回転しました";
                System.Diagnostics.Debug.WriteLine("[RotateSelectedPages] Completed successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RotateSelectedPages] Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[RotateSelectedPages] StackTrace: {ex.StackTrace}");
                _dialogService.ShowError($"回転エラー: {ex.Message}");
            }
        }

        [RelayCommand(CanExecute = nameof(HasSelectedPages))]
        private void Delete()
        {
            if (_currentDocument == null) return;
            
            var selectedPages = Pages.Where(p => p.IsSelected).OrderByDescending(p => p.PageNumber).ToList();
            
            if (_dialogService.ShowConfirmation($"{selectedPages.Count} ページを削除しますか？"))
            {
                foreach (var pageVm in selectedPages)
                {
                    _pdfEditorService.RemovePage(_currentDocument, pageVm.PageNumber);
                    Pages.Remove(pageVm);
                }
                
                // ページ番号を再設定
                for (int i = 0; i < Pages.Count; i++)
                {
                    Pages[i].UpdatePageNumber(i + 1);
                }
                
                UpdateUI();
                StatusMessage = $"{selectedPages.Count} ページを削除しました";
            }
        }

        [RelayCommand(CanExecute = nameof(CanMerge))]
        private async Task MergeAsync()
        {
            try
            {
                StatusMessage = "PDF を結合中...";
                ProgressVisibility = "Visible";
                
                var filePaths = _openDocuments.Where(d => !string.IsNullOrEmpty(d.FilePath)).Select(d => d.FilePath!).ToArray();
                var mergedDocument = await _pdfEditorService.MergePdfsAsync(filePaths);
                _openDocuments.Add(mergedDocument);
                SetCurrentDocument(mergedDocument);
                
                StatusMessage = "結合完了";
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"結合エラー: {ex.Message}");
            }
            finally
            {
                ProgressVisibility = "Collapsed";
            }
        }

        [RelayCommand(CanExecute = nameof(HasDocument))]
        private async Task SplitAsync()
        {
            // TODO: 分割ダイアログの実装
            _dialogService.ShowInformation("分割機能は現在実装中です");
        }

        [RelayCommand]
        private void Close()
        {
            if (_currentDocument != null)
            {
                _pdfEditorService.CloseDocument();
                _openDocuments.Remove(_currentDocument);
                
                if (_openDocuments.Any())
                {
                    SetCurrentDocument(_openDocuments.First());
                }
                else
                {
                    _currentDocument = null;
                    Pages.Clear();
                    EmptyStateVisibility = "Visible";
                    UpdateUI();
                }
            }
        }

        [RelayCommand]
        private void Exit()
        {
            System.Windows.Application.Current.Shutdown();
        }

        [RelayCommand]
        private void SelectAll()
        {
            foreach (var page in Pages)
            {
                page.IsSelected = true;
            }
        }

        [RelayCommand]
        private void DeselectAll()
        {
            foreach (var page in Pages)
            {
                page.IsSelected = false;
            }
        }

        [RelayCommand]
        private void About()
        {
            _dialogService.ShowInformation(
                "DocOrganizer 2.2\n\n" +
                "CubePDF互換 PDF編集ツール\n\n" +
                "Version 2.2.0\n" +
                "© 2025 DocOrganizer Project");
        }

        // 存在しないコマンドのスタブ実装
        [RelayCommand]
        private void Undo()
        {
            // TODO: 元に戻す機能の実装
            StatusMessage = "元に戻す機能は実装中です";
        }

        [RelayCommand]
        private void Redo()
        {
            // TODO: やり直し機能の実装
            StatusMessage = "やり直し機能は実装中です";
        }

        [RelayCommand]
        private void ZoomIn()
        {
            System.Diagnostics.Debug.WriteLine("[ZoomIn] Command executed!");
            // TODO: 拡大機能の実装
            StatusMessage = "拡大機能は実装中です";
            _dialogService.ShowInformation("ZoomInコマンドが実行されました！");
        }

        [RelayCommand]
        private void ZoomOut()
        {
            System.Diagnostics.Debug.WriteLine("[ZoomOut] Command executed!");
            // TODO: 縮小機能の実装
            StatusMessage = "縮小機能は実装中です";
            _dialogService.ShowInformation("ZoomOutコマンドが実行されました！");
        }

        [RelayCommand]
        private void FitToWindow()
        {
            // TODO: 全体表示機能の実装
            StatusMessage = "全体表示機能は実装中です";
        }

        [RelayCommand]
        private void ThumbnailSmall()
        {
            // TODO: サムネイルサイズ変更
            StatusMessage = "サムネイルサイズ変更は実装中です";
        }

        [RelayCommand]
        private void ThumbnailMedium()
        {
            // TODO: サムネイルサイズ変更
            StatusMessage = "サムネイルサイズ変更は実装中です";
        }

        [RelayCommand]
        private void ThumbnailLarge()
        {
            // TODO: サムネイルサイズ変更
            StatusMessage = "サムネイルサイズ変更は実装中です";
        }

        [RelayCommand]
        private void ShowHelp()
        {
            // TODO: ヘルプ表示機能
            StatusMessage = "ヘルプ機能は実装中です";
        }

        [RelayCommand]
        private void Security()
        {
            // TODO: セキュリティ設定機能
            StatusMessage = "セキュリティ設定機能は実装中です";
        }

        public void UpdateSelectedPage(PageViewModel selectedPage)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateSelectedPage] Called with page: {selectedPage?.PageNumber}");
                
                if (selectedPage == null)
                {
                    System.Diagnostics.Debug.WriteLine("[UpdateSelectedPage] selectedPage is null, returning");
                    return;
                }
                    
                _selectedPage = selectedPage;
                System.Diagnostics.Debug.WriteLine($"[UpdateSelectedPage] Selected page set to: {_selectedPage.PageNumber}");
                
                // 選択されたページの画像を拡大表示エリアに表示
                CurrentPageImage = selectedPage.PreviewImage;
                System.Diagnostics.Debug.WriteLine($"[UpdateSelectedPage] CurrentPageImage set: {CurrentPageImage != null}");
                
                // 必要に応じてプレビューサイズを更新
                if (selectedPage.Page != null)
                {
                    // 画像の実際のサイズを取得
                    var actualWidth = selectedPage.Page.Width;
                    var actualHeight = selectedPage.Page.Height;
                    
                    // 最大表示サイズ（プレビューエリアに収まるサイズ）
                    var maxDisplayWidth = 800.0;
                    var maxDisplayHeight = 1000.0;
                    
                    // アスペクト比を維持しながら、表示エリアに収まるサイズを計算
                    var widthScale = maxDisplayWidth / actualWidth;
                    var heightScale = maxDisplayHeight / actualHeight;
                    var scale = Math.Min(widthScale, heightScale);
                    
                    // スケールが1より大きい場合は、元のサイズを維持（拡大しない）
                    if (scale > 1.0)
                    {
                        PreviewWidth = actualWidth;
                        PreviewHeight = actualHeight;
                    }
                    else
                    {
                        PreviewWidth = actualWidth * scale;
                        PreviewHeight = actualHeight * scale;
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[UpdateSelectedPage] Original size: {actualWidth}x{actualHeight}");
                    System.Diagnostics.Debug.WriteLine($"[UpdateSelectedPage] Preview size updated: {PreviewWidth}x{PreviewHeight}");
                }
                
                // ステータスバーに情報を表示
                PageInfo = $"ページ {selectedPage.PageNumber}";
                StatusMessage = $"ページ {selectedPage.PageNumber} を表示中";
                UpdateUI();
                
                System.Diagnostics.Debug.WriteLine("[UpdateSelectedPage] Completed successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateSelectedPage] Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[UpdateSelectedPage] StackTrace: {ex.StackTrace}");
                _dialogService.ShowError($"ページ選択エラー: {ex.Message}");
            }
        }

        public void ReorderPages(System.Collections.Generic.List<PageViewModel> pagesToMove, PageViewModel targetPage)
        {
            if (_currentDocument == null || pagesToMove == null || targetPage == null)
                return;

            // ドラッグされたページとターゲットページのインデックスを取得
            int targetIndex = Pages.IndexOf(targetPage);
            if (targetIndex == -1)
                return;

            // ドラッグされたページを一時的に削除
            var movingPages = new System.Collections.Generic.List<(PageViewModel page, int originalIndex)>();
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
            for (int i = 0; i < Pages.Count; i++)
            {
                Pages[i].UpdatePageNumber(i + 1);
            }

            // 実際のPDFドキュメントのページ順序も更新
            _pdfEditorService.ReorderPages(_currentDocument, Pages.Select(p => p.Page).ToArray());

            StatusMessage = $"{pagesToMove.Count} ページを移動しました";
        }

        public async Task OpenImageFileAsync(string filePath)
        {
            try
            {
                StatusMessage = $"画像変換中: {Path.GetFileName(filePath)}";
                ProgressVisibility = "Visible";
                
                // 画像をPDFに変換
                var pdfDocument = await _imageProcessingService.ConvertImageToPdfAsync(filePath);
                _openDocuments.Add(pdfDocument);
                
                if (_currentDocument == null)
                {
                    SetCurrentDocument(pdfDocument);
                }
                
                UpdateUI();
                StatusMessage = $"画像変換完了: {Path.GetFileName(filePath)}";
            }
            catch (NotSupportedException ex)
            {
                // 対応していないファイル形式またはファイル破損
                var errorMessage = $"このファイルは対応していない形式か、破損している可能性があります:\n{Path.GetFileName(filePath)}";
                _dialogService.ShowError(errorMessage);
                StatusMessage = $"非対応ファイル: {Path.GetFileName(filePath)}";
            }
            catch (Exception ex) when (ex.GetType().Name.Contains("ImageProcessing"))
            {
                // ImageProcessingException専用
                var errorMessage = $"画像処理エラー: {Path.GetFileName(filePath)}\n{ex.Message}";
                _dialogService.ShowError(errorMessage);
                StatusMessage = $"画像処理エラー: {Path.GetFileName(filePath)}";
            }
            catch (Exception ex)
            {
                // その他の予期しないエラー
                var errorMessage = $"予期しないエラーが発生しました: {Path.GetFileName(filePath)}\n詳細: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $"\n内部エラー: {ex.InnerException.Message}";
                }
                _dialogService.ShowError(errorMessage);
                StatusMessage = $"エラー: {Path.GetFileName(filePath)}";
            }
            finally
            {
                ProgressVisibility = "Collapsed";
            }
        }

        public async Task OpenMultipleImageFilesAsync(IEnumerable<string> filePaths)
        {
            try
            {
                var imageFiles = filePaths.Where(f => IsImageFile(f)).ToList();
                if (!imageFiles.Any()) return;

                StatusMessage = $"{imageFiles.Count} 個の画像を変換中...";
                ProgressVisibility = "Visible";
                
                // 複数画像を1つのPDFに変換（非同期処理）
                var pdfDocument = await _imageProcessingService.ConvertImagesToPdfAsync(imageFiles);
                
                if (pdfDocument == null)
                {
                    throw new InvalidOperationException("画像からPDFドキュメントの作成に失敗しました");
                }
                
                _openDocuments.Add(pdfDocument);
                
                if (_currentDocument == null)
                {
                    // UIスレッドで実行
                    SetCurrentDocument(pdfDocument);
                }
                
                UpdateUI();
                StatusMessage = $"{imageFiles.Count} 個の画像を1つのPDFに変換完了";
            }
            catch (Exception ex)
            {
                var errorMessage = $"複数画像の変換エラー:\n{ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $"\n内部エラー: {ex.InnerException.Message}";
                }
                _dialogService.ShowError(errorMessage);
                StatusMessage = "複数画像変換エラー";
            }
            finally
            {
                ProgressVisibility = "Collapsed";
            }
        }


        private bool IsPdfFile(string filePath)
        {
            return Path.GetExtension(filePath).Equals(".pdf", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsImageFile(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension == ".jpg" || extension == ".jpeg" || 
                   extension == ".png" || extension == ".heic" || 
                   extension == ".heif" || extension == ".bmp" || 
                   extension == ".tiff" || extension == ".gif" || 
                   extension == ".webp";
        }

        [RelayCommand]
        private async Task CheckForUpdatesAsync()
        {
            if (_updateService == null)
            {
                _dialogService.ShowInformation("アップデート機能は利用できません。");
                return;
            }

            try
            {
                StatusMessage = "アップデートを確認中...";
                ProgressVisibility = "Visible";

                var updateInfo = await _updateService.CheckForUpdatesAsync();
                
                if (updateInfo != null)
                {
                    var message = $"新しいバージョン {updateInfo.Version} が利用可能です。\n\n" +
                                  $"リリース日: {updateInfo.ReleaseDate:yyyy/MM/dd}\n" +
                                  $"ファイルサイズ: {updateInfo.FileSize / 1024 / 1024:F1} MB\n\n" +
                                  $"更新内容:\n{updateInfo.ReleaseNotes}\n\n" +
                                  "今すぐダウンロードしますか？";

                    if (_dialogService.ShowConfirmation(message))
                    {
                        await DownloadAndInstallUpdateAsync(updateInfo);
                    }
                }
                else
                {
                    StatusMessage = "最新バージョンを使用しています。";
                    _dialogService.ShowInformation($"DocOrganizer {_updateService.CurrentVersion} は最新バージョンです。");
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"アップデートの確認中にエラーが発生しました: {ex.Message}");
                StatusMessage = "アップデート確認エラー";
            }
            finally
            {
                ProgressVisibility = "Collapsed";
            }
        }

        private async Task DownloadAndInstallUpdateAsync(UpdateInfo updateInfo)
        {
            if (_updateService == null) return;

            try
            {
                StatusMessage = $"アップデート {updateInfo.Version} をダウンロード中...";
                ProgressVisibility = "Visible";
                ProgressValue = 0;

                var progress = new Progress<double>(percent =>
                {
                    ProgressValue = (int)percent;
                    StatusMessage = $"ダウンロード中... {percent:F0}%";
                });

                var downloadPath = await _updateService.DownloadUpdateAsync(updateInfo, progress);
                
                if (!string.IsNullOrEmpty(downloadPath))
                {
                    StatusMessage = "アップデートを適用中...";
                    
                    var message = "アップデートを適用するには、アプリケーションを再起動する必要があります。\n" +
                                  "今すぐ再起動しますか？";

                    if (_dialogService.ShowConfirmation(message))
                    {
                        await _updateService.ApplyUpdateAsync(downloadPath);
                    }
                }
                else
                {
                    _dialogService.ShowError("アップデートのダウンロードに失敗しました。");
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"アップデートの適用中にエラーが発生しました: {ex.Message}");
            }
            finally
            {
                ProgressVisibility = "Collapsed";
                StatusMessage = "準備完了";
            }
        }
    }
}