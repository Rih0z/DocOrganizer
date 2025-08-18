using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Controls;

namespace DocOrganizer.UI.Adorners
{
    /// <summary>
    /// 🎯 V3 OSS標準: ドラッグプレビューAdorner
    /// GongSolutions.WPF.DragDrop、Microsoft公式パターン準拠
    /// </summary>
    public class V3DragPreviewAdorner : Adorner
    {
        private readonly FrameworkElement _child;
        private readonly AdornerLayer _adornerLayer;
        private Point _mousePosition;

        /// <summary>
        /// 🎯 OSS標準: Adorner初期化
        /// </summary>
        public V3DragPreviewAdorner(UIElement adornedElement, object dragData) : base(adornedElement)
        {
            // 🎯 OSS標準: ヒットテスト無効化（重要）
            IsHitTestVisible = false;
            
            _adornerLayer = AdornerLayer.GetAdornerLayer(adornedElement);
            _child = CreatePreviewElement(dragData);
            
            // 🎯 OSS標準: マウス移動追跡（修正済みAPI使用）
            adornedElement.QueryContinueDrag += OnQueryContinueDrag;
        }

        /// <summary>
        /// 🎯 OSS標準: プレビュー要素作成
        /// </summary>
        private FrameworkElement CreatePreviewElement(object dragData)
        {
            if (dragData is string[] filePaths && filePaths.Length > 0)
            {
                return CreateFilePreviewElement(filePaths);
            }
            else if (dragData is FrameworkElement element)
            {
                return CreateElementPreviewElement(element);
            }
            else
            {
                return CreateGenericPreviewElement(dragData);
            }
        }

        /// <summary>
        /// 🎯 OSS標準: ファイルプレビュー要素作成
        /// </summary>
        private FrameworkElement CreateFilePreviewElement(string[] filePaths)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(200, 70, 130, 180)),
                BorderBrush = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8, 12, 8),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 8,
                    ShadowDepth = 4,
                    Opacity = 0.3
                }
            };

            var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };
            
            // 🎯 OSS標準: アイコン表示
            var icon = new TextBlock
            {
                Text = "📁",
                FontSize = 24,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            // 🎯 OSS標準: ファイル情報表示
            var textBlock = new TextBlock
            {
                Text = filePaths.Length == 1 
                    ? System.IO.Path.GetFileName(filePaths[0])
                    : $"{filePaths.Length} files",
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center
            };

            stackPanel.Children.Add(icon);
            stackPanel.Children.Add(textBlock);
            border.Child = stackPanel;

            return border;
        }

        /// <summary>
        /// 🎯 OSS標準: 要素プレビュー作成（VisualBrushパターン）
        /// </summary>
        private FrameworkElement CreateElementPreviewElement(FrameworkElement originalElement)
        {
            var border = new Border
            {
                Width = Math.Min(originalElement.ActualWidth, 200),
                Height = Math.Min(originalElement.ActualHeight, 150),
                Background = new VisualBrush(originalElement)
                {
                    Opacity = 0.7,
                    Stretch = Stretch.Uniform
                },
                BorderBrush = new SolidColorBrush(Colors.Gray),
                BorderThickness = new Thickness(1),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 6,
                    ShadowDepth = 3,
                    Opacity = 0.4
                }
            };

            return border;
        }

        /// <summary>
        /// 🎯 OSS標準: 汎用プレビュー要素作成
        /// </summary>
        private FrameworkElement CreateGenericPreviewElement(object dragData)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(180, 100, 100, 100)),
                BorderBrush = new SolidColorBrush(Colors.DarkGray),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 4,
                    ShadowDepth = 2,
                    Opacity = 0.3
                }
            };

            var textBlock = new TextBlock
            {
                Text = "📋 Dragging...",
                Foreground = Brushes.White,
                FontSize = 12,
                FontWeight = FontWeights.Medium
            };

            border.Child = textBlock;
            return border;
        }

        /// <summary>
        /// 🎯 OSS標準: マウス位置更新
        /// </summary>
        private void OnQueryContinueDrag(object sender, QueryContinueDragEventArgs e)
        {
            _mousePosition = Mouse.GetPosition(_adornerLayer);
            InvalidateVisual();
        }

        /// <summary>
        /// 🎯 OSS標準: 子要素数
        /// </summary>
        protected override int VisualChildrenCount => 1;

        /// <summary>
        /// 🎯 OSS標準: 子要素取得
        /// </summary>
        protected override Visual GetVisualChild(int index)
        {
            if (index == 0)
                return _child;
            
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        /// <summary>
        /// 🎯 OSS標準: サイズ測定
        /// </summary>
        protected override Size MeasureOverride(Size constraint)
        {
            _child.Measure(constraint);
            return _child.DesiredSize;
        }

        /// <summary>
        /// 🎯 OSS標準: レイアウト配置
        /// </summary>
        protected override Size ArrangeOverride(Size finalSize)
        {
            // 🎯 OSS標準: マウス位置に追従配置
            var offsetX = 10; // マウスから少しオフセット
            var offsetY = 10;
            
            var rect = new Rect(
                _mousePosition.X + offsetX, 
                _mousePosition.Y + offsetY, 
                _child.DesiredSize.Width, 
                _child.DesiredSize.Height);
                
            _child.Arrange(rect);
            return finalSize;
        }

        /// <summary>
        /// 🎯 OSS標準: Adorner削除
        /// </summary>
        public void Remove()
        {
            if (_adornerLayer != null)
            {
                AdornedElement.QueryContinueDrag -= OnQueryContinueDrag;
                _adornerLayer.Remove(this);
            }
        }

        /// <summary>
        /// 🎯 OSS標準: リソース解放
        /// </summary>
        protected override void OnRender(DrawingContext drawingContext)
        {
            // 🎯 OSS標準: 背景透明化
            drawingContext.DrawRectangle(Brushes.Transparent, null, new Rect(RenderSize));
            base.OnRender(drawingContext);
        }
    }

    /// <summary>
    /// 🎯 V3 OSS標準: ドロップゾーンハイライトAdorner
    /// </summary>
    public class V3DropZoneAdorner : Adorner
    {
        private readonly Brush _highlightBrush;
        private readonly Pen _borderPen;

        public V3DropZoneAdorner(UIElement adornedElement, bool canDrop) : base(adornedElement)
        {
            IsHitTestVisible = false;
            
            if (canDrop)
            {
                _highlightBrush = new SolidColorBrush(Color.FromArgb(60, 0, 255, 0)); // 半透明緑
                _borderPen = new Pen(new SolidColorBrush(Colors.Green), 2) { DashStyle = DashStyles.Dash };
            }
            else
            {
                _highlightBrush = new SolidColorBrush(Color.FromArgb(60, 255, 0, 0)); // 半透明赤
                _borderPen = new Pen(new SolidColorBrush(Colors.Red), 2) { DashStyle = DashStyles.Dash };
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            var rect = new Rect(RenderSize);
            
            // 🎯 OSS標準: ドロップゾーン視覚化
            drawingContext.DrawRectangle(_highlightBrush, _borderPen, rect);
            
            base.OnRender(drawingContext);
        }
    }
}