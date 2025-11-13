using System;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DocOrganizer.Infrastructure.Services.V3
{
    /// <summary>
    /// 🎯 V3.0.025 PDF Performance Monitor - 品質保証・監視体制
    /// 条件付き推奨要件: パフォーマンス・メモリ使用量の継続監視
    /// </summary>
    public class PdfPerformanceMonitor : IDisposable
    {
        private readonly ILogger<PdfPerformanceMonitor> _logger;
        private readonly string _logFilePath;
        private bool _disposed = false;
        
        // パフォーマンス閾値（条件付き推奨基準）
        private const int ThumbnailGenerationTimeoutMs = 3000; // 3秒
        private const long MaxMemoryUsageMb = 500; // 500MB
        
        public PdfPerformanceMonitor(ILogger<PdfPerformanceMonitor> logger)
        {
            _logger = logger;
            _logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "release", "PDF_PERFORMANCE_LOG.txt");
            
            // 初期化ログ
            _ = LogPerformanceAsync("MONITOR_INIT", "PDF Performance Monitor initialized", 0, 0);
        }
        
        /// <summary>
        /// PDF操作のパフォーマンスを測定・監視
        /// </summary>
        public async Task<T> MonitorAsync<T>(string operation, string filePath, Func<Task<T>> action)
        {
            var stopwatch = Stopwatch.StartNew();
            var initialMemory = GC.GetTotalMemory(false);
            
            try
            {
                _logger.LogDebug("[PDF_MONITOR] 開始: {Operation}, ファイル: {FileName}", 
                    operation, Path.GetFileName(filePath));
                
                var result = await action();
                
                stopwatch.Stop();
                var finalMemory = GC.GetTotalMemory(false);
                var elapsedMs = stopwatch.ElapsedMilliseconds;
                var memoryDeltaMb = (finalMemory - initialMemory) / (1024 * 1024);
                
                // パフォーマンスログ記録
                await LogPerformanceAsync(operation, Path.GetFileName(filePath), elapsedMs, memoryDeltaMb);
                
                // 閾値チェック・アラート
                await CheckThresholdsAsync(operation, filePath, elapsedMs, memoryDeltaMb);
                
                _logger.LogDebug("[PDF_MONITOR] 完了: {Operation}, 時間: {ElapsedMs}ms, メモリ: {MemoryDelta}MB", 
                    operation, elapsedMs, memoryDeltaMb);
                
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await LogPerformanceAsync($"{operation}_ERROR", Path.GetFileName(filePath), 
                    stopwatch.ElapsedMilliseconds, 0, ex.Message);
                throw;
            }
        }
        
        /// <summary>
        /// パフォーマンスログの記録
        /// </summary>
        private async Task LogPerformanceAsync(string operation, string fileName, long elapsedMs, long memoryDeltaMb, string error = null)
        {
            try
            {
                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [PDF_PERF] " +
                              $"Operation={operation}, File={fileName}, " +
                              $"Time={elapsedMs}ms, Memory={memoryDeltaMb}MB" +
                              (error != null ? $", Error={error}" : "") + Environment.NewLine;
                
                await DocOrganizer.Core.Logging.DebugLogger.LogAsync(logEntry.TrimEnd(), "PdfPerformance");
                
                // 成功時の詳細ログ
                if (error == null && (operation.Contains("Thumbnail") || operation.Contains("Preview")))
                {
                    var detailEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [PDF_DETAIL] " +
                                     $"{operation} - File: {fileName}, " +
                                     $"Performance: {elapsedMs}ms, Memory Delta: {memoryDeltaMb}MB, " +
                                     $"Status: {(elapsedMs < ThumbnailGenerationTimeoutMs ? "OK" : "WARNING")}" + 
                                     Environment.NewLine;
                    
                    await DocOrganizer.Core.Logging.DebugLogger.LogAsync(detailEntry.TrimEnd(), "PdfDetail");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PDF_MONITOR] ログ記録エラー");
            }
        }
        
        /// <summary>
        /// パフォーマンス閾値チェック・アラート
        /// </summary>
        private async Task CheckThresholdsAsync(string operation, string filePath, long elapsedMs, long memoryDeltaMb)
        {
            var alerts = new System.Collections.Generic.List<string>();
            
            // 時間チェック
            if (elapsedMs > ThumbnailGenerationTimeoutMs)
            {
                alerts.Add($"TIME_THRESHOLD_EXCEEDED: {elapsedMs}ms > {ThumbnailGenerationTimeoutMs}ms");
            }
            
            // メモリチェック
            if (memoryDeltaMb > MaxMemoryUsageMb)
            {
                alerts.Add($"MEMORY_THRESHOLD_EXCEEDED: {memoryDeltaMb}MB > {MaxMemoryUsageMb}MB");
            }
            
            // アラート処理
            if (alerts.Count > 0)
            {
                var alertMessage = string.Join(", ", alerts);
                _logger.LogWarning("[PDF_MONITOR] 🚨 PERFORMANCE ALERT: {Operation}, File: {FileName}, {Alerts}", 
                    operation, Path.GetFileName(filePath), alertMessage);
                
                await LogPerformanceAsync($"{operation}_ALERT", Path.GetFileName(filePath), 
                    elapsedMs, memoryDeltaMb, alertMessage);
            }
        }
        
        /// <summary>
        /// 月次パフォーマンスレポート生成
        /// </summary>
        public async Task GenerateMonthlyReportAsync()
        {
            #if ENABLE_LOGGING
            try
            {
                if (!File.Exists(_logFilePath))
                {
                    _logger.LogWarning("[PDF_MONITOR] パフォーマンスログファイルが存在しません");
                    return;
                }

                var logContent = await File.ReadAllTextAsync(_logFilePath);
                var lines = logContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                var reportPath = Path.Combine(Path.GetDirectoryName(_logFilePath),
                    $"PDF_PERFORMANCE_REPORT_{DateTime.Now:yyyyMM}.txt");

                var report = $"# PDF Performance Monthly Report - {DateTime.Now:yyyy年MM月}\n\n";
                report += $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n";
                report += $"Total Log Entries: {lines.Length}\n\n";

                // 統計情報の生成（簡易版）
                var thumbnailCount = 0;
                var alertCount = 0;

                foreach (var line in lines)
                {
                    if (line.Contains("Thumbnail")) thumbnailCount++;
                    if (line.Contains("ALERT")) alertCount++;
                }

                report += $"Thumbnail Operations: {thumbnailCount}\n";
                report += $"Performance Alerts: {alertCount}\n";
                report += $"Alert Rate: {(thumbnailCount > 0 ? (double)alertCount / thumbnailCount * 100 : 0):F2}%\n\n";

                report += "## Performance Threshold Status\n";
                report += $"- Time Threshold: {ThumbnailGenerationTimeoutMs}ms\n";
                report += $"- Memory Threshold: {MaxMemoryUsageMb}MB\n\n";

                await File.WriteAllTextAsync(reportPath, report);

                _logger.LogInformation("[PDF_MONITOR] 月次レポート生成完了: {ReportPath}", reportPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PDF_MONITOR] 月次レポート生成エラー");
            }
            #else
            await Task.CompletedTask;
            #endif
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // 終了ログ
                    _ = LogPerformanceAsync("MONITOR_DISPOSE", "PDF Performance Monitor disposed", 0, 0);
                }
                _disposed = true;
            }
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}