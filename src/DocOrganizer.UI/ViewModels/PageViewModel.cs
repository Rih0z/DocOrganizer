using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DocOrganizer.Core.Models;
using SkiaSharp;
using ImageMagick;

namespace DocOrganizer.UI.ViewModels
{
    public partial class PageViewModel : ObservableObject, IDisposable
    {
        private readonly PdfPage _page;
        
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

        public PageViewModel(PdfPage page)
        {
            _page = page;
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
                
                // まずPdfPageに既にサムネイル画像があるか確認（HEICの場合はMainViewModelで事前生成される）
                if (_page.ThumbnailImage != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[LoadThumbnail] PDFページのサムネイル使用");
                    LoadThumbnailFromPdfPage();
                }
                // 画像ファイルから直接サムネイルを生成（HEIC以外）
                else if (!string.IsNullOrEmpty(_page.SourceImagePath) && System.IO.File.Exists(_page.SourceImagePath))
                {
                    var extension = Path.GetExtension(_page.SourceImagePath).ToLowerInvariant();
                    var isHeic = extension == ".heic" || extension == ".heif";
                    
                    if (!isHeic)
                    {
                        System.Diagnostics.Debug.WriteLine($"[LoadThumbnail] 画像ファイルから生成: {_page.SourceImagePath}");
                        _ = Task.Run(() => LoadThumbnailFromImage());
                        _ = Task.Run(() => LoadPreviewFromImage());
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"[LoadThumbnail] HEICファイルのため、MainViewModelでの処理を待機: {_page.SourceImagePath}");
                        // HEICファイルの場合はMainViewModelで処理されるのを待つ
                    }
                }
                // PdfPageからサムネイル画像を取得
                else if (false) // この条件は不要になったため無効化
                {
                    // このブロックは上に移動したため削除
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[LoadThumbnail] サムネイルなし - プレースホルダー生成");
                    // サムネイルがない場合はプレースホルダーを生成
                    GenerateRotatedPlaceholder();
                }
                
                // プロパティ変更通知を明示的に発火
                OnPropertyChanged(nameof(ThumbnailImage));
                OnPropertyChanged(nameof(PreviewImage));
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
                    bitmap.EndInit();
                    bitmap.Freeze();
                    
                    ThumbnailImage = bitmap;
                    
