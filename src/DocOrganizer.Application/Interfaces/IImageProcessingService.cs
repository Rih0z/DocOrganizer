using System.Collections.Generic;
using System.Threading.Tasks;
using TaxDocOrganizer.Core.Models;

namespace DocOrganizer.Application.Interfaces
{
    public interface IImageProcessingService
    {
        Task<PdfDocument> ConvertImageToPdfAsync(string imagePath);
        Task<PdfDocument> ConvertImagesToPdfAsync(IEnumerable<string> imagePaths);
        Task<byte[]> GetImageThumbnailAsync(string imagePath, int width = 150, int height = 150);
        Task<bool> IsValidImageAsync(string imagePath);
        Task<string> GetImageInfoAsync(string imagePath);
    }
}