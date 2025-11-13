using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using DocOrganizer.Core.Models;
using DocOrganizer.Application.Interfaces;
using DocOrganizer.Application.Interfaces.V3;
using SkiaSharp;

namespace DocOrganizer.UI.ViewModels
{
    /// <summary>
    /// 🎯 V3完全実装: OSS標準PageViewModel
    /// 責務: V2コード完全削除・V3サービス専用実装
    /// 目標: 左右表示の完全実現・90度回転バグ根絶
    /// </summary>
    public partial class V3PageViewModel : ObservableObject, IDisposable
    {
        private readonly PdfPage _page;
        private readonly IThumbnailGeneratorService _thumbnailService;
        // 🎯 V3修正: IImageProcessingService依存関係削除
        private readonly ITextOrientationService? _textOrientationService;
        
        [ObservableProperty]
        private int pageNumber;
        
        [ObservableProperty]
        private bool isSelected;
        
        [ObservableProperty]
        private BitmapSource? thumbnailImage;
        
        [ObservableProperty]
        private BitmapSource? previewImage;
        
        [ObservableProperty]
        private int rotation;

        /// <summary>
        /// 対応するPDFページ
        /// </summary>
        public PdfPage Page => _page;

        /// <summary>
        /// ページの一意識別子
        /// </summary>
        public Guid Id => _page.Id;

        public V3PageViewModel(
            PdfPage page, 
            IThumbnailGeneratorService thumbnailService,
            ITextOrientationService? textOrientationService = null)
        {
            _page = page;
            _thumbnailService = thumbnailService;
            _textOrientationService = textOrientationService;
            PageNumber = page.PageNumber;
            Rotation = page.Rotation;
        }

        /// <summary>
        /// 🎯 V3 OSS標準: 左側サムネイル生成
        /// </summary>
        public async Task LoadLeftThumbnailAsync()
        {
            try
            {
                // V3.0.083: まず既存のサムネイル画像を確認（Undo時の復元画像対応）
                if (_page.ThumbnailImage != null)
                {
                    // SKBitmapをBitmapSourceに変換
                    var bitmap = ConvertSKBitmapToBitmapSource(_page.ThumbnailImage);
                    if (bitmap != null)
                    {
                        if (bitmap.CanFreeze && !bitmap.IsFrozen)
                        {
                            bitmap.Freeze();
                        }
                        ThumbnailImage = bitmap;
                        return;
                    }
                }
                
                // 既存の画像がない場合は、SourceImagePathから生成
                if (!string.IsNullOrEmpty(_page.SourceImagePath) && File.Exists(_page.SourceImagePath))
                {
                    var thumbnailImageSource = await _thumbnailService.GenerateLeftPanelThumbnailAsync(_page.SourceImagePath, Rotation);
                    if (thumbnailImageSource is BitmapSource bitmapSource)
                    {
                        // 🔧 アーキテクチャレベル修正: BitmapSourceをFreezeして不変化
                        // これによりガーベージコレクションによる解放を防ぎ、画像が永続的に保持される
                        if (bitmapSource.CanFreeze && !bitmapSource.IsFrozen)
                        {
                            bitmapSource.Freeze();
                        }
                        ThumbnailImage = bitmapSource;
                    }
                }
                else
                {
                    // SKBitmap を BitmapSource に変換できないため、プレースホルダーを使用
                    ThumbnailImage = CreateErrorPlaceholder();
                }
            }
            catch
            {
                ThumbnailImage = CreateErrorPlaceholder();
            }
        }

        /// <summary>
        /// 🎯 V3 OSS標準: 右側プレビュー生成
        /// </summary>
        public async Task LoadRightPreviewAsync()
        {
            try
            {
                if (!string.IsNullOrEmpty(_page.SourceImagePath) && File.Exists(_page.SourceImagePath))
                {
                    var previewImageSource = await _thumbnailService.GenerateRightPreviewImageAsync(_page.SourceImagePath, Rotation);
                    PreviewImage = previewImageSource as BitmapSource;
                }
                else
                {
                    // SKBitmap を BitmapSource に変換できないため、プレースホルダーを使用
                    PreviewImage = CreateErrorPlaceholder();
                }
            }
            catch
            {
                PreviewImage = CreateErrorPlaceholder();
            }
        }

