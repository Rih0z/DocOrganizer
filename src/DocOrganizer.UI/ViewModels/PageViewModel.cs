using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DocOrganizer.Core.Models;
using DocOrganizer.Application.Interfaces;
using SkiaSharp;
using ImageMagick;
using System.Windows.Media.Imaging;

namespace DocOrganizer.UI.ViewModels
{
    public partial class PageViewModel : ObservableObject, IDisposable
    {
        private readonly PdfPage _page;
        private readonly IImageProcessingService? _imageProcessingService;
        private readonly ITextOrientationService? _textOrientationService;
        
        // プロパティ変更通知を外部から呼び出せるようにする
        public new void OnPropertyChanged(string? propertyName)
        {
            base.OnPropertyChanged(propertyName);
        }
        
        /// <summary>
        /// 対応するPDFページ
        /// </summary>
        public PdfPage Page => _page;
        
        [ObservableProperty]
        private int pageNumber;
        
        [ObservableProperty]
        private bool isSelected;
        
        [ObservableProperty]
        private object? thumbnailImage;
        
        [ObservableProperty]
        private object? previewImage;
        
        [ObservableProperty]
        private int rotation;

        public PageViewModel(PdfPage page, IImageProcessingService? imageProcessingService = null, ITextOrientationService? textOrientationService = null)
        {
            _page = page;
            _imageProcessingService = imageProcessingService;
            _textOrientationService = textOrientationService;
            pageNumber = page.PageNumber;
            rotation = page.Rotation;
            
            // TODO: 実際のサムネイル画像を生成
            LoadThumbnail();
        }

        public void LoadThumbnail()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[LoadThumbnail] ページ {_page.PageNumber} 開始 - Rotation: {_page.Rotation}");
                
                // HEICファイルの場合は既存のサムネイルを無視して強制再生成
                bool isSourceHeic = !string.IsNullOrEmpty(_page.SourceImagePath) && 
                                   (System.IO.Path.GetExtension(_page.SourceImagePath).Equals(".heic", StringComparison.OrdinalIgnoreCase) ||
                                    System.IO.Path.GetExtension(_page.SourceImagePath).Equals(".heif", StringComparison.OrdinalIgnoreCase));
                
                if (isSourceHeic && System.IO.File.Exists(_page.SourceImagePath))
                {
                    System.Diagnostics.Debug.WriteLine($"[LoadThumbnail] HEIC強制再生成: {_page.SourceImagePath}");
                    // キャッシュをクリア
                    ClearOptimizedCache();
                    _ = Task.Run(() => LoadThumbnailFromImage());
                    return;
                }
                
                // まずPdfPageに既にサムネイル画像があるか確認（非HEIC画像の場合のみ）
                if (_page.ThumbnailImage != null && !isSourceHeic)
                {
                    System.Diagnostics.Debug.WriteLine($"[LoadThumbnail] PDF既存サムネイル使用");
                    LoadThumbnailFromPdfPage();
                }
                // 画像ファイルから直接サムネイルを生成（HEIC以外）
                else if (!string.IsNullOrEmpty(_page.SourceImagePath) && System.IO.File.Exists(_page.SourceImagePath))
                {
                    System.Diagnostics.Debug.WriteLine($"[LoadThumbnail] 画像ファイルから生成: {_page.SourceImagePath}");
                    _ = Task.Run(() => LoadThumbnailFromImage());
                }
                // PDFページの場合
                else if (_page.ThumbnailImage == null && _page.PageNumber > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[LoadThumbnail] PDFページサムネイル待機中");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[LoadThumbnail] サムネイルなし - プレースホルダー生成");
                    // サムネイルがない場合はプレースホルダーを生成
                    GenerateRotatedPlaceholder();
                }
                
