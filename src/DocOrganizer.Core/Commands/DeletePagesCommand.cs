using System;
using System.Collections.Generic;
using System.Linq;
using DocOrganizer.Core.Models;
using SkiaSharp;

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
            public int Rotation { get; set; }  // 回転状態を保存
            public SKBitmap? ThumbnailImageCopy { get; set; }  // サムネイル画像のコピー
            public SKBitmap? PreviewImageCopy { get; set; }    // プレビュー画像のコピー

            public PageDeleteInfo(PdfPage page, int originalPosition, int pageNumber)
            {
                Page = page;
                OriginalPosition = originalPosition;
                PageNumber = pageNumber;
                Rotation = page.Rotation;  // 削除時の回転状態を記録
                
                // 画像データの完全なコピーを作成して保持
                // V3.0.082: 回転後の削除→Undo時に画像が失われる問題を修正
                if (page.ThumbnailImage != null)
                {
                    ThumbnailImageCopy = page.ThumbnailImage.Copy();
                }
                if (page.PreviewImage != null)
                {
                    PreviewImageCopy = page.PreviewImage.Copy();
                }
            }
            
            /// <summary>
            /// 保持している画像コピーを破棄
            /// </summary>
            public void Dispose()
            {
                ThumbnailImageCopy?.Dispose();
                ThumbnailImageCopy = null;
                PreviewImageCopy?.Dispose();
                PreviewImageCopy = null;
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
                // 回転状態を復元
                deleteInfo.Page.Rotation = deleteInfo.Rotation;
                
                // 保存しておいた画像データを復元
                // V3.0.084: 画像データの確実な復元 - 既存の画像を破棄せずに設定
                if (deleteInfo.ThumbnailImageCopy != null)
                {
                    // 既存の画像を破棄せずに、新しい画像を設定
                    deleteInfo.Page.SetThumbnailImage(deleteInfo.ThumbnailImageCopy.Copy());
                }
                if (deleteInfo.PreviewImageCopy != null)
                {
                    deleteInfo.Page.SetPreviewImage(deleteInfo.PreviewImageCopy.Copy());
                }
                
                // 元の位置に挿入（V3.0.084: InsertPageを使用して正しい位置に復元）
                // 現在のページ数を考慮して適切な位置を計算
                int insertPosition = Math.Min(deleteInfo.OriginalPosition, _document.Pages.Count);
                _document.InsertPage(insertPosition, deleteInfo.Page);
            }
            
            // 変更通知（ページ番号の再計算はViewModelレベルで行われる）
            _onPagesChanged?.Invoke();
        }
        
        /// <summary>
        /// デストラクタ - リソースのクリーンアップ
        /// </summary>
        ~DeletePagesCommand()
        {
            // 保持している画像コピーを破棄
            foreach (var deleteInfo in _deletedPagesInfo)
            {
                deleteInfo.Dispose();
            }
        }
    }
}