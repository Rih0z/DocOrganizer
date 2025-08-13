using System.Collections.Generic;
using System.Threading.Tasks;
using DocOrganizer.Core.Models;

namespace DocOrganizer.Application.Interfaces
{
    public interface IImageProcessingService
    {
        Task<PdfDocument> ConvertImageToPdfAsync(string imagePath);
        Task<PdfDocument> ConvertImagesToPdfAsync(IEnumerable<string> imagePaths);
        Task<byte[]> GetImageThumbnailAsync(string imagePath, int width = 150, int height = 150);
        Task<byte[]> GetImageThumbnailAsync(string imagePath, int width, int height, int rotationDegrees);
        Task<bool> IsValidImageAsync(string imagePath);
        Task<string> GetImageInfoAsync(string imagePath);
        Task<SkiaSharp.SKBitmap?> GenerateHighQualityPreviewAsync(string imagePath, int maxWidth = 1200, int maxHeight = 1600);
        
        /// <summary>
        /// 統一回転処理 - 一時的回転関数の代替
        /// </summary>
        SkiaSharp.SKBitmap RotateImage(SkiaSharp.SKBitmap source, int rotationDegrees);
        
        /// <summary>
        /// 画像をEXIF情報完全削除して読み込み（90度回転問題完全解決）
        /// </summary>
        Task<SkiaSharp.SKBitmap?> LoadImageWithoutExifAsync(string imagePath);
        
        /// <summary>
        /// WPF用にEXIF完全削除済み画像を生成（PNG再エンコード）
        /// </summary>
        Task<byte[]?> GenerateExifFreeImageForWpfAsync(string imagePath, int targetWidth = 600, int targetHeight = 800);
    }
}