                // [ObservableProperty]による自動PropertyChanged通知に依存
                // [ObservableProperty]自動通知に依存
            }
            catch (Exception ex)
            {
                // サムネイル読み込みエラーをログに記録
                System.Diagnostics.Debug.WriteLine($"[LoadThumbnail] エラー Page {_page.PageNumber}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[LoadThumbnail] スタックトレース: {ex.StackTrace}");
                ThumbnailImage = null;
                PreviewImage = null;
            }
        }

        /// <summary>
        /// 最適化されたキャッシュをクリアする
        /// </summary>
        private void ClearOptimizedCache()
        {
            try
            {
                _optimizedThumbnailCache = null;
                _optimizedPreviewCache = null;
                
                // ★修正案B: WPF BitmapImageキャッシュも強制クリア
                if (ThumbnailImage is System.Windows.Media.Imaging.BitmapImage bitmapImage)
                {
                    bitmapImage.StreamSource?.Dispose();
                }
                
                // ★修正案B: UI強制更新
                ThumbnailImage = null;
                
                System.Diagnostics.Debug.WriteLine($"[ClearOptimizedCache] キャッシュクリア完了（修正版B - WPFキャッシュ含む）");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ClearOptimizedCache] エラー: {ex.Message}");
            }
        }
        
        private void LoadThumbnailFromPdfPage()
        {
            try
            {
                if (_page.ThumbnailImage != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[LoadThumbnailFromPdfPage] PDFページのサムネイル変換 - Size: {_page.ThumbnailImage.Width}x{_page.ThumbnailImage.Height}");
                    
                    // SkiaSharpのSKBitmapをWPFで表示可能な形式に変換
                    using var data = _page.ThumbnailImage.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                    var stream = new System.IO.MemoryStream(data.ToArray());
                    
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = stream;
                    bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    // ⭐テスト: CreateOptionsを一時的に無効化して表示確認
                    // bitmap.CreateOptions = System.Windows.Media.Imaging.BitmapCreateOptions.IgnoreImageCache;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    
                    ThumbnailImage = bitmap;
                    
                    // ⭐修正: PreviewImage設定を完全削除（ドキュメント通り）
                    // 理由: PageViewModelでPreviewImageを設定すると右側の高解像度プレビューが劣化する
                    // → MainViewModelで独自に高解像度プレビューを生成する必要がある
                    
                    System.Diagnostics.Debug.WriteLine($"[LoadThumbnailFromPdfPage] 左側サムネイルのみ設定完了 - PreviewImageは右側で独自生成");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadThumbnailFromPdfPage] エラー: {ex.Message}");
            }
        }
        
        private System.Threading.CancellationTokenSource? _loadThumbnailCts;
        private System.Threading.CancellationTokenSource? _loadPreviewCts; // プレビュー読み込み専用キャンセレーション
        private string? _heicTempJpegPath; // HEIC変換時の一時ファイルパス（PDF発行まで保持）
                // 🚀 Phase 2最適化: 静的キャッシュ廃止・WeakReference活用
        private WeakReference<byte[]>? _optimizedThumbnailCache; // 最適化サムネイルキャッシュ（GC対応）
        private WeakReference<System.Windows.Media.Imaging.BitmapSource>? _optimizedPreviewCache; // 最適化プレビューキャッシュ（GC対応） // HEICファイルパス → JPEGパスのキャッシュ
                private readonly object _heicProcessingLock = new object(); // HEIC処理の排他制御（インスタンス別）

        private async void LoadThumbnailFromImage()
        {
            // 前の読み込み処理をキャンセル
            _loadThumbnailCts?.Cancel();
            _loadThumbnailCts = new System.Threading.CancellationTokenSource();
            var cancellationToken = _loadThumbnailCts.Token;
            
            try
            {
                // キャンセレーションチェック
                if (cancellationToken.IsCancellationRequested)
                    return;
                
                string imagePathToLoad = _page.SourceImagePath;
                bool isHeic = Path.GetExtension(imagePathToLoad).Equals(".heic", StringComparison.OrdinalIgnoreCase) ||
                             Path.GetExtension(imagePathToLoad).Equals(".heif", StringComparison.OrdinalIgnoreCase);
                
                if (isHeic)
                {
                    System.Diagnostics.Debug.WriteLine($"[LoadThumbnailFromImage] HEIC最適化処理開始: {Path.GetFileName(imagePathToLoad)}");
                    
                    // 🚀 Phase 2最適化: HEIC処理統一・キャッシュ活用
                    await ProcessHeicOptimizedAsync(imagePathToLoad, cancellationToken);
                    return;
                }
                
                // 通常の画像ファイル処理（HEIC以外）
                await ProcessStandardImageAsync(imagePathToLoad, cancellationToken);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadThumbnailFromImage] 最適化エラー: {ex.Message}");
                
                // エラー発生時は基本処理にフォールバック
                await ProcessImageFallbackAsync(_page.SourceImagePath, cancellationToken);
            }
        }

        /// <summary>
        /// HEIC処理の最適化版（キャッシュ活用・2重変換排除）
        /// </summary>
        private async Task ProcessHeicOptimizedAsync(string heicPath, CancellationToken cancellationToken)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[ProcessHeicOptimizedAsync] ⭐EXIF完全削除版 HEIC左側サムネイル専用処理開始: {Path.GetFileName(heicPath)}");
                
                // ⭐最終修正: HEIC画像もEXIF情報を完全削除してWPF用PNG生成
                var exifFreeImageBytes = await _imageProcessingService.GenerateExifFreeImageForWpfAsync(heicPath, 150, 200);
                
                if (cancellationToken.IsCancellationRequested || exifFreeImageBytes == null)
                    return;
                
                // ⭐修正: 左側ThumbnailImageのみ設定（PreviewImageは右側で独自生成）
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;
                        
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
                        
                        ThumbnailImage = bitmap; // 左側サムネイル専用
                        // ⭐修正: PreviewImageは設定しない（右側で独自に高解像度生成）
                        
                        // WeakReferenceキャッシュはEXIF削除版データで保存
                        _optimizedThumbnailCache = new WeakReference<byte[]>(exifFreeImageBytes);
                        
                        System.Diagnostics.Debug.WriteLine($"[ProcessHeicOptimizedAsync] ⭐EXIF削除版 左側HEICサムネイル完了 - Size: {bitmap.PixelWidth}x{bitmap.PixelHeight}: {Path.GetFileName(heicPath)}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ProcessHeicOptimizedAsync] WPF変換エラー: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ProcessHeicOptimizedAsync] エラー: {ex.Message}");
                // エラー時は元の処理にフォールバック
                await ProcessImageFallbackAsync(heicPath, cancellationToken);
            }
        }
        
        /// <summary>
        /// 通常画像ファイルの最適化処理
        /// </summary>
        private async Task ProcessStandardImageAsync(string imagePath, CancellationToken cancellationToken)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[ProcessStandardImageAsync] ⭐EXIF完全削除版 左側サムネイル専用処理開始: {Path.GetFileName(imagePath)}");
                
                // ⭐最終修正: EXIF情報を完全削除してWPF用PNG生成（90度回転問題根本解決）
                var exifFreeImageBytes = await _imageProcessingService.GenerateExifFreeImageForWpfAsync(imagePath, 150, 200);
                
                if (cancellationToken.IsCancellationRequested || exifFreeImageBytes == null)
                    return;
                
                // ⭐修正: 左側ThumbnailImageのみ設定（PreviewImageは右側で独自生成）
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;
                        
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
                        
                        ThumbnailImage = bitmap; // 左側サムネイル専用
                        // ⭐修正: PreviewImageは設定しない（右側で独自に高解像度生成）
                        
                        System.Diagnostics.Debug.WriteLine($"[ProcessStandardImageAsync] ⭐EXIF削除版 左側サムネイル完了 - Size: {bitmap.PixelWidth}x{bitmap.PixelHeight}: {Path.GetFileName(imagePath)}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ProcessStandardImageAsync] WPF変換エラー: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ProcessStandardImageAsync] エラー: {ex.Message}");
                // エラー時は元の処理にフォールバック
                await ProcessImageFallbackAsync(imagePath, cancellationToken);
            }
        }
        
        /// <summary>
        /// フォールバック処理（エラー時の基本処理）
        /// </summary>
        private async Task ProcessImageFallbackAsync(string imagePath, CancellationToken cancellationToken)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[ProcessImageFallbackAsync] フォールバック処理（左側サムネイル専用）: {Path.GetFileName(imagePath)}");
                
                // ⭐修正: 左側サムネイル専用（150x200）
                var bitmap = await _imageProcessingService.GenerateHighQualityPreviewAsync(imagePath, 150, 200);
                
                if (cancellationToken.IsCancellationRequested || bitmap == null)
                    return;
                
                // ⭐修正: 回転処理を無効化（左右統一のため）
                var finalBitmap = bitmap;
                // ⭐修正完了: フォールバック処理でも回転処理をスキップ
                System.Diagnostics.Debug.WriteLine($"[ProcessImageFallbackAsync] 回転処理スキップ - Rotation={_page.Rotation}度");
                
                // ⭐修正: 左側ThumbnailImageのみ設定（PreviewImageは右側で独自生成）
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;
                        
                    try
                    {
                        // 左側サムネイル用のWPF BitmapImage変換
                        using var data = finalBitmap.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                        var thumbnailImage = new System.Windows.Media.Imaging.BitmapImage();
                        thumbnailImage.BeginInit();
                        thumbnailImage.StreamSource = new System.IO.MemoryStream(data.ToArray());
                        thumbnailImage.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                        // ⭐テスト: CreateOptionsを一時的に無効化して表示確認
                        // thumbnailImage.CreateOptions = System.Windows.Media.Imaging.BitmapCreateOptions.IgnoreImageCache;
                        thumbnailImage.EndInit();
                        thumbnailImage.Freeze();
                        
                        ThumbnailImage = thumbnailImage; // 左側サムネイル専用
                        // ⭐修正: PreviewImageは設定しない（右側で独自に高解像度生成）
                        
                        System.Diagnostics.Debug.WriteLine($"[ProcessImageFallbackAsync] フォールバック左側サムネイル完了: {Path.GetFileName(imagePath)}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ProcessImageFallbackAsync] WPF変換エラー: {ex.Message}");
                    }
                });
                
                // メモリ適切解放
                if (finalBitmap != bitmap)
                {
                    finalBitmap?.Dispose();
                }
                bitmap?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ProcessImageFallbackAsync] フォールバックエラー: {ex.Message}");
            }
        }
        
        /// <summary>
        /// キャッシュされたサムネイルの表示
        /// </summary>
        private void DisplayCachedThumbnail(byte[] thumbnailData)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var bitmap = CreateBitmapFromBytes(thumbnailData);
                ThumbnailImage = bitmap;
                // PreviewImageは設定せず、高品質プレビューはMainViewModelで生成
                // [ObservableProperty]自動通知に依存
            });
        }
        
        /// <summary>
        /// バイト配列からBitmapSourceを作成
        /// </summary>
        private BitmapSource CreateBitmapFromBytes(byte[] imageData)
        {
            using var stream = new System.IO.MemoryStream(imageData);
            var bitmap = new System.Windows.Media.Imaging.BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = stream;
            bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
            // ⭐根本解決: Rotationプロパティを一切設定せず、画像をそのまま表示
            bitmap.CreateOptions = System.Windows.Media.Imaging.BitmapCreateOptions.IgnoreImageCache;
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }
        
        /// <summary>
        /// プレビューはサムネイルと同じ処理で統一 - HEIC/JPEG同等処理
        /// </summary>
        private void LoadPreviewFromImage()
        {
            System.Diagnostics.Debug.WriteLine($"[LoadPreviewFromImage] プレビューはサムネイル処理で統一済み - HEIC/JPEG同等処理");
            // LoadThumbnailFromImage()で既にPreviewImageも設定されているため、重複処理は不要
        }

        // 既存のメソッドは維持
        public void UpdateRotationSync()
        {
            try
            {
                // 回転値を更新
                Rotation = _page.Rotation;
                System.Diagnostics.Debug.WriteLine($"[UpdateRotationSync] ページ {_page.PageNumber} 回転更新: {_page.Rotation}度");
                
                // ★修正: キャッシュクリアのみ実行、サムネイル再生成は削除
                ClearOptimizedCache();
                
                System.Diagnostics.Debug.WriteLine($"[UpdateRotationSync] ページ {_page.PageNumber} キャッシュクリア完了 - サムネイル再生成はRegenerateThumbnailAfterRotationAsyncに委任");
                
                // ★削除: LoadThumbnail()呼び出しを完全削除（競合状態の原因）
                // 理由: RegenerateThumbnailAfterRotationAsync()と重複実行され、古い状態で上書きされる
                
                // プロパティ変更通知は回転値のみ（サムネイル更新は非同期処理完了時に実行）
                OnPropertyChanged(nameof(Rotation));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateRotationSync] エラー: {ex.Message}");
            }
        }

        
        /// <summary>
        /// 文字向きを自動検出・補正する（OCRベース）
        /// </summary>
        public async Task AutoCorrectOrientationAsync()
        {
            if (_textOrientationService == null)
            {
                System.Diagnostics.Debug.WriteLine("[AutoCorrectOrientationAsync] TextOrientationService not available");
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine($"[AutoCorrectOrientationAsync] Starting auto-correction for page {_page.PageNumber}");
                
                // 文字が読み取れるかチェック
                var hasText = await _textOrientationService.HasReadableTextAsync(_page.SourceImagePath);
                if (!hasText)
                {
                    System.Diagnostics.Debug.WriteLine($"[AutoCorrectOrientationAsync] No readable text found in page {_page.PageNumber}");
                    return;
                }
                
                // 最適な向きを検出（並列処理で高速化）
                var optimalRotation = await _textOrientationService.DetectOptimalOrientationParallelAsync(_page.SourceImagePath);
                
                System.Diagnostics.Debug.WriteLine($"[AutoCorrectOrientationAsync] Page {_page.PageNumber}: Current={_page.Rotation}°, Optimal={optimalRotation}°");
                
                if (optimalRotation != _page.Rotation)
                {
                    // 回転値を更新
                    _page.Rotation = optimalRotation;
                    Rotation = optimalRotation;
                    
                    // サムネイル再生成
                    await RegenerateThumbnailAfterRotationAsync();
                    
                    System.Diagnostics.Debug.WriteLine($"[AutoCorrectOrientationAsync] Page {_page.PageNumber} corrected to {optimalRotation}°");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[AutoCorrectOrientationAsync] Page {_page.PageNumber} already in optimal orientation");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AutoCorrectOrientationAsync] Error for page {_page.PageNumber}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 文字認識信頼度を取得（デバッグ用）
        /// </summary>
        public async Task<double> GetTextConfidenceAsync(int rotationDegrees = 0)
        {
            if (_textOrientationService == null)
                return 0.0;
                
            try
            {
                return await _textOrientationService.GetTextConfidenceAsync(_page.SourceImagePath, rotationDegrees);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetTextConfidenceAsync] Error: {ex.Message}");
                return 0.0;
            }
        }

        /// <summary>
        /// 回転後のサムネイル強制再生成
        /// </summary>
        [Obsolete("非同期版RegenerateThumbnailAfterRotationAsync()を使用してください", true)]
        public void RegenerateThumbnailAfterRotation()
        {
            throw new InvalidOperationException("この同期版メソッドは廃止されました。RegenerateThumbnailAfterRotationAsync()を使用してください。");
        }

        
        /// <summary>
        /// 回転後のサムネイル強制再生成（非同期版）
        /// </summary>
        public async Task RegenerateThumbnailAfterRotationAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[RegenerateThumbnailAfterRotationAsync] ページ {PageNumber} 回転角度 {_page.Rotation}° - 非同期版開始");
                
                // 1. 全キャッシュの完全削除
                ClearAllImageCaches();
                
                // 2. WPF Dispatcher上で確実にnull化
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    // 古いBitmapImageリソースを解放
                    if (ThumbnailImage is System.Windows.Media.Imaging.BitmapImage oldBitmap)
                    {
                        try
                        {
                            oldBitmap.StreamSource?.Dispose();
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[RegenerateThumbnailAfterRotationAsync] Bitmap解放エラー: {ex.Message}");
                        }
                    }
                    
                    // 確実にnull設定 - [ObservableProperty]自動通知のみ
                    ThumbnailImage = null;
                    
                    System.Diagnostics.Debug.WriteLine($"[RegenerateThumbnailAfterRotationAsync] ページ {PageNumber} null化完了");
                });
                
                // 3. 新しいサムネイル生成（回転角度を考慮）
                try
                {
                    System.Diagnostics.Debug.WriteLine($"[RegenerateThumbnailAfterRotationAsync] ページ {PageNumber} サムネイル再生成開始");
                    
                    // 回転角度を明示的に渡してサムネイル生成
                    await GenerateThumbnailWithRotation(_page.SourceImagePath ?? "", _page.Rotation);
                    
                    // 最終更新 - [ObservableProperty]自動通知のみ（手動通知削除）
                    System.Diagnostics.Debug.WriteLine($"[RegenerateThumbnailAfterRotationAsync] ページ {PageNumber} 最終更新完了");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[RegenerateThumbnailAfterRotationAsync] サムネイル生成エラー: {ex.Message}");
                    
                    // エラー時のフォールバック処理
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        FallbackThumbnailRegeneration();
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RegenerateThumbnailAfterRotationAsync] 致命的エラー: {ex.Message}");
                
                // 致命的エラー時の最終フォールバック
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    FallbackThumbnailRegeneration();
                });
            }
        }
        
        /// <summary>
        /// 全画像キャッシュの完全削除
        /// </summary>
        private void ClearAllImageCaches()
        {
            try
            {
                // WeakReference キャッシュクリア
                _optimizedThumbnailCache = null;
                _optimizedPreviewCache = null;
                
                // 強制ガベージコレクション
                GC.Collect();
                GC.WaitForPendingFinalizers();
                
                System.Diagnostics.Debug.WriteLine($"[ClearAllImageCaches] ページ {PageNumber} 全キャッシュクリア完了");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ClearAllImageCaches] エラー: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 回転角度を考慮したサムネイル生成
        /// </summary>
        private async Task GenerateThumbnailWithRotation(string imagePath, int rotationAngle)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[GenerateThumbnailWithRotation] 左側サムネイル専用処理開始: {Path.GetFileName(imagePath)}, 回転: {rotationAngle}度");
                
                // ⭐修正: 左側サムネイル専用（150x200）
                var bitmap = await _imageProcessingService.GenerateHighQualityPreviewAsync(imagePath, 150, 200);
                
                if (bitmap == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[GenerateThumbnailWithRotation] プレビュー生成失敗: {imagePath}");
                    return;
                }
                
                // ⭐修正: 回転処理を無効化（左右統一のため）
                var rotatedBitmap = bitmap;
                // ⭐修正完了: GenerateThumbnailWithRotationでも回転処理をスキップ
                System.Diagnostics.Debug.WriteLine($"[GenerateThumbnailWithRotation] 回転処理スキップ - rotationAngle={rotationAngle}度");
                
                // ⭐修正: 左側ThumbnailImageのみ設定（PreviewImageは右側で独自生成）
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        // 左側サムネイル用のWPF BitmapImage変換
                        using var data = rotatedBitmap.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                        var thumbnailImage = new System.Windows.Media.Imaging.BitmapImage();
                        thumbnailImage.BeginInit();
                        thumbnailImage.StreamSource = new System.IO.MemoryStream(data.ToArray());
                        thumbnailImage.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                        // ⭐テスト: CreateOptionsを一時的に無効化して表示確認
                        // thumbnailImage.CreateOptions = System.Windows.Media.Imaging.BitmapCreateOptions.IgnoreImageCache;
                        thumbnailImage.EndInit();
                        thumbnailImage.Freeze();
                        
                        ThumbnailImage = thumbnailImage; // 左側サムネイル専用
                        // ⭐修正: PreviewImageは設定しない（右側で独自に高解像度生成）
                        
                        System.Diagnostics.Debug.WriteLine($"[GenerateThumbnailWithRotation] 左側サムネイル完了 - 回転 {rotationAngle}度: {Path.GetFileName(imagePath)}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[GenerateThumbnailWithRotation] WPF変換エラー: {ex.Message}");
                    }
                });
                
                // メモリ適切解放
                if (rotatedBitmap != bitmap)
                {
                    rotatedBitmap?.Dispose();
                }
                bitmap?.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GenerateThumbnailWithRotation] エラー: {ex.Message}");
            }
        }
        
        /// <summary>
        /// エラー時のフォールバック処理
        /// </summary>
        private void FallbackThumbnailRegeneration()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[FallbackThumbnailRegeneration] ページ {PageNumber} フォールバック実行");
                
                ThumbnailImage = null; // [ObservableProperty]自動通知
                LoadThumbnail(); // 従来の方法で再試行
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[FallbackThumbnailRegeneration] フォールバックもエラー: {ex.Message}");
            }
        }

        // 他のメソッドも必要に応じて簡素化...
        // 一時ファイル管理とDispose
        public void Dispose()
        {
            // すべての非同期処理をキャンセル
            _loadThumbnailCts?.Cancel();
            _loadPreviewCts?.Cancel();
            
            // CancellationTokenSourceのDispose
            _loadThumbnailCts?.Dispose();
            _loadPreviewCts?.Dispose();
            
            // 一時ファイルのクリーンアップ
            CleanupTempFiles();
            
            System.Diagnostics.Debug.WriteLine($"[PageViewModel.Dispose] Page {PageNumber} disposed");
        }
        
        public void CleanupTempFiles()
        {
            if (!string.IsNullOrEmpty(_heicTempJpegPath) && System.IO.File.Exists(_heicTempJpegPath))
            {
                try
                {
                    System.IO.File.Delete(_heicTempJpegPath);
                    System.Diagnostics.Debug.WriteLine($"[CleanupTempFiles] Deleted: {Path.GetFileName(_heicTempJpegPath)}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CleanupTempFiles] Error deleting {_heicTempJpegPath}: {ex.Message}");
                }
            }
        }

        // 他の必要なメソッドの実装
        private void GenerateRotatedPlaceholder() 
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[GenerateRotatedPlaceholder] ページ {PageNumber} のプレースホルダー生成");
                
                // 150x200のプレースホルダー画像を生成
                var placeholderBitmap = new SkiaSharp.SKBitmap(150, 200);
                using (var canvas = new SkiaSharp.SKCanvas(placeholderBitmap))
                {
                    // 背景を白で塗りつぶし
                    canvas.Clear(SkiaSharp.SKColors.White);
                    
                    // 境界線を描画
                    using (var paint = new SkiaSharp.SKPaint())
                    {
                        paint.Color = SkiaSharp.SKColors.Gray;
                        paint.Style = SkiaSharp.SKPaintStyle.Stroke;
                        paint.StrokeWidth = 2;
                        canvas.DrawRect(1, 1, 148, 198, paint);
                    }
                    
                    // ページ番号を描画
                    using (var textPaint = new SkiaSharp.SKPaint())
                    {
                        textPaint.Color = SkiaSharp.SKColors.Gray;
                        textPaint.TextSize = 24;
                        textPaint.IsAntialias = true;
                        textPaint.TextAlign = SkiaSharp.SKTextAlign.Center;
                        canvas.DrawText($"Page {PageNumber}", 75, 100, textPaint);
                    }
                }
                
                // WPFで表示可能な形式に変換
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    using var data = placeholderBitmap.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                    var stream = new System.IO.MemoryStream(data.ToArray());
                    
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = stream;
                    bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bitmap.CreateOptions = System.Windows.Media.Imaging.BitmapCreateOptions.IgnoreImageCache;
            bitmap.EndInit();
                    bitmap.Freeze();
                    
                    ThumbnailImage = bitmap;
                    // PreviewImageは設定せず、高品質プレビューはMainViewModelで生成
                });
                
                placeholderBitmap.Dispose();
                System.Diagnostics.Debug.WriteLine($"[GenerateRotatedPlaceholder] プレースホルダー生成完了");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GenerateRotatedPlaceholder] エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// 【削除済み】重複HEIC処理メソッド - ProcessHeicOptimizedAsyncに統合
        /// </summary>
        [Obsolete("ProcessHeicOptimizedAsyncに統合済み - メモリリーク防止のため削除", true)]
        private async Task<string> ConvertHeicToJpegForPreview(string path) 
        { 
            throw new InvalidOperationException("このメソッドは削除されました。ProcessHeicOptimizedAsyncを使用してください。");
        }
        
        /// <summary>
        /// HEIC回転プレビュー更新（最適化版・メモリリーク防止）
        /// </summary>
        private async Task UpdateRotatedHeicPreviewAsync() 
        { 
            try
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateRotatedHeicPreviewAsync] HEIC回転プレビュー最適化開始");
                
                if (string.IsNullOrEmpty(_page.SourceImagePath) || !File.Exists(_page.SourceImagePath))
                    return;
                
                // 🚀 Phase 4&5最適化: 直接バイト配列処理・一時ファイル作成なし
                var thumbnailData = await _imageProcessingService.GetImageThumbnailAsync(_page.SourceImagePath, 800, 600);
                
                if (thumbnailData == null || thumbnailData.Length == 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[UpdateRotatedHeicPreviewAsync] サムネイル取得失敗");
                    return;
                }
                
                // メモリストリーム直接処理（ファイル作成なし）
                using var memoryStream = new System.IO.MemoryStream(thumbnailData);
                // ⭐重要修正: SkiaSharpのEXIF Orientation自動適用を無効化
