using System;
using System.Windows;
using Emgu.CV.Util;
using Huddle.Engine.Filter.Impl;

namespace Huddle.Engine.Filter
{
    public class OneEuroSmoothing : ISmoothing
    {
        #region private members

        private const double Rate = 30;

        private readonly OneEuroFilter _xFilter = new OneEuroFilter(2.0, 0.0);
        private readonly OneEuroFilter _yFilter = new OneEuroFilter(2.0, 0.0);
        private readonly OneEuroFilter _angleSXFilter = new OneEuroFilter(2.0, 0.0);
        private readonly OneEuroFilter _angleSYFilter = new OneEuroFilter(2.0, 0.0);
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
            return angle;

            //var radiansAngle = Math.PI * (angle % 360) / 180.0;

            //var sy = Math.Cos(radiansAngle);
            //var sx = Math.Sin(radiansAngle);

            //var smoothedAngleSX = _angleSXFilter.Filter(sx, Rate);
            //var smoothedAngleSY = _angleSYFilter.Filter(sy, Rate);

            //return Math.Atan2(smoothedAngleSX, smoothedAngleSY) * 180 / Math.PI;
        }

        public double SmoothDepth(double depth)
        {
            return _depthFilter.Filter(depth, Rate);
        }
    }
}
