using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using DocOrganizer.UI.ViewModels.V3;

namespace DocOrganizer.UI.Models.V3
{
    /// <summary>
    /// 🎯 V3 OSS標準: ドロップ情報実装
    /// GongSolutions.WPF.DragDropパターン準拠
    /// </summary>
    public class V3DropInfo : IAdvancedDropInfo
    {
        public object Data { get; private set; }
        public FrameworkElement TargetElement { get; private set; }
        public Point DropPosition { get; private set; }
        public DragDropEffects AllowedEffects { get; private set; }
        public DragDropKeyStates KeyStates { get; private set; }
        public string[] FilePaths { get; private set; }
        public int InsertIndex { get; set; }
        public DragDropEffects Effects { get; set; }

        public V3DropInfo(DragEventArgs dragEventArgs, FrameworkElement targetElement)
        {
            TargetElement = targetElement;
            DropPosition = dragEventArgs.GetPosition(targetElement);
            AllowedEffects = dragEventArgs.AllowedEffects;
            KeyStates = dragEventArgs.KeyStates;
            Effects = DragDropEffects.None;
            InsertIndex = -1;

            // 🎯 OSS標準: データ種別判定とファイルパス抽出
            if (dragEventArgs.Data.GetDataPresent(DataFormats.FileDrop))
            {
                FilePaths = (string[])dragEventArgs.Data.GetData(DataFormats.FileDrop);
                Data = FilePaths;
            }
            else
            {
                Data = dragEventArgs.Data;
                FilePaths = new string[0];
            }
        }

        /// <summary>
        /// 🎯 OSS標準: サポートファイル判定
        /// </summary>
        public bool HasSupportedFiles()
        {
            if (FilePaths == null || !FilePaths.Any())
                return false;

            return FilePaths.Any(file => IsSupportedFile(file));
        }

        /// <summary>
        /// 🎯 OSS標準: サポートファイル取得
        /// </summary>
        public string[] GetSupportedFiles()
        {
            if (FilePaths == null)
                return new string[0];

            return FilePaths.Where(file => IsSupportedFile(file)).ToArray();
        }

        /// <summary>
        /// 🎯 OSS標準: ファイル種別判定
        /// </summary>
        private bool IsSupportedFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return false;

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            
            // 画像ファイル
            var imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".heic", ".heif", ".bmp", ".tiff", ".gif", ".webp" };
            
            // PDFファイル
            var pdfExtensions = new[] { ".pdf" };
            
            return imageExtensions.Contains(extension) || pdfExtensions.Contains(extension);
        }
    }

    /// <summary>
    /// 🎯 V3 OSS標準: ドラッグ情報実装
    /// </summary>
    public class V3DragInfo : IAdvancedDragInfo
    {
        public FrameworkElement SourceElement { get; private set; }
        public Point StartPosition { get; private set; }
        public object SourceItem { get; private set; }
        public MouseEventArgs MouseEventArgs { get; private set; }

        public V3DragInfo(FrameworkElement sourceElement, MouseEventArgs mouseEventArgs)
        {
            SourceElement = sourceElement;
            MouseEventArgs = mouseEventArgs;
            StartPosition = mouseEventArgs.GetPosition(sourceElement);
            SourceItem = sourceElement.DataContext;
        }
    }

    /// <summary>
    /// 🎯 V3 OSS標準: ドラッグ完了情報実装
    /// </summary>
    public class V3DragCompletedInfo : IAdvancedDragCompletedInfo
    {
        public IAdvancedDragInfo DragInfo { get; private set; }
        public DragDropEffects DragResult { get; private set; }
        public bool IsCancelled => DragResult == DragDropEffects.None;

        public V3DragCompletedInfo(IAdvancedDragInfo dragInfo, DragDropEffects dragResult)
        {
            DragInfo = dragInfo;
            DragResult = dragResult;
        }
    }
}