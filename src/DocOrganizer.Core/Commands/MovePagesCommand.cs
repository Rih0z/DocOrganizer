using System;
using System.Collections.Generic;
using System.Linq;
using DocOrganizer.Core.Models;

namespace DocOrganizer.Core.Commands
{
    /// <summary>
    /// Phase 3: UndoableCommand実装 - ページ移動コマンド
    /// 移動前の位置情報を保存し、Undoで元の位置に復元を可能にする
    /// </summary>
    public class MovePagesCommand : IUndoableCommand
    {
        private readonly PdfDocument _document;
        private readonly List<PageMoveInfo> _moveInfo = new();
        private readonly Action _onPagesChanged;

        /// <summary>
        /// ページ移動情報
        /// </summary>
        private class PageMoveInfo
        {
            public PdfPage Page { get; set; }
            public int OriginalPosition { get; set; }
            public int NewPosition { get; set; }

            public PageMoveInfo(PdfPage page, int originalPosition, int newPosition)
            {
                Page = page;
                OriginalPosition = originalPosition;
                NewPosition = newPosition;
            }
        }

        public string Description { get; }

        /// <summary>
        /// 単一ページ移動のコンストラクタ
        /// </summary>
        /// <param name="document">対象PDF文書</param>
        /// <param name="page">移動対象ページ</param>
        /// <param name="newPosition">新しい位置</param>
        /// <param name="onPagesChanged">ページ変更時のコールバック</param>
        public MovePagesCommand(PdfDocument document, PdfPage page, int newPosition, Action onPagesChanged)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            _onPagesChanged = onPagesChanged ?? throw new ArgumentNullException(nameof(onPagesChanged));

            if (page == null)
                throw new ArgumentNullException(nameof(page));

            var originalPosition = Array.IndexOf(_document.Pages.ToArray(), page);
            if (originalPosition < 0)
                throw new ArgumentException("指定されたページが文書に存在しません", nameof(page));

            if (newPosition < 0 || newPosition >= _document.Pages.Count)
                throw new ArgumentException("移動先の位置が無効です", nameof(newPosition));

            _moveInfo.Add(new PageMoveInfo(page, originalPosition, newPosition));

            var direction = newPosition < originalPosition ? "上" : "下";
            Description = $"ページ{page.PageNumber}を{direction}に移動";
        }

        /// <summary>
        /// 複数ページ一括移動のコンストラクタ
        /// </summary>
        /// <param name="document">対象PDF文書</param>
        /// <param name="pageMoves">移動情報のリスト</param>
        /// <param name="onPagesChanged">ページ変更時のコールバック</param>
        public MovePagesCommand(PdfDocument document, List<(PdfPage page, int newPosition)> pageMoves, Action onPagesChanged)
        {
            _document = document ?? throw new ArgumentNullException(nameof(document));
            _onPagesChanged = onPagesChanged ?? throw new ArgumentNullException(nameof(onPagesChanged));

            if (pageMoves == null || !pageMoves.Any())
                throw new ArgumentException("移動対象ページが指定されていません", nameof(pageMoves));

            // 移動情報を収集
            var pagesArray = _document.Pages.ToArray();
            foreach (var (page, newPosition) in pageMoves)
            {
                var originalPosition = Array.IndexOf(pagesArray, page);
                if (originalPosition >= 0 && newPosition >= 0 && newPosition < _document.Pages.Count)
                {
                    _moveInfo.Add(new PageMoveInfo(page, originalPosition, newPosition));
                }
            }

            if (!_moveInfo.Any())
                throw new ArgumentException("有効な移動対象ページがありません");

            Description = $"{_moveInfo.Count}ページ一括移動";
        }

        /// <summary>
        /// 移動を実行
        /// </summary>
        public void Execute()
        {
            // 移動操作を実行（新しい位置順でソートして処理）
            foreach (var moveInfo in _moveInfo.OrderBy(m => m.NewPosition))
            {
                var currentIndex = Array.IndexOf(_document.Pages.ToArray(), moveInfo.Page);
                if (currentIndex >= 0 && currentIndex != moveInfo.NewPosition)
                {
                    // PDF文書の標準メソッドを使用してページを移動
                    _document.MovePage(currentIndex, moveInfo.NewPosition);
                }
            }
            
            // 変更通知
            _onPagesChanged?.Invoke();
        }

        /// <summary>
        /// 移動をUndo（元の位置に復元）
        /// </summary>
        public void Undo()
        {
            // 元の位置に復元（元の位置順でソートして処理）
            foreach (var moveInfo in _moveInfo.OrderBy(m => m.OriginalPosition))
            {
                var currentIndex = Array.IndexOf(_document.Pages.ToArray(), moveInfo.Page);
                if (currentIndex >= 0 && currentIndex != moveInfo.OriginalPosition)
                {
                    // PDF文書の標準メソッドを使用してページを元の位置に移動
                    _document.MovePage(currentIndex, moveInfo.OriginalPosition);
                }
            }
            
            // 変更通知
            _onPagesChanged?.Invoke();
        }
    }
}