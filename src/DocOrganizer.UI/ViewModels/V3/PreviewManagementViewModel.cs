using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocOrganizer.Application.Interfaces;
using DocOrganizer.Application.Interfaces.V3;
using DocOrganizer.Core.Models;
using Microsoft.Extensions.Logging;

namespace DocOrganizer.UI.ViewModels.V3
{
    /// <summary>
    /// 🎯 V3アーキテクチャ: プレビュー管理専用ViewModel
    /// 責務: CurrentPageImage更新、Zoom、サイズ調整のみ
    /// 目標: 200行以下、5メソッド以下
    /// </summary>
    public partial class PreviewManagementViewModel : ObservableObject
    {
        private readonly IImageLoaderService _imageLoaderService;
        private readonly IDialogService _dialogService;
        private readonly ILogger<PreviewManagementViewModel> _logger;
        private readonly IPdfEditorService _pdfEditorService;

        [ObservableProperty]
        private object? currentPageImage;

        [ObservableProperty]
        private double previewWidth = 800;

        [ObservableProperty]
        private double previewHeight = 1000;

        [ObservableProperty]
        private string zoomLevel = "100%";

        [ObservableProperty]
        private string pageInfo = "";

        [ObservableProperty]
        private string emptyStateVisibility = "Visible";

        private V3PageViewModel? _selectedPage;
        private PdfDocument? _currentDocument;

        public PreviewManagementViewModel(
            IImageLoaderService imageLoaderService,
            IDialogService dialogService,
            ILogger<PreviewManagementViewModel> logger,
            IPdfEditorService pdfEditorService)
        {
            _imageLoaderService = imageLoaderService;
            _dialogService = dialogService;
            _logger = logger;
            _pdfEditorService = pdfEditorService;
        }

        /// <summary>
        /// 選択ページのプレビュー更新
        /// </summary>
        public async Task UpdatePreviewAsync(V3PageViewModel? selectedPage, bool forceUpdate = false)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] UpdatePreviewAsync開始: selectedPage={selectedPage?.PageNumber}, forceUpdate={forceUpdate}");
                
