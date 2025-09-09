using System;
using System.ComponentModel;
using DocOrganizer.Core.Commands;

namespace DocOrganizer.Core.Services
{
    /// <summary>
    /// Undo/Redo機能を提供するサービスインターフェース
    /// CommunityToolkit.MvvmのRelayCommandと統合し、PropertyChangedによりCanExecuteを自動更新
    /// </summary>
    public interface IUndoRedoService : INotifyPropertyChanged
    {
        /// <summary>
        /// Undo操作が可能かどうかを示す
        /// RelayCommand.CanExecuteで使用
        /// </summary>
        bool CanUndo { get; }

        /// <summary>
        /// Redo操作が可能かどうかを示す
        /// RelayCommand.CanExecuteで使用
        /// </summary>
        bool CanRedo { get; }

        /// <summary>
        /// Undo履歴の最大サイズ（メモリ管理用）
        /// デフォルト：50操作
        /// </summary>
        int MaxHistorySize { get; set; }

        /// <summary>
        /// コマンドを実行し、Undoスタックに追加
        /// バッチ処理中の場合は、現在のバッチに追加される
        /// </summary>
        /// <param name="command">実行するコマンド</param>
        void Execute(IUndoableCommand command);

        /// <summary>
        /// 最後の操作をUndo
        /// CanUndoがfalseの場合は何もしない
        /// </summary>
        void Undo();

        /// <summary>
        /// 最後にUndoした操作をRedo
        /// CanRedoがfalseの場合は何もしない
        /// </summary>
        void Redo();

        /// <summary>
        /// バッチ処理を開始
        /// using文で使用し、Dispose時にバッチをコミット
        /// </summary>
        /// <param name="description">バッチ処理の説明</param>
        /// <returns>バッチスコープ。Disposeでバッチコミット</returns>
        IDisposable BeginBatch(string description);

        /// <summary>
        /// Undo/Redo履歴をクリア
        /// メモリ使用量制限時やドキュメント切り替え時に使用
        /// </summary>
        void ClearHistory();
    }
}