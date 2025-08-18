using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DocOrganizer.Application.Interfaces;

namespace DocOrganizer.UI.ViewModels.V3
{
    /// <summary>
    /// 🎯 V3アーキテクチャ: ステータス管理専用ViewModel
    /// 責務: プログレス・状態表示・ユーザー通知のみ
    /// 目標: 100行以下、3メソッド以下
    /// </summary>
    public partial class StatusManagementViewModel : ObservableObject
    {
        private readonly IDialogService _dialogService;
        private CancellationTokenSource? _currentOperationCts;

        [ObservableProperty]
        private bool isProcessing;

        [ObservableProperty]
        private double progressPercentage;

        [ObservableProperty]
        private string statusMessage = "準備完了";

        [ObservableProperty]
        private string detailMessage = "";

        [ObservableProperty]
        private bool canCancel;

        public StatusManagementViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;
        }

        /// <summary>
        /// 操作開始と進捗管理
        /// </summary>
        public void StartOperation(string operationName, bool cancellable = false)
        {
            IsProcessing = true;
            ProgressPercentage = 0;
            StatusMessage = operationName;
            DetailMessage = "開始中...";
            CanCancel = cancellable;

            if (cancellable)
            {
                _currentOperationCts = new CancellationTokenSource();
            }

            OperationStarted?.Invoke(this, new OperationStatusEventArgs(operationName, true));
        }

        /// <summary>
        /// 進捗更新
        /// </summary>
        public void UpdateProgress(double percentage, string detail = "")
        {
            ProgressPercentage = Math.Max(0, Math.Min(100, percentage));
            
            if (!string.IsNullOrEmpty(detail))
            {
                DetailMessage = detail;
            }

            ProgressUpdated?.Invoke(this, new ProgressEventArgs(ProgressPercentage, detail));
        }

        /// <summary>
        /// 操作完了
        /// </summary>
        public void CompleteOperation(string completionMessage = "操作完了", bool success = true)
        {
            IsProcessing = false;
            ProgressPercentage = success ? 100 : 0;
            StatusMessage = completionMessage;
            DetailMessage = "";
            CanCancel = false;

            _currentOperationCts?.Dispose();
            _currentOperationCts = null;

            OperationCompleted?.Invoke(this, new OperationStatusEventArgs(completionMessage, success));
        }

        /// <summary>
        /// 操作キャンセル
        /// </summary>
        public void CancelOperation()
        {
            if (CanCancel && _currentOperationCts != null)
            {
                _currentOperationCts.Cancel();
                CompleteOperation("操作がキャンセルされました", false);
                
                OperationCancelled?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// エラー表示とロギング
        /// </summary>
        public void ShowError(string errorMessage, Exception? exception = null)
        {
            CompleteOperation("エラーが発生しました", false);
            
            _dialogService.ShowError(errorMessage);
            
            ErrorOccurred?.Invoke(this, new ErrorEventArgs(errorMessage, exception));
        }

        /// <summary>
        /// 成功メッセージ表示
        /// </summary>
        public void ShowSuccess(string successMessage)
        {
            _dialogService.ShowInformation(successMessage);
            
            SuccessMessageShown?.Invoke(this, new MessageEventArgs(successMessage));
        }

        /// <summary>
        /// 警告メッセージ表示
        /// </summary>
        public void ShowWarning(string warningMessage)
        {
            _dialogService.ShowWarning(warningMessage);
            
            WarningMessageShown?.Invoke(this, new MessageEventArgs(warningMessage));
        }

        /// <summary>
        /// キャンセレーショントークン取得
        /// </summary>
        public CancellationToken GetCancellationToken()
        {
            return _currentOperationCts?.Token ?? CancellationToken.None;
        }

        // Events for coordination with other ViewModels
        public event EventHandler<OperationStatusEventArgs>? OperationStarted;
        public event EventHandler<ProgressEventArgs>? ProgressUpdated;
        public event EventHandler<OperationStatusEventArgs>? OperationCompleted;
        public event EventHandler? OperationCancelled;
        public event EventHandler<ErrorEventArgs>? ErrorOccurred;
        public event EventHandler<MessageEventArgs>? SuccessMessageShown;
        public event EventHandler<MessageEventArgs>? WarningMessageShown;
    }

    // Event argument classes
    public class OperationStatusEventArgs : EventArgs
    {
        public string OperationName { get; }
        public bool IsSuccess { get; }

        public OperationStatusEventArgs(string operationName, bool isSuccess)
        {
            OperationName = operationName;
            IsSuccess = isSuccess;
        }
    }

    public class ProgressEventArgs : EventArgs
    {
        public double Percentage { get; }
        public string Detail { get; }

        public ProgressEventArgs(double percentage, string detail)
        {
            Percentage = percentage;
            Detail = detail;
        }
    }

    public class ErrorEventArgs : EventArgs
    {
        public string ErrorMessage { get; }
        public Exception? Exception { get; }

        public ErrorEventArgs(string errorMessage, Exception? exception = null)
        {
            ErrorMessage = errorMessage;
            Exception = exception;
        }
    }

    public class MessageEventArgs : EventArgs
    {
        public string Message { get; }

        public MessageEventArgs(string message)
        {
            Message = message;
        }
    }
}