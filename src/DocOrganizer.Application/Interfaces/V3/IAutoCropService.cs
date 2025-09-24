using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DocOrganizer.Application.Interfaces.V3
{
    /// <summary>
    /// 画像余白自動削除サービス
    /// </summary>
    public interface IAutoCropService
    {
        /// <summary>
        /// 画像の余白を自動削除
        /// </summary>
        /// <param name="source">元画像</param>
        /// <returns>余白削除後の画像</returns>
        Task<BitmapSource> AutoCropAsync(BitmapSource source);

        /// <summary>
        /// ファイルパスから直接余白削除（高速版）
        /// </summary>
        /// <param name="imagePath">画像ファイルパス</param>
        /// <param name="fuzzPercentage">色の許容範囲（デフォルト1%）</param>
        /// <returns>余白削除後の画像バイト配列</returns>
        Task<byte[]> TrimWhitespaceAsync(string imagePath, double fuzzPercentage = 1.0);

        /// <summary>
        /// クロップ領域の分析
        /// </summary>
        /// <param name="source">分析対象画像</param>
        /// <returns>クロップ情報</returns>
        Task<CropInfo> AnalyzeCropAreaAsync(BitmapSource source);
    }

    /// <summary>
    /// クロップ情報
    /// </summary>
    public class CropInfo
    {
        public int Left { get; set; }
        public int Top { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public double CropRatio { get; set; }
        public bool WasCropped { get; set; }
    }
}