namespace DocOrganizer.Core.Commands
{
    /// <summary>
    /// Undo/Redo可能なCommandパターンの基底インターフェース
    /// GoF Commandパターンの実装でUndo機能を追加
    /// </summary>
    public interface IUndoableCommand
    {
        /// <summary>
        /// コマンドの説明（ログ出力・デバッグ用）
        /// 例：「3ページ削除」「左回転」「ページ移動」
        /// </summary>
        string Description { get; }

        /// <summary>
        /// コマンドを実行
        /// Execute()呼び出し時に実際の処理を実行
        /// Undo()で復元するために必要な状態を保存
        /// </summary>
        void Execute();

        /// <summary>
        /// コマンドをUndo（実行前の状態に戻す）
        /// Execute()で実行した処理を元に戻す
        /// </summary>
        void Undo();
    }
}