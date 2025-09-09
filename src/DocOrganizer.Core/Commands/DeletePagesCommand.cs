using System;
using System.Collections.Generic;
using System.Linq;
using DocOrganizer.Core.Models;

namespace DocOrganizer.Core.Commands
{
    /// <summary>
    /// Phase 3: UndoableCommand実装 - ページ削除コマンド
    /// 削除されたページの完全な状態を保存し、Undoで完全復元を可能にする
    /// </summary>
    public class DeletePagesCommand : IUndoableCommand
    {
        private readonly PdfDocument _document;
        private readonly List<PageDeleteInfo> _deletedPagesInfo = new();
        private readonly Action _onPagesChanged;

        /// <summary>
        /// 削除されたページの完全情報
        /// </summary>
        private class PageDeleteInfo
        {
            public PdfPage Page { get; set; }
            public int OriginalPosition { get; set; }
            public int PageNumber { get; set; }

            public PageDeleteInfo(PdfPage page, int originalPosition, int pageNumber)
            {
                Page = page;
                OriginalPosition = originalPosition;
                PageNumber = pageNumber;
            }
        }

        public string Description { get; }

        /// <summary>
        /// 削除コマンドのコンストラクタ
        /// </summary>
        /// <param name="document">対象PDF文書</param>
        /// <param name="pagesToDelete">削除対象ページリスト</param>
        /// <param name="onPagesChanged">ページ変更時のコールバック</param>
        public DeletePagesCommand(PdfDocument document, List<PdfPage> pagesToDelete, Action onPagesChanged)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            _onPagesChanged = onPagesChanged ?? throw new ArgumentNullException(nameof(onPagesChanged));

            if (pagesToDelete == null || !pagesToDelete.Any())
                throw new ArgumentException("削除対象ページが指定されていません", nameof(pagesToDelete));

            // 削除情報を事前に収集（降順でソートして削除順序を決定）
            var pagesArray = _document.Pages.ToArray();
            foreach (var page in pagesToDelete.OrderByDescending(p => Array.IndexOf(pagesArray, p)))
            {
                var originalPosition = Array.IndexOf(pagesArray, page);
                if (originalPosition >= 0)
                {
                    _deletedPagesInfo.Add(new PageDeleteInfo(page, originalPosition, page.PageNumber));
                }
            }

            Description = $"{_deletedPagesInfo.Count}ページ削除";
        }

        /// <summary>
        /// 削除を実行
        /// </summary>
        public void Execute()
        {
            // 降順（後ろから）削除することで、インデックスの変動を避ける
            foreach (var deleteInfo in _deletedPagesInfo)
            {
                _document.RemovePage(deleteInfo.Page);
            }
            
            // 変更通知
            _onPagesChanged?.Invoke();
        }

        /// <summary>
        /// 削除をUndo（復元）
        /// </summary>
        public void Undo()
        {
            // 昇順（元の位置順）で復元
            foreach (var deleteInfo in _deletedPagesInfo.OrderBy(info => info.OriginalPosition))
            {
                // AddPageで復元（PdfDocumentが内部で適切に管理）
                _document.AddPage(deleteInfo.Page);
            }
            
            // 変更通知
            _onPagesChanged?.Invoke();
        }
    }
}