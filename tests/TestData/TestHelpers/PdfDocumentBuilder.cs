using System.Collections.Generic;
using DocOrganizer.Core.Models;

namespace DocOrganizer.Tests.TestHelpers
{
    /// <summary>
    /// ビルダーパターンでテスト用PdfDocumentを構築
    /// </summary>
    public class PdfDocumentBuilder
    {
        private string _filePath = "test.pdf";
        private List<PdfPage> _pages = new();

        /// <summary>
        /// ファイルパスを設定
        /// </summary>
        public PdfDocumentBuilder WithFilePath(string filePath)
        {
            _filePath = filePath;
            return this;
        }

        /// <summary>
        /// ページを1つ追加
        /// </summary>
        /// <param name="pageNumber">ページ番号</param>
        /// <param name="rotation">回転角度（デフォルト: 0）</param>
        /// <param name="width">幅（デフォルト: 595 = A4幅）</param>
        /// <param name="height">高さ（デフォルト: 842 = A4高さ）</param>
        public PdfDocumentBuilder WithPage(int pageNumber, int rotation = 0, float width = 595, float height = 842)
        {
            var page = new PdfPage(pageNumber);
            page.SetDimensions(width, height);
            page.Rotation = rotation;
            _pages.Add(page);
            return this;
        }

        /// <summary>
        /// 複数ページを一括追加
        /// </summary>
        /// <param name="count">ページ数</param>
        public PdfDocumentBuilder WithPages(int count)
        {
            for (int i = 0; i < count; i++)
            {
                WithPage(i + 1);
            }
            return this;
        }

        /// <summary>
        /// PdfDocumentをビルド
        /// </summary>
        public PdfDocument Build()
        {
            var document = new PdfDocument
            {
                FilePath = _filePath
            };

            foreach (var page in _pages)
            {
                document.AddPage(page);
            }

            return document;
        }
    }
}
