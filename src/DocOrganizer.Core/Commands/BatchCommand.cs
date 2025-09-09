using System.Collections.Generic;
using System.Linq;

namespace DocOrganizer.Core.Commands
{
    /// <summary>
    /// 複数のUndoableCommandをまとめて実行・Undoするバッチコマンド
    /// 一括操作（複数ページ回転、一括削除など）で使用
    /// </summary>
    public class BatchCommand : IUndoableCommand
    {
        private readonly List<IUndoableCommand> _commands = new();

        /// <summary>
        /// バッチコマンドのコンストラクタ
        /// </summary>
        /// <param name="description">バッチ処理の説明</param>
        public BatchCommand(string description)
        {
            Description = description;
        }

        /// <summary>
        /// バッチ処理の説明
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// バッチにコマンドを追加
        /// UndoRedoService.BeginBatch()中に呼び出される
        /// </summary>
        /// <param name="command">追加するコマンド</param>
        public void AddCommand(IUndoableCommand command)
        {
            _commands.Add(command);
        }

        /// <summary>
        /// バッチ内の全コマンドを順次実行
        /// </summary>
        public void Execute()
        {
            foreach (var command in _commands)
            {
                command.Execute();
            }
        }

        /// <summary>
        /// バッチ内の全コマンドを逆順でUndo
        /// 後から実行されたものから順にUndo
        /// </summary>
        public void Undo()
        {
            // 逆順でUndoすることで、実行順序と逆の順序で元に戻す
            foreach (var command in _commands.AsEnumerable().Reverse())
            {
                command.Undo();
            }
        }

        /// <summary>
        /// バッチが空かどうかを判定
        /// </summary>
        /// <returns>true: 空のバッチ, false: コマンドを含む</returns>
        public bool IsEmpty => !_commands.Any();
    }
}