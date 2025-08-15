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

                // HEICファイルの場合は既存のサムネイルを無視して強制再生成
                bool isSourceHeic = !string.IsNullOrEmpty(_page.SourceImagePath) && 
                                   (System.IO.Path.GetExtension(_page.SourceImagePath).Equals(".heic", StringComparison.OrdinalIgnoreCase) ||
                                    System.IO.Path.GetExtension(_page.SourceImagePath).Equals(".heif", StringComparison.OrdinalIgnoreCase));
                
                if (isSourceHeic && System.IO.File.Exists(_page.SourceImagePath))
                {
                    // 🎯 遅延回転修正: Task.Run非同期を削除し同期処理に変更
                    ClearOptimizedCache();
                    LoadThumbnailFromImage(); // 同期実行で遅延回転を防止
                    return;
                }
                
                // まずPdfPageに既にサムネイル画像があるか確認（非HEIC画像の場合のみ）
                if (_page.ThumbnailImage != null && !isSourceHeic)
                {

                    LoadThumbnailFromPdfPage();
                }
                // 画像ファイルから直接サムネイルを生成（HEIC以外）
                else if (!string.IsNullOrEmpty(_page.SourceImagePath) && System.IO.File.Exists(_page.SourceImagePath))
                {
                    // 🎯 遅延回転修正: Task.Run非同期を削除し同期処理に変更
                    LoadThumbnailFromImage(); // 同期実行で遅延回転を防止
                }
                // PDFページの場合
                else if (_page.ThumbnailImage == null && _page.PageNumber > 0)
                {

                }
                else
                {

                    // サムネイルがない場合はプレースホルダーを生成
                    GenerateRotatedPlaceholder();
                }
                
                // [ObservableProperty]による自動PropertyChanged通知に依存
                // [ObservableProperty]自動通知に依存
            }
            catch (Exception ex)
            {
                // サムネイル読み込みエラーをログに記録


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

            }
            catch (Exception ex)
            {

            }
        }
        
        private void LoadThumbnailFromPdfPage()
        {
            try
            {
                if (_page.ThumbnailImage != null)
                {

                    // 🚀 根本修正: SkiaSharp → WriteableBitmapに直接変換
                    using var data = _page.ThumbnailImage.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                    var bitmap = CreateSimpleBitmapFromBytes(data.ToArray());
                    
                    ThumbnailImage = bitmap;
                    
                    // ⭐修正: PreviewImage設定を完全削除（ドキュメント通り）
                    // 理由: PageViewModelでPreviewImageを設定すると右側の高解像度プレビューが劣化する
                    // → MainViewModelで独自に高解像度プレビューを生成する必要がある

                }
            }
            catch (Exception ex)
            {

            }
        }
        
        private System.Threading.CancellationTokenSource? _loadThumbnailCts;
        private System.Threading.CancellationTokenSource? _loadPreviewCts; // プレビュー読み込み専用キャンセレーション
        private string? _heicTempJpegPath; // HEIC変換時の一時ファイルパス（PDF発行まで保持）
                // 🚀 Phase 2最適化: 静的キャッシュ廃止・WeakReference活用
        private WeakReference<byte[]>? _optimizedThumbnailCache; // 最適化サムネイルキャッシュ（GC対応）
        private WeakReference<System.Windows.Media.Imaging.BitmapSource>? _optimizedPreviewCache; // 最適化プレビューキャッシュ（GC対応） // HEICファイルパス → JPEGパスのキャッシュ
                private readonly object _heicProcessingLock = new object(); // HEIC処理の排他制御（インスタンス別）

        private void LoadThumbnailFromImage()
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
                    
                    // 🎯 遅延回転修正: 同期処理に変更
                    ProcessHeicOptimizedAsync(imagePathToLoad, cancellationToken).Wait();
                    return;
                }
                
                // 通常の画像ファイル処理（HEIC以外）
                ProcessStandardImageAsync(imagePathToLoad, cancellationToken).Wait();
            }
            catch (Exception ex)
            {

                // エラー発生時は基本処理にフォールバック
                ProcessImageFallbackAsync(_page.SourceImagePath, cancellationToken).Wait();
            }
        }

        /// <summary>
        /// HEIC処理の最適化版（キャッシュ活用・2重変換排除）
        /// </summary>
        /// <summary>
        /// HEIC処理の最適化版（キャッシュ活用・2重変換排除）
        /// </summary>
        private async Task ProcessHeicOptimizedAsync(string heicPath, CancellationToken cancellationToken)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[ProcessHeicOptimizedAsync] 📱 HEIC画像読み込み開始: {Path.GetFileName(heicPath)}");
                
                // 🎯 シンプル実装: HEICファイルも直接読み込み
                var imageBytes = await System.IO.File.ReadAllBytesAsync(heicPath);
                
                if (cancellationToken.IsCancellationRequested || imageBytes == null)
                    return;
                
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;
                        
                    try
                    {
                        // 🎯 シンプル実装: 基本的なBitmapImage読み込みのみ
                        var bitmap = CreateSimpleBitmapFromBytes(imageBytes);
                        
                        ThumbnailImage = bitmap; // 左側サムネイル
                        
                        // キャッシュ保存
                        _optimizedThumbnailCache = new WeakReference<byte[]>(imageBytes);
                        
                        System.Diagnostics.Debug.WriteLine($"[ProcessHeicOptimizedAsync] ✅ HEIC読み込み完了 - Size: {bitmap.PixelWidth}x{bitmap.PixelHeight}: {Path.GetFileName(heicPath)}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ProcessHeicOptimizedAsync] ❌ エラー: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ProcessHeicOptimizedAsync] ❌ ファイル読み込みエラー: {ex.Message}");
                // エラー時はフォールバック処理
                await ProcessImageFallbackAsync(heicPath, cancellationToken);
            }
        }
        
        /// <summary>
        /// 通常画像ファイルの最適化処理
        /// </summary>
        /// <summary>
        /// 通常画像ファイルの最適化処理
        /// </summary>
        private async Task ProcessStandardImageAsync(string imagePath, CancellationToken cancellationToken)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[ProcessStandardImageAsync] 📷 シンプル画像読み込み開始: {Path.GetFileName(imagePath)}");
                
                // 🎯 シンプル実装: ファイルから直接読み込み
                var imageBytes = await System.IO.File.ReadAllBytesAsync(imagePath);
                
                if (cancellationToken.IsCancellationRequested || imageBytes == null)
                    return;
                
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;
                        
                    try
                    {
                        // 🎯 シンプル実装: 基本的なBitmapImage読み込みのみ
                        var bitmap = CreateSimpleBitmapFromBytes(imageBytes);
                        
                        ThumbnailImage = bitmap; // 左側サムネイル
                        
                        System.Diagnostics.Debug.WriteLine($"[ProcessStandardImageAsync] ✅ シンプル読み込み完了 - Size: {bitmap.PixelWidth}x{bitmap.PixelHeight}: {Path.GetFileName(imagePath)}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ProcessStandardImageAsync] ❌ エラー: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ProcessStandardImageAsync] ❌ ファイル読み込みエラー: {ex.Message}");
                // エラー時はフォールバック処理
                await ProcessImageFallbackAsync(imagePath, cancellationToken);
            }
        }
        
        /// <summary>
        /// フォールバック処理（エラー時の基本処理）
        /// </summary>
        /// <summary>
        /// フォールバック処理（エラー時の基本処理）
        /// </summary>
        private async Task ProcessImageFallbackAsync(string imagePath, CancellationToken cancellationToken)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[ProcessImageFallbackAsync] 🔄 フォールバック処理開始: {Path.GetFileName(imagePath)}");
                
                // SkiaSharpによる高品質サムネイル生成（フォールバック用）
                var bitmap = await _imageProcessingService.GenerateHighQualityPreviewAsync(imagePath, 150, 200);
                
                if (cancellationToken.IsCancellationRequested || bitmap == null)
                    return;
                
                var finalBitmap = bitmap;

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;
                        
                    try
                    {
                        // SkiaSharp → PNG → BitmapImage変換
                        using var data = finalBitmap.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                        var imageBytes = data.ToArray();
                        
                        // 🎯 シンプル実装: 基本的なBitmapImage読み込みのみ
                        var thumbnailImage = CreateSimpleBitmapFromBytes(imageBytes);
                        
                        ThumbnailImage = thumbnailImage; // 左側サムネイル
                        
                        System.Diagnostics.Debug.WriteLine($"[ProcessImageFallbackAsync] ✅ フォールバック完了: {Path.GetFileName(imagePath)}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ProcessImageFallbackAsync] ❌ エラー: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine($"[ProcessImageFallbackAsync] ❌ フォールバック処理エラー: {ex.Message}");
            }
        }
        
        /// <summary>
        /// キャッシュされたサムネイルの表示
        /// </summary>
        private void DisplayCachedThumbnail(byte[] thumbnailData)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var bitmap = CreateSimpleBitmapFromBytes(thumbnailData);
                ThumbnailImage = bitmap;
                // PreviewImageは設定せず、高品質プレビューはMainViewModelで生成
                // [ObservableProperty]自動通知に依存
            });
        }
        
        /// <summary>
        /// バイト配列からBitmapSourceを作成
        /// </summary>
        private BitmapSource CreateSimpleBitmapFromBytes(byte[] imageData)
{
    // 🎯 OSS標準実装: EXIF Orientationを適切に読み取り→BitmapImage.Rotationで自動回転
    // 参考: Stack Overflow実証済みパターン + 画像ビューアOSS標準実装
    
    using var stream = new MemoryStream(imageData);
    
    // Phase 1: EXIF Orientation検出
    System.Windows.Media.Imaging.Rotation rotation = System.Windows.Media.Imaging.Rotation.Rotate0;
    try
    {
        stream.Position = 0;
        var frame = BitmapFrame.Create(stream, BitmapCreateOptions.DelayCreation, BitmapCacheOption.None);
        var metadata = frame.Metadata as BitmapMetadata;
        
        if (metadata?.ContainsQuery("System.Photo.Orientation") == true)
        {
            var orientationValue = metadata.GetQuery("System.Photo.Orientation");
            if (orientationValue != null)
            {
                var orientation = (ushort)orientationValue;
                rotation = orientation switch
                {
                    6 => System.Windows.Media.Imaging.Rotation.Rotate90,   // 右90度回転
                    3 => System.Windows.Media.Imaging.Rotation.Rotate180,  // 180度回転
                    8 => System.Windows.Media.Imaging.Rotation.Rotate270,  // 左90度回転 (右270度)
                    _ => System.Windows.Media.Imaging.Rotation.Rotate0     // 回転なし
                };
            }
        }
    }
    catch
    {
        // EXIF読み取り失敗時は回転なしで続行
        rotation = System.Windows.Media.Imaging.Rotation.Rotate0;
    }
    
    // Phase 2: BitmapImage + 自動回転設定（OSS標準パターン）
    stream.Position = 0; // ストリームを先頭に戻す
    
    var bitmap = new BitmapImage();
    bitmap.BeginInit();
    bitmap.StreamSource = stream;
    bitmap.CacheOption = BitmapCacheOption.OnLoad;
    bitmap.Rotation = rotation; // ← WPF標準的解決策（OSS実証済み）
    bitmap.EndInit();
    bitmap.Freeze();
    
    // デバッグログ
    var rotationDegrees = rotation switch 
    {
        System.Windows.Media.Imaging.Rotation.Rotate90 => "90°",
        System.Windows.Media.Imaging.Rotation.Rotate180 => "180°", 
        System.Windows.Media.Imaging.Rotation.Rotate270 => "270°",
        _ => "0°"
    };
    File.AppendAllText("DEBUG_LOG.txt", $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [OSS_STANDARD] EXIF Orientation → {rotationDegrees} rotation applied\\n");
    
    return bitmap;
}

        // 🚫 EXIF自動回転機能削除 - 不要なコード削除済み
        
        /// <summary>
        /// プレビューはサムネイルと同じ処理で統一 - HEIC/JPEG同等処理
        /// </summary>
        private void LoadPreviewFromImage()
        {

            // LoadThumbnailFromImage()で既にPreviewImageも設定されているため、重複処理は不要
        }

        // 既存のメソッドは維持
        public void UpdateRotationSync()
        {
            try
            {
                // 回転値を更新
                // ✅ 正しい修正: Windows標準アプリと同じ表示のため、EXIF回転角度を表示
                Rotation = _page.Rotation;

                // ★修正: キャッシュクリアのみ実行、サムネイル再生成は削除
                ClearOptimizedCache();

                // ★削除: LoadThumbnail()呼び出しを完全削除（競合状態の原因）
                // 理由: RegenerateThumbnailAfterRotationAsync()と重複実行され、古い状態で上書きされる
                
                // プロパティ変更通知は回転値のみ（サムネイル更新は非同期処理完了時に実行）
                OnPropertyChanged(nameof(Rotation));
            }
            catch (Exception ex)
            {

            }
        }

        
        /// <summary>
        /// 文字向きを自動検出・補正する（OCRベース）
        /// </summary>
        public async Task AutoCorrectOrientationAsync()
        {
            if (_textOrientationService == null)
            {

                return;
            }

            try
            {

                // 文字が読み取れるかチェック
                var hasText = await _textOrientationService.HasReadableTextAsync(_page.SourceImagePath);
                if (!hasText)
                {

                    return;
                }
                
                // 最適な向きを検出（並列処理で高速化）
                var optimalRotation = await _textOrientationService.DetectOptimalOrientationParallelAsync(_page.SourceImagePath);

                if (optimalRotation != _page.Rotation)
                {
                    // 回転値を更新
                    _page.Rotation = optimalRotation;
                    Rotation = optimalRotation;
                    
                    // サムネイル再生成
                    await RegenerateThumbnailAfterRotationAsync();

                }
                else
                {

                }
            }
            catch (Exception ex)
            {

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

                        }
                    }
                    
                    // 確実にnull設定 - [ObservableProperty]自動通知のみ
                    ThumbnailImage = null;

                });
                
                // 3. 新しいサムネイル生成（回転角度を考慮）
                try
                {

                    // 回転角度を明示的に渡してサムネイル生成
                    // ✅ 正しい修正: Windows標準アプリと同じ表示のため、EXIF回転角度を適用
                    await GenerateThumbnailWithRotation(_page.SourceImagePath ?? "", _page.Rotation);
                    
                    // 最終更新 - [ObservableProperty]自動通知のみ（手動通知削除）

                }
                catch (Exception ex)
                {

                    // エラー時のフォールバック処理
                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        FallbackThumbnailRegeneration();
                    });
                }
            }
            catch (Exception ex)
            {

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

            }
            catch (Exception ex)
            {

            }
        }
        
        /// <summary>
        /// 回転角度を考慮したサムネイル生成
        /// </summary>
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

                    return;
                }
                
                // ⭐修正: 回転処理を無効化（左右統一のため）
                var rotatedBitmap = bitmap;
                // ⭐修正完了: GenerateThumbnailWithRotationでも回転処理をスキップ

                // 🚀 根本修正: WPF BitmapImage → WriteableBitmap統一
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    try
                    {
                        // ⭐根本解決: SkiaSharp → WriteableBitmapに直接変換
                        using var data = rotatedBitmap.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                        var thumbnailImage = CreateSimpleBitmapFromBytes(data.ToArray());
                        
                        ThumbnailImage = thumbnailImage; // 左側サムネイル専用
                        // ⭐修正: PreviewImageは設定しない（右側で独自に高解像度生成）
                        
                        System.Diagnostics.Debug.WriteLine($"[GenerateThumbnailWithRotation] ⭐WriteableBitmap統一 左側サムネイル完了 - 回転 {rotationAngle}度: {Path.GetFileName(imagePath)}");
                    }
                    catch (Exception ex)
                    {

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

            }
        }
        
        /// <summary>
        /// エラー時のフォールバック処理
        /// </summary>
        private void FallbackThumbnailRegeneration()
        {
            try
            {

                ThumbnailImage = null; // [ObservableProperty]自動通知
                LoadThumbnail(); // 従来の方法で再試行
            }
            catch (Exception ex)
            {

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

                }
            }
        }

        // 他の必要なメソッドの実装
        private void GenerateRotatedPlaceholder() 
        {
            try
            {

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
                
                // 🚀 根本修正: SkiaSharp → WriteableBitmapに直接変換
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    using var data = placeholderBitmap.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                    var bitmap = CreateSimpleBitmapFromBytes(data.ToArray());
                    
                    ThumbnailImage = bitmap;
                    // PreviewImageは設定せず、高品質プレビューはMainViewModelで生成
                });
                
                placeholderBitmap.Dispose();

            }
            catch (Exception ex)
            {

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

                if (string.IsNullOrEmpty(_page.SourceImagePath) || !File.Exists(_page.SourceImagePath))
                    return;
                
                // 🚀 Phase 4&5最適化: 直接バイト配列処理・一時ファイル作成なし
                var thumbnailData = await _imageProcessingService.GetImageThumbnailAsync(_page.SourceImagePath, 800, 600);
                
                if (thumbnailData == null || thumbnailData.Length == 0)
                {

                    return;
                }
                
                // メモリストリーム直接処理（ファイル作成なし）
                using var memoryStream = new System.IO.MemoryStream(thumbnailData);
                // ⭐重要修正: SkiaSharpのEXIF Orientation自動適用を無効化
using var codec = SkiaSharp.SKCodec.Create(memoryStream);
using var originalBitmap = SkiaSharp.SKBitmap.Decode(codec, new SkiaSharp.SKImageInfo(codec.Info.Width, codec.Info.Height));
                
                if (originalBitmap == null) 
                {

                    return;
                }
                
                // ⭐修正: 回転処理を無効化（左右統一のため）
                SkiaSharp.SKBitmap processedBitmap = originalBitmap;
                // ⭐修正完了: ProcessHeicOptimizedAsyncでも回転処理をスキップ

                // ⭐修正: PreviewImage設定を完全削除（ドキュメント通り）
                // 理由: PageViewModelでPreviewImageを設定すると右側の高解像度プレビューが劣化する
                // → MainViewModelで独自に高解像度プレビューを生成する必要がある
                
                // WeakReferenceキャッシュのみ更新（内部処理用）
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    try 
                    {
                        using var encodedData = processedBitmap.Encode(SkiaSharp.SKEncodedImageFormat.Jpeg, 85);
                        var wpfBitmap = CreateSimpleBitmapFromBytes(encodedData.ToArray());
                        
                        // WeakReferenceキャッシュに保存（PreviewImageは設定しない）
                        _optimizedPreviewCache = new WeakReference<BitmapSource>(wpfBitmap);

                    }
                    catch (Exception ex)
                    {

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

        }
        public void UpdatePageNumber(int newPageNumber) { PageNumber = newPageNumber; }
        
        /// <summary>
        /// HEICファイルを直接処理してプレビュー生成（ファイル経由なし）

    }
}