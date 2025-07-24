using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DocOrganizer.Core.Models;
using SkiaSharp;

namespace DocOrganizer.UI.ViewModels
{
    public partial class PageViewModel : ObservableObject
    {
        private readonly PdfPage _page;
        
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
                // 画像ファイルから直接サムネイルを生成
                if (!string.IsNullOrEmpty(_page.SourceImagePath) && System.IO.File.Exists(_page.SourceImagePath))
                {
                    _ = Task.Run(() => LoadThumbnailFromImage());
                    _ = Task.Run(() => LoadPreviewFromImage());
                }
                // PdfPageからサムネイル画像を取得
                else if (_page.ThumbnailImage != null)
                {
                    // SkiaSharpのSKBitmapをWPFで表示可能な形式に変換
                    using var data = _page.ThumbnailImage.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = new System.IO.MemoryStream(data.ToArray());
                    bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    ThumbnailImage = bitmap;
                    PreviewImage = bitmap; // プレビューにも同じ画像を設定
                }
                else
                {
                    // サムネイルがない場合はプレースホルダーを表示
                    ThumbnailImage = null;
                    PreviewImage = null;
                }
            }
            catch (Exception ex)
            {
                // サムネイル読み込みエラーをログに記録
                System.Diagnostics.Debug.WriteLine($"サムネイル読み込みエラー Page {_page.PageNumber}: {ex.Message}");
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
            Rotation = _page.Rotation;
            // 画像ベースのページの場合、回転したサムネイルとプレビューを再生成
            if (!string.IsNullOrEmpty(_page.SourceImagePath))
            {
                ReloadRotatedThumbnail();
                ReloadRotatedPreview();
            }
            else
            {
                LoadThumbnail(); // 通常のPDFページ
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
    }
}