using System;
using System.Collections.Generic;
using System.Linq;

namespace DocOrganizer.Core.Models
{
    /// <summary>
    /// PDF文書を表すモデルクラス
    /// </summary>
    public class PdfDocument : IDisposable
    {
        private readonly List<PdfPage> _pages;
        private bool _disposed;

        /// <summary>
        /// PDF文書のページコレクション
        /// </summary>
        public IReadOnlyList<PdfPage> Pages => _pages;

        /// <summary>
        /// PDF文書のファイルパス
        /// </summary>
        public string? FilePath { get; set; }

        /// <summary>
        /// 文書が変更されたかどうか
        /// </summary>
        public bool IsModified { get; private set; }

        /// <summary>
        /// 元の画像ファイルパスのリスト（遅延変換用）
        /// </summary>
        public List<string> SourceImagePaths { get; set; } = new List<string>();

        /// <summary>
        /// この文書が画像から作成された一時的なものかどうか
        /// </summary>
        public bool IsTemporaryFromImages { get; set; }

        /// <summary>
        /// PdfDocumentの新しいインスタンスを初期化します
        /// </summary>
        public PdfDocument()
        {
            _pages = new List<PdfPage>();
            IsModified = false;
        }

        /// <summary>
        /// 指定されたファイルパスでPdfDocumentの新しいインスタンスを初期化します
        /// </summary>
        /// <param name="filePath">PDFファイルのパス</param>
        public PdfDocument(string filePath) : this()
        {
            FilePath = filePath;
        }

        /// <summary>
        /// ページを追加します
        /// </summary>
        /// <param name="page">追加するページ</param>
        public void AddPage(PdfPage page)
        {
            if (page == null)
                throw new ArgumentNullException(nameof(page));

            _pages.Add(page);
            IsModified = true;
        }

        /// <summary>
        /// ページを削除します
        /// </summary>
        /// <param name="page">削除するページ</param>
        /// <returns>ページが削除された場合はtrue、見つからなかった場合はfalse</returns>
        public bool RemovePage(PdfPage page)
        {
            if (page == null)
                return false;

            var removed = _pages.Remove(page);
            if (removed)
            {
                IsModified = true;
            }
            return removed;
        }

        /// <summary>
        /// 指定されたインデックスのページを削除します
        /// </summary>
        /// <param name="index">削除するページのインデックス</param>
        public void RemovePageAt(int index)
        {
            if (index < 0 || index >= _pages.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            _pages.RemoveAt(index);
            IsModified = true;
        }

        /// <summary>
        /// ページを移動します
        /// </summary>
        /// <param name="fromIndex">移動元のインデックス</param>
        /// <param name="toIndex">移動先のインデックス</param>
        public void MovePage(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= _pages.Count)
                throw new ArgumentOutOfRangeException(nameof(fromIndex));
            if (toIndex < 0 || toIndex >= _pages.Count)
                throw new ArgumentOutOfRangeException(nameof(toIndex));

            if (fromIndex == toIndex)
                return;

            var page = _pages[fromIndex];
            _pages.RemoveAt(fromIndex);
            _pages.Insert(toIndex, page);
            IsModified = true;
        }

        /// <summary>
        /// 指定されたインデックスのページを回転します
        /// </summary>
        /// <param name="pageIndices">回転するページのインデックス配列</param>
        /// <param name="degrees">回転角度（90, 180, 270度）</param>
        public void RotatePages(int[] pageIndices, int degrees)
        {
            if (pageIndices == null)
                throw new ArgumentNullException(nameof(pageIndices));

            foreach (var index in pageIndices)
            {
                if (index >= 0 && index < _pages.Count)
                {
                    var page = _pages[index];
                    page.Rotation = (page.Rotation + degrees) % 360;
                    IsModified = true;
                }
            }
        }

        /// <summary>
        /// ページ数を取得します
        /// </summary>
        /// <returns>ページ数</returns>
        public int GetPageCount()
        {
            return _pages.Count;
        }

        /// <summary>
        /// 変更フラグをクリアします
        /// </summary>
        public void ClearModifiedFlag()
        {
            IsModified = false;
        }

        /// <summary>
        /// リソースを解放します
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// リソースを解放します
        /// </summary>
        /// <param name="disposing">マネージドリソースを解放する場合はtrue</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                foreach (var page in _pages)
                {
                    page?.Dispose();
                }
                _pages.Clear();
            }

            _disposed = true;
        }
    }
}