using System;
using System.IO;

namespace DocOrganizer.Core.Logging
{
    /// <summary>
    /// DebugLoggerの問題を特定するためのシンプルなテストクラス
    /// </summary>
    public static class SimpleDebugTest
    {
        /// <summary>
        /// 最もシンプルなファイル書き込みテスト
        /// </summary>
        public static void WriteTestFile()
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var testPath = Path.Combine(baseDir, "simple_debug_test.txt");
                var content = $"SimpleDebugTest executed at {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}\n";
                content += $"BaseDirectory: {baseDir}\n";
                content += $"Environment Variable DOCORGANIZER_DEBUG: '{Environment.GetEnvironmentVariable("DOCORGANIZER_DEBUG")}'\n";
                
                #if ENABLE_LOGGING
                content += "ENABLE_LOGGING flag: ENABLED\n";
                #else
                content += "ENABLE_LOGGING flag: DISABLED\n";
                #endif
                
                File.WriteAllText(testPath, content);
            }
            catch (Exception ex)
            {
                // フォールバック: テンポラリフォルダに書き込み
                try
                {
                    var tempPath = Path.GetTempPath();
                    var fallbackPath = Path.Combine(tempPath, "simple_debug_test_fallback.txt");
                    File.WriteAllText(fallbackPath, $"SimpleDebugTest failed: {ex.Message}\n");
                }
                catch { /* 完全無視 */ }
            }
        }
    }
}