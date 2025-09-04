using System;
using System.IO;
using System.Threading.Tasks;

namespace DocOrganizer.Core.Logging
{
    /// <summary>
    /// 統一デバッグログヘルパー - Quick Win実装
    /// 環境変数でデバッグモードとログパスを制御
    /// </summary>
    public static class DebugLogger
    {
        // 環境変数でデバッグモード制御
        private static readonly bool IsDebugEnabled = 
            Environment.GetEnvironmentVariable("DOCORGANIZER_DEBUG") == "true";
        
        // 環境変数でログパス制御（デフォルトは隠しフォルダ）
        private static readonly string LogPath = 
            Environment.GetEnvironmentVariable("DOCORGANIZER_LOG_PATH") ?? 
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".logs", "debug.log");
        
        private static readonly object _lock = new object();
        
        static DebugLogger()
        {
            try
            {
                // ログディレクトリ作成
                var dir = Path.GetDirectoryName(LogPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                    
                    // Windowsの場合、隠しフォルダ属性設定
                    if (Environment.OSVersion.Platform == PlatformID.Win32NT && dir.Contains(".logs"))
                    {
                        var dirInfo = new DirectoryInfo(dir);
                        dirInfo.Attributes |= FileAttributes.Hidden;
                    }
                }
            }
            catch
            {
                // 初期化エラーは無視（ログ出力に影響しないように）
            }
        }
        
        /// <summary>
        /// 非同期ログ出力
        /// </summary>
        /// <param name="message">ログメッセージ</param>
        /// <param name="category">カテゴリ（呼び出し元クラス名など）</param>
        public static async Task LogAsync(string message, string category = null)
        {
            // デバッグモードOFFの場合は何もしない
            if (!IsDebugEnabled) return;
            
            try
            {
                var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{category ?? "General"}] {message}";
                
                // ファイル出力（非同期）
                await File.AppendAllTextAsync(LogPath, logMessage + Environment.NewLine);
                
                // コンソール出力（開発時）
                System.Diagnostics.Debug.WriteLine(logMessage);
            }
            catch
            {
                // ログ出力エラーは握りつぶす（アプリケーションの動作を妨げない）
            }
        }
        
        /// <summary>
        /// 同期ログ出力（後方互換性のため）
        /// </summary>
        public static void Log(string message, string category = null)
        {
            // デバッグモードOFFの場合は何もしない
            if (!IsDebugEnabled) return;
            
            Task.Run(async () => await LogAsync(message, category));
        }
        
        /// <summary>
        /// 現在の設定状態を取得
        /// </summary>
        /// <returns>デバッグ有効フラグとログパス</returns>
        public static (bool IsEnabled, string Path) GetConfiguration()
        {
            return (IsDebugEnabled, LogPath);
        }
        
        /// <summary>
        /// 設定情報をログに出力（起動時確認用）
        /// </summary>
        public static async Task LogConfigurationAsync()
        {
            if (!IsDebugEnabled) return;
            
            await LogAsync("=== Debug Logger Configuration ===", "Config");
            await LogAsync($"Debug Mode: {IsDebugEnabled}", "Config");
            await LogAsync($"Log Path: {LogPath}", "Config");
            await LogAsync($"Log Directory Exists: {Directory.Exists(Path.GetDirectoryName(LogPath))}", "Config");
            await LogAsync("===================================", "Config");
        }
    }
}