using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using DocOrganizer.Core.Models;
using DocOrganizer.Application.Interfaces;
using DocOrganizer.Application.Interfaces.V3;

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
                if (!string.IsNullOrEmpty(_page.SourceImagePath) && File.Exists(_page.SourceImagePath))
                {
                    var thumbnailImageSource = await _thumbnailService.GenerateLeftPanelThumbnailAsync(_page.SourceImagePath, Rotation);
                    if (thumbnailImageSource is BitmapSource bitmapSource)
                    {
                        // 🔧 アーキテクチャレベル修正: BitmapSourceをFreezeして不変化
                        // これによりガベージコレクションによる解放を防ぎ、画像が永続的に保持される
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
                // 回転適用後のサムネイル再生成
                await LoadLeftThumbnailAsync();
                await LoadRightPreviewAsync();
            }
            catch
            {
                // サムネイル再生成エラー時は何もしない
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
        /// リソース解放
        /// </summary>
        public void Dispose()
        {
            // 必要に応じてリソース解放
        }
    }
}