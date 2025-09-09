using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DocOrganizer.Core.Commands;

namespace DocOrganizer.Core.Services
{
    /// <summary>
    /// Undo/Redo機能の具体実装
    /// CommunityToolkit.MvvmのRelayCommandと統合
    /// PropertyChangedイベントでUI自動更新をサポート
    /// </summary>
    public class UndoRedoService : IUndoRedoService, INotifyPropertyChanged
    {
        private readonly Stack<IUndoableCommand> _undoStack = new();
        private readonly Stack<IUndoableCommand> _redoStack = new();
        private BatchCommand? _currentBatch;

        /// <summary>
        /// PropertyChangedイベント
        /// RelayCommand.CanExecute自動更新で使用
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Undo履歴の最大サイズ
        /// デフォルト：50操作（メモリ使用量約30-50MB想定）
        /// </summary>
        public int MaxHistorySize { get; set; } = 50;

        /// <summary>
        /// Undo操作が可能かどうか
        /// </summary>
        public bool CanUndo => _undoStack.Any();

        /// <summary>
        /// Redo操作が可能かどうか
        /// </summary>
        public bool CanRedo => _redoStack.Any();

        /// <summary>
        /// コマンドを実行し、Undoスタックに追加
        /// バッチ処理中の場合は、現在のバッチに追加される
        /// </summary>
        /// <param name="command">実行するコマンド</param>
        public void Execute(IUndoableCommand command)
        {
            if (command == null) 
                throw new ArgumentNullException(nameof(command));

            // バッチ処理中の場合は、バッチに追加（即座実行はしない）
            if (_currentBatch != null)
            {
                _currentBatch.AddCommand(command);
                return;
            }

            // 通常実行：コマンド実行 → Undoスタックに追加
            command.Execute();
            _undoStack.Push(command);
            
            // Redo履歴をクリア（新しい操作でRedo不可能になる）
            _redoStack.Clear();

            // 履歴サイズ制限
            TrimUndoHistory();

            // UI更新通知
            NotifyCanExecuteChanged();
        }

        /// <summary>
        /// 最後の操作をUndo
        /// </summary>
        public void Undo()
        {
            if (!CanUndo) return;

            var command = _undoStack.Pop();
            command.Undo();
            _redoStack.Push(command);

            // UI更新通知
            NotifyCanExecuteChanged();
        }

        /// <summary>
        /// 最後にUndoした操作をRedo
        /// </summary>
        public void Redo()
        {
            if (!CanRedo) return;

            var command = _redoStack.Pop();
            command.Execute();
            _undoStack.Push(command);

            // UI更新通知
            NotifyCanExecuteChanged();
        }

        /// <summary>
        /// バッチ処理を開始
        /// using文で使用し、Dispose時にバッチをコミット
        /// </summary>
        /// <param name="description">バッチ処理の説明</param>
        /// <returns>バッチスコープ。Disposeでバッチコミット</returns>
        public IDisposable BeginBatch(string description)
        {
            if (_currentBatch != null)
                throw new InvalidOperationException("既にバッチ処理が開始されています");

            _currentBatch = new BatchCommand(description);
            return new BatchScope(this);
        }

        /// <summary>
        /// Undo/Redo履歴をクリア
        /// </summary>
        public void ClearHistory()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            _currentBatch = null;

            // UI更新通知
            NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Undo履歴サイズを制限
        /// MaxHistorySizeを超えた場合、古い履歴を削除
        /// </summary>
        private void TrimUndoHistory()
        {
            while (_undoStack.Count > MaxHistorySize)
            {
                var items = _undoStack.ToArray();
                _undoStack.Clear();
                
                // 最新のMaxHistorySize個を残す
                for (int i = 1; i < items.Length; i++)
                {
                    _undoStack.Push(items[i]);
                }
            }
        }

        /// <summary>
        /// PropertyChangedイベント発火
        /// RelayCommand.CanExecute自動更新をトリガー
        /// </summary>
        private void NotifyCanExecuteChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanUndo)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CanRedo)));
        }

        /// <summary>
        /// バッチスコープ内部クラス
        /// using文によるRAII（Resource Acquisition Is Initialization）パターン
        /// </summary>
        private class BatchScope : IDisposable
        {
            private readonly UndoRedoService _service;
            private bool _disposed = false;

            public BatchScope(UndoRedoService service)
            {
                _service = service;
            }

            /// <summary>
            /// バッチ終了時に実行
            /// バッチが空でない場合のみUndoスタックに追加
            /// </summary>
            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;

                if (_service._currentBatch != null && !_service._currentBatch.IsEmpty)
                {
                    // バッチをUndoスタックに追加
                    _service._undoStack.Push(_service._currentBatch);
                    _service._redoStack.Clear();
                    
                    // 履歴サイズ制限適用
                    _service.TrimUndoHistory();
                }

                _service._currentBatch = null;
                
                // UI更新通知
                _service.NotifyCanExecuteChanged();
            }
        }
    }
}