// System.Windows.Rect を使用

namespace DocOrganizer.Application.Models.V3
{
    /// <summary>
    /// プレビューの現在の状態を表すクラス（WYSIWYG PDF出力用）
    /// </summary>
    public class PreviewState
    {
        /// <summary>
        /// 原寸大表示かどうか
        /// </summary>
        public bool IsOriginalSize { get; set; }

        /// <summary>
        /// 現在のズームレベル（パーセンテージ）
        /// </summary>
        public double CurrentZoomPercentage { get; set; }

        /// <summary>
        /// 現在のビューポート矩形
        /// </summary>
        public System.Windows.Rect CurrentViewportRect { get; set; }

        /// <summary>
        /// A4フィットモードかどうか
        /// </summary>
        public bool IsA4Fit => !IsOriginalSize;
    }
}