        /// <summary>
        /// エラー時プレースホルダー作成
        /// </summary>
        private BitmapSource CreateErrorPlaceholder()
        {
            try
            {
                var width = 150;
                var height = 200;
                var dpi = 96.0;
                
                var bitmap = new WriteableBitmap(width, height, dpi, dpi, System.Windows.Media.PixelFormats.Bgr24, null);
                
                // グレー背景
                var grayColor = new byte[] { 128, 128, 128 };
                var stride = width * 3;
                var pixels = new byte[height * stride];
                
                for (int i = 0; i < pixels.Length; i += 3)
                {
                    pixels[i] = grayColor[0];     // B
                    pixels[i + 1] = grayColor[1]; // G
                    pixels[i + 2] = grayColor[2]; // R
                }
                
                bitmap.WritePixels(new System.Windows.Int32Rect(0, 0, width, height), pixels, stride, 0);
                bitmap.Freeze();
                
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// SKBitmapをBitmapSourceに変換
        /// </summary>
        private BitmapSource ConvertSKBitmapToBitmapSource(SKBitmap skBitmap)
        {
            try
            {
                if (skBitmap == null) return null;
                
                var width = skBitmap.Width;
                var height = skBitmap.Height;
                var dpi = 96.0;
                
                // SKBitmapのピクセルデータを取得
                var pixels = skBitmap.Pixels;
                var stride = width * 4; // BGRA32形式
                var pixelData = new byte[height * stride];
                
                // SKColorからBGRA形式に変換
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        var skColor = pixels[y * width + x];
                        var index = (y * width + x) * 4;
                        
                        pixelData[index] = skColor.Blue;      // B
                        pixelData[index + 1] = skColor.Green;  // G
                        pixelData[index + 2] = skColor.Red;    // R
                        pixelData[index + 3] = skColor.Alpha;  // A
                    }
                }
                
                // BitmapSourceを作成
                var bitmap = BitmapSource.Create(
                    width, height,
                    dpi, dpi,
                    System.Windows.Media.PixelFormats.Bgra32,
                    null,
                    pixelData,
                    stride);
                
                // Freezeして不変にする
                if (bitmap.CanFreeze && !bitmap.IsFrozen)
                {
                    bitmap.Freeze();
                }
                
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 🎯 V3 OSS標準: 90度回転処理
        /// </summary>
        public async Task RotateLeftAsync()
        {
            try
            {
                Rotation = (Rotation - 90) % 360;
                if (Rotation < 0) Rotation += 360;
                
                // 新しい回転を適用してサムネイル再生成
                await LoadLeftThumbnailAsync();
                await LoadRightPreviewAsync();
            }
            catch
            {
                // 回転エラー時は何もしない
            }
        }

        /// <summary>
        /// 🎯 V3 OSS標準: 90度回転処理
        /// </summary>
        public async Task RotateRightAsync()
        {
            try
            {
                Rotation = (Rotation + 90) % 360;
                
                // 新しい回転を適用してサムネイル再生成
                await LoadLeftThumbnailAsync();
                await LoadRightPreviewAsync();
            }
            catch
            {
                // 回転エラー時は何もしない
            }
        }

        /// <summary>
        /// 🎯 V3 OSS標準: 同期回転更新（PageOperationViewModel用）
        /// </summary>
        public void UpdateRotationSync()
        {
            // ViewModelの回転プロパティを基盤PdfPageと同期
            Rotation = _page.Rotation;
        }

        /// <summary>
        /// 🎯 V3 OSS標準: 回転後サムネイル再生成（PageOperationViewModel用）
        /// </summary>
        public async Task RegenerateThumbnailAfterRotationAsync()
        {
            try
            {
                // 🚀 V3.0.143: Phase 1 - キャッシュ高速パス（10-30ms）
                if (_page.ThumbnailImage != null)
                {
                    SkiaSharp.SKBitmap? rotatedBitmap = null;
                    var leftThumbnail = _thumbnailService.GenerateBitmapSourceFromCache(
                        _page.ThumbnailImage,
                        Rotation,
                        out rotatedBitmap);

                    if (leftThumbnail != null && rotatedBitmap != null)
                    {
                        // ✅ PdfPage.SetThumbnailImageが自動的に古いBitmapをDispose
                        _page.SetThumbnailImage(rotatedBitmap);

                        // ✅ UIに表示
                        ThumbnailImage = leftThumbnail;

                        // ✅ 右側プレビューは更新しない（選択時のみ必要）
                        // LoadRightPreviewAsyncは呼ばない

                        return; // ✅ 10-30msで完了
                    }
                    // else: キャッシュ生成失敗 → Phase 2へフォールバック
                    // rotatedBitmapは既にDispose済み（GenerateBitmapSourceFromCache内で）
                }

                // 🔄 V3.0.143: Phase 2 - フォールバック（200-500ms）
                // キャッシュがない、またはキャッシュ回転失敗時
                await LoadLeftThumbnailAsync();   // ✅ ファイルから再生成
                await LoadRightPreviewAsync();    // ✅ 選択中の場合のみ必要
            }
            catch (Exception ex)
            {
                // ✅ エラー時はプレースホルダー表示
                ThumbnailImage = CreateErrorPlaceholder();
                System.Diagnostics.Trace.WriteLine($"[RegenerateThumbnail] エラー - Page {PageNumber}: {ex.Message}");
            }
        }

        /// <summary>
        /// 🎯 V3 OSS標準: ページ番号更新（PageOperationViewModel用）
        /// </summary>
        public void UpdatePageNumber(int newPageNumber)
        {
            // PdfPage.PageNumber は読み取り専用のため、ViewModelのプロパティのみ更新
            PageNumber = newPageNumber;
        }

        /// <summary>
        /// 🎯 V3最適化: モデルからViewModelの状態を更新（RefreshPageList最適化用）
        /// 既存のサムネイルを保持しながら、モデルの状態のみ更新
        /// </summary>
        public async Task<bool> UpdateFromModelAsync(PdfPage newPage)
        {
            try
            {
                // ページ番号の更新
                PageNumber = newPage.PageNumber;
                
                // 回転が変更された場合のみサムネイル再生成
                if (Rotation != newPage.Rotation)
                {
                    Rotation = newPage.Rotation;
                    await LoadLeftThumbnailAsync();
                    await LoadRightPreviewAsync();  // V3.0.099: 右側プレビューも更新
                    return true; // サムネイル再生成実行
                }
                
                return false; // サムネイル再生成不要
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 🎯 V3最適化: 回転を考慮したサムネイル生成（初回生成用）
        /// 回転がある場合でも確実にサムネイルを生成
        /// </summary>
        public async Task LoadThumbnailWithRotationAsync()
        {
            try
            {
                // 通常のサムネイル生成
                await LoadLeftThumbnailAsync();
                
                // サムネイル生成に失敗し、かつ回転がある場合は再試行
                if (ThumbnailImage == null && Rotation != 0)
                {
                    // 回転状態を再確認してリトライ
                    UpdateRotationSync();
                    await LoadLeftThumbnailAsync();
                }
            }
            catch
            {
                ThumbnailImage = CreateErrorPlaceholder();
            }
        }

        /// <summary>
        /// 🚨 V3.0.150: IsSelected変更時のデバッグログ
        /// </summary>
        partial void OnIsSelectedChanged(bool value)
        {
            DocOrganizer.Core.Logging.DebugLogger.Log($"[V3PageViewModel] IsSelected変更: PageId={Id}, PageNumber={PageNumber}, IsSelected={value}, StackTrace={Environment.StackTrace}");
        }

        /// <summary>
        /// リソース解放
        /// </summary>
        public void Dispose()
        {
            // 必要に応じてリソース解放
        }
    }
}