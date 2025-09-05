using System;

namespace DocOrganizer.Core.Config
{
    /// <summary>
    /// OCR機能の設定管理クラス
    /// </summary>
    public static class OcrConfig
    {
        /// <summary>
        /// OCR機能が有効かどうか
        /// 環境変数 DOCORGANIZER_OCR_ENABLED で制御
        /// </summary>
        public static bool IsOcrEnabled => 
            Environment.GetEnvironmentVariable("DOCORGANIZER_OCR_ENABLED") == "true";

        /// <summary>
        /// OCRファイルの出力パス
        /// </summary>
        public static string OcrDataPath => 
            Environment.GetEnvironmentVariable("DOCORGANIZER_OCR_PATH") ?? 
            ".ocr";

        /// <summary>
        /// OCR機能の初期化
        /// </summary>
        public static void Initialize()
        {
            if (IsOcrEnabled)
            {
                System.Diagnostics.Debug.WriteLine($"[OCR] OCR機能有効 - データパス: {OcrDataPath}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[OCR] OCR機能無効");
            }
        }

        /// <summary>
        /// ビルド時のOCR設定確認
        /// </summary>
        public static string GetBuildConfiguration()
        {
            return IsOcrEnabled ? "OCR_ENABLED" : "OCR_DISABLED";
        }
    }
}