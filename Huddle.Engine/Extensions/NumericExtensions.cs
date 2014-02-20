using System;

namespace Huddle.Engine.Extensions
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
