using System.Threading.Tasks;
using DocOrganizer.Application.Interfaces;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace DocOrganizer.Infrastructure.Services
{
    /// <summary>
    /// OCR機能無効時のダミーテキスト方向検出サービス
    /// </summary>
    public class NoOpTextOrientationService : ITextOrientationService
    {
        private readonly ILogger<NoOpTextOrientationService> _logger;

        public NoOpTextOrientationService(ILogger<NoOpTextOrientationService> logger)
        {
            _logger = logger;
            _logger.LogDebug("[OCR無効] NoOpTextOrientationServiceを使用");
        }

        /// <summary>
        /// 常に0度（正常方向）を返す
        /// </summary>
        public Task<double> DetectOrientationAsync(string imagePath)
        {
            _logger.LogDebug($"[OCR無効] 画像方向検出スキップ: {imagePath}");
            return Task.FromResult(0.0);
        }

        /// <summary>
        /// 常に0度（正常方向）を返す
        /// </summary>
        public Task<double> DetectOrientationFromStreamAsync(byte[] imageData)
        {
            _logger.LogDebug("[OCR無効] ストリームからの画像方向検出スキップ");
            return Task.FromResult(0.0);
        }

        /// <summary>
        /// 文字が最も読みやすい向きを検出（0°, 90°, 180°, 270°）
        /// OCR無効時は常に0度を返す
        /// </summary>
        public Task<int> DetectOptimalOrientationAsync(string imagePath)
        {
            _logger.LogDebug($"[OCR無効] 最適方向検出スキップ: {imagePath}");
            return Task.FromResult(0);
        }
        
        /// <summary>
        /// 指定角度での文字認識信頼度を取得
        /// OCR無効時は常に0を返す
        /// </summary>
        public Task<double> GetTextConfidenceAsync(string imagePath, int rotationDegrees)
        {
            _logger.LogDebug($"[OCR無効] 文字認識信頼度チェックスキップ: {imagePath}, 角度: {rotationDegrees}");
            return Task.FromResult(0.0);
        }
        
        /// <summary>
        /// 文字が読める向きに自動補正
        /// OCR無効時は入力画像をそのまま返す
        /// </summary>
        public Task<SKBitmap> CorrectToOptimalOrientationAsync(SKBitmap image)
        {
            _logger.LogDebug("[OCR無効] 画像方向補正スキップ");
            return Task.FromResult(image);
        }
        
        /// <summary>
        /// 文書内に読み取り可能な文字が存在するかチェック
        /// OCR無効時は常にfalseを返す
        /// </summary>
        public Task<bool> HasReadableTextAsync(string imagePath)
        {
            _logger.LogDebug($"[OCR無効] 読み取り可能文字チェックスキップ: {imagePath}");
            return Task.FromResult(false);
        }
        
        /// <summary>
        /// 複数の向きを並列で検証して最適解を高速取得
        /// OCR無効時は常に0度を返す
        /// </summary>
        public Task<int> DetectOptimalOrientationParallelAsync(string imagePath)
        {
            _logger.LogDebug($"[OCR無効] 並列方向検出スキップ: {imagePath}");
            return Task.FromResult(0);
        }

        /// <summary>
        /// OCRが無効であることを示すfalseを返す
        /// </summary>
        public bool IsAvailable()
        {
            return false;
        }

        /// <summary>
        /// 初期化処理（何もしない）
        /// </summary>
        public Task InitializeAsync()
        {
            _logger.LogInformation("[OCR無効] NoOpTextOrientationService初期化完了");
            return Task.CompletedTask;
        }
    }
}