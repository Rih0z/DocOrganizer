using System;
using System.IO;
using Xunit.Abstractions;

namespace DocOrganizer.Tests.TestHelpers
{
    /// <summary>
    /// 全テストクラスの共通ベースクラス
    /// セットアップとクリーンアップを提供
    /// </summary>
    public abstract class TestFixtureBase : IDisposable
    {
        protected readonly ITestOutputHelper Output;
        protected readonly string TestDataPath;

        protected TestFixtureBase(ITestOutputHelper output)
        {
            Output = output;
            TestDataPath = GetTestDataPath();
        }

        /// <summary>
        /// テスト用PDFファイルのパスを取得
        /// </summary>
        protected string GetTestPdfPath(string fileName)
        {
            return Path.Combine(TestDataPath, "Pdfs", fileName);
        }

        /// <summary>
        /// テスト用画像ファイルのパスを取得
        /// </summary>
        protected string GetTestImagePath(string fileName)
        {
            return Path.Combine(TestDataPath, "Images", fileName);
        }

        /// <summary>
        /// 期待値PDFファイルのパスを取得
        /// </summary>
        protected string GetExpectedPdfPath(string fileName)
        {
            return Path.Combine(TestDataPath, "Expected", fileName);
        }

        /// <summary>
        /// TestDataディレクトリのパスを取得
        /// </summary>
        private static string GetTestDataPath()
        {
            var baseDir = AppContext.BaseDirectory;
            var solutionRoot = Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\..\..\.."));
            return Path.Combine(solutionRoot, "tests", "TestData");
        }

        /// <summary>
        /// テスト終了時のクリーンアップ
        /// </summary>
        public virtual void Dispose()
        {
            TestDataGenerator.CleanupTempFiles();
        }
    }
}
