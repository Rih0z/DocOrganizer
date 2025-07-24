using System;
using SkiaSharp;

namespace DocOrganizer.Core.Models
{
    /// <summary>
    /// PDFのページを表すモデルクラス
    /// </summary>
    public class PdfPage : IDisposable
    {
        private int _rotation;
        private SKBitmap? _thumbnailImage;
        private SKBitmap? _previewImage;
        private bool _disposed;

        /// <summary>
        /// ページ番号（1から始まる）
        /// </summary>
        public int PageNumber { get; }

        /// <summary>
        /// ページの回転角度（0, 90, 180, 270度）
        /// </summary>
        public int Rotation
        {
            get => _rotation;
            set
            {
                if (value != 0 && value != 90 && value != 180 && value != 270)
                    throw new ArgumentException("Rotation must be 0, 90, 180, or 270 degrees", nameof(value));
                _rotation = value;
            }
        }

        /// <summary>
        /// ページの幅（ポイント単位）
        /// </summary>
        public float Width { get; private set; }

        /// <summary>
        /// ページの高さ（ポイント単位）
        /// </summary>
        public float Height { get; private set; }

        /// <summary>
        /// ページが選択されているかどうか
        /// </summary>
        public bool IsSelected { get; set; }

        /// <summary>
        /// サムネイル画像
        /// </summary>
        public SKBitmap? ThumbnailImage => _thumbnailImage;

        /// <summary>
        /// プレビュー画像
        /// </summary>
        public SKBitmap? PreviewImage => _previewImage;

        /// <summary>
        /// リソースが破棄されたかどうか
        /// </summary>
        public bool IsDisposed => _disposed;

        /// <summary>
        /// 元の画像ファイルパス（遅延変換用）
        /// </summary>
        public string? SourceImagePath { get; set; }

        /// <summary>
        /// PdfPageの新しいインスタンスを初期化します
        /// </summary>
        /// <param name="pageNumber">ページ番号（1から始まる）</param>
        public PdfPage(int pageNumber)
        {
            if (pageNumber <= 0)
                throw new ArgumentException("Page number must be greater than 0", nameof(pageNumber));

            PageNumber = pageNumber;
            _rotation = 0;
        }

        /// <summary>
        /// ページの寸法を設定します
        /// </summary>
        /// <param name="width">幅（ポイント単位）</param>
        /// <param name="height">高さ（ポイント単位）</param>
        public void SetDimensions(float width, float height)
        {
            if (width <= 0)
                throw new ArgumentException("Width must be greater than 0", nameof(width));
            if (height <= 0)
                throw new ArgumentException("Height must be greater than 0", nameof(height));

            Width = width;
            Height = height;
        }

        /// <summary>
        /// サムネイル画像を設定します
        /// </summary>
        /// <param name="bitmap">サムネイル画像</param>
        public void SetThumbnailImage(SKBitmap? bitmap)
        {
            _thumbnailImage?.Dispose();
            _thumbnailImage = bitmap != null ? bitmap.Copy() : null;
        }

        /// <summary>
        /// プレビュー画像を設定します
        /// </summary>
        /// <param name="bitmap">プレビュー画像</param>
        public void SetPreviewImage(SKBitmap? bitmap)
        {
            _previewImage?.Dispose();
            _previewImage = bitmap != null ? bitmap.Copy() : null;
        }

        /// <summary>
        /// 回転を考慮した実効的な寸法を取得します
        /// </summary>
        /// <returns>実効的な幅と高さのタプル</returns>
        public (float width, float height) GetEffectiveDimensions()
        {
            if (_rotation == 90 || _rotation == 270)
            {
                return (Height, Width);
            }
            return (Width, Height);
        }

        /// <summary>
        /// リソースを解放します
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// リソースを解放します
        /// </summary>
        /// <param name="disposing">マネージドリソースを解放する場合はtrue</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _thumbnailImage?.Dispose();
                _thumbnailImage = null;
                _previewImage?.Dispose();
                _previewImage = null;
            }

            _disposed = true;
        }
    }
}