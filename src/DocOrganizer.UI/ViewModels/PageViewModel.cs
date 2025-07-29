using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DocOrganizer.Core.Models;
using SkiaSharp;

namespace DocOrganizer.UI.ViewModels
{
    public partial class PageViewModel : ObservableObject
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
                
                // 画像ファイルから直接サムネイルを生成
                if (!string.IsNullOrEmpty(_page.SourceImagePath) && System.IO.File.Exists(_page.SourceImagePath))
                {
                    System.Diagnostics.Debug.WriteLine($"[LoadThumbnail] 画像ファイルから生成: {_page.SourceImagePath}");
                    _ = Task.Run(() => LoadThumbnailFromImage());
                    _ = Task.Run(() => LoadPreviewFromImage());
                }
                // PdfPageからサムネイル画像を取得
                else if (_page.ThumbnailImage != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[LoadThumbnail] PDFページのサムネイル変換 - Size: {_page.ThumbnailImage.Width}x{_page.ThumbnailImage.Height}");
                    
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
                    PreviewImage = bitmap; // プレビューにも同じ画像を設定
                    
                    System.Diagnostics.Debug.WriteLine($"[LoadThumbnail] サムネイル設定完了");
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
        
        private async void LoadThumbnailFromImage()
        {
            try
            {
                using var originalBitmap = SkiaSharp.SKBitmap.Decode(_page.SourceImagePath);
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
                });
                
                thumbnail.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"画像サムネイル生成エラー: {ex.Message}");
            }
        }
        
        private async void LoadPreviewFromImage()
        {
            try
            {
                using var originalBitmap = SkiaSharp.SKBitmap.Decode(_page.SourceImagePath);
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
                });
                
                preview.Dispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"プレビュー画像生成エラー: {ex.Message}");
            }
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
                
                // 画像を読み込んで回転を適用
                using var originalBitmap = SkiaSharp.SKBitmap.Decode(_page.SourceImagePath);
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
                
                // 画像を読み込んで回転を適用
                using var originalBitmap = SkiaSharp.SKBitmap.Decode(_page.SourceImagePath);
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
    }
}