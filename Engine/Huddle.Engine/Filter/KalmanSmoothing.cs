using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Huddle.Engine.Processor.OpenCv.Filter;

namespace Huddle.Engine.Filter
{
    public class KalmanSmoothing : ISmoothing
    {
        #region private members
        
        private readonly KalmanFilter _kalmanFilter = new KalmanFilter();

        #endregion

        public Point SmoothPoint(Point point)
        {
            throw new NotImplementedException();
        }

        public double SmoothAngle(double angle)
        {
            throw new NotImplementedException();
        }
    }
}
