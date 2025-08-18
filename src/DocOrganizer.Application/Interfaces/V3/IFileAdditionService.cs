using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DocOrganizer.Core.Models;

namespace DocOrganizer.Application.Interfaces.V3
{
    /// <summary>
    /// 🎯 V3アーキテクチャ: ファイル追加専用サービス
    /// 責務: 既存ドキュメントへの新ファイル追加処理のみ
    /// OSS標準: 単一責任原則、依存性注入、イベント駆動
    /// </summary>
    public interface IFileAdditionService
    {
        /// <summary>
        /// 画像ファイルを既存PDFドキュメントに追加
        /// </summary>
        /// <param name="document">対象ドキュメント</param>
        /// <param name="imageFiles">追加する画像ファイルパス</param>
        /// <param name="insertPosition">挿入位置（-1で末尾追加）</param>
        /// <returns>追加されたページ数</returns>
        Task<int> AddImageFilesToDocumentAsync(PdfDocument document, IEnumerable<string> imageFiles, int insertPosition = -1);

        /// <summary>
        /// PDFファイルを既存PDFドキュメントに結合追加
        /// </summary>
        /// <param name="document">対象ドキュメント</param>
        /// <param name="pdfFiles">追加するPDFファイルパス</param>
        /// <param name="insertPosition">挿入位置（-1で末尾追加）</param>
        /// <returns>追加されたページ数</returns>
        Task<int> AddPdfFilesToDocumentAsync(PdfDocument document, IEnumerable<string> pdfFiles, int insertPosition = -1);

        /// <summary>
        /// 混在ファイル（画像+PDF）を既存ドキュメントに追加
        /// </summary>
        /// <param name="document">対象ドキュメント</param>
        /// <param name="files">追加するファイルパス（画像・PDF混在可能）</param>
        /// <param name="insertPosition">挿入位置（-1で末尾追加）</param>
        /// <returns>追加結果（ページ数、処理時間等）</returns>
        Task<FileAdditionResult> AddMixedFilesToDocumentAsync(PdfDocument document, IEnumerable<string> files, int insertPosition = -1);

        /// <summary>
        /// ファイル追加可能性の事前検証
        /// </summary>
        /// <param name="files">検証対象ファイル</param>
        /// <returns>検証結果</returns>
        Task<FileAdditionValidationResult> ValidateFilesForAdditionAsync(IEnumerable<string> files);

        // イベント
        event EventHandler<FileAdditionProgressEventArgs>? ProgressUpdated;
        event EventHandler<FileAdditionCompletedEventArgs>? AdditionCompleted;
        event EventHandler<FileAdditionErrorEventArgs>? ErrorOccurred;
    }

    /// <summary>
    /// ファイル追加結果
    /// </summary>
    public class FileAdditionResult
    {
        public int AddedPagesCount { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public List<string> SuccessfulFiles { get; set; } = new();
        public List<string> FailedFiles { get; set; } = new();
        public string Summary => $"{AddedPagesCount}ページ追加完了（成功: {SuccessfulFiles.Count}, 失敗: {FailedFiles.Count}）";
    }

    /// <summary>
    /// ファイル追加検証結果
    /// </summary>
    public class FileAdditionValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> ValidFiles { get; set; } = new();
        public List<string> InvalidFiles { get; set; } = new();
        public List<string> ValidationErrors { get; set; } = new();
        public long EstimatedSizeBytes { get; set; }
    }

    // イベント引数クラス
    public class FileAdditionProgressEventArgs : EventArgs
    {
        public int ProcessedCount { get; }
        public int TotalCount { get; }
        public string CurrentFile { get; }
        public double ProgressPercentage => TotalCount > 0 ? (double)ProcessedCount / TotalCount * 100 : 0;

        public FileAdditionProgressEventArgs(int processedCount, int totalCount, string currentFile)
        {
            ProcessedCount = processedCount;
            TotalCount = totalCount;
            CurrentFile = currentFile;
        }
    }

    public class FileAdditionCompletedEventArgs : EventArgs
    {
        public FileAdditionResult Result { get; }

        public FileAdditionCompletedEventArgs(FileAdditionResult result)
        {
            Result = result;
        }
    }

    public class FileAdditionErrorEventArgs : EventArgs
    {
        public string ErrorMessage { get; }
        public Exception? Exception { get; }
        public string? FailedFile { get; }

        public FileAdditionErrorEventArgs(string errorMessage, Exception? exception = null, string? failedFile = null)
        {
            ErrorMessage = errorMessage;
            Exception = exception;
            FailedFile = failedFile;
        }
    }
}