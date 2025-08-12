using Microsoft.Extensions.Logging;
using DocOrganizer.Application.Interfaces;
using SkiaSharp;
using System.Threading.Tasks;
using System.IO;

namespace DocOrganizer.Infrastructure.Services
{
    /// <summary>
    /// IronOCR初期化問題の一時的回避用モックサービス
    /// </summary>
    public class MockTextOrientationService : ITextOrientationService
    {
        private readonly ILogger<MockTextOrientationService> _logger;

        public MockTextOrientationService(ILogger<MockTextOrientationService> logger)
        {
            _logger = logger;
        }

        public async Task<int> DetectOptimalOrientationAsync(string imagePath)
        {
            _logger.LogInformation($"Mock: Detecting orientation for {Path.GetFileName(imagePath)}");
            await Task.Delay(100); // Simulate processing
            return 0; // Always return no rotation needed
        }

        public async Task<double> GetTextConfidenceAsync(string imagePath, int rotationDegrees)
        {
            _logger.LogInformation($"Mock: Getting text confidence for {Path.GetFileName(imagePath)} at {rotationDegrees}°");
            await Task.Delay(50);
            return 85.0; // Return good confidence
        }

        public async Task<SKBitmap> CorrectToOptimalOrientationAsync(SKBitmap image)
        {
            _logger.LogInformation("Mock: Correcting image orientation");
            await Task.Delay(10);
            return image; // Return image unchanged
        }

        public async Task<bool> HasReadableTextAsync(string imagePath)
        {
            _logger.LogInformation($"Mock: Checking readable text for {Path.GetFileName(imagePath)}");
            await Task.Delay(50);
            return true; // Assume all images have text
        }

        public async Task<int> DetectOptimalOrientationParallelAsync(string imagePath)
        {
            _logger.LogInformation($"Mock: Parallel orientation detection for {Path.GetFileName(imagePath)}");
            await Task.Delay(100);
            return 0; // No rotation needed
        }
    }
}