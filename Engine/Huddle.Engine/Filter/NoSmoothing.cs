using System.Windows;

namespace Huddle.Engine.Filter
{
    public class NoSmoothing : ISmoothing
    {
        public Point SmoothPoint(Point point)
        {
            return point;
        }

        public double SmoothAngle(double angle)
        {
            return angle;
        }

        public double SmoothDepth(double depth)
        {
            return depth;
        }
    }
}