using var codec = SkiaSharp.SKCodec.Create(memoryStream);
using var originalBitmap = SkiaSharp.SKBitmap.Decode(codec, new SkiaSharp.SKImageInfo(codec.Info.Width, codec.Info.Height));
                
                if (originalBitmap == null) 
                {
                    System.Diagnostics.Debug.WriteLine($"[UpdateRotatedHeicPreviewAsync] ビットマップデコード失敗");
                    return;
                }
                
                // ⭐修正: 回転処理を無効化（左右統一のため）
                SkiaSharp.SKBitmap processedBitmap = originalBitmap;
                // ⭐修正完了: ProcessHeicOptimizedAsyncでも回転処理をスキップ
                System.Diagnostics.Debug.WriteLine($"[ProcessHeicOptimizedAsync] 回転処理スキップ - Rotation={_page.Rotation}度");
                
                // ⭐修正: PreviewImage設定を完全削除（ドキュメント通り）
                // 理由: PageViewModelでPreviewImageを設定すると右側の高解像度プレビューが劣化する
                // → MainViewModelで独自に高解像度プレビューを生成する必要がある
                
                // WeakReferenceキャッシュのみ更新（内部処理用）
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    try 
                    {
                        using var encodedData = processedBitmap.Encode(SkiaSharp.SKEncodedImageFormat.Jpeg, 85);
                        var wpfBitmap = CreateBitmapFromBytes(encodedData.ToArray());
                        
                        // WeakReferenceキャッシュに保存（PreviewImageは設定しない）
                        _optimizedPreviewCache = new WeakReference<BitmapSource>(wpfBitmap);
                        
                        System.Diagnostics.Debug.WriteLine($"[UpdateRotatedHeicPreviewAsync] HEICキャッシュ更新完了 - PreviewImageは右側で独自生成");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[UpdateRotatedHeicPreviewAsync] キャッシュ更新エラー: {ex.Message}");
                    }
                });
                
                // メモリ適切解放
                if (processedBitmap != originalBitmap)
                {
                    processedBitmap.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateRotatedHeicPreviewAsync] 最適化エラー: {ex.Message}");
            }
        }

        /// <summary>
        
        
        public void ClearPreviewImage() { PreviewImage = null; }
        /// <summary>
        /// 【最適化】WeakReferenceキャッシュのクリーンアップ（GC効率向上）
        /// </summary>
        public static void CleanupHeicCache() 
        { 
            // 静的キャッシュ廃止のため、GCに任せる
            GC.Collect(1, GCCollectionMode.Optimized);
            System.Diagnostics.Debug.WriteLine("[CleanupHeicCache] WeakReferenceキャッシュ最適化完了（GC実行）");
        }
        public void UpdatePageNumber(int newPageNumber) { PageNumber = newPageNumber; }
        
        /// <summary>
        /// HEICファイルを直接処理してプレビュー生成（ファイル経由なし）

    }
}