using System;
using System.IO;
using System.Reflection;

namespace DocOrganizer.Core.Config
{
    /// <summary>
    /// DocOrganizer統一環境変数管理システム
    /// 
    /// 全ての環境変数を一元管理し、型安全性とデフォルト値を保証
    /// 既存コードから分散していた環境変数アクセスを統一し、
    /// 保守性とテスタビリティを向上させる。
    /// 
    /// 設計原則:
    /// - 単一責任：環境変数アクセスの統一管理
    /// - 型安全：強い型付けによるランタイムエラー防止
    /// - デフォルト値：設定不要での動作保証
    /// - 後方互換：既存環境変数との互換性維持
    /// </summary>
    public static class EnvironmentConfig
    {
        #region デバッグ・ログ設定
        
        /// <summary>
        /// デバッグモードの有効/無効
        /// 環境変数: DOCORGANIZER_DEBUG
        /// デフォルト: false
        /// </summary>
        public static bool IsDebugEnabled => 
            GetBooleanValue("DOCORGANIZER_DEBUG", false);
            
        /// <summary>
        /// デバッグログファイルの出力パス
        /// 環境変数: DOCORGANIZER_LOG_PATH
        /// デフォルト: .logs/debug.log
        /// </summary>
        public static string LogPath => 
            GetStringValue("DOCORGANIZER_LOG_PATH", ".logs/debug.log");
            
        #endregion
        
        #region OCR機能設定
        
        /// <summary>
        /// OCR（光学文字認識）機能の有効/無効
        /// 環境変数: DOCORGANIZER_OCR_ENABLED
        /// デフォルト: false
        /// </summary>
        public static bool IsOcrEnabled => 
            GetBooleanValue("DOCORGANIZER_OCR_ENABLED", false);
            
        /// <summary>
        /// OCRデータファイルの配置パス
        /// 環境変数: DOCORGANIZER_OCR_PATH
        /// デフォルト: .ocr
        /// </summary>
        public static string OcrDataPath => 
            GetStringValue("DOCORGANIZER_OCR_PATH", ".ocr");
            
        #endregion
        
        #region PDF処理設定
        
        /// <summary>
        /// PDFキャッシュサイズ（MB単位）
        /// 環境変数: DOCORGANIZER_PDF_CACHE_SIZE
        /// デフォルト: 100MB
        /// </summary>
        public static int PdfCacheSize => 
            GetIntegerValue("DOCORGANIZER_PDF_CACHE_SIZE", 100);
            
        /// <summary>
        /// PDF出力品質設定（High/Medium/Low）
        /// 環境変数: DOCORGANIZER_PDF_QUALITY
        /// デフォルト: High
        /// </summary>
        public static string PdfQuality => 
            GetStringValue("DOCORGANIZER_PDF_QUALITY", "High");
            
        #endregion
        
        #region パフォーマンス設定
        
        /// <summary>
        /// 並列処理スレッド数
        /// 環境変数: DOCORGANIZER_THREAD_COUNT
        /// デフォルト: "auto" (CPU論理コア数に基づく自動設定)
        /// </summary>
        public static string ThreadCountSetting => 
            GetStringValue("DOCORGANIZER_THREAD_COUNT", "auto");
            
        /// <summary>
        /// 実際の並列処理スレッド数（整数値）
        /// "auto"の場合はCPU論理コア数、それ以外は指定値
        /// </summary>
        public static int ActualThreadCount
        {
            get
            {
                var setting = ThreadCountSetting;
                if (setting.Equals("auto", StringComparison.OrdinalIgnoreCase))
                {
                    return Environment.ProcessorCount;
                }
                return GetIntegerValue("DOCORGANIZER_THREAD_COUNT", Environment.ProcessorCount);
            }
        }
        
        /// <summary>
        /// アプリケーションの最大メモリ使用量制限（MB）
        /// 環境変数: DOCORGANIZER_MEMORY_LIMIT
        /// デフォルト: 1024MB (1GB)
        /// </summary>
        public static int MemoryLimitMB => 
            GetIntegerValue("DOCORGANIZER_MEMORY_LIMIT", 1024);
            
        #endregion
        
        #region 外部ツール統合（後方互換）
        
