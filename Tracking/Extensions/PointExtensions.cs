using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Tools.FlockingDevice.Tracking.Extensions
{
    public static class PointExtensions
    {
        public static Point Scale(this Point point, double scale)
        {
            point.X /= scale;
            point.Y /= scale;
            return point;
        }
    }
}
