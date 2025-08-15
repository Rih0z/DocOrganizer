using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocOrganizer.Application.Interfaces;
using DocOrganizer.Core.Models;

namespace DocOrganizer.UI.ViewModels.V3
{
    /// <summary>
    /// 🎯 V3アーキテクチャ: プレビュー管理専用ViewModel
    /// 責務: CurrentPageImage更新、Zoom、サイズ調整のみ
    /// 目標: 200行以下、5メソッド以下
    /// </summary>
    public partial class PreviewManagementViewModel : ObservableObject
    {
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

        private PageViewModel? _selectedPage;
        private PdfDocument? _currentDocument;

        public PreviewManagementViewModel(IPdfEditorService pdfEditorService)
        {
            _pdfEditorService = pdfEditorService;
        }

        /// <summary>
        /// 選択ページのプレビュー更新
        /// </summary>
        public async Task UpdatePreviewAsync(PageViewModel? selectedPage, bool forceUpdate = false)
        {
            try
            {
                if (selectedPage?.Page == null)
                {
                    CurrentPageImage = null;
                    PageInfo = "";
                    EmptyStateVisibility = "Visible";
                    return;
                }

                EmptyStateVisibility = "Collapsed";
                _selectedPage = selectedPage;

                // ページ情報更新
                UpdatePageInfo(selectedPage);

                // 🎯 V3新実装: OSS標準ImageLoaderService使用
                await LoadPreviewImageAsync(selectedPage, forceUpdate);
            }
            catch (Exception ex)
            {
                // プレビュー更新エラーはUIに表示しない（頻繁に呼ばれるため）
                System.Diagnostics.Debug.WriteLine($"[PreviewManagement] プレビュー更新エラー: {ex.Message}");
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
        private async Task LoadPreviewImageAsync(PageViewModel pageViewModel, bool forceUpdate)
        {
            // PageViewModelに既にPreviewImageがある場合はそれを使用
            if (!forceUpdate && pageViewModel.PreviewImage != null)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    CurrentPageImage = pageViewModel.PreviewImage;
                    UpdatePreviewSize(pageViewModel.PreviewImage);
                });
                return;
            }

            // 🎯 V3新実装: 高品質プレビュー生成
            if (_currentDocument != null)
            {
                var pageIndex = _currentDocument.Pages.ToList().IndexOf(pageViewModel.Page);
                if (pageIndex >= 0)
                {
                    var page = _currentDocument.Pages[pageIndex];

                    // 元画像パスが存在する場合は画像ベースでプレビュー
                    if (!string.IsNullOrEmpty(page.SourceImagePath) && System.IO.File.Exists(page.SourceImagePath))
                    {
                        // 🎯 V3: OSS標準ImageLoaderServiceでEXIF処理
                        await LoadImageBasedPreviewAsync(page.SourceImagePath);
                    }
                    else
                    {
                        // PDFベースでプレビュー生成
                        await LoadPdfBasedPreviewAsync(pageIndex);
                    }
                }
            }
        }

        private async Task LoadImageBasedPreviewAsync(string imagePath)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                try
                {
                    // 🎯 V3: OSS標準BitmapImage.Rotation使用
                    var bitmap = CreateBitmapWithOSSStandardRotation(imagePath);
                    CurrentPageImage = bitmap;
                    UpdatePreviewSize(bitmap);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[PreviewManagement] 画像プレビューエラー: {ex.Message}");
                }
            });
        }

        private async Task LoadPdfBasedPreviewAsync(int pageIndex)
        {
            try
            {
                // 高品質プレビューを生成（スケール3.0倍）
                var previewBitmap = await _pdfEditorService.GetPagePreviewAsync(_currentDocument!, pageIndex, 3.0f);

                if (previewBitmap != null)
                {
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        try
                        {
                            // SkiaSharpのSKBitmapを無圧縮PNG形式で変換
                            using var data = previewBitmap.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                            var bitmap = CreateBitmapFromBytes(data.ToArray());

                            CurrentPageImage = bitmap;
                            UpdatePreviewSize(bitmap);
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[PreviewManagement] PDFプレビューエラー: {ex.Message}");
                        }
                    });

                    previewBitmap.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PreviewManagement] PDF処理エラー: {ex.Message}");
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

        private void UpdatePageInfo(PageViewModel pageViewModel)
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
        public event EventHandler<object?>? PreviewUpdated;
    }
}