        /// <summary>
        /// GhostScriptバイナリパス（後方互換対応）
        /// 
        /// 優先順位:
        /// 1. DOCORGANIZER_GS_PATH (推奨・統一名)
        /// 2. GS_BIN_PATH (既存・後方互換)
        /// 3. GHOSTSCRIPT_BIN (既存・後方互換)  
        /// 4. デフォルトパス検索
        /// 
        /// 注意: V3.0.031以降、内蔵PDF処理エンジンに移行済み
        /// このプロパティは6ヶ月間の後方互換サポートのみ
        /// </summary>
        public static string GhostScriptPath => 
            GetStringValue("DOCORGANIZER_GS_PATH") ??
            GetStringValue("GS_BIN_PATH") ??
            GetStringValue("GHOSTSCRIPT_BIN") ??
            GetDefaultGhostScriptPath();
            
        #endregion
        
        #region 設定値検証・ユーティリティ
        
        /// <summary>
        /// PDF品質設定の有効性検証
        /// </summary>
        public static bool IsValidPdfQuality(string quality)
        {
            if (string.IsNullOrEmpty(quality))
                return false;
                
            var validQualities = new[] { "High", "Medium", "Low" };
            return Array.Exists(validQualities, q => 
                q.Equals(quality, StringComparison.OrdinalIgnoreCase));
        }
        
        /// <summary>
        /// 現在の設定値をすべて取得（診断・デバッグ用）
        /// </summary>
        public static string GetAllSettings()
        {
            return $@"DocOrganizer環境変数設定:
デバッグ: {IsDebugEnabled}
ログパス: {LogPath}
OCR有効: {IsOcrEnabled}
OCRパス: {OcrDataPath}
PDFキャッシュ: {PdfCacheSize}MB
PDF品質: {PdfQuality}
スレッド数: {ThreadCountSetting} (実際: {ActualThreadCount})
メモリ制限: {MemoryLimitMB}MB
GSパス: {GhostScriptPath}";
        }
        
        #endregion
        
        #region プライベート・ヘルパーメソッド
        
        /// <summary>
        /// Boolean型環境変数の取得
        /// "true" (大文字小文字無視) の場合のみtrue、その他はfalse
        /// </summary>
        private static bool GetBooleanValue(string name, bool defaultValue)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrEmpty(value))
                return defaultValue;
                
            return value.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
        
        /// <summary>
        /// 文字列型環境変数の取得
        /// </summary>
        private static string GetStringValue(string name, string defaultValue = null)
        {
            var value = Environment.GetEnvironmentVariable(name);
            return string.IsNullOrEmpty(value) ? defaultValue : value;
        }
        
        /// <summary>
        /// 整数型環境変数の取得
        /// 数値変換失敗時はデフォルト値を返却
        /// </summary>
        private static int GetIntegerValue(string name, int defaultValue)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrEmpty(value))
                return defaultValue;
                
            return int.TryParse(value, out var result) ? result : defaultValue;
        }
        
        /// <summary>
        /// GhostScriptのデフォルトインストールパス検索
        /// Windows標準インストール場所を順次確認
        /// </summary>
        private static string GetDefaultGhostScriptPath()
        {
            // Windows標準インストールパス
            var defaultPaths = new[]
            {
                @"C:\Program Files\gs\gs*\bin\gswin64c.exe",
                @"C:\Program Files (x86)\gs\gs*\bin\gswin32c.exe",
                @"C:\gs\gs*\bin\gswin64c.exe",
                "gs" // PATH環境変数での検索
            };
            
            foreach (var path in defaultPaths)
            {
                try
                {
                    if (path.Contains("*"))
                    {
                        // ワイルドカード検索（最新バージョンを検索）
                        var directory = Path.GetDirectoryName(path);
                        var pattern = Path.GetFileName(path);
                        
                        if (Directory.Exists(directory))
                        {
                            var files = Directory.GetFiles(directory, pattern);
                            if (files.Length > 0)
                                return files[0];
                        }
                    }
                    else if (File.Exists(path))
                    {
                        return path;
                    }
                }
                catch
                {
                    // パス検索エラーは無視して次のパスを試行
                    continue;
                }
            }
            
            // デフォルトパスが見つからない場合
            return "gs";
        }
        
        #endregion
        
        #region 設定変更通知（将来拡張用）
        
        /// <summary>
        /// 設定変更を監視するためのイベント（将来実装用）
        /// ホットリロード機能の基盤として準備
        /// </summary>
        public static event Action<string, object> SettingChanged;
        
        /// <summary>
        /// 設定変更の通知（内部用）
        /// </summary>
        private static void OnSettingChanged(string settingName, object newValue)
        {
            SettingChanged?.Invoke(settingName, newValue);
        }
        
        #endregion
    }
}