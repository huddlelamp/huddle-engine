using System;

namespace Huddle.Engine.Extensions
{
    public static class NumericExtensions
    {
        public static double DegreeToRadians(this double angle)
        {
            return angle * (Math.PI / 180);
        }

        public static double RadiansToDegree(this double angle)
        {
            return angle * (180.0 / Math.PI);
        }

        public static double DegreeToRadians(this float angle)
        {
            return angle * (Math.PI / 180);
        }

        public static double RadiansToDegree(this float angle)
        {
            return angle * (180.0 / Math.PI);
        }
    }
}
