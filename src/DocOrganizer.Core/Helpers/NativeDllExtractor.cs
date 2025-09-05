using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace DocOrganizer.Core.Helpers
{
    /// <summary>
    /// 埋め込みリソースからネイティブDLLを抽出して使用可能にするヘルパー
    /// </summary>
    public static class NativeDllExtractor
    {
        private static readonly string TempDllPath = Path.Combine(
            Path.GetTempPath(), 
            "DocOrganizer",
            $"v{Assembly.GetExecutingAssembly().GetName().Version}"
        );

        /// <summary>
        /// pdfium.dllを初期化（必要に応じて抽出）
        /// </summary>
        public static void InitializePdfium()
        {
            try
            {
                var dllName = "pdfium.dll";
                var targetPath = Path.Combine(TempDllPath, dllName);

                // 既に抽出済みの場合はスキップ
                if (File.Exists(targetPath))
                {
                    SetDllDirectory(TempDllPath);
                    return;
                }

                // ディレクトリ作成
                Directory.CreateDirectory(TempDllPath);

                // 埋め込みリソースから抽出
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = $"DocOrganizer.{dllName}";
                
                using (var resourceStream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (resourceStream == null)
                    {
                        // 埋め込みリソースがない場合は、従来通り外部DLLを使用
                        return;
                    }

                    using (var fileStream = File.Create(targetPath))
                    {
                        resourceStream.CopyTo(fileStream);
                    }
                }

                // DLL検索パスを設定
                SetDllDirectory(TempDllPath);
            }
            catch
            {
                // エラーが発生した場合は、従来通り外部DLLの使用を試みる
            }
        }

        /// <summary>
        /// アプリケーション終了時のクリーンアップ
        /// </summary>
        public static void Cleanup()
        {
            try
            {
                if (Directory.Exists(TempDllPath))
                {
                    Directory.Delete(TempDllPath, true);
                }
            }
            catch
            {
                // クリーンアップ失敗は無視
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);
    }
}