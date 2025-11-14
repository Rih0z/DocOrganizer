using System;
using System.Diagnostics;
using System.Threading.Tasks;
using FluentAssertions;

namespace DocOrganizer.Tests.TestHelpers
{
    /// <summary>
    /// パフォーマンステスト用のカスタムアサーション
    /// </summary>
    public static class PerformanceAssertions
    {
        /// <summary>
        /// 同期処理が指定時間内に完了することを検証
        /// </summary>
        /// <param name="action">実行する処理</param>
        /// <param name="threshold">閾値（TimeSpan）</param>
        /// <param name="because">理由（オプション）</param>
        public static void ShouldCompleteWithin(this Action action, TimeSpan threshold, string because = "")
        {
            var stopwatch = Stopwatch.StartNew();
            action();
            stopwatch.Stop();

            stopwatch.Elapsed.Should().BeLessThan(threshold, because);
        }

        /// <summary>
        /// 非同期処理が指定時間内に完了することを検証
        /// </summary>
        /// <param name="action">実行する非同期処理</param>
        /// <param name="threshold">閾値（TimeSpan）</param>
        /// <param name="because">理由（オプション）</param>
        public static async Task ShouldCompleteWithinAsync(this Func<Task> action, TimeSpan threshold, string because = "")
        {
            var stopwatch = Stopwatch.StartNew();
            await action();
            stopwatch.Stop();

            stopwatch.Elapsed.Should().BeLessThan(threshold, because);
        }

        /// <summary>
        /// 処理実行時のメモリ増加量が指定値以下であることを検証
        /// </summary>
        /// <param name="action">実行する処理</param>
        /// <param name="maxMemoryIncreaseMB">最大メモリ増加量（MB）</param>
        /// <param name="because">理由（オプション）</param>
        public static void ShouldUseMemoryLessThan(this Action action, long maxMemoryIncreaseMB, string because = "")
        {
            // GC強制実行で初期状態を安定化
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var initialMemory = GC.GetTotalMemory(forceFullCollection: true);

            // 処理実行
            action();

            // GC強制実行で最終状態を確定
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var finalMemory = GC.GetTotalMemory(forceFullCollection: true);
            var memoryIncreaseMB = (finalMemory - initialMemory) / 1024.0 / 1024.0;

            memoryIncreaseMB.Should().BeLessThan(maxMemoryIncreaseMB, because);
        }

        /// <summary>
        /// 非同期処理実行時のメモリ増加量が指定値以下であることを検証
        /// </summary>
        /// <param name="action">実行する非同期処理</param>
        /// <param name="maxMemoryIncreaseMB">最大メモリ増加量（MB）</param>
        /// <param name="because">理由（オプション）</param>
        public static async Task ShouldUseMemoryLessThanAsync(this Func<Task> action, long maxMemoryIncreaseMB, string because = "")
        {
            // GC強制実行で初期状態を安定化
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var initialMemory = GC.GetTotalMemory(forceFullCollection: true);

            // 処理実行
            await action();

            // GC強制実行で最終状態を確定
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var finalMemory = GC.GetTotalMemory(forceFullCollection: true);
            var memoryIncreaseMB = (finalMemory - initialMemory) / 1024.0 / 1024.0;

            memoryIncreaseMB.Should().BeLessThan(maxMemoryIncreaseMB, because);
        }
    }
}
