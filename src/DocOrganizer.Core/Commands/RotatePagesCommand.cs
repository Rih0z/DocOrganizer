using System;
using System.Collections.Generic;
using System.Linq;
using DocOrganizer.Core.Models;

namespace DocOrganizer.Core.Commands
{
    /// <summary>
    /// Phase 3: UndoableCommand実装 - ページ回転コマンド
    /// 回転前の角度を保存し、Undoで元の角度に復元を可能にする
    /// </summary>
    public class RotatePagesCommand : IUndoableCommand
    {
        private readonly List<PageRotationInfo> _rotationInfo = new();
        private readonly int _rotationDegrees;
        private readonly Action _onPagesChanged;
        private readonly Func<PdfPage, System.Threading.Tasks.Task> _regenerateThumbnailCallback;

        /// <summary>
        /// ページ回転情報
        /// </summary>
        private class PageRotationInfo
        {
            public PdfPage Page { get; set; }
            public int OriginalRotation { get; set; }
            public int NewRotation { get; set; }

            public PageRotationInfo(PdfPage page, int originalRotation, int rotationDegrees)
            {
                Page = page;
                OriginalRotation = originalRotation;
                
                // 新しい回転角度を計算
                var newRotation = (originalRotation + rotationDegrees) % 360;
                if (newRotation < 0) newRotation += 360;
                NewRotation = newRotation;
            }
        }

        public string Description { get; }

        /// <summary>
        /// 回転コマンドのコンストラクタ
        /// </summary>
        /// <param name="pagesToRotate">回転対象ページリスト</param>
        /// <param name="degrees">回転角度（90, 180, 270, -90など）</param>
        /// <param name="onPagesChanged">ページ変更時のコールバック</param>
        /// <param name="regenerateThumbnailCallback">サムネイル再生成コールバック</param>
        public RotatePagesCommand(
            List<PdfPage> pagesToRotate, 
            int degrees, 
            Action onPagesChanged,
            Func<PdfPage, System.Threading.Tasks.Task> regenerateThumbnailCallback = null)
        {
            _rotationDegrees = degrees;
            _onPagesChanged = onPagesChanged ?? throw new ArgumentNullException(nameof(onPagesChanged));
            _regenerateThumbnailCallback = regenerateThumbnailCallback;

            if (pagesToRotate == null || !pagesToRotate.Any())
                throw new ArgumentException("回転対象ページが指定されていません", nameof(pagesToRotate));

            // 回転情報を事前に収集
            foreach (var page in pagesToRotate)
            {
                _rotationInfo.Add(new PageRotationInfo(page, page.Rotation, degrees));
            }

            var direction = degrees > 0 ? "右" : "左";
            Description = $"{_rotationInfo.Count}ページ{direction}回転({Math.Abs(degrees)}度)";
        }

        /// <summary>
        /// 回転を実行
        /// </summary>
        public void Execute()
        {
            foreach (var rotationInfo in _rotationInfo)
            {
                // Core層データ更新（回転角度計算）
                rotationInfo.Page.Rotation = rotationInfo.NewRotation;
            }

            // 変更通知
            _onPagesChanged?.Invoke();

            // サムネイル再生成（非同期で実行）
            RegenerateThumbnailsAsync();
        }

        /// <summary>
        /// 回転をUndo（元の角度に復元）
        /// </summary>
        public void Undo()
        {
            foreach (var rotationInfo in _rotationInfo)
            {
                // 元の回転角度に復元
                rotationInfo.Page.Rotation = rotationInfo.OriginalRotation;
            }

            // 変更通知
            _onPagesChanged?.Invoke();

            // サムネイル再生成（非同期で実行）
            RegenerateThumbnailsAsync();
        }

        /// <summary>
        /// サムネイル再生成の非同期実行
        /// </summary>
        private void RegenerateThumbnailsAsync()
        {
            if (_regenerateThumbnailCallback != null)
            {
                _ = System.Threading.Tasks.Task.Run(async () =>
                {
                    foreach (var rotationInfo in _rotationInfo)
                    {
                        try
                        {
                            await _regenerateThumbnailCallback(rotationInfo.Page);
                        }
                        catch (Exception ex)
                        {
                            // サムネイル再生成エラーは無視（コア機能に影響させない）
                            System.Diagnostics.Debug.WriteLine($"Thumbnail regeneration error: {ex.Message}");
                        }
                    }
                });
            }
        }
    }
}