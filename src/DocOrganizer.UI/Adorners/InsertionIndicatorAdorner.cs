using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using DocOrganizer.UI.Behaviors;

namespace DocOrganizer.UI.Adorners
{
    /// <summary>
    /// 🎯 Phase 2: 挿入位置インジケーターAdorner
    /// OSS標準パターン準拠による視覚的フィードバック
    /// </summary>
    public class InsertionIndicatorAdorner : Adorner
    {
        private readonly V3AdvancedDragDropBehavior.InsertionPosition _position;
        private readonly Brush _indicatorBrush;
        private readonly Pen _indicatorPen;

        /// <summary>
        /// 挿入インジケーターAdorner初期化
        /// </summary>
        /// <param name="adornedElement">装飾対象要素</param>
        /// <param name="position">挿入位置</param>
        public InsertionIndicatorAdorner(UIElement adornedElement, V3AdvancedDragDropBehavior.InsertionPosition position) 
            : base(adornedElement)
        {
            _position = position;
            
            // 🎯 V3カラーガイドライン準拠: #3399FF (青色)
            _indicatorBrush = new SolidColorBrush(Color.FromRgb(51, 153, 255));
            _indicatorPen = new Pen(_indicatorBrush, 3);
            
            // 🎯 OSS標準: ヒットテスト無効化（重要）
            IsHitTestVisible = false;
        }

        /// <summary>
        /// 挿入位置インジケーターの描画
        /// </summary>
        protected override void OnRender(DrawingContext drawingContext)
        {
            var rect = new Rect(AdornedElement.RenderSize);
            
            switch (_position)
            {
                case V3AdvancedDragDropBehavior.InsertionPosition.Before:
                    // 上端に青いライン表示
                    drawingContext.DrawLine(_indicatorPen, rect.TopLeft, rect.TopRight);
                    break;
                    
                case V3AdvancedDragDropBehavior.InsertionPosition.After:
                    // 下端に青いライン表示
                    drawingContext.DrawLine(_indicatorPen, rect.BottomLeft, rect.BottomRight);
                    break;
                    
                case V3AdvancedDragDropBehavior.InsertionPosition.On:
                    // 全体を半透明青でハイライト表示
                    var highlightBrush = new SolidColorBrush(Color.FromArgb(100, 51, 153, 255));
                    drawingContext.DrawRectangle(highlightBrush, null, rect);
                    break;
            }
        }
    }
}