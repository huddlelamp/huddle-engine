using System.Windows;

namespace Huddle.Engine.Extensions
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
