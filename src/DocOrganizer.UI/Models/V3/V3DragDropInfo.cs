using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DocOrganizer.UI.ViewModels;
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
            // 🎯 根本修正: 実際のドロップ位置にある要素をHitTestで特定
            var actualDropPosition = dragEventArgs.GetPosition(targetElement);
            var actualTargetElement = FindActualTargetElement(targetElement, actualDropPosition) ?? targetElement;
            
            TargetElement = actualTargetElement;
            DropPosition = dragEventArgs.GetPosition(actualTargetElement);
            AllowedEffects = dragEventArgs.AllowedEffects;
            KeyStates = dragEventArgs.KeyStates;
            Effects = DragDropEffects.None;
            
            // 🎯 V3.0.020: InsertIndex計算実装（最重要修正）
            InsertIndex = CalculateInsertIndex(targetElement, actualDropPosition);

            // 🎯 V3.0.023: サムネイルドラッグと外部ファイルドロップの正確な区別
            if (dragEventArgs.Data.GetDataPresent(DataFormats.Text))
            {
                // サムネイルドラッグの場合 - IDataObjectを保持
                Data = dragEventArgs.Data;
                FilePaths = new string[0];
            }
            else if (dragEventArgs.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // 外部ファイルドロップの場合 - String[]を設定
                FilePaths = (string[])dragEventArgs.Data.GetData(DataFormats.FileDrop);
                Data = FilePaths;
            }
            else
            {
                // その他のデータ
                Data = dragEventArgs.Data;
                FilePaths = new string[0];
            }
        }

        /// <summary>
        /// 🎯 根本修正: HitTestでドロップ位置の実際の要素を特定
        /// </summary>
        private FrameworkElement FindActualTargetElement(FrameworkElement rootElement, Point position)
        {
            try
            {
                // HitTestでドロップ位置の要素を取得
                var hitResult = VisualTreeHelper.HitTest(rootElement, position);
                if (hitResult?.VisualHit != null)
                {
                    // ListBoxItemを探す
                    var current = hitResult.VisualHit as DependencyObject;
                    while (current != null)
                    {
                        if (current is ListBoxItem listBoxItem)
                        {
                            return listBoxItem;
                        }
                        if (current is FrameworkElement frameworkElement && 
                            frameworkElement.DataContext is V3PageViewModel)
                        {
                            return frameworkElement;
                        }
                        current = VisualTreeHelper.GetParent(current);
                    }
                }
            }
            catch
            {
                // HitTest失敗時は元の要素を返す
            }

            return null;
        }

        /// <summary>
        /// 🎯 V3.0.020: ドロップ位置に基づくInsertIndex計算（根本修正）
        /// </summary>
        private int CalculateInsertIndex(FrameworkElement targetElement, Point dropPosition)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[CalculateInsertIndex] 開始 - targetElement: {targetElement?.GetType().Name}, dropPosition: {dropPosition}");
                
                // 🎯 V3.0.025: より堅牢なListBox検索
                var listBox = FindParentListBox(targetElement);
                if (listBox == null)
                {
                    System.Diagnostics.Debug.WriteLine("[CalculateInsertIndex] ListBoxが見つからないため -1 を返します");
                    return -1;
                }
                
                System.Diagnostics.Debug.WriteLine($"[CalculateInsertIndex] ListBox発見: {listBox.Items.Count}個のアイテム");

                // ドロップ位置での アイテムインデックス計算
                var itemsCount = listBox.Items.Count;
                if (itemsCount == 0)
                {
                    System.Diagnostics.Debug.WriteLine("[CalculateInsertIndex] 空のリストのため 0 を返します");
                    return 0;
                }

                // 🎯 V3.0.025: ListBoxを基準とした座標系に変換
                var listBoxRelativePosition = targetElement.TranslatePoint(dropPosition, listBox);
                System.Diagnostics.Debug.WriteLine($"[CalculateInsertIndex] ListBox相対位置: {listBoxRelativePosition}");

                // 各ListBoxItemの位置をチェック
                for (int i = 0; i < itemsCount; i++)
                {
                    var container = listBox.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
                    if (container == null)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CalculateInsertIndex] インデックス {i} のコンテナが null - スキップ");
                        continue;
                    }

                    // 🎯 V3.0.025: ListBox基準での位置計算
                    var itemPositionInListBox = container.TranslatePoint(new Point(0, 0), listBox);
                    var itemBounds = new Rect(itemPositionInListBox, container.RenderSize);
                    
                    System.Diagnostics.Debug.WriteLine($"[CalculateInsertIndex] アイテム{i}: 位置={itemPositionInListBox}, サイズ={container.RenderSize}, 境界={itemBounds}");

                    // ドロップ位置がアイテムの上半分にある場合、そのアイテムの前に挿入
                    var itemMiddleY = itemBounds.Top + (itemBounds.Height / 2);
                    System.Diagnostics.Debug.WriteLine($"[CalculateInsertIndex] アイテム{i}中点Y: {itemMiddleY}, ドロップY: {listBoxRelativePosition.Y}");
                    
                    if (listBoxRelativePosition.Y <= itemMiddleY)
                    {
                        System.Diagnostics.Debug.WriteLine($"[CalculateInsertIndex] アイテム{i}の前に挿入 - インデックス {i} を返します");
                        return i;
                    }
                }

                // 最後のアイテムより下にドロップした場合、末尾に挿入
                System.Diagnostics.Debug.WriteLine($"[CalculateInsertIndex] 末尾に挿入 - インデックス {itemsCount} を返します");
                return itemsCount;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CalculateInsertIndex] 例外発生: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[CalculateInsertIndex] スタックトレース: {ex.StackTrace}");
                return -1;
            }
        }

        /// <summary>
        /// 🎯 V3.0.020: 親のListBoxを検索
        /// </summary>
        private ListBox FindParentListBox(DependencyObject child)
        {
            System.Diagnostics.Debug.WriteLine($"[FindParentListBox] 開始 - child: {child?.GetType().Name}");
            
            var current = child;
            var depth = 0;
            
            while (current != null && depth < 20) // 🎯 V3.0.025: 無限ループ防止
            {
                System.Diagnostics.Debug.WriteLine($"[FindParentListBox] 深度{depth}: {current.GetType().Name}");
                
                if (current is ListBox listBox)
                {
                    System.Diagnostics.Debug.WriteLine($"[FindParentListBox] ListBox発見! 深度{depth}で発見");
                    return listBox;
                }
                
                // 🎯 V3.0.025: より広範囲の親要素検索
                var parent = VisualTreeHelper.GetParent(current);
                if (parent == null)
                {
                    // VisualTreeで見つからない場合、LogicalTreeも試す
                    if (current is FrameworkElement frameworkElement)
                    {
                        parent = frameworkElement.Parent;
                        System.Diagnostics.Debug.WriteLine($"[FindParentListBox] LogicalTree経由で親要素検索: {parent?.GetType().Name}");
                    }
                }
                
                current = parent;
                depth++;
            }
            
            System.Diagnostics.Debug.WriteLine($"[FindParentListBox] ListBoxが見つかりませんでした (最大深度: {depth})");
            return null;
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
            
            // 🎯 修正: ListBoxItemのDataContextを正しく取得
            if (sourceElement is ListBoxItem listBoxItem)
            {
                // ListBoxItemの場合は直接DataContextを使用
                SourceItem = listBoxItem.DataContext;
            }
            else if (sourceElement is ListBox listBox)
            {
                // ListBoxの場合は、マウス位置のListBoxItemを特定
                var position = mouseEventArgs.GetPosition(listBox);
                var hitResult = VisualTreeHelper.HitTest(listBox, position);
                if (hitResult?.VisualHit != null)
                {
                    var current = hitResult.VisualHit as DependencyObject;
                    while (current != null)
                    {
                        if (current is ListBoxItem item)
                        {
                            SourceItem = item.DataContext;
                            break;
                        }
                        current = VisualTreeHelper.GetParent(current);
                    }
                }
                
                // フォールバック: ListBoxItemが見つからない場合
                if (SourceItem == null)
                {
                    SourceItem = sourceElement.DataContext;
                }
            }
            else
            {
                SourceItem = sourceElement.DataContext;
            }
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