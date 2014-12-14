using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Huddle.Engine.Filter.Impl;
using Huddle.Engine.Processor.OpenCv.Filter;

namespace Huddle.Engine.Filter
{
    public class KalmanSmoothing : ISmoothing
    {
        #region private members

        private const double Factor = 10000.0;

        private readonly KalmanFilter _positionFilter = new KalmanFilter();
        private readonly KalmanFilter _angleFilter = new KalmanFilter();
        private readonly KalmanFilter _depthFilter = new KalmanFilter();

        #endregion

        public Point SmoothPoint(Point point)
        {
            var estimatedPoint = _positionFilter.GetEstimatedPoint(new System.Drawing.Point((int)(point.X * Factor), (int)(point.Y * Factor)));
            return new Point(estimatedPoint.X / Factor, estimatedPoint.Y / Factor);
        }

        public double SmoothAngle(double angle)
        {
            var estimatedPoint = _angleFilter.GetEstimatedPoint(new System.Drawing.Point((int)(angle * Factor), 0));
            return estimatedPoint.X / Factor;
        }

        public double SmoothDepth(double depth)
        {
            var estimatedPoint = _depthFilter.GetEstimatedPoint(new System.Drawing.Point((int)(depth * Factor), 0));
            return estimatedPoint.X / Factor;
        }
    }
}
