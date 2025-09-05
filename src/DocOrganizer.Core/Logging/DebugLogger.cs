using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace DocOrganizer.Core.Logging
{
    /// <summary>
    /// 統一デバッグログ管理クラス
    /// 環境変数による制御を優先、なければコンパイル時デフォルト
    /// </summary>
    public static class DebugLogger
    {
        private static bool? _isEnabled = null;
        private static string _logPath = null;
        
        /// <summary>
        /// ログ出力パス
        /// </summary>
        public static string LogPath
        {
            get
            {
                if (_logPath == null)
                {
                    _logPath = GetLogPath();
                    
                    // ディレクトリが存在しない場合は作成
                    var dir = Path.GetDirectoryName(_logPath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        try
                        {
                            Directory.CreateDirectory(dir);
                        }
                        catch { }
                    }
                }
                return _logPath;
            }
        }
        
        /// <summary>
        /// デバッグログ有効フラグ
        /// </summary>
        public static bool IsDebugEnabled
        {
            get
            {
                if (!_isEnabled.HasValue)
                {
                    _isEnabled = GetIsDebugEnabled();
                }
                return _isEnabled.Value;
            }
        }

        /// <summary>
        /// ログ有効状態を取得（環境変数優先、なければコンパイル時デフォルト）
        /// </summary>
        private static bool GetIsDebugEnabled()
        {
            // 環境変数から読み込み（最優先）
            var envValue = Environment.GetEnvironmentVariable("DOCORGANIZER_DEBUG");
            if (!string.IsNullOrEmpty(envValue))
            {
                return envValue.ToLower() == "true";
            }

            // コンパイル時デフォルト値
            // - ログなし版（release）: false
            // - ログあり版（release-debug）: true
            #if ENABLE_LOGGING
            return true;  // ログあり版のデフォルト
            #else
            return false; // ログなし版のデフォルト
            #endif
        }
        
        /// <summary>
        /// ログ出力パスを取得（環境変数優先、なければデフォルト）
        /// </summary>
        private static string GetLogPath()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            
            // 環境変数から読み込み（最優先）
            var envPath = Environment.GetEnvironmentVariable("DOCORGANIZER_LOG_PATH");
            if (!string.IsNullOrEmpty(envPath))
            {
                // 絶対パスまたは相対パスとして処理
                if (Path.IsPathRooted(envPath))
                {
                    return envPath;
                }
                return Path.Combine(baseDir, envPath);
            }
            
            // デフォルトパス
            var defaultLogDir = ".logs";
            var defaultLogFile = "debug.log";
            return Path.Combine(baseDir, defaultLogDir, defaultLogFile);
        }

        /// <summary>
        /// 非同期でログを出力
        /// </summary>
        /// <param name="message">ログメッセージ</param>
        /// <param name="category">ログカテゴリ（省略可）</param>
        /// <param name="sourceFile">呼び出し元ファイル名（自動取得）</param>
        /// <param name="lineNumber">呼び出し元行番号（自動取得）</param>
        public static async Task LogAsync(string message, string category = null,
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFile = null,
            [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            if (!IsDebugEnabled || string.IsNullOrEmpty(LogPath)) return;
            
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var fileName = Path.GetFileName(sourceFile ?? "Unknown");
                var categoryStr = string.IsNullOrEmpty(category) ? "" : $"[{category}] ";
                
                var logMessage = $"[{timestamp}] {categoryStr}{message} ({fileName}:{lineNumber})";
                
                await File.AppendAllTextAsync(LogPath, logMessage + Environment.NewLine);
                
                // コンソール出力（開発時）
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"📝 {logMessage}");
                #endif
            }
            catch
            {
                // ログ出力エラーは無視
            }
        }
        
        /// <summary>
        /// 同期ログ出力（互換性のため）
        /// </summary>
        public static void Log(string message, string category = null,
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFile = null,
            [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            if (!IsDebugEnabled || string.IsNullOrEmpty(LogPath)) return;
            
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var fileName = Path.GetFileName(sourceFile ?? "Unknown");
                var categoryStr = string.IsNullOrEmpty(category) ? "" : $"[{category}] ";
                
                var logMessage = $"[{timestamp}] {categoryStr}{message} ({fileName}:{lineNumber})";
                
                File.AppendAllText(LogPath, logMessage + Environment.NewLine);
                
                // コンソール出力（開発時）
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"📝 {logMessage}");
                #endif
            }
            catch
            {
                // ログ出力エラーは無視
            }
        }
        
        /// <summary>
        /// 起動時ログを記録
        /// </summary>
        public static void LogStartup(string message)
        {
            // ログが無効な場合は何もしない
            if (!IsDebugEnabled) return;
            
            try
            {
                var startupLogPath = Path.Combine(
                    Path.GetDirectoryName(LogPath) ?? ".logs",
                    "startup.log"
                );
                
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var logMessage = $"[{timestamp}] {message}";
                
                File.AppendAllText(startupLogPath, logMessage + Environment.NewLine);
            }
            catch { }
        }
        
        /// <summary>
        /// エラーログを記録
        /// </summary>
        public static void LogError(string message, Exception ex = null)
        {
            try
            {
                var errorLogPath = Path.Combine(
                    Path.GetDirectoryName(LogPath) ?? ".logs",
                    "error.log"
                );
                
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var errorMessage = $"[{timestamp}] ERROR: {message}";
                
                if (ex != null)
                {
                    errorMessage += $"\n    Exception: {ex.GetType().Name}: {ex.Message}\n    StackTrace: {ex.StackTrace}";
                }
                
                File.AppendAllText(errorLogPath, errorMessage + Environment.NewLine);
            }
            catch { }
        }
        
        /// <summary>
        /// ログ設定をリセット（主にテスト用）
        /// </summary>
        public static void Reset()
        {
            _isEnabled = null;
            _logPath = null;
        }
    }
}