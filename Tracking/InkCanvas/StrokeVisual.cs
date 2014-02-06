using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Tools.FlockingDevice.Tracking.InkCanvas
{
    public class StrokeVisual : UIElement
    {
        #region private fields

        private readonly VisualCollection _visualChildren;

        private readonly Color _color;

        #endregion

        #region public properties

        #region Points

        private List<Point> _points;
        public IEnumerable<Point> Points
        {
            get
            {
                return _points;
            }
        }

        #endregion

        #endregion

        #region ctor

        public StrokeVisual(Point startPoint, Color color)
        {
            _visualChildren = new VisualCollection(this);
            _color = color;
            _points = new List<Point> { startPoint };
        }

        #endregion

        #region private methods

        private void DrawLine(Point startPoint, Point targetPoint)
        {
            var drawingVisual = new DrawingVisual();

            using (DrawingContext dc = drawingVisual.RenderOpen())
            {
                dc.DrawLine(new Pen(new SolidColorBrush(_color), 2.0), startPoint, targetPoint);
            }
            _visualChildren.Add(drawingVisual);
        }

        #endregion

        #region public methods

        public void AddPoint(Point newPoint)
        {
            DrawLine(_points.Last(), newPoint);
            _points.Add(newPoint);
        }

        #endregion

        #region overrides

        protected override int VisualChildrenCount
        {
            get
            {
                return _visualChildren.Count;
            }
        }

        protected override Visual GetVisualChild(int index)
        {
            if (index < 0 || index >= _visualChildren.Count)
                throw new ArgumentOutOfRangeException("index");

            return _visualChildren[index];
        }

        #endregion
    }
}
