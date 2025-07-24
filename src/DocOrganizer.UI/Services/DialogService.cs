using System.Windows;
using Microsoft.Win32;
using DocOrganizer.Application.Interfaces;
using DialogResult = DocOrganizer.Application.Interfaces.DialogResult;

namespace DocOrganizer.UI.Services
{
    public class DialogService : IDialogService
    {
        public string? ShowOpenFileDialog(string filter, string title)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = filter,
                Title = title
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public string[] ShowOpenMultipleFilesDialog(string filter, string title)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = filter,
                Title = title,
                Multiselect = true
            };

            return dialog.ShowDialog() == true ? dialog.FileNames : new string[0];
        }

        public string? ShowSaveFileDialog(string filter, string title, string? defaultFileName = null)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = filter,
                Title = title,
                FileName = defaultFileName ?? ""
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public string? ShowFolderBrowserDialog(string description)
        {
            // WPF用のシンプルなフォルダ選択実装
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = description,
                FileName = "dummy", // これは表示されない
                DefaultExt = ".txt",
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                return System.IO.Path.GetDirectoryName(dialog.FileName);
            }
            
            return null;
        }

        public DialogResult ShowMessage(string message, string title, MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.Information)
        {
            var wpfButtons = ConvertToWpfButtons(buttons);
            var wpfIcon = ConvertToWpfIcon(icon);
            var result = System.Windows.MessageBox.Show(message, title, wpfButtons, wpfIcon);
            return ConvertFromWpfResult(result);
        }

        public void ShowError(string message, string title = "エラー")
        {
            System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public void ShowWarning(string message, string title = "警告")
        {
            System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public void ShowInformation(string message, string title = "情報")
        {
            System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public bool ShowConfirmation(string message, string title = "確認")
        {
            var result = System.Windows.MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }


        private DialogResult ConvertFromWpfResult(MessageBoxResult result)
        {
            return result switch
            {
                MessageBoxResult.OK => DialogResult.OK,
                MessageBoxResult.Cancel => DialogResult.Cancel,
                MessageBoxResult.Yes => DialogResult.Yes,
                MessageBoxResult.No => DialogResult.No,
                _ => DialogResult.None
            };
        }

        private MessageBoxButton ConvertToWpfButtons(MessageBoxButtons buttons)
        {
            return buttons switch
            {
                MessageBoxButtons.OK => MessageBoxButton.OK,
                MessageBoxButtons.OKCancel => MessageBoxButton.OKCancel,
                MessageBoxButtons.YesNo => MessageBoxButton.YesNo,
                MessageBoxButtons.YesNoCancel => MessageBoxButton.YesNoCancel,
                _ => MessageBoxButton.OK
            };
        }

        private MessageBoxImage ConvertToWpfIcon(MessageBoxIcon icon)
        {
            return icon switch
            {
                MessageBoxIcon.None => MessageBoxImage.None,
                MessageBoxIcon.Information => MessageBoxImage.Information,
                MessageBoxIcon.Warning => MessageBoxImage.Warning,
                MessageBoxIcon.Error => MessageBoxImage.Error,
                MessageBoxIcon.Question => MessageBoxImage.Question,
                _ => MessageBoxImage.Information
            };
        }
    }
}