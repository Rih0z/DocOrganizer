using System;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
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
        private ImageSource? currentPageImage;
        
        /// <summary>
        /// CurrentPageImage変更時のデバッグログ出力
        /// </summary>
        partial void OnCurrentPageImageChanged(ImageSource? value)
        {
            try
            {
                AppendDebugLogSync($"[Preview] CurrentPageImage changed: {value?.GetType()?.Name ?? "null"}");
                if (value is BitmapImage bmp)
                {
                    AppendDebugLogSync($"[Preview] Image dimensions: {bmp.PixelWidth}x{bmp.PixelHeight}");
                }
            }
            catch (Exception ex)
            {
                AppendDebugLogSync($"[Preview] OnCurrentPageImageChanged error: {ex.Message}");
            }
        }

        [ObservableProperty]
        private double previewWidth = 800;

        [ObservableProperty]
        private double previewHeight = 1000;

        [ObservableProperty]
        private string zoomLevel = "100%";

        partial void OnZoomLevelChanged(string value)
        {
            if (!string.IsNullOrEmpty(value) && value.EndsWith("%"))
            {
                var zoomText = value.Replace("%", "");
                if (double.TryParse(zoomText, out var zoom))
                {
                    ApplyZoom(zoom);
                }
            }
        }



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
                // 🚨 緊急デバッグ: ファイルに出力
                await AppendDebugLogAsync($"[UpdatePreviewAsync開始] selectedPage={selectedPage?.PageNumber}, forceUpdate={forceUpdate}");
                
                if (selectedPage?.Page == null)
                {
                    await AppendDebugLogAsync("[UpdatePreviewAsync] SelectedPageがNULL - プレビュー表示不可");
                    _logger.LogWarning("[V3_Preview] SelectedPageがNULL - プレビュー表示不可");
                    CurrentPageImage = null;
                    PageInfo = "";
                    EmptyStateVisibility = "Visible";
                    return;
                }
                
                await AppendDebugLogAsync($"[UpdatePreviewAsync] _currentDocument={(_currentDocument != null ? "設定済み" : "NULL")}");
                await AppendDebugLogAsync($"[UpdatePreviewAsync] selectedPage.Page.SourceImagePath={selectedPage.Page.SourceImagePath}");
            
                _logger.LogDebug("[V3_Preview] UpdatePreviewAsync開始: PageNumber={PageNumber}, ForceUpdate={ForceUpdate}", 
                    selectedPage.PageNumber, forceUpdate);

                EmptyStateVisibility = "Collapsed";
                _selectedPage = selectedPage;

                // ページ情報更新
                UpdatePageInfo(selectedPage);

                // 🎯 V3新実装: OSS標準ImageLoaderService使用
                await LoadPreviewImageAsync(selectedPage, forceUpdate);
                
                await AppendDebugLogAsync($"[UpdatePreviewAsync完了] CurrentPageImage={CurrentPageImage != null}");
            }
            catch (Exception ex)
            {
                // プレビュー更新エラーはUIに表示しない（頻繁に呼ばれるため）
                await AppendDebugLogAsync($"[UpdatePreviewAsync例外] プレビュー更新エラー: {ex.Message}");
                await AppendDebugLogAsync($"[UpdatePreviewAsync例外] エラー詳細: {ex}");
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
            // 🚨 緊急デバッグ: ファイルに出力
            await AppendDebugLogAsync($"[LoadPreviewImageAsync開始] forceUpdate={forceUpdate}");
            await AppendDebugLogAsync($"[LoadPreviewImageAsync] pageViewModel.PreviewImage={pageViewModel.PreviewImage != null}");
            
            // V3.0.101: forceUpdateがtrueの場合は常に新しい画像を生成
            // まず、forceUpdateがtrueの場合は最新のPreviewImageを再生成
            if (forceUpdate)
            {
                await AppendDebugLogAsync("[LoadPreviewImageAsync] forceUpdate=true - PreviewImageを再生成");
                // 回転後の最新画像を生成
                await pageViewModel.LoadRightPreviewAsync();
            }
            
            // PageViewModelに既にPreviewImageがある場合はそれを使用
            if (pageViewModel.PreviewImage != null)
            {
                await AppendDebugLogAsync("[LoadPreviewImageAsync] PreviewImageを使用");
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    CurrentPageImage = pageViewModel.PreviewImage;
                    UpdatePreviewSize(pageViewModel.PreviewImage);
                });
                return;
            }

            await AppendDebugLogAsync($"[LoadPreviewImageAsync] _currentDocument確認: {(_currentDocument != null ? "存在" : "NULL")}");
            
            // 🎯 V3新実装: 高品質プレビュー生成
            if (_currentDocument != null)
            {
                var pageIndex = _currentDocument.Pages.ToList().IndexOf(pageViewModel.Page);
                await AppendDebugLogAsync($"[LoadPreviewImageAsync] pageIndex={pageIndex}");
                
                if (pageIndex >= 0)
                {
                    var page = _currentDocument.Pages[pageIndex];
                    await AppendDebugLogAsync($"[LoadPreviewImageAsync] page.SourceImagePath='{page.SourceImagePath}'");
                    await AppendDebugLogAsync($"[LoadPreviewImageAsync] ファイル存在確認: {(!string.IsNullOrEmpty(page.SourceImagePath) && System.IO.File.Exists(page.SourceImagePath))}");

                    // 元画像パスが存在する場合は画像ベースでプレビュー
                    if (!string.IsNullOrEmpty(page.SourceImagePath) && System.IO.File.Exists(page.SourceImagePath))
                    {
                        await AppendDebugLogAsync("[LoadPreviewImageAsync] 画像ベースプレビューを実行");
                        // 🎯 V3: OSS標準ImageLoaderServiceでEXIF処理
                        await LoadImageBasedPreviewAsync(page.SourceImagePath);
                    }
                    else
                    {
                        await AppendDebugLogAsync("[LoadPreviewImageAsync] PDFベースプレビューを実行");
                        // PDFベースでプレビュー生成
                        await LoadPdfBasedPreviewAsync(pageIndex);
                    }
                }
                else
                {
                    await AppendDebugLogAsync("[LoadPreviewImageAsync] エラー: pageIndexが見つからない");
                }
            }
            else
            {
                await AppendDebugLogAsync("[LoadPreviewImageAsync] エラー: _currentDocumentがNULL");
            }
            
            await AppendDebugLogAsync($"[LoadPreviewImageAsync完了] CurrentPageImage={CurrentPageImage != null}");
        }

        private async Task LoadImageBasedPreviewAsync(string imagePath)
        {
            await AppendDebugLogAsync($"[LoadImageBasedPreview開始] imagePath='{imagePath}'");
            
            try
            {
                // 🎯 V3.0.009修正: プロバイダーアーキテクチャを使用してHEIC対応
                await AppendDebugLogAsync($"[V3修正] IImageLoaderService使用開始");
                
                // プロバイダーアーキテクチャによる高品質プレビュー画像生成
                var previewImage = await _imageLoaderService.LoadHighQualityImageAsync(imagePath);
                
                if (previewImage != null)
                {
                    // UIスレッドでCurrentPageImage設定
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        try
                        {
                            CurrentPageImage = previewImage as ImageSource;
                            AppendDebugLogSync($"[V3修正] CurrentPageImage設定成功: プロバイダー経由, Type: {CurrentPageImage?.GetType()?.Name}");
                        }
                        catch (Exception ex)
                        {
                            AppendDebugLogSync($"[V3修正] CurrentPageImage設定失敗: {ex.Message}");
                            CurrentPageImage = null;
                        }
                    });
                }
                else
                {
                    await AppendDebugLogAsync($"[V3修正] プロバイダーからの画像取得失敗");
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        CurrentPageImage = null;
                    });
                }
            }
            catch (Exception ex)
            {
                await AppendDebugLogAsync($"[V3修正] 全体例外: {ex.Message}");
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    CurrentPageImage = null;
                });
            }
            
            await AppendDebugLogAsync($"[LoadImageBasedPreview完了]");
        }
        
        // 🎯 新規追加: Task.Run内用の同期版ログメソッド
        private void AppendDebugLogSync(string message)
        {
            try
            {
                DocOrganizer.Core.Logging.DebugLogger.Log(message, "PREVIEW_DEBUG");
            }
            catch
            {
                // ログ出力エラーは無視
            }
        }
        
        // 🎯 左側サムネイルと同じ変換メソッドを追加
        private BitmapImage ConvertImageSharpToBitmapImage(SixLabors.ImageSharp.Image image)
        {
            using var memoryStream = new MemoryStream();
            
            // PNG形式でメモリストリームに書き込み
            image.SaveAsPng(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            
            // WPF BitmapImageに変換
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = memoryStream;
            bitmapImage.EndInit();
            bitmapImage.Freeze(); // UIスレッド外でも使用可能にする
            
            return bitmapImage;
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
            try
            {
                // 🚨 デバッグ: CreateBitmap開始
                AppendDebugLogAsync($"[CreateBitmap] 開始: {imagePath}").Wait();
                
                // Phase 1: EXIF Orientation検出（OSS標準パターン）
                var rotation = GetRotationFromExif(imagePath);
                AppendDebugLogAsync($"[CreateBitmap] Rotation取得完了: {rotation}").Wait();

                // Phase 2: BitmapImage + WPF標準回転（OSS標準解決策）
                var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath);
                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmap.Rotation = rotation; // ← Stack Overflow実証済みパターン
                bitmap.EndInit();
                bitmap.Freeze();

                AppendDebugLogAsync($"[CreateBitmap] 成功: Width={bitmap.Width}, Height={bitmap.Height}").Wait();
                return bitmap;
            }
            catch (Exception ex)
            {
                AppendDebugLogAsync($"[CreateBitmap] 例外: {ex.Message}").Wait();
                AppendDebugLogAsync($"[CreateBitmap] 詳細: {ex}").Wait();
                
                // フォールバック: シンプルなBitmapImage作成
                try
                {
                    AppendDebugLogAsync($"[CreateBitmap] フォールバック実行").Wait();
                    var simpleBitmap = new System.Windows.Media.Imaging.BitmapImage();
                    simpleBitmap.BeginInit();
                    simpleBitmap.UriSource = new Uri(imagePath);
                    simpleBitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    simpleBitmap.EndInit();
                    simpleBitmap.Freeze();
                    AppendDebugLogAsync($"[CreateBitmap] フォールバック成功").Wait();
                    return simpleBitmap;
                }
                catch (Exception fallbackEx)
                {
                    AppendDebugLogAsync($"[CreateBitmap] フォールバック失敗: {fallbackEx.Message}").Wait();
                    return null;
                }
            }
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

        private void UpdatePreviewSize(ImageSource? image)
        {
            if (image is BitmapImage bitmapImage)
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
            
            // ✅ プレビューエリアのズーム（CurrentPageImage使用）
            if (CurrentPageImage is BitmapImage bitmap)
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

        /// <summary>
        /// 🚨 緊急デバッグ: ファイルに詳細ログを出力
        /// </summary>
        private async Task AppendDebugLogAsync(string message)
        {
            await DocOrganizer.Core.Logging.DebugLogger.LogAsync(message, "PreviewManagement");
        }

        // Events for coordination
        public event EventHandler<PreviewUpdatedEventArgs>? PreviewUpdated;
    }
}