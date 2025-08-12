using System.Threading.Tasks;
using SkiaSharp;

namespace DocOrganizer.Application.Interfaces
{
    /// <summary>
    /// 文字向き検出・補正サービス
    /// OCRベースの文字認識を使用して最適な向きを自動検出
    /// </summary>
    public interface ITextOrientationService
    {
        /// <summary>
        /// 文字が最も読みやすい向きを検出（0°, 90°, 180°, 270°）
        /// </summary>
        /// <param name="imagePath">画像ファイルパス</param>
        /// <returns>最適な回転角度</returns>
        Task<int> DetectOptimalOrientationAsync(string imagePath);
        
        /// <summary>
        /// 指定角度での文字認識信頼度を取得
        /// </summary>
        /// <param name="imagePath">画像ファイルパス</param>
        /// <param name="rotationDegrees">回転角度</param>
        /// <returns>文字認識信頼度（0-100）</returns>
        Task<double> GetTextConfidenceAsync(string imagePath, int rotationDegrees);
        
        /// <summary>
        /// 文字が読める向きに自動補正
        /// </summary>
        /// <param name="image">入力画像</param>
        /// <returns>補正済み画像</returns>
        Task<SKBitmap> CorrectToOptimalOrientationAsync(SKBitmap image);
        
        /// <summary>
        /// 文書内に読み取り可能な文字が存在するかチェック
        /// </summary>
        /// <param name="imagePath">画像ファイルパス</param>
        /// <returns>読み取り可能な文字が存在する場合true</returns>
        Task<bool> HasReadableTextAsync(string imagePath);
        
        /// <summary>
        /// 複数の向きを並列で検証して最適解を高速取得
        /// </summary>
        /// <param name="imagePath">画像ファイルパス</param>
        /// <returns>最適な回転角度</returns>
        Task<int> DetectOptimalOrientationParallelAsync(string imagePath);
    }
}