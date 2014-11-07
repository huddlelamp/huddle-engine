using System.Windows;
using Huddle.Engine.Filter.Impl;

namespace Huddle.Engine.Filter
{
    public class OneEuroSmoothing : ISmoothing
    {
        #region private members

        private const double Rate = 30;

        private readonly OneEuroFilter _xFilter = new OneEuroFilter(2.0, 0.0);
        private readonly OneEuroFilter _yFilter = new OneEuroFilter(2.0, 0.0);
        private readonly OneEuroFilter _angleFilter = new OneEuroFilter(2.0, 0.0);
        private readonly OneEuroFilter _depthFilter = new OneEuroFilter(2.0, 0.0);

        #endregion

        public Point SmoothPoint(Point point)
        {
            var x = _xFilter.Filter(point.X, Rate);
            var y = _yFilter.Filter(point.Y, Rate);
            return new Point(x, y);
        }

        public double SmoothAngle(double angle)
        {
            return _angleFilter.Filter(angle, Rate);
            //return angle;
        }

        public double SmoothDepth(double depth)
        {
            return _depthFilter.Filter(depth, Rate);
        }
    }
}