                    // プレビュー画像も設定されていれば使用
                    if (_page.PreviewImage != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[LoadThumbnailFromPdfPage] プレビュー画像も変換 - Size: {_page.PreviewImage.Width}x{_page.PreviewImage.Height}");
                        using var previewData = _page.PreviewImage.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                        var previewStream = new System.IO.MemoryStream(previewData.ToArray());
                        
                        var previewBitmap = new System.Windows.Media.Imaging.BitmapImage();
                        previewBitmap.BeginInit();
                        previewBitmap.StreamSource = previewStream;
                        previewBitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                        previewBitmap.EndInit();
                        previewBitmap.Freeze();
                        
                        PreviewImage = previewBitmap;
                    }
                    else
                    {
                        PreviewImage = bitmap; // プレビューがない場合はサムネイルを使用
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"[LoadThumbnailFromPdfPage] サムネイルとプレビュー設定完了");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadThumbnailFromPdfPage] エラー: {ex.Message}");
            }
        }
        
        private CancellationTokenSource? _loadThumbnailCts;
        private string? _heicTempJpegPath; // HEIC変換時の一時ファイルパス（PDF発行まで保持）
        private static readonly Dictionary<string, string> _heicConversionCache = new Dictionary<string, string>(); // HEICファイルパス → JPEGパスのキャッシュ
        
        private async void LoadThumbnailFromImage()
        {
            // 前の読み込み処理をキャンセル
            _loadThumbnailCts?.Cancel();
            _loadThumbnailCts = new CancellationTokenSource();
            var cancellationToken = _loadThumbnailCts.Token;
            
            string tempJpegPath = null;
            
            try
            {
                // HEICファイルチェック
                string imagePathToLoad = _page.SourceImagePath;
                bool isHeic = Path.GetExtension(imagePathToLoad).Equals(".heic", StringComparison.OrdinalIgnoreCase) ||
                             Path.GetExtension(imagePathToLoad).Equals(".heif", StringComparison.OrdinalIgnoreCase);
                
                if (isHeic)
                {
                    System.Diagnostics.Debug.WriteLine($"[LoadThumbnailFromImage] HEIC file detected, converting to JPEG: {Path.GetFileName(imagePathToLoad)}");
                    
                    // キャッシュをチェック
                    if (_heicConversionCache.ContainsKey(imagePathToLoad) && File.Exists(_heicConversionCache[imagePathToLoad]))
                    {
                        tempJpegPath = _heicConversionCache[imagePathToLoad];
                        System.Diagnostics.Debug.WriteLine($"[LoadThumbnailFromImage] Using cached JPEG: {tempJpegPath}");
                    }
                    else
                    {
                        // HEICファイルをJPEGに変換（一時的）
                        try
                        {
                            tempJpegPath = await ConvertHeicToJpegForPreview(imagePathToLoad);
                            if (string.IsNullOrEmpty(tempJpegPath) || !File.Exists(tempJpegPath))
                            {
                                System.Diagnostics.Debug.WriteLine($"[LoadThumbnailFromImage] HEIC conversion failed or file not found");
                                return;
                            }
                            
                            // キャッシュに追加
                            _heicConversionCache[imagePathToLoad] = tempJpegPath;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[LoadThumbnailFromImage] HEIC conversion error: {ex.Message}");
                            return;
                        }
                    }
                    
                    imagePathToLoad = tempJpegPath;
                    
                    // 一時ファイルパスを保持（PDF発行時まで削除しない）
                    _heicTempJpegPath = tempJpegPath;
                }
                
                using var originalBitmap = SkiaSharp.SKBitmap.Decode(imagePathToLoad);
                if (originalBitmap == null) return;
                
                // サムネイルサイズを計算
                var thumbnailSize = 150;
                var aspectRatio = (float)originalBitmap.Height / originalBitmap.Width;
                var thumbnailHeight = (int)(thumbnailSize * aspectRatio);
                
                // サムネイル生成
                var thumbnail = new SkiaSharp.SKBitmap(thumbnailSize, thumbnailHeight);
                using (var canvas = new SkiaSharp.SKCanvas(thumbnail))
                {
                    using (var paint = new SkiaSharp.SKPaint())
                    {
                        paint.IsAntialias = true;
                        paint.FilterQuality = SkiaSharp.SKFilterQuality.High;
                        
                        var destRect = SkiaSharp.SKRect.Create(0, 0, thumbnailSize, thumbnailHeight);
                        canvas.DrawBitmap(originalBitmap, destRect, paint);
                    }
                }
                
                // WPFで表示可能な形式に変換
                using var data = thumbnail.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                var stream = new System.IO.MemoryStream(data.ToArray());
                
                // UIスレッドで更新
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = stream;
                    bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    ThumbnailImage = bitmap;
                    
                    // プレビュー画像も設定（サムネイルと同じ画像を使用）
                    PreviewImage = bitmap;
                    System.Diagnostics.Debug.WriteLine($"[LoadThumbnailFromImage] Thumbnail and Preview set successfully - PreviewImage: {PreviewImage != null}");
                    
                    // MainViewModelにPreviewImage更新を通知
                    OnPropertyChanged(nameof(PreviewImage));
                });
                
                thumbnail.Dispose();
                
                // 注意: HEICの一時ファイルはここでは削除しない（PDF発行時まで保持）
                // PDF生成完了後またはアプリケーション終了時に削除する
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"画像サムネイル生成エラー: {ex.Message}");
                
                // エラー時も一時ファイルは削除しない（他の処理で使用される可能性があるため）
            }
        }
        
        private async void LoadPreviewFromImage()
        {
            string tempJpegPath = null;
            
            try
            {
                // HEICファイルチェック
                string imagePathToLoad = _page.SourceImagePath;
                bool isHeic = Path.GetExtension(imagePathToLoad).Equals(".heic", StringComparison.OrdinalIgnoreCase) ||
                             Path.GetExtension(imagePathToLoad).Equals(".heif", StringComparison.OrdinalIgnoreCase);
                
                if (isHeic)
                {
                    System.Diagnostics.Debug.WriteLine($"[LoadPreviewFromImage] HEIC file detected, converting to JPEG: {Path.GetFileName(imagePathToLoad)}");
                    
                    // キャッシュをチェック
                    if (_heicConversionCache.ContainsKey(imagePathToLoad) && File.Exists(_heicConversionCache[imagePathToLoad]))
                    {
                        tempJpegPath = _heicConversionCache[imagePathToLoad];
                        System.Diagnostics.Debug.WriteLine($"[LoadPreviewFromImage] Using cached JPEG: {tempJpegPath}");
                    }
                    else
                    {
                        // HEICファイルをJPEGに変換（一時的）
                        try
                        {
                            tempJpegPath = await ConvertHeicToJpegForPreview(imagePathToLoad);
                            if (string.IsNullOrEmpty(tempJpegPath) || !File.Exists(tempJpegPath))
                            {
                                System.Diagnostics.Debug.WriteLine($"[LoadPreviewFromImage] HEIC conversion failed or file not found");
                                return;
                            }
                            
                            // キャッシュに追加
                            _heicConversionCache[imagePathToLoad] = tempJpegPath;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[LoadPreviewFromImage] HEIC conversion error: {ex.Message}");
                            return;
                        }
                    }
                    imagePathToLoad = tempJpegPath;
                }
                
                using var originalBitmap = SkiaSharp.SKBitmap.Decode(imagePathToLoad);
                if (originalBitmap == null) return;
                
                // プレビューはより高解像度で生成
                var maxPreviewSize = 800;
                var aspectRatio = (float)originalBitmap.Height / originalBitmap.Width;
                var previewWidth = originalBitmap.Width > maxPreviewSize ? maxPreviewSize : originalBitmap.Width;
                var previewHeight = (int)(previewWidth * aspectRatio);
                
                // プレビュー生成
                var preview = new SkiaSharp.SKBitmap(previewWidth, previewHeight);
                using (var canvas = new SkiaSharp.SKCanvas(preview))
                {
                    using (var paint = new SkiaSharp.SKPaint())
                    {
                        paint.IsAntialias = true;
                        paint.FilterQuality = SkiaSharp.SKFilterQuality.High;
                        
                        var destRect = SkiaSharp.SKRect.Create(0, 0, previewWidth, previewHeight);
                        canvas.DrawBitmap(originalBitmap, destRect, paint);
                    }
                }
                
                // WPFで表示可能な形式に変換
                using var data = preview.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                var stream = new System.IO.MemoryStream(data.ToArray());
                
                // UIスレッドで更新
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = stream;
                    bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    PreviewImage = bitmap;
                    
                    // MainViewModelにPreviewImage更新を通知
                    OnPropertyChanged(nameof(PreviewImage));
                });
                
                preview.Dispose();
                
                // 一時ファイルのクリーンアップ
                if (!string.IsNullOrEmpty(tempJpegPath) && File.Exists(tempJpegPath))
                {
                    try
                    {
                        File.Delete(tempJpegPath);
                        System.Diagnostics.Debug.WriteLine($"[LoadPreviewFromImage] Temporary JPEG deleted: {Path.GetFileName(tempJpegPath)}");
                    }
                    catch (Exception deleteEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[LoadPreviewFromImage] Failed to delete temp file: {deleteEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"プレビュー画像生成エラー: {ex.Message}");
                
                // エラー時も一時ファイルをクリーンアップ
                if (!string.IsNullOrEmpty(tempJpegPath) && File.Exists(tempJpegPath))
                {
                    try
                    {
                        File.Delete(tempJpegPath);
                    }
                    catch
                    {
                        // クリーンアップエラーは無視
                    }
                }
            }
        }

        private static bool _magickNetInitialized = false;
        private static readonly object _magickInitLock = new object();
        
        /// <summary>
        /// HEICファイルをプレビュー用にJPEGに変換（安全版・キャッシュ対応）
        /// </summary>
        private async Task<string> ConvertHeicToJpegForPreview(string heicPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // ファイル存在確認
                    if (!File.Exists(heicPath))
                    {
                        System.Diagnostics.Debug.WriteLine($"[ConvertHeicToJpegForPreview] HEIC file not found: {heicPath}");
                        return null;
                    }
                    
                    // ImageMagick初期化（スレッドセーフ）
                    lock (_magickInitLock)
                    {
                        if (!_magickNetInitialized)
                        {
                            try
                            {
                                ImageMagick.MagickNET.Initialize();
                                _magickNetInitialized = true;
                                System.Diagnostics.Debug.WriteLine("[ConvertHeicToJpegForPreview] MagickNET initialized successfully");
                            }
                            catch (Exception initEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"[ConvertHeicToJpegForPreview] MagickNET initialization failed: {initEx.Message}");
                                // 初期化失敗でも処理を続行
                            }
                        }
                    }
                    
                    // 一時ファイルパス生成（ファイル名に元のファイル名を含める）
                    var sourceFileName = Path.GetFileNameWithoutExtension(heicPath);
                    var tempJpegPath = Path.Combine(Path.GetTempPath(), $"heic_preview_{sourceFileName}_{Guid.NewGuid():N}.jpg");
                    
                    using (var image = new ImageMagick.MagickImage())
                    {
                        // HEIC読み込み設定
                        var settings = new ImageMagick.MagickReadSettings
                        {
                            BackgroundColor = ImageMagick.MagickColors.White,
                            ColorSpace = ImageMagick.ColorSpace.sRGB
                        };
                        
                        // HEICファイル読み込み
                        image.Read(heicPath, settings);
                        
                        // 画像の検証
                        if (image.Width == 0 || image.Height == 0)
                        {
                            System.Diagnostics.Debug.WriteLine($"[ConvertHeicToJpegForPreview] Invalid image dimensions: {image.Width}x{image.Height}");
                            return null;
                        }
                        
                        // 向き補正（EXIF情報に基づく）
                        image.AutoOrient();
                        
                        // プレビュー用に品質を設定（高品質）
                        image.Quality = 90;
                        image.Format = ImageMagick.MagickFormat.Jpeg;
                        
                        // プレビューサイズの最適化（大きすぎる場合はリサイズ）
                        const int maxPreviewSize = 2048;
                        if (image.Width > maxPreviewSize || image.Height > maxPreviewSize)
                        {
                            var geometry = new ImageMagick.MagickGeometry(maxPreviewSize, maxPreviewSize);
                            geometry.IgnoreAspectRatio = false;
                            image.Resize(geometry);
                            System.Diagnostics.Debug.WriteLine($"[ConvertHeicToJpegForPreview] Resized to: {image.Width}x{image.Height}");
                        }
                        
                        // JPEG形式で保存
                        image.Write(tempJpegPath);
                        
                        // ファイル生成確認
                        if (!File.Exists(tempJpegPath))
                        {
                            System.Diagnostics.Debug.WriteLine("[ConvertHeicToJpegForPreview] JPEG file was not created");
                            return null;
                        }
                        
                        var fileInfo = new FileInfo(tempJpegPath);
                        System.Diagnostics.Debug.WriteLine($"[ConvertHeicToJpegForPreview] Successfully converted: {Path.GetFileName(tempJpegPath)} ({fileInfo.Length / 1024}KB)");
                    }
                    
                    return tempJpegPath;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ConvertHeicToJpegForPreview] Conversion failed: {ex.GetType().Name} - {ex.Message}");
                    
                    // スタックトレースも出力（詳細なデバッグ用）
                    System.Diagnostics.Debug.WriteLine($"[ConvertHeicToJpegForPreview] StackTrace: {ex.StackTrace}");
                    
                    return null;
                }
            });
        }

        public void UpdatePageNumber(int newPageNumber)
        {
            PageNumber = newPageNumber;
        }

        public void UpdateRotation()
        {
            System.Diagnostics.Debug.WriteLine($"[UpdateRotation] ページ {_page.PageNumber} - 回転前: {Rotation}度, 回転後: {_page.Rotation}度");
            
            // 回転値を更新（これによりUIが自動的に更新される）
            Rotation = _page.Rotation;
            
            // 画像ベースのページの場合、回転したサムネイルとプレビューを再生成
            if (!string.IsNullOrEmpty(_page.SourceImagePath))
            {
                ReloadRotatedThumbnail();
                ReloadRotatedPreview();
            }
            else
            {
                // PDFページの場合、プレースホルダーを直接生成して表示
                GenerateRotatedPlaceholder();
                
                // 強制的にプロパティ変更通知を発火
                OnPropertyChanged(nameof(ThumbnailImage));
                OnPropertyChanged(nameof(PreviewImage));
                OnPropertyChanged(nameof(Rotation));
            }
        }
        
        private async void ReloadRotatedThumbnail()
        {
            try
            {
                if (string.IsNullOrEmpty(_page.SourceImagePath) || !System.IO.File.Exists(_page.SourceImagePath))
                {
                    ThumbnailImage = null;
                    return;
                }
                
                // HEICファイルの特別処理
                SkiaSharp.SKBitmap originalBitmap = null;
                string tempJpegPath = null;
                
                try
                {
                    if (IsHeicFile(_page.SourceImagePath))
                    {
                        // HEICファイルの場合は先にJPEGに変換
                        tempJpegPath = await ConvertHeicToJpegForRotationAsync(_page.SourceImagePath);
                        if (string.IsNullOrEmpty(tempJpegPath))
                        {
                            // 変換失敗時の処理
                            ThumbnailImage = null;
                            return;
                        }
                        // ファイル存在チェックを少し待機（非同期処理の完了待ち）
                        for (int i = 0; i < 10; i++)
                        {
                            if (System.IO.File.Exists(tempJpegPath))
                            {
                                break;
                            }
                            await Task.Delay(100);
                        }
                        if (!System.IO.File.Exists(tempJpegPath))
                        {
                            ThumbnailImage = null;
                            return;
                        }
                        originalBitmap = SkiaSharp.SKBitmap.Decode(tempJpegPath);
                    }
                    else
                    {
                        // 通常の画像ファイル
                        originalBitmap = SkiaSharp.SKBitmap.Decode(_page.SourceImagePath);
                    }
                    
                    if (originalBitmap == null)
                    {
                        ThumbnailImage = null;
                        return;
                    }
                
                // 回転した画像を作成
                var rotatedBitmap = RotateBitmap(originalBitmap, _page.Rotation);
                
                // サムネイルサイズにリサイズ
                var thumbnailSize = 150;
                var aspectRatio = (float)rotatedBitmap.Height / rotatedBitmap.Width;
                var thumbnailHeight = (int)(thumbnailSize * aspectRatio);
                
                var thumbnail = new SkiaSharp.SKBitmap(thumbnailSize, thumbnailHeight);
                using (var canvas = new SkiaSharp.SKCanvas(thumbnail))
                {
                    using (var paint = new SkiaSharp.SKPaint())
                    {
                        paint.IsAntialias = true;
                        paint.FilterQuality = SkiaSharp.SKFilterQuality.High;
                        
                        var destRect = SkiaSharp.SKRect.Create(0, 0, thumbnailSize, thumbnailHeight);
                        canvas.DrawBitmap(rotatedBitmap, destRect, paint);
                    }
                }
                
                // WPFで表示可能な形式に変換（UIスレッドで実行）
                using var data = thumbnail.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                var stream = new System.IO.MemoryStream(data.ToArray());
                
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = stream;
                    bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    
                    ThumbnailImage = bitmap;
                });
                
                    // メモリクリーンアップ
                    rotatedBitmap.Dispose();
                    thumbnail.Dispose();
                    originalBitmap?.Dispose();
                }
                finally
                {
                    // 一時ファイルのクリーンアップ
                    if (!string.IsNullOrEmpty(tempJpegPath) && System.IO.File.Exists(tempJpegPath))
                    {
                        try
                        {
                            System.IO.File.Delete(tempJpegPath);
                        }
                        catch
                        {
                            // クリーンアップエラーは無視
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"回転サムネイル生成エラー: {ex.Message}");
                ThumbnailImage = null;
            }
        }
        
        private SkiaSharp.SKBitmap RotateBitmap(SkiaSharp.SKBitmap source, int degrees)
        {
            var radians = degrees * Math.PI / 180;
            var sine = Math.Abs(Math.Sin(radians));
            var cosine = Math.Abs(Math.Cos(radians));
            var originalWidth = source.Width;
            var originalHeight = source.Height;
            
            // 回転後のサイズを計算
            var rotatedWidth = (int)(cosine * originalWidth + sine * originalHeight);
            var rotatedHeight = (int)(cosine * originalHeight + sine * originalWidth);
            
            var rotatedBitmap = new SkiaSharp.SKBitmap(rotatedWidth, rotatedHeight);
            
            using (var canvas = new SkiaSharp.SKCanvas(rotatedBitmap))
            {
                canvas.Clear(SkiaSharp.SKColors.Transparent);
                canvas.Translate(rotatedWidth / 2, rotatedHeight / 2);
                canvas.RotateDegrees(degrees);
                canvas.Translate(-originalWidth / 2, -originalHeight / 2);
                canvas.DrawBitmap(source, 0, 0);
            }
            
            return rotatedBitmap;
        }
        
        private async void ReloadRotatedPreview()
        {
            try
            {
                if (string.IsNullOrEmpty(_page.SourceImagePath) || !System.IO.File.Exists(_page.SourceImagePath))
                {
                    PreviewImage = null;
                    return;
                }
                
                // HEICファイルの特別処理
                SkiaSharp.SKBitmap originalBitmap = null;
                string tempJpegPath = null;
                
                try
                {
                    if (IsHeicFile(_page.SourceImagePath))
                    {
                        // HEICファイルの場合は先にJPEGに変換
                        tempJpegPath = await ConvertHeicToJpegForRotationAsync(_page.SourceImagePath);
                        if (string.IsNullOrEmpty(tempJpegPath))
                        {
                            PreviewImage = null;
                            return;
                        }
                        // ファイル存在チェックを少し待機（非同期処理の完了待ち）
                        for (int i = 0; i < 10; i++)
                        {
                            if (System.IO.File.Exists(tempJpegPath))
                            {
                                break;
                            }
                            await Task.Delay(100);
                        }
                        if (!System.IO.File.Exists(tempJpegPath))
                        {
                            PreviewImage = null;
                            return;
                        }
                        originalBitmap = SkiaSharp.SKBitmap.Decode(tempJpegPath);
                    }
                    else
                    {
                        // 通常の画像ファイル
                        originalBitmap = SkiaSharp.SKBitmap.Decode(_page.SourceImagePath);
                    }
                    
                    if (originalBitmap == null)
                    {
                        PreviewImage = null;
                        return;
                    }
                
                // 回転した画像を作成
                var rotatedBitmap = RotateBitmap(originalBitmap, _page.Rotation);
                
                // プレビューサイズにリサイズ
                var maxPreviewSize = 800;
                var aspectRatio = (float)rotatedBitmap.Height / rotatedBitmap.Width;
                var previewWidth = rotatedBitmap.Width > maxPreviewSize ? maxPreviewSize : rotatedBitmap.Width;
                var previewHeight = (int)(previewWidth * aspectRatio);
                
                var preview = new SkiaSharp.SKBitmap(previewWidth, previewHeight);
                using (var canvas = new SkiaSharp.SKCanvas(preview))
                {
                    using (var paint = new SkiaSharp.SKPaint())
                    {
                        paint.IsAntialias = true;
                        paint.FilterQuality = SkiaSharp.SKFilterQuality.High;
                        
                        var destRect = SkiaSharp.SKRect.Create(0, 0, previewWidth, previewHeight);
                        canvas.DrawBitmap(rotatedBitmap, destRect, paint);
                    }
                }
                
                // WPFで表示可能な形式に変換（UIスレッドで実行）
                using var data = preview.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                var stream = new System.IO.MemoryStream(data.ToArray());
                
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = stream;
                    bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    
                    PreviewImage = bitmap;
                });
                
                    // メモリクリーンアップ
                    rotatedBitmap.Dispose();
                    preview.Dispose();
                    originalBitmap?.Dispose();
                }
                finally
                {
                    // 一時ファイルのクリーンアップ
                    if (!string.IsNullOrEmpty(tempJpegPath) && System.IO.File.Exists(tempJpegPath))
                    {
                        try
                        {
                            System.IO.File.Delete(tempJpegPath);
                        }
                        catch
                        {
                            // クリーンアップエラーは無視
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"回転プレビュー生成エラー: {ex.Message}");
                PreviewImage = null;
            }
        }
        
        private void GenerateRotatedPlaceholder()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[GenerateRotatedPlaceholder] ページ {_page.PageNumber} - 回転: {_page.Rotation}度");
                
                // UIスレッドで実行することを保証
                if (System.Windows.Application.Current.Dispatcher.CheckAccess())
                {
                    GenerateRotatedPlaceholderCore();
                }
                else
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() => GenerateRotatedPlaceholderCore());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GenerateRotatedPlaceholder] エラー: {ex.Message}");
                ThumbnailImage = null;
                PreviewImage = null;
            }
        }
        
        private void GenerateRotatedPlaceholderCore()
        {
            // サムネイル用のプレースホルダーを作成
            var thumbnailWidth = 120;
            var originalAspectRatio = _page.Height / _page.Width;
            var thumbnailHeight = (int)(thumbnailWidth * originalAspectRatio);
            
            // サムネイル用プレースホルダーを作成
            var thumbnailBitmap = CreatePlaceholder(thumbnailWidth, thumbnailHeight);
            
            // プレビュー用のより大きなプレースホルダーを作成
            var previewWidth = 400;
            var previewHeight = (int)(previewWidth * originalAspectRatio);
            var previewBitmap = CreatePlaceholder(previewWidth, previewHeight);
            
            // 回転を適用
            SkiaSharp.SKBitmap finalThumbnail;
            SkiaSharp.SKBitmap finalPreview;
            
            if (_page.Rotation != 0)
            {
                finalThumbnail = RotateBitmap(thumbnailBitmap, _page.Rotation);
                finalPreview = RotateBitmap(previewBitmap, _page.Rotation);
                thumbnailBitmap.Dispose();
                previewBitmap.Dispose();
            }
            else
            {
                finalThumbnail = thumbnailBitmap;
                finalPreview = previewBitmap;
            }
            
            // サムネイル用WPF画像を作成
            using (var data = finalThumbnail.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100))
            {
                var stream = new System.IO.MemoryStream(data.ToArray());
                var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                
                ThumbnailImage = bitmap;
            }
            
            // プレビュー用WPF画像を作成
            using (var data = finalPreview.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100))
            {
                var stream = new System.IO.MemoryStream(data.ToArray());
                var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                
                PreviewImage = bitmap;
            }
            
            System.Diagnostics.Debug.WriteLine($"[GenerateRotatedPlaceholder] 完了 - サムネイル: {finalThumbnail.Width}x{finalThumbnail.Height}, プレビュー: {finalPreview.Width}x{finalPreview.Height}");
            
            finalThumbnail.Dispose();
            finalPreview.Dispose();
        }
        
        private SkiaSharp.SKBitmap CreatePlaceholder(int width, int height)
        {
            var bitmap = new SkiaSharp.SKBitmap(width, height);
            using (var canvas = new SkiaSharp.SKCanvas(bitmap))
            {
                // 白背景
                canvas.Clear(SkiaSharp.SKColors.White);

                // 枠線
                using (var paint = new SkiaSharp.SKPaint())
                {
                    paint.Color = SkiaSharp.SKColors.LightGray;
                    paint.Style = SkiaSharp.SKPaintStyle.Stroke;
                    paint.StrokeWidth = 2;
                    canvas.DrawRect(1, 1, width - 2, height - 2, paint);

                    // ページ番号
                    paint.Color = SkiaSharp.SKColors.Gray;
                    paint.Style = SkiaSharp.SKPaintStyle.Fill;
                    paint.TextSize = Math.Min(width / 5, 24);
                    paint.TextAlign = SkiaSharp.SKTextAlign.Center;
                    canvas.DrawText($"Page {_page.PageNumber}", width / 2, height / 2, paint);
                    
                    // 回転角度を表示（デバッグ用）
                    if (_page.Rotation != 0)
                    {
                        paint.Color = SkiaSharp.SKColors.Red;
                        paint.TextSize = Math.Min(width / 8, 14);
                        canvas.DrawText($"{_page.Rotation}°", width / 2, height / 2 + (height / 10), paint);
                    }
                    
                    // PDFマーカー
                    paint.Color = SkiaSharp.SKColors.DarkGray;
                    paint.TextSize = Math.Min(width / 7, 16);
                    canvas.DrawText("PDF", width / 2, height / 2 - (height / 8), paint);
                }
            }
            
            return bitmap;
        }
        
        public void UpdateRotationSync()
        {
            System.Diagnostics.Debug.WriteLine($"[UpdateRotationSync] ページ {_page.PageNumber} - 回転値: {_page.Rotation}度");
            
            // 回転値を同期
            Rotation = _page.Rotation;
            
            // プレースホルダーを再生成（PDFページの場合）
            if (string.IsNullOrEmpty(_page.SourceImagePath))
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateRotationSync] PDFページのプレースホルダー再生成");
                GenerateRotatedPlaceholderCore();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateRotationSync] 画像ページのサムネイル・プレビュー同期更新");
                // 画像ページの場合は同期的に更新
                CreateRotatedImagesSync();
            }
            
            // プロパティ変更通知
            OnPropertyChanged(nameof(ThumbnailImage));
            OnPropertyChanged(nameof(PreviewImage));
            OnPropertyChanged(nameof(Rotation));
        }
        
        private void CreateRotatedImagesSync()
        {
            try
            {
                if (string.IsNullOrEmpty(_page.SourceImagePath) || !System.IO.File.Exists(_page.SourceImagePath))
                    return;
                
                SkiaSharp.SKBitmap originalBitmap = null;
                string tempJpegPath = null;
                
                try
                {
                    if (IsHeicFile(_page.SourceImagePath))
                    {
                        // HEICファイルの場合は先に同期的にJPEGに変換
                        tempJpegPath = ConvertHeicToJpegSyncForRotation(_page.SourceImagePath);
                        if (string.IsNullOrEmpty(tempJpegPath) || !System.IO.File.Exists(tempJpegPath))
                            return;
                        originalBitmap = SkiaSharp.SKBitmap.Decode(tempJpegPath);
                    }
                    else
                    {
                        originalBitmap = SkiaSharp.SKBitmap.Decode(_page.SourceImagePath);
                    }
                    
                    if (originalBitmap == null) return;
                
                // 回転した画像を作成
                var rotatedBitmap = RotateBitmap(originalBitmap, _page.Rotation);
                
                // サムネイル作成
                var thumbnailSize = 150;
                var aspectRatio = (float)rotatedBitmap.Height / rotatedBitmap.Width;
                var thumbnailHeight = (int)(thumbnailSize * aspectRatio);
                
                var thumbnail = new SkiaSharp.SKBitmap(thumbnailSize, thumbnailHeight);
                using (var canvas = new SkiaSharp.SKCanvas(thumbnail))
                {
                    using (var paint = new SkiaSharp.SKPaint())
                    {
                        paint.IsAntialias = true;
                        paint.FilterQuality = SkiaSharp.SKFilterQuality.High;
                        var destRect = SkiaSharp.SKRect.Create(0, 0, thumbnailSize, thumbnailHeight);
                        canvas.DrawBitmap(rotatedBitmap, destRect, paint);
                    }
                }
                
                // プレビュー作成
                var maxPreviewSize = 800;
                var previewWidth = rotatedBitmap.Width > maxPreviewSize ? maxPreviewSize : rotatedBitmap.Width;
                var previewHeight = (int)(previewWidth * aspectRatio);
                
                var preview = new SkiaSharp.SKBitmap(previewWidth, previewHeight);
                using (var canvas = new SkiaSharp.SKCanvas(preview))
                {
                    using (var paint = new SkiaSharp.SKPaint())
                    {
                        paint.IsAntialias = true;
                        paint.FilterQuality = SkiaSharp.SKFilterQuality.High;
                        var destRect = SkiaSharp.SKRect.Create(0, 0, previewWidth, previewHeight);
                        canvas.DrawBitmap(rotatedBitmap, destRect, paint);
                    }
                }
                
                // WPF画像に変換
                // サムネイル
                using (var data = thumbnail.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100))
                {
                    var stream = new System.IO.MemoryStream(data.ToArray());
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = stream;
                    bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    ThumbnailImage = bitmap;
                }
                
                // プレビュー
                using (var data = preview.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100))
                {
                    var stream = new System.IO.MemoryStream(data.ToArray());
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = stream;
                    bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    PreviewImage = bitmap;
                }
                
                    // メモリクリーンアップ
                    rotatedBitmap.Dispose();
                    thumbnail.Dispose();
                    preview.Dispose();
                    originalBitmap?.Dispose();
                }
                finally
                {
                    // 一時ファイルのクリーンアップ
                    if (!string.IsNullOrEmpty(tempJpegPath) && System.IO.File.Exists(tempJpegPath))
                    {
                        try
                        {
                            System.IO.File.Delete(tempJpegPath);
                        }
                        catch
                        {
                            // クリーンアップエラーは無視
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"画像同期更新エラー: {ex.Message}");
            }
        }
        
        public void ClearPreviewImage()
        {
            PreviewImage = null;
        }
        
        /// <summary>
        /// HEICファイルかどうかを判定
        /// </summary>
        private bool IsHeicFile(string filePath)
        {
            var extension = Path.GetExtension(filePath)?.ToLowerInvariant();
            return extension == ".heic" || extension == ".heif";
        }
        
        /// <summary>
        /// HEIC変換キャッシュのクリーンアップ（アプリケーション終了時に呼び出す）
        /// </summary>
        public static void CleanupHeicCache()
        {
            try
            {
                foreach (var kvp in _heicConversionCache)
                {
                    if (File.Exists(kvp.Value))
                    {
                        try
                        {
                            File.Delete(kvp.Value);
                            System.Diagnostics.Debug.WriteLine($"[CleanupHeicCache] Deleted: {Path.GetFileName(kvp.Value)}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[CleanupHeicCache] Failed to delete {kvp.Value}: {ex.Message}");
                        }
                    }
                }
                _heicConversionCache.Clear();
                System.Diagnostics.Debug.WriteLine("[CleanupHeicCache] HEIC cache cleanup completed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CleanupHeicCache] Error during cleanup: {ex.Message}");
            }
        }
        
        /// <summary>
        /// HEICファイルを回転処理用にJPEGに変換（非同期版）
        /// </summary>
        private async Task<string> ConvertHeicToJpegForRotationAsync(string heicPath)
        {
            try
            {
                var tempJpegPath = Path.GetTempFileName() + ".jpg";
                
                await Task.Run(() =>
                {
                    using (var image = new MagickImage(heicPath))
                    {
                        image.Format = MagickFormat.Jpeg;
                        image.Quality = 95;
                        image.Write(tempJpegPath);
                    }
                });
                
                return tempJpegPath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HEIC to JPEG conversion error: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// HEICファイルを回転処理用にJPEGに変換（同期版）
        /// </summary>
        private string ConvertHeicToJpegSyncForRotation(string heicPath)
        {
            try
            {
                var tempJpegPath = Path.GetTempFileName() + ".jpg";
                
                using (var image = new MagickImage(heicPath))
                {
                    image.Format = MagickFormat.Jpeg;
                    image.Quality = 95;
                    image.Write(tempJpegPath);
                }
                
                return tempJpegPath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HEIC to JPEG sync conversion error: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 一時ファイルのクリーンアップ（PDF発行完了時に呼び出す）
        /// </summary>
        public void CleanupTempFiles()
        {
            if (!string.IsNullOrEmpty(_heicTempJpegPath) && File.Exists(_heicTempJpegPath))
            {
                try
                {
                    File.Delete(_heicTempJpegPath);
                    System.Diagnostics.Debug.WriteLine($"[CleanupTempFiles] Deleted temp JPEG: {Path.GetFileName(_heicTempJpegPath)}");
                    _heicTempJpegPath = null;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CleanupTempFiles] Failed to delete temp file: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// リソースの解放
        /// </summary>
        public void Dispose()
        {
            CleanupTempFiles();
            _loadThumbnailCts?.Cancel();
            _loadThumbnailCts?.Dispose();
        }
    }
}