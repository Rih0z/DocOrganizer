using SkiaSharp;
using System.Threading.Tasks;

namespace DocOrganizer.Application.Interfaces
{
    /// <summary>
    /// 統一回転処理インターフェース
    /// 全ての画像回転処理を一箇所で管理し、ログ出力する
    /// </summary>
    public interface IRotationService
    {
        /// <summary>
        /// 画像ファイルから必要な回転角度を検出
        /// </summary>
        Task<int> DetectRequiredRotationAsync(string imagePath);
        
        /// <summary>
        /// SkiaSharp画像を回転（ログ出力付き）
        /// </summary>
        SKBitmap RotateImage(SKBitmap source, int rotationDegrees, string operationId = "");
        
        /// <summary>
        /// EXIF自動回転を完全無効化して画像を読み込み
        /// </summary>
        Task<SKBitmap> LoadImageWithoutAutoRotationAsync(string imagePath);
        
        /// <summary>
        /// 画像ファイルのEXIF Orientation値を取得
        /// </summary>
        Task<int> GetExifOrientationAsync(string imagePath);
        
        /// <summary>
        /// ログ出力の有効/無効を設定
        /// </summary>
        void SetLoggingEnabled(bool enabled);
        
        /// <summary>
        /// 回転処理統計を取得
        /// </summary>
        RotationStatistics GetStatistics();
    }
    
    /// <summary>
    /// 回転処理統計情報
    /// </summary>
    public class RotationStatistics
    {
        public int TotalRotations { get; set; }
        public int ExifDetections { get; set; }
        public int AutoRotationsPrevented { get; set; }
        public string LastOperationId { get; set; } = "";
        public string LastRotationDetails { get; set; } = "";
    }
}