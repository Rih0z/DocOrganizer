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
// Microsoft.Xaml.Behaviors.Wpfは未使用 - 削除
using DocOrganizer.UI.ViewModels.V3;
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
                
                // 🎯 第16条準拠: release/DEBUG_LOG.txt に統一
                var exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
                var logFilePath = System.IO.Path.Combine(exeDirectory, "DEBUG_LOG.txt");
                
                await File.AppendAllTextAsync(logFilePath, logEntry);
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
                    element.MouseLeftButtonDown += OnMouseLeftButtonDown;
                }
                else
                {
                    element.MouseMove -= OnMouseMove;
                    element.MouseLeftButtonDown -= OnMouseLeftButtonDown;
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

        private static void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
            _isDragging = false;
        }

        private static async void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && !_isDragging)
            {
                var currentPosition = e.GetPosition(null);
                var diff = _dragStartPoint - currentPosition;

                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    _isDragging = true;
                    await StartDragAsync(sender as FrameworkElement, e);
                }
            }
        }

        private static async Task StartDragAsync(FrameworkElement source, MouseEventArgs e)
        {
            try
            {
                var dragHandler = GetDragHandler(source);
                if (dragHandler != null)
                {
                    var dragInfo = new V3DragInfo(source, e);
                    var dragData = await dragHandler.StartDragAsync(dragInfo);
                    
                    if (dragData != null)
                    {
                        // 🎯 OSS標準: ビジュアルフィードバック表示
                        var adorner = CreateDragAdorner(source, dragData);
                        
                        var result = DragDrop.DoDragDrop(source, dragData, DragDropEffects.Copy | DragDropEffects.Move);
                        
                        // 🎯 OSS標準: ドラッグ完了処理
                        await dragHandler.DragCompletedAsync(new V3DragCompletedInfo(dragInfo, result));
                        
                        // Adorner削除
                        RemoveDragAdorner(adorner);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"🚨 V3 DragStart Error: {ex.Message}");
            }
            finally
            {
                _isDragging = false;
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
                    
                    // 🎯 OSS標準: ドロップゾーンビジュアルフィードバック
                    ShowDropZoneFeedback(target, canDrop);
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"🚨 V3 DragOver Error: {ex.Message}");
                e.Effects = DragDropEffects.None;
            }
            
            e.Handled = true;
        }

        private static async void OnDrop(object sender, DragEventArgs e)
        {
            await AppendDebugLogAsync("OnDrop開始 - ドラッグ&ドロップイベント発火");
            
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
                e.Handled = true;
                await AppendDebugLogAsync("OnDrop終了 - e.Handled = true");
            }
        }

        private static void OnDragLeave(object sender, DragEventArgs e)
        {
            // 🎯 OSS標準: ドロップゾーンフィードバック削除
            HideDropZoneFeedback(sender as FrameworkElement);
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
    }
}