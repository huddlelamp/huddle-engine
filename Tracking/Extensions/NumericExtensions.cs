using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tools.FlockingDevice.Tracking.Extensions
{
    public static class NumericExtensions
    {
        public static double DegreeToRadian(this double angle)
        {
            return angle * (Math.PI / 180);
        }

        public static double RandianToDegree(this double angle)
        {
            return angle * (180.0 / Math.PI);
        }
    }
}
