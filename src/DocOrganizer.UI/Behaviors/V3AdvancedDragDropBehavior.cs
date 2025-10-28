using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;  // ✅ V3.0.132: ScrollBar判定用
// Microsoft.Xaml.Behaviors.Wpfは未使用 - 削除
using DocOrganizer.UI.ViewModels.V3;
using DocOrganizer.UI.ViewModels;
using DocOrganizer.UI.Adorners;
using DocOrganizer.UI.Models.V3;

namespace DocOrganizer.UI.Behaviors
{
    /// <summary>
    /// 🎯 V3 OSS標準: GongSolutions.WPF.DragDropパターン準拠
    /// アタッチドプロパティベースの高度なドラッグ&ドロップBehavior
    /// </summary>
    public static class V3AdvancedDragDropBehavior
    {
        #region 🎯 V3デバッグ: 統一ログ出力
        
        /// <summary>
        /// 🎯 V3デバッグ: 統一DEBUG_LOG.txtファイルへの非同期ログ出力
        /// </summary>
        private static async Task AppendDebugLogAsync(string message)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                var logEntry = $"[{timestamp}] [V3DragDrop] {message}{Environment.NewLine}";
                
                // 🎯 第16条準拠: 統一DebugLogger使用
                await DocOrganizer.Core.Logging.DebugLogger.LogAsync(message, "V3DragDrop");
            }
            catch
            {
                // ログ出力エラーは無視（メイン処理を妨げない）
            }
        }
        
        #endregion
        
        #region OSS標準: アタッチドプロパティ (GongSolutionsパターン)

        /// <summary>
        /// 🎯 OSS標準: IsDragSource アタッチドプロパティ
        /// </summary>
        public static readonly DependencyProperty IsDragSourceProperty =
            DependencyProperty.RegisterAttached(
                "IsDragSource",
                typeof(bool),
                typeof(V3AdvancedDragDropBehavior),
                new PropertyMetadata(false, OnIsDragSourceChanged));

        /// <summary>
        /// 🎯 OSS標準: IsDropTarget アタッチドプロパティ
        /// </summary>
        public static readonly DependencyProperty IsDropTargetProperty =
            DependencyProperty.RegisterAttached(
                "IsDropTarget",
                typeof(bool),
                typeof(V3AdvancedDragDropBehavior),
                new PropertyMetadata(false, OnIsDropTargetChanged));

        /// <summary>
        /// 🎯 OSS標準: DropHandler アタッチドプロパティ
        /// </summary>
        public static readonly DependencyProperty DropHandlerProperty =
            DependencyProperty.RegisterAttached(
                "DropHandler",
                typeof(IAdvancedDropHandler),
                typeof(V3AdvancedDragDropBehavior),
                new PropertyMetadata(null));

        /// <summary>
        /// 🎯 OSS標準: DragHandler アタッチドプロパティ
        /// </summary>
        public static readonly DependencyProperty DragHandlerProperty =
            DependencyProperty.RegisterAttached(
                "DragHandler",
                typeof(IAdvancedDragHandler),
                typeof(V3AdvancedDragDropBehavior),
                new PropertyMetadata(null));

        #endregion

        #region OSS標準: アタッチドプロパティ Get/Set メソッド

        public static bool GetIsDragSource(DependencyObject obj)
            => (bool)obj.GetValue(IsDragSourceProperty);

        public static void SetIsDragSource(DependencyObject obj, bool value)
            => obj.SetValue(IsDragSourceProperty, value);

        public static bool GetIsDropTarget(DependencyObject obj)
            => (bool)obj.GetValue(IsDropTargetProperty);

        public static void SetIsDropTarget(DependencyObject obj, bool value)
            => obj.SetValue(IsDropTargetProperty, value);

        public static IAdvancedDropHandler GetDropHandler(DependencyObject obj)
            => (IAdvancedDropHandler)obj.GetValue(DropHandlerProperty);

        public static void SetDropHandler(DependencyObject obj, IAdvancedDropHandler value)
            => obj.SetValue(DropHandlerProperty, value);

        public static IAdvancedDragHandler GetDragHandler(DependencyObject obj)
            => (IAdvancedDragHandler)obj.GetValue(DragHandlerProperty);

        public static void SetDragHandler(DependencyObject obj, IAdvancedDragHandler value)
            => obj.SetValue(DragHandlerProperty, value);

        #endregion

        #region OSS標準: イベントハンドラー

        private static void OnIsDragSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement element)
            {
                if ((bool)e.NewValue)
                {
                    element.MouseMove += OnMouseMove;
                    // ✅ V3.0.133: PreviewMouseLeftButtonDownに変更（トンネリングイベントで確実に検出）
                    element.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
                    element.GiveFeedback += OnGiveFeedback;
                    _ = AppendDebugLogAsync($"[OnIsDragSourceChanged] ドラッグソース有効化（Preview使用）: {element.GetType().Name} - DataContext: {element.DataContext?.GetType().Name ?? "null"}");
                }
                else
                {
                    element.MouseMove -= OnMouseMove;
                    element.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
                    element.GiveFeedback -= OnGiveFeedback;
                    _ = AppendDebugLogAsync($"[OnIsDragSourceChanged] ドラッグソース無効化: {element.GetType().Name}");
                }
            }
        }

        private static void OnIsDropTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FrameworkElement element)
            {
                if ((bool)e.NewValue)
                {
                    _ = AppendDebugLogAsync($"OnIsDropTargetChanged - ドロップターゲット有効化: {element.GetType().Name}");
                    element.AllowDrop = true;
                    element.DragEnter += OnDragEnter;
                    element.DragOver += OnDragOver;
                    element.Drop += OnDrop;
                    element.DragLeave += OnDragLeave;
                    _ = AppendDebugLogAsync($"OnIsDropTargetChanged - イベントハンドラー登録完了: {element.GetType().Name}");
                }
                else
                {
                    _ = AppendDebugLogAsync($"OnIsDropTargetChanged - ドロップターゲット無効化: {element.GetType().Name}");
                    element.AllowDrop = false;
                    element.DragEnter -= OnDragEnter;
                    element.DragOver -= OnDragOver;
                    element.Drop -= OnDrop;
                    element.DragLeave -= OnDragLeave;
                }
            }
        }

        #endregion

        #region OSS標準: ドラッグソース処理

        private static Point _dragStartPoint;
        private static bool _isDragging;
        private static bool _isDropProcessing; // 🎯 V3.0.025: イベント重複防止フラグ

        /// <summary>
        /// ✅ V3.0.133: PreviewMouseLeftButtonDownイベントハンドラー（トンネリングイベント）
        /// ListBoxItemのクリックのみをドラッグ対象とし、ScrollBar等を完全除外
        /// </summary>
        private static void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _ = AppendDebugLogAsync($"[OnPreviewMouseLeftButtonDown] イベント発火 - sender: {sender?.GetType().Name}, OriginalSource: {e.OriginalSource?.GetType().Name}");

            // ✅ V3.0.133: ListBoxItem判定による確実なドラッグ対象識別
            // ListBoxItem上のクリックのみをドラッグ対象とする（ScrollBar、余白、ヘッダー等は除外）
            var listBoxItem = FindAncestor<ListBoxItem>(e.OriginalSource as DependencyObject);

            if (listBoxItem == null)
            {
                // ListBoxItem以外（ScrollBar、余白、ヘッダー等）はドラッグ無効
                _isDragging = false;
                _ = AppendDebugLogAsync("[OnPreviewMouseLeftButtonDown] ListBoxItem以外のクリック - ドラッグ無効化（ScrollBar等）");
                return;
            }

            _ = AppendDebugLogAsync($"[OnPreviewMouseLeftButtonDown] ListBoxItemクリック検出 - DataContext: {listBoxItem.DataContext?.GetType().Name}");

            // ✅ V3.0.130: Ctrl/Shift時はドラッグ無効化（複数選択操作を優先）
            bool isCtrlPressed = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            bool isShiftPressed = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

            if (isCtrlPressed || isShiftPressed)
            {
                // 複数選択モード: ドラッグ無効
                _isDragging = false;
                _ = AppendDebugLogAsync("[OnPreviewMouseLeftButtonDown] Ctrl/Shift検出 - ドラッグ無効化（複数選択優先）");
                return;
            }

            // ✅ ListBoxItemのドラッグ開始点を記録
            _dragStartPoint = e.GetPosition(null);
            _isDragging = false;
            _ = AppendDebugLogAsync($"[OnPreviewMouseLeftButtonDown] ListBoxItemドラッグ開始点記録 - Position: X={_dragStartPoint.X:F1}, Y={_dragStartPoint.Y:F1}");
        }


        private static async void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && !_isDragging)
            {
                // ✅ V3.0.133: ドラッグ開始点が記録されていない場合はスキップ
                // OnPreviewMouseLeftButtonDownでListBoxItem以外の場合、_dragStartPointは記録されない
                if (_dragStartPoint == default(Point))
                {
                    await AppendDebugLogAsync("[OnMouseMove] ドラッグ開始点未設定 - スキップ（ScrollBar等のクリック）");
                    return;
                }

                // ✅ V3.0.130: Ctrl/Shift時はドラッグ判定スキップ（複数選択優先）
                bool isCtrlPressed = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
                bool isShiftPressed = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

                if (isCtrlPressed || isShiftPressed)
                {
                    await AppendDebugLogAsync("[OnMouseMove] Ctrl/Shift検出 - ドラッグ判定スキップ（複数選択優先）");
                    return;  // 複数選択モード中はドラッグ無効
                }

                await AppendDebugLogAsync($"[OnMouseMove] マウス移動検出 - sender: {sender?.GetType().Name}");

                var currentPosition = e.GetPosition(null);
                var diff = _dragStartPoint - currentPosition;

                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    await AppendDebugLogAsync($"[OnMouseMove] ドラッグ距離閾値越え - 距離: X={Math.Abs(diff.X):F1}, Y={Math.Abs(diff.Y):F1}");
                    _isDragging = true;
                    await StartDragAsync(sender as FrameworkElement, e);
                }
            }
        }

        /// <summary>
        /// 🎯 V3.0.024: 視覚フィードバック - マウスカーソル制御
        /// OSS標準: GiveFeedbackイベント処理による動的カーソル変更
        /// </summary>
        private static async void OnGiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            try
            {
                await AppendDebugLogAsync($"[OnGiveFeedback] フィードバック要求 - Effects: {e.Effects}, UseDefaultCursors: {e.UseDefaultCursors}");
                
                // 🎯 OSS標準: ドラッグ効果に応じたカーソル変更
                if (e.Effects.HasFlag(DragDropEffects.Move))
                {
                    // サムネイル並び替え用: 掴みカーソル
                    Mouse.SetCursor(Cursors.Hand);
                    await AppendDebugLogAsync("[OnGiveFeedback] ✅ Move効果: 掴みカーソル(Hand)設定");
                    e.UseDefaultCursors = false;
                    e.Handled = true;
                }
                else if (e.Effects.HasFlag(DragDropEffects.Copy))
                {
                    // ファイルドロップ用: コピーカーソル
                    Mouse.SetCursor(Cursors.Cross);
                    await AppendDebugLogAsync("[OnGiveFeedback] ✅ Copy効果: コピーカーソル(Cross)設定");
                    e.UseDefaultCursors = false;
                    e.Handled = true;
                }
                else if (e.Effects == DragDropEffects.None)
                {
                    // ドロップ不可: 禁止カーソル
                    Mouse.SetCursor(Cursors.No);
                    await AppendDebugLogAsync("[OnGiveFeedback] ⚠️ None効果: 禁止カーソル(No)設定");
                    e.UseDefaultCursors = false;
                    e.Handled = true;
                }
                else
                {
                    // その他: デフォルトカーソル使用
                    await AppendDebugLogAsync($"[OnGiveFeedback] 🔄 その他効果({e.Effects}): デフォルトカーソル使用");
                    e.UseDefaultCursors = true;
                }
            }
            catch (Exception ex)
            {
                await AppendDebugLogAsync($"[OnGiveFeedback] ❌ エラー: {ex.Message}");
                // エラー時はデフォルトカーソルにフォールバック
                e.UseDefaultCursors = true;
            }
        }

        private static async Task StartDragAsync(FrameworkElement source, MouseEventArgs e)
        {
            try
            {
                await AppendDebugLogAsync($"[StartDragAsync] 開始 - source: {source?.GetType().Name}, DataContext: {source?.DataContext?.GetType().Name ?? "null"}");
                
                var dragHandler = GetDragHandler(source);
                await AppendDebugLogAsync($"[StartDragAsync] dragHandler: {dragHandler?.GetType().Name ?? "null"}");
                
                if (dragHandler != null)
                {
                    var dragInfo = new V3DragInfo(source, e);
                    await AppendDebugLogAsync($"[StartDragAsync] V3DragInfo作成完了 - SourceItem: {dragInfo.SourceItem?.GetType().Name ?? "null"}");
                    
                    var dragData = await dragHandler.StartDragAsync(dragInfo);
                    await AppendDebugLogAsync($"[StartDragAsync] dragHandler.StartDragAsync完了 - dragData: {dragData?.GetType().Name ?? "null"}");
                    
                    if (dragData != null)
                    {
                        await AppendDebugLogAsync("[StartDragAsync] DragDrop.DoDragDrop実行開始");
                        
                        // 🎯 OSS標準: ビジュアルフィードバック表示
                        var adorner = CreateDragAdorner(source, dragData);
                        
                        var result = DragDrop.DoDragDrop(source, dragData, DragDropEffects.Copy | DragDropEffects.Move);
                        await AppendDebugLogAsync($"[StartDragAsync] DragDrop.DoDragDrop完了 - result: {result}");
                        
                        // 🎯 OSS標準: ドラッグ完了処理
                        await dragHandler.DragCompletedAsync(new V3DragCompletedInfo(dragInfo, result));
                        
                        // Adorner削除
                        RemoveDragAdorner(adorner);
                    }
                    else
                    {
                        await AppendDebugLogAsync("[StartDragAsync] dragData is null - ドラッグ開始キャンセル");
                    }
                }
                else
                {
                    await AppendDebugLogAsync("[StartDragAsync] dragHandler is null - ドラッグ不可");
                }
            }
            catch (Exception ex)
            {
                await AppendDebugLogAsync($"[StartDragAsync] 例外: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"🚨 V3 DragStart Error: {ex.Message}");
            }
            finally
            {
                _isDragging = false;
                await AppendDebugLogAsync("[StartDragAsync] 終了 - _isDragging = false");
            }
        }

        #endregion

        #region OSS標準: ドロップターゲット処理

        private static async void OnDragEnter(object sender, DragEventArgs e)
        {
            await AppendDebugLogAsync("OnDragEnter - ドラッグエンターイベント発火");
            await HandleDragOverAsync(sender as FrameworkElement, e);
        }

        private static async void OnDragOver(object sender, DragEventArgs e)
        {
            await AppendDebugLogAsync("OnDragOver - ドラッグオーバーイベント発火");
            await HandleDragOverAsync(sender as FrameworkElement, e);
        }

        private static async Task HandleDragOverAsync(FrameworkElement target, DragEventArgs e)
        {
            try
            {
                var dropHandler = GetDropHandler(target);
                if (dropHandler != null)
                {
                    var dropInfo = new V3DropInfo(e, target);
                    var canDrop = await dropHandler.CanDropAsync(dropInfo);
                    
                    e.Effects = canDrop ? DragDropEffects.Copy : DragDropEffects.None;
                    
                    // 🎯 Phase 1: 詳細な挿入位置判定
                    var insertionInfo = CalculateInsertionInfo(e, target);
                    if (insertionInfo != null && canDrop)
                    {
                        await AppendDebugLogAsync($"[DragOver] 挿入位置: {insertionInfo.Position} at Y:{insertionInfo.MousePosition.Y:F1}");
                        
                        // 🎯 Phase 2: 挿入位置インジケーター表示
                        ShowInsertionIndicator(insertionInfo);
                    }
                    
                    // 🎯 OSS標準: ドロップゾーンビジュアルフィードバック
                    ShowDropZoneFeedback(target, canDrop);
                    
                    // 🎯 V3.0.125: 自動スクロール処理（境界領域検出）
                    HandleAutoScrollDuringDrag(target, e);
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"🚨 V3 DragOver Error: {ex.Message}");
                await AppendDebugLogAsync($"[HandleDragOverAsync] エラー: {ex.Message}");
                e.Effects = DragDropEffects.None;
            }
            
            e.Handled = true;
        }

        private static async void OnDrop(object sender, DragEventArgs e)
        {
            // 🎯 V3.0.025: イベント重複防止チェック
            if (_isDropProcessing)
            {
                await AppendDebugLogAsync("OnDrop - イベント重複検出: 処理をスキップします");
                e.Handled = true;
                return;
            }

            // 🎯 V3.0.025: 処理開始フラグ設定
            _isDropProcessing = true;
            await AppendDebugLogAsync("OnDrop開始 - ドラッグ&ドロップイベント発火 (_isDropProcessing = true)");
            
            try
            {
                var target = sender as FrameworkElement;
                await AppendDebugLogAsync($"OnDrop - target: {target?.GetType().Name ?? "null"}");
                
                var dropHandler = GetDropHandler(target);
                await AppendDebugLogAsync($"OnDrop - dropHandler: {dropHandler?.GetType().Name ?? "null"}");
                
                if (dropHandler != null)
                {
                    await AppendDebugLogAsync("OnDrop - V3DropInfo作成開始");
                    var dropInfo = new V3DropInfo(e, target);
                    await AppendDebugLogAsync("OnDrop - dropHandler.DropAsync呼び出し開始");
                    await dropHandler.DropAsync(dropInfo);
                    await AppendDebugLogAsync("OnDrop - dropHandler.DropAsync完了");
                }
                else
                {
                    await AppendDebugLogAsync("OnDrop - dropHandlerがnullのため処理スキップ");
                }
                
                // 🎯 OSS標準: ドロップゾーンフィードバック削除
                HideDropZoneFeedback(target);
                await AppendDebugLogAsync("OnDrop正常完了");
            }
            catch (Exception ex)
            {
                await AppendDebugLogAsync($"OnDrop例外発生: {ex.Message}");
                await AppendDebugLogAsync($"OnDropスタックトレース: {ex.StackTrace}");
                System.Diagnostics.Debug.WriteLine($"🚨 V3 Drop Error: {ex.Message}");
            }
            finally
            {
                // 🎯 V3.0.025: 処理完了フラグリセット
                _isDropProcessing = false;
                e.Handled = true;
                await AppendDebugLogAsync("OnDrop終了 - e.Handled = true (_isDropProcessing = false)");
            }
        }

        private static void OnDragLeave(object sender, DragEventArgs e)
        {
            // 🎯 OSS標準: ドロップゾーンフィードバック削除
            HideDropZoneFeedback(sender as FrameworkElement);
            
            // 🎯 Phase 2: 挿入位置インジケーター非表示
            HideInsertionIndicator();
        }

        #endregion

        #region OSS標準: ビジュアルフィードバック

        private static Adorner CreateDragAdorner(FrameworkElement source, object dragData)
        {
            // 🎯 OSS標準: ドラッグプレビューAdorner作成
            var adornerLayer = AdornerLayer.GetAdornerLayer(source);
            if (adornerLayer != null)
            {
                var adorner = new V3DragPreviewAdorner(source, dragData);
                adornerLayer.Add(adorner);
                return adorner;
            }
            return null;
        }

        private static void RemoveDragAdorner(Adorner adorner)
        {
            if (adorner != null)
            {
                var adornerLayer = AdornerLayer.GetAdornerLayer(adorner.AdornedElement);
                adornerLayer?.Remove(adorner);
            }
        }

        private static void ShowDropZoneFeedback(FrameworkElement target, bool canDrop)
        {
            if (target != null)
            {
                // 🎯 OSS標準: ドロップゾーン視覚化
                var brush = canDrop ? 
                    new SolidColorBrush(Color.FromArgb(100, 0, 255, 0)) : // 半透明緑
                    new SolidColorBrush(Color.FromArgb(100, 255, 0, 0));   // 半透明赤
                
                // 🎯 OSS標準: パネル系のBackground設定サポート
                if (target is Panel panel)
                    panel.Background = brush;
                else if (target is Control control)
                    control.Background = brush;
            }
        }

        private static void HideDropZoneFeedback(FrameworkElement target)
        {
            if (target != null)
            {
                // 🎯 OSS標準: パネル系のBackgroundクリアサポート
                if (target is Panel panel)
                    panel.ClearValue(Panel.BackgroundProperty);
                else if (target is Control control)
                    control.ClearValue(Control.BackgroundProperty);
            }
        }

        #endregion

        #region 🎯 Phase 1: 挿入位置判定ロジック拡張

        /// <summary>
        /// 挿入位置の種類
        /// </summary>
        public enum InsertionPosition
        {
            Before,  // 対象アイテムの前に挿入
            After,   // 対象アイテムの後に挿入
            On       // 対象アイテムの位置に置換
        }

        /// <summary>
        /// 挿入位置情報
        /// </summary>
        public class InsertionInfo
        {
            public InsertionPosition Position { get; set; }
            public FrameworkElement TargetItem { get; set; }
            public V3PageViewModel TargetData { get; set; }
            public Point MousePosition { get; set; }
        }

        /// <summary>
        /// マウス位置に基づく詳細な挿入位置計算
        /// </summary>
        private static InsertionInfo CalculateInsertionInfo(DragEventArgs e, FrameworkElement target)
        {
            try
            {
                var position = e.GetPosition(target);
                var listBoxItem = FindAncestor<ListBoxItem>(target);
                
                if (listBoxItem != null)
                {
                    var itemPosition = e.GetPosition(listBoxItem);
                    var itemHeight = listBoxItem.ActualHeight;
                    
                    InsertionPosition insertPos;
                    if (itemPosition.Y < itemHeight / 3)
                        insertPos = InsertionPosition.Before;
                    else if (itemPosition.Y > itemHeight * 2 / 3)
                        insertPos = InsertionPosition.After;
                    else
                        insertPos = InsertionPosition.On;
                        
                    return new InsertionInfo
                    {
                        Position = insertPos,
                        TargetItem = listBoxItem,
                        TargetData = listBoxItem.DataContext as V3PageViewModel,
                        MousePosition = itemPosition
                    };
                }
                
                return null;
            }
            catch (Exception ex)
            {
                _ = AppendDebugLogAsync($"[CalculateInsertionInfo] エラー: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// UIツリー内で指定した型の祖先要素を検索
        /// </summary>
        private static T FindAncestor<T>(DependencyObject current) where T : class
        {
            do
            {
                if (current is T ancestor)
                    return ancestor;
                current = VisualTreeHelper.GetParent(current);
            }
            while (current != null);

            return null;
        }

        /// <summary>
        /// 🎯 V3.0.125: UIツリー内で指定した型の子孫要素を検索（深さ優先）
        /// </summary>
        private static T FindDescendant<T>(DependencyObject parent) where T : class
        {
            if (parent == null) return null;

            // 自身が該当型かチェック
            if (parent is T match)
                return match;

            // 子要素を再帰的に検索
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                var result = FindDescendant<T>(child);
                if (result != null)
                    return result;
            }

            return null;
        }

        /// <summary>
        /// 現在表示中の挿入インジケーター
        /// </summary>
        private static InsertionIndicatorAdorner _currentInsertionIndicator;

        /// <summary>
        /// 🎯 V3.0.125: 自動スクロールのスキップカウンター（3イベントに1回スクロール）
        /// </summary>
        private static int _autoScrollSkipCounter = 0;

        /// <summary>
        /// 挿入位置インジケーターの表示
        /// </summary>
        private static void ShowInsertionIndicator(InsertionInfo insertionInfo)
        {
            try
            {
                // 既存インジケーターをクリア
                HideInsertionIndicator();
                
                if (insertionInfo?.TargetItem != null)
                {
                    var adornerLayer = AdornerLayer.GetAdornerLayer(insertionInfo.TargetItem);
                    if (adornerLayer != null)
                    {
                        _currentInsertionIndicator = new InsertionIndicatorAdorner(
                            insertionInfo.TargetItem, 
                            insertionInfo.Position);
                        adornerLayer.Add(_currentInsertionIndicator);
                        
                        _ = AppendDebugLogAsync($"[ShowInsertionIndicator] 位置: {insertionInfo.Position}");
                    }
                }
            }
            catch (Exception ex)
            {
                _ = AppendDebugLogAsync($"[ShowInsertionIndicator] エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// 挿入位置インジケーターの非表示
        /// </summary>
        /// <summary>
        /// 🎯 V3.0.125: ドラッグ中の自動スクロール処理
        /// OSS参考: GongSolutions.WPF.DragDrop + Stack Overflow Best Practices
        /// 境界領域での距離比例スクロール実装
        ///
        /// アルゴリズム:
        /// - 上端境界: MouseY < 50px → 上方向スクロール（速度: 50 - MouseY）
        /// - 下端境界: MouseY > Height - 50px → 下方向スクロール（速度: MouseY - (Height - 50)）
        /// - 速度は距離に比例（境界端に近いほど高速）
        ///
        /// 統合:
        /// - HandleDragOverAsync内から呼び出し（canDrop時のみ）
        /// - FindDescendant<ScrollViewer>で子要素検索
        /// - VerticalOffset直接制御（UIスレッド同期実行）
        ///
        /// パフォーマンス:
        /// - DragOver頻度: 60-100 Hz
        /// - 処理時間: < 0.3ms（境界外は即リターン）
        /// - スクロール最大速度: 50px/イベント
        /// </summary>
        /// <param name="target">ドラッグターゲット要素（ListBox）</param>
        /// <param name="e">DragEventArgs（マウス位置取得用）</param>
        private static void HandleAutoScrollDuringDrag(FrameworkElement target, DragEventArgs e)
        {
            try
            {
                // Step 1: ScrollViewer検索（子要素から探す）
                var scrollViewer = FindDescendant<ScrollViewer>(target);
                _ = AppendDebugLogAsync($"[AutoScroll] Target={target.GetType().Name}, ScrollViewer: {(scrollViewer != null ? "Found" : "NULL")}");
                if (scrollViewer == null) return;

                // Step 2: マウス位置・コンテナサイズ取得
                // 🎯 OSS標準値: Tolerance=24px, ScrollSpeed=1-3px (Stack Overflow実装は3-20px)
                // 🎯 ユーザー要望: 3倍遅く → 3イベントに1回スクロール (1px/3イベント)
                const double autoScrollZone = 24.0; // 境界領域: 上下24px (OSS標準)
                const double scrollSpeed = 1.0; // スクロール速度: 1px/実行
                const int skipInterval = 3; // スキップ間隔: 3イベントに1回実行
                double mouseY = e.GetPosition(target).Y;
                double containerHeight = target.ActualHeight;

                // 境界外早期リターン（パフォーマンス最適化）
                if (mouseY >= autoScrollZone && mouseY <= containerHeight - autoScrollZone)
                {
                    _autoScrollSkipCounter = 0; // カウンターリセット
                    return; // 中央領域: スクロール不要
                }

                // 🎯 3イベントに1回だけスクロール実行（3倍遅く）
                _autoScrollSkipCounter++;
                if (_autoScrollSkipCounter < skipInterval)
                {
                    return; // スキップ
                }
                _autoScrollSkipCounter = 0; // カウンターリセット

                // Step 3: 上端境界領域での自動スクロール
                if (mouseY < autoScrollZone && mouseY >= 0)
                {
                    // 🎯 3イベントに1回: 1px/3イベント = 約20-33px/秒 (1px/イベントの1/3速度)
                    double newOffset = Math.Max(0, scrollViewer.VerticalOffset - scrollSpeed);
                    scrollViewer.ScrollToVerticalOffset(newOffset);
                    _ = AppendDebugLogAsync($"[AutoScroll] 上方向: MouseY={mouseY:F1}, Speed={scrollSpeed}, NewOffset={newOffset:F1}");
                }
                // Step 4: 下端境界領域での自動スクロール
                else if (mouseY > containerHeight - autoScrollZone && mouseY <= containerHeight)
                {
                    // 🎯 3イベントに1回: 1px/3イベント = 約20-33px/秒 (1px/イベントの1/3速度)
                    double newOffset = Math.Min(scrollViewer.ScrollableHeight,
                                                 scrollViewer.VerticalOffset + scrollSpeed);
                    scrollViewer.ScrollToVerticalOffset(newOffset);
                    _ = AppendDebugLogAsync($"[AutoScroll] 下方向: MouseY={mouseY:F1}, Speed={scrollSpeed}, NewOffset={newOffset:F1}");
                }
            }
            catch (Exception ex)
            {
                // エラーハンドリング（スクロール失敗は致命的ではない）
                System.Diagnostics.Debug.WriteLine($"⚠️ AutoScroll Error: {ex.Message}");
                _ = AppendDebugLogAsync($"[HandleAutoScrollDuringDrag] エラー: {ex.Message}");
            }
        }

        private static void HideInsertionIndicator()
        {
            try
            {
                if (_currentInsertionIndicator != null)
                {
                    var adornerLayer = AdornerLayer.GetAdornerLayer(_currentInsertionIndicator.AdornedElement);
                    adornerLayer?.Remove(_currentInsertionIndicator);
                    _currentInsertionIndicator = null;
                    
                    _ = AppendDebugLogAsync("[HideInsertionIndicator] インジケーター非表示");
                }
            }
            catch (Exception ex)
            {
                _ = AppendDebugLogAsync($"[HideInsertionIndicator] エラー: {ex.Message}");
            }
        }

        #endregion

        }
}