using System;
using System.Collections.Generic;
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
using SkiaSharp;

namespace DocOrganizer.UI.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IPdfEditorService _pdfEditorService;
        private readonly IDialogService _dialogService;
        private readonly IImageProcessingService _imageProcessingService;
        private readonly ITextOrientationService _textOrientationService;
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
        private bool canMoveUp;
        
        [ObservableProperty]
        private bool canMoveDown;
        
        [ObservableProperty]
        private string zoomLevel = "100%";

        private PdfDocument? _currentDocument;
        private readonly ObservableCollection<PdfDocument> _openDocuments = new();
        private PageViewModel? _selectedPage;

        public MainViewModel(IPdfEditorService pdfEditorService, IDialogService dialogService, IImageProcessingService imageProcessingService, ITextOrientationService textOrientationService, IUpdateService? updateService = null)
        {
            _pdfEditorService = pdfEditorService;
            _dialogService = dialogService;
            _imageProcessingService = imageProcessingService;
            _textOrientationService = textOrientationService;
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
        private void New()
        {
            try
            {
                // 現在のドキュメントがある場合は確認
                if (_currentDocument != null && _currentDocument.IsModified)
                {
                    var result = _dialogService.ShowMessage(
                        "現在のドキュメントは変更されています。保存しますか？",
                        "確認",
                        MessageBoxButtons.YesNoCancel,
                        MessageBoxIcon.Question);
                    
                    if (result == DialogResult.Yes)
                    {
                        SaveAsync().Wait();
                    }
                    else if (result == DialogResult.Cancel)
                    {
                        return;
                    }
                }
                
                // 新規ドキュメントとして空のPDFを作成
                Close();
                StatusMessage = "新規ドキュメント";
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"新規作成エラー: {ex.Message}");
            }
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
                var pageVm = new PageViewModel(page, _imageProcessingService, _textOrientationService);
                pageVm.PropertyChanged += PageViewModel_PropertyChanged;
                Pages.Add(pageVm);
            }
            
            // ⭐修正: 全PageViewModelで回転角度を強制同期（左右プレビュー不一致修正）
            foreach (var pageVm in Pages)
            {
                pageVm.UpdateRotationSync();
                System.Diagnostics.Debug.WriteLine($"[SetCurrentDocument] Page {pageVm.PageNumber} rotation sync: {pageVm.Rotation}°");
            }
            
            EmptyStateVisibility = "Collapsed";
            UpdateUI();
            
            // 最初のページを自動選択してプレビューを即座に表示
            if (Pages.Any())
            {
                var firstPage = Pages.First();
                firstPage.IsSelected = true;
                
                // シンプルに即座に表示（全ての画像形式で統一）
                UpdateSelectedPage(firstPage);
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
                // PDFが画像から作成された場合、ImageProcessingServiceを使用してサムネイルを生成
                if (_currentDocument != null && _currentDocument.IsTemporaryFromImages)
                {
                    var pageTasks = new List<Task>();
                    
                    for (int i = 0; i < _currentDocument.Pages.Count; i++)
                    {
                        var pageIndex = i;
                        var pdfPage = _currentDocument.Pages[pageIndex];
                        
                        if (!string.IsNullOrEmpty(pdfPage.SourceImagePath))
                        {
                            var task = Task.Run(async () =>
                            {
                                try
                                {
                                    var extension = Path.GetExtension(pdfPage.SourceImagePath).ToLowerInvariant();
                                    var isHeic = extension == ".heic" || extension == ".heif";
                                    
                                    System.Diagnostics.Debug.WriteLine($"[UpdateAllThumbnailsAsync] Generating thumbnail for page {pageIndex + 1}: {pdfPage.SourceImagePath} (HEIC: {isHeic})");
                                    
                                    // サムネイル生成（ImageProcessingServiceがHEIC変換を処理）
                                    var thumbnailData = await _imageProcessingService.GetImageThumbnailAsync(
                                        pdfPage.SourceImagePath, 
                                        150, 
                                        150);
                                    
                                    if (thumbnailData != null && thumbnailData.Length > 0)
                                    {
                                        try
                                        {
                                            using var stream = new MemoryStream(thumbnailData);
                                            // ⭐重要修正: SkiaSharpのEXIF Orientation自動適用を無効化
using var codec = SKCodec.Create(stream);
var skBitmap = SKBitmap.Decode(codec, new SKImageInfo(codec.Info.Width, codec.Info.Height));
                                            
                                            if (skBitmap == null)
                                            {
                                                System.Diagnostics.Debug.WriteLine($"[UpdateAllThumbnailsAsync] Failed to decode bitmap for: {pdfPage.SourceImagePath}");
                                                return; // 現在のタスクを終了
                                            }
                                        
                                        // HEIC処理はPageViewModelに一元化（重複処理を防止）
                                        System.Diagnostics.Debug.WriteLine($"[UpdateAllThumbnailsAsync] HEIC processing delegated to PageViewModel for: {Path.GetFileName(pdfPage.SourceImagePath)}");

                                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                                        {
                                            pdfPage.SetThumbnailImage(skBitmap);
                                            
                                            // ViewModelに通知
                                            if (pageIndex < Pages.Count)
                                            {
                                                Pages[pageIndex].LoadThumbnail();
                                            }
                                        });
                                    }
                                    catch (Exception decodeEx)
                                        {
                                            System.Diagnostics.Debug.WriteLine($"[UpdateAllThumbnailsAsync] Failed to decode or set thumbnail for page {pageIndex + 1}: {decodeEx.Message}");
                                        }
                                    }
                                    else
                                    {
                                        System.Diagnostics.Debug.WriteLine($"[UpdateAllThumbnailsAsync] Empty or null thumbnail data for page {pageIndex + 1}: {pdfPage.SourceImagePath}");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[UpdateAllThumbnailsAsync] Failed to generate thumbnail for page {pageIndex + 1}: {ex.Message}");
                                }
                            });
                            
                            pageTasks.Add(task);
                        }
                    }
                    
                    // 全サムネイル生成を待機
                    await Task.WhenAll(pageTasks).ConfigureAwait(false);
                }
                else
                {
                    // 通常のPDFファイルの場合は従来の処理
                    await _pdfEditorService.UpdateAllThumbnailsAsync();
                    
                    // PageViewModelのサムネイルを更新
                    foreach (var pageVm in Pages)
                    {
                        pageVm.LoadThumbnail();
                    }
                }
            }
            catch (Exception ex)
            {
                // Failed to update thumbnails - エラーはUIに表示済み
                System.Diagnostics.Debug.WriteLine($"[UpdateAllThumbnailsAsync] Failed to update thumbnails: {ex.Message}");
            }
        }

        private void PageViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PageViewModel.IsSelected))
            {
                UpdateSelectionState();
            }
            // ★修正: ThumbnailImage変更時にCollectionView項目を更新
            else if (e.PropertyName == nameof(PageViewModel.ThumbnailImage))
            {
                // 個別PageViewModelのThumbnailImage更新時にCollectionViewを更新
                System.Windows.Application.Current.Dispatcher.Invoke(() => {
                    var collectionView = System.Windows.Data.CollectionViewSource.GetDefaultView(Pages);
                    collectionView?.Refresh();
                    System.Diagnostics.Debug.WriteLine($"[PageViewModel_PropertyChanged] ThumbnailImage更新でCollectionView更新");
                });
            }
        }

        private void UpdateSelectionState()
        {
            System.Diagnostics.Debug.WriteLine("[UpdateSelectionState] Called");
            
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
            
            System.Diagnostics.Debug.WriteLine($"[UpdateSelectionState] Selected count: {selectedCount}, HasSelectedPages: {HasSelectedPages}, CanMoveUp: {CanMoveUp}, CanMoveDown: {CanMoveDown}");
            
            // 移動コマンドの状態変更を通知
            MovePageUpCommand?.NotifyCanExecuteChanged();
            MovePageDownCommand?.NotifyCanExecuteChanged();
            
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

        private async void UpdatePreview(PageViewModel pageViewModel, bool forceUpdate = false)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[UpdatePreview] ページ {pageViewModel?.PageNumber} のプレビュー更新開始 (forceUpdate: {forceUpdate})");
                
                if (pageViewModel?.Page == null) return;

                // HEIC判定による強制高解像度プレビュー条件
                bool isHeicSource = !string.IsNullOrEmpty(pageViewModel.Page.SourceImagePath) && 
                                   IsHeicFile(pageViewModel.Page.SourceImagePath);
                
                // forceUpdate、PreviewImageがnull、またはHEICファイルの場合は処理を実行
                if (forceUpdate || pageViewModel.PreviewImage == null || isHeicSource)
                {
                    System.Diagnostics.Debug.WriteLine($"[UpdatePreview] 強制更新またはPreviewImage未設定 - PreviewImage: {pageViewModel.PreviewImage != null}");
                    
                    // PageViewModelに既にPreviewImageがある場合はそれを使用（HEIC最適化処理済み）
                    if (pageViewModel.PreviewImage != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[UpdatePreview] 左右統一処理済みPreviewImageを使用");
                        
                        // UI スレッドで実行
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            CurrentPageImage = pageViewModel.PreviewImage;
                            
                            // プレビューサイズを適切に計算（画面表示用）
                            if (CurrentPageImage is System.Windows.Media.Imaging.BitmapImage bitmapImage)
                            {
                                // 小さいサムネイルの場合は適切な表示サイズに拡大
                                var displayWidth = Math.Max(bitmapImage.PixelWidth, 600);
                                var displayHeight = Math.Max(bitmapImage.PixelHeight, 800);
                                
                                // アスペクト比を維持
                                var aspectRatio = (double)bitmapImage.PixelWidth / bitmapImage.PixelHeight;
                                if (aspectRatio > 1) // 横長
                                {
                                    displayHeight = (int)(displayWidth / aspectRatio);
                                }
                                else // 縦長
                                {
                                    displayWidth = (int)(displayHeight * aspectRatio);
                                }
                                
                                PreviewWidth = displayWidth;
                                PreviewHeight = displayHeight;
                                System.Diagnostics.Debug.WriteLine($"[UpdatePreview] CurrentPageImage設定完了 (強制更新) - Original: {bitmapImage.PixelWidth}x{bitmapImage.PixelHeight}, Display: {PreviewWidth}x{PreviewHeight}");
                            }
                        });
                        return;
                    }
                    
                    // PreviewImageがない場合の処理
                    if (_currentDocument != null)
                    {
                        var pageIndex = _currentDocument.Pages.ToList().IndexOf(pageViewModel.Page);
                        if (pageIndex >= 0)
                        {
                            var page = _currentDocument.Pages[pageIndex];
                            
                            // 元画像パスが存在する場合は新しい高品質プレビューサービスを使用
                            if (!string.IsNullOrEmpty(page.SourceImagePath) && System.IO.File.Exists(page.SourceImagePath))
                            {
                                System.Diagnostics.Debug.WriteLine($"[UpdatePreview] 高品質プレビューサービスで生成: {page.SourceImagePath}");
                                
                                try
                                {
                                    // 新しい高品質プレビューサービスを使用
                                    // ⭐最終修正: 右側プレビューもEXIF情報を完全削除（90度回転問題根本解決）
                                    var exifFreeImageBytes = await _imageProcessingService.GenerateExifFreeImageForWpfAsync(page.SourceImagePath, 1200, 1600);
                                    
                                    if (exifFreeImageBytes != null)
                                    {
                                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                                        {
                                            try
                                            {
                                                // ⭐最終修正: EXIF完全削除済みPNGから直接WPF BitmapImage作成
                                                var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                                                bitmap.BeginInit();
                                                bitmap.StreamSource = new System.IO.MemoryStream(exifFreeImageBytes);
                                                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                                                // ⭐CreateOptions不要: すでにEXIF情報が削除済みPNG
                                                bitmap.EndInit();
                                                bitmap.Freeze();
                                                
                                                CurrentPageImage = bitmap;
                                                
                                                // プレビューサイズを計算（高解像度維持）
                                                var displayWidth = Math.Max(bitmap.PixelWidth, 600);
                                                var displayHeight = Math.Max(bitmap.PixelHeight, 800);
                                                
                                                var aspectRatio = (double)bitmap.PixelWidth / bitmap.PixelHeight;
                                                if (aspectRatio > 1)
                                                {
                                                    displayHeight = (int)(displayWidth / aspectRatio);
                                                }
                                                else
                                                {
                                                    displayWidth = (int)(displayHeight * aspectRatio);
                                                }
                                                
                                                PreviewWidth = displayWidth;
                                                PreviewHeight = displayHeight;
                                                
                                                System.Diagnostics.Debug.WriteLine($"[UpdatePreview] ⭐EXIF削除版 高品質プレビュー完了 - Size: {PreviewWidth}x{PreviewHeight}");
                                            }
                                            catch (Exception uiEx)
                                            {
                                                System.Diagnostics.Debug.WriteLine($"高品質プレビューUI更新エラー: {uiEx.Message}");
                                            }
                                        });
                                        
                                        return; // 高品質プレビュー完了、PDFプレビューをスキップ
                                    }
                                }
                                catch (Exception imgEx)
                                {
                                    System.Diagnostics.Debug.WriteLine($"高品質プレビュー生成エラー: {imgEx.Message}");
                                    // エラーの場合はPDFプレビューにフォールバック
                                }
                            }
                            
                            // PDFプレビュー生成（フォールバック処理）
                            System.Diagnostics.Debug.WriteLine($"[UpdatePreview] PDFプレビューから生成");
                            
                            // 最高品質プレビューを生成（スケール3.0倍で高解像度）
                            var previewBitmap = await _pdfEditorService.GetPagePreviewAsync(_currentDocument, pageIndex, 3.0f);
                            
                            if (previewBitmap != null)
                            {
                                // UI スレッドで実行
                                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                                {
                                    try
                                    {
                                        // SkiaSharpのSKBitmapを無圧縮PNG形式で変換（最高品質）
                                        using var data = previewBitmap.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                                        var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                                        bitmap.BeginInit();
                                        bitmap.StreamSource = new System.IO.MemoryStream(data.ToArray());
                                        bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                                        // ⭐テスト: CreateOptionsを一時的に無効化して表示確認
                                        // bitmap.CreateOptions = System.Windows.Media.Imaging.BitmapCreateOptions.IgnoreImageCache;
                                        bitmap.EndInit();
                                        bitmap.Freeze();
                                        
                                        CurrentPageImage = bitmap;
                                        
                                        // プレビューサイズを更新
                                        PreviewWidth = bitmap.PixelWidth;
                                        PreviewHeight = bitmap.PixelHeight;
                                        System.Diagnostics.Debug.WriteLine($"[UpdatePreview] PDFプレビュー生成完了 - Size: {PreviewWidth}x{PreviewHeight}");
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
                else
                {
                    // forceUpdate=false かつ PreviewImageが存在する場合（通常ケース）
                    System.Diagnostics.Debug.WriteLine($"[UpdatePreview] PageViewModelのPreviewImageを使用（通常ケース）");
                    
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        CurrentPageImage = pageViewModel.PreviewImage;
                        
                        if (CurrentPageImage is System.Windows.Media.Imaging.BitmapImage bitmapImage)
                        {
                            // 小さいサムネイルの場合は適切な表示サイズに拡大
                            var displayWidth = Math.Max(bitmapImage.PixelWidth, 600);
                            var displayHeight = Math.Max(bitmapImage.PixelHeight, 800);
                            
                            // アスペクト比を維持
                            var aspectRatio = (double)bitmapImage.PixelWidth / bitmapImage.PixelHeight;
                            if (aspectRatio > 1) // 横長
                            {
                                displayHeight = (int)(displayWidth / aspectRatio);
                            }
                            else // 縦長
                            {
                                displayWidth = (int)(displayHeight * aspectRatio);
                            }
                            
                            PreviewWidth = displayWidth;
                            PreviewHeight = displayHeight;
                            System.Diagnostics.Debug.WriteLine($"[UpdatePreview] CurrentPageImage設定完了（通常） - Original: {bitmapImage.PixelWidth}x{bitmapImage.PixelHeight}, Display: {PreviewWidth}x{PreviewHeight}");
                        }
                    });
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
                    MovePageUpCommand?.NotifyCanExecuteChanged();
                    MovePageDownCommand?.NotifyCanExecuteChanged();
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

        [RelayCommand(CanExecute = nameof(HasDocument))]
        private async Task SaveAsync()
        {
            if (_currentDocument == null) return;
            
            try
            {
                // 既存ファイルの場合は上書き保存
                if (!string.IsNullOrEmpty(_currentDocument.FilePath) && 
                    File.Exists(_currentDocument.FilePath))
                {
                    await SaveDocumentAsync(_currentDocument.FilePath);
                }
                else
                {
                    // 新規ファイルの場合は名前を付けて保存
                    await SaveAsAsync();
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"保存エラー: {ex.Message}");
            }
        }

        [RelayCommand(CanExecute = nameof(HasDocument))]
        private async Task SaveAsAsync()
        {
            if (_currentDocument == null) return;
            
            try
            {
                // outputフォルダのパスを生成
                var outputDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "output");
                if (!Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }
                
                var saveDialog = new SaveFileDialog
                {
                    Filter = "PDF files (*.pdf)|*.pdf",
                    DefaultExt = "pdf",
                    InitialDirectory = outputDir,
                    FileName = $"document_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
                };
                
                if (saveDialog.ShowDialog() == true)
                {
                    await SaveDocumentAsync(saveDialog.FileName);
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"保存エラー: {ex.Message}");
            }
        }
        
        private async Task SaveDocumentAsync(string filePath)
        {
            if (_currentDocument == null) return;
            
            try
            {
                StatusMessage = "PDF を保存中...";
                ProgressVisibility = "Visible";
                
                // outputフォルダの作成
                var outputDir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }
                
                // PDF保存（品質設定適用）
                var success = await _pdfEditorService.SavePdfAsync(_currentDocument, filePath);
                
                if (success)
                {
                    StatusMessage = $"保存完了: {Path.GetFileName(filePath)}";
                    _currentDocument.FilePath = filePath;
                    
                    // PDF保存完了後、HEIC一時ファイルをクリーンアップ
                    CleanupAllTempFiles();
                    
                    UpdateUI();
                }
                else
                {
                    _dialogService.ShowError("PDFの保存に失敗しました");
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

        /// <summary>
        /// 全ページのHEIC一時ファイルをクリーンアップ
        /// </summary>
        private void CleanupAllTempFiles()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[CleanupAllTempFiles] Starting cleanup of HEIC temp files");
                
                foreach (var page in Pages)
                {
                    page.CleanupTempFiles();
                }
                
                System.Diagnostics.Debug.WriteLine("[CleanupAllTempFiles] Cleanup completed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CleanupAllTempFiles] Error during cleanup: {ex.Message}");
            }
        }

        [RelayCommand(CanExecute = nameof(HasSelectedPages))]
        private async Task RotateLeft()
        {
            System.Diagnostics.Debug.WriteLine("[RotateLeft] Command executed");
            System.Diagnostics.Debug.WriteLine($"[RotateLeft] HasSelectedPages: {HasSelectedPages}");
            await RotateSelectedPages(270); // 左回転 = 270度（反時計回り）
        }

        [RelayCommand(CanExecute = nameof(HasSelectedPages))]
        private async Task RotateRight()
        {
            System.Diagnostics.Debug.WriteLine("[RotateRight] Command executed");
            System.Diagnostics.Debug.WriteLine($"[RotateRight] HasSelectedPages: {HasSelectedPages}");
            await RotateSelectedPages(90); // 右回転 = 90度（時計回り）
        }

        private async Task RotateSelectedPages(int degrees)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[RotateSelectedPages] クリーン版開始: {degrees}度回転");
                
                if (_currentDocument == null)
                {
                    System.Diagnostics.Debug.WriteLine("[RotateSelectedPages] _currentDocument is null");
                    return;
                }
                
                var selectedPages = Pages.Where(p => p.IsSelected).ToList();
                System.Diagnostics.Debug.WriteLine($"[RotateSelectedPages] 選択ページ数: {selectedPages.Count}");
                
                if (!selectedPages.Any())
                {
                    System.Diagnostics.Debug.WriteLine("[RotateSelectedPages] 選択ページなし");
                    return;
                }
                
                // 現在選択されているページを保持
                var currentSelectedPage = selectedPages.FirstOrDefault();
                
                // ★シンプル化: UI同期実行（競合状態を排除）
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    System.Diagnostics.Debug.WriteLine($"[RotateSelectedPages] クリーン版UI更新開始 - {selectedPages.Count}ページ");
                    
                    // サムネイル再生成タスクを収集（非同期版のみ使用）
                    var regenerationTasks = new List<Task>();
                    
                    foreach (var pageVm in selectedPages)
                    {
                        System.Diagnostics.Debug.WriteLine($"[RotateSelectedPages] ページ {pageVm.PageNumber} 回転処理開始");
                        
                        // Core層データ更新（回転角度計算）
                        var newRotation = (pageVm.Page.Rotation + degrees) % 360;
                        if (newRotation < 0) newRotation += 360;
                        
                        pageVm.Page.Rotation = newRotation;
                        System.Diagnostics.Debug.WriteLine($"[RotateSelectedPages] ページ {pageVm.PageNumber} 新回転値: {newRotation}度");
                        
                        // ★クリーン化: 同期更新のみ（古い処理は実行しない）
                        pageVm.UpdateRotationSync();
                        
                        // 非同期サムネイル再生成タスクを収集（新しい処理のみ）
                        var task = pageVm.RegenerateThumbnailAfterRotationAsync();
                        regenerationTasks.Add(task);
                        
                        System.Diagnostics.Debug.WriteLine($"[RotateSelectedPages] ページ {pageVm.PageNumber} タスク登録完了");
                    }
                    
                    // 全サムネイル再生成完了を待機
                    System.Diagnostics.Debug.WriteLine($"[RotateSelectedPages] 全サムネイル再生成待機開始 ({regenerationTasks.Count}タスク)");
                    await Task.WhenAll(regenerationTasks);
                    System.Diagnostics.Debug.WriteLine("[RotateSelectedPages] 全サムネイル再生成完了");
                    
                    // ★シンプル化: 基本的なCollectionView更新のみ
                    System.Diagnostics.Debug.WriteLine("[RotateSelectedPages] CollectionView更新開始");
                    ForceCompleteCollectionRefresh();
                    
                    // 現在選択ページのプレビュー更新
                    if (currentSelectedPage != null)
                    {
                        UpdateCurrentPagePreview(currentSelectedPage);
                    }
                    
                    System.Diagnostics.Debug.WriteLine("[RotateSelectedPages] クリーン版処理完了");
                });
                
                // 成功メッセージ
                StatusMessage = $"{selectedPages.Count} ページを{Math.Abs(degrees)}度回転しました";
                System.Diagnostics.Debug.WriteLine($"[RotateSelectedPages] 処理成功: {selectedPages.Count}ページ {degrees}度回転完了");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RotateSelectedPages] エラー: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[RotateSelectedPages] スタックトレース: {ex.StackTrace}");
                _dialogService.ShowError($"回転エラー: {ex.Message}");
            }
        }

        
        /// <summary>
        /// 全ページの文字向きを自動補正（OCRベース）
        /// </summary>
        [RelayCommand(CanExecute = nameof(HasDocument))]
        private async Task AutoCorrectAllPagesOrientation()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[AutoCorrectAllPagesOrientationAsync] Starting auto-correction for all pages");
                
                if (Pages == null || !Pages.Any())
                {
                    StatusMessage = "補正対象のページがありません";
                    return;
                }
                
                StatusMessage = "文字を含むページを検索中...";
                ProgressVisibility = "Visible";
                
                // 文字を含むページを特定
                var pagesWithText = new List<PageViewModel>();
                var totalPages = Pages.Count;
                var processedPages = 0;
                
                foreach (var page in Pages)
                {
                    try
                    {
                        var hasText = await page.GetTextConfidenceAsync() > 30.0;
                        if (hasText)
                        {
                            pagesWithText.Add(page);
                        }
                        
                        processedPages++;
                        ProgressValue = (int)((double)processedPages / totalPages * 100);
                        StatusMessage = $"文字検出中... {processedPages}/{totalPages}ページ";
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AutoCorrectAllPagesOrientationAsync] Error checking text for page {page.PageNumber}: {ex.Message}");
                    }
                }
                
                if (!pagesWithText.Any())
                {
                    StatusMessage = "文字を含むページが見つかりませんでした";
                    await Task.Delay(2000);
                    StatusMessage = "準備完了";
                    ProgressVisibility = "Collapsed";
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"[AutoCorrectAllPagesOrientationAsync] Found {pagesWithText.Count} pages with text");
                
                // 文字向きを自動補正
                StatusMessage = $"{pagesWithText.Count}ページの文字向きを自動補正中...";
                var correctedPages = 0;
                processedPages = 0;
                
                foreach (var page in pagesWithText)
                {
                    try
                    {
                        var originalRotation = page.Rotation;
                        await page.AutoCorrectOrientationAsync();
                        
                        if (page.Rotation != originalRotation)
                        {
                            correctedPages++;
                        }
                        
                        processedPages++;
                        ProgressValue = (int)((double)processedPages / pagesWithText.Count * 100);
                        StatusMessage = $"文字向き補正中... {processedPages}/{pagesWithText.Count}ページ";
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AutoCorrectAllPagesOrientationAsync] Error correcting page {page.PageNumber}: {ex.Message}");
                    }
                }
                
                // 結果表示
                if (correctedPages > 0)
                {
                    StatusMessage = $"{correctedPages}ページの文字向きを自動補正しました";
                    
                    // UI更新
                    ForceCompleteCollectionRefresh();
                    
                    // 最初に補正されたページを選択
                    var firstCorrectedPage = pagesWithText.FirstOrDefault(p => p.Rotation != 0);
                    if (firstCorrectedPage != null)
                    {
                        UpdateCurrentPagePreview(firstCorrectedPage);
                    }
                }
                else
                {
                    StatusMessage = "全ページが既に最適な向きでした";
                }
                
                System.Diagnostics.Debug.WriteLine($"[AutoCorrectAllPagesOrientationAsync] Completed: {correctedPages}/{pagesWithText.Count} pages corrected");
                
                await Task.Delay(3000);
                StatusMessage = "準備完了";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AutoCorrectAllPagesOrientationAsync] Error: {ex.Message}");
                StatusMessage = $"自動補正エラー: {ex.Message}";
                _dialogService.ShowError($"文字向き自動補正エラー: {ex.Message}");
            }
            finally
            {
                ProgressVisibility = "Collapsed";
                ProgressValue = 0;
            }
        }
        
        /// <summary>
        /// 選択ページの文字向きを自動補正
        /// </summary>
        public async Task AutoCorrectSelectedPagesOrientationAsync()
        {
            try
            {
                var selectedPages = Pages.Where(p => p.IsSelected).ToList();
                
                if (!selectedPages.Any())
                {
                    StatusMessage = "補正対象のページが選択されていません";
                    return;
                }
                
                StatusMessage = $"{selectedPages.Count}ページの文字向きを自動補正中...";
                ProgressVisibility = "Visible";
                
                var correctedPages = 0;
                var processedPages = 0;
                
                foreach (var page in selectedPages)
                {
                    try
                    {
                        var originalRotation = page.Rotation;
                        await page.AutoCorrectOrientationAsync();
                        
                        if (page.Rotation != originalRotation)
                        {
                            correctedPages++;
                        }
                        
                        processedPages++;
                        ProgressValue = (int)((double)processedPages / selectedPages.Count * 100);
                        StatusMessage = $"文字向き補正中... {processedPages}/{selectedPages.Count}ページ";
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[AutoCorrectSelectedPagesOrientationAsync] Error correcting page {page.PageNumber}: {ex.Message}");
                    }
                }
                
                // 結果表示
                if (correctedPages > 0)
                {
                    StatusMessage = $"{correctedPages}ページの文字向きを自動補正しました";
                    
                    // UI更新
                    ForceCompleteCollectionRefresh();
                    
                    // 最初に補正されたページを選択
                    var firstCorrectedPage = selectedPages.FirstOrDefault();
                    if (firstCorrectedPage != null)
                    {
                        UpdateCurrentPagePreview(firstCorrectedPage);
                    }
                }
                else
                {
                    StatusMessage = "選択ページは既に最適な向きでした";
                }
                
                await Task.Delay(2000);
                StatusMessage = "準備完了";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AutoCorrectSelectedPagesOrientationAsync] Error: {ex.Message}");
                StatusMessage = $"自動補正エラー: {ex.Message}";
                _dialogService.ShowError($"文字向き自動補正エラー: {ex.Message}");
            }
            finally
            {
                ProgressVisibility = "Collapsed";
                ProgressValue = 0;
            }
        }
        
        /// <summary>
        /// WPF CollectionViewの完全リフレッシュ（強化版）
        /// </summary>
        private void ForceCompleteCollectionRefresh()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("[ForceCompleteCollectionRefresh] 強制コレクション更新開始");
                
                // ★新アプローチ: ObservableCollectionの構造変更を偽装して強制更新
                // WPFバインディングエンジンに「コレクションが変更された」と錯覚させる
                if (Pages.Count > 0)
                {
                    // 最後の要素を一時的に削除して即座に再追加
                    var lastPage = Pages.Last();
                    Pages.RemoveAt(Pages.Count - 1);
                    Pages.Add(lastPage);
                    
                    System.Diagnostics.Debug.WriteLine("[ForceCompleteCollectionRefresh] ObservableCollection構造変更による強制更新完了");
                }
                
                // 従来の方法も併用して確実性を高める
                var collectionView = System.Windows.Data.CollectionViewSource.GetDefaultView(Pages);
                if (collectionView != null)
                {
                    collectionView.Refresh();
                    System.Diagnostics.Debug.WriteLine("[ForceCompleteCollectionRefresh] CollectionView.Refresh() 完了");
                }
                
                OnPropertyChanged(nameof(Pages));
                System.Diagnostics.Debug.WriteLine("[ForceCompleteCollectionRefresh] 強制更新完了");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ForceCompleteCollectionRefresh] エラー: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 現在選択ページのプレビュー更新
        /// </summary>
        private void UpdateCurrentPagePreview(PageViewModel selectedPage)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateCurrentPagePreview] ページ {selectedPage.PageNumber} プレビュー更新");
                
                // 右側プレビューの強制更新
                UpdateSelectedPage(selectedPage);
                
                // 追加: 選択状態の強制リフレッシュ
                selectedPage.OnPropertyChanged(nameof(PageViewModel.IsSelected));
                selectedPage.OnPropertyChanged(nameof(PageViewModel.ThumbnailImage));
                
                System.Diagnostics.Debug.WriteLine($"[UpdateCurrentPagePreview] ページ {selectedPage.PageNumber} プレビュー更新完了");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateCurrentPagePreview] エラー: {ex.Message}");
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

        [RelayCommand(CanExecute = nameof(CanMoveUp))]
        private void MovePageUp()
        {
            System.Diagnostics.Debug.WriteLine("[MovePageUp] Command executed");
            
            if (_currentDocument == null || !CanMoveUp) return;
            
            var selectedPage = Pages.FirstOrDefault(p => p.IsSelected);
            if (selectedPage == null) return;
            
            var currentIndex = Pages.IndexOf(selectedPage);
            if (currentIndex <= 0) return;
            
            // ObservableCollectionで位置を移動
            Pages.Move(currentIndex, currentIndex - 1);
            
            // PDFドキュメント側も同じ順序に更新（PDF出力の順序を正しくするため）
            if (_currentDocument != null && currentIndex < _currentDocument.Pages.Count)
            {
                _currentDocument.MovePage(currentIndex, currentIndex - 1);
            }
            
            // ページ番号を再設定
            UpdatePageNumbers();
            
            // UI状態を更新
            UpdateSelectionState();
            
            StatusMessage = $"ページ {selectedPage.PageNumber} を上に移動しました";
            System.Diagnostics.Debug.WriteLine($"[MovePageUp] Page moved from {currentIndex + 1} to {currentIndex}");
        }

        [RelayCommand(CanExecute = nameof(CanMoveDown))]
        private void MovePageDown()
        {
            System.Diagnostics.Debug.WriteLine("[MovePageDown] Command executed");
            
            if (_currentDocument == null || !CanMoveDown) return;
            
            var selectedPage = Pages.FirstOrDefault(p => p.IsSelected);
            if (selectedPage == null) return;
            
            var currentIndex = Pages.IndexOf(selectedPage);
            if (currentIndex >= Pages.Count - 1) return;
            
            // ObservableCollectionで位置を移動
            Pages.Move(currentIndex, currentIndex + 1);
            
            // PDFドキュメント側も同じ順序に更新（PDF出力の順序を正しくするため）
            if (_currentDocument != null && currentIndex + 1 < _currentDocument.Pages.Count)
            {
                _currentDocument.MovePage(currentIndex, currentIndex + 1);
            }
            
            // ページ番号を再設定
            UpdatePageNumbers();
            
            // UI状態を更新
            UpdateSelectionState();
            
            StatusMessage = $"ページ {selectedPage.PageNumber} を下に移動しました";
            System.Diagnostics.Debug.WriteLine($"[MovePageDown] Page moved from {currentIndex + 1} to {currentIndex + 2}");
        }

        private void UpdatePageNumbers()
        {
            for (int i = 0; i < Pages.Count; i++)
            {
                Pages[i].UpdatePageNumber(i + 1);
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
                
                // 前の選択ページの監視を停止
                if (_selectedPage != null)
                {
                    _selectedPage.PropertyChanged -= OnSelectedPagePropertyChanged;
                }
                    
                _selectedPage = selectedPage;
                System.Diagnostics.Debug.WriteLine($"[UpdateSelectedPage] Selected page set to: {_selectedPage.PageNumber}");
                
                // 新しい選択ページの監視を開始
                selectedPage.PropertyChanged += OnSelectedPagePropertyChanged;
                
                // プレビューを即座に更新（待機なし、全形式統一）
                UpdatePreview(selectedPage, forceUpdate: true);
                
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

        private void OnSelectedPagePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PageViewModel.PreviewImage) && sender is PageViewModel pageViewModel)
            {
                System.Diagnostics.Debug.WriteLine($"[OnSelectedPagePropertyChanged] PreviewImage updated for page {pageViewModel.PageNumber}");
                
                // 選択中のページのみ更新（非選択ページのプレビュー更新を無視）
                if (pageViewModel.IsSelected && pageViewModel == _selectedPage)
                {
                    // PreviewImageが更新されたらCurrentPageImageを即座に更新
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (pageViewModel.PreviewImage != null)
                        {
                            CurrentPageImage = pageViewModel.PreviewImage;
                            System.Diagnostics.Debug.WriteLine($"[OnSelectedPagePropertyChanged] CurrentPageImage updated successfully for selected page {pageViewModel.PageNumber}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[OnSelectedPagePropertyChanged] PreviewImage is null for page {pageViewModel.PageNumber}, keeping current preview");
                        }
                    });
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[OnSelectedPagePropertyChanged] Ignoring PreviewImage update for non-selected page {pageViewModel.PageNumber}");
                }
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
            var imageFiles = filePaths.Where(f => IsImageFile(f)).ToList();
            if (!imageFiles.Any()) return;

            try
            {
                StatusMessage = $"{imageFiles.Count} 個の画像を変換中...";
                ProgressVisibility = "Visible";
                
                System.Diagnostics.Debug.WriteLine($"[OpenMultipleImageFilesAsync] Processing {imageFiles.Count} images");
                
                // 複数画像を1つのPDFに変換（非同期処理）
                var pdfDocument = await _imageProcessingService.ConvertImagesToPdfAsync(imageFiles);
                
                if (pdfDocument == null)
                {
                    System.Diagnostics.Debug.WriteLine("[OpenMultipleImageFilesAsync] PDF creation failed - null result");
                    StatusMessage = "画像変換に失敗しました";
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"[OpenMultipleImageFilesAsync] PDF created successfully with {pdfDocument.Pages.Count} pages");
                
                _openDocuments.Add(pdfDocument);
                
                if (_currentDocument == null)
                {
                    // UIスレッドで実行
                    SetCurrentDocument(pdfDocument);
                }
                
                UpdateUI();
                StatusMessage = $"{imageFiles.Count} 個の画像を1つのPDFに変換完了";
                
                System.Diagnostics.Debug.WriteLine("[OpenMultipleImageFilesAsync] Operation completed successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[OpenMultipleImageFilesAsync] Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[OpenMultipleImageFilesAsync] StackTrace: {ex.StackTrace}");
                
                // シンプルなエラーメッセージでDialogServiceエラーを回避
                var errorMessage = "複数画像の変換でエラーが発生しました";
                
                try
                {
                    _dialogService.ShowError(errorMessage);
                }
                catch (Exception dialogEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[OpenMultipleImageFilesAsync] Dialog error: {dialogEx.Message}");
                    // DialogServiceがエラーの場合はStatusMessageのみ更新
                }
                
                StatusMessage = "複数画像変換エラー";
            }
            finally
            {
                ProgressVisibility = "Collapsed";
                System.Diagnostics.Debug.WriteLine("[OpenMultipleImageFilesAsync] Progress visibility collapsed");
            }
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

        /// <summary>
        /// HEICファイル判定
        /// </summary>
        private static bool IsHeicFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return false;
            var extension = Path.GetExtension(filePath);
            return extension.Equals(".heic", StringComparison.OrdinalIgnoreCase) || 
                   extension.Equals(".heif", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// SkiaSharp SKBitmapの回転処理
        /// </summary>
        
    }
}