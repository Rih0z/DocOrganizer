namespace DocOrganizer.Application.Interfaces
{
    /// <summary>
    /// ダイアログサービスのインターフェース
    /// </summary>
    public interface IDialogService
    {
        /// <summary>
        /// ファイルを開くダイアログを表示します
        /// </summary>
        /// <param name="filter">ファイルフィルター（例: "PDF Files|*.pdf"）</param>
        /// <param name="title">ダイアログのタイトル</param>
        /// <returns>選択されたファイルパス。キャンセルされた場合はnull</returns>
        string? ShowOpenFileDialog(string filter, string title);

        /// <summary>
        /// 複数ファイルを開くダイアログを表示します
        /// </summary>
        /// <param name="filter">ファイルフィルター</param>
        /// <param name="title">ダイアログのタイトル</param>
        /// <returns>選択されたファイルパスの配列。キャンセルされた場合は空の配列</returns>
        string[] ShowOpenMultipleFilesDialog(string filter, string title);

        /// <summary>
        /// ファイルを保存するダイアログを表示します
        /// </summary>
        /// <param name="filter">ファイルフィルター</param>
        /// <param name="title">ダイアログのタイトル</param>
        /// <param name="defaultFileName">デフォルトのファイル名</param>
        /// <returns>保存先のファイルパス。キャンセルされた場合はnull</returns>
        string? ShowSaveFileDialog(string filter, string title, string? defaultFileName = null);

        /// <summary>
        /// フォルダ選択ダイアログを表示します
        /// </summary>
        /// <param name="description">ダイアログの説明</param>
        /// <returns>選択されたフォルダパス。キャンセルされた場合はnull</returns>
        string? ShowFolderBrowserDialog(string description);

        /// <summary>
        /// メッセージボックスを表示します
        /// </summary>
        /// <param name="message">メッセージ</param>
        /// <param name="title">タイトル</param>
        /// <param name="buttons">表示するボタン</param>
        /// <param name="icon">アイコンの種類</param>
        /// <returns>ユーザーが選択した結果</returns>
        DialogResult ShowMessage(string message, string title, MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.Information);

        /// <summary>
        /// エラーメッセージを表示します
        /// </summary>
        /// <param name="message">エラーメッセージ</param>
        /// <param name="title">タイトル</param>
        void ShowError(string message, string title = "エラー");

        /// <summary>
        /// 警告メッセージを表示します
        /// </summary>
        /// <param name="message">警告メッセージ</param>
        /// <param name="title">タイトル</param>
        void ShowWarning(string message, string title = "警告");

        /// <summary>
        /// 情報メッセージを表示します
        /// </summary>
        /// <param name="message">情報メッセージ</param>
        /// <param name="title">タイトル</param>
        void ShowInformation(string message, string title = "情報");

        /// <summary>
        /// 確認ダイアログを表示します
        /// </summary>
        /// <param name="message">確認メッセージ</param>
        /// <param name="title">タイトル</param>
        /// <returns>ユーザーが「はい」を選択した場合はtrue</returns>
        bool ShowConfirmation(string message, string title = "確認");
    }

    /// <summary>
    /// ダイアログの結果
    /// </summary>
    public enum DialogResult
    {
        None = 0,
        OK = 1,
        Cancel = 2,
        Yes = 6,
        No = 7
    }

    /// <summary>
    /// メッセージボックスのボタン
    /// </summary>
    public enum MessageBoxButtons
    {
        OK = 0,
        OKCancel = 1,
        YesNo = 4,
        YesNoCancel = 3
    }

    /// <summary>
    /// メッセージボックスのアイコン
    /// </summary>
    public enum MessageBoxIcon
    {
        None = 0,
        Information = 64,
        Warning = 48,
        Error = 16,
        Question = 32
    }
}