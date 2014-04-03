using System;
using System.Windows;
using ThicknessConverter = Xceed.Wpf.DataGrid.Converters.ThicknessConverter;

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

        public static double Length(this System.Drawing.Point self, System.Drawing.Point point)
        {
            return Math.Sqrt(Math.Pow(self.X - point.X, 2) + Math.Pow(self.Y - point.Y, 2));
        }
    }
}