                if (selectedPage?.Page == null)
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] SelectedPageがNULL - プレビュー表示不可");
                    _logger.LogWarning("[V3_Preview] SelectedPageがNULL - プレビュー表示不可");
                    CurrentPageImage = null;
                    PageInfo = "";
                    EmptyStateVisibility = "Visible";
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"[DEBUG] _currentDocument={(_currentDocument != null ? "設定済み" : "NULL")}");
                System.Diagnostics.Debug.WriteLine($"[DEBUG] selectedPage.Page.SourceImagePath={selectedPage.Page.SourceImagePath}");
            
                _logger.LogDebug("[V3_Preview] UpdatePreviewAsync開始: PageNumber={PageNumber}, ForceUpdate={ForceUpdate}", 
                    selectedPage.PageNumber, forceUpdate);

                EmptyStateVisibility = "Collapsed";
                _selectedPage = selectedPage;

                // ページ情報更新
                UpdatePageInfo(selectedPage);

                // 🎯 V3新実装: OSS標準ImageLoaderService使用
                await LoadPreviewImageAsync(selectedPage, forceUpdate);
                
                System.Diagnostics.Debug.WriteLine($"[DEBUG] UpdatePreviewAsync完了: CurrentPageImage={CurrentPageImage != null}");
            }
            catch (Exception ex)
            {
                // プレビュー更新エラーはUIに表示しない（頻繁に呼ばれるため）
                System.Diagnostics.Debug.WriteLine($"[PreviewManagement] プレビュー更新エラー: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[PreviewManagement] エラー詳細: {ex}");
                _logger.LogError(ex, "[V3_Preview_ERROR] UpdatePreviewAsync失敗: SelectedPage={SelectedPage}", selectedPage?.PageNumber ?? -1);
            }
        }

        /// <summary>
        /// ズームイン
        /// </summary>
        [RelayCommand]
        private void ZoomIn()
        {
            var currentZoom = GetCurrentZoomPercentage();
            var newZoom = Math.Min(currentZoom * 1.25, 500); // 最大500%
            ApplyZoom(newZoom);
        }

        /// <summary>
        /// ズームアウト
        /// </summary>
        [RelayCommand]
        private void ZoomOut()
        {
            var currentZoom = GetCurrentZoomPercentage();
            var newZoom = Math.Max(currentZoom * 0.8, 25); // 最小25%
            ApplyZoom(newZoom);
        }

        /// <summary>
        /// ウィンドウに合わせる
        /// </summary>
        [RelayCommand]
        private void FitToWindow()
        {
            if (_selectedPage?.PreviewImage is System.Windows.Media.Imaging.BitmapImage bitmap)
            {
                // 利用可能な表示領域に合わせてサイズ計算
                var availableWidth = 800; // プレビューエリアの標準幅
                var availableHeight = 1000; // プレビューエリアの標準高さ

                var scaleX = availableWidth / bitmap.PixelWidth;
                var scaleY = availableHeight / bitmap.PixelHeight;
                var scale = Math.Min(scaleX, scaleY);

                PreviewWidth = bitmap.PixelWidth * scale;
                PreviewHeight = bitmap.PixelHeight * scale;

                var zoomPercentage = scale * 100;
                ZoomLevel = $"{zoomPercentage:F0}%";
            }
        }

        // Private helper methods
        private async Task LoadPreviewImageAsync(V3PageViewModel pageViewModel, bool forceUpdate)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] LoadPreviewImageAsync開始: forceUpdate={forceUpdate}");
            System.Diagnostics.Debug.WriteLine($"[DEBUG] pageViewModel.PreviewImage={pageViewModel.PreviewImage != null}");
            
            // PageViewModelに既にPreviewImageがある場合はそれを使用
            if (!forceUpdate && pageViewModel.PreviewImage != null)
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] 既存のPreviewImageを使用");
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    CurrentPageImage = pageViewModel.PreviewImage;
                    UpdatePreviewSize(pageViewModel.PreviewImage);
                });
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[DEBUG] _currentDocument確認: {(_currentDocument != null ? "存在" : "NULL")}");
            
            // 🎯 V3新実装: 高品質プレビュー生成
            if (_currentDocument != null)
            {
                var pageIndex = _currentDocument.Pages.ToList().IndexOf(pageViewModel.Page);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] pageIndex={pageIndex}");
                
                if (pageIndex >= 0)
                {
                    var page = _currentDocument.Pages[pageIndex];
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] page.SourceImagePath='{page.SourceImagePath}'");
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] ファイル存在確認: {(!string.IsNullOrEmpty(page.SourceImagePath) && System.IO.File.Exists(page.SourceImagePath))}");

                    // 元画像パスが存在する場合は画像ベースでプレビュー
                    if (!string.IsNullOrEmpty(page.SourceImagePath) && System.IO.File.Exists(page.SourceImagePath))
                    {
                        System.Diagnostics.Debug.WriteLine("[DEBUG] 画像ベースプレビューを実行");
                        // 🎯 V3: OSS標準ImageLoaderServiceでEXIF処理
                        await LoadImageBasedPreviewAsync(page.SourceImagePath);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("[DEBUG] PDFベースプレビューを実行");
                        // PDFベースでプレビュー生成
                        await LoadPdfBasedPreviewAsync(pageIndex);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] エラー: pageIndexが見つからない");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[DEBUG] エラー: _currentDocumentがNULL");
            }
            
            System.Diagnostics.Debug.WriteLine($"[DEBUG] LoadPreviewImageAsync完了: CurrentPageImage={CurrentPageImage != null}");
        }

        private async Task LoadImageBasedPreviewAsync(string imagePath)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] LoadImageBasedPreviewAsync開始: imagePath='{imagePath}'");
            
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] BitmapImage作成開始");
                    // 🎯 V3: OSS標準BitmapImage.Rotation使用
                    var bitmap = CreateBitmapWithOSSStandardRotation(imagePath);
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] BitmapImage作成完了: bitmap={bitmap != null}");
                    
                    CurrentPageImage = bitmap;
                    UpdatePreviewSize(bitmap);
                    
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] CurrentPageImage設定完了: CurrentPageImage={CurrentPageImage != null}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PreviewManagement] 画像プレビューエラー: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"[PreviewManagement] 画像プレビューエラー詳細: {ex}");
                }
            });
        }

        private async Task LoadPdfBasedPreviewAsync(int pageIndex)
        {
            System.Diagnostics.Debug.WriteLine($"[DEBUG] LoadPdfBasedPreviewAsync開始: pageIndex={pageIndex}");
            
            try
            {
                // 高品質プレビューを生成（スケール3.0倍）
                System.Diagnostics.Debug.WriteLine("[DEBUG] GetPagePreviewAsync呼び出し開始");
                var previewBitmap = await _pdfEditorService.GetPagePreviewAsync(_currentDocument!, pageIndex, 3.0f);
                System.Diagnostics.Debug.WriteLine($"[DEBUG] GetPagePreviewAsync完了: previewBitmap={previewBitmap != null}");

                if (previewBitmap != null)
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        try
                        {
                            System.Diagnostics.Debug.WriteLine("[DEBUG] SKBitmap→PNG変換開始");
                            // SkiaSharpのSKBitmapを無圧縮PNG形式で変換
                            using var data = previewBitmap.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                            var bitmap = CreateBitmapFromBytes(data.ToArray());
                            System.Diagnostics.Debug.WriteLine($"[DEBUG] PNG変換完了: bitmap={bitmap != null}");

                            CurrentPageImage = bitmap;
                            UpdatePreviewSize(bitmap);
                            
                            System.Diagnostics.Debug.WriteLine($"[DEBUG] CurrentPageImage設定完了: CurrentPageImage={CurrentPageImage != null}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[PreviewManagement] PDFプレビューエラー: {ex.Message}");
                            System.Diagnostics.Debug.WriteLine($"[PreviewManagement] PDFプレビューエラー詳細: {ex}");
                        }
                    });

                    previewBitmap.Dispose();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[DEBUG] エラー: previewBitmapがNULL");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PreviewManagement] PDF処理エラー: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[PreviewManagement] PDF処理エラー詳細: {ex}");
            }
        }

        // 🎯 V3新実装: OSS標準回転処理
        private System.Windows.Media.Imaging.BitmapSource CreateBitmapWithOSSStandardRotation(string imagePath)
        {
            // Phase 1: EXIF Orientation検出（OSS標準パターン）
            var rotation = GetRotationFromExif(imagePath);

            // Phase 2: BitmapImage + WPF標準回転（OSS標準解決策）
            var bitmap = new System.Windows.Media.Imaging.BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(imagePath);
            bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
            bitmap.Rotation = rotation; // ← Stack Overflow実証済みパターン
            bitmap.EndInit();
            bitmap.Freeze();

            return bitmap;
        }

        private System.Windows.Media.Imaging.Rotation GetRotationFromExif(string imagePath)
        {
            try
            {
                using var stream = new System.IO.FileStream(imagePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                var frame = System.Windows.Media.Imaging.BitmapFrame.Create(stream, 
                    System.Windows.Media.Imaging.BitmapCreateOptions.DelayCreation, 
                    System.Windows.Media.Imaging.BitmapCacheOption.None);
                var metadata = frame.Metadata as System.Windows.Media.Imaging.BitmapMetadata;

                if (metadata?.ContainsQuery("System.Photo.Orientation") == true)
                {
                    var orientationValue = metadata.GetQuery("System.Photo.Orientation");
                    if (orientationValue != null)
                    {
                        var orientation = (ushort)orientationValue;
                        return orientation switch
                        {
                            6 => System.Windows.Media.Imaging.Rotation.Rotate90,   // 右90度回転
                            3 => System.Windows.Media.Imaging.Rotation.Rotate180,  // 180度回転
                            8 => System.Windows.Media.Imaging.Rotation.Rotate270,  // 左90度回転
                            _ => System.Windows.Media.Imaging.Rotation.Rotate0     // 回転なし
                        };
                    }
                }
            }
            catch
            {
                // EXIF読み取り失敗時は回転なしで続行
            }
            return System.Windows.Media.Imaging.Rotation.Rotate0;
        }

        private System.Windows.Media.Imaging.BitmapSource CreateBitmapFromBytes(byte[] imageData)
        {
            using var stream = new System.IO.MemoryStream(imageData);
            var bitmap = new System.Windows.Media.Imaging.BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = stream;
            bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

        private void UpdatePreviewSize(object? image)
        {
            if (image is System.Windows.Media.Imaging.BitmapImage bitmapImage)
            {
                PreviewWidth = bitmapImage.PixelWidth;
                PreviewHeight = bitmapImage.PixelHeight;
            }
        }

        private void UpdatePageInfo(V3PageViewModel pageViewModel)
        {
            // ページ情報の更新
            PageInfo = $"ページ {pageViewModel.PageNumber}";
        }

        private double GetCurrentZoomPercentage()
        {
            var zoomText = ZoomLevel.Replace("%", "");
            if (double.TryParse(zoomText, out var zoom))
            {
                return zoom;
            }
            return 100.0;
        }

        private void ApplyZoom(double zoomPercentage)
        {
            ZoomLevel = $"{zoomPercentage:F0}%";

            if (_selectedPage?.PreviewImage is System.Windows.Media.Imaging.BitmapImage bitmap)
            {
                var scale = zoomPercentage / 100.0;
                PreviewWidth = bitmap.PixelWidth * scale;
                PreviewHeight = bitmap.PixelHeight * scale;
            }
        }

        // Public methods for external coordination
        public void SetCurrentDocument(PdfDocument? document)
        {
            _currentDocument = document;
        }

        public void ClearPreview()
        {
            CurrentPageImage = null;
            PageInfo = "";
            EmptyStateVisibility = "Visible";
            _selectedPage = null;
        }

        // Events for coordination
        public event EventHandler<PreviewUpdatedEventArgs>? PreviewUpdated;
    }
}