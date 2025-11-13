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
        
        // V3.0.152: 診断コード削除（リリース版でファイル生成を防止）
        static DebugLogger()
        {
            // 何もしない（ENABLE_LOGGINGが無効の場合、すべての処理をスキップ）
        }
        
        /// <summary>
        /// ログ出力パス
        /// </summary>
        public static string LogPath
        {
            get
            {
                #if ENABLE_LOGGING
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
                #else
                return string.Empty;  // リリース版では空文字列
                #endif
            }
        }
        
        /// <summary>
        /// デバッグログ有効フラグ
        /// </summary>
        public static bool IsDebugEnabled
        {
            get
            {
                #if ENABLE_LOGGING
                if (!_isEnabled.HasValue)
                {
                    _isEnabled = GetIsDebugEnabled();
                }
                return _isEnabled.Value;
                #else
                return false;  // リリース版では常にfalse
                #endif
            }
        }

        /// <summary>
        /// ログ有効状態を取得（環境変数優先、なければコンパイル時デフォルト）
        /// </summary>
        private static bool GetIsDebugEnabled()
        {
            #if ENABLE_LOGGING
            var envValue = Environment.GetEnvironmentVariable("DOCORGANIZER_DEBUG");

            // 環境変数から読み込み（最優先）
            if (!string.IsNullOrEmpty(envValue))
            {
                return envValue.ToLower() == "true";
            }

            // コンパイル時デフォルト値: ログあり版のデフォルト
            return true;
            #else
            // リリース版では常にfalse
            return false;
            #endif
        }
        
        /// <summary>
        /// ログ出力パスを取得（環境変数優先、なければデフォルト）
        /// </summary>
        private static string GetLogPath()
        {
            #if ENABLE_LOGGING
            var baseDir = Environment.CurrentDirectory;

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
            #else
            return string.Empty;  // リリース版では空文字列
            #endif
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
            #if ENABLE_LOGGING
            if (!IsDebugEnabled || string.IsNullOrEmpty(LogPath))
            {
                return;
            }

            try
            {
                // ログディレクトリが存在しない場合は作成
                var logDir = Path.GetDirectoryName(LogPath);

                if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var fileName = Path.GetFileName(sourceFile ?? "Unknown");
                var categoryStr = string.IsNullOrEmpty(category) ? "" : $"[{category}] ";

                var logMessage = $"[{timestamp}] {categoryStr}{message} ({fileName}:{lineNumber})";

                await File.AppendAllTextAsync(LogPath, logMessage + Environment.NewLine);
            }
            catch
            {
                // ログ出力エラーは無視
            }
            #else
            await Task.CompletedTask;  // リリース版では何もしない
            #endif
        }
        
        /// <summary>
        /// 同期ログ出力（互換性のため）
        /// </summary>
        public static void Log(string message, string category = null,
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFile = null,
            [System.Runtime.CompilerServices.CallerLineNumber] int lineNumber = 0)
        {
            #if ENABLE_LOGGING
            if (!IsDebugEnabled || string.IsNullOrEmpty(LogPath)) return;

            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var fileName = Path.GetFileName(sourceFile ?? "Unknown");
                var categoryStr = string.IsNullOrEmpty(category) ? "" : $"[{category}] ";

                var logMessage = $"[{timestamp}] {categoryStr}{message} ({fileName}:{lineNumber})";

                File.AppendAllText(LogPath, logMessage + Environment.NewLine);
            }
            catch
            {
                // ログ出力エラーは無視
            }
            #endif
        }
        
        /// <summary>
        /// 起動時ログを記録
        /// </summary>
        public static void LogStartup(string message)
        {
            #if ENABLE_LOGGING
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
            #endif
        }

        /// <summary>
        /// エラーログを記録
        /// </summary>
        public static void LogError(string message, Exception ex = null)
        {
            #if ENABLE_LOGGING
            if (!IsDebugEnabled) return;

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
            #endif
        }

        /// <summary>
        /// ログ設定をリセット（主にテスト用）
        /// </summary>
        public static void Reset()
        {
            #if ENABLE_LOGGING
            _isEnabled = null;
            _logPath = null;
            #endif
        }
    }
}