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
        
        // 🚨 静的コンストラクタによる絶対診断
        static DebugLogger()
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var staticDiagnosticPath = Path.Combine(baseDir, "static_constructor_diagnostic.txt");
                var message = $"DebugLogger static constructor called at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}\n";
                message += $"BaseDirectory: {baseDir}\n";
                File.WriteAllText(staticDiagnosticPath, message);
            }
            catch (Exception ex)
            {
                // 緊急対応: ファイル書き込み失敗時は別の方法で記録
                try
                {
                    var tempPath = Path.GetTempPath();
                    var fallbackPath = Path.Combine(tempPath, "debuglogger_fallback.txt");
                    File.WriteAllText(fallbackPath, $"DebugLogger constructor failed: {ex.Message}\n");
                }
                catch { /* 完全無視 */ }
            }
        }
        
        /// <summary>
        /// ログ出力パス
        /// </summary>
        public static string LogPath
        {
            get
            {
                // 🚨 絶対診断: LogPath呼び出し確認
                try
                {
                    var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    var diagnosticPath = Path.Combine(baseDir, "logpath_diagnostic.txt");
                    File.WriteAllText(diagnosticPath, $"LogPath called at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}\n");
                }
                catch { /* 診断エラーは無視 */ }
                
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
                // 🚨 絶対診断: IsDebugEnabled呼び出し確認
                try
                {
                    var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    var diagnosticPath = Path.Combine(baseDir, "isdebugenabled_diagnostic.txt");
                    File.WriteAllText(diagnosticPath, $"IsDebugEnabled called at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}\n");
                }
                catch { /* 診断エラーは無視 */ }
                
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
            var envValue = Environment.GetEnvironmentVariable("DOCORGANIZER_DEBUG");
            
            // 🚨 強制診断ファイル出力（デバッガ不要）
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var diagnosticPath = Path.Combine(baseDir, "debug_diagnostic.txt");
                var diagnostics = $"=== DebugLogger 強制診断 [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ===\n";
                diagnostics += $"環境変数 DOCORGANIZER_DEBUG = '{envValue ?? "null"}'\n";
                
                #if ENABLE_LOGGING
                diagnostics += "ENABLE_LOGGING フラグ: 有効\n";
                #else
                diagnostics += "ENABLE_LOGGING フラグ: 無効\n";
                #endif
                
                File.WriteAllText(diagnosticPath, diagnostics);
            }
            catch { /* 診断ファイル出力エラーは無視 */ }
            
            // System.Diagnostics.Debug.WriteLine も併用
            System.Diagnostics.Debug.WriteLine($"=== DebugLogger診断 ===");
            System.Diagnostics.Debug.WriteLine($"環境変数 DOCORGANIZER_DEBUG = '{envValue}'");
            
            #if ENABLE_LOGGING
            System.Diagnostics.Debug.WriteLine("ENABLE_LOGGING フラグ: 有効");
            #else
            System.Diagnostics.Debug.WriteLine("ENABLE_LOGGING フラグ: 無効");
            #endif

            // 環境変数から読み込み（最優先）
            if (!string.IsNullOrEmpty(envValue))
            {
                bool result = envValue.ToLower() == "true";
                System.Diagnostics.Debug.WriteLine($"環境変数により決定: {result}");
                return result;
            }

            // コンパイル時デフォルト値
            // - ログなし版（release）: false
            // - ログあり版（release-debug）: true
            #if ENABLE_LOGGING
            System.Diagnostics.Debug.WriteLine("コンパイル時フラグによりtrue");
            return true;  // ログあり版のデフォルト
            #else
            System.Diagnostics.Debug.WriteLine("コンパイル時フラグによりfalse");
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
            // 🚨 デバッグ: ログ出力の詳細診断
            System.Diagnostics.Debug.WriteLine($"LogAsync呼び出し: IsDebugEnabled={IsDebugEnabled}, LogPath='{LogPath}', Message='{message}'");
            
            if (!IsDebugEnabled || string.IsNullOrEmpty(LogPath)) 
            {
                System.Diagnostics.Debug.WriteLine($"ログスキップ: IsDebugEnabled={IsDebugEnabled}, LogPath='{LogPath}'");
                return;
            }
            
            try
            {
                // ログディレクトリが存在しない場合は作成
                var logDir = Path.GetDirectoryName(LogPath);
                System.Diagnostics.Debug.WriteLine($"ログディレクトリ: '{logDir}'");
                
                if (!string.IsNullOrEmpty(logDir) && !Directory.Exists(logDir))
                {
                    System.Diagnostics.Debug.WriteLine($"ログディレクトリ作成: '{logDir}'");
                    Directory.CreateDirectory(logDir);
                    System.Diagnostics.Debug.WriteLine($"ログディレクトリ作成完了: '{logDir}'");
                }
                
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var fileName = Path.GetFileName(sourceFile ?? "Unknown");
                var categoryStr = string.IsNullOrEmpty(category) ? "" : $"[{category}] ";
                
                var logMessage = $"[{timestamp}] {categoryStr}{message} ({fileName}:{lineNumber})";
                System.Diagnostics.Debug.WriteLine($"ログファイル書き込み準備: '{LogPath}'");
                
                await File.AppendAllTextAsync(LogPath, logMessage + Environment.NewLine);
                System.Diagnostics.Debug.WriteLine($"ログファイル書き込み成功: '{LogPath}'");
                
                // コンソール出力（開発時）
                #if DEBUG
                System.Diagnostics.Debug.WriteLine($"📝 {logMessage}");
                #endif
            }
            catch (Exception ex)
            {
                // 🚨 デバッグ: ログ出力エラーの詳細を診断
                System.Diagnostics.Debug.WriteLine($"ログ出力エラー: {ex.GetType().Name}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"ログパス: '{LogPath}'");
                System.Diagnostics.Debug.WriteLine($"スタックトレース: {ex.StackTrace}");
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
            catch (Exception ex)
            {
                // 🚨 デバッグ: ログ出力エラーの詳細を診断
                System.Diagnostics.Debug.WriteLine($"ログ出力エラー: {ex.GetType().Name}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"ログパス: '{LogPath}'");
                System.Diagnostics.Debug.WriteLine($"スタックトレース: {ex.StackTrace}");
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
            // エラーログも IsDebugEnabled のチェックを追加
            if (!IsDebugEnabled)
            {
                return;
            }
            
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