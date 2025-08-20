using System.Windows;
using Microsoft.Win32;
using DocOrganizer.Application.Interfaces;
using DialogResult = DocOrganizer.Application.Interfaces.DialogResult;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

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

        public string? ShowInputDialog(string message, string title = "入力", string? defaultValue = null)
        {
            // シンプルな入力ダイアログの実装（MessageBoxの代替）
            var inputWindow = new Window
            {
                Title = title,
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize
            };

            var stackPanel = new StackPanel { Margin = new Thickness(20) };
            
            var messageBlock = new TextBlock 
            { 
                Text = message, 
                Margin = new Thickness(0, 0, 0, 15),
                TextWrapping = TextWrapping.Wrap
            };
            
            var textBox = new TextBox 
            { 
                Text = defaultValue ?? "",
                Margin = new Thickness(0, 0, 0, 15)
            };
            
            var buttonPanel = new StackPanel 
            { 
                Orientation = Orientation.Horizontal, 
                HorizontalAlignment = HorizontalAlignment.Right 
            };
            
            var okButton = new Button 
            { 
                Content = "OK", 
                Width = 75, 
                Margin = new Thickness(0, 0, 10, 0),
                IsDefault = true
            };
            
            var cancelButton = new Button 
            { 
                Content = "キャンセル", 
                Width = 75,
                IsCancel = true
            };

            okButton.Click += (s, e) => { inputWindow.DialogResult = true; inputWindow.Close(); };
            cancelButton.Click += (s, e) => { inputWindow.DialogResult = false; inputWindow.Close(); };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            
            stackPanel.Children.Add(messageBlock);
            stackPanel.Children.Add(textBox);
            stackPanel.Children.Add(buttonPanel);
            
            inputWindow.Content = stackPanel;
            textBox.Focus();

            return inputWindow.ShowDialog() == true ? textBox.Text : null;
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